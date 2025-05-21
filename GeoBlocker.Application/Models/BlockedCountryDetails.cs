namespace GeoBlocker.Application.Models
{
    public record BlockedCountryDetails(
        string CountryCode,
        string CountryName,
        bool IsTemporary,
        int? RemainingMinutes
    );
}