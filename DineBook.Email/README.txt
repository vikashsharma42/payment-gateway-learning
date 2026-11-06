DineBook.Email - setup

1. Add this to DineBook.Application/DineBook.Application.csproj (inside an ItemGroup):

     <ProjectReference Include="..\DineBook.Email\DineBook.Email.csproj" />

2. Add the project to DineBook.slnx.

3. Turn email on (appsettings.json, or an environment-specific file):

     "Email": { "Enabled": true, "SenderEmail": "verified@yourdomain.com", "SenderName": "DineBook" }

   The sender address must be verified in your Brevo account first.

4. Store the Brevo API key in user-secrets, never in appsettings.json:

     dotnet user-secrets set "Email:ApiKey" "xkeysib-..." --project DineBook.API
     dotnet user-secrets set "Email:ApiKey" "xkeysib-..." --project DineBook.Web

5. Admin refund key, also in user-secrets. Use a long random value:

     dotnet user-secrets set "Admin:ApiKey" "<long random string>" --project DineBook.API

With Email:Enabled=false, nothing is sent and no network call is made.
With Enabled=true and a missing ApiKey or SenderEmail, the app refuses to start.
