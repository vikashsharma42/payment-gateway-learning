using System.Globalization;

namespace DineBook.Email.EmailModels
{
    /// <summary>
    /// Base for every customer email. Each subclass is the @model of its Razor view.
    /// </summary>
    public abstract class BaseEmailModel
    {
        public string ToAddress { get; set; }

        public string Subject { get; set; }

        protected static string FormatMoney(decimal amount) =>
            "₹" + amount.ToString("N2", CultureInfo.GetCultureInfo("en-IN"));
    }
}