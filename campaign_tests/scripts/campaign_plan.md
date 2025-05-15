# Plan de Campagne de Tests Avancés pour le MultiConnector

## 1. Objectifs de la Campagne

Cette campagne de tests vise à évaluer systématiquement les capacités des différents modèles avec le MultiConnector, en se concentrant sur les fonctions Semantic Kernel et les prompts réguliers. Les objectifs spécifiques sont:

1. Cartographier les fonctions détectées par le système de vetting asynchrone
2. Évaluer les performances des différents modèles pour chaque type de fonction
3. Identifier les seuils de complexité pour chaque modèle
4. Optimiser les paramètres du MultiConnector pour une utilisation efficace des modèles

## 2. Méthodologie de Test

### 2.1 Phases de Test

La campagne de tests sera organisée en quatre phases:

1. **Phase de découverte**:
   - Identifier les préfixes de prompts pour chaque fonction Semantic Kernel
   - Vérifier comment le système détecte ces préfixes
   - Documenter les patterns de détection

2. **Phase de calibration**:
   - Tester chaque fonction avec des entrées de complexité triviale
   - Établir une ligne de base pour les performances des modèles
   - Identifier les fonctions qui fonctionnent avec tous les modèles

3. **Phase d'évaluation progressive**:
   - Augmenter progressivement la complexité des entrées
   - Identifier les seuils de performance pour chaque modèle
   - Documenter les points de défaillance

4. **Phase de validation croisée**:
   - Tester les fonctions avec des entrées similaires mais différentes
   - Vérifier la cohérence des résultats
   - Valider les seuils de performance identifiés

### 2.2 Niveaux de Complexité

Pour chaque fonction, nous testerons quatre niveaux de complexité:

1. **Niveau Trivial**:
   - Textes courts (<50 mots)
   - Structure simple
   - Vocabulaire courant
   - Pas de contexte spécifique requis

2. **Niveau Simple**:
   - Textes moyens (50-200 mots)
   - Structure claire
   - Vocabulaire standard
   - Contexte minimal requis

3. **Niveau Moyen**:
   - Textes longs (200-500 mots)
   - Structure plus complexe
   - Vocabulaire spécialisé
   - Contexte spécifique requis

4. **Niveau Difficile**:
   - Textes très longs (>500 mots)
   - Structure complexe
   - Vocabulaire technique
   - Contexte riche et nuancé requis
   - Raisonnement multi-étapes nécessaire

### 2.3 Métriques d'Évaluation

Nous utiliserons les métriques suivantes pour évaluer les performances des modèles:

1. **Taux de réussite**:
   - Pourcentage de prompts traités avec succès
   - Classé par catégorie de fonction et niveau de complexité

2. **Qualité de réponse**:
   - Pertinence: Adéquation de la réponse à la demande
   - Précision: Exactitude factuelle des informations
   - Cohérence: Logique interne de la réponse

3. **Performance**:
   - Temps de réponse: Latence moyenne par requête
   - Coût: Coût par requête et coût total
   - Efficacité: Rapport qualité/coût

4. **Robustesse**:
   - Variabilité des résultats pour des entrées similaires
   - Capacité à gérer des cas limites
   - Dégradation progressive avec la complexité

## 3. Configuration des Tests

### 3.1 Modèles à Tester

Nous testerons les modèles suivants:

1. **Modèle primaire**:
   - OpenAI GPT (via OpenAIChatCompletion ou OpenAITextCompletion)

2. **Modèles secondaires**:
   - microsoft_phi-1_5
   - TheBloke_orca_mini_3B-GGML
   - TheBloke_Mistral-7B-OpenOrca-GGUF
   - TheBloke_LLaMA2-13B-Tiefighter-GGUF

### 3.2 Fonctions à Tester

Nous testerons les catégories de fonctions suivantes:

1. **SummarizeSkill**:
   - Summarize
   - Topics
   - MakeAbstractReadable
   - Notegen

2. **ChatSkill**:
   - Chat
   - ChatFilter
   - ChatGPT
   - ChatUser
   - ChatV2

3. **WriterSkill**:
   - EmailGen
   - Translate
   - Rewrite
   - EnglishImprover
   - TwoSentenceSummary

4. **ClassificationSkill**:
   - Importance
   - Question

5. **CodingSkill**:
   - Code
   - CodePython
   - CommandLinePython
   - DOSScript

### 3.3 Paramètres du MultiConnector

Nous utiliserons les paramètres suivants pour le MultiConnector:

```csharp
var settings = new MultiTextCompletionSettings()
{
    EnablePromptSampling = true,
    MaxInstanceNb = 2,
    PromptTruncationLength = 11,
    AdjustPromptStarts = true,
    LogCallResult = true,
    LogTestCollection = true,
    ConnectorComparer = MultiTextCompletionSettings.GetWeightedConnectorComparer(1, 1),
    AnalysisSettings = new MultiCompletionAnalysisSettings()
    {
        EnableAnalysis = true,
        NbPromptTests = 2,
        TestsTemperatureTransform = d => Math.Max(d ?? 0, 0.7),
        AnalysisAwaitsManualTrigger = true,
        MaxDegreeOfParallelismTests = 1,
        MaxDegreeOfParallelismConnectorsByTest = 3,
        MaxDegreeOfParallelismEvaluations = 5,
        UpdateSuggestedSettings = true,
        DeleteAnalysisFile = false,
        SaveSuggestedSettings = true
    }
};
```

