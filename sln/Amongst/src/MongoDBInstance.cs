using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Amongst.Exception;
using Amongst.Helper;
using Amongst.Output;
#if NETSTANDARD1_6
using System.Runtime.InteropServices;

#endif

namespace Amongst
{
    public class MongoDBInstance : IDisposable
    {
        private static readonly object Sync = new object();

        private static int _instanceCount;
        private static readonly Store Store = new Store();
        private static readonly List<int> PortsInUse = new List<int>();

        private const string BIN_WIN32 = "mongodb-win32-x86_64-2008plus-3.4.4";
        private const string BIN_LINUX = "mongodb-linux-x86_64-3.4.4";
        private const string BIN_OSX = "mongodb-osx-x86_64-3.4.4";

        private readonly string _binaryPath;
        private readonly MongoDBInstanceOptions _options;
        private readonly MongoDBConnection _connection;
        private readonly Process _process;
        private readonly ManualResetEventSlim _manualReset;

        private Mutex _mutex;

        public Guid Id;
        public string ConnectionString => _connection.ToString();
        public MongoDBInstanceState State { get; private set; }

        public static MongoDBInstance Spawn()
        {
            return new MongoDBInstance(new MongoDBInstanceOptions
            {
                LogVerbosity = LogVerbosity.Normal,
                OutputHelper = null,
                CleanBeforeRun = false,
                Persist = false,
                PackageDirectory = null
            });
        }

        public static MongoDBInstance Spawn(MongoDBInstanceOptions options)
        {
            return new MongoDBInstance(options);
        }

