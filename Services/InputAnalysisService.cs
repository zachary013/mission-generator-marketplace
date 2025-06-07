using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using SmartMarketplace.Models;

namespace SmartMarketplace.Services
{
    public class InputAnalysisService : IInputAnalysisService
    {
        private readonly Dictionary<string, ProjectContext> _projectContexts;
        
        public InputAnalysisService()
        {
            _projectContexts = InitializeProjectContexts();
        }
        
        public ExtractedInformation AnalyzeInput(string input)
        {
            var info = new ExtractedInformation();
            
            // Analyse prioritaire du domaine technique
            var detectedDomain = DetectTechnicalDomain(input);
            
            // Extract basic information
            ExtractLocationInfo(input, info);
            ExtractSalaryInfo(input, info);
            ExtractDurationInfo(input, info);
            ExtractWorkModeInfo(input, info);
            ExtractExperienceInfo(input, info);
            ExtractContractTypeInfo(input, info);
            
            // Extract role and technology information based on detected domain
            ExtractRoleAndTechInfo(input, info, detectedDomain);
            
            // Set domain and position based on detection
            info.Domain = $"Développement {detectedDomain}";
            info.Position = GetPositionTitle(detectedDomain);
            
            // Enhance with domain-specific context
            EnhanceWithDomainContext(info, detectedDomain);
            
            return info;
        }
        
        private string DetectTechnicalDomain(string input)
        {
            var lowerInput = input.ToLower();
            
            // Détection explicite par mots-clés de domaine
            var domainKeywords = new Dictionary<string, List<string>>
            {
                ["Backend"] = new List<string> 
                { 
                    "backend", "back-end", "api", "serveur", "server", "base de données", 
                    "database", "node.js", "express", "php", "laravel", "python", "django",
                    "java", "spring", ".net", "c#", "postgresql", "mongodb", "mysql"
                },
                ["Frontend"] = new List<string> 
                { 
                    "frontend", "front-end", "interface", "ui", "ux", "react", "vue", 
                    "angular", "javascript", "typescript", "html", "css", "sass", 
                    "responsive", "design", "web design"
                },
                ["Mobile"] = new List<string> 
                { 
                    "mobile", "app", "application mobile", "ios", "android", 
                    "react native", "flutter", "swift", "kotlin", "xamarin"
                },
                ["DevOps"] = new List<string> 
                { 
                    "devops", "dev ops", "infrastructure", "cloud", "aws", "azure", 
                    "docker", "kubernetes", "jenkins", "ci/cd", "deployment", "terraform"
                },
                ["Fullstack"] = new List<string> 
                { 
                    "fullstack", "full-stack", "full stack", "polyvalent", "complet"
                }
            };
            
            // Score par domaine
            var domainScores = new Dictionary<string, int>();
            
            foreach (var domain in domainKeywords)
            {
                int score = 0;
                foreach (var keyword in domain.Value)
                {
                    if (lowerInput.Contains(keyword))
                    {
                        // Score plus élevé pour les mots-clés plus spécifiques
                        score += keyword.Length > 5 ? 3 : 1;
                    }
                }
                domainScores[domain.Key] = score;
            }
            
            // Retourner le domaine avec le score le plus élevé
            var bestDomain = domainScores.OrderByDescending(x => x.Value).FirstOrDefault();
            
            if (bestDomain.Value > 0)
            {
                return bestDomain.Key;
            }
            
            // Fallback : analyser le contexte général
            if (lowerInput.Contains("web") || lowerInput.Contains("site"))
                return "Frontend";
            if (lowerInput.Contains("serveur") || lowerInput.Contains("données"))
                return "Backend";
                
            return "Backend"; // Défaut
        }
        
        public string DetermineProjectType(string input)
        {
            var lowerInput = input.ToLower();
            
            foreach (var context in _projectContexts)
            {
                var keywords = GetProjectKeywords(context.Key);
                if (keywords.Any(keyword => lowerInput.Contains(keyword)))
                {
                    return context.Key;
                }
            }
            
            return "web"; // Default
        }
        
