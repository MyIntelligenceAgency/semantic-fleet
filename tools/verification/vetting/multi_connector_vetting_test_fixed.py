import os
import json
import asyncio
import time
from datetime import datetime
from typing import Dict, List, Any, Optional, Tuple

# Imports pour le MultiConnector
from python.semantic_kernel.connectors.multi_connector import (
    NamedTextCompletion,
    MultiTextCompletion,
    MultiTextCompletionSettings,
    VettingLevel
)
from python.semantic_kernel.connectors.multi_connector.analysis import MultiCompletionAnalysisSettings
from python.semantic_kernel.connectors.multi_connector.interfaces import TextCompletionClient

# Import pour les tests
from model_tester.test_loader import TestLoader
from model_tester.evaluator import ResponseEvaluator

# Classe pour les clients de complétion de texte OpenAI
class OpenAITextCompletionClient(TextCompletionClient):
    """Client de complétion de texte pour les modèles OpenAI."""
    
    def __init__(self, model_id: str, api_key: str):
        """
        Initialise un nouveau client OpenAI.
        
        Args:
            model_id: ID du modèle OpenAI
            api_key: Clé API OpenAI
        """
        self.model_id = model_id
        self.api_key = api_key
        self.base_url = "https://api.openai.com/v1"
        
        # Déterminer si c'est un modèle de chat
        chat_models = [
            "gpt-4o", "gpt-4o-mini",
            "gpt-3.5-turbo",
            "o3", "o3-2025-04-16", "o3-mini", "o3-mini-2025-01-31",
            "o4-mini", "o4-mini-2025-04-16",
            "gpt-4"
        ]
        self.is_chat_model = any(model_id.lower().startswith(cm.lower()) for cm in chat_models)
        
        # Modèles qui ont des contraintes spécifiques
        self.special_models = [
            "o3", "o3-2025-04-16", "o3-mini", "o3-mini-2025-01-31",
            "o4-mini", "o4-mini-2025-04-16"
        ]
        self.is_special_model = any(model_id.lower().startswith(sm.lower()) for sm in self.special_models)
    
    async def complete(self, text: str, settings: Optional[Dict[str, Any]] = None) -> str:
        """
        Génère une complétion de texte pour le texte d'entrée.
        
        Args:
            text: Le texte d'entrée pour lequel générer une complétion.
            settings: Paramètres optionnels pour la requête.
            
        Returns:
            Le texte de complétion généré.
        """
        import requests
        
        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json"
        }
        
        if self.is_chat_model:
            # Utiliser l'API de chat completion
            if self.is_special_model:
                # Configuration spéciale pour O3 et O4-mini
                data = {
                    "model": self.model_id,
                    "messages": [
                        {"role": "system", "content": "You are a helpful assistant."},
                        {"role": "user", "content": text}
                    ],
                    "max_completion_tokens": settings.get("max_tokens", 2000) if settings else 2000
                }
            else:
                # Configuration standard pour les autres modèles
                data = {
                    "model": self.model_id,
                    "messages": [
                        {"role": "system", "content": "You are a helpful assistant."},
                        {"role": "user", "content": text}
                    ],
                    "temperature": settings.get("temperature", 0.7) if settings else 0.7,
                    "max_tokens": settings.get("max_tokens", 2000) if settings else 2000
                }
            
            response = requests.post(
                f"{self.base_url}/chat/completions",
                headers=headers,
                json=data
            )
            response.raise_for_status()
            result = response.json()
            
            return result.get("choices", [{}])[0].get("message", {}).get("content", "")
        else:
            # Utiliser l'API de completion standard
            data = {
                "model": self.model_id,
                "prompt": text,
                "temperature": settings.get("temperature", 0.7) if settings else 0.7,
                "max_tokens": settings.get("max_tokens", 2000) if settings else 2000
            }
            
            response = requests.post(
                f"{self.base_url}/completions",
                headers=headers,
                json=data
            )
            response.raise_for_status()
            result = response.json()
            
            return result.get("choices", [{}])[0].get("text", "")
    
    async def get_text_contents(self, text: str, execution_settings: Optional[Any] = None) -> List[Any]:
        """
        Génère des contenus textuels pour le texte d'entrée.
        
        Args:
            text: Le texte d'entrée pour lequel générer des contenus.
            execution_settings: Paramètres d'exécution optionnels.
            
        Returns:
            Liste des contenus textuels générés.
        """
        # Convertir les paramètres d'exécution en paramètres de requête
        settings = {}
        if execution_settings:
            settings = {k: v for k, v in vars(execution_settings).items() if not k.startswith('_')}
        
        response_text = await self.complete(text, settings)
        
        # Créer un objet TextContent simple
        class SimpleTextContent:
            def __init__(self, text_value):
                self.text = text_value
        
        return [SimpleTextContent(response_text)]
    
    async def get_streaming_text_contents(self, text: str, execution_settings: Optional[Any] = None):
        """
        Génère des contenus textuels en streaming pour le texte d'entrée.
        
        Args:
            text: Le texte d'entrée pour lequel générer des contenus.
            execution_settings: Paramètres d'exécution optionnels.
            
        Yields:
            Contenus textuels générés en streaming.
        """
        # Pour simplifier, nous n'implémentons pas le streaming réel
        # mais retournons simplement le résultat complet
        class SimpleStreamingTextContent:
            def __init__(self, text_value):
                self.text = text_value
        
        # Convertir les paramètres d'exécution en paramètres de requête
        settings = {}
        if execution_settings:
            settings = {k: v for k, v in vars(execution_settings).items() if not k.startswith('_')}
        
        response_text = await self.complete(text, settings)
        
        # Simuler un streaming en divisant la réponse en morceaux
        chunks = [response_text[i:i+20] for i in range(0, len(response_text), 20)]
        for chunk in chunks:
            yield SimpleStreamingTextContent(chunk)
            await asyncio.sleep(0.1)  # Simuler un délai


