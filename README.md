# DineBook

A .NET 10 Razor Pages app for restaurant table booking, food orders and payment (Cashfree sandbox).
Design details are in [architecture.md](architecture.md). What changed recently is in [CHANGES.md](CHANGES.md).

## 1. Solution structure

```
DineBook.slnx
├── DineBook.Domain/              Entities, value objects (RestaurantSlug, Money, Address), rules. No dependencies.
├── DineBook.Common/              ValueObject base, IDomainEvent
├── DineBook.Infrastructure/      EF Core context, configurations, seeders, DB-Schema scripts
├── DineBook.Application/         Use cases (ReservationService, MenuService, PaymentService, ...)
├── DineBook.Payments/            Cashfree gateway behind IPaymentGateway
├── DineBook.Email/               Brevo sender and Razor email templates
└── DineBook.Web/                 Razor Pages site (the only front end)
```

## 2. Public pages and addresses

Restaurants are reached by their **slug** (the web address of a restaurant, for example `spice-garden`).

| URL | Who can open it | What it does |
|-----|-----------------|--------------|
| `/` | Everyone | Not found. There is no restaurant list. A restaurant is reached only by its slug. |
| `/{slug}` | Everyone | Restaurant dashboard: locations (switch with `?location=ID`), the menu grouped by category, and the booking form. |
| `POST /{slug}/{location}/book` | Signed-in accounts | Books a table. A signed-out visitor is sent to `/{slug}/Login` first. |
| `/{slug}/Reservations/{code}` | Everyone who has the code (cancel and order need the phone number) | Booking details, food orders, payment, cancel. A code under another restaurant's slug is "not found". |
| `/{slug}/Checkout/{id}` | Everyone who has the order id | Pays one food order. An order under another restaurant's slug is "not found". |
| `/{slug}/Login`, `/{slug}/Registration` | Everyone | Sign in and create an account at that restaurant. There is no restaurant list or select. A signed-in user is sent on to the restaurant (or their dashboard). |
| `/{slug}/Orders` | Signed-in customer | The customer's food orders at that restaurant. |
| `/Login` | Administrators | Administrator sign-in (no restaurant). |
| `/Payment/Result`, `/Payment/Webhook` | Cashfree | Payment return page, and the server-to-server webhook. |
| `/Admin/...` | Admin | Restaurants, locations, tables, menu, users. Sign in at `/Admin/Login`. |
| `/Staff` | Staff and managers | Tables and bookings for the chosen location. |

Slug rules (`RestaurantSlug` in the Domain): lowercase letters, digits and single hyphens, at most 60 characters.
A name gives its slug (`Spice Garden` becomes `spice-garden`). A second similar name gets `-2`, `-3`, and so on.
Names that collide with a page word (`admin`, `login`, `staff`, `restaurants`, `bookings`, ...) get `-restaurant`.
Reserved words cannot be used as slugs.

## 3. Requirements

- .NET 10 SDK (`dotnet --version` starts with `10.`)
- SQL Server (LocalDB works for development). The connection string is `ConnectionStrings:DbConnection`.
- Internet access for Bootstrap (CDN), dish photos (picsum.photos) and the Cashfree sandbox.

## 4. First-time setup

### Step 1: Build
```bash
dotnet restore
dotnet build
```

### Step 2: Create the database
`DineBook.Infrastructure/Database/DB-Schema/ddd-domain-migration.sql` is a **create-only** script for the current domain model. Run it once on an empty database. It includes `Restaurant.Slug` with a unique index.

The project does not use EF migrations. An older database created before the slug column existed must be rebuilt, or the `Slug` column added and filled in by hand, before the app will run.

In the Development environment, `DiningSeeder` adds sample restaurants at startup (each one gets its slug from its name).

### Step 3: Admin account and payment settings
Set these with user-secrets, in `DineBook.Web`:
```bash
cd DineBook.Web
dotnet user-secrets set "Admin:Email" "you@example.com"
dotnet user-secrets set "Admin:Password" "a-password-of-at-least-12-characters"
dotnet user-secrets set "Cashfree:AppId" "your_test_app_id"
dotnet user-secrets set "Cashfree:SecretKey" "your_test_secret_key"
cd ..
```
The first admin account is created at startup when no admin exists (`AdminAccountSeeder`).

## 5. Run

```bash
dotnet run --project DineBook.Web
```

Open a restaurant by its slug, for example `https://localhost:7080/spice-garden`. The root address `/` is not found on purpose.

## 6. Booking and payment flow

1. Open `/{slug}`. Browse the locations and the menu. Choose a location with the switcher.
2. To book, sign in (or create an account). Then pick a date, a party size and a time, and confirm. You get a booking code.
3. On `/{slug}/Reservations/{code}`, add food orders. Each order is placed in one action.
4. Pay an order. It opens `/{slug}/Checkout/{id}`, which creates a Cashfree session. Cashfree returns to `/Payment/Result`, which confirms the status with Cashfree.
5. Cashfree may also call `/Payment/Webhook`. Duplicates are safe.

Test card: `4111 1111 1111 1111`, any future expiry, CVV `123`.

For a local Cashfree return or webhook, expose the site with ngrok and use the HTTPS address in `Site:PublicBaseUrl`.

## 7. Configuration

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:DbConnection` | SQL Server connection |
| `Site:PublicBaseUrl` | Base address used for the payment return URL |
| `Admin:Email`, `Admin:Password` | First admin account (keep these in user-secrets) |
| `Cashfree:AppId`, `Cashfree:SecretKey`, `Cashfree:Environment` | Payment gateway (`sandbox` or `production`) |
| `Dining` section | Time zone, party size, notice period, advance days, booking length, slot length |
| `Email` section | `Enabled`, `SenderEmail`, `SenderName`. The Brevo API key goes in user-secrets |

## 8. Known gaps

- `/{slug}/Checkout/{id}` and `/{slug}/Reservations/{code}` do not yet check that the signed-in account owns the booking. Booking pages are public; only the booking itself needs sign-in.
- Bookings, the menu and bills are per restaurant, not per location yet. The location switcher only changes what the dashboard shows.
- `appsettings.json` in `DineBook.Web` contains an admin email and password in plain text. Move them to user-secrets and rotate the password.
- Sign-out (`/Admin/Logout`) is the only sign-out page. It is shared by all roles.
- No kitchen or staff screens for seating and serving yet (see [architecture.md](architecture.md)).
