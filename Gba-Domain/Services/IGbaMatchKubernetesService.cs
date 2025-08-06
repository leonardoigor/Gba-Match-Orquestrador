using Gba_Domain.Models;

namespace Gba_Domain.Services;

public interface IGbaMatchKubernetesService
{
    Task<GbaMatchInfo> CreateMatchAsync(Guid matchId);
    Task DeleteMatchAsync(string podName);
    Task DeleteAllMatchesAsync();
}
