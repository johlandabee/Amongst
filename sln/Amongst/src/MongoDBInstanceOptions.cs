using System.IO;
using Amongst.Output;

namespace Amongst
{
    public class MongoDBInstanceOptions
    {
        private string _packageDirectory;

        /// <summary>
        /// Sets MongoDB's log verbosity.
        /// </summary>
        public LogVerbosity LogVerbosity;

        /// <summary>
        /// A helper object implementing <see cref="IMongoDBInstanceOutputHelper"/> to hand over MongoDB's output.
        /// Default helper is <see cref="TextFileOutputHelper"/>.
        /// </summary>
        public IMongoDBInstanceOutputHelper OutputHelper;

        /// <summary>
        /// If set, the instances directory will be deleted before starting up.
        /// This should not be used in combination with <see cref="Persist"/>.
        /// False by default.
        /// </summary>
        public bool CleanBeforeRun;

        /// <summary>
        /// If set, the last instance will be reused. Data will be persistant between runs.
        /// This should not be used in combination with <see cref="CleanBeforeRun"/>
        /// False by default.
        /// </summary>
        public bool Persist;

        /// <summary>
        /// This option will allow multiple test runner instances loading the Amongst.dll module.
        /// False by default.
        /// </summary>
        public bool AllowMultipleRunners;

        /// <summary>
        /// This option lets you configure a custom path to your NuGet package directory.
        /// Empty by default.
        /// </summary>
        public string PackageDirectory
        {
            get => _packageDirectory;
            set {
                if (!Directory.Exists(value))
                    throw new DirectoryNotFoundException("PackageDirectory must be an existing path.");

                _packageDirectory = value;
            }
        }
    }
}