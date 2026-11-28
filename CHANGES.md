# Changes

> Entries below are a history of each change. Where a later entry changes an earlier one, the later entry applies. The current behaviour is in [README.md](README.md) and [architecture.md](architecture.md).

## Customer country and phone format (latest)

- **Country on every customer account:** `ApplicationUser.CountryCode` (ISO alpha-2, for example `IN`). Registration asks for the country first. The country sets the flag, the dial code, the number of digits and the grouping.
- **Stored phone:** the dial code plus the national number, digits only (`919876543210`). Lookups use the same form, so a booking phone matches whether it was typed as `98765 43210` or `+91 98765 43210`.
- **Phone field (all customer forms):** a country dropdown with flags, then the national number. `wwwroot/js/phone-field.js` (custom, self-hosted, no CSP change) groups the digits as the country writes them and changes the placeholder when the country changes. A number typed with `+` is left as typed and checked on the server.
- **Countries:** the `Country` enum (`Domain/Enums/Country.cs`, 24 countries) is the country type everywhere. Its name is the ISO code, so the database column holds the same values as before. Phone rules for each country (dial code, digit length, grouping, example) are in `Domain/Enums/CountryRules.cs`. To add a country, add a value to the enum and a rule in `Rules()`. `PhoneNumbers.Resolve` turns what was typed into the stored form and rejects a number of the wrong length for its country.
- **Display:** `Helpers/PhoneDisplay.cs` shows `🇮🇳 +91 98765 43210` (flag from the ISO code). Used on staff and manager booking lists.
- **Replaces** the India-only helper from the entry below (`IndianPhone` is removed).
- **Existing databases:** run `DB-Schema/customer-country-migration.sql`. It adds the column (existing accounts become `IN`) and adds `91` to 10-digit Indian customer phones. Check the review query in the script first.
- **Staff and admin accounts:** the country is set to India by default. Staff phones are not asked for on the admin form, as before.
- **Files:** `Domain/Entities/ApplicationUser.cs`, `Domain/Entities/Reservation.cs`, `Domain/Enums/Country.cs` and `CountryRules.cs` (new), `Domain/ValueObjects/PhoneNumbers.cs` (new), `Infrastructure/.../ApplicationUserConfiguration.cs`, `DB-Schema/ddd-domain-migration.sql`, `DB-Schema/customer-country-migration.sql` (new), `Application/Services/RegistrationService.cs`, `CustomerService.cs`, `AdminAccountSeeder.cs`, `Interface/IRegistrationService.cs`, `Interface/ICustomerService.cs`, `Web/Helpers/PhoneDisplay.cs` (new), `Web/Models/PhoneFieldModel.cs` (new), `Web/wwwroot/js/phone-field.js` (new), `Web/Pages/Shared/_PhoneField.cshtml` (new), `Web/Pages/Shared/_Layout.cshtml`, the customer DTOs, and the registration, profile, checkout, deposit, booking-details and staff and manager pages.
- **Not built here:** no .NET SDK in this environment. Run `dotnet build` and apply the migration before testing.
- **To check:** the phone sent to Cashfree is now the full number with the dial code (for example `919876543210`). Confirm the sandbox accepts it.

## Indian flag on phone numbers (latest)

- Customer phone numbers are shown with the Indian flag and +91, for example `🇮🇳 +91 98765 43210`. This applies to the staff and manager booking lists.
- Every customer phone box now has a `🇮🇳 +91` prefix and expects the 10-digit number (placeholder `98765 43210`). This covers registration, profile, checkout, the booking deposit page, and the change, cancel, pre-order and pay forms on the booking page. The browser autofills the national number (`autocomplete="tel-national"`).
- Storage and lookups are unchanged. Phone numbers are still saved as digits only. A number saved with 91 in front is still shown correctly, but to find a booking the phone must be typed the same way it was saved. See the open item below.
- Open item: a number saved as `91XXXXXXXXXX` does not match the same number typed as 10 digits when looking up a booking or paying. Fixing this needs one normalization rule and a one-time data update of existing `Users.Phone` values.
- Files: `Helpers/IndianPhone.cs` (new), `Pages/Restaurants/Registration.cshtml`, `Pages/Restaurants/Profile.cshtml`, `Pages/Checkout.cshtml`, `Pages/Reservations/Deposit.cshtml`, `Pages/Reservations/Details.cshtml`, `Pages/Staff/Reservations.cshtml`, `Pages/Manager/Reservations.cshtml`.
- Not built here: no .NET SDK in this environment. Run `dotnet build` before release.

