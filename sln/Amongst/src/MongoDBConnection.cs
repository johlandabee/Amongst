using System.Net;

namespace Amongst
{
    public class MongoDBConnection
    {
        public IPAddress IP;
        public short Port;

        //------------------------------------------------------------------------------------------------------------->

        public MongoDBConnection(IPAddress ip, short port)
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