# Modifications à apporter au script de campagne de tests

## Mise à jour de la liste des modèles à tester

Le script `run_real_models_campaign.ps1` doit être modifié pour inclure les modèles spécifiés par l'utilisateur. Voici les modifications précises à apporter :

### Modification de la liste `$modelsToTest`

Remplacer la liste actuelle des modèles (lignes 51-63) par la liste suivante :

```powershell
$modelsToTest = @(
    # Modèles OpenAI
    "gpt-4o",
    "gpt-4o-mini",
    "gpt-3.5-turbo",
    
    # Modèles O3 et O4-mini (à tester)
    "o3",
    "o4-mini",
    
    # Modèles via OpenRouter
    "anthropic/claude-3-sonnet-20240229",  # Claude 3.7 Sonnet
    "google/gemini-pro-1.5",               # Gemini 2.5 Pro
    
    # Modèles Qwen via OpenRouter
    "qwen/qwen-1.5b",                      # Qwen 3 1.5B
    "qwen/qwen-8b",                        # Qwen 3 8B
    "qwen/qwen-14b",                       # Qwen 3 14B
    "qwen/qwen-30b-a3b",                   # Qwen 3 30B A3B
    "qwen/qwen-32b"                        # Qwen 3 32B
)
```

## Mise à jour du fichier `.env`

Le fichier `.env` doit être mis à jour avec la clé API OpenRouter fournie par l'utilisateur :

```
# OpenRouter API
OPENROUTER_API_KEY=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
OPENROUTER_BASE_URL=https://openrouter.ai/api/v1
```

## Modification du script `transparent_model_test.py`

Si nécessaire, mettre à jour les identifiants des modèles Qwen dans le script `transparent_model_test.py` pour correspondre aux modèles spécifiés par l'utilisateur.

## Diff pour la modification de `run_real_models_campaign.ps1`

```diff
@@ -51,14 +51,21 @@
 $modelsToTest = @(
     # Modèles OpenAI
     "gpt-4o",
     "gpt-4o-mini",
     "gpt-3.5-turbo",
-    "gpt-4",
+    
+    # Modèles O3 et O4-mini (à tester)
+    "o3",
+    "o4-mini",
     
     # Modèles via OpenRouter
-    "anthropic/claude-3-sonnet-20240229",
-    "google/gemini-pro-1.5",
-    "qwen/qwen-72b",
-    "qwen/qwen-chat"
+    "anthropic/claude-3-sonnet-20240229",  # Claude 3.7 Sonnet
+    "google/gemini-pro-1.5",               # Gemini 2.5 Pro
+    
+    # Modèles Qwen via OpenRouter
+    "qwen/qwen-1.5b",                      # Qwen 3 1.5B
+    "qwen/qwen-8b",                        # Qwen 3 8B
+    "qwen/qwen-14b",                       # Qwen 3 14B
+    "qwen/qwen-30b-a3b",                   # Qwen 3 30B A3B
+    "qwen/qwen-32b"                        # Qwen 3 32B
 )
 ```

## Notes importantes

1. **Vérification des modèles** : Avant d'exécuter la campagne complète, il est recommandé de vérifier que tous les modèles spécifiés sont bien disponibles via l'API OpenRouter. Certains modèles Qwen spécifiques pourraient avoir des identifiants légèrement différents.

2. **Gestion des erreurs** : Si certains modèles ne sont pas disponibles, le script devrait continuer l'exécution avec les modèles disponibles et noter les erreurs dans le rapport final.

3. **Encodage** : Pour éviter les problèmes d'encodage dans les rapports générés, s'assurer que tous les fichiers sont écrits avec l'encodage UTF-8.