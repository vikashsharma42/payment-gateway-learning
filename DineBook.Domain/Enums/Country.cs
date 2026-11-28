namespace DineBook.Domain.Enums
{
    // Countries customers can register from. Each name is its ISO 3166-1 alpha-2 code, stored as text in ApplicationUser.CountryCode.
    // The phone rules for each country are in CountryRules.
    public enum Country
    {
        IN, US, CA, GB, AE, SA, QA, OM, KW, SG, MY, TH,
        ID, PH, JP, NP, BD, LK, PK, AU, DE, FR, IT, ES
    }
}
