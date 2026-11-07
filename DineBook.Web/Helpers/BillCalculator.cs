using System;

namespace DineBook.Web.Helpers
{
    public static class BillCalculator
    {
        public static Bill Calculate(decimal subtotal, RestaurantCharges charges)
        {
            ArgumentNullException.ThrowIfNull(charges);

            var fee = charges.SystemAccessFee;
            var tax = Round((subtotal + fee) * charges.TaxPercent / 100m);
            var total = subtotal + fee + tax;

            var taxType = string.IsNullOrWhiteSpace(charges.TaxType) ? "GST" : charges.TaxType.Trim();

            return new Bill(subtotal, fee, taxType, charges.TaxPercent, tax, total);
        }

        private static decimal Round(decimal amount) =>
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
}
