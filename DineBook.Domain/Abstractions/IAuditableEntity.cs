using System;

namespace DineBook.Domain.Abstractions
{
    /// <summary>
    /// Entities that record who created and last changed them. The DbContext fills these on save.
    /// </summary>
    public interface IAuditableEntity
    {
        DateTimeOffset CreatedDateTime { get; set; }

        string CreatedBy { get; set; }

        DateTimeOffset UpdatedDateTime { get; set; }

        string UpdatedBy { get; set; }
    }
}
