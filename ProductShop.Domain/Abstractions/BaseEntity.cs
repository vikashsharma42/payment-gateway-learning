namespace ProductShop.Domain.Abstractions
{
    public abstract class BaseEntity
    {
        public virtual long Id { get; protected set; }

        public bool IsTransient() => Id == default;
    }
}
