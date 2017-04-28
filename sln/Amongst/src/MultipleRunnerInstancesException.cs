using System;

namespace Amongst
{
    public class MultipleRunnerInstancesException : Exception
    {
        public MultipleRunnerInstancesException(string message) : base(message) { }
    }
}