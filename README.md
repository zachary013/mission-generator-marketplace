# 🚀 SmartMarketplace - AI-Powered Mission Generator

> **Système intelligent de génération de missions freelance utilisant l'IA Grok**

Une application web ASP.NET Core qui transforme des descriptions simples en missions freelance détaillées et professionnelles grâce à l'intelligence artificielle.

---

## 🎯 Vue d'ensemble

### Problématique
Les recruteurs perdent du temps à rédiger des offres d'emploi détaillées à partir de besoins simples. Le processus manuel est long et souvent incohérent.

### Solution
**SmartMarketplace** automatise la génération de missions freelance en utilisant :
- 🤖 **IA** pour la génération de contenu intelligent
- 📝 **Templates spécialisés** par domaine technique
- 🎯 **Analyse contextuelle** des besoins utilisateur
- ✅ **Validation automatique** des contraintes

---

## 📊 Flux de Données

### 1. Analyse de l'Input Utilisateur

```csharp
// Exemple d'analyse
Input: "Backend Node.js Rabat 3500DH remote 6 mois"

ExtractedInformation {
    Domain: "Backend",
    City: "Rabat",
    Salary: 3500,
    Currency: "DH",
    WorkMode: "REMOTE",
    Duration: 6,
    DurationType: "MONTH",
    Technologies: ["Node.js", "Express.js", "MongoDB"]
}
```

### 2. Sélection du Template

```csharp
// Template Backend sélectionné
MissionTemplate {
    Domain: "Backend",
    TitleTemplate: "Développeur Backend {tech} - {city}",
    CoreTechnologies: ["Node.js", "Express.js", "MongoDB", "PostgreSQL"],
    ResponsibilityTemplates: [
        "Conception et développement d'APIs RESTful",
        "Optimisation des performances et scalabilité",
        "Intégration avec bases de données"
    ]
}
```

### 3. Génération du Prompt

```text
Tu es un expert en recrutement IT spécialisé en Backend.

CONTRAINTES ABSOLUES :
- Ville : Rabat (NE PAS CHANGER)
- Budget : 3500 DH (EXACT)
- Domaine : Backend (RESPECTER)

TECHNOLOGIES AUTORISÉES POUR BACKEND :
Node.js, Express.js, PHP, Laravel, Python, Django, PostgreSQL, MongoDB...

Génère un JSON avec mission détaillée...
```

### 4. Réponse de l'IA

```json
{
  "title": "Développeur Backend Node.js/Express.js - Rabat",
  "description": "🎯 CONTEXTE : API e-commerce haute performance...",
  "city": "Rabat",
  "estimatedDailyRate": 3500,
  "requiredExpertises": ["Node.js", "Express.js", "MongoDB", "REST API"]
}
```

---

## 🎨 Interface Utilisateur

#### 1. Le client exprime son besoin

<img width="1512" alt="Screenshot 2025-06-07 at 17 39 55" src="https://github.com/user-attachments/assets/c7b0742c-9641-4572-bfe7-b2db37eaba28" />

#### 2. Mission generee qui repond a son besoin

<img width="1512" alt="Screenshot 2025-06-07 at 17 40 21" src="https://github.com/user-attachments/assets/1c250db5-58ed-4250-9c2b-5fbef3be24a0" />


---

## 🧠 Intelligence Artificielle

### Système de Prompts Intelligents

#### 1. Prompts Spécialisés par Domaine

```csharp
var domainPrompts = new Dictionary<string, string> {
    ["Backend"] = @"
        SPÉCIFICITÉS BACKEND :
        - Focus sur APIs, bases de données, architecture serveur
        - Mentionner scalabilité et performances
        - Inclure sécurité et authentification
        - Éviter technologies frontend (React, Vue, Angular)
    ",
    ["Frontend"] = @"
        SPÉCIFICITÉS FRONTEND :
        - Focus sur interface utilisateur et expérience
        - Mentionner responsive design et accessibilité
        - Inclure frameworks JS modernes
    "
};
```

#### 2. Variations de Style

```csharp
var promptVariations = new List<PromptVariation> {
    new() {
        Style = "Technique et précis",
        Tone = "Professionnel",
        Focus = "Architecture et performance",
        Keywords = ["API", "scalabilité", "architecture"]
    },
    new() {
        Style = "Orienté projet",
        Tone = "Dynamique", 
        Focus = "Développement et intégration"
    }
};
```

#### 3. Contraintes Strictes

```text
CONTRAINTES ABSOLUES À RESPECTER :
- Ville : {extractedInfo.City} (NE PAS CHANGER)
- Budget : {extractedInfo.Salary} {extractedInfo.Currency} (EXACT)
- Durée : {extractedInfo.Duration} {extractedInfo.DurationType}
- Domaine : {domain} (RESPECTER LE DOMAINE)
```

### Fallback Intelligent

En cas d'échec de l'API Grok, le système utilise ses templates internes :

```csharp
private Mission GenerateIntelligentFallbackMission(string input, ExtractedInformation info)
{
    var template = _templateService.GetTemplate(domain, info.Experience);
    var title = GenerateTitleFromTemplate(template, info);
    var description = _templateService.GenerateContextualDescription(template, info);
    
    return new Mission {
        Title = title,
        Description = description,
        // ... autres propriétés respectées exactement
    };
}
```

---

## 📁 Structure du Projet

