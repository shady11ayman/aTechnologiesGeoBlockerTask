namespace GeoBlocker.Domain.Entities
{
    public record TemporalBlock(string CountryCode, string CountryName, DateTimeOffset ExpiresAt);
}