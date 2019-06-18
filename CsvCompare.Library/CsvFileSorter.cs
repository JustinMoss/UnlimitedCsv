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
            using (var reader = new CsvReader(fileName))
            {
                // Determine identifier column locations
                var headers = reader.GetNextRow();

                var identifierLocations = new List<int>();
                for (var i = 0; i < headers.Count; i++)
                    if (identifierColumns.Contains(headers[i]))
                        identifierLocations.Add(i);

                var sorted = new SortedDictionary<string, IList<string>>();
                while (true)
                {
                    var row = reader.GetNextRow();
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
