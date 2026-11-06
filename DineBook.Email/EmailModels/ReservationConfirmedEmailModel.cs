using System;
using System.Globalization;

namespace DineBook.Email.EmailModels
{
    public sealed class ReservationConfirmedEmailModel : BaseEmailModel
    {
        public ReservationConfirmedEmailModel(
            string toAddress,
            string customerName,
            string restaurantName,
            string reservationCode,
            DateTime startsAt,
            int guests)
        {
            ToAddress = toAddress;
            Subject = $"Your table at {restaurantName} is booked ({reservationCode})";
            CustomerName = customerName;
            RestaurantName = restaurantName;
            ReservationCode = reservationCode;
            When = startsAt.ToString("dddd, d MMMM yyyy 'at' HH:mm", CultureInfo.InvariantCulture);
            Guests = guests;
        }

        public string CustomerName { get; }

        public string RestaurantName { get; }

        public string ReservationCode { get; }

        public string When { get; }

        public int Guests { get; }
    }
}
