# 🚀 SmartMarketplace API - Backend ASP.NET Core 9

> **API intelligente de génération de missions freelance avec intégration multi-IA (Grok, GPT-4o, Mistral)**

## 📋 Vue d'ensemble

Cette API ASP.NET Core 8 permet de générer automatiquement des missions freelance détaillées à partir de descriptions simples en utilisant 3 services d'IA différents avec système de fallback intelligent.

## 🏗️ Architecture

```
SmartMarketplace/
├── 🎮 Controllers/
│   └── MissionController.cs          # Endpoints API
├── 📊 Models/
│   ├── Mission.cs                    # Modèle principal
│   ├── ApiResponse.cs                # Réponse standardisée
│   └── GenerateMissionRequest.cs     # Requête de génération
├── 🔧 Services/
│   ├── IAIService.cs                 # Interface service principal
│   ├── AIService.cs                  # Orchestrateur IA intelligent
│   ├── IGrokService.cs               # Interface Grok
│   ├── GrokService.cs                # Service Grok X.AI
│   ├── IOpenAIService.cs             # Interface OpenAI
│   ├── OpenAIService.cs              # Service GPT-4o
│   ├── IMistralService.cs            # Interface Mistral
│   └── MistralService.cs             # Service Mistral
├── ⚙️ Configuration/
│   └── AIConfig.cs                   # Configuration IA
├── Program.cs                        # Configuration app
├── appsettings.json                  # Paramètres
└── SmartMarketplace.csproj          # Projet
```

## 🔗 Endpoints API

### 1. **POST** `/api/Mission/generate`
Génère une mission freelance à partir d'une description simple.

**Request Body:**
```json
{
  "simpleInput": "Backend Node.js Rabat 3500DH remote 6 mois senior",
  "preferredProvider": "Grok" // Optionnel: "Grok", "OpenAI", "Mistral"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "guid-generated",
    "title": "Développeur Backend Node.js/Express.js - Rabat (REMOTE)",
    "description": "🎯 CONTEXTE DU PROJET :\nDéveloppement d'une API e-commerce...",
    "country": "Morocco",
    "city": "Rabat",
    "workMode": "REMOTE",
    "duration": 6,
    "durationType": "MONTH",
    "startImmediately": true,
    "startDate": null,
    "experienceYear": "7-12",
    "contractType": "FORFAIT",
    "estimatedDailyRate": 350,
    "domain": "Backend Development",
    "position": "Développeur Backend",
    "requiredExpertises": ["Node.js", "Express.js", "MongoDB", "PostgreSQL"],
    "createdAt": "2025-06-14T22:00:00Z"
  },
  "provider": "Grok"
}
```

### 2. **POST** `/api/Mission/save`
Sauvegarde une mission.

**Request Body:**
```json
{
  "title": "Titre de la mission",
  "description": "Description détaillée",
  "city": "Rabat",
  // ... autres propriétés Mission
}
```

### 3. **GET** `/api/Mission`
Récupère toutes les missions sauvegardées.

### 4. **GET** `/api/Mission/{id}`
Récupère une mission par son ID.

### 5. **DELETE** `/api/Mission/{id}`
Supprime une mission.

### 6. **GET** `/api/Mission/ai-status`
Vérifie le statut des services IA.

**Response:**
```json
{
  "success": true,
  "data": {
    "Grok": true,
    "OpenAI": false,
    "Mistral": true
  }
}
```

## 🤖 Intelligence Artificielle

### Système Multi-IA avec Fallback

1. **Provider Préféré** → Essaie le service demandé
2. **Provider par Défaut** → Fallback vers le service configuré
3. **Autres Providers** → Essaie les services restants
4. **Fallback Intelligent** → Génération locale si tous échouent

### Extraction Intelligente

L'API analyse automatiquement l'input utilisateur pour extraire :

- **Ville** : Rabat, Casablanca, Marrakech, etc.
- **Mode de travail** : Remote, Onsite, Hybrid
- **Durée** : 3 mois, 6 semaines, 1 an
- **Budget** : 3500DH → 350€ (conversion automatique)
- **Niveau** : Junior, Senior, Expert
- **Technologies** : Node.js, React, Python, etc.
- **Domaine** : Backend, Frontend, Full Stack, etc.

