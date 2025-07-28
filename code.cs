using EdiFileProcessor.Models;
using EdiFileProcessor.Helpers;
using System;
using System.Linq;
using System.IO;

namespace EdiFileProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string configPath = "ediConfig.json";
                EdiConfig config = ConfigLoader.Load(configPath);

                // Decide which frequencies should run today
                bool isDaily = true;
                bool isMonthly = DateTime.Today.Day == 1;

                var dirsToProcess = config.States
                    .Where(s =>
                        (isDaily && s.Frequency.Equals("Daily", StringComparison.OrdinalIgnoreCase)) ||
                        (isMonthly && s.Frequency.Equals("Monthly", StringComparison.OrdinalIgnoreCase))
                    )
                    .SelectMany(s => s.Directories)
                    .ToList();

                if (!dirsToProcess.Any())
                {
                    Console.WriteLine("No directories to process today based on config and date.");
                    return;
                }

                Console.WriteLine($"📁 Directories to process today: {dirsToProcess.Count}");
                foreach (var dir in dirsToProcess)
                {
                    Console.WriteLine($" → {dir}");
                    
                    // Sample code to copy files (can expand later)
                    if (Directory.Exists(dir))
                    {
                        string[] files = Directory.GetFiles(dir, "*.edi");
                        foreach (var file in files)
                        {
                            Console.WriteLine($"    - Found file: {Path.GetFileName(file)}");

                            // TODO: Copy to local temp folder and call validation
                            // TODO: Archive if successful
                        }
                    }
                    else
                    {
                        Console.WriteLine($"    - Directory not found: {dir}");
                    }
                }

                Console.WriteLine("\n✅ Directory scan completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
