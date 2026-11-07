namespace DineBook.Web.Helpers
{
    // The rates a restaurant charges, read from its row: GST percent and the flat system access fee.
    public sealed record RestaurantCharges(decimal TaxPercent, decimal SystemAccessFee, string TaxType);
}
