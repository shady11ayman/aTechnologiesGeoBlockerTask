using Microsoft.AspNetCore.Mvc;
using GeoBlocker.Application.Interfaces;

namespace GeoBlocker.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly IBlockedStore _store;

        public LogsController(IBlockedStore store)
        {
            _store = store;
        }

        // GET /api/logs/blocked-attempts
        [HttpGet("blocked-attempts")]
        public IActionResult GetBlockedAttempts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var logsOrdered = _store.GetLogs().OrderByDescending(l => l.Timestamp);
            return Ok(Paginate(logsOrdered, page, pageSize));
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