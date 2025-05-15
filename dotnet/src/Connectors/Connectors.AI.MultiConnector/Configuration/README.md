# Configuration du MultiConnector

Ce répertoire contient les composants de configuration du MultiConnector, qui permettent de définir et de gérer les paramètres des différents connecteurs.

## Vue d'ensemble

Le système de configuration du MultiConnector est conçu pour :

1. Définir les paramètres de connexion aux différentes API de modèles de langage
2. Configurer les options spécifiques à chaque connecteur
3. Centraliser la gestion des clés API et des endpoints
4. Fournir une structure extensible pour ajouter de nouveaux connecteurs

## Composants principaux

### ConnectorConfigurationBase

`ConnectorConfigurationBase` est la classe de base pour toutes les configurations de connecteurs. Elle définit :

- Les propriétés communes à tous les connecteurs
- Les méthodes de validation de base
- L'interface pour l'initialisation des connecteurs

### MultiConnectorConfiguration

`MultiConnectorConfiguration` est la classe principale qui gère la configuration globale du MultiConnector. Elle contient :

- La liste des configurations de tous les connecteurs
- Les paramètres globaux du MultiConnector
- Les méthodes pour ajouter et gérer les connecteurs

### AzureOpenAIConfiguration

`AzureOpenAIConfiguration` est la configuration spécifique pour les connecteurs Azure OpenAI. Elle inclut :

- L'endpoint Azure OpenAI
- La clé API
- Le nom du déploiement
- Les options spécifiques à Azure OpenAI

### OpenAIConfiguration

`OpenAIConfiguration` est la configuration spécifique pour les connecteurs OpenAI. Elle inclut :

- La clé API OpenAI
- Le modèle à utiliser
- Les options spécifiques à OpenAI

### OobaboogaConnectorConfiguration

`OobaboogaConnectorConfiguration` est la configuration spécifique pour les connecteurs Oobabooga. Elle inclut :

- L'endpoint Oobabooga
- Les ports pour les API bloquantes et streaming
- Les options spécifiques à Oobabooga

### TokenCountFunction

`TokenCountFunction` est une fonction qui permet de calculer le nombre de tokens dans un texte. Elle est utilisée pour :

- Estimer le coût des requêtes
- Vérifier les limites de tokens
- Optimiser l'utilisation des modèles

## Utilisation

### Configuration de base

```csharp
// Créer une configuration pour le MultiConnector
var config = new MultiConnectorConfiguration
{
    // Ajouter une configuration OpenAI
    Connectors = new List<ConnectorConfigurationBase>
    {
        new OpenAIConfiguration
        {
            ModelId = "gpt-4o",
            ApiKey = "your-openai-api-key",
            IsDefault = true
        },
        
        // Ajouter une configuration Azure OpenAI
        new AzureOpenAIConfiguration
        {
            ModelId = "gpt-35-turbo",
            Endpoint = "https://your-resource.openai.azure.com/",
            ApiKey = "your-azure-openai-api-key",
            DeploymentName = "your-deployment-name"
        },
        
        // Ajouter une configuration Oobabooga
        new OobaboogaConnectorConfiguration
        {
            ModelId = "llama-7b",
            Endpoint = "http://localhost",
            BlockingPort = 5000,
            StreamingPort = 5005
        }
    }
};
```

### Utilisation avec le MultiConnector

```csharp
// Créer une configuration pour le MultiConnector
var config = new MultiConnectorConfiguration
{
    // Ajouter les configurations des connecteurs
    Connectors = new List<ConnectorConfigurationBase>
    {
        new OpenAIConfiguration
        {
            ModelId = "gpt-4o",
            ApiKey = "your-openai-api-key",
            IsDefault = true
        },
        new OobaboogaConnectorConfiguration
        {
            ModelId = "llama-7b",
            Endpoint = "http://localhost",
            BlockingPort = 5000,
            StreamingPort = 5005
        }
    }
};

// Créer les connecteurs à partir de la configuration
var connectors = new List<NamedTextCompletion>();
foreach (var connectorConfig in config.Connectors)
{
    var connector = connectorConfig.CreateConnector();
    connectors.Add(new NamedTextCompletion(connectorConfig.ModelId, connector));
}

// Créer le MultiConnector
var settings = new MultiTextCompletionSettings();
var multiConnector = new MultiTextCompletion(
    settings,
    connectors.First(c => c.Name == "gpt-4o"),
    connectors.Where(c => c.Name != "gpt-4o").ToArray());
```

### Chargement depuis un fichier de configuration

```csharp
// Charger la configuration depuis un fichier JSON
var configJson = File.ReadAllText("config.json");
var config = JsonSerializer.Deserialize<MultiConnectorConfiguration>(configJson);

// Créer les connecteurs à partir de la configuration
var connectors = new List<NamedTextCompletion>();
foreach (var connectorConfig in config.Connectors)
{
    var connector = connectorConfig.CreateConnector();
    connectors.Add(new NamedTextCompletion(connectorConfig.ModelId, connector));
}

// Créer le MultiConnector
var settings = new MultiTextCompletionSettings();
var multiConnector = new MultiTextCompletion(
    settings,
    connectors.First(c => c.IsDefault),
    connectors.Where(c => !c.IsDefault).ToArray());
```

