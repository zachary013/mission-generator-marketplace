using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartMarketplace.Models;
using SmartMarketplace.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SmartMarketplace.Pages
{
    public class CreateMissionModel : PageModel
    {
        private readonly IGrokService _grokService;

        public CreateMissionModel(IGrokService grokService)
        {
            _grokService = grokService;
        }

        [BindProperty]
        public Mission Mission { get; set; }

        public SelectList WorkModeOptions { get; set; }
        public SelectList DurationTypeOptions { get; set; }
        public SelectList ExperienceYearOptions { get; set; }
        public SelectList ContractTypeOptions { get; set; }
        
        public string MissionJson { get; set; }
        public bool SubmissionSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsGenerationMode { get; set; }
        public string ExpertisesInputValue { get; set; }

        public void OnGet()
        {
            // Check if there's an error being handled
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionHandlerPathFeature != null)
            {
                ErrorMessage = "Une erreur s'est produite. Veuillez réessayer plus tard.";
            }
            
            // Initialize SelectLists for dropdown menus
            InitializeSelectLists();
            
            // Initialize a new Mission object
            Mission = new Mission
            {
                StartImmediately = true // Default value
            };
            
            // Default mode is simple input
            IsGenerationMode = false;
        }

        public async Task<IActionResult> OnPostGenerateAsync(string simpleInput)
        {
            if (string.IsNullOrWhiteSpace(simpleInput))
            {
                ErrorMessage = "Veuillez saisir une description de votre besoin.";
                return Page();
            }

            try
            {
                // Initialize SelectLists for dropdown menus
                InitializeSelectLists();
                
                // Set generation mode
                IsGenerationMode = true;
                
                // Parse simple input for budget
                decimal budget = ExtractBudget(simpleInput);
                
                // Extract technologies
                List<string> technologies = ExtractTechnologies(simpleInput);
                string techString = string.Join(", ", technologies);
                
                // Store for the form
                ExpertisesInputValue = techString;
                
                // Build the prompt for Grok API
                string prompt = $@"
                Génère une mission complète de développement freelance basée sur cette description simple: '{simpleInput}'.
                
                Utilise ces informations pour créer:
                1. Un titre professionnel et accrocheur (max 80 caractères)
                2. Une description technique détaillée qui inclut:
                   - Le contexte du projet
                   - Les responsabilités du développeur
                   - Les livrables attendus
                   - La stack technique complète
                3. Informations logistiques:
                   - Pays: Maroc
                   - Ville: Casablanca (par défaut, sauf si une autre ville est mentionnée)
                   - Mode de travail: REMOTE (par défaut, sauf si ONSITE ou HYBRID est mentionné)
                   - Durée: 3 mois (par défaut, ou estime une durée réaliste)
                4. Informations contractuelles:
                   - TJM: {budget} MAD (si un montant est mentionné)
                   - Type de contrat: REGIE (par défaut)
                   - Expérience requise: 3-7 ans (par défaut, ou estime selon la complexité)
                   - Domaine: Développement web (par défaut, ou précise selon le contexte)
                   - Poste: Développeur {techString} (adapte selon les technologies)
                
                Réponds uniquement avec un JSON structuré comme suit:
                {{
                  ""title"": ""Titre de la mission"",
                  ""description"": ""Description détaillée"",
                  ""country"": ""Maroc"",
                  ""city"": ""Ville"",
                  ""workMode"": ""REMOTE/ONSITE/HYBRID"",
                  ""duration"": nombre,
                  ""durationType"": ""MONTH/YEAR"",
                  ""startImmediately"": true/false,
                  ""experienceYear"": ""0-3/3-7/7-12/12+"",
                  ""contractType"": ""FORFAIT/REGIE"",
                  ""estimatedDailyRate"": nombre,
                  ""domain"": ""Domaine"",
                  ""position"": ""Poste"",
                  ""requiredExpertises"": [""tech1"", ""tech2"", ...]
                }}";
                
                // Call Grok API
                string grokResponse = await _grokService.CallGrokApiAsync(prompt);
                
                // Check if response is valid
                if (!string.IsNullOrWhiteSpace(grokResponse) && !grokResponse.StartsWith("Error"))
                {
                    try
                    {
                        // Extract JSON from response (in case there's additional text)
                        string jsonContent = ExtractJsonFromResponse(grokResponse);
                        
                        // Deserialize the JSON response
                        var generatedMission = JsonSerializer.Deserialize<Mission>(jsonContent);
                        
                        // Set the generated mission
                        Mission = generatedMission;
                        
                        // Ensure StartImmediately is set correctly
                        if (Mission.StartImmediately && Mission.StartDate.HasValue)
                        {
                            Mission.StartDate = null;
                        }
                        
                        return Page();
                    }
                    catch (JsonException ex)
                    {
                        // If JSON parsing fails, try to generate a mission from the text response
                        Mission = GenerateFallbackMission(simpleInput, grokResponse, budget, technologies);
                        return Page();
                    }
                }
                else
                {
                    // If API call fails, generate a basic mission
                    Mission = GenerateFallbackMission(simpleInput, "", budget, technologies);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Une erreur s'est produite lors de la génération de la mission: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Process RequiredExpertises from comma-separated string
            string expertisesInput = Request.Form["ExpertisesInput"];
            if (!string.IsNullOrWhiteSpace(expertisesInput))
            {
                Mission.RequiredExpertises = expertisesInput
                    .Split(',')
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
            }

            if (!ModelState.IsValid)
            {
                // Re-initialize SelectLists if validation fails
                InitializeSelectLists();
                IsGenerationMode = true;
                ExpertisesInputValue = expertisesInput;
                return Page();
            }

            // Serialize the Mission object to JSON (for debugging purposes)
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            MissionJson = JsonSerializer.Serialize(Mission, options);
            SubmissionSuccessful = true;

            // TODO: Save the Mission to your database
            // For example: await _context.Missions.AddAsync(Mission);
            // await _context.SaveChangesAsync();

            return Page();
        }
        
        private void InitializeSelectLists()
        {
            WorkModeOptions = new SelectList(new[] { "REMOTE", "ONSITE", "HYBRID" });
            DurationTypeOptions = new SelectList(new[] { "MONTH", "YEAR" });
            ExperienceYearOptions = new SelectList(new[] { "0-3", "3-7", "7-12", "12+" });
            ContractTypeOptions = new SelectList(new[] { "FORFAIT", "REGIE" });
        }
        
        private decimal ExtractBudget(string input)
        {
            // Look for patterns like "4000 dh", "4000dh", "4000 MAD", "4,000 MAD", etc.
            var regex = new Regex(@"(\d+[,\s]?\d*)\s*(?:dh|MAD|dirhams)", RegexOptions.IgnoreCase);
            var match = regex.Match(input);
            
            if (match.Success)
            {
                string budgetStr = match.Groups[1].Value.Replace(" ", "").Replace(",", "");
                if (decimal.TryParse(budgetStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal budget))
                {
                    return budget;
                }
            }
            
            // Default budget if not found
            return 4000m;
        }
        
        private List<string> ExtractTechnologies(string input)
        {
            // Common technologies to look for
            var commonTechs = new List<string>
            {
                "React", "Angular", "Vue", "JavaScript", "TypeScript", "Node.js", "Express", 
                "PHP", "Laravel", "Symfony", "Python", "Django", "Flask", "Java", "Spring", 
                "C#", ".NET", "ASP.NET", "Ruby", "Rails", "HTML", "CSS", "SASS", "LESS",
                "SQL", "MySQL", "PostgreSQL", "MongoDB", "Firebase", "AWS", "Azure", "Docker",
                "Kubernetes", "DevOps", "CI/CD", "Git", "React Native", "Flutter", "Swift",
                "Kotlin", "Android", "iOS", "WordPress", "Shopify", "Magento", "Drupal"
            };
            
            var foundTechs = new List<string>();
            
            // Check for each technology in the input
            foreach (var tech in commonTechs)
            {
                if (input.IndexOf(tech, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    foundTechs.Add(tech);
                }
            }
            
            // If no technologies found, extract words that might be technologies
            if (foundTechs.Count == 0)
            {
                var words = input.Split(new[] { ' ', ',', '.', ';', ':', '/', '\\', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    // Words that are likely technologies (not common words, numbers, etc.)
                    if (word.Length > 2 && !decimal.TryParse(word, out _) && 
                        !new[] { "dh", "mad", "dirhams", "pour", "avec", "and", "the", "une", "des", "les" }
                            .Contains(word.ToLowerInvariant()))
                    {
                        foundTechs.Add(word);
                    }
                }
            }
            
            return foundTechs;
        }
        
        private string ExtractJsonFromResponse(string response)
        {
            // Find the first '{' and the last '}'
            int start = response.IndexOf('{');
            int end = response.LastIndexOf('}');
            
            if (start >= 0 && end > start)
            {
                return response.Substring(start, end - start + 1);
            }
            
            return response; // Return the original if no JSON found
        }
        
        private Mission GenerateFallbackMission(string simpleInput, string grokResponse, decimal budget, List<string> technologies)
        {
            string techString = string.Join(", ", technologies);
            string title = $"Développement {techString}";
            
            if (title.Length > 80)
            {
                title = $"Développement {technologies.FirstOrDefault() ?? "web"}";
            }
            
            string description = !string.IsNullOrWhiteSpace(grokResponse) 
                ? grokResponse 
                : $"Nous recherchons un développeur expérimenté en {techString} pour un projet de développement web. " +
                  $"Le candidat devra maîtriser les technologies mentionnées et être capable de travailler de manière autonome.";
            
            return new Mission
            {
                Title = title,
                Description = description,
                Country = "Maroc",
                City = "Casablanca",
                WorkMode = "REMOTE",
                Duration = 3,
                DurationType = "MONTH",
                StartImmediately = true,
                ExperienceYear = "3-7",
                ContractType = "REGIE",
                EstimatedDailyRate = budget,
                Domain = "Développement web",
                Position = $"Développeur {technologies.FirstOrDefault() ?? "web"}",
                RequiredExpertises = technologies
            };
        }
    }
}
