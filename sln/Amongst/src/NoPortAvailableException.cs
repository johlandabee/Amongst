using System;

namespace Amongst
{
    public class NoPortAvailableException : Exception
    {
        public NoPortAvailableException(string message) : base(message) { }
    }
}