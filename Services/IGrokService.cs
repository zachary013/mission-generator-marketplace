using System.Threading.Tasks;

namespace SmartMarketplace.Services
{
    public interface IGrokService
    {
        Task<string> CallGrokApiAsync(string prompt);
    }
}
