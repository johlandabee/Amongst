using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
#if NETSTANDARD1_6

#endif

namespace Amongst
{

    public class MongoDBInstance : IDisposable
    {
        private const string BIN_WIN32 = "mongodb-win32-x86_64-2008plus-3.4.4";
        private const string BIN_LINUX = "mongodb-linux-x86_64-3.4.4";
        private const string BIN_OSX = "mongodb-osx-x86_64-3.4.4";

        private readonly IMongoDBInstanceOutputHelper _output;
        private static Int32 _instanceCount;

        private readonly Process _process;
        private readonly ManualResetEventSlim _manualReset;

        private Mutex _mutex;

        public Guid Id;
        public string Connectionstring { get; }
        public MongoDBInstaceState State { get; private set; }

        public static MongoDBInstance Spawn(IMongoDBInstanceOutputHelper output = null)
        {
            return new MongoDBInstance(output);
        }

        private void AssertSingleInstance()
        {
            const string moduleGuid = "9A66C037-5217-4125-82F8-DBB7C6E415AB";

            _mutex = new Mutex(false, $"Global\\{moduleGuid}");
            if (!_mutex.WaitOne(0)) {
                _mutex.Dispose();

                throw new MultipleRunnerInstancesException("Multiple Runner instances detected.");
            }

            if (_instanceCount > 1)
                _output.WriteLine(
                    $"[Warning]: You already spawned {_instanceCount} instances of {nameof(MongoDBInstance)}. " +
                    "It is recommended to share a MongoDB instance across your tests using a fixture. " +
                    "You can read more about shared context wihin xUnit here: https://xunit.github.io/docs/shared-context.html " +
                    "If this is intentional, you can igore this Warning.");
        }

        private MongoDBInstance(IMongoDBInstanceOutputHelper output)
        {
            Id = Guid.NewGuid();

            var instancePath = Path.Combine(GetBasePath(), "instances", $"{Id:N}");
            var dbPath = Path.Combine(instancePath, "data");
            var dbPathUri = new Uri(dbPath).AbsolutePath;

            Directory.CreateDirectory(dbPath);

            _output = output;
            if (_output == null) {
                var logDir = Path.Combine(instancePath, "logs");
                var logFile = Path.Combine(logDir, $"{DateTime.Now:hh-mm-ss_yy-MM-dd}.log");

                Directory.CreateDirectory(logDir);

                _output = new TextFileOutputHelper(logFile);
            }

            AssertSingleInstance();

            const string fileName = "mongod";
            const string bind = "127.0.0.1";

            var port = GetAvailablePort();
            var args = new[]
            {
                $"--bind_ip {bind}",
                $"--port {port}",
                $"--dbpath {dbPathUri}",
                "--noprealloc",
                "--smallfiles",
                "--nojournal"
            };

            var connectionstring = $"mongodb://{bind}:{port}/";

            var binaryPath = GetBinaryPath();
            var fullPath = Path.Combine(binaryPath, fileName);

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

            State = MongoDBInstaceState.Starting;

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            _manualReset = new ManualResetEventSlim();
            _manualReset.Wait(TimeSpan.FromSeconds(10));

            if (!_manualReset.IsSet)
                throw new MongodStartupTimeoutException("mongod failed to start after 30 seconds.");

            Connectionstring = connectionstring;

            _instanceCount++;
        }

        private void FetchReadyState(Object sender, DataReceivedEventArgs e)
        {
            const string pattern = "waiting for connections on port";

            var p = sender as Process;
            if (p == null || string.IsNullOrEmpty(e.Data) || !e.Data.Contains(pattern)) return;

            _manualReset.Set();

            State = MongoDBInstaceState.Running;
            p.OutputDataReceived -= FetchReadyState;
        }

        private void OnOutputDataReceived(Object sender, DataReceivedEventArgs e)
        {
            var p = sender as Process;
            if (p == null || string.IsNullOrEmpty(e.Data)) return;

            var name = p.HasExited ? "null" : p.ProcessName;
            var id = p.HasExited ? -1 : p.Id;

            _output.WriteLine($"[{DateTime.Now}][Info][{name}:{id}][{Id:N}]: {e.Data}");
        }

        private void OnErrorDataReceived(Object sender, DataReceivedEventArgs e)
        {
            var p = sender as Process;
            if (p == null || string.IsNullOrEmpty(e.Data)) return;

            var name = p.HasExited ? "null" : p.ProcessName;
            var id = p.HasExited ? -1 : p.Id;

            _output.WriteLine($"[{DateTime.Now}][Error][{name}:{id}][{Id:N}]: {e.Data}");
        }

        private void OnExited(Object sender, EventArgs e)
        {
            State = MongoDBInstaceState.Stopped;

            var p = sender as Process;
            if (p == null) return;

            p.Exited -= OnExited;

            var name = p.HasExited ? "null" : p.ProcessName;
            var id = p.HasExited ? -1 : p.Id;

            _output.WriteLine($"[{DateTime.Now}][Info][{name}:{id}][{Id:N}]: Has exited with exit code {p.ExitCode}");
        }

        private static string GetBasePath()
        {
            return Environment.GetEnvironmentVariable("MONGEST_PATH") ?? new Func<string>(() =>
            {
                var codeBase = typeof(MongoDBInstance).GetTypeInfo().Assembly.CodeBase;
                var assemblyDir = Path.GetDirectoryName(new Uri(codeBase).LocalPath);

                return assemblyDir;
            })();
        }

        private static string GetBinaryPath()
        {
#if NETSTANDARD1_6
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            var osDescription = RuntimeInformation.OSDescription;
#elif NET46
            const Boolean isWindows = true;
            const Boolean isLinux = false;
            const Boolean isMac = false;

            const string osDescription = "Win32";
#endif
            string version;
            if (isWindows)
                version = BIN_WIN32;
            else if (isLinux)
                version = BIN_LINUX;
            else if (isMac)
                version = BIN_OSX;
            else
                throw new PlatformNotSupportedException($"Platform {osDescription} is not supported.");

            return Path.Combine(GetBasePath(), "tools", version, "bin");
        }

        private static Int16 GetAvailablePort()
        {
            const Int16 begin = 27018;

            var port = (Int16) Enumerable.Range(begin, Int16.MaxValue)
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
                    "Counld not create a new mongodb instance. No port available.");

            return port;
        }

        public void Import()
        {
            const string fileName = "mongoimport";

            throw new NotImplementedException();
        }

        public void Export()
        {
            const string fileName = "mongoexport";

            throw new NotImplementedException();
        }


        public void Stop()
        {
            if (_process.HasExited) return;

            _process?.Kill();
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