using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ClosedXML.Excel;

namespace EdiFileProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== EDI File Processor with AI Insights ===");

            // Load config
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "edi_config.json");
            if (!File.Exists(configPath))
            {
                Console.WriteLine("Config file not found: " + configPath);
                return;
            }

            var configJson = File.ReadAllText(configPath);
            var config = JsonConvert.DeserializeObject<List<StateConfig>>(configJson);

            foreach (var state in config)
            {
                Console.WriteLine($"Processing state: {state.State} ({state.Frequency})");

                if (!Directory.Exists(state.Path))
                {
                    Console.WriteLine($"Skipping missing directory: {state.Path}");
                    continue;
                }

                // Working/report/archive paths created under source dir
                string workingPath = Path.Combine(state.Path, "Working");
                string reportPath = Path.Combine(state.Path, "Report");
                string archivePath = Path.Combine(state.Path, "Archive", DateTime.Now.ToString("yyyy-MM-dd"));

                Directory.CreateDirectory(workingPath);
                Directory.CreateDirectory(reportPath);
                Directory.CreateDirectory(archivePath);

                // Get all allowed files
                var allowedExtensions = new[] { "*.p", "*.edi", "*.txt" };
                var files = allowedExtensions
                    .SelectMany(pattern => Directory.GetFiles(state.Path, pattern))
                    .ToList();

                if (files.Count == 0)
                {
                    Console.WriteLine($"No files found for state {state.State}");
                    continue;
                }

                List<FileProcessResult> results = new List<FileProcessResult>();

                foreach (var file in files)
                {
                    try
                    {
                        string fileName = Path.GetFileName(file);
                        string localFile = Path.Combine(workingPath, fileName);

                        // Copy file to Working folder
                        File.Copy(file, localFile, true);

                        var start = DateTime.Now;

                        // HIPAA validation placeholder (replace with your API logic)
                        bool isSuccess = HipaaValidator.Validate(localFile);

                        var end = DateTime.Now;
                        double procTime = (end - start).TotalSeconds;

                        // AI-like simulation
                        double predicted = Math.Round(procTime * 0.9 + 1.5, 2); // simple heuristic
                        string anomaly = procTime > predicted * 2 ? "Yes" : "No";
                        string confidence = new Random().Next(70, 99).ToString() + "%";

                        results.Add(new FileProcessResult
                        {
                            FileName = fileName,
                            State = state.State,
                            Records = new Random().Next(500, 2000), // simulate record count
                            ProcessingTime = procTime.ToString("0.00") + " sec",
                            Status = isSuccess ? "Pass" : "Fail",
                            AIPredictedTime = predicted + " sec",
                            AIAnomaly = anomaly,
                            AIConfidence = confidence
                        });

                        // Move original file to archive
                        string archiveFile = Path.Combine(archivePath, fileName);
                        File.Move(file, archiveFile);

                        Console.WriteLine($"Processed {fileName} [{state.State}] => { (isSuccess ? "OK" : "FAIL") }");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {file}: {ex.Message}");
                    }
                }

                // Save report
                SaveExcelReport(reportPath, results);
            }

            Console.WriteLine("=== Processing completed ===");
        }

        static void SaveExcelReport(string reportPath, List<FileProcessResult> results)
        {
            if (results.Count == 0) return;

            string reportFile = Path.Combine(reportPath, $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("AI_Report");

                // Insert table
                ws.Cell(1, 1).InsertTable(results);

                // Autofit
                ws.Columns().AdjustToContents();

                // Save
                workbook.SaveAs(reportFile);
            }

            Console.WriteLine($"Excel Report saved: {reportFile}");
        }
    }

    // Config model
    public class StateConfig
    {
        public string State { get; set; }
        public string Path { get; set; }
        public string Frequency { get; set; }
    }

    // Processing result model
    public class FileProcessResult
    {
        public string FileName { get; set; }
        public string State { get; set; }
        public int Records { get; set; }
        public string ProcessingTime { get; set; }
        public string Status { get; set; }
        public string AIPredictedTime { get; set; }
        public string AIAnomaly { get; set; }
        public string AIConfidence { get; set; }
    }

    // Stub for HIPAA Validator
    public static class HipaaValidator
    {
        public static bool Validate(string filePath)
        {
            // Replace this with your actual validation & API call logic
            // Simulated as 80% success rate
            return new Random().NextDouble() > 0.2;
        }
    }
}
