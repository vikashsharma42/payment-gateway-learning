# DineBook

A .NET 10 restaurant table booking and food ordering site (Razor Pages), with Cashfree payments in sandbox mode.

Guests book a table, pre-order food from the booking, and pay each food order separately. Customers see all their bookings and orders in one place. Admins manage restaurants, tables and menus.

See [architecture.md](architecture.md) for the design and [CHANGES.md](CHANGES.md) for the latest update.

## 1. Solution structure

```
DineBook.slnx
├── DineBook.Domain/           Entities and rules (no dependencies)
├── DineBook.Common/           Shared base types
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
dotnet user-secrets set "Admin:Email" "you@example.com"
dotnet user-secrets set "Admin:Password" "a-password-of-at-least-12-characters"
```

`Admin:Email` and `Admin:Password` create the first admin account when no admin exists.

### Step 4: Run
```bash
cd DineBook.Web
dotnet run
```
The site runs at `https://localhost:7080` and `http://localhost:5080` (see `Properties/launchSettings.json`).

In development, sample restaurants and menus are added when the database has none (`DiningSeeder`).

## 4. What guests and customers can do

- **Book a table:** pick a date, party size and time. Name, phone and email are filled in from your account, or from your last booking on this browser.
- **My bookings** (`/Bookings`): all bookings made with your account phone, or on this browser.
- **Booking details** (`/Reservations/Details?code=...`): add food, pay each food order, or cancel the booking.
- **Order history** (`/Orders`): food ordered for your bookings, with a photo for each dish.
- **Checkout** (`/Checkout/{orderId}`): pay one food order through Cashfree. The receipt goes to the booking's email.

### Opening a booking without signing in
A booking made on this browser opens without a phone number. If someone sends you a booking code, open it with the phone number used for the booking. This browser then remembers it. The phone number is checked once, not on every page.

### Sign-in and registration
- `/Login`: email and password. Creates the site cookie `DineBook.Auth` (7 days).
- `/Registration`: creates an account. A guest record with the same phone is linked to it. You are signed in straight away.
- Password: at least 12 characters on registration.
- Sign out: the **Sign out** link in the header (`/Logout`) ends the site cookie.

## 5. Admin

- `/Admin`: restaurant list. `/Admin/Restaurants/New`: create a restaurant. `/Admin/Restaurants/{id}`: tables and menu.
- `/Admin/Users`: staff accounts.
- Admin pages require the Admin role.

## 6. Payments

1. The guest pays a food order on the booking page. `PaymentService` reads the amount from the database, never from the form.
2. It creates a Cashfree order and saves a `Payment` with status `Pending`. Email and phone come from the booking.
3. The browser opens Cashfree checkout with the payment session.
4. After payment, `/Payment/Result` asks Cashfree for the status and marks the payment paid.

**Not yet wired:** the Cashfree webhook. `PaymentService.HandleWebhookAsync` exists, but no web page or API endpoint calls it. Payments are marked paid only when the result page checks the status. Until the webhook is wired, a payment that succeeds but whose customer never returns to the site stays pending.

## 7. Configuration reference

| Key | Where | Purpose |
|-----|-------|---------|
| `ConnectionStrings:DbConnection` | appsettings / user-secrets | SQL Server connection string |
| `Cashfree:AppId`, `Cashfree:SecretKey` | user-secrets | Cashfree credentials |
| `Cashfree:Environment` | appsettings | `sandbox` or `production` (keep `sandbox` for testing) |
| `Admin:Email`, `Admin:Password` | user-secrets | First admin account |
| `Site:PublicBaseUrl` | appsettings | Base URL used for the payment return page |
| `Dining:*` | appsettings | Time zone, party size, notice, advance days, booking length, slot length |
| `Email:Enabled`, `Email:SenderEmail`, `Email:SenderName` | appsettings | Booking and payment emails |
| `Email:ApiKey` | user-secrets | Brevo API key (emails are off while `Enabled` is false) |

`Jwt:Key` is not needed for this version. JWT is part of the planned API.

## 8. Troubleshooting

| Problem | Solution |
|---------|----------|
| `Invalid column name 'ImageUrl'` | Run `Database/Scripts/001_dish_images.sql` (Step 2). |
| Cashfree secrets missing | Set `Cashfree:AppId` and `Cashfree:SecretKey` (Step 3). |
| Dish photos do not load | Needs internet access to `picsum.photos`, or the URL in `MenuItem.ImageUrl` is wrong. |
| Phone field asks for a booking phone | The booking was made on another browser. Enter its phone number once. |
| My bookings is empty | You are signed out and made the booking on another browser, or the account phone differs from the booking phone. |
| HTTPS certificate warning | `dotnet dev-certs https --trust` |

## 9. Notes

- Never commit secrets. Keep Cashfree keys and the admin password in user-secrets.
- Test card: `4111 1111 1111 1111`, any future expiry, CVV `123`.
- Do not trust the browser redirect alone in production. Wire the webhook before going live (section 6).