## Configuration avancée

### Configuration avec options avancées

```csharp
// Créer une configuration OpenAI avec options avancées
var openAiConfig = new OpenAIConfiguration
{
    ModelId = "gpt-4o",
    ApiKey = "your-openai-api-key",
    IsDefault = true,
    Temperature = 0.7,
    MaxTokens = 1000,
    FrequencyPenalty = 0.5,
    PresencePenalty = 0.5,
    StopSequences = new List<string> { "###" }
};

// Créer une configuration Oobabooga avec options avancées
var oobaboogaConfig = new OobaboogaConnectorConfiguration
{
    ModelId = "llama-7b",
    Endpoint = "http://localhost",
    BlockingPort = 5000,
    StreamingPort = 5005,
    Temperature = 0.8,
    MaxTokens = 500,
    TopP = 0.9,
    TopK = 40,
    RepetitionPenalty = 1.1,
    TypicalP = 0.95
};

// Ajouter les configurations à la configuration globale
var config = new MultiConnectorConfiguration
{
    Connectors = new List<ConnectorConfigurationBase>
    {
        openAiConfig,
        oobaboogaConfig
    }
};
```

### Configuration avec fonction de comptage de tokens personnalisée

```csharp
// Créer une fonction de comptage de tokens personnalisée
var tokenCountFunction = new TokenCountFunction
{
    CountFunction = (text) => text.Split(' ').Length,  // Approximation simple
    CostPerInputToken = 0.00001m,
    CostPerOutputToken = 0.00002m
};

// Créer une configuration avec la fonction de comptage personnalisée
var openAiConfig = new OpenAIConfiguration
{
    ModelId = "gpt-4o",
    ApiKey = "your-openai-api-key",
    TokenCountFunction = tokenCountFunction
};
```

### Configuration avec paramètres globaux

```csharp
// Créer une configuration avec paramètres globaux
var config = new MultiConnectorConfiguration
{
    // Paramètres globaux
    GlobalParameters = new Dictionary<string, object>
    {
        { "SystemSupplement", "You are a helpful assistant." },
        { "UserPreamble", "I need help with the following task:" },
        { "SemanticRemarks", "Provide clear and concise answers." }
    },
    
    // Connecteurs
    Connectors = new List<ConnectorConfigurationBase>
    {
        new OpenAIConfiguration
        {
            ModelId = "gpt-4o",
            ApiKey = "your-openai-api-key",
            IsDefault = true
        },
        new OobaboogaConnectorConfiguration
        {
            ModelId = "llama-7b",
            Endpoint = "http://localhost",
            BlockingPort = 5000,
            StreamingPort = 5005
        }
    }
};
```

## Bonnes pratiques

1. **Sécurisez les clés API** : Ne stockez pas les clés API directement dans le code. Utilisez des variables d'environnement, des secrets utilisateur ou un gestionnaire de secrets.

2. **Validez les configurations** : Assurez-vous que toutes les configurations sont valides avant de les utiliser pour créer des connecteurs.

3. **Utilisez des valeurs par défaut sensées** : Définissez des valeurs par défaut appropriées pour les paramètres qui ne sont pas spécifiés explicitement.

4. **Centralisez la gestion des configurations** : Utilisez un fichier de configuration central pour gérer tous les connecteurs.

5. **Adaptez les paramètres à chaque modèle** : Chaque modèle a ses propres caractéristiques et paramètres optimaux. Adaptez les configurations en conséquence.

## Exemple de fichier de configuration JSON

```json
{
  "GlobalParameters": {
    "SystemSupplement": "You are a helpful assistant.",
    "UserPreamble": "I need help with the following task:",
    "SemanticRemarks": "Provide clear and concise answers."
  },
  "Connectors": [
    {
      "Type": "OpenAI",
      "ModelId": "gpt-4o",
      "ApiKey": "your-openai-api-key",
      "IsDefault": true,
      "Temperature": 0.7,
      "MaxTokens": 1000
    },
    {
      "Type": "AzureOpenAI",
      "ModelId": "gpt-35-turbo",
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "your-azure-openai-api-key",
      "DeploymentName": "your-deployment-name",
      "Temperature": 0.5,
      "MaxTokens": 800
    },
    {
      "Type": "Oobabooga",
      "ModelId": "llama-7b",
      "Endpoint": "http://localhost",
      "BlockingPort": 5000,
      "StreamingPort": 5005,
      "Temperature": 0.8,
      "MaxTokens": 500
    }
  ]
}
```

## Intégration avec le MultiConnector

Le système de configuration est automatiquement intégré au MultiConnector lorsque vous créez des connecteurs à partir des configurations. Vous n'avez pas besoin de l'instancier ou de le gérer manuellement, sauf si vous souhaitez un contrôle plus fin sur le processus de configuration.

Pour plus d'informations sur l'intégration avec le MultiConnector, consultez le [README du MultiConnector](../README.md).