# Changes

## Restaurant dashboard by slug (latest)

Restaurants are reached only by their slug. The old restaurant list at `/` is gone.

- `/` is not found. Use `/{slug}`, for example `/spice-garden`.
- `/{slug}` is the restaurant dashboard, open to everyone. It shows:
  - the open locations, with a switcher (`/{slug}?location=ID`). The first open location is shown by default;
  - the menu of available dishes, grouped by category;
  - the booking form, for signed-in accounts only. Signed-out visitors see a "Sign in to book" button that returns them to the dashboard after sign-in.
- Booking (`POST /{slug}`) needs a signed-in account. A signed-out request is challenged and redirected to `/Login?ReturnUrl=/{slug}`.
- `/{slug}/Reservations/{code}` and `/{slug}/Checkout/{id}` open only under the slug of their own restaurant. Any other slug gives "not found".
- Sign-in and registration stay at `/Login` and `/Registration`, not under a slug. Registration asks for the restaurant and then opens its dashboard.
- Signed-in customers who sign in without a return address stay on `/Login` with a hint to open a restaurant's link.
- After payment, `/Payment/Result` links back to the booking.
- Reserved slugs now also include `staff` and `restaurants`, so a restaurant named "Staff" gets `staff-restaurant`.

Known gaps are listed in [README.md](README.md) section 8.

## Restaurant locations

- A restaurant can have several locations (branches). Each location has a name, street, city, state, country, zip code and optional phone. The address is no longer on `Restaurant`.
- Tables belong to a location. Table labels are unique per location.
- Staff and managers belong to one restaurant (`ApplicationUser.RestaurantId`). Admins and customers do not.
- After sign-in, staff go to `/Staff`. They pick one of the restaurant's open locations in the switcher, and see that location's tables. The choice is stored in the `DineBook.Location` cookie and checked against the user's restaurant on every request.
- Admin: `/Admin/Restaurants/New` creates the restaurant and its first location. `/Admin/Restaurants/{id}` has a Locations section (add, close or reopen) and shows tables for the chosen location. The last open location cannot be closed.
- Admin: `/Admin/Users` asks for a restaurant when creating Staff or Manager accounts.
- Not yet location-aware: bookings, the menu and bills are per restaurant. A booking takes any free table across the restaurant's locations.

## Restaurant web addresses

- Each restaurant has a unique slug in `Restaurant.Slug`, created from the name by `RestaurantSlug.FromName` (see README section 2 for the rules).
- The database column is in `DineBook.Infrastructure/Database/DB-Schema/ddd-domain-migration.sql`. Existing databases need the column added by hand (see README section 4).

## Dish photos and order history

- `MenuItem.ImageUrl` and `OrderItem.ImageUrl`. A new order copies the photo, so past orders keep it when the menu changes.
- The seeder adds placeholder photos (picsum.photos) for sample dishes in Development.
- Dish photos are placeholders. There is no admin screen for photos yet.
- Not in this version: the separate "My bookings" and "Order history" pages described in earlier notes. A booking is reached from the dashboard or its code.

## Error handling

- `DomainException` becomes a toast and a redirect, so the user sees the rule that failed.
- Unhandled errors are logged once and shown on the error page (`/Error/{statusCode}` outside Development).
- Known gaps: on a failed registration or admin form, typed values are not kept.
