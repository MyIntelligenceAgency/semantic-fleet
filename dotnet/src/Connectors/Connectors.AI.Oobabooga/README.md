# Connecteur Oobabooga pour Semantic Kernel

Ce connecteur permet d'intégrer les modèles de langage hébergés via [Oobabooga Text Generation WebUI](https://github.com/oobabooga/text-generation-webui) dans vos applications Semantic Kernel.

## Vue d'ensemble

Le connecteur Oobabooga fournit une interface entre Semantic Kernel et les modèles de langage open-source hébergés localement via Oobabooga. Il prend en charge :

- La complétion de texte (bloquante et streaming)
- La complétion de chat (bloquante et streaming)
- La configuration avancée des paramètres de génération
- L'intégration transparente avec le MultiConnector

Ce connecteur est particulièrement utile pour :
- Exécuter des modèles localement sans dépendre d'API externes
- Réduire les coûts en utilisant des modèles open-source
- Expérimenter avec différents modèles et paramètres
- Assurer la confidentialité des données en gardant tout en local

## 🚨 Avertissement de compatibilité

En raison de récents changements dans l'API Oobabooga (voir [commit 454fcf3 du 13/11/2023](https://github.com/oobabooga/text-generation-webui/commit/454fcf39a95691f5e375c48fbc6fe6aa96f0c738)), **toutes les versions d'Oobabooga au-delà de ce commit ne sont plus prises en charge** par ce connecteur.

Le concepteur d'Oobabooga a remplacé l'API traditionnelle par une nouvelle API modelée sur celle d'OpenAI. Nous travaillons à mettre à jour le connecteur pour supporter cette nouvelle API.

## Installation

### Via NuGet

```bash
dotnet add package MyIA.SemanticKernel.Connectors.AI.Oobabooga
```

### Dans .NET Interactive

```csharp
#r "nuget: MyIA.SemanticKernel.Connectors.AI.Oobabooga"
```

## Configuration d'Oobabooga

Avant d'utiliser le connecteur, vous devez installer et configurer Oobabooga Text Generation WebUI :

