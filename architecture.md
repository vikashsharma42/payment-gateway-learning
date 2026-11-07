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
| Payments     | Payments                                  | Payment (gateway attempt for one food order)                |
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

Layers, outside in: Web (Razor Pages) -> Application (use cases) -> Domain (rules) and Infrastructure (EF Core). Payments is a separate project behind `IPaymentGateway`.
Domain has no framework or database references.

## Aggregates

- **Reservation** (root): one booking, identified by an 8-character code. Confirmed, Seated, Completed, Cancelled, or NoShow.
  It holds a table for `DurationMinutes`. Only Confirmed and Seated bookings block a table.
- **Orders** (food order): one food order for one booking. Its items copy name, price and photo when placed.
  A `Payment` is one Cashfree attempt to pay one food order; a paid payment marks that order paid. Each order is paid separately.
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

## Sign-in and admin

- Customers and admins sign in at `/Login` with email and password. Passwords are stored only as PBKDF2 hashes (`PasswordHasher`).
  A successful login creates the site cookie `DineBook.Auth` (HttpOnly, Secure, SameSite=Strict, 7 days, sliding).
  Admins are sent to `/Admin` after login; customers to `/`.
- Registration (`/Registration`) creates the account, links it to any guest record with the same phone, and signs in with the same cookie.
- The JWT code (`JwtSignInManager`, `JwtTokenService`, cookies `dinebook.access` and `dinebook.refresh`) is kept for the API planned
  for later. Nothing in the web app issues JWT cookies right now; sign-out only clears them.
- Sign-out (`/Logout`, POST) ends only the site cookie `DineBook.Auth`. It does not touch the JWT cookies, which are unused by the web app today.
  `/Admin/Logout` is the older JWT sign-out and is not used by any page.
- Authorization: `/Admin` pages require the `AdminOnly` policy (role `Admin`).
- First admin: when no admin exists, `AdminAccountSeeder` creates one from `Admin:Email` and `Admin:Password` (user-secrets).
  `Admin:Password`, when set, must be at least 12 characters.
- Use cases live in `RestaurantAdminService` (Application), not in the pages.

## Guest memory and booking access

- `BookingAccess` (Web) decides which bookings a visitor may see or change: bookings made with the account phone, and bookings
  this browser made or unlocked with the phone number.
- The browser memory is the cookie `dinebook.guest`: a list of booking codes and the last contact details, encrypted and signed
  with ASP.NET Data Protection. Forms are prefilled from the account first, then from this memory.
- Opening someone else's booking needs the booking phone number once. Unlocking does not save that person's details as the visitor's.

## Configuration

- `Dining` section: time zone, max party size, minimum notice, advance days, default duration, slot length.
- `Email` section: `Enabled`, `SenderEmail`, `SenderName`. `ApiKey` in user-secrets.
- `Cashfree` and `Site` sections for the legacy payment flow.

## Not built yet

- Payment or deposit for food orders (orders are pay-at-table for now).
- Kitchen or staff screens (seat, serve, no-show). The domain methods exist, but no UI calls them.
- Email confirmation for food orders. Reminder emails before a booking.


## Database changes

Schema changes are plain SQL scripts in `DineBook.Infrastructure/Database/Scripts/`, numbered in order and safe to run more than once. EF migrations are not used. When the entities change, add a script that makes the same change to the database.

- `001_dish_images.sql`: adds `ImageUrl` to `MenuItem` and `OrderItem`, and fills in sample photos.

Order items copy the dish name, price and photo when the order is placed, so past orders do not change when the menu does.
