using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SmartMarketplace.Models;

namespace SmartMarketplace.Services
{
    public class PromptService : IPromptService
    {
        private readonly Dictionary<string, List<PromptVariation>> _promptVariations;
        private readonly Random _random;
        
        public PromptService()
        {
            _random = new Random();
            _promptVariations = InitializePromptVariations();
        }
        
        public string GeneratePrompt(string domain, ExtractedInformation extractedInfo, string originalInput)
        {
            var variation = GetRandomPromptVariation(domain);
            var domainLower = domain.ToLower();
            
            return $@"
Tu es un expert en recrutement IT spécialisé en {domain}. 

ANALYSE DE L'INPUT UTILISATEUR :
""{originalInput}""

CONTRAINTES ABSOLUES À RESPECTER :
- Ville : {extractedInfo.City} (NE PAS CHANGER)
- Budget : {extractedInfo.Salary} {extractedInfo.Currency} (EXACT)
- Durée : {extractedInfo.Duration} {extractedInfo.DurationType}
- Mode : {extractedInfo.WorkMode}
- Domaine : {domain} (RESPECTER LE DOMAINE)

{GetDomainSpecificInstructions(domainLower)}

STYLE DE GÉNÉRATION : {variation}

INSTRUCTIONS PRÉCISES :
1. Titre : Créer un titre spécifique au domaine {domain}
2. Description : Utiliser le template spécialisé pour {domain}
3. Technologies : UNIQUEMENT celles pertinentes pour {domain}
4. Budget : UTILISER EXACTEMENT {extractedInfo.Salary} DH
5. Localisation : GARDER {extractedInfo.City}

TECHNOLOGIES AUTORISÉES POUR {domain.ToUpper()} :
{GetAllowedTechnologies(domainLower)}

Génère un JSON avec cette structure EXACTE :
{{
  ""title"": ""Titre spécialisé {domain}"",
  ""description"": ""Description détaillée avec contexte {domain}"",
  ""country"": ""{extractedInfo.Country}"",
  ""city"": ""{extractedInfo.City}"",
  ""workMode"": ""{extractedInfo.WorkMode}"",
  ""duration"": {extractedInfo.Duration},
  ""durationType"": ""{extractedInfo.DurationType}"",
  ""startImmediately"": true,
  ""experienceYear"": ""{extractedInfo.Experience}"",
  ""contractType"": ""{extractedInfo.ContractType}"",
  ""estimatedDailyRate"": {extractedInfo.Salary},
  ""domain"": ""Développement {domain}"",
  ""position"": ""Développeur {domain}"",
  ""requiredExpertises"": [""tech1"", ""tech2"", ""tech3""]
}}";
        }
        
        public string GetRandomPromptVariation(string domain)
        {
            var domainKey = domain.ToLower();
            if (_promptVariations.ContainsKey(domainKey))
            {
                var variations = _promptVariations[domainKey];
                var selectedVariation = variations[_random.Next(variations.Count)];
                return $"Style: {selectedVariation.Style}, Ton: {selectedVariation.Tone}, Focus: {selectedVariation.Focus}";
            }
            
            return "Style: Professionnel, Ton: Engageant, Focus: Technique";
        }
        
