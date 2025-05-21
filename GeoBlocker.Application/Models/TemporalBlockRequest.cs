namespace GeoBlocker.Application.Models
{
    public record TemporalBlockRequest(string CountryCode, int DurationMinutes);
}