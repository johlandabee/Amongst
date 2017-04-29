using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Amongst.Test
{
    public class UnitTest1
    {
        private XunitTestOutputHelper _outout;

        public UnitTest1(ITestOutputHelper output)
        {
            _outout = new XunitTestOutputHelper(output);
        }

        [Fact]
        public void Test1()
        {
            Environment.SetEnvironmentVariable("MONGEST_PATH", Path.GetFullPath(@"..\..\..\..\..\"));

            //_outout = null;
            var instances = Enumerable.Range(0, 10).Select(i => MongoDBInstance.Spawn(_outout)).ToList();
            foreach (var i in instances) {
                i.Stop();
            }

            _outout = null;
            instances = Enumerable.Range(0, 10).Select(i => MongoDBInstance.Spawn(_outout)).ToList();
            foreach (var i in instances) {
                i.Stop();
            }
        }
    }
}