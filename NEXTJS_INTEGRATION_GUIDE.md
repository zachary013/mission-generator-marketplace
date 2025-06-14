# 📚 Guide d'Intégration SmartMarketplace API avec Next.js

> **Documentation complète pour agent IA - Intégration Frontend/Backend**

## 🏗️ Architecture de l'API

### Base URL
```
HTTPS: https://localhost:5001
HTTP: http://localhost:5000 (redirige vers HTTPS)
```

### Format de Réponse Standard
Toutes les réponses suivent le format `ApiResponse<T>` :

```typescript
interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  errorMessage: string | null;
  provider: string | null; // Provider IA utilisé
}
```

## 📊 Modèles de Données

### 1. Mission (Modèle Principal)
```typescript
interface Mission {
  id?: string;                    // GUID généré automatiquement
  title: string;                  // Titre de la mission (requis)
  description: string;            // Description détaillée (requis)
  country: string;                // Pays (requis)
  city: string;                   // Ville (requis)
  workMode: 'ONSITE' | 'REMOTE' | 'HYBRID'; // Mode de travail
  duration: number;               // Durée numérique
  durationType: 'DAY' | 'WEEK' | 'MONTH' | 'YEAR'; // Type de durée
  startImmediately: boolean;      // Démarrage immédiat
  startDate?: string;             // Date de début (format: yyyy-MM-dd)
  experienceYear: string;         // "0-3", "3-7", "7-12", "12+"
  contractType: 'REGIE' | 'FORFAIT' | 'CDI' | 'CDD'; // Type de contrat
  estimatedDailyRate: number;     // Tarif journalier en euros
  domain: string;                 // Domaine technique
  position: string;               // Poste/Position
  requiredExpertises: string[];   // Compétences requises
  createdAt: Date;                // Date de création (auto)
}
```

### 2. GenerateMissionRequest
```typescript
interface GenerateMissionRequest {
  simpleInput: string;            // Description simple (min 10 caractères, requis)
  preferredProvider?: 'Grok' | 'OpenAI' | 'Mistral'; // Provider IA préféré
}
```

## 🔗 Endpoints API Complets

### 1. **POST** `/api/Mission/generate` - Génération de Mission
**Description** : Génère une mission complète à partir d'une description simple

**Request Body** :
```json
{
  "simpleInput": "Backend Node.js Rabat 3500DH remote 6 mois senior",
  "preferredProvider": "Grok"
}
```

**Response Success (200)** :
```json
{
  "success": true,
  "data": {
    "id": "21c0d771-e370-452d-beaa-d27259ea64ee",
    "title": "Développeur Backend Node.js - Rabat",
    "description": "🎯 CONTEXTE DU PROJET :\nDéveloppement d'une application...",
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
    "createdAt": "2025-06-14T23:07:09.734376Z"
  },
  "errorMessage": null,
  "provider": "AI Generated"
}
```

**Response Error (400)** :
```json
{
  "success": false,
  "data": null,
  "errorMessage": "Validation errors: SimpleInput is required, SimpleInput must be at least 10 characters",
  "provider": null
}
```

### 2. **POST** `/api/Mission/save` - Sauvegarde de Mission
**Description** : Sauvegarde une mission en mémoire

**Request Body** :
```json
{
  "title": "Développeur Backend Node.js - Test",
  "description": "Mission de test pour l'API",
  "country": "Morocco",
  "city": "Rabat",
  "workMode": "REMOTE",
  "duration": 3,
  "durationType": "MONTH",
  "startImmediately": true,
  "experienceYear": "3-7",
  "contractType": "FORFAIT",
  "estimatedDailyRate": 350,
  "domain": "Backend Development",
  "position": "Développeur Backend",
  "requiredExpertises": ["Node.js", "Express.js", "MongoDB"]
}
```

**Response Success (200)** :
```json
{
  "success": true,
  "data": {
    "id": "d8302364-14a7-4ae0-9fe3-0a25994d9928",
    "title": "Développeur Backend Node.js - Test",
    // ... autres propriétés
    "createdAt": "2025-06-14T23:09:30.330359Z"
  },
  "errorMessage": null,
  "provider": null
}
```

### 3. **GET** `/api/Mission` - Récupérer Toutes les Missions
**Description** : Récupère toutes les missions sauvegardées (triées par date de création décroissante)

**Response Success (200)** :
```json
{
  "success": true,
  "data": [
    {
      "id": "d8302364-14a7-4ae0-9fe3-0a25994d9928",
      "title": "Développeur Backend Node.js - Test",
      // ... propriétés complètes de la mission
    }
  ],
  "errorMessage": null,
  "provider": null
}
```

### 4. **GET** `/api/Mission/{id}` - Récupérer une Mission par ID
**Description** : Récupère une mission spécifique par son ID

**URL Params** : `id` (string) - GUID de la mission