## Location picker (latest)

- The restaurant header has a **Change location** button. It opens a picker that lists every location of the restaurant with its address and an Open or Closed badge, and an arrow on each open location.
- Closed locations are shown but cannot be selected. The location being viewed is marked "Viewing".
- The picker keeps the tab you are on (overview, menu or table booking) when you switch location.
- Files: `Pages/Shared/_RestaurantHeader.cshtml`, `wwwroot/css/site.css` (location picker section).
- Not included: the "Use current location" button and the delivery or pickup badges. Locations do not store coordinates or delivery settings yet.

## Table booking look (latest)

- The booking page follows the pickup-schedule design: a row of day chips for the next seven days, a scrollable list of times with a radio on each row, and a Confirm button that stays grey until a time is chosen.
- Form field names are unchanged (`Input.Date`, `Input.Guests`, `Input.StartsAt`), so the server code is unchanged.
- Files: `Pages/Restaurants/Book.cshtml`, `wwwroot/css/site.css` (booking section), `wwwroot/js/ux.js`.

## Animated pages (latest)

- Sections of every page appear one after another (a short stagger), so the main content of each page enters in steps.
- A thin progress bar at the top of the window moves while a link or form is loading, and finishes when the next page shows. It is driven by `wwwroot/js/ux.js`.
- Both effects are turned off for people who ask their device for reduced motion.

## Pagination (latest)

- **Shared paging:** `DineBook.Application/Paging` has `PagedResult<T>` (items, page, page size, total) and `ToPagedAsync()`. Each page is one count and one `Skip/Take` query in SQL Server, not a load of every row.
- **Query values:** `PageQuery` (`Page`, `PageSize`) is the base of the DTOs for paged lists. `UserSearchQuery` inherits it, so the search and the page number travel together.
- **Pager:** `Pages/Shared/_Pager.cshtml` shows Previous/Next and "Page x of y". It keeps the current search in the link. A page past the end shows the last page.
- **Pages now paged (20 rows each):**
  - Admin users list (`/Admin/Users`). This replaces the old cap of 200 users, and the sort is stable so rows never repeat or skip between pages.
  - Admin home (`/Admin`): every restaurant, open or closed.
  - Admin restaurant grid (`/Admin/Restaurants`): restaurants with an open location, as on the public directory.
  - Admin payments monitor (`/Admin/Payments`): every payment and refund, newest first.
- **Fixed:** `Pages/Admin/Restaurants/Index.cshtml.cs` was in the public `DineBook.Web.Pages` namespace, so the admin route ran the public page model. It now lives in `DineBook.Web.Pages.Admin.Restaurants`. The behaviour is unchanged.
- **Not paged yet:** staff and manager booking and order lists (bounded per location and day), customer booking, order and payment lists, and the booking page's order list. Each is small per person or per day, but a restaurant with many open orders should be paged next.

## Input DTOs and UX (latest)

