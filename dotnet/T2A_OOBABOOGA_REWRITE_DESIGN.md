# T2a — Oobabooga Connector Rewrite Design (file-level)

**Source grain :** Issue [#6853](https://github.com/jsboige/CoursIA/issues/6853) (Axe 2 SK upgrade), plan tranche-2 du `BREAKING_CHANGES_INVENTORY.md`.
**Branche :** `upgrade-sk` dans ce submodule.
**Date :** 2026-07-18 (po-2026 c.476).
**Statut :** design de la réécriture tranche 2a, prêt pour exécution sur fenêtre dédiée (~2-4h focus).

## Objectif T2a (mesurable)

Faire compiler le projet `Connectors.AI.Oobabooga.csproj` : **83 erreurs → 0**.
Effet aval : débloque MSBuild pour `Connectors.AI.MultiConnector` (suite de T2b)
puis `Connectors.UnitTests` (T2c). **PAS de bump du pointeur submodule CoursIA**
dans T2a (acceptance #5 : la solution doit être verte d'abord).

## Pourquoi c'est atomique (pas incrémental par étape)

MSBuild s'arrête au premier projet en échec. Toutes les 83 erreurs sont dans le
projet Oobabooga, et tout y est couplé via la classe de base générique
`OobaboogaCompletionBase<TInput, TParams, TRequest, TResponse, TResult, TStreaming>`
qui produit les result-wrappers obsolètes (`TCompletionResult`/`TCompletionStreamingResult`).
Supprimer un wrapper casse les services qui le référencent ; réécrire un service
casse la base qui le typed-parameter. → un seul commit atomique fait passer
83 → 0. Les commits intermédiaires augmenteraient temporairement le count.

## Signatures SK 1.78 VÉRIFIÉES contre l'assembly

(confirmées par lecture `Microsoft.SemanticKernel.Abstractions.xml` 1.78.0 / net8.0,
pas devinées)

```csharp
// Microsoft.SemanticKernel.ChatCompletion
public interface IChatCompletionService : IAIService
{
    Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory, PromptExecutionSettings? executionSettings,
        Kernel? kernel, CancellationToken cancellationToken);
    IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory, PromptExecutionSettings? executionSettings,
        Kernel? kernel, CancellationToken cancellationToken);
}

// Microsoft.SemanticKernel.TextGeneration
public interface ITextGenerationService : IAIService
{
    Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt, PromptExecutionSettings? executionSettings,
        Kernel? kernel, CancellationToken cancellationToken);
    IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt, PromptExecutionSettings? executionSettings,
        Kernel? kernel, CancellationToken cancellationToken);
}

// ChatMessageContent properties: Content, Role, Items, Encoding, AuthorName, Source
// AuthorRole : Microsoft.SemanticKernel.ChatCompletion (Assistant/User/System)
// IAIService : Attributes IReadOnlyDictionary<string, object?>, Metadata IDictionary
// DI registration SK 1.x : IKernelBuilder.Services.AddKeyedSingleton<TService>(serviceId, factory)
//   ou Kernel.KernelBuilderExtensions. Pas de WithAIService<T>.
```

## Plan de réécriture file-level

### 1. `Completion/OobaboogaCompletionBase.cs` (la fondation — REFAIRE la signature générique)

- **Retirer** les params type `TCompletionResult` + `TCompletionStreamingResult` de la
  classe générique. Nouvelle arité :
  `OobaboogaCompletionBase<TCompletionInput, TOobaboogaParameters, TCompletionRequest, TCompletionResponse>`
  (4 params, + la contrainte `TOobaboogaParameters : OobaboogaCompletionRequestSettings, new()`).
- `GetCompletionsBaseAsync` → retourne `Task<IReadOnlyList<string>>` (les textes
  extraits de la réponse). Le flux HTTP (SendAsync → deserialize `TCompletionResponse`)
  reste IDENTIQUE ; remplacer l'appel `this.GetCompletionResults(completionResponse)`
  par `this.ExtractCompletionTexts(completionResponse)`.
- `GetStreamingCompletionsBaseAsync` → retourne `IAsyncEnumerable<string>`. Le flux
  websocket est RECONÇU : `ProcessWebSocketMessagesAsync` écrit dans un
  `Channel<string>` (bounded) au lieu d'un `CompletionStreamingResultBase`. Le
  consumer yield les chaînes.
- Méthode abstraite : `protected abstract IReadOnlyList<string> ExtractCompletionTexts(TCompletionResponse)`.
- La base non-générique `OobaboogaCompletionBase` : `GetResponseObject` reste abstrait
  (retourne `CompletionStreamingResponseBase?`). `ProcessWebSocketMessagesAsync` reçoit
  un `ChannelWriter<string>` au lieu d'un `CompletionStreamingResultBase` ; sur
  `TextStreamEvent`, extraire le text chunk du responseObject et `channel.Write(text)`;
  sur `StreamEndEvent`, `channel.Complete()`.

### 2. SUPPRIMER (5 result-wrappers obsolètes — ne sont plus référencés)

- `Completion/ChatCompletion/ChatCompletionResult.cs`
- `Completion/ChatCompletion/ChatCompletionStreamingResult.cs`
- `Completion/TextCompletion/TextCompletionResult.cs`
- `Completion/TextCompletion/TextCompletionStreamingResult.cs`
- `Completion/CompletionStreamingResultBase.cs`

### 3. `Completion/ChatCompletion/OobaboogaChatCompletion.cs` (RÉÉCRIRE — IChatCompletionService)

```csharp
public sealed class OobaboogaChatCompletion
    : OobaboogaCompletionBase<ChatHistory, OobaboogaChatCompletionRequestSettings,
                              OobaboogaChatCompletionRequest, ChatCompletionResponse>,
      IChatCompletionService
{
    // ctor inchangé (settings)
    // CreateNewChat : ChatHistory now in .ChatCompletion namespace — update using only

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory, PromptExecutionSettings? executionSettings,
        Kernel? kernel, CancellationToken cancellationToken)
    {
        var texts = await this.GetCompletionsBaseAsync(chatHistory,
            OobaboogaCompletionRequestSettings.FromPromptExecutionSettings(executionSettings),
            cancellationToken);
        return texts.Select(t => new ChatMessageContent(AuthorRole.Assistant, t))
                    .ToList();
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory, PromptExecutionSettings? executionSettings,
        Kernel? kernel, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var text in this.GetStreamingCompletionsBaseAsync(chatHistory,
            OobaboogaCompletionRequestSettings.FromPromptExecutionSettings(executionSettings),
            cancellationToken))
        {
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, text);
        }
    }

    // ExtractCompletionTexts : from ChatCompletionResponse → list of result strings
    //   (was: build ChatCompletionResult wrappers)
    // CreateCompletionRequest : inchangé (déjà bon)
}
```

**Point d'attention :** l'ancien `OobaboogaChatCompletion` implémentait AUSSI
`ITextCompletion` (GetCompletionsAsync(string text)). En SK 1.78, `ITextGenerationService`
est une interface distincte. Soit déléguer au `OobaboogaTextCompletion` dans la DI
(l'extension `alsoAsTextCompletion` enregistre déjà 2 services), soit réimplémenter
`ITextGenerationService` ici. **Décision recommandée : ne pas implémenter
ITextGenerationService dans la classe chat** — laisser l'extension DI enregistrer
`OobaboogaTextCompletion` séparément (pattern plus propre, moins de double-API).

### 4. `Completion/TextCompletion/OobaboogaTextCompletion.cs` (RÉÉCRIRE — ITextGenerationService)

```csharp
public sealed class OobaboogaTextCompletion
    : OobaboogaCompletionBase<string, OobaboogaCompletionRequestSettings,
                              OobaboogaCompletionRequest, TextCompletionResponse>,
      ITextGenerationService
{
    public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt, PromptExecutionSettings? executionSettings,
        Kernel? kernel, CancellationToken cancellationToken)
    {
        var texts = await this.GetCompletionsBaseAsync(prompt,
            OobaboogaCompletionRequestSettings.FromPromptExecutionSettings(executionSettings),
            cancellationToken);
        return texts.Select(t => new TextContent(t, this, modelId: null)).ToList();
    }

    public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt, PromptExecutionSettings? executionSettings,
        Kernel? kernel, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var text in this.GetStreamingCompletionsBaseAsync(prompt,
            OobaboogaCompletionRequestSettings.FromPromptExecutionSettings(executionSettings),
            cancellationToken))
        {
            yield return new StreamingTextContent(text);
        }
    }
}
```

### 5. `OobaboogaKernelBuilderExtensions.cs` (DI — migrer vers IKernelBuilder + AddKeyedSingleton)

```csharp
public static class OobaboogaKernelBuilderExtensions
{
    public static IKernelBuilder AddOobaboogaTextGeneration(
        this IKernelBuilder builder, OobaboogaTextCompletionSettings settings,
        string? serviceId = null, bool setAsDefault = false)
    {
        builder.Services.AddKeyedSingleton<ITextGenerationService>(serviceId,
            (sp, _) => new OobaboogaTextCompletion(settings));
        if (setAsDefault)
            builder.Services.AddKeyedSingleton<ITextGenerationService>(null,
                (sp, _) => new OobaboogaTextCompletion(settings));
        return builder;
    }

    public static IKernelBuilder AddOobaboogaChatCompletion(
        this IKernelBuilder builder, OobaboogaChatCompletionSettings settings,
        string? serviceId = null, bool alsoAsTextGeneration = true,
        bool setAsDefault = false)
    {
        builder.Services.AddKeyedSingleton<IChatCompletionService>(serviceId,
            (sp, _) => new OobaboogaChatCompletion(settings));
        if (alsoAsTextGeneration)
            builder.Services.AddKeyedSingleton<ITextGenerationService>(serviceId,
                (sp, _) => new OobaboogaChatCompletion(settings));
        return builder;
    }
}
```

### 6. `InternalUtilities/src/Diagnostics/Verify.cs` — `ParameterView` → `KernelArguments`

### 7. `InternalUtilities/src/Http/HttpClientProvider.cs` — `IDelegatingHandlerFactory` SUPPRIMÉ en SK 1.78

Recoder avec `HttpClient` direct (pas de factory de handler). Si l'ancien code
construisait un `HttpClient(handler, disposeHandler)` via la factory, remplacer par
`new HttpClient()` ou un `IHttpClientFactory` (.NET standard). Vérifier les usages.

## Risques à valider pendant l'exécution

- **`OobaboogaCompletionRequestSettings.FromRequestSettings`** : la méthode prend
  l'ancien `AIRequestSettings`. En SK 1.78 c'est `PromptExecutionSettings`. Le helper
  `FromRequestSettings(requestSettings, null)` doit être migré en
  `FromPromptExecutionSettings(executionSettings)` (signature du settings parser).
  Vérifier le corps de `OobaboogaCompletionRequestSettings`.
- **`ChatHistory`/`AddUserMessage`/`AddSystemMessage`** : API stable en SK 1.78,
  namespace `.ChatCompletion` seulement — `using` change, corps inchangé.
- **`Text.Json.Deserialize<ChatCompletionStreamingResponse>`** : inchangé.
- **`Verify.NotEmptyList(chat, ...)`** : `Verify` custom dans InternalUtilities —
  après fix `ParameterView`, devrait compiler. Vérifier que `NotEmptyList` existe.
- **`Telemetry.HttpUserAgent` / `HttpRequest.CreatePostRequest`** : helpers SK internes
  possiblement déplacés. Vérifier dans SK 1.78 (`Microsoft.SemanticKernel.Http`).
- **`SKException`** : namespace `Microsoft.SemanticKernel` — stable.

## Ordre d'exécution (commit unique atomique)

1. Éditer `OobaboogaCompletionBase.cs` (nouvelle arité + signatures base).
2. Éditer `OobaboogaChatCompletion.cs` + `OobaboogaTextCompletion.cs` (nouvelles
   interfaces + `ExtractCompletionTexts`).
3. Supprimer les 5 result-wrappers.
4. Éditer `OobaboogaKernelBuilderExtensions.cs` (DI).
5. Éditer `Verify.cs` + `HttpClientProvider.cs` (InternalUtilities).
6. `dotnet build dotnet/Semantic-Fleet-dotnet.sln -c Release` — mesurer 83 → N.
7. Itérer sur les CS résiduels (Text.Json, Telemetry, namespaces) jusqu'à 0 Oobabooga.
8. Commit atomique, mesurer `MultiConnector` erreurs suivantes (T2b).

## Acceptance T2a

- [ ] `dotnet build` : projet `Connectors.AI.Oobabooga.csproj` = 0 erreur.
- [ ] Solution build avance au-delà d'Oobabooga (MultiConnector erreurs exposées = T2b).
- [ ] PAS de bump pointeur submodule CoursIA (acceptance #5).
- [ ] `See #6853` (epic Axe 2 pas résolu par T2a seul).

`See #6853` (epic Axe 2, tranche 2a).

---

## Erratum / Raffinement contrat streaming (c.477, 2026-07-18)

Lecture approfondie du code streaming existant (`ChatCompletionStreamingResult.cs`,
`TextCompletionStreamingResponse.cs`, `OobaboogaChatHistory.cs`) — la section 1
ci-dessus **sous-spécifie** l'extraction du texte streaming. Correction du contrat :

### La lacune de la section 1

La section 1 disait « sur `TextStreamEvent`, extraire le text chunk du responseObject
et `channel.Write(text)` » **uniformément**. C'est **FAUX pour le chat** : la réponse
streaming chat (`ChatCompletionStreamingResponse`) ne porte pas un chunk texte direct,
mais un `History.Visible` (liste de listes) contenant le message **cumulatif** complet,
pas le delta. L'ancien `ChatCompletionStreamingResult.AppendResponse` (lignes 28-43)
faisait un **suivi de delta stateful** :

```csharp
// chat : extraction stateful (DELTA, pas chunk direct)
var newMessage = response.History.Visible.Last().Last(); // message cumulatif
var newChunk = newMessage.Substring(this._lastSentMessage.Length); // delta seul
this._lastSentMessage = newMessage; // maj état
```

Tandis que le text streaming (`TextCompletionStreamingResponse`) porte directement
`response.Text` (le chunk, pas cumulatif). **Les deux contrats d'extraction diffèrent.**

### Contrat corrigé — `ExtractStreamText` abstraite per-service + stateful

La base **non-générique** ne peut pas appeler une abstraite générique (typing). Donc :

1. **`ProcessWebSocketMessagesAsync` déménage sur la classe GÉNÉRIQUE**
   (`OobaboogaCompletionBase<TInput, TParams, TRequest, TResponse>`), pas sur la base
   non-générique. Elle peut alors appeler les 3 méthodes abstraites du service concret.
2. Nouvelle méthode abstraite sur la classe générique :

   ```csharp
   /// Extrait le chunk de texte (delta pour chat, direct pour text) d'un message
   /// streaming. Retourne null si l'event n'est pas un text-stream (ex: stream_end).
   protected abstract string? ExtractStreamText(CompletionStreamingResponseBase response);
   ```

3. Le chat service implémente `ExtractStreamText` avec un champ privé
   `_lastSentMessage` (état d'instance, reset à 0 par call streaming) — la logique
   de delta est encapsulée dans le service, pas dans la base.
4. Le text service implémente `ExtractStreamText` → `((TextCompletionStreamingResponse)response).Text`.
5. `ProcessWebSocketMessagesAsync` (sur la générique) :

   ```csharp
   switch (responseObject.Event)
   {
       case CompletionStreamingResponseBase.ResponseObjectTextStreamEvent:
           var chunk = this.ExtractStreamText(responseObject);
           if (chunk is not null) await writer.WriteAsync(chunk, cancellationToken);
           break;
       case CompletionStreamingResponseBase.ResponseObjectStreamEndEvent:
           writer.Complete();
           // ... close websocket comme avant
           break;
   }
   ```

6. La base non-générique `OobaboogaCompletionBase` ne garde que `OobaboogaSettings` +
   `Logger` + `LogActionDetails` + l'abstraite `GetResponseObject` (mais
   `GetResponseObject` peut aussi déménager sur la générique puisqu'elle est
   implémentée per-service). **Décision : tout déménager sur la générique** sauf
   settings/logger — la base non-générique devient un simple holder.

### Impact sur l'ordre d'exécution

L'étape 1 (base) est plus lourde qu'estimé : relocation de `ProcessWebSocketMessagesAsync`
+ nouvelle abstraite `ExtractStreamText` + 2 implémentations stateful (chat delta,
text direct). Renforce la conclusion « atomic multi-hour, pas 30-min » — confirmer
fenêtre dédiée avant de toucher au code.

`See #6853` (epic Axe 2, tranche 2a — erratum contrat streaming c.477).