**Response Success (200)** :
```json
{
  "success": true,
  "data": {
    "id": "d8302364-14a7-4ae0-9fe3-0a25994d9928",
    "title": "Développeur Backend Node.js - Test",
    // ... propriétés complètes
  },
  "errorMessage": null,
  "provider": null
}
```

**Response Error (404)** :
```json
{
  "success": false,
  "data": null,
  "errorMessage": "Mission non trouvée.",
  "provider": null
}
```

### 5. **DELETE** `/api/Mission/{id}` - Supprimer une Mission
**Description** : Supprime une mission par son ID

**URL Params** : `id` (string) - GUID de la mission

**Response Success (200)** :
```json
{
  "success": true,
  "data": true,
  "errorMessage": null,
  "provider": null
}
```

### 6. **GET** `/api/Mission/ai-status` - Statut des Services IA
**Description** : Vérifie la disponibilité des services IA

**Response Success (200)** :
```json
{
  "success": true,
  "data": {
    "Grok": false,
    "OpenAI": false,
    "Mistral": false
  },
  "errorMessage": null,
  "provider": null
}
```

### 7. **GET** `/health` - Health Check
**Description** : Vérifie l'état de l'API

**Response Success (200)** :
```json
{
  "status": "Healthy",
  "timestamp": "2025-06-14T23:05:16.790902Z",
  "version": "1.0.0"
}
```

## 🤖 Intelligence Artificielle - Providers Disponibles

### Providers Configurés
1. **Grok** (X.AI) - Provider par défaut
   - Modèle : `grok-beta`
   - Endpoint : `https://api.x.ai/v1`

2. **OpenAI** (GPT)
   - Modèle : `gpt-4o-mini`
   - Endpoint : `https://api.openai.com/v1`

3. **Mistral**
   - Modèle : `mistral-small-latest`
   - Endpoint : `https://api.mistral.ai/v1`

### Système de Fallback Intelligent
1. **Provider Préféré** → Essaie le service demandé
2. **Provider par Défaut** → Fallback vers Grok
3. **Autres Providers** → Essaie OpenAI puis Mistral
4. **Génération Locale** → Si tous échouent, génération basée sur templates

### Extraction Automatique depuis SimpleInput
L'IA analyse automatiquement et extrait :
- **Ville** : Rabat, Casablanca, Marrakech, Fès, etc.
- **Mode** : Remote, Onsite, Hybrid
- **Durée** : 3 mois, 6 semaines, 1 an
- **Budget** : 3500DH → 350€ (conversion automatique)
- **Niveau** : Junior (0-3), Senior (7-12), Expert (12+)
- **Technologies** : Node.js, React, Python, AWS, etc.
- **Domaine** : Backend, Frontend, Full Stack, DevOps, etc.

## 💻 Implémentation Next.js

### 1. Configuration API Client (`lib/api.ts`)
```typescript
import axios from 'axios';

const apiClient = axios.create({
  baseURL: 'https://localhost:5001/api',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000, // 30 secondes pour la génération IA
});

// Intercepteur pour gérer les erreurs
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error('API Error:', error.response?.data || error.message);
    return Promise.reject(error);
  }
);

export default apiClient;
```

### 2. Service Missions (`services/missionService.ts`)
```typescript
import apiClient from '../lib/api';
import { Mission, GenerateMissionRequest, ApiResponse } from '../types/api';

export const missionService = {
  // Générer une mission
  async generateMission(
    simpleInput: string, 
    preferredProvider?: 'Grok' | 'OpenAI' | 'Mistral'
  ): Promise<ApiResponse<Mission>> {
    const response = await apiClient.post<ApiResponse<Mission>>('/Mission/generate', {
      simpleInput,
      preferredProvider
    });
    return response.data;
  },

  // Sauvegarder une mission
  async saveMission(mission: Omit<Mission, 'id' | 'createdAt'>): Promise<ApiResponse<Mission>> {
    const response = await apiClient.post<ApiResponse<Mission>>('/Mission/save', mission);
    return response.data;
  },

  // Récupérer toutes les missions
  async getAllMissions(): Promise<ApiResponse<Mission[]>> {
    const response = await apiClient.get<ApiResponse<Mission[]>>('/Mission');
    return response.data;
  },

  // Récupérer une mission par ID
  async getMissionById(id: string): Promise<ApiResponse<Mission>> {
    const response = await apiClient.get<ApiResponse<Mission>>(`/Mission/${id}`);
    return response.data;
  },

  // Supprimer une mission
  async deleteMission(id: string): Promise<ApiResponse<boolean>> {
    const response = await apiClient.delete<ApiResponse<boolean>>(`/Mission/${id}`);
    return response.data;
  },

  // Vérifier le statut des IA
  async getAIStatus(): Promise<ApiResponse<Record<string, boolean>>> {
    const response = await apiClient.get<ApiResponse<Record<string, boolean>>>('/Mission/ai-status');
    return response.data;
  }
};
```

