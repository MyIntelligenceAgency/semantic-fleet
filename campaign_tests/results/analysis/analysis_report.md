# Rapport d'Analyse des Tests du MultiConnector

Date: 2025-05-15 05:25:43

## Résumé

- Nombre total de tests: 132
- Nombre de modèles testés: 5
- Nombre de fonctions testées: 33

## Performances des Modèles

| Modèle | Taux de Réussite | Temps d'Exécution Moyen (ms) | Tokens Moyens | Coût Moyen | Tests |
|--------|-----------------|------------------------------|---------------|------------|-------|
| TheBloke_Mistral-7B-OpenOrca-GGUF | 63.99% | 185.86 | 178.36 | $0.000560 | 132 |
| microsoft_phi-1_5 | 61.66% | 173.09 | 180.39 | $0.000559 | 132 |
| TheBloke_orca_mini_3B-GGML | 62.60% | 176.54 | 173.73 | $0.000552 | 132 |
| TheBloke_LLaMA2-13B-Tiefighter-GGUF | 60.92% | 173.39 | 176.83 | $0.000535 | 132 |
| Primary | 89.64% | 351.17 | 290.80 | $0.002985 | 132 |

## Performances par Niveau de Complexité

### Niveau Trivial

| Modèle | Taux de Réussite | Tests |
|--------|-----------------|-------|
| TheBloke_Mistral-7B-OpenOrca-GGUF | 88.03% | 33 |
| microsoft_phi-1_5 | 84.87% | 33 |
| TheBloke_orca_mini_3B-GGML | 83.82% | 33 |
| TheBloke_LLaMA2-13B-Tiefighter-GGUF | 83.40% | 33 |
| Primary | 89.29% | 33 |

### Niveau Simple

| Modèle | Taux de Réussite | Tests |
|--------|-----------------|-------|
| TheBloke_Mistral-7B-OpenOrca-GGUF | 71.71% | 33 |
| microsoft_phi-1_5 | 70.15% | 33 |
| TheBloke_orca_mini_3B-GGML | 69.36% | 33 |
| TheBloke_LLaMA2-13B-Tiefighter-GGUF | 67.55% | 33 |
| Primary | 91.80% | 33 |

### Niveau Medium

| Modèle | Taux de Réussite | Tests |
|--------|-----------------|-------|
| TheBloke_Mistral-7B-OpenOrca-GGUF | 56.96% | 33 |
| microsoft_phi-1_5 | 52.81% | 33 |
| TheBloke_orca_mini_3B-GGML | 55.08% | 33 |
| TheBloke_LLaMA2-13B-Tiefighter-GGUF | 55.85% | 33 |
| Primary | 89.63% | 33 |

### Niveau Hard

| Modèle | Taux de Réussite | Tests |
|--------|-----------------|-------|
| TheBloke_Mistral-7B-OpenOrca-GGUF | 39.25% | 33 |
| microsoft_phi-1_5 | 38.83% | 33 |
| TheBloke_orca_mini_3B-GGML | 42.14% | 33 |
| TheBloke_LLaMA2-13B-Tiefighter-GGUF | 36.86% | 33 |
| Primary | 87.84% | 33 |

## Seuils de Complexité

| Modèle | Trivial | Simple | Medium | Hard |
|--------|---------|--------|--------|------|
| TheBloke_Mistral-7B-OpenOrca-GGUF | 88.03% | 71.71% | 56.96% | 39.25% |
| microsoft_phi-1_5 | 84.87% | 70.15% | 52.81% | 38.83% |
| TheBloke_orca_mini_3B-GGML | 83.82% | 69.36% | 55.08% | 42.14% |
| TheBloke_LLaMA2-13B-Tiefighter-GGUF | 83.40% | 67.55% | 55.85% | 36.86% |
| Primary | 89.29% | 91.80% | 89.63% | 87.84% |

## Recommandations

### Assignations de Modèles

### Lignes Directrices de Complexité

| Modèle | Niveau | Taux de Réussite | Recommandé |
|--------|--------|-----------------|------------|
| TheBloke_Mistral-7B-OpenOrca-GGUF | Trivial | 88.03% | Oui |
| TheBloke_Mistral-7B-OpenOrca-GGUF | Simple | 71.71% | Oui |
| TheBloke_Mistral-7B-OpenOrca-GGUF | Medium | 56.96% | Non |
| TheBloke_Mistral-7B-OpenOrca-GGUF | Hard | 39.25% | Non |
| microsoft_phi-1_5 | Trivial | 84.87% | Oui |
| microsoft_phi-1_5 | Simple | 70.15% | Oui |
| microsoft_phi-1_5 | Medium | 52.81% | Non |
| microsoft_phi-1_5 | Hard | 38.83% | Non |
| TheBloke_orca_mini_3B-GGML | Trivial | 83.82% | Oui |
| TheBloke_orca_mini_3B-GGML | Simple | 69.36% | Non |
| TheBloke_orca_mini_3B-GGML | Medium | 55.08% | Non |
| TheBloke_orca_mini_3B-GGML | Hard | 42.14% | Non |
| TheBloke_LLaMA2-13B-Tiefighter-GGUF | Trivial | 83.40% | Oui |
| TheBloke_LLaMA2-13B-Tiefighter-GGUF | Simple | 67.55% | Non |
| TheBloke_LLaMA2-13B-Tiefighter-GGUF | Medium | 55.85% | Non |
| TheBloke_LLaMA2-13B-Tiefighter-GGUF | Hard | 36.86% | Non |
| Primary | Trivial | 89.29% | Oui |
| Primary | Simple | 91.80% | Oui |
| Primary | Medium | 89.63% | Oui |
| Primary | Hard | 87.84% | Oui |

### Suggestions d'Optimisation

- Augmenter le nombre d'échantillons pour les fonctions avec des résultats incohérents
- Ajuster les paramètres de température pour les modèles avec des taux de réussite faibles
- Utiliser des expressions régulières pour les préfixes qui se chevauchent
- Optimiser les transformations de prompts pour les modèles secondaires