        private Dictionary<string, List<PromptVariation>> InitializePromptVariations()
        {
            return new Dictionary<string, List<PromptVariation>>
            {
                ["backend"] = new List<PromptVariation>
                {
                    new PromptVariation
                    {
                        Style = "Technique et précis",
                        Tone = "Professionnel",
                        Focus = "Architecture et performance",
                        Keywords = new List<string> { "API", "scalabilité", "architecture", "performance" }
                    },
                    new PromptVariation
                    {
                        Style = "Orienté projet",
                        Tone = "Dynamique",
                        Focus = "Développement et intégration",
                        Keywords = new List<string> { "développement", "intégration", "services", "données" }
                    },
                    new PromptVariation
                    {
                        Style = "Axé sécurité",
                        Tone = "Rigoureux",
                        Focus = "Sécurité et fiabilité",
                        Keywords = new List<string> { "sécurité", "authentification", "fiabilité", "monitoring" }
                    }
                },
                ["frontend"] = new List<PromptVariation>
                {
                    new PromptVariation
                    {
                        Style = "UX/UI orienté",
                        Tone = "Créatif",
                        Focus = "Expérience utilisateur",
                        Keywords = new List<string> { "interface", "UX", "responsive", "accessibilité" }
                    },
                    new PromptVariation
                    {
                        Style = "Performance focus",
                        Tone = "Technique",
                        Focus = "Optimisation et performance",
                        Keywords = new List<string> { "performance", "optimisation", "SEO", "PWA" }
                    },
                    new PromptVariation
                    {
                        Style = "Framework spécialisé",
                        Tone = "Expert",
                        Focus = "Maîtrise des frameworks",
                        Keywords = new List<string> { "React", "Vue", "Angular", "composants" }
                    }
                },
                ["mobile"] = new List<PromptVariation>
                {
                    new PromptVariation
                    {
                        Style = "Cross-platform",
                        Tone = "Innovant",
                        Focus = "Développement multi-plateforme",
                        Keywords = new List<string> { "React Native", "Flutter", "cross-platform", "stores" }
                    },
                    new PromptVariation
                    {
                        Style = "Performance mobile",
                        Tone = "Technique",
                        Focus = "Optimisation mobile",
                        Keywords = new List<string> { "performance", "batterie", "mémoire", "offline" }
                    }
                },
                ["devops"] = new List<PromptVariation>
                {
                    new PromptVariation
                    {
                        Style = "Infrastructure as Code",
                        Tone = "Systémique",
                        Focus = "Automatisation",
                        Keywords = new List<string> { "IaC", "automatisation", "CI/CD", "cloud" }
                    },
                    new PromptVariation
                    {
                        Style = "Monitoring et observabilité",
                        Tone = "Analytique",
                        Focus = "Surveillance et performance",
                        Keywords = new List<string> { "monitoring", "logs", "métriques", "alerting" }
                    }
                }
            };
        }
        
        private string GetDomainSpecificInstructions(string domain)
        {
            var instructions = new Dictionary<string, string>
            {
                ["backend"] = @"
SPÉCIFICITÉS BACKEND :
- Focus sur les APIs, bases de données, et architecture serveur
- Mentionner la scalabilité et les performances
- Inclure sécurité et authentification
- Éviter les technologies frontend (React, Vue, Angular)",
                
                ["frontend"] = @"
SPÉCIFICITÉS FRONTEND :
- Focus sur l'interface utilisateur et l'expérience
- Mentionner responsive design et accessibilité
- Inclure les frameworks JS modernes
- Éviter les technologies backend pures",
                
                ["mobile"] = @"
SPÉCIFICITÉS MOBILE :
- Focus sur iOS/Android et cross-platform
- Mentionner App Store et Google Play
- Inclure notifications push et offline
- Technologies : React Native, Flutter, native",
                
                ["devops"] = @"
SPÉCIFICITÉS DEVOPS :
- Focus sur infrastructure et automatisation
- Mentionner cloud, containers, et CI/CD
- Inclure monitoring et sécurité
- Technologies : Docker, Kubernetes, AWS, Jenkins"
            };
            
            return instructions.GetValueOrDefault(domain, "Instructions générales de développement");
        }
        
        private string GetAllowedTechnologies(string domain)
        {
            var technologies = new Dictionary<string, List<string>>
            {
                ["backend"] = new List<string> 
                { 
                    "Node.js", "Express.js", "PHP", "Laravel", "Python", "Django", 
                    "Java", "Spring", "C#", ".NET", "PostgreSQL", "MongoDB", 
                    "Redis", "Docker", "JWT", "REST API", "GraphQL" 
                },
                ["frontend"] = new List<string> 
                { 
                    "React", "Vue.js", "Angular", "TypeScript", "JavaScript", 
                    "HTML5", "CSS3", "Sass", "Redux", "Vuex", "Webpack", 
                    "Jest", "Cypress", "Figma", "Responsive Design" 
                },
                ["mobile"] = new List<string> 
                { 
                    "React Native", "Flutter", "Swift", "Kotlin", "Dart", 
                    "Firebase", "Expo", "Redux", "AsyncStorage", "Push Notifications",
                    "App Store", "Google Play", "Fastlane" 
                },
                ["devops"] = new List<string> 
                { 
                    "Docker", "Kubernetes", "AWS", "Azure", "Jenkins", "GitLab CI",
                    "Terraform", "Ansible", "Prometheus", "Grafana", "ELK Stack",
                    "Nginx", "Linux", "Bash", "CI/CD" 
                }
            };
            
            var domainTechs = technologies.GetValueOrDefault(domain, new List<string>());
            return string.Join(", ", domainTechs);
        }
    }
}
