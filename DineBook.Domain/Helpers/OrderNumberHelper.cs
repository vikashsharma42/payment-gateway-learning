using System;
using System.Security.Cryptography;

namespace DineBook.Domain.Helpers
{
    // Builds the order number shown to guests and staff, for example ORD-20261126-K7Q2MX.
    // The database has a unique index on it, so a rare clash is rejected rather than saved twice.
    public static class OrderNumberHelper
    {
        public const string Prefix = "ORD";
        public const int RandomLength = 6;
        public const int MaxLength = 20;

        // No 0/O or 1/I, so the number is easy to read aloud
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string Generate(DateTime utcNow)
        {
            var chars = new char[RandomLength];
            for (var i = 0; i < RandomLength; i++)
                chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

            return $"{Prefix}-{utcNow:yyyyMMdd}-{new string(chars)}";
        }
    }
}
