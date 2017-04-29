namespace Amongst.Exception
{ 
    public class MultipleTestRunnerInstancesException : System.Exception
    {
        public MultipleTestRunnerInstancesException(string message) : base(message) { }
    }
}