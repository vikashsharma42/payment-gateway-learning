namespace DineBook.Domain.Enums
{
    // What the security monitor noticed. Each kind has a risk weight in RiskScoring.
    public enum SecurityEventKind
    {
        LoginFailed,
        LoginLocked,
        SqlInjectionSuspected,
        XssSuspected,
        ScannerDetected,
        HoneypotProbe,
        AutomatedClient,
        CsrfRejected,
        RiskRestricted,
        AddressUnblocked
    }
}
