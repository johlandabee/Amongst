using System;
using System.IO;

namespace Amongst.Output
{
    public class TextFileOutputHelper : IMongoDBInstanceOutputHelper
    {
        private readonly object _sync = new object();
        private readonly StreamWriter _writer;

        public TextFileOutputHelper(string path)
        {
            lock (_sync)
                _writer = new StreamWriter(File.Create(path));
        }

        public void WriteLine(string message)
        {
            lock (_sync)
                _writer.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            lock (_sync)
                _writer.WriteLine(format, args);
        }

        //------------------------------------------------------------------------------------------------------------->

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            _writer.Dispose();
        }
    }
}