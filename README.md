
using System.Collections.Generic;

namespace EdiFileProcessor.Models
{
    public class EdiConfig
    {
        public List<StateDirectory> States { get; set; }
    }

    public class StateDirectory
    {
        public string StateCode { get; set; }
        public string Frequency { get; set; }  // "Daily" or "Monthly"
        public List<string> Directories { get; set; }
    }
}




using EdiFileProcessor.Models;
using System;
using System.IO;
using Newtonsoft.Json;

namespace EdiFileProcessor.Helpers
{
    public static class ConfigLoader
    {
        public static EdiConfig Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Config file not found: {path}");

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<EdiConfig>(json);
        }
    }
}



using EdiFileProcessor.Models;
using EdiFileProcessor.Helpers;
using System;
using System.Linq;

namespace EdiFileProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            string frequencyToRun = "Daily"; // Or "Monthly"

            string configPath = "ediConfig.json";
            EdiConfig config = ConfigLoader.Load(configPath);

            var dirsToProcess = config.States
                .Where(s => s.Frequency.Equals(frequencyToRun, StringComparison.OrdinalIgnoreCase))
                .SelectMany(s => s.Directories)
                .ToList();

            Console.WriteLine($"Processing {dirsToProcess.Count} directories for {frequencyToRun} frequency:");
            foreach (var dir in dirsToProcess)
            {
                Console.WriteLine($" → {dir}");
                // Next: Copy files, validate, move to archive, etc.
            }

            Console.ReadLine();
        }
    }
}

