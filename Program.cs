using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FileProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== File Processor with ML.NET ===");

            var config = ConfigManager.LoadConfig("config.json");

            foreach (var state in config.States)
            {
                Console.WriteLine($"Processing state: {state.Name} ({state.Frequency})");

                foreach (var dir in state.Directories)
                {
                    if (!Directory.Exists(dir))
                    {
                        Console.WriteLine($"Directory not found: {dir}");
                        continue;
                    }

                    // Setup working/report/archive folders under the source dir
                    string workingPath = Path.Combine(dir, "Working");
                    string reportPath = Path.Combine(dir, "Report");
                    string archivePath = Path.Combine(dir, "Archive");

                    Directory.CreateDirectory(workingPath);
                    Directory.CreateDirectory(reportPath);
                    Directory.CreateDirectory(archivePath);

                    // File types to look for
                    string[] patterns = new[] { "*.edi", "*.p", "*.txt" };

                    var files = patterns.SelectMany(p => Directory.GetFiles(dir, p)).ToList();

                    if (files.Count == 0)
                    {
                        Console.WriteLine($"No files found in {dir}");
                        continue;
                    }

                    Console.WriteLine($"{files.Count} files found in {dir}");

                    // Run ML.NET anomaly detection
                    RunMLAnomalyDetection(files, reportPath);

                    foreach (var file in files)
                    {
                        try
                        {
                            string fileName = Path.GetFileName(file);
                            string destFile = Path.Combine(workingPath, fileName);

                            File.Copy(file, destFile, true);

                            Console.WriteLine($"Copied {fileName} → Working folder");

                            // Simulate validation / processing
                            // TODO: integrate actual HIPS validator here
                            string reportFile = Path.Combine(reportPath, fileName + ".report.txt");
                            File.WriteAllText(reportFile, $"Processed {fileName} at {DateTime.Now}");

                            Console.WriteLine($"Report generated: {reportFile}");

                            // Move to Archive
                            string archivedFile = Path.Combine(archivePath, fileName);
                            File.Move(file, archivedFile);

                            Console.WriteLine($"Archived {fileName}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing {file}: {ex.Message}");
                        }
                    }
                }
            }

            Console.WriteLine("=== Processing Complete ===");
            Console.ReadLine();
        }

        /// <summary>
        /// Use ML.NET anomaly detection to detect unusual file sizes
        /// </summary>
        static void RunMLAnomalyDetection(List<string> files, string reportPath)
        {
            var mlContext = new MLContext();

            var data = files.Select(f => new FileRecord
            {
                FileName = Path.GetFileName(f),
                Size = new FileInfo(f).Length
            });

            IDataView dataView = mlContext.Data.LoadFromEnumerable(data);

            var pipeline = mlContext.Transforms.DetectIidSpike(
                outputColumnName: nameof(FilePrediction.Prediction),
                inputColumnName: nameof(FileRecord.Size),
                confidence: 95,
                pvalueHistoryLength: 10);

            var model = pipeline.Fit(dataView);
            var transformed = model.Transform(dataView);

            var predictions = mlContext.Data.CreateEnumerable<FilePrediction>(transformed, reuseRowObject: false).ToList();

            string anomalyReport = Path.Combine(reportPath, $"AnomalyReport_{DateTime.Now:yyyyMMddHHmmss}.txt");
            using (var sw = new StreamWriter(anomalyReport))
            {
                sw.WriteLine("=== Anomaly Detection Report ===");
                sw.WriteLine($"Generated: {DateTime.Now}");
                sw.WriteLine("--------------------------------");
                int i = 0;
                foreach (var record in data.Zip(predictions, (d, p) => new { d, p }))
                {
                    string status = record.p.Prediction[0] == 1 ? "ANOMALY" : "Normal";
                    sw.WriteLine($"{record.d.FileName,-30} | Size: {record.d.Size,10} | {status}");
                    i++;
                }
            }

            Console.WriteLine($"ML.NET anomaly report saved → {anomalyReport}");
        }
    }

    public class FileRecord
    {
        public string FileName { get; set; }
        public float Size { get; set; }
    }

    public class FilePrediction
    {
        [VectorType(3)]
        public double[] Prediction { get; set; }
    }
}
