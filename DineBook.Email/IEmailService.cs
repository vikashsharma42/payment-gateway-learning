using DineBook.Email.EmailModels;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Email
{
    public interface IEmailService
    {
        /// <summary>Renders the view for the model and sends it. Throws when sending fails.</summary>
        Task SendAsync(BaseEmailModel emailModel, CancellationToken ct = default);
    }
}