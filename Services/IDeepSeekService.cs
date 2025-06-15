using SmartMarketplace.Models;

namespace SmartMarketplace.Services;

public interface IDeepSeekService
{
    Task<Mission?> GenerateMissionAsync(string prompt);
}
