using SmartMarketplace.Models;

namespace SmartMarketplace.Services;

public interface ILlamaService
{
    Task<Mission?> GenerateMissionAsync(string prompt);
}
