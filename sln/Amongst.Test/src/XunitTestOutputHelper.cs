﻿using Xunit.Abstractions;

namespace Amongst.Test
{
    internal class XunitTestOutputHelper : IMongoDBInstanceOutputHelper
    {
        private readonly ITestOutputHelper _output;

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

        public void Dispose() { }
    }
}