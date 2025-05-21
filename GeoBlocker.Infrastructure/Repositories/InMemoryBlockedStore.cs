using System.Collections.Concurrent;
using GeoBlocker.Application.Interfaces;
using GeoBlocker.Application.Models;
using GeoBlocker.Domain.Entities;

namespace GeoBlocker.Infrastructure.Repositories
{
    public class InMemoryBlockedStore : IBlockedStore
    {
        private readonly ConcurrentDictionary<string, BlockedCountry> _blocked =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, TemporalBlock> _temporal =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentQueue<BlockedAttempt> _logs = new();

        // 1) Permanent Blocks
        public bool AddPermanent(string code, string name)
        {
            // If we successfully add to permanent, remove from temporal
            var added = _blocked.TryAdd(code, new BlockedCountry(code, name));
            if (added)
            {
                _temporal.TryRemove(code, out _);
            }
            return added;
        }

        public bool RemoveBlocked(string code)
        {
            var removedPermanent = _blocked.TryRemove(code, out _);
            var removedTemporal = _temporal.TryRemove(code, out _);
            return removedPermanent || removedTemporal;
        }

        public IEnumerable<BlockedCountry> GetAllPermanent()
        {
            return _blocked.Values;
        }

        // 2) Temporal Blocks
        public bool AddTemporal(string code, string name, int minutes, out string error)
        {
            error = string.Empty;
            var expires = DateTimeOffset.UtcNow.AddMinutes(minutes);

            if (_blocked.ContainsKey(code))
            {
                error = $"Country {code} is already permanently blocked.";
                return false;
            }

            if (_temporal.ContainsKey(code))
            {
                error = $"Country {code} is already temporally blocked.";
                return false;
            }

            return _temporal.TryAdd(code, new TemporalBlock(code, name, expires));
        }

        public void RemoveExpiredTemporal()
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var kvp in _temporal)
            {
                if (kvp.Value.ExpiresAt <= now)
                {
                    _temporal.TryRemove(kvp.Key, out _);
                }
            }
        }

        // 3) Detailed View of Currently Blocked Countries
        public IEnumerable<BlockedCountryDetails> GetAllCurrentlyBlockedDetails()
        {
            var now = DateTimeOffset.UtcNow;

            // Permanent => IsTemporary:false, RemainingMinutes:null
            var permanent = _blocked.Values.Select(b =>
                new BlockedCountryDetails(
                    b.CountryCode,
                    b.CountryName,
                    false,
                    null
                )
            );

            // Active temporal => compute remaining time
            var activeTemporal = _temporal.Values
                .Where(t => t.ExpiresAt > now)
                .Select(t =>
                {
                    var remaining = t.ExpiresAt - now;
                    var remainingMinutes = (int)Math.Ceiling(remaining.TotalMinutes);

                    return new BlockedCountryDetails(
                        t.CountryCode,
                        t.CountryName,
                        true,
                        remainingMinutes
                    );
                });

            return permanent.Concat(activeTemporal);
        }

        // 4) Check if a Country is Blocked
        public bool IsBlocked(string code)
        {
            return _blocked.ContainsKey(code) || _temporal.ContainsKey(code);
        }

        // 5) Logging
        public void Log(BlockedAttempt attempt)
        {
            _logs.Enqueue(attempt);
        }

        public IEnumerable<BlockedAttempt> GetLogs()
        {
            return _logs;
        }
    }
}