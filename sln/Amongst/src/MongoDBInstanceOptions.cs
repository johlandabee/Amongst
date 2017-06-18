using System.IO;
using Amongst.Output;

namespace Amongst
{
    public class MongoDBInstanceOptions
    {
        /// <summary>
        /// Sets MongoDB's log verbosity.
        /// </summary>
        public LogVerbosity Verbosity { get; set; }

        /// <summary>
        /// A helper object implementing <see cref="IMongoDBInstanceOutputHelper"/> to hand over MongoDB's output.
        /// Default helper is <see cref="TextFileOutputHelper"/>.
        /// </summary>
        public IMongoDBInstanceOutputHelper OutputHelper { get; set; }

        /// <summary>
        /// If set, the instances directory will be deleted before starting up.
        /// Will be ignored if <see cref="Persist"/> is set.
        /// False by default.
        /// </summary>
        public bool CleanBeforeRun { get; set; }

        /// <summary>
        /// If set, the last instance will be reused. Data will be persistant between runs.
        /// <see cref="CleanBeforeRun"/> will be ignored.
        /// False by default.
        /// </summary>
        public bool Persist { get; set; }

        /// <summary>
        /// This option will allow multiple test runner instances loading the Amongst.dll module.
        /// False by default.
        /// </summary>
        public bool AllowMultipleRunners { get; set; }

        /// <summary>
        /// This option lets you configure a custom path to your NuGet package directory.
        /// Empty by default.
        /// </summary>
        public string PackageDirectory { get; set; }
    }
}