namespace Amongst.Exception
{
    public class MongodStartTimeoutException : System.Exception
    {
        public MongodStartTimeoutException(string message) : base(message) { }
    }
}