# Mocks Arithmétiques pour le MultiConnector

Ce répertoire contient des implémentations de mocks arithmétiques qui simulent des modèles de langage pour les tests et le développement du MultiConnector.

## Vue d'ensemble

Les mocks arithmétiques sont des implémentations simplifiées qui simulent le comportement des modèles de langage en se concentrant sur des opérations arithmétiques de base. Ils permettent de tester le MultiConnector sans avoir besoin d'accéder à de véritables API de modèles de langage, ce qui est particulièrement utile pour :

1. Le développement et les tests unitaires
2. La démonstration des fonctionnalités du MultiConnector
3. Le débogage des problèmes de routage et de transformation
4. Les tests de performance et de robustesse

## Composants principaux

### ArithmeticCompletionService

`ArithmeticCompletionService` est une implémentation de `ITextCompletion` qui simule un service de complétion de texte en effectuant des opérations arithmétiques simples. Il peut :

- Reconnaître et résoudre des expressions arithmétiques dans les prompts
- Simuler des délais de réponse pour imiter le comportement des API réelles
- Générer des réponses en streaming pour tester les fonctionnalités de streaming

### ArithmeticEngine

`ArithmeticEngine` est le moteur qui analyse et résout les expressions arithmétiques. Il supporte :

- Les opérations de base (addition, soustraction, multiplication, division)
- La détection des opérations à partir de texte en langage naturel
- La génération de réponses formatées

### ArithmeticOperation

`ArithmeticOperation` est une énumération des différentes opérations arithmétiques supportées par le mock :

- Addition
- Soustraction
- Multiplication
- Division

### ArithmeticStreamingResultBase

`ArithmeticStreamingResultBase` est la classe de base pour les résultats en streaming. Elle fournit :

- Une implémentation de base pour le streaming de résultats
- Des méthodes pour simuler la génération progressive de réponses

### ArithmeticComputingStreamingResult

`ArithmeticComputingStreamingResult` est une implémentation spécifique pour le streaming des calculs arithmétiques. Elle :

- Simule le calcul étape par étape
- Génère des réponses partielles pour tester le streaming

### ArithmeticVettingStreamingResult

`ArithmeticVettingStreamingResult` est une implémentation spécifique pour le streaming des résultats de validation. Elle :

- Simule le processus de validation
- Génère des réponses de validation pour tester le système d'analyse

## Utilisation

### Configuration de base

```csharp
// Créer un service de complétion arithmétique
var arithmeticCompletion = new ArithmeticCompletionService
{
    Name = "ArithmeticMock",
    DelayMilliseconds = 100  // Simuler un délai de 100ms
};

// Créer une instance nommée pour l'utilisation avec le MultiConnector
var namedArithmeticCompletion = new NamedTextCompletion("ArithmeticMock", arithmeticCompletion);
```

### Intégration avec le MultiConnector

```csharp
// Créer une instance des paramètres du MultiConnector
var settings = new MultiTextCompletionSettings();

// Créer des instances des connecteurs
var openAiNamedCompletion = new NamedTextCompletion("Primary", openAiCompletion);
var arithmeticCompletion = new ArithmeticCompletionService { Name = "ArithmeticMock" };
var namedArithmeticCompletion = new NamedTextCompletion("ArithmeticMock", arithmeticCompletion);

// Configurer le Kernel avec le MultiConnector
var builder = Kernel.Builder;
builder.WithMultiConnectorCompletionService(
    settings: settings,
    mainTextCompletion: openAiNamedCompletion,
    setAsDefault: true,
    otherCompletions: new[] { namedArithmeticCompletion });

var kernel = builder.Build();
```

### Utilisation directe

```csharp
// Créer un service de complétion arithmétique
var arithmeticCompletion = new ArithmeticCompletionService();

// Obtenir une complétion pour une expression arithmétique
var result = await arithmeticCompletion.CompleteAsync(
    "Calculate 25 + 17",
    new CompleteRequestSettings
    {
        MaxTokens = 100,
        Temperature = 0.0
    });

Console.WriteLine(result);  // Affiche "42"
```