        private MongoDBInstance(MongoDBInstanceOptions options)
        {
            _options = options;

            lock (Sync)
                Store.Load();

            if (_options.Persist) {
                lock (Sync) {
                    if (Store.Persistence.Id == Guid.Empty)
                        Store.Persistence.Id = Guid.NewGuid();

                    Store.Persistence.LastRun = DateTime.Now;
                    Store.Save();
                }

                Id = Store.Persistence.Id;

                _options.OutputHelper.WriteLine($"[{DateTime.Now}][Info]: Persistence enabled. Instance id = {Id}");
            }
            else {
                Id = Guid.NewGuid();
            }

            var instancesPath = Path.Combine(Directory.GetCurrentDirectory(), "instances");
            if (_options.CleanBeforeRun && !_options.Persist)
                Directory.Delete(instancesPath, true);

            var instancePath = Path.Combine(instancesPath, $"{Id:N}");

            if (_options.OutputHelper == null) {
                var logDir = Path.Combine(instancePath, "logs");
                var logFile = Path.Combine(logDir, $"{DateTime.Now:hh-mm-ss_yy-MM-dd}.log");

                Directory.CreateDirectory(logDir);

                _options.OutputHelper = new TextFileOutputHelper(logFile);
            }

            AssertSingleInstance();

            _connection = new MongoDBConnection(
                IPAddress.Loopback,
                GetAvailablePort()
            );

            var dbPath = Path.Combine(instancePath, "data");
            Directory.CreateDirectory(dbPath);

            // Use Replace(): Uri cannot be created from UNIX path.
            var dbPathUri = dbPath.Replace("\\", "/");

            var dbLogLevel = _options.LogVerbosity == LogVerbosity.Quiet
                ? "--quiet"
                : _options.LogVerbosity == LogVerbosity.Verbose
                    ? "--verbose"
                    : null;

            var args = new[]
            {
                $"--bind_ip {_connection.IP}",
                $"--port {_connection.Port}",
                $"--dbpath {dbPathUri}",
                "--noprealloc",
                "--smallfiles",
                "--nojournal",
                $"{dbLogLevel}"
            };

            _binaryPath = GetBinaryPath();
            var fullPath = Path.Combine(_binaryPath, "mongod");

#if NETSTANDARD1_6
            if (IsUnix())
                SetExecutableBit(fullPath);
#endif

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = string.Join(" ", args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            _process.OutputDataReceived += FetchReadyState;
            _process.OutputDataReceived += OnOutputDataReceived;
            _process.ErrorDataReceived += OnErrorDataReceived;
            _process.Exited += OnExited;

            _process.Start();

            State = MongoDBInstanceState.Starting;

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            const int timeout = 30;

            _manualReset = new ManualResetEventSlim();
            _manualReset.Wait(TimeSpan.FromSeconds(timeout));

            if (!_manualReset.IsSet)
                throw new TimeoutException($"mongod failed to start after {timeout} seconds.");

            _instanceCount++;
        }

        private void AssertSingleInstance()
        {
            const string moduleGuid = "9A66C037-5217-4125-82F8-DBB7C6E415AB";

            _mutex = new Mutex(false, $"Global\\{moduleGuid}");
            if (!_mutex.WaitOne(0)) {
                _mutex.Dispose();

                if (!_options.AllowMultipleRunners)
                    throw new MultipleTestRunnerInstancesException("Multiple test runner instances detected.");
            }

            if (_instanceCount > 1)
                _options.OutputHelper.WriteLine(
                    $"[{DateTime.Now}][Warning]: You already spawned {_instanceCount} instances of mongod. " +
                    "It is recommended to share a MongoDB instance across your tests using a fixture. " +
                    "You can read more about shared context within xUnit here: https://xunit.github.io/docs/shared-context.html. " +
                    "If this is intentional, you can igore this Warning.");
        }

        private void FetchReadyState(object sender, DataReceivedEventArgs e)
        {
            const string pattern = "waiting for connections on port";

            var process = sender as Process;
            if (string.IsNullOrEmpty(e.Data) || !e.Data.Contains(pattern)) return;

            State = MongoDBInstanceState.Running;

            // ReSharper disable once PossibleNullReferenceException
            process.OutputDataReceived -= FetchReadyState;

            _manualReset.Set();
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            if (string.IsNullOrEmpty(e.Data)) return;

            // ReSharper disable once PossibleNullReferenceException
            var pidAndName = process.HasExited ? null : $"[{process.ProcessName}:{process.Id}]";

            _options.OutputHelper.WriteLine($"[{DateTime.Now}][Info][{Id:N}]{pidAndName}: {e.Data}");
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            if (string.IsNullOrEmpty(e.Data)) return;

            // ReSharper disable once PossibleNullReferenceException
            var pidAndName = process.HasExited ? null : $"[{process.ProcessName}:{process.Id}]";

            _options.OutputHelper.WriteLine($"[{DateTime.Now}][Error][{Id:N}]{pidAndName}: {e.Data}");
        }

        private void OnExited(object sender, EventArgs e)
        {
            State = MongoDBInstanceState.Stopped;

            lock (Sync) {
                PortsInUse.Remove(_connection.Port);
            }

            var process = sender as Process;
            var prefix = $"[{DateTime.Now}][Info][{Id:N}]";

            // ReSharper disable once PossibleNullReferenceException
            _options.OutputHelper.WriteLine($"{prefix}: Instance stopped with exit code {process.ExitCode}");

            process.Exited -= OnExited;
        }

#pragma warning disable CS0162
        private string GetBinaryPath()
        {
#if NETSTANDARD1_6
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            var isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            var osDescription = RuntimeInformation.OSDescription.Trim();
#elif NET46
            const bool isWindows = true;
            const bool isLinux = false;
            const bool isOSX = false;

            var osDescription = $"{Environment.OSVersion}".Trim();
#endif
            string build;
            if (isWindows)
                build = BIN_WIN32;
            else if (isLinux)
                build = BIN_LINUX;
            else if (isOSX)
                build = BIN_OSX;
            else
                throw new PlatformNotSupportedException($"Platform {osDescription} is not supported.");

            var prefix = $"[{DateTime.Now}][Info]";
            _options.OutputHelper.WriteLine(
                $"{prefix}: Detected {osDescription}. Using {build} binaries.");

            string binPath;
            if (Directory.Exists(Store.BinaryPath)) {
                binPath = Store.BinaryPath;

                _options.OutputHelper.WriteLine($"{prefix}: Using cached binary path.");
            }
            else {
                var binSegment = Path.Combine("tools", $"{build}", "bin");

                var homePath = isWindows
                    ? Environment.GetEnvironmentVariable("USERPROFILE")
                    : Environment.GetEnvironmentVariable("HOME");

                var searchPaths = new List<string>
                {
                    Directory.GetCurrentDirectory(),
                    Path.Combine(homePath, ".nuget", "packages"),
                    _options.PackageDirectory
                }.Where(p => !string.IsNullOrEmpty(p));

                binPath = searchPaths.Select(path =>
                    FolderSearch.FindDownwards(path, binSegment)
                    ?? FolderSearch.FindUpwards(path, binSegment)).FirstOrDefault();
            }

            if (binPath == null)
                throw new DirectoryNotFoundException(
                    "MongoDB binarys could not be found. " +
                    "If you have a custom package path, please specify it using the PackageDirectory option. " +
                    "If you already specified a custom package directory, double check if it is correct.");

            lock (Sync) {
                Store.BinaryPath = binPath;
                Store.Save();
            }

            _options.OutputHelper.WriteLine($"[{DateTime.Now}][Info]: Full binary path: {binPath}");

            return binPath;
        }
#pragma warning restore

#if NETSTANDARD1_6
        private static void SetExecutableBit(string file)
        {
            // Workaround until dotnet core 2.0
            var p = Process.Start("chmod", $"+x {file}");
            p.WaitForExit();

            if (p.ExitCode != 0)
                throw new CouldNotSetExecutableBit($"Could not set executable bit for {file}");
        }

        private static bool IsUnix()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }
#endif

