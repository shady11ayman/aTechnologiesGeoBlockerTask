namespace GeoBlocker.Domain.Entities
{
    public record BlockedAttempt(
        string Ip,
        DateTimeOffset Timestamp,
        string CountryCode,
        bool IsBlocked,
        string UserAgent
    );
}