using SmartMarketplace.Models;

namespace SmartMarketplace.Services
{
    public interface IMissionTemplateService
    {
        MissionTemplate GetTemplate(string domain, string experienceLevel);
        List<string> GetDomainSpecificTechnologies(string domain);
        string GenerateContextualDescription(MissionTemplate template, ExtractedInformation info);
    }
    
    public class MissionTemplate
    {
        public string Domain { get; set; } = "";
        public string TitleTemplate { get; set; } = "";
        public string ContextDescription { get; set; } = "";
        public List<string> ResponsibilityTemplates { get; set; } = new List<string>();
        public List<string> RequiredSkills { get; set; } = new List<string>();
        public List<string> CoreTechnologies { get; set; } = new List<string>();
        public List<string> ComplementaryTechnologies { get; set; } = new List<string>();
        public string ExperienceContext { get; set; } = "";
        public List<string> ProjectTypes { get; set; } = new List<string>();
    }
}
