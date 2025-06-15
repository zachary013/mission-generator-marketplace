using SmartMarketplace.Models;

namespace SmartMarketplace.Services;

public interface IGeminiService
{
    Task<Mission?> GenerateMissionAsync(string prompt);
}
