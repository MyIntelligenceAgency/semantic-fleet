using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace CampaignTests
{
    /// <summary>
    /// Script pour tester le système de détection de préfixes du MultiConnector
    /// </summary>
    public class PrefixDetectionTester
    {
        private readonly string _skillsDirectory;
        private readonly string _outputDirectory;
        private readonly int _truncationLength;

        public PrefixDetectionTester(string skillsDirectory, string outputDirectory, int truncationLength = 11)
        {
            this._skillsDirectory = skillsDirectory;
            this._outputDirectory = outputDirectory;
            this._truncationLength = truncationLength;

            Directory.CreateDirectory(outputDirectory);
        }

        /// <summary>
        /// Exécute les tests de détection de préfixes
        /// </summary>
        public async Task RunTestsAsync()
        {
            Console.WriteLine("Démarrage des tests de détection de préfixes...");

            // Collecter les informations sur les skills et fonctions
            var skillFunctions = await this.CollectSkillFunctionsAsync();

            // Générer des signatures de prompts
            var signatures = this.GeneratePromptSignatures(skillFunctions);

            // Tester la détection de préfixes
            var results = this.TestPrefixDetection(signatures, skillFunctions);

            // Générer un rapport
            await this.GenerateReportAsync(results);

            Console.WriteLine("Tests terminés. Rapport généré dans le répertoire de sortie.");
        }

        /// <summary>
        /// Collecte les informations sur les skills et fonctions
        /// </summary>
        private async Task<List<SkillFunctionInfo>> CollectSkillFunctionsAsync()
        {
            var skillFunctions = new List<SkillFunctionInfo>();
            var skillDirectories = Directory.GetDirectories(this._skillsDirectory);

            foreach (var skillDir in skillDirectories)
            {
                var skillName = Path.GetFileName(skillDir);
                var functionDirectories = Directory.GetDirectories(skillDir);

                foreach (var functionDir in functionDirectories)
                {
                    var functionName = Path.GetFileName(functionDir);
                    var promptPath = Path.Combine(functionDir, "skprompt.txt");
                    var configPath = Path.Combine(functionDir, "config.json");

                    if (!File.Exists(promptPath) || !File.Exists(configPath))
                    {
                        continue;
                    }

                    var promptText = await File.ReadAllTextAsync(promptPath);
                    var configText = await File.ReadAllTextAsync(configPath);

                    var functionInfo = new SkillFunctionInfo
                    {
                        SkillName = skillName,
                        FunctionName = functionName,
                        PromptText = promptText,
                        ConfigJson = configText,
                        PromptStart = promptText.Length > this._truncationLength
                            ? promptText.Substring(0, this._truncationLength)
                            : promptText
                    };

                    skillFunctions.Add(functionInfo);
                    Console.WriteLine($"Collecté: {skillName}.{functionName}");
                }
            }

            return skillFunctions;
        }

        /// <summary>
        /// Génère des signatures de prompts pour les fonctions
        /// </summary>
        private List<PromptSignature> GeneratePromptSignatures(List<SkillFunctionInfo> skillFunctions)
        {
            var signatures = new List<PromptSignature>();

            foreach (var function in skillFunctions)
            {
                var settings = new AIRequestSettings();
                var signature = new PromptSignature(settings, function.PromptStart);
                signatures.Add(signature);

                Console.WriteLine($"Signature générée pour {function.SkillName}.{function.FunctionName}: '{function.PromptStart}'");
            }

            return signatures;
        }

        /// <summary>
        /// Teste la détection de préfixes
        /// </summary>
        private List<PrefixDetectionResult> TestPrefixDetection(
            List<PromptSignature> signatures,
            List<SkillFunctionInfo> skillFunctions)
        {
            var results = new List<PrefixDetectionResult>();

            // Pour chaque fonction, créer un job de complétion et tester si les signatures correspondent
            foreach (var function in skillFunctions)
            {
                var completionJob = new CompletionJob(function.PromptText, new AIRequestSettings());
                var matchingSignatures = new List<PromptSignatureMatch>();

                foreach (var signature in signatures)
                {
                    bool matches = signature.Matches(completionJob);

                    if (matches)
                    {
                        matchingSignatures.Add(new PromptSignatureMatch
                        {
                            Signature = signature,
                            IsExactMatch = signature.PromptStart == function.PromptStart
                        });
                    }
                }

                var result = new PrefixDetectionResult
                {
                    Function = function,
                    MatchingSignatures = matchingSignatures
                };

                results.Add(result);

                Console.WriteLine($"Test pour {function.SkillName}.{function.FunctionName}: " +
                                 $"{matchingSignatures.Count} signatures correspondantes");
            }

            return results;
        }

        /// <summary>
        /// Génère un rapport des résultats
        /// </summary>
        private async Task GenerateReportAsync(List<PrefixDetectionResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Rapport de Test de Détection de Préfixes");
            sb.AppendLine();
            sb.AppendLine("## Résumé");
            sb.AppendLine();
            sb.AppendLine($"- Nombre total de fonctions testées: {results.Count}");
            sb.AppendLine($"- Fonctions avec correspondance exacte: {results.Count(r => r.MatchingSignatures.Any(m => m.IsExactMatch))}");
            sb.AppendLine($"- Fonctions avec correspondances multiples: {results.Count(r => r.MatchingSignatures.Count > 1)}");
            sb.AppendLine($"- Fonctions sans correspondance: {results.Count(r => r.MatchingSignatures.Count == 0)}");
            sb.AppendLine();

            sb.AppendLine("## Résultats Détaillés");
            sb.AppendLine();

            foreach (var result in results)
            {
                sb.AppendLine($"### {result.Function.SkillName}.{result.Function.FunctionName}");
                sb.AppendLine();
                sb.AppendLine($"- Préfixe: `{result.Function.PromptStart}`");
                sb.AppendLine($"- Nombre de signatures correspondantes: {result.MatchingSignatures.Count}");
                sb.AppendLine();

                if (result.MatchingSignatures.Count > 0)
                {
                    sb.AppendLine("#### Signatures Correspondantes");
                    sb.AppendLine();
                    sb.AppendLine("| Préfixe | Correspondance Exacte |");
                    sb.AppendLine("|---------|----------------------|");

                    foreach (var match in result.MatchingSignatures)
                    {
                        sb.AppendLine($"| `{match.Signature.PromptStart}` | {(match.IsExactMatch ? "Oui" : "Non")} |");
                    }

                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine("Aucune signature correspondante trouvée.");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("## Analyse des Problèmes Potentiels");
            sb.AppendLine();

            // Identifier les préfixes qui se chevauchent
            var overlappingPrefixes = this.IdentifyOverlappingPrefixes(results);

            if (overlappingPrefixes.Count > 0)
            {
                sb.AppendLine("### Préfixes qui se Chevauchent");
                sb.AppendLine();
                sb.AppendLine("Les préfixes suivants se chevauchent et pourraient causer des problèmes de détection:");
                sb.AppendLine();

                foreach (var overlap in overlappingPrefixes)
                {
                    sb.AppendLine($"- `{overlap.Prefix1}` et `{overlap.Prefix2}` (Fonctions: {overlap.Function1} et {overlap.Function2})");
                }

                sb.AppendLine();
            }

            // Identifier les fonctions sans correspondance exacte
            var noExactMatches = results.Where(r => !r.MatchingSignatures.Any(m => m.IsExactMatch)).ToList();

            if (noExactMatches.Count > 0)
            {
                sb.AppendLine("### Fonctions sans Correspondance Exacte");
                sb.AppendLine();
                sb.AppendLine("Les fonctions suivantes n'ont pas de correspondance exacte:");
                sb.AppendLine();

                foreach (var result in noExactMatches)
                {
                    sb.AppendLine($"- {result.Function.SkillName}.{result.Function.FunctionName} (Préfixe: `{result.Function.PromptStart}`)");
                }

                sb.AppendLine();
            }

            string reportPath = Path.Combine(this._outputDirectory, "prefix_detection_report.md");
            await File.WriteAllTextAsync(reportPath, sb.ToString());
        }

        /// <summary>
        /// Identifie les préfixes qui se chevauchent
        /// </summary>
        private List<OverlappingPrefix> IdentifyOverlappingPrefixes(List<PrefixDetectionResult> results)
        {
            var overlappingPrefixes = new List<OverlappingPrefix>();

            for (int i = 0; i < results.Count; i++)
            {
                for (int j = i + 1; j < results.Count; j++)
                {
                    var prefix1 = results[i].Function.PromptStart;
                    var prefix2 = results[j].Function.PromptStart;

                    if (prefix1.StartsWith(prefix2) || prefix2.StartsWith(prefix1))
                    {
                        overlappingPrefixes.Add(new OverlappingPrefix
                        {
                            Prefix1 = prefix1,
                            Prefix2 = prefix2,
                            Function1 = $"{results[i].Function.SkillName}.{results[i].Function.FunctionName}",
                            Function2 = $"{results[j].Function.SkillName}.{results[j].Function.FunctionName}"
                        });
                    }
                }
            }

            return overlappingPrefixes;
        }
    }

    /// <summary>
    /// Informations sur une fonction d'un skill
    /// </summary>
    public class SkillFunctionInfo
    {
        public string SkillName { get; set; } = "";
        public string FunctionName { get; set; } = "";
        public string PromptText { get; set; } = "";
        public string ConfigJson { get; set; } = "";
        public string PromptStart { get; set; } = "";
    }

    /// <summary>
    /// Résultat d'un test de détection de préfixe
    /// </summary>
    public class PrefixDetectionResult
    {
        public SkillFunctionInfo Function { get; set; } = new();
        public List<PromptSignatureMatch> MatchingSignatures { get; set; } = new();
    }

    /// <summary>
    /// Correspondance d'une signature de prompt
    /// </summary>
    public class PromptSignatureMatch
    {
        public PromptSignature Signature { get; set; } = new();
        public bool IsExactMatch { get; set; }
    }

    /// <summary>
    /// Préfixes qui se chevauchent
    /// </summary>
    public class OverlappingPrefix
    {
        public string Prefix1 { get; set; } = "";
        public string Prefix2 { get; set; } = "";
        public string Function1 { get; set; } = "";
        public string Function2 { get; set; } = "";
    }

    /// <summary>
    /// Programme principal
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string skillsDirectory = args.Length > 0 ? args[0] : "../../Samples/skills";
            string outputDirectory = args.Length > 1 ? args[1] : "../results";
            int truncationLength = args.Length > 2 ? int.Parse(args[2]) : 11;

            var tester = new PrefixDetectionTester(skillsDirectory, outputDirectory, truncationLength);
            await tester.RunTestsAsync();
        }
    }
}
