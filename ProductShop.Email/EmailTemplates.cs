using System.Globalization;
using System.Net;

namespace ProductShop.Email
{
    /// <summary>
    /// Builds the customer emails. Every value is HTML-encoded, so data from the database
    /// can never inject markup into an email.
    /// </summary>
    public static class EmailTemplates
    {
        private static readonly CultureInfo IndiaCulture = new CultureInfo("en-IN");

        public static EmailMessage PaymentReceived(string to, string orderReference, decimal amount) =>
            new EmailMessage(
                to,
                "Payment received - " + orderReference,
                Wrap(
                    "Payment received",
                    $"Thank you. We have received your payment of <strong>{Money(amount)}</strong> " +
                    $"for order <code>{Enc(orderReference)}</code>."));

        public static EmailMessage RefundIssued(string to, string orderReference, decimal amount) =>
            new EmailMessage(
                to,
                "Refund issued - " + orderReference,
                Wrap(
                    "Refund issued",
                    $"A refund of <strong>{Money(amount)}</strong> for order <code>{Enc(orderReference)}</code> " +
                    "has been issued. It can take a few working days to reach your account."));

        private static string Money(decimal amount) => "₹" + amount.ToString("N2", IndiaCulture);

        private static string Enc(string value) => WebUtility.HtmlEncode(value ?? string.Empty);

        private static string Wrap(string heading, string bodyHtml) =>
            "<!doctype html><html><body style=\"font-family:Arial,sans-serif;color:#0f172a;\">" +
            $"<h2>{Enc(heading)}</h2><p>{bodyHtml}</p>" +
            "<p style=\"color:#64748b;font-size:12px;\">This is an automated message from ProductShop.</p>" +
            "</body></html>";
    }
}