### 3. Types TypeScript (`types/api.ts`)
```typescript
export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  errorMessage: string | null;
  provider: string | null;
}

export interface Mission {
  id?: string;
  title: string;
  description: string;
  country: string;
  city: string;
  workMode: 'ONSITE' | 'REMOTE' | 'HYBRID';
  duration: number;
  durationType: 'DAY' | 'WEEK' | 'MONTH' | 'YEAR';
  startImmediately: boolean;
  startDate?: string;
  experienceYear: string;
  contractType: 'REGIE' | 'FORFAIT' | 'CDI' | 'CDD';
  estimatedDailyRate: number;
  domain: string;
  position: string;
  requiredExpertises: string[];
  createdAt: Date;
}

export interface GenerateMissionRequest {
  simpleInput: string;
  preferredProvider?: 'Grok' | 'OpenAI' | 'Mistral';
}
```

### 4. Hook React (`hooks/useMissions.ts`)
```typescript
import { useState, useEffect } from 'react';
import { missionService } from '../services/missionService';
import { Mission, ApiResponse } from '../types/api';

export function useMissions() {
  const [missions, setMissions] = useState<Mission[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');

  const fetchMissions = async () => {
    try {
      setLoading(true);
      const result = await missionService.getAllMissions();
      if (result.success && result.data) {
        setMissions(result.data);
      } else {
        setError(result.errorMessage || 'Erreur inconnue');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erreur réseau');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchMissions();
  }, []);

  const deleteMission = async (id: string) => {
    const result = await missionService.deleteMission(id);
    if (result.success) {
      setMissions(missions.filter(m => m.id !== id));
    } else {
      throw new Error(result.errorMessage || 'Erreur lors de la suppression');
    }
  };

  return {
    missions,
    loading,
    error,
    refetch: fetchMissions,
    deleteMission
  };
}
```

## 🎯 Exemples d'Utilisation

### Génération de Mission Simple
```typescript
const generateMission = async () => {
  try {
    const result = await missionService.generateMission(
      "Backend Node.js Rabat 3500DH remote 6 mois senior",
      "Grok"
    );
    
    if (result.success && result.data) {
      console.log('Mission générée:', result.data);
      console.log('Provider utilisé:', result.provider);
    } else {
      console.error('Erreur:', result.errorMessage);
    }
  } catch (error) {
    console.error('Erreur réseau:', error);
  }
};
```

### Formats d'Input Supportés
```typescript
const exemples = [
  "Backend Node.js Rabat 3500DH remote 6 mois senior",
  "Frontend React Casablanca 4000DH",
  "DevOps AWS Marrakech 5000DH onsite",
  "Fullstack MERN Rabat 4500DH hybrid 3 mois",
  "Mobile React Native Tanger 3000DH junior",
  "UI/UX Designer Agadir 2500DH remote freelance",
  "Python Django Fès 2800DH onsite 1 an"
];
```

## 🛡️ Gestion d'Erreurs

### Codes de Statut HTTP
- **200** : Succès
- **400** : Erreur de validation (champs requis, format incorrect)
- **404** : Ressource non trouvée
- **500** : Erreur serveur interne

### Validation des Données
- **SimpleInput** : Minimum 10 caractères, requis
- **Mission.Title** : Requis
- **Mission.Description** : Requis
- **Mission.Country** : Requis
- **Mission.City** : Requis

### Gestion des Timeouts
- **Génération IA** : Timeout de 30 secondes recommandé
- **Autres endpoints** : Timeout standard de 10 secondes

## 🚀 Démarrage Rapide

### 1. Installation
```bash
npm install axios
```

### 2. Configuration
```typescript
// Copier les fichiers types, services et hooks
// Configurer l'URL de base : https://localhost:5001/api
```

### 3. Utilisation
```tsx
import { missionService } from './services/missionService';

const MyComponent = () => {
  const handleGenerate = async () => {
    const result = await missionService.generateMission(
      "Backend Node.js Rabat 3500DH remote"
    );
    // Traiter le résultat
  };
};
```

## 📝 Notes Importantes

1. **HTTPS Obligatoire** : L'API fonctionne uniquement en HTTPS sur le port 5001
2. **CORS Configuré** : Accepte toutes les origines en développement
3. **Stockage Temporaire** : Les missions sont stockées en mémoire (redémarrage = perte des données)
4. **Fallback IA** : Si tous les services IA échouent, génération locale basée sur templates
5. **Validation Robuste** : Validation côté serveur avec messages d'erreur clairs
6. **Logging Complet** : Tous les appels sont loggés pour le debugging

Cette documentation vous permet d'intégrer parfaitement votre frontend Next.js avec l'API SmartMarketplace ! 🎯
