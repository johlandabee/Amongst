using System;
using System.IO;

namespace Amongst
{
    public class TextFileOutputHelper : IMongoDBInstanceOutputHelper
    {
        private readonly object Sync = new Object();
        private readonly StreamWriter _writer;

        public TextFileOutputHelper(string path)
        {
            lock (Sync) {
                _writer = new StreamWriter(File.Create(path));
            }
        }

        public void WriteLine(string message)
        {
            lock (Sync) {
                _writer.WriteLine(message);
            }
        }

        public void WriteLine(string format, params object[] args)
        {
            lock (Sync) {
                _writer.WriteLine(format, args);
            }
        }

        public void Dispose() { _writer.Dispose(); }
    }
}