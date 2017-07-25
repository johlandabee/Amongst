namespace Amongst.Exception
{
    public class ExitCodeException : System.Exception
    {
        public ExitCodeException(string message) : base(message)
        {
        }
    }
}