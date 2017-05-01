using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Amongst.Exception;
using Amongst.Output;

#if NETSTANDARD1_6
using System.Runtime.InteropServices;
#endif

namespace Amongst
{
    public class MongoDBInstance : IDisposable
    {
        private static readonly List<int> PortsInUse = new List<int>();

        private const string BIN_WIN32 = "mongodb-win32-x86_64-2008plus-3.4.4";
        private const string BIN_LINUX = "mongodb-linux-x86_64-3.4.4";
        private const string BIN_OSX = "mongodb-osx-x86_64-3.4.4";

        private readonly IMongoDBInstanceOutputHelper _output;
        private readonly LogVerbosity _logVerbosity;

        private static int _instanceCount;

        private readonly string _binaryPath;
        private readonly MongoDBConnection _connection;
        private readonly Process _process;
        private readonly ManualResetEventSlim _manualReset;

        private Mutex _mutex;

        public Guid Id;
        public string ConnectionString => _connection.ToString();
        public MongoDBInstanceState State { get; private set; }

        public static MongoDBInstance Spawn(IMongoDBInstanceOutputHelper output = null,
            LogVerbosity logVerbosity = LogVerbosity.Quiet)
        {
            return new MongoDBInstance(output, logVerbosity);
        }

        private MongoDBInstance(IMongoDBInstanceOutputHelper output, LogVerbosity logVerbosity)
        {
            _output = output;
            _logVerbosity = logVerbosity;
           
            Id = Guid.NewGuid();

            var instancePath = Path.Combine(GetBasePath(), "instances", $"{Id:N}");

            if (_output == null) {
                var logDir = Path.Combine(instancePath, "logs");
                var logFile = Path.Combine(logDir, $"{DateTime.Now:hh-mm-ss_yy-MM-dd}.log");

                Directory.CreateDirectory(logDir);

                _output = new TextFileOutputHelper(logFile);
            }

            AssertSingleInstance();

            _connection = new MongoDBConnection(
                IPAddress.Loopback,
                GetAvailablePort()
            );

            var dbPath = Path.Combine(instancePath, "data");
            Directory.CreateDirectory(dbPath);
            
            // Use Replace(): Uri cannot be created from Unix path.
            var dbPathUri = dbPath.Replace("\\","/");

            var dbLogLevel = logVerbosity == LogVerbosity.Quiet 
                ? "--quiet" : logVerbosity == LogVerbosity.Verbose 
                ? "--verbose" : null;

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
                throw new MongodStartTimeoutException($"mongod failed to start after {timeout} seconds.");

            _instanceCount++;
        }

        private void AssertSingleInstance()
        {
            const string moduleGuid = "9A66C037-5217-4125-82F8-DBB7C6E415AB";

            _mutex = new Mutex(false, $"Global\\{moduleGuid}");
            if (!_mutex.WaitOne(0)) {
                _mutex.Dispose();

                var allow = Environment.GetEnvironmentVariable("AMONGST_ALLOW_MULTIPLE_RUNNERS");
                if (allow == null)
                    throw new MultipleTestRunnerInstancesException("Multiple test runner instances detected.");
            }

            if (_instanceCount > 1)
                _output.WriteLine(
                    $"[{DateTime.Now}][Warning]: You already spawned {_instanceCount} instances of mongod. " +
                    "It is recommended to share a MongoDB instance across your tests using a fixture. " +
                    "You can read more about shared context within xUnit here: https://xunit.github.io/docs/shared-context.html " +
                    "If it is intentional, you can igore this Warning.");
        }

        private void FetchReadyState(object sender, DataReceivedEventArgs e)
        {
            const string pattern = "waiting for connections on port";

            var process = sender as Process;
            if (string.IsNullOrEmpty(e.Data) || !e.Data.Contains(pattern)) return;

            State = MongoDBInstanceState.Running;
            process.OutputDataReceived -= FetchReadyState;

            _manualReset.Set();
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            if (string.IsNullOrEmpty(e.Data)) return;

            var pidAndName = process.HasExited ? null : $"[{process.ProcessName}:{process.Id}]";

            _output.WriteLine($"[{DateTime.Now}][Info][{Id:N}]{pidAndName}: {e.Data}");
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            if (string.IsNullOrEmpty(e.Data)) return;

            var pidAndName = process.HasExited ? null : $"[{process.ProcessName}:{process.Id}]";

            _output.WriteLine($"[{DateTime.Now}][Error][{Id:N}]{pidAndName}: {e.Data}");
        }

        private void OnExited(object sender, EventArgs e)
        {
            State = MongoDBInstanceState.Stopped;
            PortsInUse.Remove(_connection.Port);

            var process = sender as Process;
            var prefix = $"[{DateTime.Now}][Info][{Id:N}]";

            _output.WriteLine($"{prefix}: Instance stopped with exit code {process.ExitCode}");

            process.Exited -= OnExited;
        }

        private static string GetBasePath()
        {
            return Environment.GetEnvironmentVariable("AMONGST_PATH") ?? new Func<string>(() =>
            {
                var codeBase = typeof(MongoDBInstance).GetTypeInfo().Assembly.CodeBase;
                var assemblyDir = Path.GetDirectoryName(new Uri(codeBase).LocalPath);

                return assemblyDir;
            })();
        }

#pragma warning disable CS0162
        private string GetBinaryPath()
        {
#if NETSTANDARD1_6
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            var isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            var osDescription = RuntimeInformation.OSDescription;
#elif NET46
            const bool isWindows = true;
            const bool isLinux = false;
            const bool isOSX = false;

            var osDescription = Environment.OSVersion;
#endif
            string version;
            if (isWindows)
                version = BIN_WIN32;
            else if (isLinux)
                version = BIN_LINUX;
            else if (isOSX)
                version = BIN_OSX;
            else
                throw new PlatformNotSupportedException($"Platform {osDescription} is not supported.");


            _output.WriteLine($"[{DateTime.Now}][Info]: Detected {osDescription}. Using {version} binaries.");

            return Path.Combine(GetBasePath(), "tools", version, "bin");
        }
#pragma warning restore

#if NETSTANDARD1_6
        private static void SetExecutableBit(string file)
        {
            // Workaround until dotnet core 2.0
            var p = Process.Start("chmod", $"+x {file}");
            p.WaitForExit();

            if(p.ExitCode != 0)
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

            var port = (short) Enumerable.Range(begin, short.MaxValue)
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
                    "Counld not spawn a new mongod instance. No port available.");

            PortsInUse.Add(port);

            return port;
        }

        public void Import()
        {
            var fullPath = Path.Combine(_binaryPath, "mongoimport");

#if NETSTANDARD1_6
            if(IsUnix())
                SetExecutableBit(fullPath);
#endif

            throw new NotImplementedException();
        }

        public void Export()
        {
            var fullPath = Path.Combine(_binaryPath, "mongoexport");

#if NETSTANDARD1_6
            if (IsUnix())
                SetExecutableBit(fullPath);
#endif

            throw new NotImplementedException();
        }

        public void Stop()
        {
            if (_process.HasExited) return;

            State = MongoDBInstanceState.Stopping;

            _process?.Kill();

            const int timeout = 5000;
            var exited = _process.WaitForExit(5000);

            if (!exited)
                throw new MongodStopTimeoutException($"Failed to stop mongod after {timeout} milliseconds.");

            State = MongoDBInstanceState.Stopped;      
        }

        public void Dispose()
        {
            Stop();

            _process?.CancelErrorRead();
            _process?.CancelOutputRead();

            _output?.Dispose();
            _mutex?.Dispose();

            _instanceCount--;
        }
    }
}