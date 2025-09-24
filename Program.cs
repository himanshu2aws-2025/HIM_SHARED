using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Drawing;

namespace FileProcessorWithAI
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== File Processor with ML.NET Demo ===");

            string[] sourceDirectories = new string[]
            {
                @"C:\NAS\State1",
                @"C:\NAS\State2"
            };

            string[] allowedExtensions = { ".edi", ".p", ".txt" };

            foreach (var dir in sourceDirectories)
            {
                if (!Directory.Exists(dir))
                {
                    Console.WriteLine($"⚠️ Directory not found: {dir}");
                    continue;
                }

                Console.WriteLine($"\n🔍 Scanning directory: {dir}");

                var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                                     .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLower()));

                foreach (var file in files)
                {
                    Console.WriteLine($"📂 Found file: {Path.GetFileName(file)}");

                    // Step 1: Copy file to Working folder
                    string workingFolder = Path.Combine(dir, "Working");
                    Directory.CreateDirectory(workingFolder);
                    string destFile = Path.Combine(workingFolder, Path.GetFileName(file));
                    File.Copy(file, destFile, true);

                    // Step 2: Run ML.NET anomaly detection
                    bool isAnomaly = RunMLAnomalyDetection(file);

                    // Step 3: Save AI result into Excel report (with daily summary chart & highlighting)
                    string reportFolder = Path.Combine(dir, "Reports");
                    Directory.CreateDirectory(reportFolder);
                    string reportPath = Path.Combine(reportFolder, "AI_Report.xlsx");

                    SavePredictionToExcel(reportPath, file, isAnomaly);

                    // Step 4: Move original file to Archive
                    string archiveFolder = Path.Combine(dir, "Archive");
                    Directory.CreateDirectory(archiveFolder);
                    string archivedFile = Path.Combine(archiveFolder, Path.GetFileName(file));
                    File.Move(file, archivedFile, true);

                    Console.WriteLine($"📦 Archived: {archivedFile}");
                }
            }

            Console.WriteLine("\n=== Processing Completed ===");
        }

        // ML.NET Input class
        public class FileData { public float FileSize { get; set; } }

        // ML.NET Output class
        public class Prediction { [VectorType(3)] public double[] Prediction { get; set; } }

        static bool RunMLAnomalyDetection(string filePath)
        {
            var mlContext = new MLContext();

            long fileSize = new FileInfo(filePath).Length;
            var data = new List<FileData>();
            for (int i = 0; i < 20; i++)
            {
                data.Add(new FileData { FileSize = (float)(fileSize * (i < 18 ? 1 : 1.5)) });
            }

            var dataView = mlContext.Data.LoadFromEnumerable(data);
            var pipeline = mlContext.Transforms.DetectIidSpike(
                outputColumnName: nameof(Prediction.Prediction),
                inputColumnName: nameof(FileData.FileSize),
                confidence: 95,
                pvalueHistoryLength: 8);

            var model = pipeline.Fit(dataView);
            var transformed = model.Transform(dataView);
            var predictions = mlContext.Data.CreateEnumerable<Prediction>(transformed, reuseRowObject: false).ToList();
            var last = predictions.Last().Prediction;

            return last[0] == 1; // Spike detected
        }

        static void SavePredictionToExcel(string reportPath, string filePath, bool isAnomaly)
        {
            FileInfo reportFile = new FileInfo(reportPath);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (ExcelPackage package = new ExcelPackage(reportFile))
            {
                var ws = package.Workbook.Worksheets.FirstOrDefault() ?? package.Workbook.Worksheets.Add("AI_Report");

                int row = ws.Dimension?.End.Row + 1 ?? 1;
                if (row == 1)
                {
                    ws.Cells[row, 1].Value = "Timestamp";
                    ws.Cells[row, 2].Value = "FileName";
                    ws.Cells[row, 3].Value = "IsAnomaly";
                    row++;
                }

                ws.Cells[row, 1].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ws.Cells[row, 2].Value = Path.GetFileName(filePath);
                ws.Cells[row, 3].Value = isAnomaly ? 1 : 0;

                // Highlight anomaly row in red
                if (isAnomaly)
                {
                    using (var rng = ws.Cells[row, 1, row, 3])
                    {
                        rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        rng.Style.Fill.BackgroundColor.SetColor(Color.LightPink);
                        rng.Style.Font.Color.SetColor(Color.DarkRed);
                        rng.Style.Font.Bold = true;
                    }
                }

                // Daily summary sheet
                var wsSummary = package.Workbook.Worksheets.FirstOrDefault(s => s.Name == "DailySummary")
                                ?? package.Workbook.Worksheets.Add("DailySummary");

                var grouped = ws.Cells[2, 1, ws.Dimension.End.Row, 3]
                                .GroupBy(c => DateTime.Parse(c.Worksheet.Cells[c.Start.Row, 1].Text).Date)
                                .Select(g => new
                                {
                                    Date = g.Key,
                                    AnomalyCount = g.Count(c => c.Worksheet.Cells[c.Start.Row, 3].Text == "1")
                                })
                                .OrderBy(x => x.Date)
                                .ToList();

                wsSummary.Cells.Clear();
                wsSummary.Cells[1, 1].Value = "Date";
                wsSummary.Cells[1, 2].Value = "AnomalyCount";

                int sRow = 2;
                foreach (var g in grouped)
                {
                    wsSummary.Cells[sRow, 1].Value = g.Date.ToString("yyyy-MM-dd");
                    wsSummary.Cells[sRow, 2].Value = g.AnomalyCount;

                    // Highlight days with anomalies
                    if (g.AnomalyCount > 0)
                    {
                        using (var rng = wsSummary.Cells[sRow, 1, sRow, 2])
                        {
                            rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            rng.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                            rng.Style.Font.Color.SetColor(Color.DarkOrange);
                            rng.Style.Font.Bold = true;
                        }
                    }

                    sRow++;
                }

                // Add chart if not already present
                if (!wsSummary.Drawings.Any())
                {
                    var chart = wsSummary.Drawings.AddChart("DailyAnomaliesChart", eChartType.LineMarkers);
                    chart.Title.Text = "Daily Anomaly Trend";
                    chart.SetPosition(1, 0, 3, 0);
                    chart.SetSize(600, 400);

                    var series = chart.Series.Add(wsSummary.Cells[2, 2, sRow - 1, 2], wsSummary.Cells[2, 1, sRow - 1, 1]);
                    series.Header = "Anomalies per Day";
                }

                package.Save();
            }

            Console.WriteLine($"📊 Report updated (with highlighting): {reportPath}");
        }
    }
}
