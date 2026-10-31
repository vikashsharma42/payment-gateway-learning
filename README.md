# ProductShop

ProductShop is a .NET 10 solution with two applications:

1. **ProductShop.Web**: A Razor Pages website that shows 10 products as cards (image, name, description, and price).
2. **ProductShop.Api**: A Web API that returns the same products as JSON, so a frontend (React, Angular, plain JavaScript, etc.) can call it.

Both projects share a class library, **ProductShop.Core**, which contains the product model and the service interface. The data is currently hardcoded in the backend. To load data from a database later, you only need to replace one service class. The Web and API projects will keep working without changes.

---

## 1. Solution structure

```
ProductShop/
├── ProductShop.sln
├── README.md
├── .gitignore
├── ProductShop.Core/                 (Class Library, net10.0)
│   ├── ProductShop.Core.csproj
│   ├── Models/
│   │   └── Product.cs
│   └── Services/
│       ├── IProductService.cs
│       └── HardcodedProductService.cs
├── ProductShop.Web/                  (Razor Pages web app, net10.0)
│   ├── ProductShop.Web.csproj
│   ├── Program.cs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── wwwroot/                      (static files, e.g. local images)
│   └── Pages/
│       ├── _ViewImports.cshtml
│       ├── _ViewStart.cshtml
│       ├── Index.cshtml              (product card UI)
│       ├── Index.cshtml.cs           (page model, gets data from the service)
│       ├── Error.cshtml
│       ├── Error.cshtml.cs
│       └── Shared/
│           └── _Layout.cshtml        (Bootstrap layout)
└── ProductShop.Api/                  (ASP.NET Core Web API, net10.0)
    ├── ProductShop.Api.csproj
    ├── Program.cs
    ├── Properties/
    │   └── launchSettings.json
    └── Controllers/
        └── ProductsController.cs
```

---

## 2. What each project does

### ProductShop.Core (Class Library)
This project has no UI and no HTTP code. It contains:

- **Models/Product.cs**: the product model.
  - `Id` (int), `Name`, `Description`, `Price` (decimal, in rupees), `ImageUrl`
- **Services/IProductService.cs**: the data access contract.
  - `GetAllAsync()`: returns all products.
  - `GetByIdAsync(int id)`: returns one product, or `null` if it does not exist.
- **Services/HardcodedProductService.cs**: a temporary implementation of `IProductService` that holds 10 products in code.

This design lets you add a database later by writing one new class (for example, `DbProductService`).

### ProductShop.Web (Razor Pages)
- **Program.cs**: enables Razor Pages and registers `IProductService` with `HardcodedProductService`.
- **Pages/Index.cshtml.cs**: in `OnGetAsync`, loads the products from the service and stores them in the `Products` property.
- **Pages/Index.cshtml**: renders the products as Bootstrap cards. Prices are shown with the rupee sign and Indian-style grouping (for example, `65,999`).
- **Pages/Shared/_Layout.cshtml**: the navbar and the Bootstrap CSS (loaded from a CDN).
- **Pages/Error.cshtml(.cs)**: the error page shown in production when an unhandled exception occurs.

### ProductShop.Api (Web API)
- **Program.cs**: configures controllers, OpenAPI, service registration, and CORS.
- **Controllers/ProductsController.cs**: exposes two endpoints (see Section 5).

CORS is enabled so that a browser-based frontend running on a different port can call the API. For development, the policy allows any origin. In production, restrict it to your frontend's domain.

---

## 3. Requirements

- **.NET 10 SDK**. Check your installed version with:
  ```bash
  dotnet --version
  ```
  The output should start with `10.`. If it does not, download the SDK from https://dotnet.microsoft.com/download.
- An editor: Visual Studio 2022 (17.12 or later), VS Code, or JetBrains Rider.
- An internet connection. The Bootstrap CSS and the product images (from `picsum.photos`) are loaded from external servers.

---

## 4. Setup and run

### Option A: Visual Studio
1. Open `ProductShop.sln`.
2. In Solution Explorer, right-click `ProductShop.Web` and choose **Set as Startup Project**.
3. Press **F5**. The website opens in your browser.
4. To run the API as well, right-click `ProductShop.Api` and choose **Debug → Start New Instance**.

You can also configure **Multiple Startup Projects** to run both at the same time.

### Option B: Command line
From the solution folder (where the `.sln` file is):

```bash
# Restore packages and build
dotnet restore
dotnet build

# Terminal 1: the Razor Pages website
dotnet run --project ProductShop.Web

# Terminal 2: the Web API
dotnet run --project ProductShop.Api
```

Default URLs (from `launchSettings.json`):

| Project          | HTTP                  | HTTPS                   |
|------------------|-----------------------|-------------------------|
| ProductShop.Web  | http://localhost:5080 | https://localhost:7080  |
| ProductShop.Api  | http://localhost:5200 | https://localhost:7200  |

If you see an HTTPS certificate warning, trust the development certificate once:
```bash
dotnet dev-certs https --trust
```

