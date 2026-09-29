# Système de Gestion des Prompts du MultiConnector

Ce répertoire contient les composants du système de gestion des prompts du MultiConnector, qui permettent de transformer et d'adapter les prompts en fonction des différents modèles et types de tâches.

## Vue d'ensemble

Le système de gestion des prompts du MultiConnector est conçu pour :

1. Identifier automatiquement le type de prompt en fonction de son contenu
2. Appliquer des transformations spécifiques à chaque type de prompt
3. Adapter les prompts aux caractéristiques de chaque modèle
4. Gérer les paramètres de génération spécifiques à chaque connecteur

Ce système permet d'optimiser les performances des différents modèles en leur fournissant des prompts adaptés à leurs capacités.

## Composants principaux

### PromptSignature

`PromptSignature` est responsable de l'identification du type de prompt en fonction de son début. Il permet de :

- Extraire une signature à partir d'un prompt
- Comparer des signatures pour déterminer le type de prompt
- Gérer un registre des types de prompts connus

### PromptTransform

`PromptTransform` gère la transformation des prompts en fonction de différents critères. Il supporte :

- La transformation basée sur un template
- Différents types d'interpolation pour remplacer des variables dans le template
- Des transformations conditionnelles en fonction du type de prompt

### PromptConnectorSettings

`PromptConnectorSettings` contient les paramètres spécifiques à un connecteur pour un type de prompt donné. Il inclut :

- Les paramètres de génération (température, max_tokens, etc.)
- Les transformations à appliquer
- Le niveau de validation (Vetting Level)

### PromptMultiConnectorSettings

`PromptMultiConnectorSettings` gère l'ensemble des paramètres pour tous les connecteurs et types de prompts. Il permet de :

- Configurer des paramètres globaux
- Définir des transformations spécifiques à chaque type de prompt
- Gérer les paramètres spécifiques à chaque connecteur

### PromptType

`PromptType` est une énumération des différents types de prompts supportés par le système. Elle inclut :

- Les types de prompts prédéfinis (Code, Math, Writing, etc.)
- Un type générique pour les prompts non catégorisés
- Des méthodes d'extension pour faciliter la manipulation des types

### MaxTokensAdjustment

`MaxTokensAdjustment` permet d'ajuster dynamiquement le nombre maximum de tokens en fonction de différents critères, comme :

- La longueur du prompt
- Le type de modèle
- La complexité de la tâche

## Types d'interpolation

Le système supporte trois types d'interpolation pour les templates de prompts :

1. **InterpolateKeys** : Remplacement simple des tokens `{keyName}` par leur valeur
2. **InterpolateFormattable** : Formatage avancé avec `{keyName:format}`
3. **InterpolateDynamicLinqExpression** : Expressions dynamiques complexes

## Utilisation

### Configuration de base

```csharp
// Créer une transformation de prompt simple
var promptTransform = new PromptTransform
{
    Template = "### System:\n{SystemSupplement}\n\n### User:\n{0}\n\n### Assistant:",
    InterpolationType = PromptInterpolationType.InterpolateKeys
};

// Configurer les paramètres pour un connecteur spécifique
var connectorSettings = new PromptConnectorSettings
{
    PromptTransform = promptTransform,
    Temperature = 0.7,
    MaxTokens = 1000
};

// Ajouter les paramètres à la configuration globale
var multiConnectorSettings = new PromptMultiConnectorSettings();
multiConnectorSettings.SetConnectorSettings("gpt-4o", PromptType.Code, connectorSettings);
```

### Utilisation des signatures de prompts

```csharp
// Créer une signature de prompt
var signature = new PromptSignature("Write a function that");

// Vérifier si un prompt correspond à cette signature
bool isMatch = signature.IsMatch("Write a function that calculates the factorial of a number");

// Enregistrer un type de prompt pour cette signature
PromptSignature.RegisterPromptType(signature, PromptType.Code);

// Obtenir le type de prompt pour un prompt donné
PromptType promptType = PromptSignature.GetPromptType("Write a function that calculates the factorial of a number");
```

### Transformation de prompts avancée

```csharp
// Créer une transformation avec interpolation formattable
var advancedTransform = new PromptTransform
{
    Template = "# Task: {TaskName}\n\n## Context:\n{Context}\n\n## Instructions:\n{Instructions}\n\n## Output Format:\n{OutputFormat:format}",
    InterpolationType = PromptInterpolationType.InterpolateFormattable
};

// Appliquer la transformation
string transformedPrompt = advancedTransform.Transform("Original prompt", new Dictionary<string, object>
{
    { "TaskName", "Code Generation" },
    { "Context", "We are developing a mathematical library" },
    { "Instructions", "Write a function to calculate factorial" },
    { "OutputFormat", "Python code with documentation" }
});
```

