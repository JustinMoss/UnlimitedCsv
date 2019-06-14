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
                // Determine identifier column locations
                var parsedTokens = CsvParser.Parse(reader);
                var enumerator = parsedTokens.GetEnumerator();

                var headers = CsvParser.GetNextRow(enumerator);

                var identifierLocations = new List<int>();
                for (var i = 0; i < headers.Count; i++)
                    if (identifierColumns.Contains(headers[i]))
                        identifierLocations.Add(i);

                var sorted = new SortedDictionary<string, IList<string>>();
                while (true)
                {
                    var row = CsvParser.GetNextRow(enumerator);
                    if (row == null) break;

                    var key = string.Concat(identifierLocations.Select(i => row[i]));
                    sorted.Add(key, row);
                }

                var output = sorted.ToList().Select(kv => kv.Value).ToList();
                output.Insert(0, headers);

                using (var writer = new StreamWriter(fileSortedName))
                    await CsvWriter.WriteEnumerableToWriter(output, writer);
            }
        }
    }
}
