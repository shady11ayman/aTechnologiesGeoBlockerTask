

using Microsoft.AspNetCore.Mvc;
using GeoBlocker.Application.Interfaces;
using GeoBlocker.Application.Models;
using GeoBlocker.Application.Services;
using System.Globalization;

namespace GeoBlocker.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountriesController : ControllerBase
    {
        private readonly BlockCountryService _blockCountryService;
        private readonly IBlockedStore _store;

        public CountriesController(BlockCountryService blockCountryService, IBlockedStore store)
        {
            _blockCountryService = blockCountryService;
            _store = store;
        }

        // POST /api/countries/block
        [HttpPost("block")]
        public IActionResult AddBlockedCountry([FromQuery] string code)
        {
            if (!TryValidCountry(code, out var region))
                return BadRequest($"Invalid country code '{code}'.");

            var result = _blockCountryService.Execute(region.TwoLetterISORegionName, region.EnglishName);
            if (!result)
                return Conflict("Country is already blocked.");

            return Created($"/api/countries/block/{region.TwoLetterISORegionName}", new
            {
                code = region.TwoLetterISORegionName,
                name = region.EnglishName
            });
        }

        // DELETE /api/countries/block/{code}
        [HttpDelete("block/{code}")]
        public IActionResult RemoveBlockedCountry(string code)
        {
            if (!_store.RemoveBlocked(code))
                return NotFound();

            return NoContent();
        }

        // GET /api/countries/blocked
        [HttpGet("blocked")]
        public IActionResult GetBlockedCountries(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
           
            var data = _store.GetAllCurrentlyBlockedDetails();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLowerInvariant();
                data = data.Where(c =>
                    c.CountryCode.ToLowerInvariant().Contains(search) ||
                    c.CountryName.ToLowerInvariant().Contains(search));
            }

            return Ok(Paginate(data, page, pageSize));
        }

        // POST /api/countries/temporal-block
        [HttpPost("temporal-block")]
        public IActionResult AddTemporalBlock([FromBody] TemporalBlockRequest req)
        {
            if (req.DurationMinutes < 1 || req.DurationMinutes > 1440)
                return BadRequest("Duration must be between 1 and 1440 minutes.");

            if (!TryValidCountry(req.CountryCode, out var region))
                return BadRequest("Invalid country code.");

            if (!_store.AddTemporal(region.TwoLetterISORegionName, region.EnglishName, req.DurationMinutes, out var err))
                return Conflict(err);

            return Ok(new
            {
                country = region.TwoLetterISORegionName,
                expiresAt = DateTimeOffset.UtcNow.AddMinutes(req.DurationMinutes)
            });
        }

        private bool TryValidCountry(string code, out RegionInfo region)
        {
            region = null!;
            try
            {
                region = new RegionInfo(code);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private object Paginate<T>(IEnumerable<T> data, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var total = data.Count();
            var items = data.Skip((page - 1) * pageSize).Take(pageSize);
            return new
            {
                page,
                pageSize,
                total,
                items
            };
        }
    }
}