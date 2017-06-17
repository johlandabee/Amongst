// ReSharper disable PossibleNullReferenceException

using System;
using System.Diagnostics;
using Amongst.Helper;


namespace Amongst
{
    public partial class MongoDBInstance
    {
        private void OnFetchReadyState(object sender, DataReceivedEventArgs e)
        {
            const string pattern = "waiting for connections on port";

            var process = sender as Process;
            if (string.IsNullOrEmpty(e.Data) || !e.Data.Contains(pattern)) return;

            State = MongoDBInstanceState.Running;

            process.OutputDataReceived -= OnFetchReadyState;

            _manualReset.Set();
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            if (string.IsNullOrEmpty(e.Data)) return;

            var pidAndName = process.HasExited ? null : $"[{process.ProcessName}:{process.Id}]";

            _options.OutputHelper.WriteLine($"[{DateTime.Now}][Info][{Id:N}]{pidAndName}: {e.Data}");
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            if (string.IsNullOrEmpty(e.Data)) return;

            var pidAndName = process.HasExited ? null : $"[{process.ProcessName}:{process.Id}]";

            _options.OutputHelper.WriteLine($"[{DateTime.Now}][Error][{Id:N}]{pidAndName}: {e.Data}");
        }

        private void OnExited(object sender, EventArgs e)
        {
            State = MongoDBInstanceState.Stopped;

            PortManager.Free(_connection.Port);

            var process = sender as Process;
            var prefix = $"[{DateTime.Now}][Info][{Id:N}]";

            _options.OutputHelper.WriteLine($"{prefix}: Instance stopped with exit code {process.ExitCode}");

            process.Exited -= OnExited;
        }
    }
}