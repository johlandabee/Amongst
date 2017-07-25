using System.Net;
using System.Net.Sockets;
using Amongst.Exception;

namespace Amongst.Helper
{
    /// <summary>
    /// Receives and keeps track of the next available port.
    /// This class is not threadsafe.
    /// </summary>
    public class PortManager : IPortManager
    {
        private readonly ITcpListenerAdapter _tcpListener;

        //------------------------------------------------------------------------------------------------------------->

        public PortManager()
        {
            _tcpListener = new TcpListenerAdapter(IPAddress.Loopback, 0);
        }

        public PortManager(ITcpListenerAdapter tcpListener)
        {
            _tcpListener = tcpListener;
        }

        /// <summary>
        /// Receives the next available port.
        /// </summary>
        /// <exception cref="NoPortAvailableException">Throws if there is no port available.</exception>
        /// <returns>Available port.</returns>
        public int GetAvailablePort()
        {
            int port;

            try
            {
                _tcpListener.Start();

                port = _tcpListener.LocalEndpoint().Port;
            }
            catch (SocketException)
            {
                throw new NoPortAvailableException(
                    "Counld not spawn a new mongod instance. No port available.");
            }
            finally
            {
                _tcpListener.Stop();
            }

            return port;
        }
    }
}