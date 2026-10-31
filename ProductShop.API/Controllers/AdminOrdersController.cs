using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductShop.Application.Services.Interface;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.API.Controllers
{
    [ApiController]
    [Route("api/admin/orders")]
    public class AdminOrdersController : ControllerBase
    {
        private const string KeyHeader = "X-Admin-Key";

        private readonly IPaymentService _payments;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminOrdersController> _logger;

        public AdminOrdersController(
            IPaymentService payments,
            IConfiguration configuration,
            ILogger<AdminOrdersController> logger)
        {
            _payments = payments;
            _configuration = configuration;
            _logger = logger;
        }

        // Refunds move money, so this is restricted. Unauthorized requests get 404,
        // so the endpoint is not advertised to people probing the API.
        [HttpPost("{gatewayOrderId}/refund")]
        [EnableRateLimiting("admin")]
        public async Task<IActionResult> RefundAsync(string gatewayOrderId, CancellationToken ct)
        {
            if (!IsAuthorized())
            {
                _logger.LogWarning("Rejected admin refund request: missing or wrong key");
                return NotFound();
            }

            var order = await _payments.RefundAsync(gatewayOrderId, ct);
            if (order is null)
                return NotFound();

            _logger.LogInformation(
                "Admin refund for order {GatewayOrderId} finished with status {Status}",
                gatewayOrderId, order.Status);

            return Ok(new
            {
                orderId = order.GatewayOrderId,
                status = order.Status.ToString(),
                refundId = order.RefundId
            });
        }

        private bool IsAuthorized()
        {
            // Disabled unless a key is configured (set Admin:ApiKey in user-secrets)
            var expected = _configuration["Admin:ApiKey"];
            if (string.IsNullOrWhiteSpace(expected))
                return false;

            if (!Request.Headers.TryGetValue(KeyHeader, out var provided))
                return false;

            // Constant-time comparison, so the key cannot be guessed by timing responses
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(provided.ToString()));
        }
    }
}