- **Every form and query input has a DTO** in `DineBook.Web/Dtos`. Page models bind to one DTO per form instead of loose properties or nested classes:
  - `CredentialsInput` (sign-in), `CustomerRegistrationDto`, `CustomerProfileDto`, `CheckoutDto`, `BookingDto`, `BookingPhoneDto`, `LocationSwitchDto` (staff), `RestaurantChargesDto`, `RestaurantDetailsDto`, `NewLocationDto`, `NewTableDto`, `NewMenuItemDto`, `RestaurantCreateDto`, `StaffUserCreateDto`, `UserSearchQuery`, `PaymentResultQuery`.
  - Field names in forms changed to `Input.X`, `Rates.X` and so on. Views were updated to match.
  - The payment return page reads `order_id` through `PaymentResultQuery`, so the gateway's link still works.
- **Route values are not DTOs.** `{slug}`, `{code}` and similar values stay as properties, as do button values such as `orderId`.
- **Admin restaurant page** validates only the form that was submitted. Errors from the other forms on the same page no longer block it.
- **UX for every page** (shared layout, stylesheet and `wwwroot/js/ux.js`):
  - Every `data-submit-once` form disables its buttons after the first submit, and re-enables them when a page is restored from history.
  - Server-side field errors outline the field, and focus moves to the first one. The outline clears as the person types.
  - Touch-sized controls on touch screens, 16px inputs on phones (no zoom on iOS), consistent focus rings, and no toast animation when the person prefers reduced motion.

## Sign-in, sign-out and scale (latest)

- **One sign-in form model:** `CredentialsInput` (email and password, with the same rules) is used by every sign-in page.
- **One sign-in redirect rule:** `AppPageModel.RedirectAfterSignIn` decides where a user goes after signing in (safe return URL, admin, staff or manager, customer). Sign-in pages call it instead of repeating it.
- **One sign-out page for every role:** `/Logout` shows a confirmation ("Stay signed in" or "Sign out"), then signs out, clears the staff location cookie and shows a toast. The old `/Admin/Logout` address forwards to it, and every sign-out button now posts to `/Logout`.
- **Rate limits no longer block normal use.** The old limit was 5 POSTs per IP every 5 minutes for sign-in, sign-up, booking, cancel and checkout, so a retry after a failed payment, or several people behind one office address, were blocked. Now:
  - `auth` (sign-in and sign-up): 20 POSTs per minute per client address.
  - `checkout` (booking, orders, cancel, payment): 30 POSTs per minute per signed-in account, or per address for anonymous requests.
- **Behind a load balancer:** set `Proxy:TrustForwardedHeaders` to `true` only when the proxy overwrites `X-Forwarded-For` and `X-Forwarded-Proto`. Otherwise every visitor shares the proxy's address, and so one rate-limit bucket.
- **Several servers:** set `DataProtection:KeysPath` to a folder all servers share. Without it, a sign-in made on one server cannot be read by another (the cookie fails to decrypt).
- **Static files** (CSS, JavaScript, images) are cached in the browser for one hour.

Still open for very large traffic (not done here): distributed rate limiting (the counters are per server), a shared cache for restaurant pages, connection-pool sizing, and a database lock per restaurant for bookings. Each needs a load test against the real SQL Server.

## Header, customer links and favicon (latest)

- The site header is back in the shared layout, with links for the signed-in role (`_RoleNav.cshtml`). Staff, manager and admin links are as before.
- Customers see **Orders** (`/{slug}/Orders`), **Profile** (`/{slug}/Profile`) and **Sign out**. **My dashboard** links to `/Customer/Dashboard`.
- The restaurant for those links comes from the address. Visiting `/{slug}/...` remembers the restaurant in the `DineBook.Restaurant` cookie (30 days), so the header and "Edit profile" still work on pages without a slug. Not-found pages do not set it.
- Profile moved from `/Customer/Profile` to `/{slug}/Profile`. It needs a signed-in customer, like the orders page.
- Sign out is in the header for every signed-in user. Customers are sent back to their restaurant. Other roles go to `/Login` as before.
- `customer` and `profile` are reserved slugs, so a restaurant or location cannot take them. Existing restaurants with those slugs are not changed.
- A favicon (`wwwroot/favicon.svg` and `wwwroot/favicon.ico`) is linked from the shared layout, so it shows on every page.