        public string GenerateContextualPrompt(string input, ExtractedInformation extractedInfo)
        {
            var projectType = DetermineProjectType(input);
            var context = _projectContexts.GetValueOrDefault(projectType, _projectContexts["web"]);
            
            return $@"
Tu es un expert en recrutement IT spécialisé dans les projets {projectType}.

CONTEXTE DU PROJET : {context.Description}
COMPLEXITÉ : {context.Complexity}
INDUSTRIE : {context.Industry}

ANALYSE DE L'INPUT UTILISATEUR :
""{input}""

INFORMATIONS EXTRAITES :
- Localisation : {extractedInfo.City}, {extractedInfo.Country}
- Budget : {extractedInfo.Salary} {extractedInfo.Currency}
- Durée : {extractedInfo.Duration} {extractedInfo.DurationType}
- Mode : {extractedInfo.WorkMode}
- Expérience : {extractedInfo.Experience} ans
- Contrat : {extractedInfo.ContractType}
- Technologies détectées : {string.Join(", ", extractedInfo.Expertises)}

TECHNOLOGIES SUGGÉRÉES POUR CE TYPE DE PROJET :
{string.Join(", ", context.SuggestedTechnologies)}

INSTRUCTIONS :
1. Génère un titre accrocheur et professionnel
2. Crée une description détaillée incluant :
   - Contexte entreprise/startup
   - Défis techniques spécifiques au domaine {projectType}
   - Responsabilités précises
   - Stack technique moderne et pertinente
   - Critères de sélection adaptés au niveau d'expérience
3. Respecte EXACTEMENT les valeurs extraites (budget, ville, durée, etc.)
4. Ajoute des technologies complémentaires pertinentes
5. Adapte le ton selon le type de projet ({context.Complexity})

Retourne un JSON valide avec la structure demandée.";
        }
        
        private Dictionary<string, ProjectContext> InitializeProjectContexts()
        {
            return new Dictionary<string, ProjectContext>
            {
                ["e-commerce"] = new ProjectContext
                {
                    Type = "e-commerce",
                    Industry = "Commerce en ligne",
                    Complexity = "Élevée - Gestion des paiements, inventaire, sécurité",
                    SuggestedTechnologies = new List<string> { "React", "Node.js", "Stripe", "MongoDB", "Redis", "AWS" },
                    Description = "Plateforme e-commerce avec gestion complète des commandes, paiements sécurisés et expérience utilisateur optimisée"
                },
                ["fintech"] = new ProjectContext
                {
                    Type = "fintech",
                    Industry = "Services financiers",
                    Complexity = "Très élevée - Sécurité bancaire, conformité réglementaire",
                    SuggestedTechnologies = new List<string> { "Java", "Spring Security", "PostgreSQL", "Kafka", "Docker", "Kubernetes" },
                    Description = "Application fintech nécessitant une sécurité de niveau bancaire et une conformité réglementaire stricte"
                },
                ["marketplace"] = new ProjectContext
                {
                    Type = "marketplace",
                    Industry = "Plateforme multi-vendeurs",
                    Complexity = "Élevée - Gestion multi-utilisateurs, commissions, disputes",
                    SuggestedTechnologies = new List<string> { "React", "Node.js", "PostgreSQL", "Elasticsearch", "Redis", "Stripe" },
                    Description = "Marketplace connectant vendeurs et acheteurs avec système de commission et gestion des transactions"
                },
                ["mobile"] = new ProjectContext
                {
                    Type = "mobile",
                    Industry = "Applications mobiles",
                    Complexity = "Moyenne à élevée - Performance, UX native, stores",
                    SuggestedTechnologies = new List<string> { "React Native", "Flutter", "Firebase", "Push Notifications", "App Store" },
                    Description = "Application mobile native ou hybride avec expérience utilisateur optimisée et intégration stores"
                },
                ["saas"] = new ProjectContext
                {
                    Type = "saas",
                    Industry = "Software as a Service",
                    Complexity = "Élevée - Multi-tenant, scalabilité, abonnements",
                    SuggestedTechnologies = new List<string> { "React", "Node.js", "PostgreSQL", "Stripe", "AWS", "Docker" },
                    Description = "Solution SaaS multi-tenant avec gestion d'abonnements et architecture scalable"
                },
                ["web"] = new ProjectContext
                {
                    Type = "web",
                    Industry = "Développement web",
                    Complexity = "Moyenne - Interface moderne, responsive, performance",
                    SuggestedTechnologies = new List<string> { "React", "Vue.js", "Node.js", "Express", "MongoDB", "Git" },
                    Description = "Application web moderne avec interface responsive et architecture robuste"
                }
            };
        }
        
