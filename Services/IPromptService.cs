namespace SmartMarketplace.Services
{
    public interface IPromptService
    {
        string GeneratePrompt(string domain, ExtractedInformation extractedInfo, string originalInput);
        string GetRandomPromptVariation(string domain);
    }
    
    public class PromptVariation
    {
        public string Style { get; set; } = "";
        public string Tone { get; set; } = "";
        public string Focus { get; set; } = "";
        public List<string> Keywords { get; set; } = new List<string>();
    }
}
