# ProductShop

A .NET 10 learning project with a Razor Pages product storefront, a Web API for frontend consumption, and Cashfree payment gateway integration (sandbox mode).

## 1. Architecture

The project uses **Domain-Driven Design (DDD)** with a few deliberate simplifications:

- **Domain** holds the entities and their business rules (`Product`, `Order`). It has no dependencies on other projects or packages.
- **Infrastructure** holds the EF Core `AppDbContext`, entity configurations, seed data, and SQLite setup.
- **Payments** holds the Cashfree integration behind the `IPaymentGateway` interface.
- **Web** (Razor Pages) and **Api** (controllers) use `AppDbContext` directly. There is **no repository layer**, by design.

```
ProductShop.Domain          (entities, business rules, no dependencies)
        ▲
        │
ProductShop.Infrastructure  (AppDbContext, EF Core, SQLite)
        ▲                ▲
        │                │
ProductShop.Web     ProductShop.Api ──► ProductShop.Payments (Cashfree, IPaymentGateway)
(Razor Pages)       (Controllers)
```

Dependencies only point inward. The Domain project references nothing.

## 2. Project structure

```
ProductShop/
├── ProductShop.sln
├── README.md
├── .gitignore
├── ProductShop.Domain/
│   ├── Common/DomainException.cs
│   ├── Products/Product.cs              (Create, ChangePrice)
│   └── Orders/Order.cs, OrderStatus.cs  (Create, MarkAsPaid, MarkAsFailed)
├── ProductShop.Infrastructure/
│   ├── DependencyInjection.cs           (AddInfrastructure)
│   └── Persistence/
│       ├── AppDbContext.cs
│       └── Configurations/
│           ├── ProductConfiguration.cs  (includes the 10 seed products)
│           └── OrderConfiguration.cs
├── ProductShop.Payments/
│   ├── Abstractions/IPaymentGateway.cs
│   ├── Cashfree/CashfreeGateway.cs
│   ├── Options/CashfreeOptions.cs
│   └── Extensions/PaymentServiceCollectionExtensions.cs  (AddCashfreePayments)
├── ProductShop.Web/                     (Razor Pages, product listing)
└── ProductShop.Api/
    ├── Program.cs
    ├── appsettings.json
    └── Controllers/
        ├── ProductsController.cs
        └── PaymentsController.cs
```

## 3. Requirements

- .NET 10 SDK (`dotnet --version` should start with `10.`)
- EF Core CLI tools: `dotnet tool install --global dotnet-ef`
- An editor: Visual Studio 2022 (17.12+), VS Code, or Rider
- Internet access (Bootstrap CDN, product images from `picsum.photos`, Cashfree sandbox)

## 4. First-time setup

Run all commands from the **solution folder** (where `ProductShop.sln` is). The SQLite file `productshop.db` is created in the current working directory, so using the same folder for every command keeps Web and Api on the same database.

### Step 1: Restore and build
```bash
dotnet restore
dotnet build
```

### Step 2: Create the database
```bash
dotnet ef migrations add InitialCreate --project ProductShop.Infrastructure --startup-project ProductShop.Api
dotnet ef database update --project ProductShop.Infrastructure --startup-project ProductShop.Api
```

This creates `productshop.db` with the `Products` and `Orders` tables and the 10 seed products.

### Step 3: Set Cashfree test secrets (Api project)
Get the **Test Environment** App ID and Secret Key from the Cashfree dashboard (Developers → API Keys). Never put them in `appsettings.json` or commit them to Git.

```bash
cd ProductShop.Api
dotnet user-secrets init
dotnet user-secrets set "Cashfree:AppId" "your_test_app_id"
dotnet user-secrets set "Cashfree:SecretKey" "your_test_secret_key"
cd ..
```

`Cashfree:Environment` is already `sandbox` in `appsettings.json`.

Note: `ValidateOnStart` makes the Api fail at startup if these secrets are missing. That is intentional.