## 4. Procédure de Test

### 4.1 Préparation

1. Analyser les préfixes des fonctions Semantic Kernel:
   - Exécuter le script `analyze_prefixes.cs`
   - Examiner le rapport généré pour comprendre les patterns de préfixes

2. Générer des jeux de données de test:
   - Exécuter le script `generate_test_data.cs` pour chaque niveau de complexité
   - Vérifier que les données générées correspondent aux critères de complexité

3. Configurer l'environnement de test:
   - Vérifier que tous les modèles sont disponibles
   - Configurer les paramètres du MultiConnector
   - Préparer les systèmes de logging

### 4.2 Exécution des Tests

1. **Phase de découverte**:
   - Exécuter le script `test_prefix_detection.cs`
   - Analyser les résultats pour comprendre la détection des préfixes

2. **Phase de calibration**:
   - Exécuter les tests avec des entrées de niveau trivial
   - Collecter les résultats pour chaque modèle et fonction

3. **Phase d'évaluation progressive**:
   - Exécuter les tests avec des entrées de niveaux simple, moyen et difficile
   - Collecter les résultats pour chaque modèle, fonction et niveau de complexité

4. **Phase de validation croisée**:
   - Exécuter les tests avec des entrées alternatives
   - Comparer les résultats avec ceux des phases précédentes

### 4.3 Analyse des Résultats

1. Calculer les métriques d'évaluation pour chaque modèle, fonction et niveau de complexité
2. Identifier les seuils de complexité pour chaque modèle
3. Analyser les patterns de réussite et d'échec
4. Évaluer l'efficacité du système de vetting

### 4.4 Optimisation

1. Identifier les fonctions les mieux adaptées à chaque modèle
2. Proposer des configurations optimales pour le MultiConnector
3. Tester les configurations proposées
4. Mesurer les gains de performance et de coût

## 5. Livrables

### 5.1 Rapports

1. **Rapport d'analyse des préfixes**:
   - Description du système de détection de préfixes
   - Analyse des patterns de préfixes
   - Recommandations d'amélioration

2. **Rapport de performance des modèles**:
   - Performances globales de chaque modèle
   - Performances par catégorie de fonction
   - Performances par niveau de complexité

3. **Rapport de seuils de complexité**:
   - Seuils identifiés pour chaque modèle
   - Analyse des facteurs limitants
   - Recommandations pour l'utilisation optimale

4. **Rapport d'optimisation**:
   - Configurations optimales proposées
   - Gains de performance et de coût
   - Recommandations pour l'implémentation

### 5.2 Données

1. **Jeux de données de test**:
   - Données générées pour chaque niveau de complexité
   - Documentation des critères de complexité

2. **Résultats bruts**:
   - Logs d'exécution
   - Réponses des modèles
   - Métriques calculées

3. **Configurations optimisées**:
   - Fichiers de configuration pour le MultiConnector
   - Scripts d'implémentation

## 6. Calendrier d'Exécution

1. **Semaine 1**: Préparation et phase de découverte
   - Analyse des préfixes
   - Génération des données de test
   - Configuration de l'environnement

2. **Semaine 2**: Phase de calibration et début de l'évaluation progressive
   - Tests avec entrées triviales
   - Tests avec entrées simples
   - Analyse préliminaire des résultats

3. **Semaine 3**: Suite de l'évaluation progressive et validation croisée
   - Tests avec entrées moyennes et difficiles
   - Tests de validation croisée
   - Analyse complète des résultats

4. **Semaine 4**: Optimisation et finalisation
   - Développement des configurations optimales
   - Tests des configurations proposées
   - Rédaction des rapports finaux

## 7. Ressources Nécessaires

1. **Matériel**:
   - Serveurs pour l'exécution des modèles
   - Stockage pour les données de test et les résultats

2. **Logiciel**:
   - MultiConnector configuré
   - Modèles installés et opérationnels
   - Scripts de test développés

3. **Personnel**:
   - Ingénieur de test pour l'exécution des tests
   - Analyste de données pour l'analyse des résultats
   - Développeur pour l'optimisation des configurations

## 8. Risques et Mitigations

1. **Risque**: Indisponibilité des modèles
   - **Mitigation**: Préparer des alternatives pour chaque modèle

2. **Risque**: Problèmes de performance des serveurs
   - **Mitigation**: Planifier les tests pendant les périodes de faible charge

3. **Risque**: Résultats incohérents
   - **Mitigation**: Augmenter le nombre de tests pour chaque configuration

4. **Risque**: Dépassement de budget pour les API payantes
   - **Mitigation**: Définir des limites strictes et surveiller la consommation

## 9. Conclusion

Cette campagne de tests permettra d'évaluer de manière systématique les capacités des différents modèles avec le MultiConnector, en se concentrant sur les fonctions Semantic Kernel et les prompts réguliers. Les résultats fourniront des informations précieuses pour optimiser le système de vetting et améliorer les performances globales du MultiConnector.