        private ProjectContext AnalyzeProjectContext(string input)
        {
            var projectType = DetermineProjectType(input);
            return _projectContexts.GetValueOrDefault(projectType, _projectContexts["web"]);
        }
        
        private List<string> GetProjectKeywords(string projectType)
        {
            var keywords = new Dictionary<string, List<string>>
            {
                ["e-commerce"] = new List<string> { "e-commerce", "boutique", "vente", "commande", "panier", "paiement", "shop" },
                ["fintech"] = new List<string> { "fintech", "banque", "finance", "paiement", "crypto", "blockchain", "trading" },
                ["marketplace"] = new List<string> { "marketplace", "plateforme", "vendeur", "acheteur", "commission", "multi-vendeur" },
                ["mobile"] = new List<string> { "mobile", "app", "application", "android", "ios", "react native", "flutter" },
                ["saas"] = new List<string> { "saas", "abonnement", "subscription", "multi-tenant", "cloud", "service" },
                ["web"] = new List<string> { "web", "site", "interface", "frontend", "backend", "api" }
            };
            
            return keywords.GetValueOrDefault(projectType, new List<string>());
        }
        
        private void ExtractLocationInfo(string input, ExtractedInformation info)
        {
            var moroccanCities = new Dictionary<string, List<string>>
            {
                { "Casablanca", new List<string> { "casablanca", "casa" } },
                { "Rabat", new List<string> { "rabat" } },
                { "Marrakech", new List<string> { "marrakech", "marrakesh" } },
                { "Fès", new List<string> { "fès", "fes" } },
                { "Tanger", new List<string> { "tanger", "tangier" } },
                { "Agadir", new List<string> { "agadir" } },
                { "Meknès", new List<string> { "meknès", "meknes" } },
                { "Oujda", new List<string> { "oujda" } },
                { "Kénitra", new List<string> { "kénitra", "kenitra" } },
                { "Tétouan", new List<string> { "tétouan", "tetouan" } }
            };
            
            foreach (var city in moroccanCities)
            {
                foreach (var variant in city.Value)
                {
                    if (Regex.IsMatch(input, $@"\b{variant}\b", RegexOptions.IgnoreCase))
                    {
                        info.City = city.Key;
                        return;
                    }
                }
            }
        }
        
        private void ExtractSalaryInfo(string input, ExtractedInformation info)
        {
            var salaryPatterns = new List<string>
            {
                @"(\d+(?:[.,]\d+)?(?:\s*k)?)\s*(?:dh|mad|dirhams?)",
                @"(\d+(?:[.,]\d+)?(?:\s*k)?)\s*(?:euros?|eur|€)",
                @"(\d+(?:[.,]\d+)?(?:\s*k)?)\s*(?:\$|usd|dollars?)",
                @"(?:budget|tarif|prix|salaire).*?(\d+(?:[.,]\d+)?(?:\s*k)?)"
            };
            
            foreach (var pattern in salaryPatterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string salaryStr = match.Groups[1].Value.Replace(" ", "").Replace(",", ".");
                    
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
                    
                    // Determine currency
                    string fullMatch = match.Value.ToLowerInvariant();
                    if (fullMatch.Contains("dh") || fullMatch.Contains("mad") || fullMatch.Contains("dirham"))
                        info.Currency = "DH";
                    else if (fullMatch.Contains("euro") || fullMatch.Contains("eur") || fullMatch.Contains("€"))
                        info.Currency = "EUR";
                    else if (fullMatch.Contains("$") || fullMatch.Contains("usd") || fullMatch.Contains("dollar"))
                        info.Currency = "USD";
                    
                    break;
                }
            }
        }
        
        private void ExtractDurationInfo(string input, ExtractedInformation info)
        {
            var durationRegex = new Regex(@"(\d+)\s*(?:mois|month|months|ans?|year|years|années?)", RegexOptions.IgnoreCase);
            var match = durationRegex.Match(input);
            
            if (match.Success && int.TryParse(match.Groups[1].Value, out int duration))
            {
                info.Duration = duration;
                string unit = match.Value.ToLowerInvariant();
                info.DurationType = (unit.Contains("mois") || unit.Contains("month")) ? "MONTH" : "YEAR";
            }
        }
        
