using GeoBlocker.Domain.Entities;
using GeoBlocker.Application.Models;

namespace GeoBlocker.Application.Interfaces
{
    public interface IGeoService
    {
        Task<GeoResult?> LookupAsync(string ip, CancellationToken ct);
    }
}