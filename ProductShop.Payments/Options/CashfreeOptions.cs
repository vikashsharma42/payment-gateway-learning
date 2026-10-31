namespace ProductShop.Payments.Options;

public class CashfreeOptions
{
    public const string SectionName = "Cashfree";

    public string AppId { get; set; }
    public string SecretKey { get; set; }
    public string Environment { get; set; } = "sandbox";
    public string ApiVersion { get; set; } = "2023-08-01";

    public string BaseUrl => Environment.Equals("production", StringComparison.OrdinalIgnoreCase)
        ? "https://api.cashfree.com/pg"
        : "https://sandbox.cashfree.com/pg";
}