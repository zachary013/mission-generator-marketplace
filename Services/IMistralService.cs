using SmartMarketplace.Models;

namespace SmartMarketplace.Services;

public interface IMistralService
{
    Task<Mission?> GenerateMissionAsync(string prompt);
    Task<bool> IsAvailableAsync();
}
