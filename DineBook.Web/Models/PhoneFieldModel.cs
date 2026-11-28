using DineBook.Domain.Enums;

namespace DineBook.Web.Models
{
    // Inputs for the shared phone field (Pages/Shared/_PhoneField.cshtml).
    // CountryName and PhoneName are the form field names, so model binding and validation errors line up with the DTO.
    public sealed record PhoneFieldModel(
        string Id,
        string CountryName,
        Country Country,
        string PhoneName,
        string Phone,
        bool Required);
}
