// ReSharper disable PossibleNullReferenceException

using System;
using System.Diagnostics;
using Amongst.Helper;

namespace Amongst
{
    public partial class MongoDBInstance
    {
        /// <summary>
        /// Waits for mongod to start up.
        /// </summary>
        /// <param name="sender">mongod process</param>
        /// <param name="e"></param>
        private void OnFetchReadyState(object sender, DataReceivedEventArgs e)
        {
            const string pattern = "waiting for connections on port";

            var process = sender as Process;
            if (string.IsNullOrEmpty(e.Data) || !e.Data.Contains(pattern))
            {
                return;
            }

            State = MongoDBInstanceState.Running;

            process.OutputDataReceived -= OnFetchReadyState;

            _manualReset.Set();
        }

        /// <summary>
        /// Handles monogd's stdout.
        /// Also used for mongoimport/mongoexport.
        /// </summary>
        /// <param name="sender">mongod process</param>
        /// <param name="e"></param>
        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            var pidAndName = process.HasExited ? null : $"[{process.ProcessName}:{process.Id}]";

            _options.OutputHelper.WriteLine($"[{DateTime.Now}][Info][{Id:N}]{pidAndName}: {e.Data}");
        }

        /// <summary>
        /// Handles mongod's stderr.
        /// Also used for mongoimport/mongoexport.
        /// </summary>
        /// <param name="sender">mongod process</param>
        /// <param name="e"></param>
        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            var pidAndName = process.HasExited ? null : $"[{process.ProcessName}:{process.Id}]";

            _options.OutputHelper.WriteLine($"[{DateTime.Now}][Error][{Id:N}]{pidAndName}: {e.Data}");
        }

        /// <summary>
        /// Handles mongod's exit event.
        /// Frees the used port.
        /// </summary>
        /// <param name="sender">mongod process</param>
        /// <param name="e"></param>
        private void OnExited(object sender, EventArgs e)
        {
            State = MongoDBInstanceState.Stopped;

            var process = sender as Process;
            var prefix = $"[{DateTime.Now}][Info][{Id:N}]";

            _options.OutputHelper.WriteLine($"{prefix}: Instance stopped with exit code {process.ExitCode}");

            process.Exited -= OnExited;
        }
    }
}