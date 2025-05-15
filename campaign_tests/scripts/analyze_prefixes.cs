using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace CampaignTests
{
    /// <summary>
    /// Classe pour analyser les préfixes des fonctions Semantic Kernel
    /// </summary>
    public class PrefixAnalyzer
    {
        private readonly string _skillsDirectory;
        private readonly int _truncationLength;
        private readonly Dictionary<string, List<SkillFunctionInfo>> _skillFunctions = new();

        public PrefixAnalyzer(string skillsDirectory, int truncationLength = 11)
        {
            this._skillsDirectory = skillsDirectory;
            this._truncationLength = truncationLength;
        }

        /// <summary>
        /// Analyse tous les skills et fonctions dans le répertoire spécifié
        /// </summary>
        public async Task AnalyzeAllSkillsAsync()
        {
            var skillDirectories = Directory.GetDirectories(this._skillsDirectory);

            foreach (var skillDir in skillDirectories)
            {
                var skillName = Path.GetFileName(skillDir);
                var functionDirectories = Directory.GetDirectories(skillDir);

                foreach (var functionDir in functionDirectories)
                {
                    var functionName = Path.GetFileName(functionDir);
                    await this.AnalyzeSkillFunctionAsync(skillName, functionName, functionDir);
                }
            }
        }

        /// <summary>
        /// Analyse une fonction spécifique d'un skill
        /// </summary>
        private async Task AnalyzeSkillFunctionAsync(string skillName, string functionName, string functionDir)
        {
            var promptPath = Path.Combine(functionDir, "skprompt.txt");
            var configPath = Path.Combine(functionDir, "config.json");

            if (!File.Exists(promptPath) || !File.Exists(configPath))
            {
                Console.WriteLine($"Skipping {skillName}.{functionName}: Missing prompt or config file");
                return;
            }

            var promptText = await File.ReadAllTextAsync(promptPath);
            var configText = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(configText);

            var functionInfo = new SkillFunctionInfo
            {
                SkillName = skillName,
                FunctionName = functionName,
                PromptText = promptText,
                ConfigJson = configText,
                PromptType = config.ContainsKey("type") ? config["type"].GetString() : "unknown",
                PromptStart = promptText.Length > this._truncationLength ? promptText.Substring(0, this._truncationLength) : promptText,
                Variables = this.ExtractVariables(promptText)
            };

            if (!this._skillFunctions.ContainsKey(skillName))
            {
                this._skillFunctions[skillName] = new List<SkillFunctionInfo>();
            }

            this._skillFunctions[skillName].Add(functionInfo);

            Console.WriteLine($"Analyzed {skillName}.{functionName}");
            Console.WriteLine($"  Prompt Start: '{functionInfo.PromptStart}'");
            Console.WriteLine($"  Variables: {string.Join(", ", functionInfo.Variables)}");
            Console.WriteLine();
        }

        /// <summary>
        /// Extrait les variables d'un prompt (format {{$variable}})
        /// </summary>
        private List<string> ExtractVariables(string promptText)
        {
            var variables = new List<string>();
            var regex = new Regex(@"\{\{\$(.*?)\}\}");
            var matches = regex.Matches(promptText);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    variables.Add(match.Groups[1].Value);
                }
            }

            return variables.Distinct().ToList();
        }

        /// <summary>
        /// Génère un rapport d'analyse des préfixes
        /// </summary>
        public async Task GenerateReportAsync(string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Rapport d'Analyse des Préfixes de Fonctions Semantic Kernel");
            sb.AppendLine();
            sb.AppendLine("## Résumé");
            sb.AppendLine();
            sb.AppendLine($"- Nombre total de skills: {this._skillFunctions.Count}");
            sb.AppendLine($"- Nombre total de fonctions: {this._skillFunctions.Sum(kv => kv.Value.Count)}");
            sb.AppendLine();

            sb.AppendLine("## Analyse par Skill");
            sb.AppendLine();

            foreach (var skillEntry in this._skillFunctions)
            {
                sb.AppendLine($"### {skillEntry.Key}");
                sb.AppendLine();
                sb.AppendLine($"Nombre de fonctions: {skillEntry.Value.Count}");
                sb.AppendLine();
                sb.AppendLine("| Fonction | Type | Préfixe | Variables |");
                sb.AppendLine("|----------|------|---------|-----------|");

                foreach (var function in skillEntry.Value)
                {
                    var escapedPrefix = function.PromptStart.Replace("|", "\\|").Replace("\n", "\\n");
                    sb.AppendLine($"| {function.FunctionName} | {function.PromptType} | `{escapedPrefix}` | {string.Join(", ", function.Variables)} |");
                }

                sb.AppendLine();
            }

            sb.AppendLine("## Analyse des Patterns de Préfixes");
            sb.AppendLine();

            // Analyse des patterns communs dans les préfixes
            var prefixPatterns = this.AnalyzePrefixPatterns();
            foreach (var pattern in prefixPatterns)
            {
                sb.AppendLine($"- Pattern: `{pattern.Key}` - Utilisé dans {pattern.Value} fonctions");
            }

            await File.WriteAllTextAsync(outputPath, sb.ToString());
            Console.WriteLine($"Rapport généré: {outputPath}");
        }

        /// <summary>
        /// Analyse les patterns communs dans les préfixes
        /// </summary>
        private Dictionary<string, int> AnalyzePrefixPatterns()
        {
            var patterns = new Dictionary<string, int>();

            foreach (var skillEntry in this._skillFunctions)
            {
                foreach (var function in skillEntry.Value)
                {
                    // Analyse des premiers caractères (sans tenir compte des espaces et sauts de ligne)
                    var normalizedPrefix = Regex.Replace(function.PromptStart, @"\s+", " ").Trim();

                    if (normalizedPrefix.Length > 0)
                    {
                        if (!patterns.ContainsKey(normalizedPrefix))
                        {
                            patterns[normalizedPrefix] = 0;
                        }

                        patterns[normalizedPrefix]++;
                    }
                }
            }

            return patterns.OrderByDescending(kv => kv.Value)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        /// <summary>
        /// Génère des signatures de prompts pour chaque fonction
        /// </summary>
        public List<PromptSignature> GeneratePromptSignatures()
        {
            var signatures = new List<PromptSignature>();

            foreach (var skillEntry in this._skillFunctions)
            {
                foreach (var function in skillEntry.Value)
                {
                    var settings = new AIRequestSettings();

                    // Extraire les paramètres de configuration
                    try
                    {
                        var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(function.ConfigJson);
                        if (config.ContainsKey("completion"))
                        {
                            var completion = config["completion"];
                            if (completion.TryGetProperty("temperature", out var temp))
                            {
                                settings.ExtensionData["temperature"] = temp.GetDouble();
                            }

                            if (completion.TryGetProperty("top_p", out var topP))
                            {
                                settings.ExtensionData["top_p"] = topP.GetDouble();
                            }

                            if (completion.TryGetProperty("max_tokens", out var maxTokens))
                            {
                                settings.ExtensionData["max_tokens"] = maxTokens.GetInt32();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors de l'analyse de la configuration pour {skillEntry.Key}.{function.FunctionName}: {ex.Message}");
                    }

                    var signature = new PromptSignature(settings, function.PromptStart);
                    signatures.Add(signature);
                }
            }

            return signatures;
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
        public string PromptType { get; set; } = "";
        public string PromptStart { get; set; } = "";
        public List<string> Variables { get; set; } = new();
    }

    /// <summary>
    /// Programme principal
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string skillsDirectory = args.Length > 0 ? args[0] : "../../Samples/skills";
            string outputPath = args.Length > 1 ? args[1] : "../results/prefix_analysis_report.md";

            Console.WriteLine($"Analyzing skills in: {skillsDirectory}");
            Console.WriteLine($"Output report will be saved to: {outputPath}");

            var analyzer = new PrefixAnalyzer(skillsDirectory);
            await analyzer.AnalyzeAllSkillsAsync();
            await analyzer.GenerateReportAsync(outputPath);

            Console.WriteLine("Analysis complete!");
        }
    }
}
