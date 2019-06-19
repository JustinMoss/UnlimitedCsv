using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CsvCompare.Library
{
    public class CsvFileSorter
    {
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
    }
}
