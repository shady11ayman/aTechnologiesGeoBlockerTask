using GeoBlocker.Application.Interfaces;

namespace GeoBlocker.Application.Services
{
    public class BlockCountryService
    {
        private readonly IBlockedStore _blockedStore;

        public BlockCountryService(IBlockedStore blockedStore)
        {
            _blockedStore = blockedStore;
        }

        public bool Execute(string countryCode, string countryName)
        {
            return _blockedStore.AddPermanent(countryCode, countryName);
        }
    }
}