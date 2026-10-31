namespace ProductShop.Domain.Abstractions
{
    public abstract class BaseEntity
    {
        public virtual int Id { get; protected set; }

        public bool IsTransient() => Id == default;
    }
}
