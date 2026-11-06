namespace DineBook.Domain.Abstractions
{
    public abstract class BaseEntity
    {
        public virtual long Id { get; protected set; }
    }
}
