# DineBook - Architecture

DineBook lets a customer reserve a table at a restaurant location and, for that booking, order food and pay the bill.
Staff seat, serve and close bookings, managers run their restaurant, and administrators run the platform and its security.

## Bounded contexts

| Context      | Entities (DineBook.Domain/Entities)                    | Owns                                                              |
|--------------|--------------------------------------------------------|-------------------------------------------------------------------|
| Identity     | ApplicationUser (`Identity`)                           | Sign-in account, role (Customer, Staff, Manager, Admin), restaurant link, active flag |
| Organization | Restaurant, Location                                   | Restaurant slug, opening hours, charges (tax, access fee), locations and their open state |
| Dining       | Table, Reservation, ReservationRules                   | Tables per location, bookings and the rules that hold a table     |
| Menu         | MenuItem (with menu categories)                        | Dishes per restaurant, price and availability                     |
| Ordering     | Orders, OrderItem                                      | Food orders for one booking (aggregate root with its lines)       |
| Payments     | Payment                                                | One Cashfree attempt to pay one food order                        |
| Security     | SecurityEvent, RiskScoring                             | Security log entries, risk weights and sign-in limits             |
| Email        | DineBook.Email                                         | Razor email templates, Brevo sender                               |
| Shared       | DineBook.Common, DineBook.Domain (Abstractions, Validation, ValueObjects) | Entity base, value objects (Money, Address), `DomainValidation<T>`, `DomainException` |

Entities follow one style: a constructor that checks its input with `DomainValidation<T>.Ensure(...)`, private setters,
and a virtual navigation property next to each foreign key. Child rows are added through their parent (for example
`restaurant.AddReservation`, `Orders.AddOrderItem`). Every entity implements `IAuditableEntity`; `ApplicationDbContext`
fills the audit fields on save. Entities never throw their own exceptions; they throw `DomainException` through the validation helper.

Layers, outside in: Web (Razor Pages) -> Application (services) -> Domain (rules) and Infrastructure (EF Core).
Domain has no framework or database references, except the security scoring, which is pure code.

## Aggregates and key rules

- **Reservation**: one booking, identified by an 8-character code. Statuses: Confirmed, Seated, Completed, Cancelled, NoShow.
  - Holds a table for `DurationMinutes`. Only Confirmed and Seated bookings block a table.
  - Belongs to one location through its table, so bookings are per location.
  - The customer can change the party size and special requests, and cancel, only while the booking is Confirmed and before
    `ChangeDeadline` (start time minus 2 hours, `ReservationRules.CustomerChangeCutoff`). Staff actions are not limited by it.
  - Changing the party size picks a free table in the same location that seats the new party (in a serializable transaction).
- **Orders** (root): all dishes ordered for one booking at one time.
  - At least one item, at most 30 different items, 1 to 20 units per item, each item at most once (duplicates are merged first),
    every item available and on the same restaurant's menu.
  - Name and price are copied into each OrderItem, so later menu edits do not change past orders.
  - Food can be ordered while the booking is Confirmed or Seated and before it ends.
- **Payment**: one Cashfree attempt to pay one food order. A paid payment marks the order paid. Refunds are recorded on the payment.
- **MenuItem**: one dish for one restaurant, with availability and price.
- **SecurityEvent**: one security observation (kind, address, path, account, detail, whether it was blocked). Stored in `SecurityEvents`.

## Main flows

**Table booking** (`ReservationService.BookAsync`)
1. Check the rules: party size, notice period, advance window, opening hours, location open.
2. Inside a serializable transaction (SQL Server), load the overlapping blocking bookings of the restaurant.
3. Refuse if the guest already has an overlapping Confirmed or Seated booking at this restaurant.
4. Pick the best-fit table in the chosen location: the smallest active table that seats the party and is free for the whole slot.
5. Save, then send the confirmation email. A failed email is logged and does not undo the booking.

**Change or cancel a booking** (`ReservationService.UpdateBookingAsync`, `CancelAsync`)
1. Check the booking code and phone number. An unknown code and a wrong phone give the same message.
2. Refuse after the change deadline (2 hours before the start), with a message to call the restaurant.
3. For a new party size, pick a table again in the same location, in a serializable transaction.
4. Save, then send the cancellation email when cancelling.

**Food order** (`FoodOrderService.PlaceOrderAsync`)
1. Find the booking by code and check the phone number.
2. Check that the booking still accepts food.
3. Merge duplicate lines, load the items for this restaurant, and build the order.
4. Save the order in one transaction.