---

## 5. Verifying the application

### Razor Pages website
Open `http://localhost:5080/` (or `https://localhost:7080/`).

You should see 10 product cards: Laptop, Smartphone, Wireless Headphones, Smartwatch, Mechanical Keyboard, Wireless Mouse, 27" Monitor, Tablet, Backpack, and Water Bottle.

### API endpoints

| Method | URL                        | Description                                  |
|--------|----------------------------|----------------------------------------------|
| GET    | `/api/products`            | All 10 products (JSON array)                 |
| GET    | `/api/products/{id}`       | One product by id (1 to 10)                  |
| GET    | `/openapi/v1.json`         | OpenAPI document (Development environment only) |

Example: `http://localhost:5200/api/products/3`

Response:
```json
{
  "id": 3,
  "name": "Wireless Headphones",
  "description": "Noise cancelling, 30h battery",
  "price": 2999,
  "imageUrl": "https://picsum.photos/seed/headphones/600/400"
}
```

If the id does not exist (for example, `/api/products/99`), the API returns `404 Not Found`.

---

## 6. Calling the API from a frontend

Plain JavaScript example:

```html
<script>
  const API_BASE = "http://localhost:5200/api";

  async function loadProducts() {
    const res = await fetch(`${API_BASE}/products`);
    if (!res.ok) throw new Error("API error: " + res.status);
    const products = await res.json();

    products.forEach(p => {
      console.log(`${p.name} - ₹${p.price}`);
    });
  }

  loadProducts();
</script>
```

Notes:
- If your frontend runs over HTTPS, change `API_BASE` to `https://localhost:7200/api`.
- Run `dotnet dev-certs https --trust` first to avoid certificate warnings in the browser.
- JSON property names are camelCase (`name`, `price`, `imageUrl`), not the C# names (`Name`, `Price`).

---

## 7. Changing product data (current version)

The products are defined in `ProductShop.Core/Services/HardcodedProductService.cs`. To change a product's name, description, price, or image, edit that file directly:

```csharp
new() { Id = 1, Name = "Laptop", Description = "14-inch, 16GB RAM, 512GB SSD", Price = 65999, ImageUrl = "https://picsum.photos/seed/laptop/600/400" },
```

Prices are decimal values in rupees. The website displays them as `₹65,999`.

### Using local images
1. Copy the image file to `ProductShop.Web/wwwroot/images/`, for example `laptop.jpg`.
2. Set `ImageUrl` to `/images/laptop.jpg`.

Note: a relative path like this works only in the Web project. The API project has no `wwwroot`, so for the API use a full URL, or serve the images from the API project.

---

## 8. Adding a database (next step)

The design makes a database easy to add:

1. Create an EF Core `DbContext` and a `DbProductService` class that implements `IProductService`. You can place them in `ProductShop.Core` or in a new project such as `ProductShop.Data`.
2. In both `Program.cs` files, change the registration:

   ```csharp
   // current
   builder.Services.AddSingleton<IProductService, HardcodedProductService>();

   // after adding a database
   builder.Services.AddScoped<IProductService, DbProductService>();
   ```

3. The Razor Page (`Index.cshtml.cs`) and the API controller (`ProductsController.cs`) do not need any changes, because both depend only on `IProductService`.

Use `AddScoped` instead of `AddSingleton` when the service depends on a `DbContext`, because `DbContext` is scoped.

---

## 9. Troubleshooting

| Problem | Solution |
|---------|----------|
| `dotnet` command not found | Install the .NET 10 SDK and restart the terminal. |
| `The type or namespace ... could not be found` | Run `dotnet restore`. Check the project references with `dotnet sln list`. |
| The page has no styling | Check your internet connection. The Bootstrap link is in `_Layout.cshtml`. |
| Product images do not load | `picsum.photos` needs internet access. Or use local images (Section 7). |
| CORS error in the frontend | Make sure the API is running. CORS is configured in `Program.cs` with the `AllowFrontend` policy. |
| HTTPS certificate warning | Run `dotnet dev-certs https --trust`. |
| Port already in use | Change the port in `launchSettings.json`, or stop the process that is using it. |

---

## 10. Current limitations

- The data is hardcoded. Changes made in code are only visible after you rebuild and restart.
- There is no cart, login, or payment feature.
- CORS allows all origins, which is acceptable only for development.
- Product images come from external URLs (`picsum.photos`).
- The website has only a product listing page. There is no product detail page.

---

## 11. Possible next steps

- A database service with EF Core and SQLite or SQL Server.
- A product detail page (`/Products/Details/{id}`).
- A cart and a Cashfree payment integration (sandbox mode).
- An admin page to add and edit products.
- Authentication for the API and a stricter production CORS policy.

---

## 12. Quick command reference

```bash
dotnet --version                       # should show 10.x
dotnet restore
dotnet build
dotnet run --project ProductShop.Web   # http://localhost:5080
dotnet run --project ProductShop.Api   # http://localhost:5200/api/products
```
