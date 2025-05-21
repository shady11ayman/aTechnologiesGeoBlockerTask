using GeoBlocker.Application.Models;
using GeoBlocker.Domain.Entities;

namespace GeoBlocker.Application.Interfaces
{
    public interface IBlockedStore
    {
        bool AddPermanent(string code, string name);
        bool RemoveBlocked(string code);
        IEnumerable<BlockedCountry> GetAllPermanent();

        bool AddTemporal(string code, string name, int minutes, out string error);
        public IEnumerable<BlockedCountryDetails> GetAllCurrentlyBlockedDetails();
        void RemoveExpiredTemporal();
        bool IsBlocked(string code);

        void Log(BlockedAttempt attempt);
        IEnumerable<BlockedAttempt> GetLogs();
    }
}