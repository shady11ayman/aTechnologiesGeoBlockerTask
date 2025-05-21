using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using GeoBlocker.Application.Interfaces;
using GeoBlocker.Application.Models;
using Newtonsoft.Json;

namespace GeoBlocker.Infrastructure.Geolocation
{
    public class IpApiService : IGeoService
    {
        private readonly HttpClient _http;
        private readonly IpApiConfig _cfg;

        private const int MaxRetries = 3;
        private static readonly TimeSpan DefaultRetryAfter = TimeSpan.FromSeconds(10);

        public IpApiService(HttpClient http, IOptions<IpApiConfig> cfg)
        {
            _http = http;
            _cfg = cfg.Value;
        }

        public async Task<GeoResult?> LookupAsync(string ip, CancellationToken ct)
        {
            var url = $"{_cfg.BaseUrl}?apiKey={_cfg.ApiKey}&ip={ip}";

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                HttpResponseMessage resp;
                try
                {
                    resp = await _http.GetAsync(url, ct);
                }
                catch
                {
                    if (attempt == MaxRetries)
                        return null;
                    else
                        continue;
                }

                if (resp.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    if (resp.Headers.RetryAfter?.Delta is TimeSpan retryDelta)
                    {
                        await Task.Delay(retryDelta, ct);
                    }
                    else
                    {
                        await Task.Delay(DefaultRetryAfter, ct);
                    }

                    if (attempt == MaxRetries)
                        return null;
                    else
                        continue;
                }
                else if (!resp.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await resp.Content.ReadAsStringAsync(ct);
                var ipGeoResp = JsonConvert.DeserializeObject<IpGeolocationResponse>(json);
                if (ipGeoResp == null)
                    return null;

                return new GeoResult
                {
                    Ip = ipGeoResp.Ip,
                    CountryCode = ipGeoResp.CountryCode2,
                    CountryName = ipGeoResp.CountryName,
                    Org = ipGeoResp.Isp
                };
            }

            return null;
        }
    }

    public class IpApiConfig
    {
        public string BaseUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string Fields { get; set; } = "";
    }

    internal class IpGeolocationResponse
    {
        [JsonProperty("ip")]
        public string Ip { get; set; } = "";

        [JsonProperty("country_code2")]
        public string CountryCode2 { get; set; } = "";

        [JsonProperty("country_name")]
        public string CountryName { get; set; } = "";

        [JsonProperty("isp")]
        public string Isp { get; set; } = "";
    }
}