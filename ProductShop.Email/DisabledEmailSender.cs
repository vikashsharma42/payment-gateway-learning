using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Email
{
    /// <summary>
    /// Used when Email:Enabled is false. Does nothing and makes no network calls.
    /// </summary>
    public sealed class DisabledEmailSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken ct = default) => Task.CompletedTask;
    }
}