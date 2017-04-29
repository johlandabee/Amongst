using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Amongst.Test
{
    public class MongoDBInstanceTest
    {
        private readonly XunitTestOutputHelper _outout;

        public MongoDBInstanceTest(ITestOutputHelper output)
        {
            Environment.SetEnvironmentVariable("AMONGST_PATH", Path.GetFullPath(@"..\..\..\..\..\"));
            Environment.SetEnvironmentVariable("AMONGST_ALLOW_MULTIPLE_RUNNERS", "");

            _outout = new XunitTestOutputHelper(output);
        }

        [Fact]
        public void Should_spawn_and_stop_a_new_instance()
        {
            var instance = MongoDBInstance.Spawn(_outout);

            Assert.Equal(MongoDBInstanceState.Running, instance.State);
            Assert.NotEqual(Guid.Empty, instance.Id);

            instance.Stop();

            Assert.Equal(MongoDBInstanceState.Stopped, instance.State);

            instance.Dispose();
        }
    }
}