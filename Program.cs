using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLPv2.Common;
using NLPv2.Infrastructure;
using NLPv2.Services;
using Serilog;

namespace NLPv2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Charger la configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Configurer le logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("Démarrage de l'application...");

                // Configurer les services
                var services = new ServiceCollection();

                // Ajouter la configuration
                services.AddSingleton<IConfiguration>(configuration);

                // Configurer les options
                services.Configure<DatabaseSettings>(configuration.GetSection("Database"));
                services.Configure<MLSettings>(configuration.GetSection("ML"));

                // Enregistrer les services
                services.AddSingleton<ILanguageDetectionService, LanguageDetectionService>();
                services.AddSingleton<IStatisticalClassificationService, StatisticalClassificationService>();
                services.AddSingleton<IMLClassificationService, MLClassificationService>();
                services.AddSingleton<ICombinedClassificationService, CombinedClassificationService>();
                services.AddSingleton<IDatabaseContext, DatabaseContext>();

                var serviceProvider = services.BuildServiceProvider();

                // Récupérer les services
                var dbContext = serviceProvider.GetRequiredService<IDatabaseContext>();
                var mlService = serviceProvider.GetRequiredService<IMLClassificationService>();
                var statisticalService = serviceProvider.GetRequiredService<IStatisticalClassificationService>();
                var combinedService = serviceProvider.GetRequiredService<ICombinedClassificationService>();

                // 1. Load training data
                Log.Information("Loading training data...");
                var trainingData = dbContext.GetAllSwiftData();
                Log.Information("Loaded {Count} records from database", trainingData.Count);

                // 2. Train ML model
                Log.Information("Training ML model...");
                mlService.TrainModel(trainingData);
                Log.Information("Model training completed");

                // 3. Evaluate ML model
                Log.Information("Evaluating ML model...");
                mlService.EvaluateModel(trainingData);

                // 4. Learn optimal weights for combined approach
                Log.Information("Learning optimal weights for combined approach...");
                combinedService.LearnWeights(trainingData);
                Log.Information("Weight optimization completed");

                // 5. Interactive classification loop
                Log.Information("Starting interactive classification...");
                Console.WriteLine("\nEnter SWIFT text to classify (type 'EXIT' to quit):");
                Console.WriteLine("================================================");

                while (true)
                {
                    Console.Write("\nSWIFT > ");
                    var input = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(input) || input.ToUpper() == "EXIT")
                        break;

                    var result = combinedService.ClassifyText(input);

                    Console.WriteLine($"\nClassification Results:");
                    Console.WriteLine($"Category: {result.Category}");
                    Console.WriteLine($"Language: {result.Language}");
                    Console.WriteLine("\nProbabilities:");
                    foreach (var prob in result.Probabilities.OrderByDescending(p => p.Value))
                    {
                        Console.WriteLine($"Category {prob.Key}: {prob.Value:P2}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Une erreur fatale s'est produite");
            }
            finally
            {
                Log.Information("Arrêt de l'application");
                Log.CloseAndFlush();
            }
        }
    }
}
