using Microsoft.Extensions.Options;
using SmartMarketplace.Configuration;
using SmartMarketplace.Models;
using System.Text.RegularExpressions;

namespace SmartMarketplace.Services;

public class AIService : IAIService
{
    private readonly IGeminiService _geminiService;
    private readonly IDeepSeekService _deepSeekService;
    private readonly IMistralService _mistralService;
    private readonly AIConfig _config;
    private readonly ILogger<AIService> _logger;

    public AIService(
        IGeminiService geminiService,
        IDeepSeekService deepSeekService,
        IMistralService mistralService,
        IOptions<AIConfig> config,
        ILogger<AIService> logger)
    {
        _geminiService = geminiService;
        _deepSeekService = deepSeekService;
        _mistralService = mistralService;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<MissionGenerationResult> GenerateMissionAsync(string simpleInput, string? preferredProvider = null)
    {
        var prompt = BuildIntelligentPrompt(simpleInput);
        _logger.LogInformation("Starting mission generation with input: {Input}, preferred provider: {Provider}", 
            simpleInput, preferredProvider ?? "None");
        
        // Try preferred provider first
        if (!string.IsNullOrEmpty(preferredProvider))
        {
            _logger.LogInformation("Trying preferred provider: {Provider}", preferredProvider);
            var mission = await TryGenerateWithProvider(preferredProvider, prompt);
            if (mission != null)
            {
                EnhanceMissionWithIntelligentDefaults(mission, simpleInput);
                _logger.LogInformation("Mission generated successfully with preferred provider: {Provider}", preferredProvider);
                return new MissionGenerationResult { Mission = mission, Provider = preferredProvider };
            }
            _logger.LogWarning("Preferred provider {Provider} failed, trying default provider", preferredProvider);
        }

        // Try default provider
        _logger.LogInformation("Trying default provider: {Provider}", _config.DefaultProvider);
        var defaultMission = await TryGenerateWithProvider(_config.DefaultProvider, prompt);
        if (defaultMission != null)
        {
            EnhanceMissionWithIntelligentDefaults(defaultMission, simpleInput);
            _logger.LogInformation("Mission generated successfully with default provider: {Provider}", _config.DefaultProvider);
            return new MissionGenerationResult { Mission = defaultMission, Provider = _config.DefaultProvider };
        }
        _logger.LogWarning("Default provider {Provider} failed, trying fallback providers", _config.DefaultProvider);

        // Fallback to other providers
        var providers = GetAvailableProviders().Where(p => p != _config.DefaultProvider && p != preferredProvider);
        foreach (var provider in providers)
        {
            _logger.LogInformation("Trying fallback provider: {Provider}", provider);
            var fallbackMission = await TryGenerateWithProvider(provider, prompt);
            if (fallbackMission != null)
            {
                EnhanceMissionWithIntelligentDefaults(fallbackMission, simpleInput);
                _logger.LogInformation("Mission generated successfully with fallback provider: {Provider}", provider);
                return new MissionGenerationResult { Mission = fallbackMission, Provider = provider };
            }
        }

        // Ultimate fallback - generate intelligent mission from input analysis
        _logger.LogWarning("All AI providers failed, using intelligent fallback generation");
        var intelligentMission = GenerateIntelligentFallbackMission(simpleInput);
        return new MissionGenerationResult { Mission = intelligentMission, Provider = "Intelligent Fallback" };
    }

    private async Task<Mission?> TryGenerateWithProvider(string providerName, string prompt)
    {
        try
        {
            _logger.LogInformation("Attempting to generate mission with provider: {Provider}", providerName);
            
            var mission = providerName.ToLower() switch
            {
                "gemini" => await _geminiService.GenerateMissionAsync(prompt),
                "deepseek" => await _deepSeekService.GenerateMissionAsync(prompt),
                "mistral" => await _mistralService.GenerateMissionAsync(prompt),
                _ => null
            };

            if (mission != null)
            {
                _logger.LogInformation("Successfully generated mission with provider: {Provider}", providerName);
                return mission;
            }
            else
            {
                _logger.LogWarning("Provider {Provider} returned null mission", providerName);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error with provider {Provider}: {Message}", providerName, ex.Message);
            return null;
        }
    }

    private string BuildIntelligentPrompt(string userInput)
    {
        var extractedInfo = ExtractInformationFromInput(userInput);
        
        return $@"Tu es un expert en création de missions freelance.
À partir de cette description : ""{userInput}""

Génère une mission professionnelle au format JSON EXACT suivant (pas de commentaires) :
{{
  ""title"": ""Titre mission"",
  ""description"": ""Description détaillée avec contexte, missions, stack technique, modalités"", 
  ""country"": ""Morocco"",
  ""city"": ""{extractedInfo.City ?? "Rabat"}"",
  ""workMode"": ""{extractedInfo.WorkMode ?? "REMOTE"}"",
  ""duration"": {extractedInfo.Duration ?? 3},
  ""durationType"": ""{extractedInfo.DurationType ?? "MONTH"}"",
  ""startImmediately"": true,
  ""startDate"": null,
  ""experienceYear"": ""{extractedInfo.ExperienceLevel ?? "3-7"}"",
  ""contractType"": ""{extractedInfo.ContractType ?? "FORFAIT"}"",
  ""estimatedDailyRate"": {extractedInfo.DailyRate ?? 350},
  ""domain"": ""{extractedInfo.Domain ?? "Développement Web"}"",
  ""position"": ""{extractedInfo.Position ?? "Développeur"}"", 
  ""requiredExpertises"": {GetTechnologiesJson(extractedInfo.Technologies)}
}}

RÈGLES STRICTES :
- Extraire automatiquement ville, pays, budget, durée du texte
- Convertir DH en euros (diviser par 10) si mentionné
- Identifier compétences techniques pertinentes
- Déterminer niveau d'expérience approprié
- Description détaillée avec émojis et structure professionnelle
- JSON valide uniquement, pas de texte supplémentaire";
    }

    private ExtractedInfo ExtractInformationFromInput(string input)
    {
        var info = new ExtractedInfo();
        var inputLower = input.ToLower();

        // Extract city
        var cities = new[] { "rabat", "casablanca", "marrakech", "fès", "tanger", "agadir", "oujda", "kenitra", "tetouan", "safi" };
        info.City = cities.FirstOrDefault(city => inputLower.Contains(city))?.ToTitleCase();

        // Extract work mode
        if (inputLower.Contains("remote") || inputLower.Contains("télétravail"))
            info.WorkMode = "REMOTE";
        else if (inputLower.Contains("onsite") || inputLower.Contains("présentiel"))
            info.WorkMode = "ONSITE";
        else if (inputLower.Contains("hybrid") || inputLower.Contains("hybride"))
            info.WorkMode = "HYBRID";

        // Extract duration
        var durationMatch = Regex.Match(input, @"(\d+)\s*(mois|month|semaine|week|jour|day|an|year)", RegexOptions.IgnoreCase);
        if (durationMatch.Success)
        {
            info.Duration = int.Parse(durationMatch.Groups[1].Value);
            var unit = durationMatch.Groups[2].Value.ToLower();
            info.DurationType = unit.Contains("mois") || unit.Contains("month") ? "MONTH" :
                               unit.Contains("semaine") || unit.Contains("week") ? "WEEK" :
                               unit.Contains("jour") || unit.Contains("day") ? "DAY" : "YEAR";
        }

        // Extract salary/rate
        var salaryMatch = Regex.Match(input, @"(\d+)\s*(dh|mad|euro|eur|€)", RegexOptions.IgnoreCase);
        if (salaryMatch.Success)
        {
            var amount = decimal.Parse(salaryMatch.Groups[1].Value);
            var currency = salaryMatch.Groups[2].Value.ToLower();
            // Conversion DH to EUR: 1 EUR ≈ 11 DH
            info.DailyRate = currency.Contains("dh") || currency.Contains("mad") ? Math.Round(amount / 11, 0) : amount;
        }

        // Extract experience level
        if (inputLower.Contains("senior") || inputLower.Contains("expert"))
            info.ExperienceLevel = "7-12";
        else if (inputLower.Contains("junior") || inputLower.Contains("débutant"))
            info.ExperienceLevel = "0-3";
        else if (inputLower.Contains("lead") || inputLower.Contains("architect"))
            info.ExperienceLevel = "12+";

        // Extract domain and technologies
        ExtractDomainAndTechnologies(inputLower, info);

        return info;
    }

    private void ExtractDomainAndTechnologies(string inputLower, ExtractedInfo info)
    {
        var techMappings = new Dictionary<string, (string Domain, string Position, List<string> Technologies)>
        {
            ["backend"] = ("Backend Development", "Développeur Backend", new List<string> { "Node.js", "Express.js", "MongoDB", "PostgreSQL" }),
            ["frontend"] = ("Frontend Development", "Développeur Frontend", new List<string> { "React", "Vue.js", "JavaScript", "TypeScript", "HTML5", "CSS3" }),
            ["fullstack"] = ("Full Stack Development", "Développeur Full Stack", new List<string> { "React", "Node.js", "MongoDB", "Express.js" }),
            ["mobile"] = ("Mobile Development", "Développeur Mobile", new List<string> { "React Native", "Flutter", "iOS", "Android" }),
            ["devops"] = ("DevOps", "Ingénieur DevOps", new List<string> { "Docker", "Kubernetes", "AWS", "Jenkins", "Terraform" }),
            ["data"] = ("Data Science", "Data Scientist", new List<string> { "Python", "Pandas", "Machine Learning", "SQL" }),
            ["ui/ux"] = ("UI/UX Design", "Designer UI/UX", new List<string> { "Figma", "Adobe XD", "Sketch", "Prototyping" })
        };

        // Specific technologies
        var specificTechs = new Dictionary<string, List<string>>
        {
            ["react"] = new List<string> { "React", "JavaScript", "HTML5", "CSS3" },
            ["vue"] = new List<string> { "Vue.js", "JavaScript", "HTML5", "CSS3" },
            ["angular"] = new List<string> { "Angular", "TypeScript", "HTML5", "CSS3" },
            ["node"] = new List<string> { "Node.js", "Express.js", "JavaScript", "MongoDB" },
            ["laravel"] = new List<string> { "Laravel", "PHP", "MySQL", "Eloquent", "Blade" },
            ["php"] = new List<string> { "PHP", "Laravel", "MySQL", "Symfony" },
            ["python"] = new List<string> { "Python", "Django", "Flask", "PostgreSQL" },
            ["java"] = new List<string> { "Java", "Spring Boot", "Maven", "PostgreSQL" },
            ["dotnet"] = new List<string> { "C#", ".NET Core", "ASP.NET", "SQL Server" }
        };

        foreach (var mapping in techMappings)
        {
            if (inputLower.Contains(mapping.Key))
            {
                info.Domain = mapping.Value.Domain;
                info.Position = mapping.Value.Position;
                info.Technologies.AddRange(mapping.Value.Technologies);
                return;
            }
        }

        foreach (var tech in specificTechs)
        {
            if (inputLower.Contains(tech.Key))
            {
                info.Technologies.AddRange(tech.Value);
                if (tech.Key == "react" || tech.Key == "vue" || tech.Key == "angular")
                {
                    info.Domain = "Frontend Development";
                    info.Position = "Développeur Frontend";
                }
                else if (tech.Key == "laravel" || tech.Key == "php")
                {
                    info.Domain = "Backend Development";
                    info.Position = "Développeur Backend PHP/Laravel";
                }
                else if (tech.Key == "node")
                {
                    info.Domain = "Backend Development";
                    info.Position = "Développeur Backend Node.js";
                }
                else if (tech.Key == "node" || tech.Key == "php" || tech.Key == "python" || tech.Key == "java")
                {
                    info.Domain = "Backend Development";
                    info.Position = "Développeur Backend";
                }
                break;
            }
        }
    }

    private string GetTechnologiesJson(List<string> technologies)
    {
        if (!technologies.Any())
            return @"[""JavaScript"", ""HTML5"", ""CSS3""]";
        
        var techArray = string.Join(@""", """, technologies.Distinct());
        return $@"[""{techArray}""]";
    }

    private void EnhanceMissionWithIntelligentDefaults(Mission mission, string originalInput)
    {
        // Ensure required fields have intelligent defaults
        if (string.IsNullOrEmpty(mission.Country))
            mission.Country = "Morocco";

        if (string.IsNullOrEmpty(mission.City))
            mission.City = "Rabat";

        if (mission.EstimatedDailyRate <= 0)
            mission.EstimatedDailyRate = 350;

        if (mission.Duration <= 0)
            mission.Duration = 3;

        if (string.IsNullOrEmpty(mission.ExperienceYear))
            mission.ExperienceYear = "3-7";

        if (!mission.RequiredExpertises.Any())
            mission.RequiredExpertises = new List<string> { "JavaScript", "HTML5", "CSS3" };

        // Generate ID
        mission.Id = Guid.NewGuid().ToString();
    }

    private Mission GenerateIntelligentFallbackMission(string input)
    {
        var info = ExtractInformationFromInput(input);
        
        return new Mission
        {
            Id = Guid.NewGuid().ToString(),
            Title = $"{info.Position ?? "Développeur"} - {info.City ?? "Rabat"}",
            Description = GenerateFallbackDescription(info),
            Country = "Morocco",
            City = info.City ?? "Rabat",
            WorkMode = info.WorkMode ?? "REMOTE",
            Duration = info.Duration ?? 3,
            DurationType = info.DurationType ?? "MONTH",
            StartImmediately = true,
            ExperienceYear = info.ExperienceLevel ?? "3-7",
            ContractType = info.ContractType ?? "FORFAIT",
            EstimatedDailyRate = info.DailyRate ?? 350,
            Domain = info.Domain ?? "Développement Web",
            Position = info.Position ?? "Développeur",
            RequiredExpertises = info.Technologies.Any() ? info.Technologies : new List<string> { "JavaScript", "HTML5", "CSS3" }
        };
    }

    private string GenerateFallbackDescription(ExtractedInfo info)
    {
        return $@"🎯 CONTEXTE DU PROJET :
Développement d'une application web moderne avec les dernières technologies.

💼 VOS MISSIONS :
• Développement et intégration de fonctionnalités
• Collaboration avec l'équipe technique
• Respect des bonnes pratiques de développement
• Tests et débogage des fonctionnalités

🛠️ STACK TECHNIQUE :
{string.Join(", ", info.Technologies.Any() ? info.Technologies : new List<string> { "JavaScript", "HTML5", "CSS3" })}

📍 MODALITÉS :
• Localisation : {info.City ?? "Rabat"}, Maroc
• Mode : {info.WorkMode ?? "REMOTE"}
• Durée : {info.Duration ?? 3} {info.DurationType?.ToLower() ?? "mois"}
• Expérience requise : {info.ExperienceLevel ?? "3-7"} ans";
    }

    public async Task<bool> IsProviderAvailableAsync(string providerName)
    {
        return providerName.ToLower() switch
        {
            "gemini" => await IsGeminiAvailableAsync(),
            "deepseek" => await IsDeepSeekAvailableAsync(),
            "mistral" => await IsMistralAvailableAsync(),
            _ => false
        };
    }

    private Task<bool> IsGeminiAvailableAsync()
    {
        try
        {
            // Simple test to check if Gemini service is available
            return Task.FromResult(!string.IsNullOrEmpty(_config.Gemini.ApiKey));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private Task<bool> IsDeepSeekAvailableAsync()
    {
        try
        {
            // Simple test to check if Deep Seek service is available
            return Task.FromResult(!string.IsNullOrEmpty(_config.DeepSeek.ApiKey));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private Task<bool> IsMistralAvailableAsync()
    {
        try
        {
            // Simple test to check if Mistral service is available
            return Task.FromResult(!string.IsNullOrEmpty(_config.Mistral.ApiKey));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public List<string> GetAvailableProviders()
    {
        return new List<string> { "Gemini", "Mistral", "DeepSeek" };
    }

    private class ExtractedInfo
    {
        public string? City { get; set; }
        public string? WorkMode { get; set; }
        public int? Duration { get; set; }
        public string? DurationType { get; set; }
        public decimal? DailyRate { get; set; }
        public string? ExperienceLevel { get; set; }
        public string? ContractType { get; set; }
        public string? Domain { get; set; }
        public string? Position { get; set; }
        public List<string> Technologies { get; set; } = new();
    }
}

public static class StringExtensions
{
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        return char.ToUpper(input[0]) + input.Substring(1).ToLower();
    }
}


