using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GeoBlocker.Application.Interfaces;

namespace GeoBlocker.Infrastructure.BackgroundServices
{
    public class TemporalBlockCleanupService : BackgroundService
    {
        private readonly IBlockedStore _store;
        private readonly ILogger<TemporalBlockCleanupService> _logger;

        public TemporalBlockCleanupService(IBlockedStore store, ILogger<TemporalBlockCleanupService> logger)
        {
            _store = store;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _store.RemoveExpiredTemporal();
                _logger.LogDebug("Temporal-block cleanup executed at {Time}", DateTimeOffset.UtcNow);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}