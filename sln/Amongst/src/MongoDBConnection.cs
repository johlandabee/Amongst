using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Amongst
{
    internal class MongoDBConnection
    {
        public IPAddress IP { get; }
        public short Port { get; }

        public MongoDBConnection(IPAddress ip, short port)
        {
            IP = ip;
            Port = port;
        }

        public override string ToString()
        {
            return $"mongodb://{IP}:{Port}";
        }
    }
}