### Utilisation avec streaming

```csharp
// Créer un service de complétion arithmétique
var arithmeticCompletion = new ArithmeticCompletionService();

// Obtenir une complétion en streaming
await foreach (var chunk in arithmeticCompletion.GetStreamingCompletionAsync(
    "Calculate 25 + 17",
    new CompleteRequestSettings
    {
        MaxTokens = 100,
        Temperature = 0.0
    }))
{
    Console.Write(chunk);  // Affiche progressivement "42"
}
```

## Personnalisation

### Ajustement des délais

```csharp
// Créer un service de complétion arithmétique avec un délai personnalisé
var arithmeticCompletion = new ArithmeticCompletionService
{
    DelayMilliseconds = 500  // Simuler un délai de 500ms
};
```

### Simulation d'erreurs

```csharp
// Créer un service de complétion arithmétique qui simule des erreurs
var arithmeticCompletion = new ArithmeticCompletionService
{
    ErrorRate = 0.2  // 20% de chance d'erreur
};
```

### Personnalisation des réponses

```csharp
// Créer un service de complétion arithmétique avec des réponses personnalisées
var arithmeticCompletion = new ArithmeticCompletionService
{
    ResponseFormatter = (operation, result) => 
        $"The result of {operation} is {result}. This was calculated by the arithmetic mock."
};
```

## Cas d'utilisation

### Tests unitaires

Les mocks arithmétiques sont particulièrement utiles pour les tests unitaires du MultiConnector :

```csharp
[Fact]
public async Task MultiConnector_Should_Route_To_Arithmetic_Mock_For_Math_Prompts()
{
    // Arrange
    var settings = new MultiTextCompletionSettings();
    var arithmeticCompletion = new ArithmeticCompletionService();
    var namedArithmeticCompletion = new NamedTextCompletion("ArithmeticMock", arithmeticCompletion);
    
    var multiCompletion = new MultiTextCompletion(
        settings,
        new NamedTextCompletion("Primary", new MockTextCompletion()),
        new[] { namedArithmeticCompletion });
    
    // Act
    var result = await multiCompletion.CompleteAsync("Calculate 25 + 17");
    
    // Assert
    Assert.Equal("42", result.Text);
}
```

### Démonstrations

Les mocks arithmétiques sont également utiles pour les démonstrations et les notebooks :

```csharp
// Créer un service de complétion arithmétique pour la démonstration
var arithmeticCompletion = new ArithmeticCompletionService
{
    DelayMilliseconds = 500,  // Délai visible pour la démonstration
    ResponseFormatter = (operation, result) => 
        $"The result of {operation} is {result}. This was calculated instantly without API calls."
};

// Démontrer le routage du MultiConnector
Console.WriteLine("Sending math prompt to MultiConnector...");
var result = await multiCompletion.CompleteAsync("Calculate 25 + 17");
Console.WriteLine($"Result: {result.Text}");
```

## Limitations

Les mocks arithmétiques ont plusieurs limitations :

1. Ils ne supportent que les opérations arithmétiques simples
2. Ils ne peuvent pas traiter des prompts complexes ou en langage naturel
3. Ils ne simulent pas toutes les caractéristiques des véritables modèles de langage
4. Ils sont principalement destinés aux tests et aux démonstrations

## Intégration avec le MultiConnector

Les mocks arithmétiques sont automatiquement intégrés au MultiConnector lorsque vous les ajoutez comme connecteurs secondaires. Le système d'analyse du MultiConnector les évaluera et les utilisera pour les prompts arithmétiques si leurs performances sont satisfaisantes.

Pour plus d'informations sur l'intégration avec le MultiConnector, consultez le [README du MultiConnector](../README.md).