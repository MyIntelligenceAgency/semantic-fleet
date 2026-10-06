# Configuration des Modèles pour Semantic Fleet

Ce document explique comment configurer l'accès aux différents modèles d'IA dans le projet Semantic Fleet, tant pour les scripts Python que pour les applications C#.

## Table des Matières

1. [Configuration des Variables d'Environnement](#configuration-des-variables-denvironnement)
2. [Modèles Disponibles](#modèles-disponibles)
3. [Utilisation en Python](#utilisation-en-python)
4. [Utilisation en C#](#utilisation-en-c)
5. [Ajout de Nouveaux Modèles](#ajout-de-nouveaux-modèles)

## Configuration des Variables d'Environnement

Le projet utilise un fichier `.env` à la racine pour stocker les clés API et les URLs de base pour les différents services. Pour configurer votre environnement :

1. Copiez le fichier `.env.example` en `.env` :
   ```bash
   cp .env.example .env
   ```

2. Modifiez le fichier `.env` pour ajouter vos clés API :
   ```
   OPENAI_API_KEY=votre_clé_api_openai
   OPENROUTER_API_KEY=votre_clé_api_openrouter
   ```

### Structure du Fichier .env

Le fichier `.env` est organisé en sections :

- **OpenAI API** : Configuration pour accéder aux modèles OpenAI
- **OpenRouter API** : Configuration pour accéder à divers modèles via OpenRouter
- **Modèles spécifiques** : Configurations pour des modèles particuliers (Claude, Gemini, Qwen)
- **Modèles locaux** : Configurations pour les modèles hébergés localement

## Modèles Disponibles

### Via OpenAI
- GPT-4o
- GPT-4o-mini
- GPT-3.5-turbo
- GPT-4

### Via OpenRouter
- Claude 3 Sonnet 3.7 (`anthropic/claude-3-sonnet-20240229`)
- Gemini Pro 2.5 (`google/gemini-pro-1.5`)
- Qwen 3 72B (`qwen/qwen-72b`)
- Qwen 3 Chat (`qwen/qwen-chat`)

### Modèles Locaux
- Micro
- Mini
- Medium

## Utilisation en Python

Les scripts Python utilisent le module `dotenv` pour charger les variables d'environnement et les rendre disponibles via `os.environ`.

### Exemple d'Utilisation

```python
import os
from dotenv import load_dotenv

# Charger les variables d'environnement
load_dotenv()

# Accéder aux configurations
openai_api_key = os.environ.get("OPENAI_API_KEY")
openrouter_api_key = os.environ.get("OPENROUTER_API_KEY")

# Utiliser un modèle spécifique
claude_api_key = os.environ.get("CLAUDE_SONNET_API_KEY", os.environ.get("OPENROUTER_API_KEY"))
claude_base_url = os.environ.get("CLAUDE_SONNET_BASE_URL", os.environ.get("OPENROUTER_BASE_URL"))
claude_model_id = os.environ.get("CLAUDE_SONNET_MODEL_ID")
```

### Dans le Module api_utils.py

Le module `api_utils.py` dans le répertoire `model_tester` définit un dictionnaire `API_CONFIGS` qui contient les configurations pour tous les modèles disponibles. Vous pouvez l'utiliser comme suit :

```python
from model_tester.api_utils import API_CONFIGS

# Utiliser la configuration OpenAI
openai_config = API_CONFIGS["openai"]
api_key = openai_config["api_key"]
base_url = openai_config["base_url"]

# Utiliser la configuration Claude Sonnet
claude_config = API_CONFIGS["claude_sonnet"]
```

## Utilisation en C#

Les applications C# utilisent un système de configuration basé sur des fichiers JSON et des variables d'environnement.

### Configuration dans testsettings.json

Le fichier `testsettings.json` dans le répertoire `dotnet/src/IntegrationTests` contient les configurations pour les tests d'intégration. Il est structuré comme suit :

```json
{
  "OpenAI": {
    "ServiceId": "text-davinci-003",
    "ModelId": "text-davinci-003",
    "ChatModelId": "gpt-3.5-turbo",
    "ApiKey": ""
  },
  "OpenRouter": {
    "ServiceId": "openrouter",
    "ApiKey": "",
    "BaseUrl": "https://openrouter.ai/api/v1",
    "Models": {
      "ClaudeSonnet": {
        "ModelId": "anthropic/claude-3-sonnet-20240229",
        "ChatModelId": "anthropic/claude-3-sonnet-20240229"
      },
      // Autres modèles...
    }
  }
}
```

### Exemple d'Utilisation en C#

```csharp
using Microsoft.Extensions.Configuration;

// Charger la configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile(path: "testsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(path: "testsettings.development.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<YourClass>()
    .Build();

// Accéder aux configurations OpenAI
var openAiConfiguration = configuration.GetSection("OpenAI").Get<OpenAIConfiguration>();
string apiKey = openAiConfiguration.ApiKey;
string modelId = openAiConfiguration.ModelId;

// Accéder aux configurations OpenRouter
var openRouterConfiguration = configuration.GetSection("OpenRouter").Get<OpenRouterConfiguration>();
string claudeModelId = openRouterConfiguration.Models.ClaudeSonnet.ModelId;
```

## Ajout de Nouveaux Modèles

### En Python

1. Ajoutez les variables d'environnement dans le fichier `.env` :
   ```
   NOUVEAU_MODELE_API_KEY=votre_clé_api
   NOUVEAU_MODELE_BASE_URL=url_de_base
   NOUVEAU_MODELE_MODEL_ID=id_du_modèle
   ```

2. Mettez à jour le dictionnaire `API_CONFIGS` dans `api_utils.py` :
   ```python
   API_CONFIGS = {
       # Configurations existantes...
       "nouveau_modele": {
           "api_key": os.environ.get("NOUVEAU_MODELE_API_KEY", "DEFAULT_KEY"),
           "base_url": os.environ.get("NOUVEAU_MODELE_BASE_URL", "DEFAULT_URL"),
           "model_id": os.environ.get("NOUVEAU_MODELE_MODEL_ID", "DEFAULT_MODEL_ID")
       }
   }
   ```

### En C#

1. Ajoutez la configuration dans `testsettings.json` :
   ```json
   {
     "OpenRouter": {
       "Models": {
         "NouveauModele": {
           "ModelId": "provider/nouveau-modele",
           "ChatModelId": "provider/nouveau-modele"
         }
       }
     }
   }
   ```

2. Créez ou mettez à jour la classe de configuration correspondante :
   ```csharp
   public class OpenRouterConfiguration
   {
       public string ServiceId { get; set; }
       public string ApiKey { get; set; }
       public string BaseUrl { get; set; }
       public OpenRouterModels Models { get; set; }
   }

   public class OpenRouterModels
   {
       public ModelConfig ClaudeSonnet { get; set; }
       public ModelConfig GeminiPro { get; set; }
       // Ajoutez votre nouveau modèle ici
       public ModelConfig NouveauModele { get; set; }
   }

   public class ModelConfig
   {
       public string ModelId { get; set; }
       public string ChatModelId { get; set; }
   }
   ```

---

Pour toute question ou problème concernant la configuration des modèles, veuillez ouvrir une issue sur le dépôt GitHub du projet.