using System;

#if NETSTANDARD1_6

#endif

namespace Amongst
{
    public class MultipleRunnerInstancesException : Exception
    {
        public MultipleRunnerInstancesException(string message) : base(message) { }
    }
}