## Restaurant dashboard by slug (latest)

Restaurants are reached only by their slug. The old restaurant list at `/` is gone.

- `/` is not found. Use `/{slug}`, for example `/spice-garden`.
- `/{slug}` is the restaurant dashboard, open to everyone. It shows:
  - the open locations, with a switcher (`/{slug}?location=ID`). The first open location is shown by default;
  - the menu of available dishes, grouped by category;
  - the booking form, for signed-in accounts only. Signed-out visitors see a "Sign in to book" button that returns them to the dashboard after sign-in.
- Booking (`POST /{slug}`) needs a signed-in account. A signed-out request is challenged and redirected to `/Login?ReturnUrl=/{slug}`.
- `/{slug}/Reservations/{code}` and `/{slug}/Checkout/{id}` open only under the slug of their own restaurant. Any other slug gives "not found".
- Sign-in and registration are under the restaurant's slug: `/{slug}/Login` and `/{slug}/Registration`. The restaurant comes from the address, so there is no restaurant select.
- `/Login` is for administrators only.
- A signed-in user who opens a sign-in page is sent on: customers to the restaurant, staff and managers to their dashboard, administrators to theirs.
- The customer's food orders are at `/{slug}/Orders` (only that restaurant's orders). The old `/Customer/Orders` page is removed.
- The site header (and its navigation and sign-out link) is removed from the shared layout.
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

## Customer booking changes (party size, special requests, 2-hour cutoff)

- Customers can now change the **party size** and **special requests** of a confirmed booking from the booking page ("Change your booking" card). Enter the phone number used for the booking, then save.
- When the party size changes, the booking moves to a free table in the same location that fits the new size. If none is free, the guest sees a friendly message to call the restaurant, and nothing changes.
- Changes and cancellations close **2 hours before the start time** (`ReservationRules.CustomerChangeCutoff`). Until then the page shows "You can change or cancel this booking until <time>". After it, the form and the cancel button are hidden and a note explains to call the restaurant. The domain rule is enforced on the server too, so the cutoff cannot be bypassed.
- The phone number check is the same as for cancelling: an unknown code and a wrong phone give the same message.
- Not changed: the time, date and table cannot be edited by the customer (cancel and book again). No email is sent for a change yet.
- Files: `Domain/Entities/Reservation.cs` (`ChangeDeadline`, `CanCustomerChange`, `CancelByCustomer`, `ChangeParty`, `ChangeSpecialRequests`), `Domain/Entities/ReservationRules.cs`, `Application/Services/ReservationService.cs` (`UpdateBookingAsync`, `CancelAsync` uses the cutoff), `Application/Services/Interface/IReservationService.cs`, `Web/Dtos/BookingChangeDto.cs` (new), `Web/Pages/Reservations/Details.cshtml(.cs)`.

## Friendlier customer wording

- Booking, changes, cancellation, pre-order and bill pages now use plain, warm sentences. Examples: "Choose a day", "Show available times", "Confirm booking", "Show my orders", "Save changes".
- Customer messages were reworded to say what happened and what to do next, for example "Sorry, no table is free at that time for your party size. Please try another time." and "We couldn't find a booking with that code and phone number. Please check them and try again."
- Sign-in, sign-out, registration, profile and checkout text was simplified in the same way. Staff-only pages keep their original wording.

## Staff, manager and admin review

- **Bug fixed:** after adding a user on the admin Users page, the list lost its search text and page number. The search and page now stay the same after a successful add and when the form shows an error.
- **Friendlier text:** empty-state messages now say what to do next (for example "No dishes yet. Add your first dish above." and "No tables yet. Add one so customers can book this location."). Error toasts start with "Please check ...". Bill-charge and location descriptions are clearer.
- Staff and manager pages keep their existing behaviour. Authorisation was re-checked: the Admin, Manager, Staff and Customer folders are protected by folder rules, and manager pages check for a missing restaurant before showing data.
- Not changed: the staff and manager lists are still not paged. Those lists are capped at the most recent rows.