## 5. Run

```bash
# Terminal 1: Razor Pages website
dotnet run --project ProductShop.Web      # http://localhost:5080

# Terminal 2: Web API
dotnet run --project ProductShop.Api      # http://localhost:5200
```

In Visual Studio, set **Multiple Startup Projects** (Web and Api), or start them separately. If you start from inside a project folder, the working directory changes and the SQLite file path changes too. Run from the solution folder to avoid this.

## 6. Endpoints

| Method | URL                              | Description                                            |
|--------|----------------------------------|--------------------------------------------------------|
| GET    | `/` (Web)                        | Product listing page                                   |
| GET    | `/api/products`                  | All products                                           |
| GET    | `/api/products/{id}`             | One product (404 if not found)                         |
| POST   | `/api/payments/create-order`     | Creates a Cashfree order. Body: `{ "productId": 3, "customerEmail": "...", "customerPhone": "9999999999" }` |
| GET    | `/api/payments/status/{orderId}` | Confirms status with Cashfree and updates the order    |
| POST   | `/api/payments/webhook`          | Cashfree server-to-server callback (signature verified)|

Example:
```bash
curl -X POST http://localhost:5200/api/payments/create-order \
  -H "Content-Type: application/json" \
  -d '{"productId":3,"customerEmail":"test@example.com","customerPhone":"9999999999"}'
```

The response contains `paymentSessionId`, which the frontend passes to the Cashfree JS SDK to open checkout.

## 7. Payment flow

1. Browser calls `POST /api/payments/create-order` with a `productId`.
2. The API reads the **price from the database**, calls Cashfree, and saves an `Order` with status `Pending`.
3. The frontend opens Cashfree checkout using `paymentSessionId`.
4. After payment, the frontend calls `GET /api/payments/status/{orderId}`. The API checks the status with Cashfree and calls `order.MarkAsPaid()` if it is paid.
5. Cashfree may also call `POST /api/payments/webhook`. Duplicate calls are safe because `MarkAsPaid()` returns `false` when the order is already paid.

Business rules live in the entity. For example, `Order.MarkAsPaid()` rejects paying a failed order, and `Product.ChangePrice()` rejects zero or negative prices.

## 8. Changing product data

Products are seeded in `ProductShop.Infrastructure/Persistence/Configurations/ProductConfiguration.cs`. Changing that file only affects a fresh database. For an existing database, either:

- update the `Products` table directly (for example with DB Browser for SQLite), or
- add a migration after changing the seed data.

## 9. Webhook testing (local)

Cashfree cannot reach `localhost`. Options:

- Use **ngrok** (`ngrok http 5200`) and set the public URL plus `/api/payments/webhook` in the Cashfree dashboard (Developers → Webhooks, test mode).
- Or skip the webhook and rely on `GET /api/payments/status/{orderId}`, which checks the status directly with Cashfree.

## 10. Troubleshooting

| Problem | Solution |
|---------|----------|
| `Cashfree:AppId and Cashfree:SecretKey must be set` on startup | Run the `user-secrets set` commands in the Api project (Step 3). |
| `no such table: Products` | Run the migration commands (Step 2). |
| Different data in Web and Api | Both must run from the solution folder so they use the same `productshop.db`. |
| `dotnet ef` not found | `dotnet tool install --global dotnet-ef`, then restart the terminal. |
| `Failed to create payment order` | Check the Api console log. Usually the sandbox keys are wrong or the environment is `production`. |
| Images not loading | Needs internet access to `picsum.photos`. |
| HTTPS warning | `dotnet dev-certs https --trust` |

## 11. Notes

- Never commit secrets. Keep Cashfree keys in user-secrets, and never use live keys for this project.
- The CORS policy allows all origins. That is acceptable only for development.
- Test card: `4111 1111 1111 1111`, any future expiry, CVV `123`.
- Do not change the webhook or status logic to skip signature checks or to trust the browser redirect alone.
