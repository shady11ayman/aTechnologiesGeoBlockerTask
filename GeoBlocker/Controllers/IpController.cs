using Microsoft.AspNetCore.Mvc;
using GeoBlocker.Application.Interfaces;
using GeoBlocker.Domain.Entities;
using System.Net;

namespace GeoBlocker.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IpController : ControllerBase
    {
        private readonly IGeoService _geo;
        private readonly IBlockedStore _store;

        public IpController(IGeoService geo, IBlockedStore store)
        {
            _geo = geo;
            _store = store;
        }

        // GET /api/ip/lookup?ipAddress=...
        [HttpGet("lookup")]
        public async Task<IActionResult> IpLookup([FromQuery] string? ipAddress, CancellationToken ct)
        {
            var ip = string.IsNullOrWhiteSpace(ipAddress)
                ? GetCallerIp()
                : ipAddress;

            if (!IPAddress.TryParse(ip, out _))
                return BadRequest("Invalid IP address format.");

            var result = await _geo.LookupAsync(ip, ct);
            if (result == null)
                return StatusCode(502, "Failed to fetch IP details from upstream service.");

            return Ok(result);
        }

        // GET /api/ip/check-block
        [HttpGet("check-block")]
        public async Task<IActionResult> IpCheckBlock(CancellationToken ct)
        {
            var ip = GetCallerIp();
            var lookup = await _geo.LookupAsync(ip, ct);
            if (lookup == null)
                return StatusCode(502, "Failed to fetch IP details from upstream service.");

            var isBlocked = _store.IsBlocked(lookup.CountryCode);

            _store.Log(new BlockedAttempt(
                lookup.Ip,
                DateTimeOffset.UtcNow,
                lookup.CountryCode,
                isBlocked,
                Request.Headers.UserAgent.ToString()
            ));

            return Ok(new
            {
                ip = lookup.Ip,
                country = lookup.CountryCode,
                isBlocked
            });
        }

        private string GetCallerIp()
        {
            var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0]
                     ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                     ?? "UNKNOWN";
            return ip;
        }
    }
}