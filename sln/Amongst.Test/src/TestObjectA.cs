using MongoDB.Bson.Serialization.Attributes;

namespace Amongst.Test
{
    public class TestObjectA
    {
        [BsonId]
        public int TestPropertyA;
        public string TestPropertyB;
        public int[] TestPropertyC;
        public string[] TestPropertyD;
    }
}
