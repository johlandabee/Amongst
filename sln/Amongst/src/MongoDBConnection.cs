using System.Net;

namespace Amongst
{
    public class MongoDBConnection
    {
        public IPAddress IP { get; }
        public int Port { get; }

        //------------------------------------------------------------------------------------------------------------->

        public MongoDBConnection(IPAddress ip, int port)
        {
            IP = ip;
            Port = port;
        }

        public override string ToString()
        {
            return $"mongodb://{IP}:{Port}/";
        }
    }
}