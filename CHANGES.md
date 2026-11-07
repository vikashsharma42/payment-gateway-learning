# Changes: booking, payment, order history and dish photos

## Before running
1. Run the database script once on the database used by DineBook.Web:
   `DineBook.Infrastructure/Database/Scripts/001_dish_images.sql`
   It adds `ImageUrl` to `MenuItem` and `OrderItem`, fills in the sample dish photos, and copies them into existing order items. It is safe to run more than once. EF migrations are not used.
2. Run `dotnet build` and fix any compile errors. This code was written without a compile step in the environment where it was made.

## Fixes
- Registration: the account was saved and then the request crashed (`NotImplementedException`). Registration now completes, and the new account is signed in with the same cookie as Login.
- Registration password: the form now requires at least 12 characters (it said 12 but allowed 9).
- Sign out: the header and admin Sign out button both use `/Logout`, which ends the site cookie `DineBook.Auth`. Before, the header's sign out only cleared JWT cookies, so the site cookie stayed and the user stayed signed in.
- Checkout: a bill can only be opened by the visitor who owns its booking. Other visitors get "not found".

## Booking and payment
- Each booking has its own food orders and payments.
- `/Bookings` lists every booking made with the account phone, or on this browser.
- The booking and registration forms are prefilled from the account, or from the last booking on this browser.
- Opening a booking made by someone else asks for the phone number once. The browser remembers it. Those details are not saved as the visitor's.
- Checkout uses the booking's email and phone. Nobody types them again.
- The booking details page has the food, a pay button per food order, add food and cancel. The owner does not enter the phone number.
- Guest memory is an encrypted, signed cookie (`dinebook.guest`), made with ASP.NET Data Protection.

## Order history and photos
- `/Orders` shows food orders grouped by booking, with a photo for each dish.
- `MenuItem.ImageUrl` and `OrderItem.ImageUrl`. A new order copies the photo, so the history stays correct when the menu changes.
- The seeder adds placeholder photos (picsum.photos) for sample dishes, and fills them into existing databases in development.

## Phone input
- Phone fields show the India flag and a +91 prefix (`wwwroot/img/flag-in.svg`). The value entered is still the 10-digit number.

## Known gaps
- The Cashfree webhook is not wired (no endpoint calls `HandleWebhookAsync`). Payments are marked paid only when the result page checks the status.
- Dish photos are placeholders. Set real URLs in `MenuItem.ImageUrl`. There is no admin screen for photos yet.
- The API project (`DineBook.API`) is not in this version.
- `/Admin/Login` does not issue JWT cookies. The JWT code is kept for the API.
