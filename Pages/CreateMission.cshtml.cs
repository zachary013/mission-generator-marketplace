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
                
                // Generate mission using the improved GrokService
                Mission = await _grokService.GenerateFullMissionAsync(simpleInput, extractedInfo);
                
                // Set the expertises input value for the form
                ExpertisesInputValue = string.Join(", ", Mission.RequiredExpertises ?? new List<string>());
                
                return Page();
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
        
        private ExtractedInformation ExtractInformationFromInput(string input)
        {
            var info = new ExtractedInformation();
            
            // Extract title if explicitly mentioned
            var titleRegex = new Regex(@"(?:recherche|cherche|besoin d[e']un|besoin d[e']une)\s+([^,\.]+)", RegexOptions.IgnoreCase);
            var titleMatch = titleRegex.Match(input);
            if (titleMatch.Success && titleMatch.Groups.Count > 1)
            {
                info.Title = titleMatch.Groups[1].Value.Trim();
            }
            
            // Extract city with improved pattern matching
            var moroccanCities = new Dictionary<string, List<string>> {
                { "Casablanca", new List<string> { "casablanca", "casa" } },
                { "Rabat", new List<string> { "rabat" } },
                { "Marrakech", new List<string> { "marrakech", "marrakesh" } },
                { "Fès", new List<string> { "fès", "fes" } },
                { "Tanger", new List<string> { "tanger", "tangier" } },
                { "Agadir", new List<string> { "agadir" } },
                { "Meknès", new List<string> { "meknès", "meknes" } },
                { "Oujda", new List<string> { "oujda" } },
                { "Kénitra", new List<string> { "kénitra", "kenitra" } },
                { "Tétouan", new List<string> { "tétouan", "tetouan" } },
                { "Safi", new List<string> { "safi" } },
                { "Mohammedia", new List<string> { "mohammedia" } },
                { "El Jadida", new List<string> { "el jadida", "eljadida" } },
                { "Béni Mellal", new List<string> { "béni mellal", "beni mellal" } },
                { "Nador", new List<string> { "nador" } },
                { "Khémisset", new List<string> { "khémisset", "khemisset" } },
                { "Taza", new List<string> { "taza" } },
                { "Settat", new List<string> { "settat" } }
            };
            
            foreach (var city in moroccanCities)
            {
                foreach (var variant in city.Value)
                {
                    if (Regex.IsMatch(input, $@"\b{variant}\b", RegexOptions.IgnoreCase))
                    {
                        info.City = city.Key;
                        break;
                    }
                }
                if (info.City != "Casablanca") // If we found a match, stop searching
                    break;
            }
            
            // Extract country with improved pattern matching
            var countries = new Dictionary<string, List<string>> {
                { "Maroc", new List<string> { "maroc", "morocco", "marocain", "marocaine" } },
                { "France", new List<string> { "france", "français", "française" } },
                { "Belgique", new List<string> { "belgique", "belge" } },
                { "Suisse", new List<string> { "suisse" } },
                { "Canada", new List<string> { "canada", "canadien", "canadienne" } },
                { "Tunisie", new List<string> { "tunisie", "tunisien", "tunisienne" } },
                { "Algérie", new List<string> { "algérie", "algerie", "algérien", "algerien" } }
            };
            
            foreach (var country in countries)
            {
                foreach (var variant in country.Value)
                {
                    if (Regex.IsMatch(input, $@"\b{variant}\b", RegexOptions.IgnoreCase))
                    {
                        info.Country = country.Key;
                        break;
                    }
                }
                if (info.Country != "Maroc") // If we found a non-default match, stop searching
                    break;
            }
            
            // Extract work mode with improved pattern matching
            var workModes = new Dictionary<string, List<string>> {
                { "REMOTE", new List<string> { "remote", "télétravail", "teletravail", "à distance", "a distance", "en ligne" } },
                { "ONSITE", new List<string> { "onsite", "sur site", "sur place", "présentiel", "presentiel", "bureau" } },
                { "HYBRID", new List<string> { "hybrid", "hybride", "mixte", "semi-présentiel", "semi-presentiel" } }
            };
            
            foreach (var mode in workModes)
            {
                foreach (var variant in mode.Value)
                {
                    if (Regex.IsMatch(input, $@"\b{variant}\b", RegexOptions.IgnoreCase))
                    {
                        info.WorkMode = mode.Key;
                        break;
                    }
                }
                if (info.WorkMode != "REMOTE") // If we found a non-default match, stop searching
                    break;
            }
            
            // Extract salary and currency with improved pattern matching
            var salaryRegex = new Regex(@"(\d+(?:[.,]\d+)?(?:\s*k)?)\s*(?:dh|mad|dirhams|euros?|eur|€|\$|usd|dollars?)", RegexOptions.IgnoreCase);
            var salaryMatch = salaryRegex.Match(input);
            
            if (salaryMatch.Success)
            {
                string salaryStr = salaryMatch.Groups[1].Value.Replace(" ", "").Replace(",", ".");
                
                // Handle 'k' suffix (thousands)
                if (salaryStr.EndsWith("k", StringComparison.OrdinalIgnoreCase))
                {
                    salaryStr = salaryStr.Substring(0, salaryStr.Length - 1);
                    if (decimal.TryParse(salaryStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal salaryValue))
                    {
                        info.Salary = salaryValue * 1000;
                    }
                }
                else if (decimal.TryParse(salaryStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal salary))
                {
                    info.Salary = salary;
                }
                
                // Extract currency
                string fullMatch = salaryMatch.Value.ToLowerInvariant();
                if (fullMatch.Contains("dh") || fullMatch.Contains("mad") || fullMatch.Contains("dirham"))
                {
                    info.Currency = "DH";
                }
                else if (fullMatch.Contains("euro") || fullMatch.Contains("eur") || fullMatch.Contains("€"))
                {
                    info.Currency = "EUR";
                }
                else if (fullMatch.Contains("$") || fullMatch.Contains("usd") || fullMatch.Contains("dollar"))
                {
                    info.Currency = "USD";
                }
            }
            
            // Extract contract type with improved pattern matching
            var contractTypes = new Dictionary<string, List<string>> {
                { "FORFAIT", new List<string> { "forfait", "projet", "project", "fixed price", "prix fixe" } },
                { "REGIE", new List<string> { "régie", "regie", "time and material", "temps passé", "temps passe" } }
            };
            
            foreach (var type in contractTypes)
            {
                foreach (var variant in type.Value)
                {
                    if (Regex.IsMatch(input, $@"\b{variant}\b", RegexOptions.IgnoreCase))
                    {
                        info.ContractType = type.Key;
                        break;
                    }
                }
                if (info.ContractType != "REGIE") // If we found a non-default match, stop searching
                    break;
            }
            
            // Extract duration with improved pattern matching
            var durationRegex = new Regex(@"(\d+)\s*(?:mois|month|months|ans?|year|years|années?|semaines?|week|weeks|jours?|day|days)", RegexOptions.IgnoreCase);
            var durationMatch = durationRegex.Match(input);
            
            if (durationMatch.Success)
            {
                if (int.TryParse(durationMatch.Groups[1].Value, out int duration))
                {
                    info.Duration = duration;
                    
                    string unit = durationMatch.Value.ToLowerInvariant();
                    if (unit.Contains("mois") || unit.Contains("month"))
                    {
                        info.DurationType = "MONTH";
                    }
                    else if (unit.Contains("an") || unit.Contains("année") || unit.Contains("year"))
                    {
                        info.DurationType = "YEAR";
                    }
                    else if (unit.Contains("semaine") || unit.Contains("week"))
                    {
                        // Convert weeks to months (approximately)
                        info.DurationType = "MONTH";
                        info.Duration = Math.Max(1, duration / 4); // Rough approximation: 4 weeks = 1 month
                    }
                    else if (unit.Contains("jour") || unit.Contains("day"))
                    {
                        // Convert days to months (approximately)
                        info.DurationType = "MONTH";
                        info.Duration = Math.Max(1, duration / 30); // Rough approximation: 30 days = 1 month
                    }
                }
            }
            
            // Extract experience level with improved pattern matching
            var experienceLevels = new Dictionary<string, List<string>> {
                { "0-3", new List<string> { "junior", "débutant", "debutant", "entry level", "entry-level", "0-3", "0 à 3", "0 a 3" } },
                { "3-7", new List<string> { "intermédiaire", "intermediaire", "mid level", "mid-level", "3-7", "3 à 7", "3 a 7" } },
                { "7-12", new List<string> { "senior", "expérimenté", "experimente", "7-12", "7 à 12", "7 a 12" } },
                { "12+", new List<string> { "expert", "lead", "architecte", "architect", "12+", "plus de 12", "plus que 12" } }
            };
            
            foreach (var level in experienceLevels)
            {
                foreach (var variant in level.Value)
                {
                    if (Regex.IsMatch(input, $@"\b{variant}\b", RegexOptions.IgnoreCase))
                    {
                        info.Experience = level.Key;
                        break;
                    }
                }
                if (info.Experience != "3-7") // If we found a non-default match, stop searching
                    break;
            }
            
            // Extract position and domain with improved pattern matching
            var techRoles = new Dictionary<string, List<string>> {
                { "Frontend", new List<string> { "frontend", "front-end", "front end", "react", "angular", "vue", "javascript", "typescript", "html", "css", "ui", "ux", "interface" } },
                { "Backend", new List<string> { "backend", "back-end", "back end", "node", "express", "php", "laravel", "symfony", "python", "django", "flask", "java", "spring", "c#", ".net", "asp.net", "ruby", "rails", "api" } },
                { "Fullstack", new List<string> { "fullstack", "full-stack", "full stack", "mern", "mean", "polyvalent" } },
                { "Mobile", new List<string> { "mobile", "react native", "flutter", "swift", "kotlin", "android", "ios", "app", "application mobile" } },
                { "DevOps", new List<string> { "devops", "aws", "azure", "gcp", "docker", "kubernetes", "jenkins", "ci/cd", "terraform", "ansible", "cloud", "infrastructure" } },
                { "Data", new List<string> { "data", "sql", "mysql", "postgresql", "mongodb", "nosql", "big data", "hadoop", "spark", "etl", "bi", "business intelligence", "data science", "machine learning", "ml", "ai", "intelligence artificielle" } },
                { "Sécurité", new List<string> { "sécurité", "securite", "security", "pentest", "cybersécurité", "cybersecurite", "cybersecurity" } },
                { "CMS", new List<string> { "cms", "wordpress", "drupal", "joomla", "magento", "shopify", "prestashop" } }
            };
            
            // Look for specific job titles first
            var jobTitles = new Dictionary<string, string> {
                { @"\b(?:développeur|developer|dev)\s+(?:front[- ]?end|frontend)\b", "Développeur Frontend" },
                { @"\b(?:développeur|developer|dev)\s+(?:back[- ]?end|backend)\b", "Développeur Backend" },
                { @"\b(?:développeur|developer|dev)\s+(?:full[- ]?stack|fullstack)\b", "Développeur Fullstack" },
                { @"\b(?:développeur|developer|dev)\s+(?:mobile)\b", "Développeur Mobile" },
                { @"\b(?:ingénieur|engineer)\s+(?:dev ?ops)\b", "Ingénieur DevOps" },
                { @"\b(?:data scientist|data analyst|analyste de données)\b", "Data Scientist" },
                { @"\b(?:expert|ingénieur|engineer)\s+(?:sécurité|securite|security)\b", "Expert en Sécurité" },
                { @"\b(?:développeur|developer|dev)\s+(?:wordpress|drupal|cms)\b", "Développeur CMS" }
            };
            
            foreach (var title in jobTitles)
            {
                if (Regex.IsMatch(input, title.Key, RegexOptions.IgnoreCase))
                {
                    info.Position = title.Value;
                    break;
                }
            }
            
            // If no specific job title was found, try to determine domain and position
            if (string.IsNullOrEmpty(info.Position))
            {
                foreach (var role in techRoles)
                {
                    foreach (var keyword in role.Value)
                    {
                        if (Regex.IsMatch(input, $@"\b{keyword}\b", RegexOptions.IgnoreCase))
                        {
                            info.Domain = $"Développement {role.Key}";
                            info.Position = $"Développeur {role.Key}";
                            
                            // Add the keyword to expertises if it's a specific technology
                            if (keyword != role.Key.ToLower() && 
                                !keyword.Contains("end") && 
                                !keyword.Contains("stack") &&
                                !keyword.Contains("développement") &&
                                !keyword.Contains("development"))
                            {
                                AddExpertiseIfNotExists(info.Expertises, keyword);
                            }
                            break;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(info.Domain))
                        break;
                }
            }
            
            // Extract specific technologies for expertises with improved pattern matching
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
                // Use word boundary to match whole words
                if (Regex.IsMatch(input, $@"\b{Regex.Escape(tech)}\b", RegexOptions.IgnoreCase))
                {
                    AddExpertiseIfNotExists(info.Expertises, tech);
                }
            }
            
            return info;
        }
        
        private void AddExpertiseIfNotExists(List<string> expertises, string expertise)
        {
            // Normalize expertise name (proper casing for common technologies)
            string normalizedExpertise = NormalizeExpertiseName(expertise);
            
            // Check if it already exists (case-insensitive)
            if (!expertises.Contains(normalizedExpertise, StringComparer.OrdinalIgnoreCase))
            {
                expertises.Add(normalizedExpertise);
            }
        }
        
        private string NormalizeExpertiseName(string expertise)
        {
            // Dictionary of common technologies with their proper casing
            var techCasing = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "react", "React" },
                { "angular", "Angular" },
                { "vue", "Vue.js" },
                { "javascript", "JavaScript" },
                { "typescript", "TypeScript" },
                { "node.js", "Node.js" },
                { "node", "Node.js" },
                { "express", "Express.js" },
                { "php", "PHP" },
                { "laravel", "Laravel" },
                { "symfony", "Symfony" },
                { "python", "Python" },
                { "django", "Django" },
                { "flask", "Flask" },
                { "java", "Java" },
                { "spring", "Spring" },
                { "c#", "C#" },
                { ".net", ".NET" },
                { "asp.net", "ASP.NET" },
                { "ruby", "Ruby" },
                { "rails", "Ruby on Rails" },
                { "html", "HTML" },
                { "css", "CSS" },
                { "sass", "SASS" },
                { "less", "LESS" },
                { "sql", "SQL" },
                { "mysql", "MySQL" },
                { "postgresql", "PostgreSQL" },
                { "mongodb", "MongoDB" },
                { "firebase", "Firebase" },
                { "aws", "AWS" },
                { "azure", "Azure" },
                { "docker", "Docker" },
                { "kubernetes", "Kubernetes" },
                { "devops", "DevOps" },
                { "ci/cd", "CI/CD" },
                { "git", "Git" },
                { "react native", "React Native" },
                { "flutter", "Flutter" },
                { "swift", "Swift" },
                { "kotlin", "Kotlin" },
                { "android", "Android" },
                { "ios", "iOS" },
                { "wordpress", "WordPress" },
                { "shopify", "Shopify" },
                { "magento", "Magento" },
                { "drupal", "Drupal" }
            };
            
            // Return the properly cased version if it exists, otherwise return the original
            return techCasing.TryGetValue(expertise, out string properCasing) ? properCasing : expertise;
        }
    }
}