### Prompts Intelligents

```csharp
// Exemple de prompt généré automatiquement
var prompt = $@"Tu es un expert en création de missions freelance.
À partir de cette description : ""Backend Node.js Rabat 3500DH remote 6 mois senior""

Génère une mission au format JSON EXACT :
{{
  ""title"": ""Développeur Backend Node.js - Rabat"",
  ""city"": ""Rabat"",
  ""estimatedDailyRate"": 350,
  ""workMode"": ""REMOTE"",
  ""duration"": 6,
  ""experienceYear"": ""7-12"",
  ""requiredExpertises"": [""Node.js"", ""Express.js"", ""MongoDB""]
}}";
```

## ⚙️ Configuration

### 1. Clés API dans `appsettings.json`

```json
{
  "AI": {
    "DefaultProvider": "Grok",
    "Grok": {
      "ApiKey": "xai-YOUR_GROK_KEY_HERE",
      "BaseUrl": "https://api.x.ai/v1",
      "Model": "grok-beta"
    },
    "OpenAI": {
      "ApiKey": "sk-YOUR_OPENAI_KEY_HERE",
      "BaseUrl": "https://api.openai.com/v1",
      "Model": "gpt-4o"
    },
    "Mistral": {
      "ApiKey": "YOUR_MISTRAL_KEY_HERE",
      "BaseUrl": "https://api.mistral.ai/v1",
      "Model": "mistral-small-latest"
    }
  }
}
```

### 2. Variables d'environnement (Production)

```bash
export AI__Grok__ApiKey="xai-your-key"
export AI__OpenAI__ApiKey="sk-your-key"
export AI__Mistral__ApiKey="your-key"
```

## 🚀 Installation et Démarrage

### Prérequis
- **.NET 8.0 SDK**
- **Clés API** pour au moins un service IA

### Installation

```bash
# 1. Cloner et naviguer
cd SmartMarketplace

# 2. Restaurer les dépendances
dotnet restore

# 3. Configurer les clés API dans appsettings.json

# 4. Compiler
dotnet build

# 5. Lancer l'API
dotnet run
```

### Accès

- **API** : `https://localhost:7000`
- **Swagger UI** : `https://localhost:7000` (en développement)
- **Health Check** : `https://localhost:7000/health`


## 📊 Exemples de Génération

### Input Simple → Mission Complète

**Input :**
```
"DevOps AWS Marrakech 5000DH"
```

**Output :**
```json
{
  "title": "Ingénieur DevOps AWS/Docker - Marrakech",
  "description": "🎯 CONTEXTE : Migration cloud AWS avec microservices...",
  "city": "Marrakech",
  "estimatedDailyRate": 500,
  "workMode": "REMOTE",
  "domain": "DevOps",
  "requiredExpertises": ["AWS", "Docker", "Kubernetes", "Terraform"]
}
```

## 🔧 Fonctionnalités Avancées

### 1. **Fallback Intelligent**
Si tous les services IA échouent, génération locale basée sur des templates.

### 2. **Validation Automatique**
Validation des données d'entrée et de sortie avec messages d'erreur clairs.

### 3. **Logging Complet**
Traçabilité complète des appels API et erreurs.

### 4. **CORS Configuré**
Prêt pour intégration frontend.

### 5. **Swagger Documentation**
Documentation interactive automatique.

## 🛡️ Gestion d'Erreurs

### Codes de Réponse

- **200** : Succès
- **400** : Erreur de validation
- **404** : Ressource non trouvée
- **500** : Erreur serveur

### Format d'Erreur

```json
{
  "success": false,
  "errorMessage": "Description de l'erreur",
  "data": null
}
```

## 🔄 Workflow Complet

1. **Client** envoie description simple
2. **AIService** analyse et extrait informations
3. **Prompt intelligent** généré automatiquement
4. **Tentative avec provider préféré**
5. **Fallback** vers autres providers si échec
6. **Génération locale** en dernier recours
7. **Validation** et enrichissement des données
8. **Retour** de la mission complète

