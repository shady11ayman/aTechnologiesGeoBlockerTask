namespace GeoBlocker.Application.Models
{
    public class GeoResult
    {
        public string Ip { get; set; } = "";
        public string CountryCode { get; set; } = "";
        public string CountryName { get; set; } = "";
        public string Org { get; set; } = "";
    }
}