## Security: attack detection, brute-force protection, duplicate-booking check, security log and risk scoring

**Run this first:** `DineBook.Infrastructure/Database/DB-Schema/security-events.sql` creates the `SecurityEvents` table. The app now needs it; it is the only schema change.

- **SQL injection and XSS detection (`ThreatDetector`, `SecurityMiddleware`):** the query string and small form posts are checked for common attack patterns. A match is logged and the request is refused with 400. Only the field name is logged, not the payload. The real protection is still parameterised EF Core queries and Razor's output encoding. The scan found no raw SQL and no `Html.Raw`.
- **Brute-force protection (`Login`, `SignInPolicy`):** (failed sign-ins now add 2 risk points, not 5, see the follow-up entry) 5 failed sign-ins for one account in 15 minutes pause that account. 20 failures from one address in 15 minutes pause that address. Each block is logged. The existing per-address rate limit still applies.
- **Duplicate bookings (`ReservationService.BookAsync`):** a guest cannot book a time that overlaps one of their own confirmed or seated bookings at this restaurant. The check runs inside the serializable transaction.
- **CSRF:** Razor Pages already validates antiforgery tokens on form posts. This release adds a strict `DineBook.Antiforgery` cookie with an `X-CSRF-TOKEN` header name, and logs rejected posts (400 responses). The payment webhook stays exempt.
- **Bot and scanner detection:** requests from known scanner tools (sqlmap, nikto, nmap and others) are refused. Requests to common probe paths (`/wp-admin`, `/.env`, `/.git` and similar) get 404 and are logged. Scripted clients are logged on form posts but not refused.
- **Security event log (`/Admin/Monitoring`, admin only):** 24-hour counts, the highest-risk addresses in the last hour, and a paged list of events with the address, request, account, detail and whether each was blocked.
- **Risk scoring (`RiskScoring`):** each event adds points to its address (SQL injection and XSS 40, scanner 30, honeypot 20, CSRF rejection 10, failed sign-in, lockout or scripted post 5). The score is the total over the last hour. Medium from 30, High from 60, Critical from 80. Requests from a Critical address are refused for as long as the score stays at 80 or more. The score is cached for 30 seconds per server.
- **Failure policy:** if the monitor itself fails, the error is logged and the request continues. Attacks may then get through to the normal defences.
- **Not included:** AI or machine-learning anomaly detection (this is rule-based), CAPTCHA challenges, an unblock button, log retention cleanup, alert emails, and enforcing the Content-Security-Policy (it is still report-only). The risk cache is per server, so a shared load balancer will apply limits slightly differently.
- **Note:** the older `DineBook.Database` project uses plural table names that don't match the EF mappings. The `DB-Schema` scripts are the ones the code uses.
- **Files:** Domain: `SecurityEvent.cs`, `SecurityEventKind.cs`, `Security/RiskScoring.cs`. Infrastructure: `SecurityEventConfiguration.cs`, `ApplicationDbContext.cs`, `DB-Schema/security-events.sql`. Application: `ISecurityService.cs`, `SecurityService.cs`, `ReservationService.cs`, DI registration. Web: `ClientInfo.cs`, `ThreatDetector.cs`, `SecurityMiddleware.cs`, `Startup.cs`, `Pages/Login.cshtml.cs`, `Pages/Admin/Monitoring/`, `_RoleNav.cshtml`.
- **Paging:** the staff and manager bookings and orders lists are now paged, with the current page kept across seat, complete, serve and cancel actions.

## Paged staff and manager lists