```
SmartMarketplace/
├── 📁 Pages/                          # Interface utilisateur
│   ├── CreateMission.cshtml           # Page principale
│   ├── CreateMission.cshtml.cs        # Code-behind
│   └── Shared/                        # Layouts partagés
│
├── 📁 Services/                       # Logique métier
│   ├── 🔍 IInputAnalysisService.cs    # Interface analyse
│   ├── 🔍 InputAnalysisService.cs     # Analyse des inputs
│   ├── 📝 IMissionTemplateService.cs  # Interface templates
│   ├── 📝 MissionTemplateService.cs   # Templates spécialisés
│   ├── 💬 IPromptService.cs           # Interface prompts
│   ├── 💬 PromptService.cs            # Génération prompts
│   ├── 🤖 IGrokService.cs             # Interface Grok
│   └── 🤖 GrokService.cs              # Communication IA
│
├── 📁 Models/                         # Modèles de données
│   └── Mission.cs                     # Modèle Mission
│
├── 📁 wwwroot/                        # Ressources statiques
│   ├── css/site.css                   # Styles personnalisés
│   ├── js/site.js                     # Scripts JavaScript
│   └── lib/                           # Bibliothèques (Bootstrap, jQuery)
│
├── Program.cs                         # Configuration application
├── appsettings.json                   # Configuration
└── SmartMarketplace.csproj           # Fichier projet
```

---

## 🚀 Installation et Démarrage

### Prérequis

- ✅ **.NET 9.0 SDK** ou supérieur
- ✅ **IDE** : Visual Studio 2022, VS Code, ou JetBrains Rider
- ✅ **Clé API Grok** (X.AI)

### Installation

```bash
# 1. Cloner le repository
git clone https://github.com/votre-username/SmartMarketplace.git
cd SmartMarketplace

# 2. Restaurer les dépendances
dotnet restore

# 3. Configurer la clé API Grok (dans GrokService.cs)
# Remplacer: private const string ApiKey = "VOTRE_CLE_API";

# 4. Compiler le projet
dotnet build

# 5. Lancer l'application
dotnet run
```

### Accès à l'Application

```
🌐 URL HTTP   : http://localhost:5000
```

### Configuration des Services

```csharp
// Program.cs - Injection de dépendances
builder.Services.AddHttpClient<IGrokService, GrokService>();
builder.Services.AddScoped<IInputAnalysisService, InputAnalysisService>();
builder.Services.AddScoped<IMissionTemplateService, MissionTemplateService>();
builder.Services.AddScoped<IPromptService, PromptService>();
```

---

## 📈 Exemples d'Utilisation

### Exemple 1 : Mission Backend

**Input Utilisateur :**
```
"Développeur Backend Node.js Rabat 3500DH remote 6 mois senior"
```

**Output Généré :**
```json
{
  "title": "Développeur Backend Node.js/Express.js - Rabat (REMOTE)",
  "description": "🎯 CONTEXTE DU PROJET :\nDéveloppement d'une API e-commerce haute performance avec architecture microservices.\n\n💼 VOS MISSIONS :\n• Conception et développement d'APIs RESTful/GraphQL\n• Optimisation des performances et de la scalabilité\n• Intégration avec bases de données et services externes\n• Mise en place de tests unitaires et d'intégration\n\n🛠️ STACK TECHNIQUE :\nNode.js, Express.js, MongoDB, PostgreSQL, Redis\n\n📍 MODALITÉS :\n• Localisation : Rabat, Maroc\n• Mode : REMOTE\n• Durée : 6 mois\n• Expérience requise : 7-12 ans\n• Type de contrat : REGIE",
  "country": "Maroc",
  "city": "Rabat",
  "workMode": "REMOTE",
  "duration": 6,
  "durationType": "MONTH",
  "experienceYear": "7-12",
  "contractType": "REGIE",
  "estimatedDailyRate": 3500,
  "domain": "Développement Backend",
  "position": "Développeur Backend",
  "requiredExpertises": ["Node.js", "Express.js", "MongoDB", "PostgreSQL", "REST API", "Docker", "JWT"]
}
```

### Exemple 2 : Mission Frontend

**Input Utilisateur :**
```
"Frontend React Casablanca 4000DH"
```

**Output Généré :**
```json
{
  "title": "Développeur Frontend React/TypeScript - Casablanca",
  "description": "🎯 CONTEXTE DU PROJET :\nRefonte complète d'une plateforme e-commerce avec React et expérience utilisateur optimisée.\n\n💼 VOS MISSIONS :\n• Développement d'interfaces utilisateur responsive\n• Intégration avec APIs REST et GraphQL\n• Optimisation des performances et de l'accessibilité\n• Collaboration étroite avec les équipes UX/UI\n\n🛠️ STACK TECHNIQUE :\nReact, TypeScript, CSS3, HTML5, Redux\n\n📍 MODALITÉS :\n• Localisation : Casablanca, Maroc\n• Mode : REMOTE\n• Durée : 3 mois\n• Expérience requise : 3-7 ans",
  "estimatedDailyRate": 4000,
  "requiredExpertises": ["React", "TypeScript", "JavaScript", "HTML5", "CSS3", "Redux", "Responsive Design"]
}
```

### Exemple 3 : Mission DevOps

**Input Utilisateur :**
```
"DevOps AWS Marrakech 5000DH"
```

**Output Généré :**
```json
{
  "title": "Ingénieur DevOps AWS/Docker - Marrakech",
  "description": "🎯 CONTEXTE DU PROJET :\nMigration d'infrastructure vers le cloud AWS avec mise en place d'une architecture microservices.\n\n💼 VOS MISSIONS :\n• Mise en place de pipelines CI/CD\n• Gestion de l'infrastructure cloud (AWS)\n• Containerisation avec Docker et Kubernetes\n• Monitoring et alerting des services\n\n🛠️ STACK TECHNIQUE :\nAWS, Docker, Kubernetes, Jenkins, Terraform",
  "estimatedDailyRate": 5000,
  "requiredExpertises": ["AWS", "Docker", "Kubernetes", "Jenkins", "Terraform", "CI/CD", "Monitoring"]
}
```


