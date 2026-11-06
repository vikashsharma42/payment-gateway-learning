using System;
using System.Globalization;

namespace DineBook.Email.EmailModels
{
    public sealed class ReservationCancelledEmailModel : BaseEmailModel
    {
        public ReservationCancelledEmailModel(
            string toAddress,
            string customerName,
            string restaurantName,
            string reservationCode,
            DateTime startsAt)
        {
            ToAddress = toAddress;
            Subject = $"Your booking at {restaurantName} was cancelled ({reservationCode})";
            CustomerName = customerName;
            RestaurantName = restaurantName;
            ReservationCode = reservationCode;
            When = startsAt.ToString("dddd, d MMMM yyyy 'at' HH:mm", CultureInfo.InvariantCulture);
        }

        public string CustomerName { get; }

        public string RestaurantName { get; }

        public string ReservationCode { get; }

        public string When { get; }
    }
}
