using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Email
{
    public sealed record EmailMessage(string To, string Subject, string HtmlBody);

    public interface IEmailSender
    {
        /// <summary>
        /// Sends one email. Throws when the provider rejects it. Callers decide whether a failure matters.
        /// </summary>
        Task SendAsync(EmailMessage message, CancellationToken ct = default);
    }
}