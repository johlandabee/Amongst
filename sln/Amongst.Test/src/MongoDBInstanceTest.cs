using System;
using System.Collections.Generic;
using Amongst.Test.Helper;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using MongoDB.Driver;
using System.Net;
using System.Net.Sockets;
using Amongst.Exception;
using MongoDB.Bson;

namespace Amongst.Test
{
    public class MongoDBInstanceTest
    {
        private readonly XunitTestOutputHelper _output;

        public MongoDBInstanceTest(ITestOutputHelper output)
        {
            _output = new XunitTestOutputHelper(output);
        }

        [Fact]
        public void Spawn_Should_Start_A_New_Instance()
        {
            var instance = MongoDBInstance.Spawn(new MongoDBInstanceOptions
            {
                Verbosity = LogVerbosity.Verbose,
                OutputHelper = _output,
                AllowMultipleRunners = true
            });

            instance.Should().NotBeNull().And.BeAssignableTo<MongoDBInstance>();
            instance.State.ShouldBeEquivalentTo(MongoDBInstanceState.Running);
            instance.Id.Should().NotBeEmpty();

            instance.Stop();
        }
        
        // TODO: ConncetionString test

        [Fact]
        public void Insatnce_Should_Be_Persistent()
        {
            var instance = MongoDBInstance.Spawn(new MongoDBInstanceOptions
            {
                Verbosity = LogVerbosity.Verbose,
                OutputHelper = _output,
                AllowMultipleRunners = true,
                Persist = true
            });

            instance.Stop();

            // TODO

            instance = MongoDBInstance.Spawn(new MongoDBInstanceOptions
            {
                Verbosity = LogVerbosity.Verbose,
                OutputHelper = _output,
                AllowMultipleRunners = true,
                Persist = true
            });
            instance.Stop();
        }

        [Fact]
        public void Stop_Should_Stop_The_Current_Instance()
        {
            var instance = MongoDBInstance.Spawn(new MongoDBInstanceOptions
            {
                Verbosity = LogVerbosity.Verbose,
                OutputHelper = _output,
                AllowMultipleRunners = true
            });

            instance.State.ShouldBeEquivalentTo(MongoDBInstanceState.Running);

            instance.Stop();

            instance.State.ShouldBeEquivalentTo(MongoDBInstanceState.Stopped);
        }
    }
}