using DineBook.Domain.Enums;
using System;

namespace DineBook.Domain.Security
{
    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    // Each event adds points to its address. The score is the total over the last hour.
    public static class RiskScoring
    {
        public const int CriticalScore = 80;

        public static readonly TimeSpan Window = TimeSpan.FromHours(1);

        public static int PointsFor(SecurityEventKind kind) => kind switch
        {
            SecurityEventKind.SqlInjectionSuspected => 40,
            SecurityEventKind.XssSuspected => 40,
            SecurityEventKind.ScannerDetected => 30,
            SecurityEventKind.HoneypotProbe => 20,
            SecurityEventKind.CsrfRejected => 10,
            // Sign-in failures weigh little: the per-account and per-address sign-in limits are the main control,
            // and a shared Wi-Fi or mobile address must not be refused because of a few mistyped passwords
            SecurityEventKind.LoginFailed => 2,
            SecurityEventKind.LoginLocked => 2,
            SecurityEventKind.AutomatedClient => 5,
            _ => 0
        };

        public static RiskLevel LevelOf(int score) => score switch
        {
            >= CriticalScore => RiskLevel.Critical,
            >= 60 => RiskLevel.High,
            >= 30 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };

        // Requests from an address at Critical risk are refused
        public static bool IsRestricted(int score) => LevelOf(score) == RiskLevel.Critical;
    }

    // Sign-in brute-force limits, counted over the window
    public static class SignInPolicy
    {
        public const int MaxFailedPerAccount = 5;
        public const int MaxFailedPerAddress = 20;

        public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    }
}