        private static short GetAvailablePort()
        {
            const short begin = 27018;
            const short count = 100;

            var port = (short) Enumerable.Range(begin, count)
                .Except(PortsInUse)
                .FirstOrDefault(p =>
                {
                    var listener = new TcpListener(IPAddress.Loopback, p);
                    try {
                        listener.Start();
                    }
                    catch (SocketException) {
                        return false;
                    }
                    finally {
                        listener.Stop();
                    }

                    return true;
                });

            if (port < begin)
                throw new NoPortAvailableException(
                    $"Counld not spawn a new mongod instance. No port available within the range {begin}-{begin + count}");

            lock (Sync) {
                PortsInUse.Add(port);
            }

            return port;
        }

        public void Import(string database, string collection, string filePath, bool dropCollection = true,
            int timeout = 5000)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Could not import file {filePath}.");

            var fullPath = Path.Combine(_binaryPath, "mongoimport");

#if NETSTANDARD1_6
            if (IsUnix())
                SetExecutableBit(fullPath);
#endif

            filePath = filePath.Replace("\\", "/");

            var drop = dropCollection ? "--drop" : null;
            var logVerbosity = _options.LogVerbosity == LogVerbosity.Quiet
                ? "--quiet"
                : _options.LogVerbosity == LogVerbosity.Verbose
                    ? "--verbose"
                    : null;

            var args = new[]
            {
                $"--host {_connection.IP}:{_connection.Port}",
                $"--db {database}",
                $"--collection {collection}",
                $"--file {filePath}",
                $"{drop}",
                $"{logVerbosity}"
            };

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = string.Join(" ", args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            p.OutputDataReceived += OnOutputDataReceived;
            p.ErrorDataReceived += OnErrorDataReceived;

            p.Start();

            var exited = p.WaitForExit(timeout);
            if (!exited)
                throw new TimeoutException(
                    $"Mongoimport failed to import {filePath} to {database}/{collection} after {timeout} milliseconds.");

            if (p.ExitCode != 0)
                throw new ExitCodeException(
                    $"Mongoimport failed to import {filePath} to {database}/{collection}. Exit code {p.ExitCode}.");

            if (_options.LogVerbosity > LogVerbosity.Normal)
                _options.OutputHelper.WriteLine($"Successfully imported {filePath} to {database}/{collection}");
        }

        public void Export(string database, string collection, string filePath, int timeout = 5000)
        {
            var fullPath = Path.Combine(_binaryPath, "mongoexport");

#if NETSTANDARD1_6
            if (IsUnix())
                SetExecutableBit(fullPath);
#endif
            filePath = filePath.Replace("\\", "/");

            var logVerbosity = _options.LogVerbosity == LogVerbosity.Quiet
                ? "--quiet"
                : _options.LogVerbosity == LogVerbosity.Verbose
                    ? "--verbose"
                    : null;

            var args = new[]
            {
                $"--host {_connection.IP}:{_connection.Port}",
                $"--db {database}",
                $"--collection {collection}",
                $"--out {filePath}",
                $"{logVerbosity}"
            };

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = string.Join(" ", args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            p.OutputDataReceived += OnOutputDataReceived;
            p.ErrorDataReceived += OnErrorDataReceived;

            p.Start();

            var exited = p.WaitForExit(timeout);
            if (!exited)
                throw new TimeoutException(
                    $"Mongoexport failed to export {database}/{collection} to {filePath} after {timeout} milliseconds.");

            if (p.ExitCode != 0)
                throw new ExitCodeException(
                    $"Mongoexport failed to export {database}/{collection} to {filePath}. Exit code {p.ExitCode}.");

            if (_options.LogVerbosity > LogVerbosity.Normal)
                _options.OutputHelper.WriteLine($"Successfully exported {database}/{collection} to {filePath}");
        }

        public void Stop()
        {
            if (_process.HasExited) return;

            State = MongoDBInstanceState.Stopping;

            _process?.Kill();

            const int timeout = 5000;
            var exited = _process.WaitForExit(timeout);

            if (!exited)
                throw new TimeoutException($"Failed to stop mongod after {timeout} milliseconds.");

            State = MongoDBInstanceState.Stopped;

            _process?.CancelErrorRead();
            _process?.CancelOutputRead();
        }

        public void Dispose()
        {
            Stop();

            Dispose(true);
            GC.SuppressFinalize(this);

            _instanceCount--;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            _options.OutputHelper.Dispose();
            _mutex.Dispose();
        }
    }
}