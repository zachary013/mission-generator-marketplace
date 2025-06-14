using SmartMarketplace.Models;

namespace SmartMarketplace.Services;

public interface IOpenAIService
{
    Task<Mission?> GenerateMissionAsync(string prompt);
    Task<bool> IsAvailableAsync();
}