1. Suivez les instructions d'installation sur le [dépôt GitHub d'Oobabooga](https://github.com/oobabooga/text-generation-webui)
2. Téléchargez les modèles que vous souhaitez utiliser
3. Lancez Oobabooga avec les API activées

Pour plus de détails, consultez notre [guide d'installation d'Oobabooga](../../../../../docs/OOBABOOGA.md).

## Utilisation

### Complétion de texte

```csharp
// Créer les paramètres pour la complétion de texte
var settings = new OobaboogaTextCompletionSettings(
    endpoint: new Uri("http://localhost/"),
    blockingPort: 5000,
    streamingPort: 5005);

// Créer l'instance de complétion de texte
var oobabooga = new OobaboogaTextCompletion(settings);

// Configurer les paramètres de la requête
var requestSettings = new OobaboogaCompletionRequestSettings
{
    MaxTokens = 100,
    Temperature = 0.7,
    TopP = 0.9,
    RepetitionPenalty = 1.1
};

// Obtenir une complétion de texte (mode bloquant)
var completion = await oobabooga.CompleteAsync("Écrivez un poème sur l'intelligence artificielle", requestSettings);
Console.WriteLine(completion.Text);

// Obtenir une complétion de texte (mode streaming)
await foreach (var chunk in oobabooga.GetStreamingCompletionAsync(
    "Écrivez un poème sur l'intelligence artificielle", requestSettings))
{
    Console.Write(chunk);
}
```

### Complétion de chat

```csharp
// Créer les paramètres pour la complétion de chat
var settings = new OobaboogaChatCompletionSettings(
    endpoint: new Uri("http://localhost/"),
    blockingPort: 5000,
    streamingPort: 5005);

// Créer l'instance de complétion de chat
var oobabooga = new OobaboogaChatCompletion(settings);

// Créer l'historique du chat
var chatHistory = new ChatHistory();
chatHistory.AddUserMessage("Bonjour, pouvez-vous m'aider à résoudre un problème de mathématiques ?");
chatHistory.AddAssistantMessage("Bien sûr, je serais ravi de vous aider. Quel est le problème ?");

// Configurer les paramètres de la requête
var requestSettings = new OobaboogaChatRequestSettings
{
    MaxTokens = 200,
    Temperature = 0.5,
    TopP = 0.95
};

// Obtenir une complétion de chat (mode bloquant)
chatHistory.AddUserMessage("Calculez l'intégrale de x^2 dx");
var completion = await oobabooga.GetChatCompletionsAsync(chatHistory, requestSettings);
Console.WriteLine(completion.First().Content);

// Obtenir une complétion de chat (mode streaming)
chatHistory.AddUserMessage("Expliquez la dérivation de cette intégrale");
await foreach (var chunk in oobabooga.GetStreamingChatCompletionsAsync(chatHistory, requestSettings))
{
    Console.Write(chunk.Content);
}
```

### Intégration avec Semantic Kernel

```csharp
// Créer un builder de kernel
var builder = new KernelBuilder();

// Ajouter le service de complétion Oobabooga
builder.WithOobaboogaTextCompletionService(
    serviceId: "oobabooga",
    endpoint: new Uri("http://localhost/"),
    blockingPort: 5000,
    streamingPort: 5005);

// Construire le kernel
var kernel = builder.Build();

// Créer une fonction sémantique
var prompt = "{{$input}}\n\nRésumez ce texte en une phrase.";
var summarize = kernel.CreateSemanticFunction(prompt, maxTokens: 100);

// Exécuter la fonction
var result = await summarize.InvokeAsync("L'intelligence artificielle (IA) est un domaine de l'informatique qui vise à créer des systèmes capables d'effectuer des tâches qui nécessiteraient normalement l'intelligence humaine. Ces tâches comprennent l'apprentissage, le raisonnement, la résolution de problèmes, la perception et la compréhension du langage naturel. L'IA peut être classée en deux catégories principales : l'IA faible, qui est conçue pour effectuer une tâche spécifique, et l'IA forte, qui possède les capacités cognitives d'un être humain.");
Console.WriteLine(result);
```

### Intégration avec MultiConnector

```csharp
// Créer les paramètres pour la complétion de texte Oobabooga
var oobaboogaSettings = new OobaboogaTextCompletionSettings(
    endpoint: new Uri("http://localhost/"),
    blockingPort: 5000,
    streamingPort: 5005);

// Créer l'instance de complétion de texte Oobabooga
var oobabooga = new OobaboogaTextCompletion(oobaboogaSettings);
var namedOobabooga = new NamedTextCompletion("llama-7b", oobabooga);

// Créer l'instance de complétion de texte OpenAI
var openAiSettings = new OpenAITextCompletionSettings("your-api-key", "gpt-3.5-turbo");
var openAi = new OpenAITextCompletion(openAiSettings);
var namedOpenAi = new NamedTextCompletion("gpt-3.5-turbo", openAi);

// Créer les paramètres du MultiConnector
var multiConnectorSettings = new MultiTextCompletionSettings();

// Créer le MultiConnector
var multiConnector = new MultiTextCompletion(
    multiConnectorSettings,
    namedOpenAi,
    new[] { namedOobabooga });

// Utiliser le MultiConnector
var result = await multiConnector.CompleteAsync(
    "Expliquez le concept d'intelligence artificielle",
    new CompleteRequestSettings { MaxTokens = 100 });
Console.WriteLine(result.Text);
```

## Paramètres avancés

### OobaboogaCompletionRequestSettings

Le connecteur Oobabooga prend en charge de nombreux paramètres pour contrôler la génération de texte :

```csharp
var requestSettings = new OobaboogaCompletionRequestSettings
{
    // Paramètres de base
    MaxTokens = 200,           // Nombre maximum de tokens à générer
    Temperature = 0.7,         // Contrôle la créativité (0.0 = déterministe, 1.0 = créatif)
    TopP = 0.9,                // Probabilité cumulative pour le sampling
    
    // Paramètres avancés
    TopK = 40,                 // Limite le sampling aux K tokens les plus probables
    RepetitionPenalty = 1.1,   // Pénalité pour la répétition de tokens
    PresencePenalty = 0.0,     // Pénalité pour la présence de tokens spécifiques
    FrequencyPenalty = 0.0,    // Pénalité basée sur la fréquence des tokens
    
    // Paramètres spécifiques à Oobabooga
    TypicalP = 0.95,           // Sampling typique
    TfsZ = 1.0,                // Tail-free sampling
    TopA = 0.0,                // Top-A sampling
    Mirostat = 0,              // Mode Mirostat (0 = désactivé)
    MirostatTau = 5.0,         // Paramètre Tau pour Mirostat
    MirostatEta = 0.1,         // Paramètre Eta pour Mirostat
    
    // Contrôle du comportement
    DoSample = true,           // Activer le sampling (vs greedy decoding)
    EarlyStopping = false,     // Arrêt anticipé de la génération
    UseCache = true,           // Utiliser le cache pour les requêtes répétées
    
    // Séquences d'arrêt
    StopSequences = new List<string> { "###", "User:" }
};
```

### Format de chat

Le connecteur Oobabooga prend en charge différents formats de chat pour s'adapter aux différents modèles :

```csharp
var settings = new OobaboogaChatCompletionSettings(
    endpoint: new Uri("http://localhost/"),
    blockingPort: 5000,
    streamingPort: 5005)
{
    // Format de chat pour Llama 2
    ChatFormat = @"<s>[INST] {{prompt}} [/INST]",
    MessageFormat = @"[INST] {{prompt}} [/INST]",
    ResponseFormat = @"{{response}}",
    
    // Autres formats disponibles :
    // ChatFormat = @"### Instruction:\n{{prompt}}\n\n### Response:",  // Alpaca
    // ChatFormat = @"USER: {{prompt}}\nASSISTANT:",                  // Vicuna
    // ChatFormat = @"<human>: {{prompt}}\n<bot>:",                   // OpenAssistant
};
```

## Modèles recommandés

Le connecteur Oobabooga fonctionne avec de nombreux modèles, mais voici quelques recommandations :

### Petits modèles (< 7B)
- TinyLlama (1.1B)
- Phi-2 (2.7B)
- StableLM-2-1.6B (1.6B)
- Gemma-2B

### Modèles moyens (7B-13B)
- Llama-2-7B-Chat
- Mistral-7B-Instruct
- Vicuna-13B
- Neural-Chat-7B

### Grands modèles (> 13B)
- Llama-2-13B-Chat
- Wizard-Vicuna-30B
- Falcon-40B-Instruct

## Résolution des problèmes

### Problèmes de connexion

Si vous rencontrez des problèmes de connexion à Oobabooga :

1. Vérifiez que Oobabooga est en cours d'exécution avec les API activées
2. Assurez-vous que les ports spécifiés correspondent à ceux configurés dans Oobabooga
3. Vérifiez que l'endpoint est accessible (généralement `http://localhost/`)

### Erreurs de génération

Si vous rencontrez des erreurs lors de la génération de texte :

1. Vérifiez que le modèle est correctement chargé dans Oobabooga
2. Réduisez la valeur de `MaxTokens` si vous obtenez des erreurs de mémoire
3. Ajustez les paramètres de génération (température, top_p, etc.) pour améliorer la qualité

### Problèmes de performance

Si vous rencontrez des problèmes de performance :

1. Utilisez un modèle plus petit ou une quantification plus agressive
2. Réduisez la valeur de `MaxTokens` pour générer des réponses plus courtes
3. Activez `UseCache` pour réutiliser les résultats des requêtes répétées

## Bonnes pratiques

1. **Adaptez les prompts au modèle** : Les modèles plus petits nécessitent des prompts plus directs et plus simples.

2. **Ajustez les paramètres de génération** : Chaque modèle a ses propres paramètres optimaux. Expérimentez pour trouver les meilleurs.

3. **Utilisez le streaming pour les réponses longues** : Le mode streaming permet d'afficher les résultats progressivement, améliorant l'expérience utilisateur.

4. **Combinez avec MultiConnector** : Utilisez le MultiConnector pour basculer automatiquement entre différents modèles en fonction de la complexité de la tâche.

5. **Préparez des formats de chat adaptés** : Chaque famille de modèles (Llama, Vicuna, etc.) a son propre format de chat optimal.

## Ressources additionnelles

- [Guide d'installation d'Oobabooga](../../../../../docs/OOBABOOGA.md)
- [Documentation du MultiConnector](../Connectors.AI.MultiConnector/README.md)
- [Guide d'intégration des petits modèles](../../../../../docs/SMALL_MODELS_INTEGRATION.md)
- [Notebooks d'exemples](../../../../notebooks/README.md)

## Licence

Ce projet est sous licence MIT. Voir le fichier LICENSE pour plus de détails.