namespace DineBook.Domain.Abstractions
{
    /// <summary>
    /// Marks an entity that is loaded and saved on its own, together with the children it owns.
    /// </summary>
    public abstract class AggregateRoot : BaseEntity
    {
    }
}