        private void ExtractWorkModeInfo(string input, ExtractedInformation info)
        {
            var workModes = new Dictionary<string, List<string>>
            {
                { "REMOTE", new List<string> { "remote", "télétravail", "teletravail", "à distance", "en ligne" } },
                { "ONSITE", new List<string> { "onsite", "sur site", "présentiel", "bureau" } },
                { "HYBRID", new List<string> { "hybrid", "hybride", "mixte", "semi-présentiel" } }
            };
            
            foreach (var mode in workModes)
            {
                if (mode.Value.Any(variant => Regex.IsMatch(input, $@"\b{variant}\b", RegexOptions.IgnoreCase)))
                {
                    info.WorkMode = mode.Key;
                    break;
                }
            }
        }
        
        private void ExtractExperienceInfo(string input, ExtractedInformation info)
        {
            var experienceLevels = new Dictionary<string, List<string>>
            {
                { "0-3", new List<string> { "junior", "débutant", "entry level", "0-3" } },
                { "3-7", new List<string> { "intermédiaire", "mid level", "3-7" } },
                { "7-12", new List<string> { "senior", "expérimenté", "7-12" } },
                { "12+", new List<string> { "expert", "lead", "architecte", "12+" } }
            };
            
            foreach (var level in experienceLevels)
            {
                if (level.Value.Any(variant => Regex.IsMatch(input, $@"\b{variant}\b", RegexOptions.IgnoreCase)))
                {
                    info.Experience = level.Key;
                    break;
                }
            }
        }
        
        private void ExtractContractTypeInfo(string input, ExtractedInformation info)
        {
            var contractTypes = new Dictionary<string, List<string>>
            {
                { "FORFAIT", new List<string> { "forfait", "projet", "fixed price", "prix fixe" } },
                { "REGIE", new List<string> { "régie", "regie", "time and material", "temps passé" } }
            };
            
            foreach (var type in contractTypes)
            {
                if (type.Value.Any(variant => Regex.IsMatch(input, $@"\b{variant}\b", RegexOptions.IgnoreCase)))
                {
                    info.ContractType = type.Key;
                    break;
                }
            }
        }
        
        private void ExtractRoleAndTechInfo(string input, ExtractedInformation info, string detectedDomain)
        {
            // Technologies spécifiques par domaine
            var domainTechnologies = new Dictionary<string, List<string>>
            {
                ["Backend"] = new List<string> 
                { 
                    "node.js", "express", "php", "laravel", "python", "django", 
                    "java", "spring", "c#", ".net", "postgresql", "mongodb", "mysql", "redis"
                },
                ["Frontend"] = new List<string> 
                { 
                    "react", "vue", "angular", "javascript", "typescript", "html", "css", 
                    "sass", "redux", "webpack", "bootstrap", "tailwind"
                },
                ["Mobile"] = new List<string> 
                { 
                    "react native", "flutter", "swift", "kotlin", "firebase", "expo"
                },
                ["DevOps"] = new List<string> 
                { 
                    "docker", "kubernetes", "aws", "azure", "jenkins", "terraform", "ansible"
                },
                ["Fullstack"] = new List<string> 
                { 
                    "react", "node.js", "mongodb", "express", "vue", "laravel"
                }
            };
            
            // Extraire les technologies pertinentes pour le domaine détecté
            var relevantTechs = domainTechnologies.GetValueOrDefault(detectedDomain, new List<string>());
            
            foreach (var tech in relevantTechs)
            {
                if (Regex.IsMatch(input, $@"\b{Regex.Escape(tech)}\b", RegexOptions.IgnoreCase))
                {
                    if (!info.Expertises.Contains(tech, StringComparer.OrdinalIgnoreCase))
                    {
                        info.Expertises.Add(NormalizeTechName(tech));
                    }
                }
            }
            
            // Si aucune technologie détectée, ajouter les technologies par défaut du domaine
            if (info.Expertises.Count == 0)
            {
                var defaultTechs = GetDefaultTechnologies(detectedDomain);
                info.Expertises.AddRange(defaultTechs.Take(3));
            }
        }
        
        private string GetPositionTitle(string domain)
        {
            var titles = new Dictionary<string, string>
            {
                ["Backend"] = "Développeur Backend",
                ["Frontend"] = "Développeur Frontend", 
                ["Mobile"] = "Développeur Mobile",
                ["DevOps"] = "Ingénieur DevOps",
                ["Fullstack"] = "Développeur Fullstack"
            };
            
            return titles.GetValueOrDefault(domain, "Développeur");
        }
        
