using System.Net;
using System.Net.Sockets;

namespace Amongst.Helper
{
    public class TcpListenerAdapter : ITcpListenerAdapter
    {
        private readonly TcpListener _wrappedListener;

        public TcpListenerAdapter(IPAddress address, ushort port)
        {
            _wrappedListener = new TcpListener(address, port);
        }

        public void Start()
        {
            _wrappedListener.Start();
        }

        public void Stop()
        {
            _wrappedListener.Stop();
        }

        public IPEndPoint LocalEndpoint()
        {
            return (IPEndPoint) _wrappedListener.LocalEndpoint;
        }
    }
}
