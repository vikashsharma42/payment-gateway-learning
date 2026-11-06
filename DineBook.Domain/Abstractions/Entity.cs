namespace DineBook.Domain.Abstractions
{
    public abstract class Entity
    {
        public virtual long Id { get; protected set; }
    }
}
