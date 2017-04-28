using System;

#if NETSTANDARD1_6

#endif

namespace Amongst
{
#if NET46
    internal enum OSPlatform
    {
        Windows,
        Linux,
        OSX
    }
#endif

    public class NoPortAvailableException : Exception
    {
        public NoPortAvailableException(string message) : base(message) { }
    }
}