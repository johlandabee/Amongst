using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using MongoDB.Driver;

namespace Amongst.Test
{
    public class MongoDBInstanceTest : IDisposable
    {
        private readonly XunitTestOutputHelper _outout;

        public MongoDBInstanceTest(ITestOutputHelper output)
        {
            // Do not use OS dependent path seperator.
            // Local build, back to project root.
            var toolsPath = Path.GetFullPath("../../../../../");

            var ci = Environment.GetEnvironmentVariable("CI");
            if (ci != null)
                // Back to project root from artifacts/ folder.
                toolsPath = "../";
            
            Environment.SetEnvironmentVariable("AMONGST_PATH", toolsPath);
            Environment.SetEnvironmentVariable("AMONGST_ALLOW_MULTIPLE_RUNNERS", "");

            _outout = new XunitTestOutputHelper(output);
        }

        [Fact]
        public void Should_spawn_connect_insert_and_receive_data_finally_stop()
        {
            

            var instance = MongoDBInstance.Spawn(_outout, LogVerbosity.Verbose);

            instance.State.ShouldBeEquivalentTo(MongoDBInstanceState.Running, "because it was spawned.");
            instance.Id.Should().NotBeEmpty("because a new Guid gets assigned on Spawn().");
            instance.ConnectionString.Should().MatchRegex(@"^mongodb:\/\/127\.0\.0\.1:[0-9]{5}\/$",
                "because mongod is stated on 127.0.0.1 with a port assigned above 27017.");

            var client = new MongoClient(instance.ConnectionString);
            client.Should().NotBeNull("because we connect to our mongodb instance.");

            var db = client.GetDatabase("UnitTest");
            db.Should().NotBeNull("because we got our UnitTest database.");

            var collection = db.GetCollection<TestObjectA>("TestCollection");
            collection.Should().NotBeNull("because we received it form your mongodb instance.");

            var objA = new TestObjectA
            {
                TestPropertyA = 42,
                TestPropertyB = "FourtyTwo",
                TestPropertyC = new []{ 4, 2 },
                TestPropertyD = new[] { "Fourty", "Two" }
            };

            collection.InsertOne(objA);

            var resultA = collection.Find(f => f.TestPropertyB == "FourtyTwo").FirstOrDefault();
            resultA.Should().NotBeNull("because we inserted a data set before.");
            resultA.ShouldBeEquivalentTo(objA, "because that's the data be inserted.");
            
            instance.Stop();
            instance.State.ShouldBeEquivalentTo(MongoDBInstanceState.Stopped, "because we stopped it.");

            instance.Dispose();
        }

        public void Dispose()
        {
            _outout?.Dispose();
        }
    }
}