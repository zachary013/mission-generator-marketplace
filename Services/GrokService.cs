using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using SmartMarketplace.Models;

namespace SmartMarketplace.Services
{
    public class GrokService : IGrokService
    {
        private readonly HttpClient _httpClient;
        private readonly IPromptService _promptService;
        private readonly IMissionTemplateService _templateService;
        private const string ApiUrl = "https://api.x.ai/v1/chat/completions";
        private const string ApiKey = "xai-y7lIUR23aSbFKStNYfGqdnW56WwfQTEDxghnEIPpXfvNE8qnWaIs8hbAemsNKGCeYTLON1vOnUOyxboZ";

        public GrokService(HttpClient httpClient, IPromptService promptService, IMissionTemplateService templateService)
        {
            _httpClient = httpClient;
            _promptService = promptService;
            _templateService = templateService;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> CallGrokApiAsync(string prompt)
        {
            try
            {
                // Create request payload
                var requestData = new
                {
                    model = "grok-3",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000
                };

                // Serialize the request data to JSON
                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send the request to the API
                var response = await _httpClient.PostAsync(ApiUrl, content);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read and parse the response
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(jsonResponse);
                    
                    // Extract the response text from choices[0].message.content
                    var root = document.RootElement;
                    if (root.TryGetProperty("choices", out var choices) && 
                        choices.GetArrayLength() > 0 &&
                        choices[0].TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var messageContent))
                    {
                        return messageContent.GetString();
                    }
                    
                    return "No valid response content found";
                }
                else
                {
                    // Handle unsuccessful response
                    return $"Error calling Grok API: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                return $"Error calling Grok API: {ex.Message}";
            }
        }
        
