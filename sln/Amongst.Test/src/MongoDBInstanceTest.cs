using System;
using System.IO;
using System.Collections.Generic;
using Amongst.Test.Helper;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using MongoDB.Driver;
using System.Net;
using System.Net.Sockets;
using Amongst.Exception;

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

        [Fact]
        public void Spawn_Should_Throw_NoPortAvailableException()
        {
            const short begin = 27018;
            const short end = begin + 100;

            var listeners = new List<TcpListener>();
            for (var i = begin; i < end; i++) {
                var l = new TcpListener(IPAddress.Loopback, i);

                try {
                    l.Start();
                    listeners.Add(l);
                }
                catch (SocketException) {
                    _output.WriteLine($"[Info][XUnit]: Port {i} is unavailable.");
                }
            }

            Action spawn = () => MongoDBInstance.Spawn(new MongoDBInstanceOptions
            {
                Verbosity = LogVerbosity.Verbose,
                OutputHelper = _output,
                AllowMultipleRunners = true
            });

            spawn.ShouldThrow<NoPortAvailableException>();

            listeners.ForEach(l => l.Stop());
        }

        [Fact]
        public void ConnectionString_Should_Be_Valid()
        {
            var instance = MongoDBInstance.Spawn(new MongoDBInstanceOptions
            {
                Verbosity = LogVerbosity.Verbose,
                OutputHelper = _output,
                AllowMultipleRunners = true
            });

            instance.ConnectionString.Should().MatchRegex(@"^mongodb:\/\/127\.0\.0\.1:[0-9]{5}\/$");

            new MongoClient(instance.ConnectionString).Should().NotBeNull();

            instance.Stop();
        }

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