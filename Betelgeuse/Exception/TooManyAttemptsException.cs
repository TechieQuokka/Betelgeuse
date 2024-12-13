namespace Betelgeuse
{
    public class TooManyAttemptsException : Exception
    {
        public TooManyAttemptsException() : base("Too many login attempts. Please try again later.")
        {
        }

        public TooManyAttemptsException(string message) : base(message)
        {
        }

        public TooManyAttemptsException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
