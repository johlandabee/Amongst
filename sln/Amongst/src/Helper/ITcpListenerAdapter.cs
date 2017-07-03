using System.Net;

namespace Amongst.Helper
{
    public interface ITcpListenerAdapter
    {
        void Start();

        void Stop();

        IPEndPoint LocalEndpoint();
    }
}
