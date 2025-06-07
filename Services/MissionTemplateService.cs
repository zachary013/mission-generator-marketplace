using System;
using System.Collections.Generic;
using System.Linq;
using SmartMarketplace.Models;

namespace SmartMarketplace.Services
{
    public class MissionTemplateService : IMissionTemplateService
    {
        private readonly Dictionary<string, MissionTemplate> _templates;
        
        public MissionTemplateService()
        {
            _templates = InitializeTemplates();
        }
        
        public MissionTemplate GetTemplate(string domain, string experienceLevel)
        {
            var key = domain.ToLower();
            if (_templates.ContainsKey(key))
            {
                var template = _templates[key];
                // Adapter le template selon le niveau d'expérience
                return AdaptTemplateForExperience(template, experienceLevel);
            }
            
            return _templates["backend"]; // Template par défaut
        }
        
        public List<string> GetDomainSpecificTechnologies(string domain)
        {
            var template = GetTemplate(domain, "3-7");
            var allTechs = new List<string>();
            allTechs.AddRange(template.CoreTechnologies);
            allTechs.AddRange(template.ComplementaryTechnologies.Take(3)); // Limiter les technologies complémentaires
            return allTechs;
        }
        
        public string GenerateContextualDescription(MissionTemplate template, ExtractedInformation info)
        {
            var random = new Random();
            var selectedProject = template.ProjectTypes[random.Next(template.ProjectTypes.Count)];
            var selectedResponsibilities = template.ResponsibilityTemplates.Take(4).ToList();
            
            return $@"🎯 CONTEXTE DU PROJET :
{selectedProject}

💼 VOS MISSIONS :
{string.Join("\n", selectedResponsibilities.Select(r => $"• {r}"))}

🛠️ PROFIL RECHERCHÉ :
{template.ExperienceContext}

🔧 STACK TECHNIQUE :
{string.Join(", ", template.CoreTechnologies.Take(5))}

📍 MODALITÉS :
• Localisation : {info.City}, {info.Country}
• Mode : {info.WorkMode}
• Durée : {info.Duration} {(info.DurationType == "MONTH" ? "mois" : "ans")}
• Expérience requise : {info.Experience} ans
• Type de contrat : {info.ContractType}

{GetClosingMessage(template.Domain)}";
        }
        
        private Dictionary<string, MissionTemplate> InitializeTemplates()
        {
            return new Dictionary<string, MissionTemplate>
            {
                ["backend"] = new MissionTemplate
                {
                    Domain = "Backend",
                    TitleTemplate = "Développeur Backend {tech} - {city}",
                    ContextDescription = "Développement d'APIs robustes et scalables",
                    ResponsibilityTemplates = new List<string>
                    {
                        "Conception et développement d'APIs RESTful/GraphQL",
                        "Optimisation des performances et de la scalabilité",
                        "Intégration avec bases de données et services externes",
                        "Mise en place de tests unitaires et d'intégration",
                        "Sécurisation des endpoints et gestion des authentifications",
                        "Documentation technique des APIs",
                        "Monitoring et debugging des services backend"
                    },
                    CoreTechnologies = new List<string> { "Node.js", "Express.js", "MongoDB", "PostgreSQL", "Redis" },
                    ComplementaryTechnologies = new List<string> { "Docker", "JWT", "Swagger", "Jest", "Postman", "AWS", "Nginx" },
                    ExperienceContext = "Solide expérience en développement backend avec maîtrise des architectures API",
                    ProjectTypes = new List<string>
                    {
                        "Développement d'une API e-commerce avec gestion des commandes et paiements",
                        "Création d'un système de gestion de contenu avec API headless",
                        "Mise en place d'une architecture microservices pour une fintech",
                        "Développement d'APIs pour application mobile avec forte charge"
                    }
                },
                ["frontend"] = new MissionTemplate
                {
                    Domain = "Frontend",
                    TitleTemplate = "Développeur Frontend {tech} - {city}",
                    ContextDescription = "Création d'interfaces utilisateur modernes et performantes",
                    ResponsibilityTemplates = new List<string>
                    {
                        "Développement d'interfaces utilisateur responsive",
                        "Intégration avec APIs REST et GraphQL",
                        "Optimisation des performances et de l'accessibilité",
                        "Mise en place de tests unitaires et e2e",
                        "Collaboration étroite avec les équipes UX/UI",
                        "Maintenance et évolution du code existant",
                        "Veille technologique et bonnes pratiques"
                    },
                    CoreTechnologies = new List<string> { "React", "TypeScript", "CSS3", "HTML5", "JavaScript" },
                    ComplementaryTechnologies = new List<string> { "Redux", "React Router", "Styled Components", "Jest", "Cypress", "Webpack", "Sass" },
                    ExperienceContext = "Expertise en développement frontend avec maîtrise des frameworks modernes",
                    ProjectTypes = new List<string>
                    {
                        "Refonte complète d'une plateforme e-commerce avec React",
                        "Développement d'un dashboard admin avec visualisations avancées",
                        "Création d'une PWA pour améliorer l'expérience mobile",
                        "Interface de trading en temps réel avec WebSockets"
                    }
                },
                ["fullstack"] = new MissionTemplate
                {
                    Domain = "Fullstack",
                    TitleTemplate = "Développeur Fullstack {tech} - {city}",
                    ContextDescription = "Développement complet d'applications web modernes",
                    ResponsibilityTemplates = new List<string>
                    {
                        "Développement frontend et backend de l'application",
                        "Conception de l'architecture technique globale",
                        "Intégration complète des fonctionnalités",
                        "Optimisation des performances full-stack",
                        "Déploiement et maintenance de l'application",
                        "Collaboration avec les équipes produit et design",
                        "Formation et support technique aux équipes"
                    },
                    CoreTechnologies = new List<string> { "React", "Node.js", "MongoDB", "Express.js", "TypeScript" },
                    ComplementaryTechnologies = new List<string> { "Redux", "JWT", "Docker", "AWS", "Git", "Jest", "Cypress" },
                    ExperienceContext = "Vision complète du développement web avec expertise frontend et backend",
                    ProjectTypes = new List<string>
                    {
                        "Développement complet d'une marketplace B2B",
                        "Création d'une plateforme SaaS de gestion de projets",
                        "Application de réservation en ligne avec paiements",
                        "Système de gestion documentaire avec workflow"
                    }
                },
                ["mobile"] = new MissionTemplate
                {
                    Domain = "Mobile",
                    TitleTemplate = "Développeur Mobile {tech} - {city}",
                    ContextDescription = "Développement d'applications mobiles natives et hybrides",
                    ResponsibilityTemplates = new List<string>
                    {
                        "Développement d'applications iOS et Android",
                        "Intégration avec APIs et services backend",
                        "Optimisation des performances mobiles",
                        "Gestion des notifications push",
                        "Publication sur App Store et Google Play",
                        "Tests sur différents devices et OS",
                        "Maintenance et mises à jour des applications"
                    },
                    CoreTechnologies = new List<string> { "React Native", "Flutter", "Firebase", "Redux", "AsyncStorage" },
                    ComplementaryTechnologies = new List<string> { "Expo", "CodePush", "Fastlane", "Detox", "Flipper", "Crashlytics" },
                    ExperienceContext = "Expertise en développement mobile avec connaissance des spécificités iOS/Android",
                    ProjectTypes = new List<string>
                    {
                        "Application e-commerce mobile avec paiement intégré",
                        "App de livraison avec géolocalisation temps réel",
                        "Application bancaire mobile sécurisée",
                        "Réseau social mobile avec chat et notifications"
                    }
                },
                ["devops"] = new MissionTemplate
                {
                    Domain = "DevOps",
                    TitleTemplate = "Ingénieur DevOps {tech} - {city}",
                    ContextDescription = "Automatisation et optimisation de l'infrastructure",
                    ResponsibilityTemplates = new List<string>
                    {
                        "Mise en place de pipelines CI/CD",
                        "Gestion de l'infrastructure cloud (AWS/Azure)",
                        "Containerisation avec Docker et Kubernetes",
                        "Monitoring et alerting des services",
                        "Automatisation des déploiements",
                        "Sécurisation de l'infrastructure",
                        "Optimisation des coûts cloud"
                    },
                    CoreTechnologies = new List<string> { "Docker", "Kubernetes", "AWS", "Jenkins", "Terraform" },
                    ComplementaryTechnologies = new List<string> { "Ansible", "Prometheus", "Grafana", "ELK Stack", "GitLab CI", "Helm" },
                    ExperienceContext = "Solide expérience en infrastructure cloud et automatisation",
                    ProjectTypes = new List<string>
                    {
                        "Migration d'infrastructure vers le cloud AWS",
                        "Mise en place d'une architecture microservices",
                        "Automatisation complète des déploiements",
                        "Optimisation d'infrastructure haute disponibilité"
                    }
                }
            };
        }
        
