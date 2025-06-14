using SmartMarketplace.Models;

namespace SmartMarketplace.Services;

public interface IAIService
{
    Task<Mission?> GenerateMissionAsync(string simpleInput, string? preferredProvider = null);
    Task<bool> IsProviderAvailableAsync(string providerName);
    List<string> GetAvailableProviders();
}
