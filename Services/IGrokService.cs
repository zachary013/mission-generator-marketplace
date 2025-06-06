using System.Threading.Tasks;
using SmartMarketplace.Models;

namespace SmartMarketplace.Services
{
    public interface IGrokService
    {
        Task<string> CallGrokApiAsync(string prompt);
        Task<Mission> GenerateFullMissionAsync(string simpleInput, ExtractedInformation extractedInfo);
    }
    
    public class ExtractedInformation
    {
        public string Title { get; set; } = "";
        public string Country { get; set; } = "Maroc";
        public string City { get; set; } = "Casablanca";
        public string WorkMode { get; set; } = "REMOTE";
        public int Duration { get; set; } = 3;
        public string DurationType { get; set; } = "MONTH";
        public decimal Salary { get; set; } = 4000;
        public string Currency { get; set; } = "DH";
        public string ContractType { get; set; } = "REGIE";
        public string Experience { get; set; } = "3-7";
        public string Domain { get; set; } = "Développement web";
        public string Position { get; set; } = "Développeur";
        public System.Collections.Generic.List<string> Expertises { get; set; } = new System.Collections.Generic.List<string>();
    }
}
