using System;
using Xunit.Abstractions;
using Amongst.Output;

namespace Amongst.Test.Helper
{
    internal class XunitTestOutputHelper : IMongoDBInstanceOutputHelper
    {
        private readonly ITestOutputHelper _output;

        //------------------------------------------------------------------------------------------------------------->

        public XunitTestOutputHelper(ITestOutputHelper output)
        {
            _output = output;
        }

        public void WriteLine(string message)
        {
            _output.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            _output.WriteLine(format, args);
        }

        //------------------------------------------------------------------------------------------------------------->

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Nothing to dispose here.
        }
    }
}