using System.Globalization;

namespace ProductShop.Web.Helpers
{
    public static class Money
    {
        private static readonly CultureInfo IndiaCulture = new CultureInfo("en-IN");

        // 65999 -> "₹65,999.00"; 100000 -> "₹1,00,000.00"
        public static string Inr(decimal amount) => "₹" + amount.ToString("N2", IndiaCulture);
    }
}
