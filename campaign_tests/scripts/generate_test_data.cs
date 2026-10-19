using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CampaignTests
{
    /// <summary>
    /// Niveau de complexité des données de test
    /// </summary>
    public enum ComplexityLevel
    {
        Trivial,
        Simple,
        Medium,
        Hard
    }

    /// <summary>
    /// Catégorie de fonction Semantic Kernel
    /// </summary>
    public enum FunctionCategory
    {
        Summarize,
        Chat,
        Writer,
        Classification,
        Coding,
        QA,
        Misc
    }

    /// <summary>
    /// Générateur de données de test pour les fonctions Semantic Kernel
    /// </summary>
    public class TestDataGenerator
    {
        private readonly string _outputDirectory;
        private readonly Random _random = new Random();

        public TestDataGenerator(string outputDirectory)
        {
            this._outputDirectory = outputDirectory;
            Directory.CreateDirectory(outputDirectory);
        }

        /// <summary>
        /// Génère des jeux de données pour toutes les catégories et niveaux de complexité
        /// </summary>
        public async Task GenerateAllTestDataAsync()
        {
            foreach (FunctionCategory category in Enum.GetValues(typeof(FunctionCategory)))
            {
                foreach (ComplexityLevel level in Enum.GetValues(typeof(ComplexityLevel)))
                {
                    await this.GenerateTestDataAsync(category, level);
                }
            }
        }

        /// <summary>
        /// Génère un jeu de données pour une catégorie et un niveau de complexité spécifiques
        /// </summary>
        public async Task GenerateTestDataAsync(FunctionCategory category, ComplexityLevel level)
        {
            var testData = new List<string>();

            // Nombre d'échantillons à générer par catégorie et niveau
            int sampleCount = 5;

            for (int i = 0; i < sampleCount; i++)
            {
                string sample = this.GenerateSample(category, level);
                testData.Add(sample);
            }

            string fileName = $"{category}_{level}.json";
            string filePath = Path.Combine(this._outputDirectory, fileName);

            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(testData, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Generated test data: {filePath}");
        }

        /// <summary>
        /// Génère un échantillon de test en fonction de la catégorie et du niveau de complexité
        /// </summary>
        private string GenerateSample(FunctionCategory category, ComplexityLevel level)
        {
            switch (category)
            {
                case FunctionCategory.Summarize:
                    return this.GenerateSummarizeData(level);
                case FunctionCategory.Chat:
                    return this.GenerateChatData(level);
                case FunctionCategory.Writer:
                    return this.GenerateWriterData(level);
/// <summary>
        /// Génère des données pour les fonctions de résumé
        /// </summary>
        private string GenerateSummarizeData(ComplexityLevel level)
        {
            switch (level)
            {
                case ComplexityLevel.Trivial:
                    return this.GetRandomElement(TrivialSummarizeData);
                case ComplexityLevel.Simple:
                    return this.GetRandomElement(SimpleSummarizeData);
                case ComplexityLevel.Medium:
                    return this.GetRandomElement(MediumSummarizeData);
                case ComplexityLevel.Hard:
                    return this.GetRandomElement(HardSummarizeData);
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        /// <summary>
        /// Génère des données pour les fonctions de chat
        /// </summary>
        private string GenerateChatData(ComplexityLevel level)
        {
            switch (level)
            {
                case ComplexityLevel.Trivial:
                    return this.GetRandomElement(TrivialChatData);
                case ComplexityLevel.Simple:
                    return this.GetRandomElement(SimpleChatData);
                case ComplexityLevel.Medium:
                    return this.GetRandomElement(MediumChatData);
                case ComplexityLevel.Hard:
                    return this.GetRandomElement(HardChatData);
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        /// <summary>
        /// Génère des données pour les fonctions d'écriture
        /// </summary>
        private string GenerateWriterData(ComplexityLevel level)
        {
            switch (level)
            {
                case ComplexityLevel.Trivial:
                    return this.GetRandomElement(TrivialWriterData);
                case ComplexityLevel.Simple:
                    return this.GetRandomElement(SimpleWriterData);
                case ComplexityLevel.Medium:
                    return this.GetRandomElement(MediumWriterData);
                case ComplexityLevel.Hard:
                    return this.GetRandomElement(HardWriterData);
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        /// <summary>
        /// Génère des données pour les fonctions de classification
        /// </summary>
        private string GenerateClassificationData(ComplexityLevel level)
        {
            switch (level)
            {
                case ComplexityLevel.Trivial:
                    return this.GetRandomElement(TrivialClassificationData);
                case ComplexityLevel.Simple:
                    return this.GetRandomElement(SimpleClassificationData);
                case ComplexityLevel.Medium:
                    return this.GetRandomElement(MediumClassificationData);
                case ComplexityLevel.Hard:
                    return this.GetRandomElement(HardClassificationData);
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        /// <summary>
        /// Génère des données pour les fonctions de codage
        /// </summary>
        private string GenerateCodingData(ComplexityLevel level)
        {
            switch (level)
            {
                case ComplexityLevel.Trivial:
                    return this.GetRandomElement(TrivialCodingData);
                case ComplexityLevel.Simple:
                    return this.GetRandomElement(SimpleCodingData);
                case ComplexityLevel.Medium:
                    return this.GetRandomElement(MediumCodingData);
                case ComplexityLevel.Hard:
                    return this.GetRandomElement(HardCodingData);
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }
                case FunctionCategory.Classification:
                    return this.GenerateClassificationData(level);
                case FunctionCategory.Coding:
                    return this.GenerateCodingData(level);
                case FunctionCategory.QA:
                    return this.GenerateQAData(level);
                case FunctionCategory.Misc:
                    return this.GenerateMiscData(level);
                default:
                    throw new ArgumentOutOfRangeException(nameof(category));
            }
        }

/// <summary>
        /// Génère des données pour les fonctions de questions-réponses
        /// </summary>
        private string GenerateQAData(ComplexityLevel level)
        {
            switch (level)
            {
                case ComplexityLevel.Trivial:
                    return this.GetRandomElement(TrivialQAData);
                case ComplexityLevel.Simple:
                    return this.GetRandomElement(SimpleQAData);
                case ComplexityLevel.Medium:
                    return this.GetRandomElement(MediumQAData);
                case ComplexityLevel.Hard:
                    return this.GetRandomElement(HardQAData);
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        /// <summary>
        /// Génère des données pour les fonctions diverses
        /// </summary>
        private string GenerateMiscData(ComplexityLevel level)
        {
            switch (level)
            {
                case ComplexityLevel.Trivial:
                    return this.GetRandomElement(TrivialMiscData);
                case ComplexityLevel.Simple:
                    return this.GetRandomElement(SimpleMiscData);
                case ComplexityLevel.Medium:
                    return this.GetRandomElement(MediumMiscData);
                case ComplexityLevel.Hard:
                    return this.GetRandomElement(HardMiscData);
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        /// <summary>
        /// Sélectionne un élément aléatoire dans une liste
        /// </summary>
        private T GetRandomElement<T>(IReadOnlyList<T> list)
        {
            return list[this._random.Next(list.Count)];
        }

        #region Données de test

        // Données pour les fonctions de résumé
        private static readonly List<string> TrivialSummarizeData = new()
        {
            "Le ciel est bleu. Il fait beau aujourd'hui.",
            "J'ai mangé une pomme. Elle était délicieuse.",
            "Le chat dort sur le canapé. Il ronronne doucement.",
            "La voiture est rouge. Elle est garée devant la maison.",
            "L'oiseau chante dans l'arbre. Son chant est mélodieux."
        };

        private static readonly List<string> SimpleSummarizeData = new()
        {
            "Le réchauffement climatique est un phénomène d'augmentation de la température moyenne des océans et de l'atmosphère terrestre. Il est principalement causé par les émissions de gaz à effet de serre liées aux activités humaines. Les conséquences sont nombreuses : montée des eaux, événements climatiques extrêmes, perturbation des écosystèmes.",
            "La photosynthèse est le processus par lequel les plantes et certaines bactéries utilisent l'énergie lumineuse pour produire du glucose à partir de dioxyde de carbone et d'eau. Ce processus libère de l'oxygène comme sous-produit. La photosynthèse est essentielle à la vie sur Terre car elle fournit de l'oxygène et de la nourriture.",
            "L'intelligence artificielle (IA) est un ensemble de théories et de techniques développant des programmes informatiques complexes capables de simuler certains traits de l'intelligence humaine. Elle comprend l'apprentissage automatique, le traitement du langage naturel et la vision par ordinateur. L'IA est utilisée dans de nombreux domaines comme la médecine, la finance et les transports.",
            "La Révolution française est une période de bouleversements sociaux et politiques qui a eu lieu en France entre 1789 et 1799. Elle a conduit à l'abolition de la monarchie absolue et à l'établissement d'une république. Les idéaux de liberté, d'égalité et de fraternité ont inspiré de nombreux mouvements politiques dans le monde entier.",
            "Le système solaire est composé du Soleil et de tous les objets célestes qui gravitent autour de lui : huit planètes, leurs satellites, des astéroïdes, des comètes et des météoroïdes. La Terre est la troisième planète à partir du Soleil et la seule connue pour abriter la vie."
        };
