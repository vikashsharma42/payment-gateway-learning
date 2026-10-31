# ProductShop

A .NET 10 project with a Razor Pages product storefront, a Web API for frontend consumption, and Cashfree payment gateway integration (sandbox mode).

The project follows the same layered structure as the Nexora Intelligence API: Domain, Application, Infrastructure, Payments, and presentation (API and Web). See [architecture.md](architecture.md) for the design details.

## 1. Solution structure

```
ProductShop/
├── ProductShop.slnx
├── README.md
├── architecture.md
├── .gitignore
├── .editorconfig
├── ProductShop.Domain/                         (no dependencies)
│   ├── Abstractions/BaseEntity.cs
│   ├── Entities/Product.cs                     (ChangePrice)
│   ├── Entities/Order.cs                       (MarkAsPaid, MarkAsFailed)
│   ├── Enum/OrderStatus.cs
│   └── Exceptions/DomainException.cs
├── ProductShop.Infrastructure/                 (EF Core + SQLite)
│   ├── Database/ApplicationDbContext.cs
│   ├── Database/Configurations/ProductConfiguration.cs   (10 seed products)
│   ├── Database/Configurations/OrderConfiguration.cs
│   └── DependencyInjection/IServiceCollectionExtensions.cs (AddApplicationDatabase)
├── ProductShop.Payments/                       (gateway integration)
│   ├── Configuration/CashfreeOptions.cs
│   ├── Gateway/IPaymentGateway.cs
│   ├── Gateway/PaymentResults.cs
│   ├── Cashfree/CashfreeGateway.cs
│   └── DependencyInjection/IServiceCollectionExtensions.cs (AddCashfreePayments)
├── ProductShop.Application/                    (use cases)
│   ├── Services/PaymentService.cs
│   ├── Services/Interface/IPaymentService.cs
│   └── DependencyInjection/IServiceCollectionExtensions.cs (AddProductShopApplication)
├── ProductShop.API/                            (Web API)
│   ├── Program.cs, Startup.cs
│   ├── appsettings.json
│   ├── Configuration/IServiceCollectionExtension.cs      (CORS, JSON, OpenAPI)
│   ├── Configuration/IApplicationBuilderExtension.cs     (middleware, endpoints)
│   ├── Controllers/ProductsController.cs                 (direct DB)
│   ├── Controllers/PaymentsController.cs                 (uses IPaymentService)
│   ├── DataObjects/Request/CreateOrderRequest.cs
│   ├── DataObjects/Response/ProductResponse.cs, CreateOrderResponse.cs, OrderStatusResponse.cs
│   └── Middleware/ExceptionHandlingMiddleware.cs
└── ProductShop.Web/                            (Razor Pages)
    ├── Program.cs, Startup.cs
    ├── appsettings.json
    ├── Pages/Index.cshtml(.cs)                           (direct DB, Buy button)
    ├── Pages/Checkout.cshtml(.cs)                        (email/phone form, creates order, opens Cashfree)
    ├── Pages/Payment/Result.cshtml(.cs)                  (status after payment)
    ├── Pages/Error.cshtml(.cs), Pages/_ViewImports.cshtml, Pages/_ViewStart.cshtml
    ├── Pages/Shared/_Layout.cshtml
    └── wwwroot/
```

## 2. Architecture rules

- **Domain** has no dependencies. Business rules live in the entities (`Order.MarkAsPaid()`, `Product.ChangePrice()`).
- **Infrastructure** owns `ApplicationDbContext`, EF Core configurations, and seed data.
- **Payments** isolates the gateway behind `IPaymentGateway`. Cashfree is the only implementation.
- **Application** contains use cases (`PaymentService`). It depends on Domain, Infrastructure, and Payments.
- **API and Web** are the presentation layers. They are composed in `Startup.cs`.
- There is **no repository layer**. The Web pages and `ProductsController` query `ApplicationDbContext` directly. `PaymentService` also uses the context directly, as `ChatSessionService` does in Nexora.

## 3. Requirements

- .NET 10 SDK (`dotnet --version` should start with `10.`)
- EF Core CLI tools: `dotnet tool install --global dotnet-ef`
- Visual Studio 2022 (17.12+), VS Code, or Rider
- Internet access (Bootstrap CDN, product images from `picsum.photos`, Cashfree sandbox)

## 4. First-time setup

Run every command from the **solution folder** (where `ProductShop.slnx` is). The SQLite file `productshop.db` is created relative to the current directory, so using one folder for all commands keeps Web and API on the same database.

### Step 1: Restore and build
```bash
dotnet restore
dotnet build
```

### Step 2: Create the database
```bash
dotnet ef migrations add InitialCreate --project ProductShop.Infrastructure --startup-project ProductShop.API --output-dir Database/Migrations
dotnet ef database update --project ProductShop.Infrastructure --startup-project ProductShop.API
```

This creates `productshop.db` with the `Products` and `Orders` tables and the 10 seed products. After every model change, run `migrations add` again with a new name, then `database update`.

### Step 3: Set Cashfree test secrets (shared by API and Web)
`ProductShop.API` and `ProductShop.Web` use the same `UserSecretsId`, so you set the secrets once:

```bash
cd ProductShop.API
dotnet user-secrets set "Cashfree:AppId" "your_test_app_id"
dotnet user-secrets set "Cashfree:SecretKey" "your_test_secret_key"
cd ..
```

Both projects read these values. Never put them in `appsettings.json` or commit them to Git. Both projects will fail at startup if the secrets are missing (`ValidateOnStart`). This is intentional.

## 5. Run

```bash
# Terminal 1: Razor Pages website (products, checkout, payment result)
dotnet run --project ProductShop.Web      # http://localhost:5080

# Terminal 2: Web API
dotnet run --project ProductShop.API      # http://localhost:5200
```

Visual Studio: set **Multiple Startup Projects** (Web and API), or start them separately.

## 5b. Payment from Razor Pages

1. Open the home page (`/`) and click **Buy** on a product.
2. `/Checkout/{id}` asks for email and phone. Clicking **Continue to payment** creates the order. The price is read from the database.
3. The **Pay** button opens Cashfree checkout (sandbox).
4. After payment, Cashfree redirects to `/Payment/Result?order_id=...`. That page confirms the status with Cashfree and shows success, failed, or pending.

Test with card `4111 1111 1111 1111`, any future expiry, CVV `123`.

If Cashfree rejects the `localhost` return URL, expose the Web app with ngrok and open the site through its HTTPS URL.

## 6. Endpoints

| Method | URL                              | Description                                             |
|--------|----------------------------------|---------------------------------------------------------|
| GET    | `/` (Web)                        | Product listing page                                    |
| GET    | `/api/products`                  | All products                                            |
| GET    | `/api/products/{id}`             | One product (404 if not found)                          |
| POST   | `/api/payments/create-order`     | Creates a Cashfree order for a product (price from DB)  |
| GET    | `/api/payments/status/{orderId}` | Confirms the status with Cashfree and updates the order |
| POST   | `/api/payments/webhook`          | Cashfree server-to-server callback (signature verified) |
| GET    | `/openapi/v1.json`               | OpenAPI document (Development only)                     |

Create order request:
```bash
curl -X POST http://localhost:5200/api/payments/create-order \
  -H "Content-Type: application/json" \
  -d '{"productId":3,"customerEmail":"test@example.com","customerPhone":"9999999999"}'
```

Response:
```json
{
  "orderId": "order_638...",
  "paymentSessionId": "session_...",
  "amount": 2999,
  "currency": "INR"
}
```

Errors use RFC 7807 ProblemDetails. A business rule violation returns 400, and an unexpected error returns 500 with a generic message.

## 7. Payment flow

1. The browser calls `POST /api/payments/create-order` with a `productId`.
2. `PaymentService` reads the **price from the database**, creates the Cashfree order, and saves an `Order` with status `Pending`.
3. The frontend opens Cashfree checkout with the returned `paymentSessionId`.
4. After payment, the frontend calls `GET /api/payments/status/{orderId}`. The service checks the status with Cashfree and calls `order.MarkAsPaid()` if it is paid.
5. Cashfree may also call `POST /api/payments/webhook`. Duplicates are safe, because `MarkAsPaid()` returns `false` when the order is already paid.

## 8. Changing product data

Seed products are in `ProductShop.Infrastructure/Database/Configurations/ProductConfiguration.cs`. Changes there only apply to a new database. For an existing database, either update the `Products` table directly (for example, with DB Browser for SQLite), or add a migration.

## 9. Webhook testing (local)

Cashfree cannot reach `localhost`. Options:
- Use **ngrok** (`ngrok http 5200`) and register `https://<id>.ngrok-free.app/api/payments/webhook` in the Cashfree dashboard (test mode).
- Or skip the webhook and rely on `GET /api/payments/status/{orderId}`, which checks the status directly with Cashfree.

## 10. Configuration reference

| Key | Project | Purpose |
|-----|---------|---------|
| `ConnectionStrings:DbConnection` | API, Web | SQLite connection string |
| `Cors:Origins` | API | Allowed frontend origins |
| `Cashfree:AppId`, `Cashfree:SecretKey` | API and Web (shared user-secrets) | Cashfree credentials |
| `Cashfree:Environment` | API, Web | `sandbox` or `production` (keep `sandbox`) |

## 11. Troubleshooting

| Problem | Solution |
|---------|----------|
| `Cashfree:AppId and Cashfree:SecretKey must be set` | Run the `user-secrets set` commands in `ProductShop.API` (Step 3). |
| `no such table: Products` | Run the migration commands (Step 2). |
| Web and API show different data | Run both from the solution folder so they share `productshop.db`. |
| `dotnet ef` not found | `dotnet tool install --global dotnet-ef`, then restart the terminal. |
| CORS error in the frontend | Add your frontend origin to `Cors:Origins` in `ProductShop.API/appsettings.json`. |
| HTTPS certificate warning | `dotnet dev-certs https --trust` |
| Images not loading | Needs internet access to `picsum.photos`. |

## 12. Notes

- Never commit secrets. Keep Cashfree keys in user-secrets, and do not use live keys for this project.
- Test card: `4111 1111 1111 1111`, any future expiry, CVV `123`.
- Do not skip signature verification in the webhook, and do not trust the browser redirect alone to mark an order as paid.
