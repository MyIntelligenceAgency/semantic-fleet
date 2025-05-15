using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmallModelTestDataGenerator
{
    /// <summary>
    /// Générateur de données de test adaptées aux modèles plus petits pour le MultiConnector.
    /// </summary>
    public class Program
    {
        private static readonly Random random = new Random();

        public static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: dotnet run -- <output_directory>");
                return;
            }

            string outputDirectory = args[0];
            Directory.CreateDirectory(outputDirectory);

            Console.WriteLine($"Génération des données de test pour les modèles plus petits dans {outputDirectory}...");

            // Générer des données pour différents niveaux de complexité
            await GenerateTestDataForComplexity(outputDirectory, "Trivial");
            await GenerateTestDataForComplexity(outputDirectory, "Simple");

            Console.WriteLine("Génération des données de test terminée.");
        }

        private static async Task GenerateTestDataForComplexity(string outputDirectory, string complexity)
        {
            Console.WriteLine($"Génération des données pour le niveau de complexité: {complexity}");

            // Créer un répertoire pour le niveau de complexité
            string complexityDir = Path.Combine(outputDirectory, complexity);
            Directory.CreateDirectory(complexityDir);

            // Générer des données pour différentes fonctions
            await GenerateSummarizeData(complexityDir, complexity);
            await GenerateChatData(complexityDir, complexity);
            await GenerateWriterData(complexityDir, complexity);
            await GenerateClassificationData(complexityDir, complexity);
        }

        private static async Task GenerateSummarizeData(string outputDirectory, string complexity)
        {
            var data = new List<TestData>();

            // Ajuster la longueur et la complexité du texte en fonction du niveau de complexité
            int textLength = complexity == "Trivial" ? 200 : 400;
            int sentenceComplexity = complexity == "Trivial" ? 1 : 2;

            for (int i = 0; i < 10; i++)
            {
                string text = GenerateText(textLength, sentenceComplexity);
                data.Add(new TestData
                {
                    Input = text,
                    ExpectedOutput = GenerateSummary(text, complexity),
                    Complexity = complexity
                });
            }

            await SaveTestData(outputDirectory, "SummarizeData.json", data);
        }

        private static async Task GenerateChatData(string outputDirectory, string complexity)
        {
            var data = new List<TestData>();

            // Ajuster la longueur et la complexité des messages en fonction du niveau de complexité
            int messageCount = complexity == "Trivial" ? 3 : 5;
            int messageLength = complexity == "Trivial" ? 50 : 100;

            for (int i = 0; i < 10; i++)
            {
                var conversation = GenerateConversation(messageCount, messageLength);
                data.Add(new TestData
                {
                    Input = JsonConvert.SerializeObject(conversation),
                    ExpectedOutput = GenerateChatResponse(conversation, complexity),
                    Complexity = complexity
                });
            }

            await SaveTestData(outputDirectory, "ChatData.json", data);
        }

        private static async Task GenerateWriterData(string outputDirectory, string complexity)
        {
            var data = new List<TestData>();

            // Ajuster la complexité des sujets en fonction du niveau de complexité
            var topics = complexity == "Trivial"
                ? new[] { "chats", "chiens", "maisons", "écoles", "parcs" }
                : new[] { "intelligence artificielle", "changement climatique", "économie mondiale", "exploration spatiale", "médecine moderne" };

            for (int i = 0; i < 10; i++)
            {
                string topic = topics[random.Next(topics.Length)];
                data.Add(new TestData
                {
                    Input = $"Écrivez un court paragraphe sur {topic}.",
                    ExpectedOutput = GenerateWriterResponse(topic, complexity),
                    Complexity = complexity
                });
            }

            await SaveTestData(outputDirectory, "WriterData.json", data);
        }

        private static async Task GenerateClassificationData(string outputDirectory, string complexity)
        {
            var data = new List<TestData>();

            // Ajuster la complexité des textes à classifier en fonction du niveau de complexité
            int textLength = complexity == "Trivial" ? 100 : 200;
            var categories = new[] { "Positif", "Négatif", "Neutre" };

            for (int i = 0; i < 10; i++)
            {
                string text = GenerateText(textLength, complexity == "Trivial" ? 1 : 2);
                string category = categories[random.Next(categories.Length)];

                // Ajuster le texte pour correspondre à la catégorie
                text = AdjustTextForCategory(text, category);

                data.Add(new TestData
                {
                    Input = text,
                    ExpectedOutput = category,
                    Complexity = complexity
                });
            }

            await SaveTestData(outputDirectory, "ClassificationData.json", data);
        }

        private static string GenerateText(int length, int sentenceComplexity)
        {
            var simpleSentenceTemplates = new[]
            {
                "Le {sujet} est {adjectif}.",
                "{sujet} {verbe} {objet}.",
                "Il y a {nombre} {sujet} dans {lieu}."
            };

            var complexSentenceTemplates = new[]
            {
                "Le {sujet}, qui est {adjectif}, {verbe} {objet} quand {condition}.",
                "Bien que le {sujet} soit {adjectif}, il {verbe} néanmoins {objet} pour {raison}.",
                "En considérant que {sujet} {verbe} {objet}, nous pouvons conclure que {conclusion}."
            };

            var sujets = new[] { "chat", "chien", "homme", "femme", "enfant", "professeur", "étudiant", "scientifique", "artiste", "médecin" };
            var adjectifs = new[] { "grand", "petit", "intelligent", "beau", "fort", "rapide", "lent", "curieux", "créatif", "prudent" };
            var verbes = new[] { "mange", "court", "dort", "parle", "étudie", "travaille", "joue", "lit", "écrit", "observe" };
            var objets = new[] { "livre", "pomme", "maison", "voiture", "ordinateur", "téléphone", "arbre", "fleur", "montagne", "rivière" };
            var lieux = new[] { "maison", "école", "bureau", "parc", "ville", "forêt", "montagne", "plage", "bibliothèque", "restaurant" };
            var nombres = new[] { "deux", "trois", "quatre", "cinq", "plusieurs", "beaucoup de", "peu de", "quelques" };
            var conditions = new[] { "il fait beau", "il pleut", "c'est nécessaire", "c'est possible", "le temps le permet" };
            var raisons = new[] { "apprendre", "se divertir", "se reposer", "travailler", "s'améliorer" };
            var conclusions = new[] { "c'est important", "c'est intéressant", "c'est utile", "c'est nécessaire", "c'est bénéfique" };

            var templates = sentenceComplexity == 1 ? simpleSentenceTemplates : complexSentenceTemplates;
            var sb = new StringBuilder();

            while (sb.Length < length)
            {
                var template = templates[random.Next(templates.Length)];

                template = template
                    .Replace("{sujet}", sujets[random.Next(sujets.Length)])
                    .Replace("{adjectif}", adjectifs[random.Next(adjectifs.Length)])
                    .Replace("{verbe}", verbes[random.Next(verbes.Length)])
                    .Replace("{objet}", objets[random.Next(objets.Length)])
                    .Replace("{lieu}", lieux[random.Next(lieux.Length)])
                    .Replace("{nombre}", nombres[random.Next(nombres.Length)])
                    .Replace("{condition}", conditions[random.Next(conditions.Length)])
                    .Replace("{raison}", raisons[random.Next(raisons.Length)])
                    .Replace("{conclusion}", conclusions[random.Next(conclusions.Length)]);

                sb.Append(template).Append(" ");
            }

            return sb.ToString().Trim();
        }

        private static string GenerateSummary(string text, string complexity)
        {
            // Simuler un résumé en prenant les premières phrases
            var sentences = text.Split('.', StringSplitOptions.RemoveEmptyEntries);
            int sentenceCount = complexity == "Trivial" ? 1 : 2;

            return string.Join(". ", sentences.Take(sentenceCount)) + ".";
        }

        private static List<ChatMessage> GenerateConversation(int messageCount, int messageLength)
        {
            var conversation = new List<ChatMessage>();
            var roles = new[] { "user", "assistant" };

            for (int i = 0; i < messageCount; i++)
            {
                conversation.Add(new ChatMessage
                {
                    Role = roles[i % 2],
                    Content = GenerateText(messageLength, 1)
                });
            }

            return conversation;
        }

        private static string GenerateChatResponse(List<ChatMessage> conversation, string complexity)
        {
            // Générer une réponse basée sur le dernier message
            var lastMessage = conversation.Last();

            if (lastMessage.Role == "assistant")
            {
                return GenerateText(complexity == "Trivial" ? 50 : 100, complexity == "Trivial" ? 1 : 2);
            }
            else
            {
                return "Je comprends votre question. " + GenerateText(complexity == "Trivial" ? 30 : 80, complexity == "Trivial" ? 1 : 2);
            }
        }

        private static string GenerateWriterResponse(string topic, string complexity)
        {
            int paragraphLength = complexity == "Trivial" ? 100 : 200;
            return GenerateText(paragraphLength, complexity == "Trivial" ? 1 : 2);
        }

        private static string AdjustTextForCategory(string text, string category)
        {
            var positiveWords = new[] { "excellent", "merveilleux", "fantastique", "incroyable", "superbe" };
            var negativeWords = new[] { "terrible", "horrible", "mauvais", "décevant", "médiocre" };
            var neutralWords = new[] { "normal", "standard", "moyen", "ordinaire", "typique" };

            var words = text.Split(' ');
            var sb = new StringBuilder();

            for (int i = 0; i < words.Length; i++)
            {
                if (i > 0 && i % 10 == 0)
                {
                    string[] categoryWords = category switch
                    {
                        "Positif" => positiveWords,
                        "Négatif" => negativeWords,
                        _ => neutralWords
                    };

                    sb.Append(categoryWords[random.Next(categoryWords.Length)]).Append(" ");
                }

                sb.Append(words[i]).Append(" ");
            }

            return sb.ToString().Trim();
        }

        private static async Task SaveTestData(string directory, string filename, List<TestData> data)
        {
            string filePath = Path.Combine(directory, filename);
            await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(data, Formatting.Indented));
            Console.WriteLine($"Données de test enregistrées dans: {filePath}");
        }
    }

    public class TestData
    {
        public string Input { get; set; }
        public string ExpectedOutput { get; set; }
        public string Complexity { get; set; }
    }

    public class ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}