class MultiConnectorTester:
    """
    Classe pour tester le MultiConnector avec vetting automatique.
    """
    
    def __init__(self, api_key: str, output_dir: str = "../../../results"):
        """
        Initialise le testeur de MultiConnector.
        
        Args:
            api_key: Clé API OpenAI
            output_dir: Répertoire pour sauvegarder les résultats
        """
        self.api_key = api_key
        self.output_dir = output_dir
        self.test_loader = TestLoader()
        self.evaluator = ResponseEvaluator()
        
        # Créer le répertoire de sortie s'il n'existe pas
        os.makedirs(output_dir, exist_ok=True)
    
    async def setup_multi_connector(self, primary_model: str, secondary_models: List[str]) -> MultiTextCompletion:
        """
        Configure le MultiConnector avec un modèle principal et des modèles secondaires.
        
        Args:
            primary_model: ID du modèle principal pour le vetting
            secondary_models: Liste des IDs des modèles secondaires
            
        Returns:
            Instance configurée de MultiTextCompletion
        """
        # Créer le client pour le modèle principal
        primary_client = OpenAITextCompletionClient(primary_model, self.api_key)
        primary_completion = NamedTextCompletion(primary_model, primary_client)
        
        # Créer les clients pour les modèles secondaires
        secondary_completions = []
        for model_id in secondary_models:
            client = OpenAITextCompletionClient(model_id, self.api_key)
            completion = NamedTextCompletion(model_id, client)
            secondary_completions.append(completion)
        
        # Configurer les paramètres du MultiConnector
        settings = MultiTextCompletionSettings()
        settings.enable_prompt_sampling = True
        settings.max_instance_nb = 5
        settings.log_call_result = True
        
        # Configurer les paramètres d'analyse pour le vetting automatique
        analysis_settings = MultiCompletionAnalysisSettings()
        analysis_settings.enable_vetting = True
        analysis_settings.vetting_threshold = 0.8
        settings.analysis_settings = analysis_settings
        
        # Créer l'instance de MultiTextCompletion
        multi_completion = MultiTextCompletion(
            settings,
            primary_completion,
            other_completions=secondary_completions
        )
        
        return multi_completion
    
    async def run_tests(self, 
                       primary_model: str,
                       secondary_models: List[str],
                       categories: Optional[List[str]] = None,
                       difficulties: Optional[List[str]] = None,
                       max_tests_per_category: int = 1) -> Dict[str, Any]:
        """
        Exécute les tests sur les modèles spécifiés en utilisant le MultiConnector.
        
        Args:
            primary_model: ID du modèle principal pour le vetting
            secondary_models: Liste des IDs des modèles secondaires
            categories: Liste des catégories de test à exécuter (None = toutes)
            difficulties: Liste des niveaux de difficulté à tester (None = tous)
            max_tests_per_category: Nombre maximum de tests par catégorie
            
        Returns:
            Résultats des tests
        """
        # Utiliser toutes les catégories disponibles si non spécifiées
        if categories is None:
            categories = self.test_loader.test_categories
        
        # Utiliser tous les niveaux de difficulté si non spécifiés
        if difficulties is None:
            difficulties = self.test_loader.difficulty_levels
        
        # Configurer le MultiConnector
        multi_completion = await self.setup_multi_connector(primary_model, secondary_models)
        
        # Préparer les résultats
        results = {
            "timestamp": datetime.now().isoformat(),
            "configuration": {
                "primary_model": primary_model,
                "secondary_models": secondary_models,
                "categories": categories,
                "difficulties": difficulties,
                "max_tests_per_category": max_tests_per_category
            },
            "results": {}
        }
        
        # Initialiser les résultats pour chaque modèle
        results["results"]["openai"] = {}
        for model_id in [primary_model] + secondary_models:
            results["results"]["openai"][model_id] = {
                "summary": {
                    "global_score": 0.0,
                    "scores_by_category": {},
                    "scores_by_difficulty": {},
                    "avg_response_time": 0.0,
                    "total_tokens": 0,
                    "cost": 0.0
                },
                "detailed_results": {}
            }
        
        # Exécuter les tests pour chaque catégorie et difficulté
        for category in categories:
            print(f"\nTest de la catégorie: {category}")
            
            for difficulty in difficulties:
                print(f"  Niveau de difficulté: {difficulty}")
                
                # Préparer le prompt
                prompt = self.test_loader.get_test_prompt(category, difficulty)
                if not prompt:
                    print(f"    Prompt non disponible, test ignoré")
                    continue
                
                # Exécuter le test avec le modèle principal
                print(f"    Exécution du test avec le modèle principal: {primary_model}")
                start_time = time.time()
                try:
                    response_text = await multi_completion.complete(prompt, {"temperature": 0.2, "max_tokens": 2000})
                    execution_time = time.time() - start_time
                    
                    # Évaluer la réponse
                    evaluation = self.evaluator.evaluate_response(category, difficulty, prompt, response_text)
                    
                    # Ajouter les métriques d'exécution
                    evaluation["execution_metrics"] = {
                        "response_time": execution_time,
                        "tokens_used": len(response_text.split()) * 2,  # Estimation grossière
                        "start_time": start_time,
                        "end_time": start_time + execution_time
                    }
                    
                    # Ajouter la réponse brute
                    evaluation["raw_response"] = response_text
                    
                    # Sauvegarder le résultat détaillé
                    if category not in results["results"]["openai"][primary_model]["detailed_results"]:
                        results["results"]["openai"][primary_model]["detailed_results"][category] = {}
                    
                    results["results"]["openai"][primary_model]["detailed_results"][category][difficulty] = evaluation
                    
                    print(f"      Score: {evaluation['global_score']:.2f}/5.0, Temps: {execution_time:.2f}s")
                    
                except Exception as e:
                    print(f"      Erreur lors du test: {e}")
                
                # Attendre un peu pour éviter de surcharger l'API
                await asyncio.sleep(1)
        
        # Calculer les scores globaux et les moyennes pour chaque modèle
        for model_id in [primary_model] + secondary_models:
            model_results = results["results"]["openai"][model_id]
            detailed_results = model_results["detailed_results"]
            
            total_score = 0.0
            total_time = 0.0
            total_tokens = 0
            total_tests = 0
            
            # Scores par catégorie et difficulté
            category_scores = {cat: {"total": 0.0, "count": 0} for cat in categories}
            difficulty_scores = {diff: {"total": 0.0, "count": 0} for diff in difficulties}
            
            for category, category_results in detailed_results.items():
                for difficulty, evaluation in category_results.items():
                    if "global_score" in evaluation:
                        global_score = evaluation["global_score"]
                        execution_time = evaluation["execution_metrics"]["response_time"]
                        tokens_used = evaluation["execution_metrics"]["tokens_used"]
                        
                        total_score += global_score
                        total_time += execution_time
                        total_tokens += tokens_used
                        total_tests += 1
                        
                        # Mettre à jour les scores par catégorie
                        category_scores[category]["total"] += global_score
                        category_scores[category]["count"] += 1
                        
                        # Mettre à jour les scores par difficulté
                        difficulty_scores[difficulty]["total"] += global_score
                        difficulty_scores[difficulty]["count"] += 1
            
            # Calculer les moyennes
            if total_tests > 0:
                model_results["summary"]["global_score"] = round(total_score / total_tests, 2)
                model_results["summary"]["avg_response_time"] = round(total_time / total_tests, 2)
                model_results["summary"]["total_tokens"] = total_tokens
                
                # Calculer les scores moyens par catégorie
                for cat, data in category_scores.items():
                    if data["count"] > 0:
                        model_results["summary"]["scores_by_category"][cat] = round(data["total"] / data["count"], 2)
                
                # Calculer les scores moyens par difficulté
                for diff, data in difficulty_scores.items():
                    if data["count"] > 0:
                        model_results["summary"]["scores_by_difficulty"][diff] = round(data["total"] / data["count"], 2)
        
        # Sauvegarder les résultats
        self._save_results(results, "vetting_tests/multi_connector_vetting_results_fixed.json")
        
        return results
    
    def _save_results(self, results: Dict[str, Any], filename: str) -> None:
        """
        Sauvegarde les résultats dans un fichier JSON.
        
        Args:
            results: Résultats à sauvegarder
            filename: Nom du fichier
        """
        filepath = os.path.join(self.output_dir, filename)
        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(results, f, indent=2, ensure_ascii=False)
        print(f"Résultats sauvegardés dans {filepath}")
    
    def generate_report(self, results_file: str = "vetting_tests/multi_connector_vetting_results_fixed.json") -> str:
        """
        Génère un rapport détaillé à partir des résultats.
        
        Args:
            results_file: Fichier de résultats à utiliser
            
        Returns:
            Rapport au format Markdown
        """
        # Charger les résultats
        filepath = os.path.join(self.output_dir, results_file)
        with open(filepath, "r", encoding="utf-8") as f:
            results = json.load(f)
        
        # Générer le rapport
        report = "# Rapport d'évaluation des modèles avec MultiConnector et vetting automatique\n\n"
        report += f"Date: {datetime.fromisoformat(results['timestamp']).strftime('%d/%m/%Y %H:%M:%S')}\n\n"
        
        # Tableau comparatif des performances globales
        report += "## Performances globales\n\n"
        report += "| Modèle | Score global | Temps moyen (s) | Tokens totaux |\n"
        report += "|--------|-------------|----------------|---------------|\n"
        
        for model_id, model_results in results["results"]["openai"].items():
            summary = model_results["summary"]
            report += f"| {model_id} | {summary['global_score']:.2f}/5.0 | {summary['avg_response_time']:.2f} | {summary['total_tokens']} |\n"
        
        # Performances par catégorie
        report += "\n## Performances par catégorie\n\n"
        report += "| Modèle | " + " | ".join(results["configuration"]["categories"]) + " |\n"
        report += "|--------|-" + "-|-".join(["----" for _ in results["configuration"]["categories"]]) + " |\n"
        
        for model_id, model_results in results["results"]["openai"].items():
            scores_by_category = model_results["summary"]["scores_by_category"]
            scores = []
            for category in results["configuration"]["categories"]:
                score = scores_by_category.get(category, "N/A")
                if isinstance(score, (int, float)):
                    scores.append(f"{score:.2f}")
                else:
                    scores.append(score)
            report += f"| {model_id} | " + " | ".join(scores) + " |\n"
        
        # Performances par niveau de difficulté
        report += "\n## Performances par niveau de difficulté\n\n"
        report += "| Modèle | " + " | ".join(results["configuration"]["difficulties"]) + " |\n"
        report += "|--------|-" + "-|-".join(["----" for _ in results["configuration"]["difficulties"]]) + " |\n"
        
        for model_id, model_results in results["results"]["openai"].items():
            scores_by_difficulty = model_results["summary"]["scores_by_difficulty"]
            scores = []
            for difficulty in results["configuration"]["difficulties"]:
                score = scores_by_difficulty.get(difficulty, "N/A")
                if isinstance(score, (int, float)):
                    scores.append(f"{score:.2f}")
                else:
                    scores.append(score)
            report += f"| {model_id} | " + " | ".join(scores) + " |\n"
        
        # Analyse des forces et faiblesses
        report += "\n## Analyse des forces et faiblesses\n\n"
        
        for model_id, model_results in results["results"]["openai"].items():
            report += f"### Modèle: {model_id}\n\n"
            
            # Identifier les forces (catégories avec les meilleurs scores)
            scores_by_category = model_results["summary"]["scores_by_category"]
            if scores_by_category:
                sorted_categories = sorted(scores_by_category.items(), key=lambda x: x[1], reverse=True)
                
                report += "**Forces:**\n\n"
                for category, score in sorted_categories[:2]:  # Top 2 des forces
                    report += f"- {category}: {score:.2f}/5.0\n"
                
                report += "\n**Faiblesses:**\n\n"
                for category, score in sorted_categories[-2:]:  # Top 2 des faiblesses
                    report += f"- {category}: {score:.2f}/5.0\n"
            
            report += "\n"
        
        # Recommandations pour le routage optimal
        report += "\n## Recommandations pour le routage optimal\n\n"
        
        # Trouver le meilleur modèle pour chaque catégorie
        best_models_by_category = {}
        for category in results["configuration"]["categories"]:
            best_score = 0
            best_model = None
            
            for model_id, model_results in results["results"]["openai"].items():
                score = model_results["summary"]["scores_by_category"].get(category, 0)
                if score > best_score:
                    best_score = score
                    best_model = model_id
            
            if best_model:
                best_models_by_category[category] = (best_model, best_score)
        
        for category, (model, score) in best_models_by_category.items():
            report += f"- Pour les tâches de **{category}**, utiliser **{model}** - Score: {score:.2f}/5.0\n"
        
        # Ajouter des notes sur les spécificités des modèles O3 et O4-mini
        report += "\n## Notes sur les spécificités des modèles\n\n"
        report += "### Modèles O3 et O4-mini\n\n"
        report += "Ces modèles ont des contraintes spécifiques dans l'API OpenAI :\n\n"
        report += "1. Ils n'acceptent pas le paramètre `temperature` (ou uniquement la valeur par défaut 1)\n"
        report += "2. Ils utilisent `max_completion_tokens` au lieu de `max_tokens`\n\n"
        report += "Pour utiliser ces modèles dans le MultiConnector, il est nécessaire d'adapter les paramètres de requête en fonction de ces contraintes.\n"
        
        # Sauvegarder le rapport
        report_filepath = os.path.join(self.output_dir, "reports/vetting_reports/rapport_vetting_fixed.md")
        with open(report_filepath, "w", encoding="utf-8") as f:
            f.write(report)
        
        print(f"Rapport généré et sauvegardé dans {report_filepath}")
        return report


async def main():
    """
    Point d'entrée principal
    """
    # Clé API OpenAI
    api_key = "***REMOVED***"
    
    # Modèles à tester
    primary_model = "gpt-4o"  # Modèle principal pour le vetting
    secondary_models = ["gpt-4o-mini", "gpt-3.5-turbo", "o3", "o4-mini", "gpt-4"]
    
    # Catégories et difficultés à tester
    categories = ["multi_step_reasoning", "code_comprehension", "advanced_math"]
    difficulties = ["medium", "hard"]
    
    # Créer et exécuter le testeur
    tester = MultiConnectorTester(api_key)
    results = await tester.run_tests(
        primary_model=primary_model,
        secondary_models=secondary_models,
        categories=categories,
        difficulties=difficulties,
        max_tests_per_category=1
    )
    
    # Générer le rapport
    tester.generate_report()


if __name__ == "__main__":
    asyncio.run(main())
