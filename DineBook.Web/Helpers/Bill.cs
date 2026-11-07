namespace DineBook.Web.Helpers
{
    // One food order's bill, as the customer sees it. Every amount is rounded to paise.
    public sealed record Bill(
        decimal Subtotal,
        decimal SystemAccessFee,
        string TaxType,
        decimal TaxPercent,
        decimal Tax,
        decimal Total);
}
