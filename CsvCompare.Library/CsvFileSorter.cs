using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvCompare.Library
{
    public class CsvFileSorter
    {
        public static async Task ExternalMergeSort(string fileName, int maxFileSize, List<string> identifierColumns, string fileSortedName, string tempFolder)
        {
            // Split files to max size  files
            var (longestRow, splitFiles) = SplitFiles(fileName, maxFileSize, tempFolder);

            // Sort each file
            var sortedFiles = new List<string>();
            foreach (var splitFile in splitFiles)
            {
                var sortFileName = $"{splitFile}_sorted";
                await SortFileInMemory(splitFile, identifierColumns, sortFileName);
                sortedFiles.Add(sortFileName);
            }

            // Merge files
            MergeFiles(fileName, maxFileSize, splitFiles, longestRow, identifierColumns, fileSortedName);

            // Clean up
            for (var i = 0; i < splitFiles.Count; i++)
            {
                File.Delete(sortedFiles[i]);
                File.Delete(splitFiles[i]);
            }
        }

        public static async Task SortFileInMemory(string fileName, List<string> identifierColumns, string fileSortedName)
        {
            using (var reader = new StreamReader(fileName))
            {
                var headerRow = await reader.ReadLineAsync();
                var headerTokens = CsvParser.Parse(headerRow + Environment.NewLine);
                var headers = CsvParser.GetNextRow(headerTokens.GetEnumerator());

                var identifierLocations = new List<int>();
                for (var i = 0; i < headers.Count; i++)
                    if (identifierColumns.Contains(headers[i]))
                        identifierLocations.Add(i);

                string rowText;
                var row = new string[headers.Count];
                var sorted = new SortedDictionary<string, string>();
                while ((rowText = reader.ReadLine()) != null)
                {
                    var parsed = CsvParser.Parse(rowText + Environment.NewLine);
                    CsvParser.FillNextRow(parsed.GetEnumerator(), row);

                    var key = string.Concat(identifierLocations.Select(i => row[i]));
                    sorted.Add(key, rowText);
                }

                using (var writer = new StreamWriter(fileSortedName))
                {
                    await writer.WriteLineAsync(headerRow);
                    foreach (var value in sorted.Values)
                        await writer.WriteLineAsync(value);
                }
            }
        }

        private static (int, List<string>) SplitFiles(string fileName, int maxFileSize, string tempFolder)
        {
            var splitCount = 0;
            var longestRow = 0;
            string splitFileName = null;
            var files = new List<string>();
            using (var reader = new StreamReader(fileName))
            {
                var headers = reader.ReadLine();
                var builder = new StringBuilder();

                while (true)
                {
                    var line = reader.ReadLine();

                    if (line == null)
                    {
                        if (builder.Length > 0)
                        {
                            File.WriteAllText(splitFileName, builder.ToString());
                            builder.Clear();
                        }
                        break;
                    }

                    if (line.Length > longestRow)
                        longestRow = line.Length;

                    if (builder.Length == 0)
                    {
                        builder.AppendLine(headers);
                        splitCount++;
                        splitFileName = Path.Combine(tempFolder, $"split_file_{splitCount}");
                        files.Add(splitFileName);
                    }

                    builder.AppendLine(line);

                    if (builder.Length >= maxFileSize)
                    {
                        File.WriteAllText(splitFileName, builder.ToString());
                        builder.Clear();
                    }
                }
            }

            return (longestRow, files);
        }

        private static void MergeFiles(string fileName, int maxFileSize, List<string> splitFiles, int longestRow, List<string> identifierColumns, string fileSortedName)
        {
            var filesCount = splitFiles.Count;

            var recordsPerQueue = maxFileSize / longestRow / filesCount; // This should be based on some logic.  Maybe - Max Memory / Num Queues / Max Record Size
            if (recordsPerQueue < 1)
                recordsPerQueue = 1;

            // Open files and skip first line headers
            var readers = new StreamReader[filesCount];
            for (var i = 0; i < filesCount; i++)
            {
                readers[i] = new StreamReader(splitFiles[i]);
                readers[i].ReadLine();
            }

            // Create Queues
            var recordQueues = new Queue<(string Key, string Value)>[filesCount];
            for (var i = 0; i < filesCount; i++)
                recordQueues[i] = new Queue<(string, string)>();

            var identifierLocations = new List<int>();
            string headerRow;
            using (var headerReader = new StreamReader(fileName))
            {
                headerRow = headerReader.ReadLine();
                var headerTokens = CsvParser.Parse(headerRow + Environment.NewLine);
                var headers = CsvParser.GetNextRow(headerTokens.GetEnumerator());
                for (var i = 0; i < headers.Count; i++)
                    if (identifierColumns.Contains(headers[i]))
                        identifierLocations.Add(i);
            }

            // Fill queues
            for (var i = 0; i < filesCount; i++)
                FillQueue(readers[i], recordQueues[i], recordsPerQueue, identifierLocations);

            // Begin Merging
            using (var writer = new StreamWriter(fileSortedName))
            {
                writer.WriteLine(headerRow);
                while (true)
                {
                    var lowestIndex = -1;
                    (string Key, string Value) lowestValue = (null, null);

                    // Find next lowest value
                    for (var i = 0; i < filesCount; i++)
                    {
                        var queue = recordQueues[i];
                        if (queue == null)
                            continue;

                        var item = queue.Peek();
                        if (lowestIndex == -1 || string.CompareOrdinal(item.Key, lowestValue.Key) < 0)
                        {
                            lowestIndex = i;
                            lowestValue = item;
                        }
                    }

                    // Check if all queues are empty
                    if (lowestIndex == -1)
                        break;

                    // Found lowest, write and dequeue
                    writer.WriteLine(lowestValue.Value);
                    recordQueues[lowestIndex].Dequeue();

                    // If queue emptied, try to rehydrate
                    if (recordQueues[lowestIndex].Count == 0)
                    {
                        FillQueue(readers[lowestIndex], recordQueues[lowestIndex], recordsPerQueue, identifierLocations);
                        // If still empty, remove the queue
                        if (recordQueues[lowestIndex].Count == 0)
                            recordQueues[lowestIndex] = null;
                    }
                }
            }

            for (var i = 0; i < filesCount; i++)
                readers[i].Close();
        }

        private static void FillQueue(StreamReader reader, Queue<(string Key, string Value)> queue, int recordsPerQueue, List<int> identifierLocations)
        {
            for (var j = 0; j < recordsPerQueue; j++)
            {
                if (reader.Peek() < 0)
                    break;
                var line = reader.ReadLine();
                var parsed = CsvParser.Parse(line);
                var row = CsvParser.GetNextRow(parsed.GetEnumerator()); // Test speed diff with parsing entire thing instead of line by line
                var key = string.Concat(identifierLocations.Select(k => row[k]));
                queue.Enqueue((key, line));
            }
        }
    }
}
