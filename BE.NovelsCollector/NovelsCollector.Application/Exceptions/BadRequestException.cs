using NovelsCollector.Domain;

namespace NovelsCollector.Application.Exceptions
{
    public class BadRequestException : DomainException
    {
        public BadRequestException()
        {
        }

        public BadRequestException(string message)
            : base(message)
        {
        }

        public BadRequestException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
