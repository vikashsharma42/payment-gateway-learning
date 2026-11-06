# DineBook - Architecture

DineBook lets a customer reserve a table at a restaurant and, for that booking, order food:
many dishes in one order, placed in one action.

## Bounded contexts

| Context      | Domain folder (DineBook.Domain/Entities)  | Owns                                                        |
|--------------|-------------------------------------------|-------------------------------------------------------------|
| Identity     | Identity (`DineBook.Domain/Identity`)     | ApplicationUser (sign-in account, role, active flag)        |
| Organization | Organization                              | Restaurant (status, tables, menu categories, dishes)        |
| Dining       | Dining                                    | RestaurantTable, Reservation, ReservationPolicy, ReservationRules |
| Menu         | Menu                                      | MenuCategory, MenuItem                                      |
| Ordering     | Ordering                                  | Orders (aggregate root), OrderItems                         |
| Payments     | Payments                                  | Bill (aggregate root), Payment (gateway attempt for a bill) |
| Customers    | Customers                                 | Customer (guest, matched by phone)                          |
| Email        | DineBook.Email                            | Razor email templates, Brevo sender                         |
| Shared       | DineBook.Common                           | ValueObject base, IDomainEvent                              |

DineBook.Domain also holds `Abstractions` (Entity, AggregateRoot, IAuditableEntity), `Enums`,
`Exceptions` (DomainException), `Validation` (DomainValidation) and `ValueObjects` (Money, Address).

Entities follow one style: public constructor that checks its input with `DomainValidation<T>.Ensure(...)` (all rules live in that one class, e.g. `IsNotNullOrWhiteSpace`, `HasMaxLength`, `IsEmail`, `IsPhoneNumber`, `IsBetween`, `IsEqualTo`, `IsTrue`; entities never throw their own exceptions),
private setters, a virtual navigation property next to each foreign key (`[ForeignKey]`), and children held in a
private `List<T>` exposed as `IReadOnlyCollection<T>`, added through the parent (`customer.AddReservation`,
`restaurant.AddTable`, `order.AddOrderItem`). Every entity implements `IAuditableEntity`
(CreatedDateTime, CreatedBy, UpdatedDateTime, UpdatedBy); `ApplicationDbContext` fills them on save, using
`ICurrentUserProvider` (Web registers one that reads the signed-in email) or "system" when none is registered. `UserRefreshToken` (a hashed refresh token) is a persistence record, so it
lives in DineBook.Infrastructure/Database/Security, not in the Domain.

Layers, outside in: Web / API -> Application (use cases) -> Domain (rules) and Infrastructure (EF Core).
Domain has no framework or database references.

## Aggregates

- **Reservation** (root): one booking, identified by an 8-character code. Confirmed, Seated, Completed, Cancelled, or NoShow.
  It holds a table for `DurationMinutes`. Only Confirmed and Seated bookings block a table.
- **Bill** (root): what a booking owes for its non-cancelled orders. The amount is fixed when the bill is created.
  A `Payment` is one Cashfree attempt to pay a bill; a paid payment marks the bill paid.
- **MenuItem** (root): one dish for one restaurant, with availability and price.
- **Orders** (root): all dishes ordered for one reservation at one time.
  - `Orders.AddOrderItem(menuItem, quantity)` adds one dish; `Reservation.AddFoodOrder` attaches the order to the booking.
  - Rules: at least one item, at most 30 different items, 1 to 20 units per item, each item at most once
    (the service merges duplicates first), every item available and on the same restaurant's menu.
  - Name and price are copied into each `OrderItems`, so later menu edits do not change past orders.

## Main flows

**Table booking** (`ReservationService.BookAsync`)
1. Check the rules: party size, notice period, advance window, opening hours.
2. Inside a serializable transaction (SQL Server), load overlapping blocking bookings.
3. Pick the best-fit table: the smallest active table that seats the party and is free for the whole slot.
4. Save, then send the confirmation email. Email failure is logged and does not undo the booking.

**Food order** (`FoodOrderService.PlaceOrderAsync`)
1. Find the booking by code and check the phone number. Unknown code and wrong phone give the same message.
2. Check that the booking still accepts food (Confirmed or Seated, before it ends).
3. Merge duplicate lines, load the items for this restaurant only, and build the order with `new Orders(...)` and `AddOrderItem`.
4. Save the order in one transaction.

**Cancel booking**: needs the code and the phone number. Only Confirmed bookings can be cancelled.

## Security decisions

- The booking code alone cannot change a booking. Cancel and order both need the phone number.
- Anti-forgery tokens on every form; rate limit (`checkout` policy) on POSTs that change data.
- Emails never log the recipient address. Email errors are logged with the subject only.
- Cashfree secrets and the Brevo API key live in user-secrets, not in appsettings.

## Admin

- Routes: `/Admin/Login`, `/Admin` (restaurant list), `/Admin/Restaurants/New`, `/Admin/Restaurants/{id}` (tables and menu).
- Login: email and password (`/Admin/Login`). Passwords are stored only as PBKDF2 hashes (`PasswordHasher`).
  A successful login issues a JWT access token (15 minutes) and a refresh token (7 days). Both travel in
  HttpOnly, Secure, SameSite=Strict cookies (`dinebook.access`, `dinebook.refresh`), never in page scripts.
  Login is rate limited per IP, and the error message is the same for an unknown email, a wrong password and
  a deactivated account.
- JWT bearer (`JwtBearerSetup`): reads the access cookie; when it has expired, the refresh token is rotated
  and new cookies are set. A token is rejected when its `jti` is blacklisted (sign-out) or the user is no
  longer active (`IUserActiveCache`). Not signed in: redirect to `/Admin/Login?returnUrl=...`. Wrong role:
  redirect to `/Admin/Login?denied=1`.
- Sign-out (`/Admin/Logout`, POST) revokes the user's refresh tokens, blacklists the access token and clears the cookies.
- Authorization: every page under `/Admin` requires the `AdminOnly` policy (role `Admin`), except sign-in and sign-out.
- First admin: when no admin exists, `AdminAccountSeeder` creates one from `Admin:Email` and `Admin:Password` (user-secrets).
- Use cases live in `RestaurantAdminService` (Application), not in the pages. The domain rules
  (duplicate table labels, capacity, price, opening hours) apply to admin input as they do everywhere.
- The app will not start without `Jwt:Key` (at least 32 characters, in user-secrets). `Admin:Password`, when set, must be at least 12 characters.

## Configuration

- `Dining` section: time zone, max party size, minimum notice, advance days, default duration, slot length.
- `Email` section: `Enabled`, `SenderEmail`, `SenderName`. `ApiKey` in user-secrets.
- `Cashfree` and `Site` sections for the legacy payment flow.

## Not built yet

- Payment or deposit for food orders (orders are pay-at-table for now).
- Kitchen or staff screens (seat, serve, no-show). The domain methods exist, but no UI calls them.
- Email confirmation for food orders. Reminder emails before a booking.
