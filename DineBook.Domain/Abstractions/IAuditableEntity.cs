using System;

namespace DineBook.Domain.Abstractions
{
    public interface IAuditableEntity
    {
        DateTimeOffset CreatedDateTime { get; set; }

        string CreatedBy { get; set; }

        DateTimeOffset UpdatedDateTime { get; set; }

        string UpdatedBy { get; set; }
    }
}