        private MissionTemplate AdaptTemplateForExperience(MissionTemplate template, string experienceLevel)
        {
            var adaptedTemplate = new MissionTemplate
            {
                Domain = template.Domain,
                TitleTemplate = template.TitleTemplate,
                ContextDescription = template.ContextDescription,
                ResponsibilityTemplates = new List<string>(template.ResponsibilityTemplates),
                CoreTechnologies = new List<string>(template.CoreTechnologies),
                ComplementaryTechnologies = new List<string>(template.ComplementaryTechnologies),
                ProjectTypes = new List<string>(template.ProjectTypes)
            };
            
            // Adapter selon le niveau d'expérience
            switch (experienceLevel)
            {
                case "0-3":
                    adaptedTemplate.ExperienceContext = "Profil junior avec bases solides et envie d'apprendre";
                    adaptedTemplate.ResponsibilityTemplates = template.ResponsibilityTemplates.Take(4).ToList();
                    break;
                case "7-12":
                    adaptedTemplate.ExperienceContext = "Profil senior avec capacité de mentoring et d'architecture";
                    adaptedTemplate.ResponsibilityTemplates.Add("Mentoring des développeurs junior");
                    adaptedTemplate.ResponsibilityTemplates.Add("Définition de l'architecture technique");
                    break;
                case "12+":
                    adaptedTemplate.ExperienceContext = "Expert technique avec vision stratégique";
                    adaptedTemplate.ResponsibilityTemplates.Add("Leadership technique de l'équipe");
                    adaptedTemplate.ResponsibilityTemplates.Add("Définition des standards et bonnes pratiques");
                    break;
                default: // 3-7
                    adaptedTemplate.ExperienceContext = template.ExperienceContext;
                    break;
            }
            
            return adaptedTemplate;
        }
        
        private string GetClosingMessage(string domain)
        {
            var messages = new Dictionary<string, string>
            {
                ["Backend"] = "Rejoignez notre équipe pour construire des APIs robustes et scalables !",
                ["Frontend"] = "Participez à la création d'expériences utilisateur exceptionnelles !",
                ["Fullstack"] = "Contribuez à l'ensemble de notre stack technologique !",
                ["Mobile"] = "Développez la prochaine app qui révolutionnera le mobile !",
                ["DevOps"] = "Optimisez notre infrastructure pour une performance maximale !"
            };
            
            return messages.GetValueOrDefault(domain, "Rejoignez notre équipe technique dynamique !");
        }
    }
}
