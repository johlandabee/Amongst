using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using Amongst.Exception;
using Amongst.Helper;
using Amongst.Output;

namespace Amongst
{
    public partial class MongoDBInstance : IDisposable
    {
        private static readonly object Sync = new object();

        private static int _instanceCount;
        private static readonly Store Store = new Store();

        private readonly string _instancesPath;
        private readonly string _binaryPath;

        private readonly MongoDBInstanceOptions _options;
        private readonly MongoDBConnection _connection;
        private readonly Process _process;
        private readonly ManualResetEventSlim _manualReset;

        private Mutex _mutex;

        public Guid Id { get; private set; }
        public string ConnectionString => _connection.ToString();
        public MongoDBInstanceState State { get; private set; }

        //------------------------------------------------------------------------------------------------------------->

        /// <summary>
        /// Spawns a new instance of <see cref="MongoDBInstance"/> with default options.
        /// </summary>
        /// <returns><see cref="MongoDBInstance"/></returns>
        public static MongoDBInstance Spawn()
        {
            return Spawn(new MongoDBInstanceOptions
            {
                Verbosity = LogVerbosity.Normal,
                OutputHelper = null,
                CleanBeforeRun = false,
                Persist = false,
                PackageDirectory = null
            });
        }

        /// <summary>
        /// Spawns a new instance of <see cref="MongoDBInstance"/> with user defined options.
        /// </summary>
        /// <param name="options"><see cref="MongoDBInstanceOptions"/></param>
        /// <returns><see cref="MongoDBInstance"/></returns>
        public static MongoDBInstance Spawn(MongoDBInstanceOptions options)
        {
            return new MongoDBInstance(options, new PortManager());
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="options"><see cref="MongoDBInstanceOptions"/></param>
        /// <param name="portManager"><see cref="PortManager"/></param>
        public MongoDBInstance(MongoDBInstanceOptions options, IPortManager portManager)
        {
            _options = options;

            AssertSingleRunner();

            _instancesPath = Path.Combine(Directory.GetCurrentDirectory(), "instances");
            Prepare();

            _process = new Process();
            _manualReset = new ManualResetEventSlim();
            _binaryPath = GetBinaryPath();
            _connection = new MongoDBConnection(
                IPAddress.Loopback,
                (short) portManager.GetAvailablePort()
            );

            var instancePath = Path.Combine(_instancesPath, $"{Id:N}");
            var dbPath = Path.Combine(instancePath, "data");

            Directory.CreateDirectory(dbPath);

            // mongod wants a UNIX path.
            var dbPathUri = dbPath.Replace("\\", "/");
            var dbLogLevel = _options.Verbosity == LogVerbosity.Quiet
                ? "--quiet"
                : _options.Verbosity == LogVerbosity.Verbose
                    ? "--verbose"
                    : null;

            ApplyDefaultOutputHelper(instancePath);
            ApplyEventHandler();
            Start(new[]
            {
                $"--bind_ip {_connection.IP}",
                $"--port {_connection.Port}",
                $"--dbpath {dbPathUri}",
                "--noprealloc",
                "--smallfiles",
                "--nojournal",
                $"{dbLogLevel}"
            });

            lock (Sync)
            {
                _instanceCount++;
            }
        }

        /// <summary>
        /// Make sure that we not run multiple runners simultaneously to prevent heavy system load by accident.
        /// This behavior can be disabled within the <see cref="MongoDBInstanceOptions"/>.
        /// </summary>
        private void AssertSingleRunner()
        {
            const string moduleGuid = "9A66C037-5217-4125-82F8-DBB7C6E415AB";

            _mutex = new Mutex(false, $"Global\\{moduleGuid}");
            if (!_mutex.WaitOne(0))
            {
                if (!_options.AllowMultipleRunners)
                {
                    throw new MultipleTestRunnerInstancesException("Multiple test runner instances detected.");
                }
            }

            if (_instanceCount > 1)
            {
                _options.OutputHelper.WriteLine(
                    $"[{DateTime.Now}][Warning]: You already spawned {_instanceCount} instances of mongod. " +
                    "It is recommended to share a MongoDB instance across your tests using a fixture. " +
                    "You can read more about shared context within xUnit here: https://xunit.github.io/docs/shared-context.html. " +
                    "If this is intentional, you can igore this Warning.");
            }
        }

        /// <summary>
        /// Sets a new id or uses the last one known if persistence is enabled.
        /// Also creates the instances directory and purges it if CleanBeforerRun is set to true. 
        /// </summary>
        /// <returns>Path to the current instance data direcory.</returns>
        private void Prepare()
        {
            if (_options.CleanBeforeRun && !_options.Persist)
            {
                Directory.Delete(_instancesPath, true);
            }

            Directory.CreateDirectory(_instancesPath);

            lock (Sync)
            {
                Store.Load(_instancesPath);
            }

            if (_options.Persist)
            {
                lock (Sync)
                {
                    if (Store.Persistence.Id == Guid.Empty)
                    {
                        Store.Persistence.Id = Guid.NewGuid();
                    }

                    Store.Persistence.LastRun = DateTime.Now;
                    Store.Save(_instancesPath);
                }

                Id = Store.Persistence.Id;

                _options.OutputHelper.WriteLine($"[{DateTime.Now}][Info]: Persistence enabled. Instance id = {Id}");
            }
            else
            {
                Id = Guid.NewGuid();
            }
        }

        /// <summary>
        /// Applies the default output helper <see cref="TextFileOutputHelper"/> if none was set.
        /// </summary>
        /// <param name="instancePath">The data path of the current instance.</param>
        private void ApplyDefaultOutputHelper(string instancePath)
        {
            if (_options.OutputHelper != null)
            {
                return;
            }

            var logDir = Path.Combine(instancePath, "logs");
            var logFile = Path.Combine(logDir, $"{DateTime.Now:hh-mm-ss_yy-MM-dd}.log");

            Directory.CreateDirectory(logDir);

            _options.OutputHelper = new TextFileOutputHelper(logFile);
        }

        /// <summary>
        /// Applys event handlers to our process.
        /// </summary>
        private void ApplyEventHandler()
        {
            _process.OutputDataReceived += OnFetchReadyState;
            _process.OutputDataReceived += OnOutputDataReceived;
            _process.ErrorDataReceived += OnErrorDataReceived;
            _process.Exited += OnExited;
        }

        /// <summary>
        /// Starts a new mongod process with passed arguments.
        /// </summary>
        /// <param name="args">mongod start arguments.</param>
        private void Start(string[] args)
        {
            var fullPath = Path.Combine(_binaryPath, "mongod");

#if NETSTANDARD1_6
            if (IsUnix())
            {
                SetExecutableBit(fullPath);
            }
#endif

            _process.StartInfo = new ProcessStartInfo
            {
                FileName = fullPath,
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _process.Start();

            State = MongoDBInstanceState.Starting;

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            const int timeout = 30;

            _manualReset.Wait(TimeSpan.FromSeconds(timeout));

            if (!_manualReset.IsSet)
            {
                throw new TimeoutException($"mongod failed to start after {timeout} seconds.");
            }
        }

        /// <summary>
        /// Kills the mongod process and sets the appropriate state.
        /// </summary>
        public void Stop()
        {
            if (_process.HasExited)
            {
                State = MongoDBInstanceState.Stopped;

                return;
            }

            State = MongoDBInstanceState.Stopping;

            _process?.Kill();

            const int timeout = 5000;
            var exited = _process.WaitForExit(timeout);

            if (!exited)
            {
                throw new TimeoutException($"Failed to stop mongod after {timeout} milliseconds.");
            }

            State = MongoDBInstanceState.Stopped;

            _process?.CancelErrorRead();
            _process?.CancelOutputRead();
        }

        //------------------------------------------------------------------------------------------------------------->

        void IDisposable.Dispose()
        {
            Stop();

            Dispose(true);
            GC.SuppressFinalize(this);

            lock (Sync)
            {
                _instanceCount--;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _options.OutputHelper.Dispose();
                _mutex.Dispose();
            }
        }
    }
}