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
        public string SimpleInput { get; set; }
        public bool ShowGeneratedMission { get; set; }

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
                StartImmediately = true, // Default value
                Country = "Maroc" // Default country
            };
            
            // Default mode is simple input
            IsGenerationMode = false;
            ShowGeneratedMission = false;
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
                // Store the original input
                SimpleInput = simpleInput;
                
                // Initialize SelectLists for dropdown menus
                InitializeSelectLists();
                
                // Set generation mode
                IsGenerationMode = true;
                ShowGeneratedMission = true;
                
                // Extract information from the simple input
                var extractedInfo = ExtractInformationFromInput(simpleInput);
                
                // Build the prompt for Grok API
                string prompt = $@"
                Génère une mission complète de développement freelance basée sur cette description simple: '{simpleInput}'.
                
                Utilise ces informations pour créer:
                1. Un titre professionnel et accrocheur (max 80 caractères) qui reflète exactement le rôle mentionné dans l'entrée
                2. Une description technique détaillée qui inclut:
                   - Le contexte du projet
                   - Les responsabilités du développeur
                   - Les livrables attendus
                   - La stack technique complète
                3. Informations logistiques:
                   - Pays: {extractedInfo.Country}
                   - Ville: {extractedInfo.City}
                   - Mode de travail: {extractedInfo.WorkMode}
                   - Durée: {extractedInfo.Duration} {extractedInfo.DurationType}
                4. Informations contractuelles:
                   - TJM: {extractedInfo.Salary} {extractedInfo.Currency}
                   - Type de contrat: {extractedInfo.ContractType}
                   - Expérience requise: {extractedInfo.Experience}
                   - Domaine: {extractedInfo.Domain}
                   - Poste: {extractedInfo.Position}
                
                Réponds uniquement avec un JSON structuré comme suit:
                {{
                  ""title"": ""Titre de la mission"",
                  ""description"": ""Description détaillée"",
                  ""country"": ""{extractedInfo.Country}"",
                  ""city"": ""{extractedInfo.City}"",
                  ""workMode"": ""{extractedInfo.WorkMode}"",
                  ""duration"": {extractedInfo.Duration},
                  ""durationType"": ""{extractedInfo.DurationType}"",
                  ""startImmediately"": true,
                  ""experienceYear"": ""{extractedInfo.Experience}"",
                  ""contractType"": ""{extractedInfo.ContractType}"",
                  ""estimatedDailyRate"": {extractedInfo.Salary},
                  ""domain"": ""{extractedInfo.Domain}"",
                  ""position"": ""{extractedInfo.Position}"",
                  ""requiredExpertises"": {JsonSerializer.Serialize(extractedInfo.Expertises)}
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
                        
                        // Set the expertises input value for the form
                        ExpertisesInputValue = string.Join(", ", Mission.RequiredExpertises ?? new List<string>());
                        
                        return Page();
                    }
                    catch (JsonException ex)
                    {
                        // If JSON parsing fails, try to generate a mission from the text response
                        Mission = GenerateFallbackMission(simpleInput, grokResponse, extractedInfo);
                        ExpertisesInputValue = string.Join(", ", Mission.RequiredExpertises ?? new List<string>());
                        return Page();
                    }
                }
                else
                {
                    // If API call fails, generate a basic mission
                    Mission = GenerateFallbackMission(simpleInput, "", extractedInfo);
                    ExpertisesInputValue = string.Join(", ", Mission.RequiredExpertises ?? new List<string>());
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
                ShowGeneratedMission = true;
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
            ShowGeneratedMission = true;

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
        
        private class ExtractedInformation
        {
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
            public List<string> Expertises { get; set; } = new List<string>();
        }
        
        private ExtractedInformation ExtractInformationFromInput(string input)
        {
            var info = new ExtractedInformation();
            
            // Extract city
            var moroccanCities = new[] { 
                "Casablanca", "Rabat", "Marrakech", "Fès", "Tanger", "Agadir", "Meknès", "Oujda", 
                "Kénitra", "Tétouan", "Safi", "Mohammedia", "El Jadida", "Béni Mellal", "Nador", 
                "Khémisset", "Taza", "Settat", "Berrechid", "Khénifra", "Larache", "Guelmim", 
                "Khouribga", "Ouarzazate", "Youssoufia", "Dakhla", "Laâyoune", "Essaouira", "Ifrane"
            };
            
            foreach (var city in moroccanCities)
            {
                if (input.IndexOf(city, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    info.City = city;
                    break;
                }
            }
            
            // Extract country (if explicitly mentioned)
            var countries = new[] { "Maroc", "France", "Belgique", "Suisse", "Canada", "Tunisie", "Algérie" };
            foreach (var country in countries)
            {
                if (input.IndexOf(country, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    info.Country = country;
                    break;
                }
            }
            
            // Extract work mode
            if (input.IndexOf("remote", StringComparison.OrdinalIgnoreCase) >= 0 || 
                input.IndexOf("télétravail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                input.IndexOf("à distance", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                info.WorkMode = "REMOTE";
            }
            else if (input.IndexOf("onsite", StringComparison.OrdinalIgnoreCase) >= 0 || 
                     input.IndexOf("sur site", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     input.IndexOf("présentiel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                info.WorkMode = "ONSITE";
            }
            else if (input.IndexOf("hybrid", StringComparison.OrdinalIgnoreCase) >= 0 || 
                     input.IndexOf("hybride", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                info.WorkMode = "HYBRID";
            }
            
            // Extract salary and currency
            var salaryRegex = new Regex(@"(\d+[,\s]?\d*)\s*(?:dh|MAD|dirhams|euros|EUR|€|\$|USD|dollars)", RegexOptions.IgnoreCase);
            var match = salaryRegex.Match(input);
            
            if (match.Success)
            {
                string budgetStr = match.Groups[1].Value.Replace(" ", "").Replace(",", "");
                if (decimal.TryParse(budgetStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal budget))
                {
                    info.Salary = budget;
                    
                    // Extract currency
                    string fullMatch = match.Value.ToLowerInvariant();
                    if (fullMatch.Contains("dh") || fullMatch.Contains("mad") || fullMatch.Contains("dirhams"))
                    {
                        info.Currency = "DH";
                    }
                    else if (fullMatch.Contains("euros") || fullMatch.Contains("eur") || fullMatch.Contains("€"))
                    {
                        info.Currency = "EUR";
                    }
                    else if (fullMatch.Contains("$") || fullMatch.Contains("usd") || fullMatch.Contains("dollars"))
                    {
                        info.Currency = "USD";
                    }
                }
            }
            
            // Extract contract type
            if (input.IndexOf("forfait", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                info.ContractType = "FORFAIT";
            }
            else if (input.IndexOf("régie", StringComparison.OrdinalIgnoreCase) >= 0 || 
                     input.IndexOf("regie", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                info.ContractType = "REGIE";
            }
            
            // Extract duration
            var durationRegex = new Regex(@"(\d+)\s*(?:mois|ans|années|semaines|jours)", RegexOptions.IgnoreCase);
            match = durationRegex.Match(input);
            
            if (match.Success)
            {
                if (int.TryParse(match.Groups[1].Value, out int duration))
                {
                    info.Duration = duration;
                    
                    string unit = match.Value.ToLowerInvariant();
                    if (unit.Contains("mois"))
                    {
                        info.DurationType = "MONTH";
                    }
                    else if (unit.Contains("ans") || unit.Contains("années"))
                    {
                        info.DurationType = "YEAR";
                    }
                    else if (unit.Contains("semaines") || unit.Contains("jours"))
                    {
                        // Convert weeks to months (approximately)
                        info.DurationType = "MONTH";
                        if (unit.Contains("semaines"))
                        {
                            info.Duration = Math.Max(1, duration / 4); // Rough approximation: 4 weeks = 1 month
                        }
                        else // days
                        {
                            info.Duration = Math.Max(1, duration / 30); // Rough approximation: 30 days = 1 month
                        }
                    }
                }
            }
            
            // Extract experience level
            if (input.IndexOf("junior", StringComparison.OrdinalIgnoreCase) >= 0 || 
                input.IndexOf("débutant", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                info.Experience = "0-3";
            }
            else if (input.IndexOf("senior", StringComparison.OrdinalIgnoreCase) >= 0 || 
                     input.IndexOf("expérimenté", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                info.Experience = "7-12";
            }
            else if (input.IndexOf("expert", StringComparison.OrdinalIgnoreCase) >= 0 || 
                     input.IndexOf("lead", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     input.IndexOf("architecte", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                info.Experience = "12+";
            }
            
            // Extract position and domain
            var techRoles = new Dictionary<string, List<string>> {
                { "Frontend", new List<string> { "react", "angular", "vue", "javascript", "typescript", "html", "css", "frontend", "front-end", "front end" } },
                { "Backend", new List<string> { "node", "express", "php", "laravel", "symfony", "python", "django", "flask", "java", "spring", "c#", ".net", "asp.net", "ruby", "rails", "backend", "back-end", "back end" } },
                { "Fullstack", new List<string> { "fullstack", "full-stack", "full stack", "mern", "mean" } },
                { "Mobile", new List<string> { "react native", "flutter", "swift", "kotlin", "android", "ios", "mobile" } },
                { "DevOps", new List<string> { "devops", "aws", "azure", "gcp", "docker", "kubernetes", "jenkins", "ci/cd", "terraform", "ansible" } },
                { "Data", new List<string> { "data", "sql", "mysql", "postgresql", "mongodb", "nosql", "big data", "hadoop", "spark", "etl", "bi", "business intelligence", "data science", "machine learning", "ml", "ai", "intelligence artificielle" } },
                { "Sécurité", new List<string> { "sécurité", "security", "pentest", "cybersécurité", "cybersecurity" } },
                { "CMS", new List<string> { "wordpress", "drupal", "joomla", "magento", "shopify", "prestashop", "cms" } }
            };
            
            foreach (var role in techRoles)
            {
                foreach (var keyword in role.Value)
                {
                    if (input.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        info.Domain = $"Développement {role.Key}";
                        info.Position = $"Développeur {role.Key}";
                        info.Expertises.Add(keyword);
                        break;
                    }
                }
                
                if (info.Expertises.Count > 0)
                {
                    break;
                }
            }
            
            // Extract specific technologies for expertises
            var commonTechs = new List<string>
            {
                "React", "Angular", "Vue", "JavaScript", "TypeScript", "Node.js", "Express", 
                "PHP", "Laravel", "Symfony", "Python", "Django", "Flask", "Java", "Spring", 
                "C#", ".NET", "ASP.NET", "Ruby", "Rails", "HTML", "CSS", "SASS", "LESS",
                "SQL", "MySQL", "PostgreSQL", "MongoDB", "Firebase", "AWS", "Azure", "Docker",
                "Kubernetes", "DevOps", "CI/CD", "Git", "React Native", "Flutter", "Swift",
                "Kotlin", "Android", "iOS", "WordPress", "Shopify", "Magento", "Drupal"
            };
            
            foreach (var tech in commonTechs)
            {
                if (input.IndexOf(tech, StringComparison.OrdinalIgnoreCase) >= 0 && 
                    !info.Expertises.Contains(tech, StringComparer.OrdinalIgnoreCase))
                {
                    info.Expertises.Add(tech);
                }
            }
            
            return info;
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
        
        private Mission GenerateFallbackMission(string simpleInput, string grokResponse, ExtractedInformation extractedInfo)
        {
            string techString = string.Join(", ", extractedInfo.Expertises);
            string title = !string.IsNullOrEmpty(extractedInfo.Position) 
                ? extractedInfo.Position 
                : $"Développement {techString}";
            
            if (title.Length > 80)
            {
                title = $"Développement {extractedInfo.Expertises.FirstOrDefault() ?? "web"}";
            }
            
            string description = !string.IsNullOrWhiteSpace(grokResponse) 
                ? grokResponse 
                : $"Nous recherchons un développeur expérimenté en {techString} pour un projet de développement web. " +
                  $"Le candidat devra maîtriser les technologies mentionnées et être capable de travailler de manière autonome.";
            
            return new Mission
            {
                Title = title,
                Description = description,
                Country = extractedInfo.Country,
                City = extractedInfo.City,
                WorkMode = extractedInfo.WorkMode,
                Duration = extractedInfo.Duration,
                DurationType = extractedInfo.DurationType,
                StartImmediately = true,
                ExperienceYear = extractedInfo.Experience,
                ContractType = extractedInfo.ContractType,
                EstimatedDailyRate = extractedInfo.Salary,
                Domain = extractedInfo.Domain,
                Position = extractedInfo.Position,
                RequiredExpertises = extractedInfo.Expertises
            };
        }
    }
}