**Payment** (`PaymentService`)
1. `/{slug}/Checkout/{id}` creates a Cashfree session for one order.
2. `/Payment/Result` confirms the status with Cashfree. `/Payment/Webhook` checks the signature and is exempt from the antiforgery check.
3. A paid payment marks the order paid and sends a receipt email. Refunds send a refund email.

**Staff work** (`StaffWorkService`, `ReservationService`, `FoodOrderService`)
- Staff choose a location (stored in a cookie that is checked against their restaurant on every request).
- Seat, complete and no-show change a booking's status. Serve and cancel change an order.
- Every action first checks that the booking or order is at that location, so a staff member cannot change another location's records.

## Security

**Sign-in and sessions**
- Cookie authentication (`DineBook.Auth`), 7-day expiry with sliding renewal. Sign-in at `/Login`, or `/{slug}/Login` for a restaurant.
- Passwords are stored as hashes. Failed sign-ins give the same message for an unknown email and a wrong password.
- Brute-force protection (`SignInPolicy`): 5 failed sign-ins for one account in 15 minutes pauses that account; 20 failures from one address in 15 minutes pauses sign-in from that address. Each pause is logged.
- Each login POST is also rate limited per address (`auth` policy: 20 per minute).
- An admin can unblock an address. Its risk and sign-in counting then starts again from that moment.

**Forms and requests**
- Antiforgery cookie `DineBook.Antiforgery`; the header name is `X-CSRF-TOKEN`. Rejected form posts are logged.
- Rate limit `checkout` on POSTs that change bookings or payments: 30 per minute per signed-in user, or per address when signed out.
- Honeypot: a hidden field `website_url` on the sign-in, registration, profile, checkout, booking and booking-details forms. A filled-in field means a form-filling bot, and the request is refused.
- Security middleware runs first on every request:
  - refuses addresses at Critical risk (403);
  - refuses known scanner tools (403);
  - answers probes of paths this site does not use, such as `/wp-admin` and `/.env`, with 404;
  - refuses SQL injection and script-injection patterns in the query string and small form posts (400). Only field names are logged, not the payload;
  - logs scripted clients and any address above 180 requests in one minute. These are logged, not refused.
- Razor encodes output and the data layer uses parameters. The code has no raw SQL and no `Html.Raw`.

**Risk scoring** (`RiskScoring`)
- Points per event: SQL injection 40, script injection 40, scanner 30, honeypot 20, CSRF rejection 10, failed or locked sign-in 2, scripted client or high request rate 5.
- The score is the total for one address over the last hour. Medium from 30, High from 60, Critical from 80. Critical addresses are refused.
- Scores are cached for 30 seconds per server.

**Headers and hosting**
- Security headers on every response: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, a strict Referrer-Policy and Permissions-Policy, `Cross-Origin-Opener-Policy: same-origin`.
- Content-Security-Policy is enforced. Scripts come from the site and the Bootstrap and Cashfree hosts. Inline styles are allowed.
- Data Protection keys go to `DataProtection:KeysPath` when set, so cookies work across servers.
- Forwarded client addresses are used only when `Proxy:TrustForwardedHeaders` is `true`.

**Duplicates and money**
- Serializable transactions protect the table check and the duplicate-booking check.
- The payment webhook checks its signature. Duplicate Cashfree notifications are safe.

**Logs and retention**
- `SecurityEvents` keeps 30 days. A background job deletes older events every six hours.
- Emails never log the recipient address. Email errors are logged with the subject only.

## Authorization

- Folder rules (`Startup`): `/Admin` needs the Admin role (`AdminOnly`); `/Manager` needs Manager (`ManagerOnly`);
  `/Staff` needs Staff or Manager (`StaffArea`); `/Customer` needs Customer (`CustomerOnly`).
- Manager pages take the restaurant from the signed-in account, so a manager changes only their own restaurant.
- Staff and managers work at one restaurant; the location they chose is checked on every request.
- `AdminAccountSeeder` creates the first admin from `Admin:Email` and `Admin:Password` when no admin exists.
- Admin, staff and manager pages list rows in pages (20 per page by default, up to 100).

## Configuration

- `Dining` section: time zone, max party size, minimum notice, advance days, default duration, slot length.
- `Email` section: `Enabled`, `SenderEmail`, `SenderName`. `Email:ApiKey` in user-secrets.
- `Cashfree` section: `AppId`, `SecretKey` (user-secrets), `Environment`, `ApiVersion`.
- `Site:PublicBaseUrl`: base address for the payment return URL.
- `DataProtection:KeysPath`, `Proxy:TrustForwardedHeaders`: see Security.

## Not built yet

- Machine-learning or anomaly detection for security. Detection is rule-based.
- CAPTCHA (left out on request).
- Alert emails for security events.
- Confirmation emails for food orders, and reminder emails before a booking.
