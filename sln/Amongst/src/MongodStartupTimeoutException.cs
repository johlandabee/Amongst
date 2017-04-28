using System;

namespace Amongst
{
    public class MongodStartupTimeoutException : Exception
    {
        public MongodStartupTimeoutException(string message) : base(message) { }
    }
}