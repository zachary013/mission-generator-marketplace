using SmartMarketplace.Models;

namespace SmartMarketplace.Services
{
    public interface IInputAnalysisService
    {
        ExtractedInformation AnalyzeInput(string input);
        string DetermineProjectType(string input);
        string GenerateContextualPrompt(string input, ExtractedInformation extractedInfo);
    }
    
    public class ProjectContext
    {
        public string Type { get; set; } = "";
        public string Industry { get; set; } = "";
        public string Complexity { get; set; } = "";
        public List<string> SuggestedTechnologies { get; set; } = new List<string>();
        public string Description { get; set; } = "";
    }
}