        public async Task<Mission> GenerateFullMissionAsync(string simpleInput, ExtractedInformation extractedInfo)
        {
            try
            {
                // Build a sophisticated prompt based on the domain and context
                string prompt = BuildIntelligentPrompt(simpleInput, extractedInfo);

                // Call the API
                string grokResponse = await CallGrokApiAsync(prompt);
                
                // Check if response is valid
                if (!string.IsNullOrWhiteSpace(grokResponse) && !grokResponse.StartsWith("Error"))
                {
                    try
                    {
                        // Extract JSON from response (in case there's additional text)
                        string jsonContent = ExtractJsonFromResponse(grokResponse);
                        
                        // Deserialize the JSON response
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var generatedMission = JsonSerializer.Deserialize<Mission>(jsonContent, options);
                        
                        // Ensure the extracted values are preserved and validate the result
                        if (generatedMission != null)
                        {
                            // Override with extracted values to ensure they're preserved
                            generatedMission.Country = extractedInfo.Country;
                            generatedMission.City = extractedInfo.City;
                            generatedMission.WorkMode = extractedInfo.WorkMode;
                            generatedMission.Duration = extractedInfo.Duration;
                            generatedMission.DurationType = extractedInfo.DurationType;
                            generatedMission.EstimatedDailyRate = extractedInfo.Salary;
                            generatedMission.ContractType = extractedInfo.ContractType;
                            generatedMission.ExperienceYear = extractedInfo.Experience;
                            
                            // Only override these if they were explicitly extracted
                            if (!string.IsNullOrEmpty(extractedInfo.Title))
                                generatedMission.Title = extractedInfo.Title;
                                
                            if (!string.IsNullOrEmpty(extractedInfo.Position))
                                generatedMission.Position = extractedInfo.Position;
                                
                            if (!string.IsNullOrEmpty(extractedInfo.Domain))
                                generatedMission.Domain = extractedInfo.Domain;
                                
                            // Ensure StartImmediately is set correctly
                            if (generatedMission.StartImmediately && generatedMission.StartDate.HasValue)
                            {
                                generatedMission.StartDate = null;
                            }
                            
                            // Validate and enhance the generated mission
                            ValidateAndEnhanceMission(generatedMission, extractedInfo);
                            
                            return generatedMission;
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"JSON parsing error: {ex.Message}");
                    }
                }
                
                // Fallback to a basic mission if API call fails or returns invalid JSON
                return GenerateIntelligentFallbackMission(simpleInput, extractedInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateFullMissionAsync: {ex.Message}");
                return GenerateIntelligentFallbackMission(simpleInput, extractedInfo);
            }
        }

        private string BuildIntelligentPrompt(string simpleInput, ExtractedInformation extractedInfo)
        {
            // Déterminer le domaine à partir des informations extraites
            var domain = ExtractDomainFromInfo(extractedInfo);
            
            // Utiliser le service de prompts pour générer un prompt spécialisé
            return _promptService.GeneratePrompt(domain, extractedInfo, simpleInput);
        }
        
        private string ExtractDomainFromInfo(ExtractedInformation info)
        {
            if (!string.IsNullOrEmpty(info.Domain))
            {
                // Extraire le domaine de "Développement Backend" -> "Backend"
                var parts = info.Domain.Split(' ');
                if (parts.Length > 1)
                {
                    return parts[1]; // "Backend", "Frontend", etc.
                }
            }
            
            if (!string.IsNullOrEmpty(info.Position))
            {
                if (info.Position.Contains("Backend")) return "Backend";
                if (info.Position.Contains("Frontend")) return "Frontend";
                if (info.Position.Contains("Mobile")) return "Mobile";
                if (info.Position.Contains("DevOps")) return "DevOps";
                if (info.Position.Contains("Fullstack")) return "Fullstack";
            }
            
            return "Backend"; // Défaut
        }

        private string DetermineProjectContext(string input, ExtractedInformation extractedInfo)
        {
            var contexts = new Dictionary<string, string>
            {
                ["e-commerce"] = "Développement d'une plateforme e-commerce moderne avec gestion des commandes, paiements et inventaire",
                ["marketplace"] = "Création d'une marketplace multi-vendeurs avec système de commission et gestion des transactions",
                ["fintech"] = "Application fintech sécurisée avec intégration bancaire et conformité réglementaire",
                ["startup"] = "Projet innovant en phase de croissance nécessitant une architecture scalable",
                ["entreprise"] = "Solution d'entreprise robuste avec intégration aux systèmes existants",
                ["mobile"] = "Application mobile native/hybride avec expérience utilisateur optimisée",
                ["web"] = "Application web moderne avec interface responsive et performance optimisée",
                ["api"] = "Développement d'API RESTful/GraphQL avec documentation complète",
                ["devops"] = "Mise en place d'infrastructure cloud et pipeline CI/CD automatisé",
                ["data"] = "Solution de traitement et analyse de données avec tableaux de bord interactifs"
            };

            string lowerInput = input.ToLower();
            foreach (var context in contexts)
            {
                if (lowerInput.Contains(context.Key))
                {
                    return context.Value;
                }
            }

            // Default context based on domain
            if (!string.IsNullOrEmpty(extractedInfo.Domain))
            {
                if (extractedInfo.Domain.Contains("Mobile"))
                    return contexts["mobile"];
                if (extractedInfo.Domain.Contains("DevOps"))
                    return contexts["devops"];
                if (extractedInfo.Domain.Contains("Data"))
                    return contexts["data"];
            }

            return "Projet de développement logiciel nécessitant une expertise technique approfondie";
        }

        private string GetRoleSpecificRequirements(ExtractedInformation extractedInfo)
        {
            var requirements = new Dictionary<string, string>
            {
                ["Frontend"] = "Maîtrise des frameworks modernes, responsive design, optimisation des performances, tests unitaires",
                ["Backend"] = "Architecture API, sécurité, optimisation base de données, scalabilité, tests d'intégration",
                ["Fullstack"] = "Vision complète du produit, coordination front/back, architecture système, polyvalence technique",
                ["Mobile"] = "Développement natif/hybride, publication stores, optimisation mobile, tests sur devices",
                ["DevOps"] = "Infrastructure as Code, monitoring, sécurité, automatisation, cloud computing",
                ["Data"] = "Modélisation données, ETL, visualisation, machine learning, big data technologies"
            };

            if (!string.IsNullOrEmpty(extractedInfo.Position))
            {
                foreach (var req in requirements)
                {
                    if (extractedInfo.Position.Contains(req.Key))
                    {
                        return req.Value;
                    }
                }
            }

            return "Expertise technique solide, autonomie, capacité d'analyse et de résolution de problèmes";
        }

        private string GetIndustryContext(string input)
        {
            var industries = new Dictionary<string, string>
            {
                ["banque"] = "Secteur bancaire avec contraintes de sécurité et conformité réglementaire strictes",
                ["assurance"] = "Industrie de l'assurance nécessitant fiabilité et gestion des risques",
                ["retail"] = "Commerce de détail avec focus sur l'expérience client et la performance",
                ["santé"] = "Domaine médical avec respect des normes RGPD et sécurité des données",
                ["éducation"] = "Secteur éducatif avec interface intuitive et accessibilité",
                ["logistique"] = "Supply chain et logistique avec optimisation des processus",
                ["immobilier"] = "Marché immobilier avec géolocalisation et visualisation avancée"
            };

            string lowerInput = input.ToLower();
            foreach (var industry in industries)
            {
                if (lowerInput.Contains(industry.Key))
                {
                    return industry.Value;
                }
            }

            return "Environnement professionnel dynamique avec standards de qualité élevés";
        }

        private void ValidateAndEnhanceMission(Mission mission, ExtractedInformation extractedInfo)
        {
            // Ensure minimum expertise count
            if (mission.RequiredExpertises == null || mission.RequiredExpertises.Count < 3)
            {
                mission.RequiredExpertises = EnhanceExpertisesList(extractedInfo);
            }

            // Validate title length and quality
            if (string.IsNullOrEmpty(mission.Title) || mission.Title.Length < 10)
            {
                mission.Title = GenerateIntelligentTitle(extractedInfo);
            }

            // Ensure description is substantial
            if (string.IsNullOrEmpty(mission.Description) || mission.Description.Length < 200)
            {
                mission.Description = GenerateIntelligentDescription(extractedInfo);
            }
        }

        private List<string> EnhanceExpertisesList(ExtractedInformation extractedInfo)
        {
            var expertises = new List<string>(extractedInfo.Expertises);
            
            // Add complementary technologies based on existing ones
            var complementaryTechs = new Dictionary<string, List<string>>
            {
                ["React"] = new List<string> { "Redux", "React Router", "Jest" },
                ["Angular"] = new List<string> { "TypeScript", "RxJS", "Angular Material" },
                ["Vue.js"] = new List<string> { "Vuex", "Vue Router", "Nuxt.js" },
                ["Node.js"] = new List<string> { "Express.js", "MongoDB", "JWT" },
                ["PHP"] = new List<string> { "MySQL", "Composer", "PHPUnit" },
                ["Python"] = new List<string> { "Django", "PostgreSQL", "Redis" },
                ["DevOps"] = new List<string> { "Docker", "Kubernetes", "Jenkins", "AWS" },
                ["Mobile"] = new List<string> { "React Native", "Firebase", "App Store" }
            };

            foreach (var expertise in extractedInfo.Expertises)
            {
                if (complementaryTechs.ContainsKey(expertise))
                {
                    foreach (var complementary in complementaryTechs[expertise])
                    {
                        if (!expertises.Contains(complementary))
                        {
                            expertises.Add(complementary);
                        }
                    }
                }
            }

            // Ensure minimum of 4 expertises
            if (expertises.Count < 4)
            {
                var defaultTechs = new List<string> { "Git", "Agile", "REST API", "Responsive Design" };
                foreach (var tech in defaultTechs)
                {
                    if (!expertises.Contains(tech) && expertises.Count < 6)
                    {
                        expertises.Add(tech);
                    }
                }
            }

            return expertises;
        }

        private string GenerateIntelligentTitle(ExtractedInformation extractedInfo)
        {
            var titleTemplates = new List<string>
            {
                $"{extractedInfo.Position} - {extractedInfo.City} ({extractedInfo.WorkMode})",
                $"Mission {extractedInfo.Position} - {extractedInfo.Duration} {(extractedInfo.DurationType == "MONTH" ? "mois" : "ans")}",
                $"{extractedInfo.Position} Expérimenté - Projet Innovant",
                $"Développeur {string.Join("/", extractedInfo.Expertises.Take(2))} - {extractedInfo.City}"
            };

            return titleTemplates[new Random().Next(titleTemplates.Count)];
        }

        private string GenerateIntelligentDescription(ExtractedInformation extractedInfo)
        {
            return $@"Nous recherchons un {extractedInfo.Position} expérimenté pour rejoindre notre équipe sur un projet stratégique.

🎯 CONTEXTE DU PROJET :
Développement d'une solution innovante nécessitant une expertise technique approfondie et une approche méthodique.

💼 VOS MISSIONS :
• Conception et développement de fonctionnalités complexes
• Collaboration étroite avec l'équipe produit et design
• Optimisation des performances et de la sécurité
• Participation aux revues de code et à l'amélioration continue
• Documentation technique et formation des équipes

🛠️ STACK TECHNIQUE :
{string.Join(", ", extractedInfo.Expertises)}

📍 MODALITÉS :
• Localisation : {extractedInfo.City}, {extractedInfo.Country}
• Mode : {extractedInfo.WorkMode}
• Durée : {extractedInfo.Duration} {(extractedInfo.DurationType == "MONTH" ? "mois" : "ans")}
• Expérience requise : {extractedInfo.Experience} ans
• Type de contrat : {extractedInfo.ContractType}

Rejoignez-nous pour contribuer à un projet d'envergure dans un environnement technique stimulant !";
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
        
        private Mission GenerateIntelligentFallbackMission(string simpleInput, ExtractedInformation extractedInfo)
        {
            // Déterminer le domaine
            var domain = ExtractDomainFromInfo(extractedInfo);
            
            // Utiliser le service de templates pour générer une mission de qualité
            var template = _templateService.GetTemplate(domain, extractedInfo.Experience);
            
            // Générer le titre avec le template
            var title = GenerateTitleFromTemplate(template, extractedInfo);
            
            // Générer la description avec le template
            var description = _templateService.GenerateContextualDescription(template, extractedInfo);
            
            // Obtenir les technologies spécifiques au domaine
            var expertises = _templateService.GetDomainSpecificTechnologies(domain);
            
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
                RequiredExpertises = expertises
            };
        }
        
        private string GenerateTitleFromTemplate(MissionTemplate template, ExtractedInformation info)
        {
            var titleTemplate = template.TitleTemplate;
            
            // Remplacer les placeholders
            titleTemplate = titleTemplate.Replace("{tech}", string.Join("/", info.Expertises.Take(2)));
            titleTemplate = titleTemplate.Replace("{city}", info.City);
            
            // Si le template est trop générique, créer un titre personnalisé
            if (titleTemplate.Length < 20)
            {
                var mainTech = info.Expertises.FirstOrDefault() ?? template.CoreTechnologies.FirstOrDefault();
                return $"{info.Position} {mainTech} - {info.City} ({info.WorkMode})";
            }
            
            return titleTemplate;
        }
    }
}
