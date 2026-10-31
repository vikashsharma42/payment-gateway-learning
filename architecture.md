# ProductShop Architecture

ProductShop follows the layered structure used in the Nexora Intelligence API. The goal is a clear separation between business rules, data access, external services, and presentation, without adding repositories or other patterns the project does not need.

## Dependency graph

```
ProductShop.API ─────┐
                     ├──► ProductShop.Application ──► ProductShop.Payments
ProductShop.Web ─────┤                 │
                     │                 ▼
                     └──────► ProductShop.Infrastructure ──► ProductShop.Domain
```

- Dependencies point inward. `ProductShop.Domain` references nothing.
- `ProductShop.Payments` references nothing from this solution. It only depends on framework packages.
- `ProductShop.Web` references Application and Infrastructure. Product pages read the database directly, and the checkout pages call `IPaymentService`.
- `ProductShop.API` is the composition root for the API. Its `Startup.cs` wires all layers.

## Layers

### Domain
- `Entities/Product` and `Entities/Order` keep their state private and change it through methods.
- `Order.MarkAsPaid()` returns `false` for an already paid order. This makes webhook and status handling idempotent.
- Invalid operations throw `DomainException`. The API maps it to HTTP 400.

### Infrastructure
- `ApplicationDbContext` is the only EF Core context.
- Configurations live in `Database/Configurations`, one class per entity. Products are seeded there.
- `AddApplicationDatabase` registers SQLite using `ConnectionStrings:DbConnection`.

### Payments
- `IPaymentGateway` is the abstraction. Application code never references Cashfree types.
- `CashfreeGateway` calls the Cashfree REST API with `HttpClient` and verifies webhook signatures with HMAC-SHA256.
- `AddCashfreePayments` binds `CashfreeOptions` and validates the secrets at startup.

### Application
- `IPaymentService` / `PaymentService` implement the payment use cases:
  - `CreateOrderAsync`: read the price from the database, create the gateway order, save a `Pending` order.
  - `GetOrderStatusAsync`: confirm with the gateway and mark the order paid.
  - `HandleWebhookAsync`: verify the signature, then mark the order paid.

### API
- `Configuration/` holds the service and middleware setup, following the Nexora pattern.
- `Controllers/ProductsController` queries `ApplicationDbContext` directly.
- `Controllers/PaymentsController` calls `IPaymentService`.
- `DataObjects/` holds request and response records, so entities are never returned directly from payment endpoints.
- `Middleware/ExceptionHandlingMiddleware` returns ProblemDetails responses.

### Web
- Razor Pages read `ApplicationDbContext` directly.
- `Shared/_Layout.cshtml` provides the Bootstrap layout.

## Request flows

### Product listing
```
GET /api/products  →  ProductsController  →  ApplicationDbContext (AsNoTracking)  →  JSON
GET /               →  IndexModel.OnGetAsync  →  ApplicationDbContext             →  Razor view
```

### Create order
```
POST /api/payments/create-order
  → PaymentsController
  → PaymentService.CreateOrderAsync
      ├── ApplicationDbContext: load Product (price from DB)
      ├── IPaymentGateway.CreateOrderAsync  → Cashfree
      └── ApplicationDbContext: save Order (Pending)
  → 200 { orderId, paymentSessionId, amount, currency }
```

### Status check
```
GET /api/payments/status/{orderId}
  → PaymentService.GetOrderStatusAsync
      ├── load Order
      ├── IPaymentGateway.GetOrderStatusAsync → Cashfree
      └── if PAID and MarkAsPaid() returns true → save
```

### Webhook
```
POST /api/payments/webhook
  → PaymentService.HandleWebhookAsync
      ├── IPaymentGateway.VerifyWebhookSignature(rawBody, timestamp, signature)
      ├── if invalid → 400
      └── if PAYMENT_SUCCESS_WEBHOOK → load Order → MarkAsPaid() → save
```

### Razor checkout
```
GET  /Checkout/{id}   →  CheckoutModel.OnGetAsync      → product details + form
POST /Checkout/{id}   →  CheckoutModel.OnPostAsync
                          → IPaymentService.CreateOrderAsync (price from DB, Pending order)
                          → page shows "Pay" button with paymentSessionId
Browser: Cashfree JS SDK checkout (redirectTarget: _self)
GET  /Payment/Result?order_id=...  →  ResultModel.OnGetAsync
                          → IPaymentService.GetOrderStatusAsync (confirms with Cashfree)
```

## Design decisions

| Decision | Reason |
|----------|--------|
| No repository | Keeps the code smaller. The controllers and pages use `ApplicationDbContext` directly, as requested. |
| Application service for payments only | Payment logic spans the gateway, the database, and domain rules, so it needs one place. Product reads are simple queries. |
| `IPaymentGateway` abstraction | Makes a second gateway possible without changing `PaymentService` or the controllers. |
| `Startup.cs` + `Program.cs` | Matches the Nexora project. Services are grouped by extension methods. |
| Shared user-secrets for API and Web | Both projects need the Cashfree keys, and one secret store avoids duplicating them. |
| Prices from the database | The client sends only a `productId`. The price can never be set by the browser. |
| Idempotent `MarkAsPaid()` | Cashfree can send the same webhook more than once. |

## Adding a second payment gateway

1. Create `ProductShop.Payments/<Provider>/<Provider>Gateway.cs` that implements `IPaymentGateway`.
2. Add an `Add<Provider>Payments` extension method next to `AddCashfreePayments`.
3. Replace the registration in `Startup.cs`.

`PaymentService` and the controllers stay the same.
