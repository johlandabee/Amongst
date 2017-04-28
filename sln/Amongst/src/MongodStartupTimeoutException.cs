using System;

#if NETSTANDARD1_6

#endif

namespace Amongst
{
    public class MongodStartupTimeoutException : Exception
    {
        public MongodStartupTimeoutException(string message) : base(message) { }
    }
}