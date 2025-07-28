using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using EdiFileProcessor.Models;
using EdiFileProcessor.Helpers;

namespace EdiFileProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== HIPAA 834 EDI File Processor ===");

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ediConfig.json");
            if (!File.Exists(configPath))
            {
                Console.WriteLine("❌ ediConfig.json not found!");
                return;
            }

            var configJson = File.ReadAllText(configPath);
            var config = JsonConvert.DeserializeObject<EdiConfig>(configJson);

            string workingRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Working");
            string reportRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Report");
            string archiveRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archive", DateTime.Now.ToString("yyyy-MM-dd"));

            Directory.CreateDirectory(workingRoot);
            Directory.CreateDirectory(reportRoot);
            Directory.CreateDirectory(archiveRoot);

            foreach (var state in config.States)
            {
                if (!ShouldProcessToday(state.Frequency)) continue;

                Console.WriteLine($"\n📂 Processing state: {state.StateCode} ({state.Frequency})");

                foreach (var dir in state.Directories)
                {
                    if (!Directory.Exists(dir))
                    {
                        Console.WriteLine($"  ⚠ Skipping. Directory not found: {dir}");
                        continue;
                    }

                    var ediFiles = Directory.GetFiles(dir, "*.edi");
                    if (ediFiles.Length == 0)
                    {
                        Console.WriteLine("  ℹ No .edi files found.");
                        continue;
                    }

                    string stateWorkDir = Path.Combine(workingRoot, state.StateCode);
                    string stateArchiveDir = Path.Combine(archiveRoot, state.StateCode);
                    Directory.CreateDirectory(stateWorkDir);
                    Directory.CreateDirectory(stateArchiveDir);

                    foreach (var file in ediFiles)
                    {
                        string fileName = Path.GetFileName(file);
                        string localFilePath = Path.Combine(stateWorkDir, fileName);
                        string reportFilePath = Path.Combine(reportRoot, $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                        string archiveFilePath = Path.Combine(stateArchiveDir, fileName);

                        try
                        {
                            File.Copy(file, localFilePath, true);
                            Console.WriteLine($"    ✔ Copied: {fileName}");

                            // 🏥 HIPAA Validation Logic
                            HipaaValidator.ValidateFile(localFilePath, reportFilePath);
                            Console.WriteLine($"    📊 Report Generated: {reportFilePath}");

                            // 🗄 Archive
                            File.Copy(localFilePath, archiveFilePath, true);
                            Console.WriteLine($"    📁 Archived: {archiveFilePath}");

                            // Delete copied working file if needed
                            File.Delete(localFilePath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"    ❌ Error: {ex.Message}");
                        }
                    }
                }
            }

            Console.WriteLine("\n✅ Processing complete.");
        }

        static bool ShouldProcessToday(string frequency)
        {
            if (string.Equals(frequency, "Daily", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(frequency, "Monthly", StringComparison.OrdinalIgnoreCase))
                return DateTime.Today.Day == 1; // Only run Monthly on 1st

            return false;
        }
    }
}
