# Analyse des Performances des Modèles Réels

Date: 2025-05-15 13:53:59

## Résumé

- Nombre total de tests: 4
- Nombre de modèles testés: 6
- Nombre de fonctions testées: 2

## Performances Globales des Modèles

| Modèle | Taux de Réussite | Temps d'Exécution Moyen (ms) | Tokens Moyens | Coût Moyen | Efficacité Coût/Performance | Tests |
|--------|-----------------|------------------------------|---------------|------------|------------------------------|-------|
| claude-3.7-sonnet | 100.00% | 1800.00 | 280.00 | $0.002800 | 357.14 | 1 |
| gpt-4o | 100.00% | 2733.33 | 300.00 | $0.009333 | 107.14 | 3 |
| qwen/qwen3-32b | 100.00% | 2800.00 | 320.00 | $0.006400 | 156.25 | 1 |
| anthropic/claude-3.7-sonnet | 100.00% | 2350.00 | 310.00 | $0.009920 | 100.81 | 2 |
| google/gemini-pro-1.5 | 100.00% | 1800.00 | 280.00 | $0.000980 | 1020.41 | 1 |
| gpt-3.5-turbo | 50.00% | 1350.00 | 265.00 | $0.000530 | 943.40 | 2 |

## Performances par Provider

| Provider | Taux de Réussite | Temps d'Exécution Moyen (ms) | Tokens Moyens | Coût Moyen | Tests |
|----------|-----------------|------------------------------|---------------|------------|-------|
| openrouter | 100.00% | 2225.00 | 295.00 | $0.004945 | 4 |
| openai | 75.00% | 1925.00 | 270.00 | $0.004140 | 4 |

## Efficacité Coût/Performance

| Modèle | Efficacité | Catégorie |
|--------|------------|------------|
| google/gemini-pro-1.5 | 1020.41 | Excellent rapport qualité/prix |
| gpt-3.5-turbo | 943.40 | Bon rapport qualité/prix |
| claude-3.7-sonnet | 357.14 | Bon rapport qualité/prix |
| qwen/qwen3-32b | 156.25 | Rapport qualité/prix moyen |
| gpt-4o | 107.14 | Rapport qualité/prix moyen |
| anthropic/claude-3.7-sonnet | 100.81 | Rapport qualité/prix faible |

## Performances par Niveau de Complexité

### Niveau Simple

| Modèle | Taux de Réussite | Tests |
|--------|-----------------|-------|
| gpt-4o | 100.00% | 1 |
| google/gemini-pro-1.5 | 100.00% | 1 |
| gpt-3.5-turbo | 100.00% | 1 |
| anthropic/claude-3.7-sonnet | 100.00% | 1 |

### Niveau Medium

| Modèle | Taux de Réussite | Tests |
|--------|-----------------|-------|
| qwen/qwen3-32b | 100.00% | 1 |
| anthropic/claude-3.7-sonnet | 100.00% | 1 |
| gpt-4o | 100.00% | 1 |
| gpt-3.5-turbo | 0.00% | 1 |

## Recommandations

### Assignations de Modèles Recommandées

#### qwen/qwen3-32b

- CodingSkill.CodePython

#### gpt-4o

- SummarizeSkill.Summarize

### Suggestions d'Optimisation

- Utiliser GPT-4o pour les tâches complexes nécessitant un raisonnement avancé
- Utiliser Claude 3 Sonnet pour les tâches de génération de texte et de résumé
- Utiliser GPT-4o-mini ou Gemini Pro pour un bon équilibre performance/coût
- Utiliser GPT-3.5-turbo pour les tâches simples à moyen coût
- Ajuster les paramètres du MultiConnector en fonction des performances des modèles
- Optimiser les transformations de prompts pour chaque modèle
