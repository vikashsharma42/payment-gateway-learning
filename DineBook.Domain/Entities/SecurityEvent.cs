using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Security;
using System;

namespace DineBook.Domain.Entities
{
    // One thing the security monitor noticed: a failed sign-in, a blocked attack or a probe.
    // Used for the admin log and for each address's risk score.
    public class SecurityEvent : Entity
    {
        // Required by EF Core
        private SecurityEvent()
        {
        }

        public SecurityEvent(
            SecurityEventKind kind,
            string ipAddress,
            string userAgent,
            string method,
            string path,
            string subject,
            string detail,
            bool blocked,
            DateTimeOffset occurredAt)
        {
            Kind = kind;
            Points = RiskScoring.PointsFor(kind);
            IpAddress = Clip(ipAddress, 45, "unknown");
            UserAgent = Clip(userAgent, 300, string.Empty);
            Method = Clip(method, 10, string.Empty);
            Path = Clip(path, 300, string.Empty);
            Subject = Clip(subject, 254, string.Empty);
            Detail = Clip(detail, 500, string.Empty);
            Blocked = blocked;
            OccurredAt = occurredAt;
        }

        public SecurityEventKind Kind { get; private set; }

        public int Points { get; private set; }

        public string IpAddress { get; private set; }

        public string UserAgent { get; private set; }

        public string Method { get; private set; }

        public string Path { get; private set; }

        // The account the event is about (an email for sign-in events), lower case
        public string Subject { get; private set; }

        public string Detail { get; private set; }

        public bool Blocked { get; private set; }

        public DateTimeOffset OccurredAt { get; private set; }

        private static string Clip(string value, int max, string fallback) =>
            string.IsNullOrEmpty(value) ? fallback : value.Length <= max ? value : value[..max];
    }
}