        private List<string> GetDefaultTechnologies(string domain)
        {
            var defaultTechs = new Dictionary<string, List<string>>
            {
                ["Backend"] = new List<string> { "Node.js", "Express.js", "MongoDB", "PostgreSQL", "REST API" },
                ["Frontend"] = new List<string> { "React", "JavaScript", "HTML5", "CSS3", "TypeScript" },
                ["Mobile"] = new List<string> { "React Native", "Flutter", "Firebase", "Redux" },
                ["DevOps"] = new List<string> { "Docker", "Kubernetes", "AWS", "Jenkins", "CI/CD" },
                ["Fullstack"] = new List<string> { "React", "Node.js", "MongoDB", "Express.js", "TypeScript" }
            };
            
            return defaultTechs.GetValueOrDefault(domain, new List<string> { "Git", "Agile" });
        }
        
        private string NormalizeTechName(string tech)
        {
            var techCasing = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "node.js", "Node.js" },
                { "express", "Express.js" },
                { "react", "React" },
                { "vue", "Vue.js" },
                { "angular", "Angular" },
                { "javascript", "JavaScript" },
                { "typescript", "TypeScript" },
                { "mongodb", "MongoDB" },
                { "postgresql", "PostgreSQL" },
                { "mysql", "MySQL" },
                { "php", "PHP" },
                { "laravel", "Laravel" },
                { "python", "Python" },
                { "django", "Django" },
                { "java", "Java" },
                { "spring", "Spring" },
                { "c#", "C#" },
                { ".net", ".NET" },
                { "html", "HTML5" },
                { "css", "CSS3" },
                { "sass", "Sass" },
                { "redux", "Redux" },
                { "webpack", "Webpack" },
                { "docker", "Docker" },
                { "kubernetes", "Kubernetes" },
                { "aws", "AWS" },
                { "jenkins", "Jenkins" },
                { "terraform", "Terraform" },
                { "react native", "React Native" },
                { "flutter", "Flutter" },
                { "firebase", "Firebase" }
            };
            
            return techCasing.GetValueOrDefault(tech, tech);
        }
        
        private void EnhanceWithDomainContext(ExtractedInformation info, string domain)
        {
            // Ajuster le salaire si trop bas selon le domaine
            if (info.Salary <= 1000)
            {
                var domainSalaries = new Dictionary<string, decimal>
                {
                    ["Backend"] = 4000,
                    ["Frontend"] = 3500,
                    ["Mobile"] = 4500,
                    ["DevOps"] = 5000,
                    ["Fullstack"] = 4200
                };
                
                info.Salary = domainSalaries.GetValueOrDefault(domain, 3500);
            }
            
            // Assurer un minimum de technologies pertinentes
            if (info.Expertises.Count < 4)
            {
                var additionalTechs = GetDefaultTechnologies(domain);
                foreach (var tech in additionalTechs)
                {
                    if (!info.Expertises.Contains(tech, StringComparer.OrdinalIgnoreCase) && info.Expertises.Count < 6)
                    {
                        info.Expertises.Add(tech);
                    }
                }
            }
        }
        
        private string DetermineRoleFromContext(ProjectContext context, List<string> expertises)
        {
            var roleMapping = new Dictionary<string, string>
            {
                ["e-commerce"] = "Développeur E-commerce",
                ["fintech"] = "Développeur Fintech",
                ["marketplace"] = "Développeur Marketplace",
                ["mobile"] = "Développeur Mobile",
                ["saas"] = "Développeur SaaS",
                ["web"] = "Développeur Web"
            };
            
            return roleMapping.GetValueOrDefault(context.Type, "Développeur");
        }
        
        private void EnhanceWithContext(ExtractedInformation info, ProjectContext context)
        {
            // Add missing technologies based on context
            if (info.Expertises.Count < 3)
            {
                foreach (var tech in context.SuggestedTechnologies.Take(5))
                {
                    if (!info.Expertises.Contains(tech, StringComparer.OrdinalIgnoreCase))
                    {
                        info.Expertises.Add(tech);
                    }
                }
            }
            
            // Adjust salary based on complexity if not specified
            if (info.Salary <= 1000 && context.Complexity.Contains("Très élevée"))
            {
                info.Salary = 8000; // Higher rate for complex projects
            }
            else if (info.Salary <= 1000 && context.Complexity.Contains("Élevée"))
            {
                info.Salary = 6000;
            }
        }
    }
}
