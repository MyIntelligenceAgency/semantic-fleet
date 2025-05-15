// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector
{
    /// <summary>
    /// Transformateur de prompts spécifique à chaque modèle.
    /// </summary>
    public class ModelSpecificPromptTransformer
    {
        private readonly Dictionary<string, PromptTransform> _modelTransforms;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="ModelSpecificPromptTransformer"/>.
        /// </summary>
        public ModelSpecificPromptTransformer()
        {
            _modelTransforms = InitializeModelTransforms();
        }

        /// <summary>
        /// Transforme un prompt en fonction du modèle spécifié.
        /// </summary>
        /// <param name="originalPrompt">Prompt original</param>
        /// <param name="modelId">Identifiant du modèle</param>
        /// <param name="context">Contexte pour la transformation</param>
        /// <returns>Prompt transformé</returns>
        public string TransformPrompt(string originalPrompt, string modelId, Dictionary<string, object>? context = null)
        {
            if (string.IsNullOrEmpty(originalPrompt))
            {
                return originalPrompt;
            }

            // Normaliser l'ID du modèle
            string normalizedModelId = NormalizeModelId(modelId);

            // Vérifier si une transformation spécifique existe pour ce modèle
            if (_modelTransforms.TryGetValue(normalizedModelId, out var transform))
            {
                return transform.DefaultTransform(originalPrompt, context);
            }

            // Si aucune transformation spécifique n'est trouvée, retourner le prompt original
            return originalPrompt;
        }

        /// <summary>
        /// Normalise l'ID du modèle pour la recherche de transformation.
        /// </summary>
        /// <param name="modelId">Identifiant du modèle</param>
        /// <returns>Identifiant normalisé</returns>
        private string NormalizeModelId(string modelId)
        {
            if (modelId.StartsWith("gpt-"))
            {
                return "gpt";
            }
            else if (modelId.Contains("claude"))
            {
                return "claude";
            }
            else if (modelId.Contains("gemini"))
            {
                return "gemini";
            }
            else if (modelId.Contains("qwen"))
            {
                return "qwen";
            }
            else
            {
                return modelId;
            }
        }

        /// <summary>
        /// Initialise les transformations spécifiques à chaque modèle.
        /// </summary>
        /// <returns>Dictionnaire des transformations par modèle</returns>
        private Dictionary<string, PromptTransform> InitializeModelTransforms()
        {
            return new Dictionary<string, PromptTransform>
            {
                // Transformation pour les modèles GPT (OpenAI)
                {
                    "gpt", new PromptTransform
                    {
                        Template = @"
Je vais vous donner une tâche à accomplir. Veuillez suivre ces instructions précisément.

Contexte: {context}

Objectif: {objective}

Instructions détaillées:
{0}

Format de sortie attendu:
{output_format}
",
                        InterpolationType = PromptInterpolationType.InterpolateKeys
                    }
                },

                // Transformation pour les modèles Claude (Anthropic)
                {
                    "claude", new PromptTransform
                    {
                        Template = @"
<instructions>
{0}
</instructions>

<format>
{output_format}
</format>

<examples>
{examples}
</examples>
",
                        InterpolationType = PromptInterpolationType.InterpolateKeys
                    }
                },

                // Transformation pour les modèles Gemini (Google)
                {
                    "gemini", new PromptTransform
                    {
                        Template = @"
{0}

Assurez-vous de fournir une réponse concise et directe.
",
                        InterpolationType = PromptInterpolationType.InterpolateKeys
                    }
                },

                // Transformation pour les modèles Qwen (Alibaba)
                {
                    "qwen", new PromptTransform
                    {
                        Template = @"
Voici la tâche à accomplir:
{0}

Voici quelques exemples pour vous guider:
{examples}

Veuillez suivre un raisonnement étape par étape pour résoudre cette tâche.
",
                        InterpolationType = PromptInterpolationType.InterpolateKeys
                    }
                }
            };
        }

        /// <summary>
        /// Obtient les exemples few-shot pour un modèle spécifique.
        /// </summary>
        /// <param name="modelId">Identifiant du modèle</param>
        /// <param name="taskType">Type de tâche</param>
        /// <returns>Exemples few-shot</returns>
        public string GetFewShotExamples(string modelId, string taskType)
        {
            string normalizedModelId = NormalizeModelId(modelId);

            // Exemples few-shot pour les tâches de code
            if (taskType == "code")
            {
                switch (normalizedModelId)
                {
                    case "gpt":
                        return @"
Exemple 1:
Entrée: Écrivez une fonction Python qui calcule la somme des nombres pairs dans une liste.
Sortie:
```python
def sum_even_numbers(numbers):
    # Calcule la somme des nombres pairs dans une liste
    return sum(num for num in numbers if num % 2 == 0)
```

Exemple 2:
Entrée: Écrivez une fonction JavaScript qui inverse une chaîne de caractères.
Sortie:
```javascript
/**
 * Inverse une chaîne de caractères
 * @param {string} str - La chaîne à inverser
 * @return {string} La chaîne inversée
 */
function reverseString(str) {
    return str.split('').reverse().join('');
}
```";

                    case "claude":
                        return @"
Exemple 1:
Entrée: Écrivez une fonction Python qui calcule la somme des nombres pairs dans une liste.
Sortie:
```python
def sum_even_numbers(numbers):
    # Calcule la somme des nombres pairs dans une liste
    result = 0
    for num in numbers:
        if num % 2 == 0:
            result += num
    return result
```

Exemple 2:
Entrée: Écrivez une fonction JavaScript qui inverse une chaîne de caractères.
Sortie:
```javascript
/**
 * Inverse une chaîne de caractères
 * @param {string} str - La chaîne à inverser
 * @return {string} La chaîne inversée
 */
function reverseString(str) {
    let reversed = '';
    for (let i = str.length - 1; i >= 0; i--) {
        reversed += str[i];
    }
    return reversed;
}
```

Exemple 3:
Entrée: Écrivez une classe C# qui représente un compte bancaire.
Sortie:
```csharp
/// <summary>
/// Représente un compte bancaire simple
/// </summary>
public class BankAccount
{
    /// <summary>
    /// Obtient ou définit le solde du compte
    /// </summary>
    public decimal Balance { get; private set; }

    /// <summary>
    /// Obtient le numéro du compte
    /// </summary>
    public string AccountNumber { get; }

    /// <summary>
    /// Initialise une nouvelle instance de la classe BankAccount
    /// </summary>
    /// <param name=""accountNumber"">Numéro du compte</param>
    /// <param name=""initialBalance"">Solde initial</param>
    public BankAccount(string accountNumber, decimal initialBalance = 0)
    {
        AccountNumber = accountNumber;
        Balance = initialBalance;
    }

    /// <summary>
    /// Dépose un montant sur le compte
    /// </summary>
    /// <param name=""amount"">Montant à déposer</param>
    /// <exception cref=""ArgumentException"">Levée si le montant est négatif</exception>
    public void Deposit(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentException(""Le montant du dépôt doit être positif"", nameof(amount));
        }

        Balance += amount;
    }

    /// <summary>
    /// Retire un montant du compte
    /// </summary>
    /// <param name=""amount"">Montant à retirer</param>
    /// <exception cref=""ArgumentException"">Levée si le montant est négatif</exception>
    /// <exception cref=""InvalidOperationException"">Levée si le solde est insuffisant</exception>
    public void Withdraw(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentException(""Le montant du retrait doit être positif"", nameof(amount));
        }

        if (amount > Balance)
        {
            throw new InvalidOperationException(""Solde insuffisant"");
        }

        Balance -= amount;
    }
}
```";

                    case "gemini":
                        return @"
Exemple:
Entrée: Écrivez une fonction Python qui calcule la somme des nombres pairs dans une liste.
Sortie:
```python
def sum_even_numbers(numbers):
    return sum(num for num in numbers if num % 2 == 0)
```";

                    case "qwen":
                        return @"
Exemple 1:
Entrée: Écrivez une fonction Python qui calcule la somme des nombres pairs dans une liste.
Analyse:
1. Nous devons filtrer les nombres pairs dans la liste
2. Nous devons calculer la somme de ces nombres
3. Nous pouvons utiliser une compréhension de liste pour filtrer les nombres pairs
4. Nous pouvons utiliser la fonction sum() pour calculer la somme

Sortie:
```python
def sum_even_numbers(numbers):
    return sum(num for num in numbers if num % 2 == 0)
```

Exemple 2:
Entrée: Écrivez une fonction JavaScript qui inverse une chaîne de caractères.
Analyse:
1. Nous devons transformer la chaîne en un tableau de caractères
2. Nous devons inverser ce tableau
3. Nous devons rejoindre les caractères pour former une nouvelle chaîne

Sortie:
```javascript
function reverseString(str) {
    return str.split('').reverse().join('');
}
```";

                    default:
                        return "";
                }
            }
            // Exemples few-shot pour les tâches de résumé
            else if (taskType == "summarization")
            {
                // Implémentation similaire pour les tâches de résumé
                return "";
            }
            // Exemples few-shot pour les tâches de raisonnement
            else if (taskType == "reasoning")
            {
                // Implémentation similaire pour les tâches de raisonnement
                return "";
            }
            // Exemples few-shot pour les tâches d'écriture
            else if (taskType == "writing")
            {
                // Implémentation similaire pour les tâches d'écriture
                return "";
            }
            // Exemples few-shot pour les tâches de classification
            else if (taskType == "classification")
            {
                // Implémentation similaire pour les tâches de classification
                return "";
            }
            else
            {
                return "";
            }
        }
    }
}
