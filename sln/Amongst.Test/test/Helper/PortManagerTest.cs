using System;
using System.Net;
using System.Net.Sockets;
using Amongst.Exception;
using Amongst.Helper;
using Xunit;
using Moq;
using FluentAssertions;

namespace Amongst.Test.Helper
{
    public class PortManagerTest
    {
        [Fact]
        public void GetAvailablePort_ShouldReturn_Port_3844()
        {
            const int port = 3844;

            var tcpListener = new Mock<ITcpListenerAdapter>();
            tcpListener.Setup(x => x.LocalEndpoint()).Returns(new IPEndPoint(IPAddress.Loopback, 3844));

            var portManager = new PortManager(tcpListener.Object);

            portManager.GetAvailablePort().ShouldBeEquivalentTo(port);
        }

        [Fact]
        public void GetAvailabePort_ShoudThrow_NoPortAvailableException()
        {
            var tcpListener = new Mock<ITcpListenerAdapter>();
            tcpListener.Setup(x => x.Start()).Throws<SocketException>();

            var portManager = new PortManager(tcpListener.Object);

            Action invoke = () => { portManager.GetAvailablePort(); };
            invoke.ShouldThrow<NoPortAvailableException>();
        }
    }
}
