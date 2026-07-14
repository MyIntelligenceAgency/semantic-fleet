import asyncio
import argparse
from multi_connector_vetting_test import MultiConnectorTester

async def main():
    """
    Point d'entrée principal pour l'exécution des tests de vetting automatique
    """
    parser = argparse.ArgumentParser(description="Tests du MultiConnector avec vetting automatique")
    parser.add_argument("--api-key", default="YOUR_OPENAI_API_KEY", 
                        help="Clé API OpenAI")
    parser.add_argument("--primary-model", default="gpt-4o", 
                        help="Modèle principal pour le vetting")
    parser.add_argument("--secondary-models", nargs="+", 
                        default=["gpt-4o-mini", "gpt-3.5-turbo", "o3", "o4-mini", "gpt-4"],
                        help="Modèles secondaires à tester")
    parser.add_argument("--categories", nargs="+", 
                        default=["multi_step_reasoning", "code_comprehension", "advanced_math", "structured_data_analysis", "long_context"],
                        help="Catégories de test à exécuter")
    parser.add_argument("--difficulties", nargs="+", 
                        default=["simple", "medium", "hard"],
                        help="Niveaux de difficulté à tester")
    parser.add_argument("--max-tests", type=int, default=1, 
                        help="Nombre maximum de tests par catégorie")
    parser.add_argument("--output-dir", default="../../../results",
                        help="Répertoire pour sauvegarder les résultats")
    parser.add_argument("--report-only", action="store_true", 
                        help="Générer uniquement le rapport à partir des résultats existants")
    
    args = parser.parse_args()
    
    # Créer le testeur
    tester = MultiConnectorTester(api_key=args.api_key, output_dir=args.output_dir)
    
    if args.report_only:
        # Générer uniquement le rapport
        tester.generate_report()
    else:
        # Exécuter les tests
        print("\n=== Configuration des tests ===")
        print(f"Modèle principal: {args.primary_model}")
        print(f"Modèles secondaires: {', '.join(args.secondary_models)}")
        print(f"Catégories: {', '.join(args.categories)}")
        print(f"Difficultés: {', '.join(args.difficulties)}")
        print(f"Tests max par catégorie: {args.max_tests}")
        print("============================\n")
        
        results = await tester.run_tests(
            primary_model=args.primary_model,
            secondary_models=args.secondary_models,
            categories=args.categories,
            difficulties=args.difficulties,
            max_tests_per_category=args.max_tests
        )
        
        # Générer le rapport
        tester.generate_report()
        
        print("\nTests terminés et rapport généré avec succès!")

if __name__ == "__main__":
    asyncio.run(main())
