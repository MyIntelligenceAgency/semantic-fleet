# Carnets .NET — Semantic Fleet (connecteurs Oobabooga et MultiConnector)

[Version anglaise](README.en.md)

## Ouverture — ce que cette série enseigne (Découverte)

Brancher un modèle local dans une application .NET n'est pas un appel HTTP : c'est un **contrat de configuration**, un **connecteur**, et — quand on veut faire vite et payer moins — une **flotte** de modèles qui se répartit le travail. Cette série enseigne cette chaîne de bout en bout, en .NET Interactive : declare un backend OpenAI ou Oobabooga, interroge un modèle local en complétion de texte puis en conversation, puis délègue une fonction sémantique d'un grand modèle vers un modèle plus petit en pilotant le routage sur le coût et la durée.

**À qui elle s'adresse.** Un lecteur .NET qui connaît le kernel et veut brancher un LLM local sur un connecteur Semantic Kernel, puis comprendre comment le MultiConnector répartit une requête entre plusieurs connecteurs. Les prérequis exacts sont en [Installation](#installation).

## Comment lire ce README (Découverte)

Un README de série se lit à plusieurs vitesses : le parcours principal par les numéros nus, l'approfondissement par les lettres, la recherche en cours par les sous-séries et la section « Pour aller plus loin ».

- **Vous branchez un modèle local pour la première fois** : commencez par le carnet de configuration, puis le carnet de complétion de texte.
- **Vous voulez la conversation plutôt que la complétion** : allez directement au carnet sur les capacités de chat.
- **Vous vous interessez au routage multi-connecteurs** : les deux derniers carnets forment la progression complète — d'abord les fondations, puis l'optimisation.

## Parcours principal (Découverte)

| # | Notebook | Ce qu'on y apprend | Public | Pour approfondir |
|---|---|---|---|---|
| 0 | [AI Backend Settings](00-AI-settings.ipynb) | Le contrat de configuration d'un kernel : déclarer un backend OpenAI et un backend Oobabooga, distinguer le connecteur principal du connecteur secondaire du MultiConnector, nommer chaque connecteur secondaire, et enregistrer l'ensemble dans un fichier de configuration local versionnable. Le carnet pose les questions et lit les réponses au fil de l'exécution : il n'y a pas de valeur en clair à recopier. | Découverte | [README de Semantic Fleet](https://github.com/MyIntelligenceAgency/semantic-fleet) |
| 1 | [Text Completion with Oobabooga](01-oobabooga-text-completion.ipynb) | La complétion de texte contre un modèle local : où sont le port bloquant et le port de streaming d'une instance Oobabooga, comment les déclarés dans les réglages de complétion, puis deux appels — une complétion simple, et la même en streaming pour voir la réponse arriver par morceaux. | Découverte | [README de Semantic Fleet](https://github.com/MyIntelligenceAgency/semantic-fleet) |
| 2 | [Advanced Chat Capabilities with Oobabooga](02-oobabooga-chat-capabilities.ipynb) | La conversation contre un modèle local : ce qui distingue les réglages de chat des réglages de complétion (préréglages de génération, préréglages de personnage), un échange simple avec le personnage par défaut, puis le streaming de la réponse et ses peculiarities d'affichage. | Découverte | [README de Semantic Fleet](https://github.com/MyIntelligenceAgency/semantic-fleet) |
| 3 | [Understanding Multi-Connector Text Completion with Arithmetic Mocks](03-multiConnector-intro-with-arithmetic-mocks.ipynb) | Le MultiConnector sur un terrain contrôlé : construire des connecteurs « arithmétiques » dont chacun ne sait qu'une opération, les reúne en un connecteur principal lent et capable de tout face à des connecteurs secondaires spécialisés plus rapides et moins coûteux, fixer les poids de gain de coût et de durée, puis observer le routage décider vers qui part une opération. Aucune clé de modèle n'est nécessaire : les connecteurs sont des doublures. | Découverte | [README de Semantic Fleet](https://github.com/MyIntelligenceAgency/semantic-fleet) |
| 4 | [Multi-Connector Optimization with Explicit Summarization Prompts](04-multiConnector-optimization-with-summarization-prompt.ipynb) | Le MultiConnector au service d'un coût réel : déporter une fonction sémantique (un prompt de summarisation) d'un grand modèle vers un plus petit, en activant le routage sur des métriques de performance — vitesse et coût — et en mesurant ce que l'optimisation fait gagner sur le chemin critique. | Recherche | [Guide MultiConnector](https://github.com/MyIntelligenceAgency/semantic-fleet/blob/main/dotnet/src/IntegrationTests/Connectors/MultiConnector/README.md) |

## Approfondissements (Découverte)

Cette série ne porte aucun palier lettré : la gradation est portée par la numérotation des carnets et par la progression backend → complétion → conversation → routage. Le détail des réglages de configuration se lit directement dans [le carnet de configuration](00-AI-settings.ipynb), et le contrat de configuration dans [son fichier d'exemple](config/settings.example.json).

## Installation

- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) — le kernel des carnets est .NET Interactive.
- [Visual Studio Code](https://code.visualstudio.com) et l'extension [Polyglot](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode) pour exécuter les carnets.
- Une instance Oobabooga (ou tout endpoint compatible OpenAI) avec les ports bloquant et de streaming disponibles : la procédure de lancement est dans [la documentation Oobabooga](../../docs/OOBABOOGA.md).
- Les paquets NuGet `Microsoft.SemanticKernel` et `MyIA.SemanticKernel.Connectors.AI.MultiConnector` sont importés par les carnets eux-mêmes.

La configuration se fait par le carnet `00-AI-settings.ipynb`, qui enregistre les réglages dans `config/settings.json`. Ce fichier est **ignoré par Git** : il contient des identifiants. Pour partir d'une copie, dupliquez `config/settings.example.json` et modifiez-la à la main.

## Pour aller plus loin — niveau Recherche

- **Le guide du MultiConnector** : le contrat complet du connecteur et de son optimisation est dans [le guide du dépôt](https://github.com/MyIntelligenceAgency/semantic-fleet/blob/main/dotnet/src/IntegrationTests/Connectors/MultiConnector/README.md).
- **Le transport Oobabooga retiré** : le pont websocket dédié a été supprimé (Oobabooga l'a remplacé par une API HTTP compatible OpenAI) ; les notes historiques restent dans [docs/OOBABOOGA.md](../../docs/OOBABOOGA.md).
- **La version anglaise** de ce README est conservée telle quelle dans [README.en.md](README.en.md).

## Ressources

- [Semantic Fleet — README du dépôt](https://github.com/MyIntelligenceAgency/semantic-fleet) : la vision d'ensemble, l'installation des paquets et les autres axes du projet.
- [Documentation Oobabooga](../../docs/OOBABOOGA.md) : lancer les modèles locaux et exposer leurs endpoints.
