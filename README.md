# DineBook

A .NET 10 restaurant table booking and food ordering site (Razor Pages), with Cashfree payments in sandbox mode.

Signed-in customers book a table, pre-order food from the booking, and pay each food order separately. There are no guest bookings: every booking belongs to an account. Customers see all their bookings and orders in one place. Admins manage restaurants, tables and menus.

See [architecture.md](architecture.md) for the design and [CHANGES.md](CHANGES.md) for the latest update.

## 1. Solution structure

```
DineBook.slnx
├── DineBook.Domain/           Entities and rules (no dependencies)
├── DineBook.Infrastructure/   EF Core (SQL Server), seed data, SQL scripts
│   └── Database/Scripts/      Numbered SQL scripts, run by hand
├── DineBook.Application/      Use cases (booking, orders, payments, sign-in, registration)
├── DineBook.Payments/         Cashfree gateway behind IPaymentGateway
├── DineBook.Email/            Razor email templates, Brevo sender
└── DineBook.Web/              Razor Pages site (the app you run)
```

`DineBook.API` is commented out in the solution. It is planned for later and is not in this version.

## 2. Requirements

- .NET 10 SDK (`dotnet --version` starts with `10.`)
- SQL Server (LocalDB works for development)
- Internet access for Bootstrap (CDN), dish photos (picsum.photos) and the Cashfree sandbox

## 3. Setup

### Step 1: Build
```bash
dotnet restore
dotnet build
```

### Step 2: Create or update the database
This project does not use EF migrations. Schema changes are SQL scripts in `DineBook.Infrastructure/Database/Scripts/`. Run them in order on the database named in `ConnectionStrings:DbConnection`. Each script is safe to run more than once.

| Script | What it does |
|--------|--------------|
| `001_dish_images.sql` | Adds `ImageUrl` to `MenuItem` and `OrderItem`, and fills in the sample dish photos |
| `002_accounts_own_bookings.sql` | Bookings belong to a customer account. Adds `ApplicationUser.Phone` and `Reservation.ApplicationUserId`. Stops if a booking has no account. |
| `003_restaurant_slugs.sql` | Adds `Restaurant.Slug`, the web address of each restaurant (for example `/spice-garden/`), and fills it in from the names |

Run one script with sqlcmd:
```bash
sqlcmd -S <server> -d <database> -E -i DineBook.Infrastructure/Database/Scripts/001_dish_images.sql
```
Or open it in SSMS and run it. The `GO` lines are required.

When you add a script, give it the next number and run it on every database (development, staging, production).

### Step 3: Settings
Set secrets with user-secrets from the `DineBook.Web` folder:

```bash
cd DineBook.Web
dotnet user-secrets set "Cashfree:AppId" "your_test_app_id"
dotnet user-secrets set "Cashfree:SecretKey" "your_test_secret"
```

No admin is created automatically. Register an account, then make it an admin in SQL: `UPDATE ApplicationUser SET Role = 'Admin' WHERE Email = 'you@example.com';`

### Step 4: Run
```bash
cd DineBook.Web
dotnet run
```
The site runs at `https://localhost:7080` and `http://localhost:5080` (see `Properties/launchSettings.json`).

In development, sample restaurants and menus are added when the database has none (`DiningSeeder`).

## 4. What customers can do

Every restaurant has its own web address, for example `/spice-garden/`. Its booking page is that address, and the pages for that restaurant sit under it. The restaurant list stays at `/`, and the admin pages keep `/Admin`.

- **Book a table:** sign in first, then pick a date, party size and time. Name, phone and email come from your account. Visitors who are not signed in see a "Sign in to book" button instead.
- **My bookings** (`/{slug}/Bookings`): every booking made with your account.
- **Booking details** (`/{slug}/Reservations/{code}`): add food, pay each food order, or cancel the booking. Only the account that made the booking can open it.
- **Order history** (`/{slug}/Orders`): food ordered for your bookings, with a photo for each dish.
- **Checkout** (`/{slug}/Checkout/{orderId}`): pay one food order through Cashfree. The receipt goes to the booking's email.

