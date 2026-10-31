ProductShop.Email - setup

1. Add this to ProductShop.Application/ProductShop.Application.csproj (inside an ItemGroup):

     <ProjectReference Include="..\ProductShop.Email\ProductShop.Email.csproj" />

2. Add the project to ProductShop.slnx.

3. Turn email on (appsettings.json, or an environment-specific file):

     "Email": { "Enabled": true, "SenderEmail": "verified@yourdomain.com", "SenderName": "ProductShop" }

   The sender address must be verified in your Brevo account first.

4. Store the Brevo API key in user-secrets, never in appsettings.json:

     dotnet user-secrets set "Email:ApiKey" "xkeysib-..." --project ProductShop.API
     dotnet user-secrets set "Email:ApiKey" "xkeysib-..." --project ProductShop.Web

5. Admin refund key, also in user-secrets. Use a long random value:

     dotnet user-secrets set "Admin:ApiKey" "<long random string>" --project ProductShop.API

With Email:Enabled=false, nothing is sent and no network call is made.
With Enabled=true and a missing ApiKey or SenderEmail, the app refuses to start.