- Staff bookings and food orders, and the manager's bookings and food orders, now load one page at a time (20 rows by default, up to 100), with the page controls at the bottom.
- Seat, Complete, No-show, Mark served and Cancel return to the page the user was on. The page number is carried in hidden form fields, so the list no longer jumps back to page 1.
- Page models and views agree on the paged result types, so the pages build.
- Not paged: the menu and table lists. Those are small per restaurant.

## Security follow-up: unblock, lower sign-in weight, log clean-up

- **Unblock (`/Admin/Monitoring`):** each risky address has an Unblock button. It records an `AddressUnblocked` event with the administrator's name. From then on, that address's risk score and sign-in failures count only events after the unblock. Other addresses are not affected. The risk score is cached for up to 30 seconds per server, so a refusal can last that long after unblocking.
- **Sign-in failures weigh less:** each failed or locked sign-in now adds 2 risk points, not 5. Twenty failures from one address in an hour are now Medium, not Critical, so a shared Wi-Fi or mobile address is not refused because of mistyped passwords. The per-account lock (5 failures in 15 minutes) and the per-address sign-in pause (20 in 15 minutes) still apply.
- **Log retention:** `SecurityLogCleanupService` deletes security events older than 30 days, checked every six hours. It only runs in the web app and keeps retrying after an error.
- **Files:** `SecurityEventKind.cs`, `RiskScoring.cs`, `ISecurityService.cs`, `SecurityService.cs`, `Admin/Monitoring/Index.cshtml(.cs)`, `Infrastructure/SecurityLogCleanupService.cs` (new), `Startup.cs`.
- **Still not done, and why:**
  - CAPTCHA needs a provider account and keys.
  - Machine-learning anomaly detection needs labelled traffic data. Detection stays rule-based.
  - Enforcing the Content-Security-Policy needs a browser test of every page, because it could break scripts.
  - Alert emails need an administrator address and an email rule before they can be sent.

## Bot detection, request-rate monitoring and enforced Content-Security-Policy

- **Bot trap (honeypot):** a hidden field named `website_url` is on the sign-in, registration, profile, checkout, booking and booking-details forms. People never see or fill it. A form post with that field filled in is refused with 400, and logged as a honeypot event (20 risk points). Screen readers skip it (`aria-hidden`).
- **Request-rate monitoring:** an address that sends more than 180 requests in one minute is logged once for that minute as an automated client (5 risk points). It is logged, not refused. The limit is a fixed threshold. This is not machine learning.
- **Content-Security-Policy is enforced**, not report-only. The one inline script (admin Users page) moved to `wwwroot/js/create-user-modal.js`. Every external script host in the pages is allowed in the policy.
- **Not added, as requested:** CAPTCHA.
- **Not built:** machine-learning or anomaly detection (it needs labelled traffic data), and alert emails.
- **Check in a browser before release:** enforcing the policy could block something not covered by the test. Check checkout (the Cashfree payment SDK loads its own frames and requests), the booking slot picker, the admin Users popup, and sign-in.
- **Files:** `SecurityMiddleware.cs`, `ThreatDetector.cs`, `SecurityHeaders.cs`, `wwwroot/js/create-user-modal.js` (new), and the trap added to `_SignInForm`, `Registration`, `Profile`, `Checkout`, `Book` and `Details` views. `Admin/Users/Index.cshtml` now loads the script from a file.

## Removed the unused JWT packages

- Sign-in uses cookies, so the JWT packages were no longer used. Removed `System.IdentityModel.Tokens.Jwt` (DineBook.Application) and `Microsoft.AspNetCore.Authentication.JwtBearer` (DineBook.Web).
- No code used either package. Two comments that mentioned JWT were updated.
- Check with `dotnet build` after restoring packages.

## Confirmation popups instead of a page

