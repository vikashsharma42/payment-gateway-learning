using DineBook.Email.EmailModels;
using DineBook.Email.Renderer;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Email
{
    public sealed class EmailService : IEmailService
    {
        private readonly IRazorViewToStringRenderer _renderer;
        private readonly IEmailSender _sender;

        public EmailService(IRazorViewToStringRenderer renderer, IEmailSender sender)
        {
            _renderer = renderer;
            _sender = sender;
        }

        public async Task SendAsync(BaseEmailModel emailModel, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(emailModel);

            var viewPath = ResolveViewPath(emailModel);
            var body = await _renderer.RenderViewToStringAsync(viewPath, emailModel);

            // The body is always the rendered HTML view
            await _sender.SendAsync(new EmailMessage(emailModel.ToAddress, emailModel.Subject, body), ct);
        }

        private static string ResolveViewPath(BaseEmailModel emailModel)
        {
            switch (emailModel)
            {
                case PaymentReceivedEmailModel:
                    return "/Views/Emails/PaymentReceivedEmail/Html.cshtml";

                case RefundIssuedEmailModel:
                    return "/Views/Emails/RefundIssuedEmail/Html.cshtml";

                case ReservationConfirmedEmailModel:
                    return "/Views/Emails/ReservationConfirmedEmail/Html.cshtml";

                case ReservationCancelledEmailModel:
                    return "/Views/Emails/ReservationCancelledEmail/Html.cshtml";

                default:
                    throw new InvalidOperationException("Email model type does not have a corresponding Razor view.");
            }
        }
    }
}
