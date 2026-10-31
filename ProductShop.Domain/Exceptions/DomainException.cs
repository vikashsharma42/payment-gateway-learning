using System;

namespace ProductShop.Domain.Exceptions
{
    /// <summary>
    /// Thrown when a business rule is violated. The API maps it to HTTP 400.
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