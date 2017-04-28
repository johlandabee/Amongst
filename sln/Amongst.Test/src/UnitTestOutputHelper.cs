using System;
using Xunit.Abstractions;

namespace Amongst.Test
{
    internal class UnitTestOutputHelper : IMongoDBInstanceOutputHelper
    {
        private readonly ITestOutputHelper _output;

        public UnitTestOutputHelper(ITestOutputHelper output) { _output = output; }

        public void WriteLine(String message)
        {
            _output.WriteLine(message);   
        }

        public void WriteLine(String format, params Object[] args)
        {
            _output.WriteLine(format, args);  
        }

        public void Dispose() { }
    }
}