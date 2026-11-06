using System;

namespace DineBook.Domain.Exceptions
{
    /// <summary>
    /// Thrown when a business rule is violated. The API maps it to HTTP 400, the Web app shows it as a message.
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException() : base("A domain rule was violated.")
        {
        }

        public DomainException(string message) : base(message)
        {
        }

        public DomainException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
