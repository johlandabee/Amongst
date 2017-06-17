using System;

namespace Amongst.Output
{
    public class ConsoleOutputHelper : IMongoDBInstanceOutputHelper
    {
        public void Dispose() { }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}