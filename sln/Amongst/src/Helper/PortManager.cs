using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Amongst.Exception;

namespace Amongst.Helper
{
    public class PortManager
    {
        private static readonly object Sync = new object();
        private static readonly List<int> PortsInUse = new List<int>();

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        public static short GetAvailablePort()
        {
            const short begin = 27018;
            const short count = 100;

            var port = (short) Enumerable.Range(begin, count)
                .Except(PortsInUse)
                .FirstOrDefault(p =>
                {
                    var listener = new TcpListener(IPAddress.Loopback, p);
                    try {
                        listener.Start();
                    }
                    catch (SocketException) {
                        return false;
                    }
                    finally {
                        listener.Stop();
                    }

                    return true;
                });

            if (port < begin)
                throw new NoPortAvailableException(
                    $"Counld not spawn a new mongod instance. No port available within the range {begin}-{begin + count}");

            lock (Sync)
                PortsInUse.Add(port);

            return port;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="port"></param>
        public static void Free(short port)
        {
            lock (Sync)
                PortsInUse.Remove(port);
        }
    }
}
