# Analyse des résultats et métriques d'évaluation

Ce document décrit les métriques à considérer pour l'analyse des résultats de la campagne de tests avec les modèles réels, ainsi que la méthodologie d'analyse à appliquer.

## 1. Métriques principales

### 1.1 Taux de réussite

Le taux de réussite est la métrique la plus importante pour évaluer la performance d'un modèle. Il représente la proportion de tests réussis par rapport au nombre total de tests effectués.

**Calcul** : Nombre de tests réussis / Nombre total de tests

**Interprétation** :
- Excellent : > 95%
- Bon : 85% - 95%
- Moyen : 70% - 85%
- Faible : < 70%

### 1.2 Temps d'exécution

Le temps d'exécution mesure la rapidité de réponse du modèle. Cette métrique est importante pour les applications nécessitant des réponses en temps réel.

**Calcul** : Temps moyen de réponse en secondes

**Interprétation** :
- Excellent : < 1s
- Bon : 1s - 3s
- Moyen : 3s - 5s
- Lent : > 5s

### 1.3 Utilisation de tokens

Cette métrique mesure le nombre de tokens utilisés par le modèle pour générer une réponse. Elle est directement liée au coût d'utilisation du modèle.

**Calcul** : Nombre moyen de tokens (prompt + complétion) par requête

**Interprétation** : Dépend du contexte et de la complexité de la tâche

### 1.4 Coût

Le coût représente le coût financier d'utilisation du modèle. Il est calculé en fonction du nombre de tokens utilisés et du prix par token du modèle.

**Calcul** : (Tokens prompt × Prix par token prompt + Tokens complétion × Prix par token complétion) / 1000

**Interprétation** : Dépend du budget et des contraintes financières du projet

### 1.5 Efficacité coût/performance

Cette métrique combine le taux de réussite et le coût pour évaluer le rapport qualité/prix du modèle.

**Calcul** : Taux de réussite / Coût moyen par requête

**Interprétation** :
- Plus la valeur est élevée, meilleur est le rapport qualité/prix
- Permet de comparer des modèles ayant des performances et des coûts différents

## 2. Métriques secondaires

### 2.1 Performances par niveau de complexité

Cette métrique évalue la performance du modèle en fonction de la complexité des prompts.

**Calcul** : Taux de réussite par niveau de complexité (Trivial, Simple, Medium, Hard)

**Interprétation** :
- Permet d'identifier les modèles les plus adaptés à chaque niveau de complexité
- Aide à définir une stratégie de routage basée sur la complexité

### 2.2 Performances par type de tâche

Cette métrique évalue la performance du modèle en fonction du type de tâche (raisonnement, code, mathématiques, etc.).

**Calcul** : Taux de réussite par type de tâche

**Interprétation** :
- Permet d'identifier les modèles les plus adaptés à chaque type de tâche
- Aide à définir une stratégie de routage basée sur le type de tâche

### 2.3 Stabilité des réponses

Cette métrique évalue la cohérence et la stabilité des réponses du modèle.

**Calcul** : Écart-type des temps de réponse et des taux de réussite

**Interprétation** :
- Un faible écart-type indique une grande stabilité
- Un écart-type élevé indique une variabilité importante dans les performances

## 3. Méthodologie d'analyse

### 3.1 Analyse globale des performances

1. **Classement des modèles** : Classer les modèles par taux de réussite global
2. **Analyse des temps d'exécution** : Comparer les temps d'exécution moyens
3. **Analyse des coûts** : Comparer les coûts moyens par requête
4. **Analyse de l'efficacité coût/performance** : Classer les modèles par rapport qualité/prix

### 3.2 Analyse par niveau de complexité

1. **Matrice de performance** : Créer une matrice modèles × niveaux de complexité
2. **Identification des forces et faiblesses** : Identifier les modèles les plus performants pour chaque niveau de complexité
3. **Analyse des écarts** : Analyser les écarts de performance entre les niveaux de complexité pour chaque modèle

### 3.3 Analyse par type de tâche

1. **Matrice de performance** : Créer une matrice modèles × types de tâche
2. **Identification des forces et faiblesses** : Identifier les modèles les plus performants pour chaque type de tâche
3. **Analyse des écarts** : Analyser les écarts de performance entre les types de tâche pour chaque modèle

### 3.4 Analyse comparative des providers

1. **Comparaison OpenAI vs OpenRouter** : Comparer les performances globales des modèles OpenAI et OpenRouter
2. **Analyse des spécificités** : Identifier les spécificités de chaque provider en termes de performances, coûts et stabilité

### 3.5 Analyse des modèles Qwen

1. **Comparaison des variantes** : Comparer les performances des différentes variantes de Qwen (1.5B, 8B, 14B, 30B A3B, 32B)
2. **Analyse du rapport taille/performance** : Évaluer l'impact de la taille du modèle sur les performances
3. **Identification du meilleur compromis** : Identifier la variante offrant le meilleur compromis entre performances et coût

## 4. Visualisations recommandées

### 4.1 Graphiques de performance globale

1. **Diagramme en barres des taux de réussite** : Visualiser les taux de réussite de chaque modèle
2. **Diagramme en barres des temps d'exécution** : Visualiser les temps d'exécution moyens de chaque modèle
3. **Diagramme en barres des coûts** : Visualiser les coûts moyens de chaque modèle
4. **Diagramme en barres de l'efficacité coût/performance** : Visualiser le rapport qualité/prix de chaque modèle

### 4.2 Graphiques de performance par niveau de complexité

1. **Graphique linéaire des performances par niveau de complexité** : Visualiser l'évolution des performances en fonction de la complexité
2. **Heatmap des performances par niveau de complexité** : Visualiser les performances de tous les modèles pour tous les niveaux de complexité

### 4.3 Graphiques de performance par type de tâche

1. **Graphique radar des performances par type de tâche** : Visualiser les forces et faiblesses de chaque modèle selon le type de tâche
2. **Heatmap des performances par type de tâche** : Visualiser les performances de tous les modèles pour tous les types de tâche

## 5. Interprétation des résultats

### 5.1 Identification des modèles les plus performants

- **Modèle le plus précis** : Modèle avec le taux de réussite le plus élevé
- **Modèle le plus rapide** : Modèle avec le temps d'exécution le plus faible
- **Modèle le plus économique** : Modèle avec le coût le plus faible
- **Modèle avec le meilleur rapport qualité/prix** : Modèle avec l'efficacité coût/performance la plus élevée

### 5.2 Identification des cas d'utilisation optimaux

- **Tâches complexes** : Modèles les plus performants pour les niveaux de complexité élevés
- **Tâches simples** : Modèles offrant le meilleur rapport qualité/prix pour les niveaux de complexité faibles
- **Tâches spécifiques** : Modèles les plus performants pour chaque type de tâche

### 5.3 Recommandations pour le MultiConnector

- **Stratégie de routage** : Recommandations pour le routage des requêtes vers les modèles les plus adaptés
- **Optimisation des prompts** : Recommandations pour l'optimisation des prompts en fonction des modèles
- **Stratégie de fallback** : Recommandations pour la mise en place d'une stratégie de fallback en cas d'échec