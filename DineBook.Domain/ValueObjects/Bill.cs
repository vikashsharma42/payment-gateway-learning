namespace DineBook.Domain.ValueObjects
{
    // What a guest pays for one order: the items, GST on the items, and the restaurant's access fee.
    // Built by Restaurant.CalculateBill. Never stored on its own: Payment stores the total.
    public sealed record Bill(Money Subtotal, Money Tax, Money AccessFee)
    {
        public Money Total => Subtotal.Add(Tax).Add(AccessFee);
    }
}
