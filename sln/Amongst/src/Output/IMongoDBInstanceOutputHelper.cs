using System;

namespace Amongst.Output
{
    public interface IMongoDBInstanceOutputHelper : IDisposable
    {
        void WriteLine(string message);

        void WriteLine(string format, params object[] args);
    }
}