### Ajustement dynamique des tokens

```csharp
// Créer une fonction d'ajustement des tokens
var tokenAdjustment = new MaxTokensAdjustment
{
    BaseMaxTokens = 1000,
    AdjustmentFactor = 0.5,
    MinTokens = 100,
    MaxTokens = 2000
};

// Appliquer l'ajustement en fonction de la longueur du prompt
int adjustedMaxTokens = tokenAdjustment.GetAdjustedMaxTokens(promptLength: 500);
```

## Configuration avancée

### Paramètres globaux

```csharp
var multiConnectorSettings = new PromptMultiConnectorSettings
{
    GlobalParameters = new Dictionary<string, object>
    {
        { "SystemSupplement", "You are a helpful assistant specialized in programming." },
        { "UserPreamble", "I need help with the following task:" },
        { "SemanticRemarks", "Provide clear and concise code with comments." }
    }
};
```

### Transformations spécifiques au type de prompt

```csharp
// Créer des transformations spécifiques pour chaque type de prompt
var codeTransform = new PromptTransform
{
    Template = "# Coding Task\n\n{0}\n\nPlease provide the code in the requested language with proper documentation.",
    InterpolationType = PromptInterpolationType.InterpolateKeys
};

var mathTransform = new PromptTransform
{
    Template = "# Math Problem\n\n{0}\n\nPlease solve step by step, showing all your work.",
    InterpolationType = PromptInterpolationType.InterpolateKeys
};

// Configurer les transformations pour chaque type de prompt
multiConnectorSettings.SetPromptTypeTransform(PromptType.Code, codeTransform);
multiConnectorSettings.SetPromptTypeTransform(PromptType.Math, mathTransform);
```

### Configuration complète

```csharp
// Créer une configuration complète
var multiConnectorSettings = new PromptMultiConnectorSettings
{
    GlobalParameters = new Dictionary<string, object>
    {
        { "SystemSupplement", "You are a helpful assistant." }
    },
    GlobalPromptTransform = new PromptTransform
    {
        Template = "### System:\n{SystemSupplement}\n\n### User:\n{0}\n\n### Assistant:",
        InterpolationType = PromptInterpolationType.InterpolateKeys
    }
};

// Configurer les transformations spécifiques
multiConnectorSettings.SetPromptTypeTransform(PromptType.Code, codeTransform);
multiConnectorSettings.SetPromptTypeTransform(PromptType.Math, mathTransform);

// Configurer les paramètres spécifiques aux connecteurs
multiConnectorSettings.SetConnectorSettings("gpt-4o", PromptType.Code, new PromptConnectorSettings
{
    Temperature = 0.5,
    MaxTokens = 1500,
    PromptTransform = new PromptTransform
    {
        Template = "# GPT-4o Coding Task\n\n{0}\n\nPlease provide optimized code with detailed comments.",
        InterpolationType = PromptInterpolationType.InterpolateKeys
    }
});

multiConnectorSettings.SetConnectorSettings("claude-3.7-sonnet", PromptType.Writing, new PromptConnectorSettings
{
    Temperature = 0.7,
    MaxTokens = 2000,
    PromptTransform = new PromptTransform
    {
        Template = "<instructions>\n{0}\n</instructions>\n\n<format>\nClear and engaging prose\n</format>",
        InterpolationType = PromptInterpolationType.InterpolateKeys
    }
});
```

## Bonnes pratiques

1. **Utilisez des signatures précises** : Définissez des signatures de prompts précises pour permettre une identification fiable des types de prompts.

2. **Adaptez les templates aux modèles** : Créez des templates spécifiques à chaque modèle pour exploiter au mieux leurs capacités.

3. **Utilisez les paramètres globaux** : Définissez des paramètres globaux pour les éléments communs à tous les prompts.

4. **Ajustez les paramètres de génération** : Adaptez la température, le nombre maximum de tokens et les autres paramètres en fonction du type de prompt et du modèle.

5. **Testez les transformations** : Vérifiez que les transformations produisent des prompts bien formés et adaptés à chaque modèle.

## Intégration avec le MultiConnector

Le système de gestion des prompts est automatiquement intégré au MultiConnector lorsque vous configurez les paramètres. Vous n'avez pas besoin de l'instancier ou de le gérer manuellement, sauf si vous souhaitez un contrôle plus fin sur le processus de transformation.

Pour plus d'informations sur l'intégration avec le MultiConnector, consultez le [README du MultiConnector](../README.md).