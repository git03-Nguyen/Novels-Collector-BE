using NovelsCollector.Domain;

namespace NovelsCollector.Application.Exceptions
{
    public class UnauthorizedException : DomainException
    {
        public UnauthorizedException()
        {
        }

        public UnauthorizedException(string message)
            : base(message)
        {
        }

        public UnauthorizedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
