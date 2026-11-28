# DineBook

A .NET 10 Razor Pages app for restaurant table booking, food orders, bill payment (Cashfree) and staff work.
Design details are in [architecture.md](architecture.md). Release history is in [CHANGES.md](CHANGES.md).

## 1. Solution structure

```
DineBook.slnx
├── DineBook.Domain/              Entities, value objects (Money, Address, RestaurantSlug), rules, security scoring. No dependencies.
├── DineBook.Common/              ValueObject base, IDomainEvent
├── DineBook.Infrastructure/      EF Core context, configurations, seeders, DB-Schema scripts
├── DineBook.Application/         Use cases (ReservationService, MenuService, PaymentService, StaffWorkService, SecurityService, ...)
├── DineBook.Payments/            Cashfree gateway behind IPaymentGateway
├── DineBook.Email/               Brevo sender and Razor email templates
└── DineBook.Web/                 Razor Pages site (the only front end)
```

## 2. Pages and addresses

Restaurants are reached by their **slug** (for example `spice-garden`). Each restaurant has one or more **locations**, and each location has its own tables.

| URL | Who can open it | What it does |
|-----|-----------------|--------------|
| `/` | Everyone | Not found. There is no restaurant list. |
| `/{slug}` | Everyone | The restaurant page and its locations. |
| `/{slug}/{locationSlug}` | Everyone | One location. |
| `/{slug}/{locationSlug}/menu` | Everyone | The menu, grouped by category. |
| `/{slug}/{locationSlug}/book` | Everyone to view; signed-in customers to book | Pick a day, party size and time, then confirm. Signed-out visitors see a sign-in prompt. |
| `/{slug}/Reservations/{code}` | Anyone with the booking code and phone number | Booking details, change party size or special requests, cancel, pre-order food, pay. |
| `/{slug}/Checkout/{id}` | Anyone with the order id | Pays one food order through Cashfree. |
| `/{slug}/Login`, `/{slug}/Registration` | Everyone | Sign in and create a customer account at that restaurant. |
| `/{slug}/Profile` | Signed-in customer | Edit name and phone. |
| `/{slug}/Orders` | Signed-in customer | The customer's food orders at that restaurant. |
| `/customer`, `/Customer/Reservations`, `/Customer/Payments` | Signed-in customer | The customer's home, bookings and payments. |
| `/Login` | Administrators and staff without a restaurant address | Sign-in page for administrators (also used by `/Admin/Login`). |
| `/Logout` | Signed-in users | Sign-out. Every Sign out button opens a confirmation popup ("Are you sure you want to logout?"). This page is the fallback. `/Admin/Logout` forwards here. |
| `/Staff`, `/Staff/Dashboard`, `/Staff/Reservations`, `/Staff/Orders` | Staff and managers | Choose a location; seat, complete or mark no-show bookings; serve or cancel food orders. |
| `/Manager/...` | Managers | Dashboard, bookings, orders, menu, tables, locations, restaurant details, and tax and access-fee rates. |
| `/Admin`, `/Admin/Restaurants`, `/Admin/Restaurants/New`, `/Admin/Restaurants/{id}` | Administrators | Restaurants, locations, tables, menu and bill charges. |
| `/Admin/Users`, `/Admin/Payments`, `/Admin/Monitoring`, `/Admin/Dashboard` | Administrators | Users (paged), payments and refunds (paged), the security log and risky addresses, and the dashboard. |
| `/Payment/Result`, `/Payment/Webhook` | Cashfree | Payment return page, and the server-to-server webhook. |

Lists in the staff, manager and admin areas are paged (20 rows by default).

Slug rules (`RestaurantSlug`): lowercase letters, digits and single hyphens, at most 60 characters.
A name gives its slug (`Spice Garden` becomes `spice-garden`). A second similar name gets `-2`, `-3`, and so on.
Names that collide with a page word (`admin`, `login`, `staff`, `restaurants`, `bookings`, ...) get `-restaurant`.

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
Run these once on an empty database, in this order:

1. `DineBook.Infrastructure/Database/DB-Schema/ddd-domain-migration.sql`: the create-only script for the current domain model (includes `Restaurant.Slug` with a unique index).
2. `DineBook.Infrastructure/Database/DB-Schema/security-events.sql`: creates the `SecurityEvents` table used by the security monitor. The app needs it.
3. Existing databases only: `DineBook.Infrastructure/Database/DB-Schema/customer-country-migration.sql` (customer country and phone format).

The project does not use EF migrations. An older database must be rebuilt, or the missing columns and tables added by hand, before the app will run.
In the Development environment, `DiningSeeder` adds sample restaurants at startup, each with a slug from its name.

### Step 3: Admin account and payment settings
Set these with user-secrets, in `DineBook.Web`:
```bash
cd DineBook.Web
dotnet user-secrets set "Admin:Email" "you@example.com"
dotnet user-secrets set "Admin:Password" "a-password-of-at-least-12-characters"
dotnet user-secrets set "Cashfree:AppId" "your_test_app_id"
dotnet user-secrets set "Cashfree:SecretKey" "your_test_secret_key"
dotnet user-secrets set "Email:ApiKey" "your_brevo_api_key"
cd ..
```
`AdminAccountSeeder` creates the first admin account at startup when no admin exists.

## 5. Run

```bash
dotnet run --project DineBook.Web
```

Open a restaurant by its slug, for example `https://localhost:7080/spice-garden`. The root address `/` is not found on purpose.

## 6. Booking, orders and payment

1. Open `/{slug}`, choose a location, and browse the menu.
2. To book, sign in or create an account. Pick a day, a party size and a time, then confirm. You get a booking code. A guest cannot book a time that overlaps their own booking at that restaurant.
3. On `/{slug}/Reservations/{code}` (with the phone number on the booking):
   - Change the party size or special requests. Changes and cancellations close **2 hours before the start time**.
   - Cancel the booking in a confirmation popup (enter the phone number), subject to the same 2-hour cutoff.
   - Pre-order food. Each order is placed in one action.
   - Pay the bill for the orders. This opens `/{slug}/Checkout/{id}`, which creates a Cashfree session.
4. Cashfree returns to `/Payment/Result`, which confirms the status with Cashfree.
5. Cashfree may also call `/Payment/Webhook`. Duplicates are safe.

Staff at the booking's location seat the guests, mark completion or no-show, and serve or cancel the food orders.

Test card: `4111 1111 1111 1111`, any future expiry, CVV `123`.

For a local Cashfree return or webhook, expose the site with ngrok and use the HTTPS address in `Site:PublicBaseUrl`.

## 7. Configuration

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:DbConnection` | SQL Server connection |
| `Site:PublicBaseUrl` | Base address used for the payment return URL |
| `Admin:Email`, `Admin:Password` | First admin account. Keep both in user-secrets. |
| `Cashfree:AppId`, `Cashfree:SecretKey`, `Cashfree:Environment`, `Cashfree:ApiVersion` | Payment gateway (`sandbox` or `production`) |
| `Dining` section | `TimeZoneId` (default Asia/Kolkata), `MaxGuestsPerBooking` (10), `MinNoticeMinutes` (60), `MaxAdvanceDays` (30), `DefaultDurationMinutes` (90), `SlotMinutes` (30) |
| `Email` section | `Enabled`, `SenderEmail`, `SenderName`. `Email:ApiKey` goes in user-secrets. |
| `DataProtection:KeysPath` | Folder that every server can reach, so sign-in cookies work across servers |
| `Proxy:TrustForwardedHeaders` | Set to `true` only behind a trusted reverse proxy, so client addresses come from `X-Forwarded-For` |

## 8. Known gaps

- Changes, cancellations and pre-orders are checked by booking code and phone number, not by the signed-in account. Anyone with both can act on that booking.
- Menus are per restaurant. Bookings and tables are per location. Payments are per food order.
- `appsettings.json` has `Admin:Email` and `Admin:Password` keys. Keep real values in user-secrets, and change any password that was ever stored in the file.
- Not built: machine-learning anomaly detection, CAPTCHA (left out on request), alert emails for security events, confirmation emails for food orders, and reminder emails before a booking.
- The Content-Security-Policy is enforced. Check checkout (Cashfree) in a browser after any change to scripts or frames.
- Security counts are kept per server. Behind a load balancer, limits apply slightly differently on each server.