### Sign-in and registration
- `/{slug}/Login`: email and password. Creates the site cookie `DineBook.Auth` (7 days).
- `/{slug}/Registration`: creates an account. Each phone number can belong to one account only. You are signed in straight away.
- Password: at least 12 characters on registration.
- Sign out: the **Sign out** link in the header (`/{slug}/Logout`) ends the site cookie.

## 5. Admin

- `/Admin`: restaurant list. `/Admin/Restaurants/New`: create a restaurant. `/Admin/Restaurants/{id}`: tables and menu.
- `/Admin/Users`: staff accounts.
- Admin pages require the Admin role. Admins sign in at `/Admin/Login`.

## 6. Payments

1. The customer pays a food order on the booking page. `PaymentService` reads the amount from the database, never from the form.
2. It creates a Cashfree order and saves a `Payment` with status `Pending`. Email and phone come from the booking.
3. The browser opens Cashfree checkout with the payment session.
4. After payment, `/{slug}/Payment/Result` asks Cashfree for the status and marks the payment paid.

**Webhook:** Cashfree calls `POST /Payment/Webhook` after each payment. The request is checked with the HMAC-SHA256 signature (`x-webhook-signature`, over `x-webhook-timestamp` plus the raw body) using `Cashfree:SecretKey`. A valid event marks the payment paid, but only if the amount matches. Unsigned or wrong-signature requests get 401. Malformed signed requests get 400. Repeated events are safe.

Register the webhook URL in the Cashfree dashboard (sandbox first): `https://<your-public-host>/Payment/Webhook`. The public host must be reachable from the internet. For local testing, use a tunnel such as ngrok and set `Site:PublicBaseUrl` to match. The result page still checks the status as well, so a payment is also marked paid when the customer returns.

## 7. Configuration reference

| Key | Where | Purpose |
|-----|-------|---------|
| `ConnectionStrings:DbConnection` | appsettings / user-secrets | SQL Server connection string |
| `Cashfree:AppId`, `Cashfree:SecretKey` | user-secrets | Cashfree credentials |
| `Cashfree:Environment` | appsettings | `sandbox` or `production` (keep `sandbox` for testing) |
| `Site:PublicBaseUrl` | appsettings | Base URL used for the payment return page |
| `Dining:*` | appsettings | Time zone, party size, notice, advance days, booking length, slot length |
| `Email:Enabled`, `Email:SenderEmail`, `Email:SenderName` | appsettings | Booking and payment emails |
| `Email:ApiKey` | user-secrets | Brevo API key (emails are off while `Enabled` is false) |


## 8. Troubleshooting

| Problem | Solution |
|---------|----------|
| `Invalid column name 'ImageUrl'` | Run `Database/Scripts/001_dish_images.sql` (Step 2). |
| Cashfree secrets missing | Set `Cashfree:AppId` and `Cashfree:SecretKey` (Step 3). |
| Dish photos do not load | Needs internet access to `picsum.photos`, or the URL in `MenuItem.ImageUrl` is wrong. |
| My bookings is empty | Bookings are listed for the account that made them. Sign in with that account. |
| HTTPS certificate warning | `dotnet dev-certs https --trust` |

## 9. Errors

- Rule violations (`DomainException`, for example "No table is free") are caught once by `DomainExceptionMiddleware`. The visitor sees the message as a toast. Form POSTs redirect back to the same page; GET requests go to the home page.
- Payment webhooks (`/Payment/Webhook`) get status codes from the same middleware: 200 when the event is valid but needs no change, 400 for a malformed body.
- Any other error is logged once by `GlobalExceptionHandler` (without form data) and the visitor sees the error page.

## 10. Notes

- Never commit secrets. Keep Cashfree keys and the admin password in user-secrets.
- Test card: `4111 1111 1111 1111`, any future expiry, CVV `123`.
- Do not trust the browser redirect alone in production. Configure the Cashfree webhook (section 6) before going live.