- **Sign out:** every Sign out button (top bar, admin dashboard and admin home) opens a popup: "Logout", "Are you sure you want to logout?", with Cancel and Log out. The `/Logout` page remains as the fallback for direct links.
- **Cancel booking:** the booking page asks for the phone number in a confirmation popup, instead of a card on the page. The cancel rules are unchanged.
- Other confirmations were already popups (the admin add-user form and the location picker). The other forms on the page stay as they are.
- Files: `Pages/Shared/_LogoutModal.cshtml` (new), `_Layout.cshtml`, `_RoleNav.cshtml`, `Admin/Index.cshtml`, `Admin/Dashboard.cshtml`, `Reservations/Details.cshtml`, `wwwroot/css/site.css`.

## Web review: payment return address and old sign-out form

- **Bug fixed: payment return page.** Checkout sends Cashfree back to `/Payment/Result`, but the page lived in the `Payments` folder, so the return URL returned 404 after payment. The pages now declare `/Payment/Result` and `/Payment/Webhook`, matching the README and the webhook address. The "Check again" link uses the page's file path (`/Payments/Result`).
- **Bug fixed: old sign-out form.** A POST to `/Admin/Logout` redirected with GET, so sign-out never ran. It now uses `RedirectToPagePreserveMethod("/Logout")` (a 307 redirect, which keeps the POST), so the real sign-out runs.
- `/Admin/Logout` is marked `[AllowAnonymous]`, so the redirect works for any caller, including a session the admin folder rule would otherwise block. The real `/Logout` handles the sign-out.
- Checked by script: every `asp-page-handler` has a matching handler in its page model. Field bindings and `Model.` references were not re-checked in this pass.

## Architecture: Web no longer reads the database directly

- The web layer now reaches the database only through Application services. Pages and background jobs no longer take `ApplicationDbContext`.
- **Checkout:** the order lookup moved to `IPaymentService.GetOrderForCustomerAsync`. The rule that an order must belong to this restaurant and this customer is now in the service, not the page. The account pre-fill uses `ICustomerService.FindProfileAsync`.
- **Payment return page:** the booking links come from `IPaymentService.GetBookingLinksAsync`.
- **Security log clean-up:** the retention delete moved to `ISecurityService.DeleteEventsOlderThanAsync`. The background job now uses the service instead of the DbContext.
- **Naming fix:** `Services/WebAuthenticationService .cs` (a stray space before `.cs`) is renamed to `WebAuthenticationService.cs`.
- Still open: Application services still use `ApplicationDbContext` and EF Core types (13 files), and 33 `SaveChangesAsync` calls are not yet grouped into a unit of work. That is the next step (repositories and unit of work).
- Startup still uses the DbContext for database setup and seeding. That is the composition root, so it is expected.
- Not compiled here: no .NET SDK. Each changed file parses cleanly. Run `dotnet build` before use.

## Architecture (Option C, first slice): domain events, specifications

- **Domain events.** `Entity` can hold domain events (`AddDomainEvent`, `DomainEvents`, `ClearDomainEvents`). The events are not stored in the database. `IDomainEvent` has an `OccurredAt` time.
- **First event: `PaymentPaidEvent`.** `Payment.MarkAsPaid` raises it. The receipt email is no longer sent inside `PaymentService`. It is sent by `PaymentPaidEmailHandler`, after the payment is saved.
- **Dispatch after save.** `PaymentService.MarkPaidAsync` saves first, then calls `IDomainEventDispatcher`, then clears the events. A failing handler is logged and skipped, so a failed email cannot undo a payment. Event types are listed explicitly in the dispatcher (no reflection).
- **Specifications.** `ISpecification<T>` lives in the Domain layer. `PaymentByGatewayOrderIdSpecification` replaces the repeated gateway-id lambda in `PaymentService` (four places). `Matching()` applies a specification to an EF query in Infrastructure.
- Not done yet (remaining Option C work): repositories and a unit of work, moving EF out of the other Application services (13 files), specifications for the reservation and order queries, domain events for security logging and reservation changes, and the read model for the staff and manager lists.
- Not compiled. The project was not built, per your instruction. Syntax was checked by parsing only. Run `dotnet build` and fix any compile errors first.
