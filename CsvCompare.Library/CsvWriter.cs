using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvCompare.Library
{
    public class CsvWriter
    {
        public const char BackslashNChar = (char)10;
        public const char BackslashRChar = (char)13;
        public const char QuoteChar = (char)34;
        public const char CommaChar = (char)44;

        public static async Task WriteComparisonResultsToFileAsync(string filename, ComparisonResults results)
        {
            var rows = CsvParser.BuildResultsEnumerable(results);

            using (var writer = new StreamWriter(filename))
                await WriteEnumerableToWriter(rows, writer);
        }

        public static async Task<string> WriteComparisonResultsToStringAsync(ComparisonResults results)
        {
            var rows = CsvParser.BuildResultsEnumerable(results);

            var builder = new StringBuilder();
            using (var writer = new StringWriter(builder))
                await WriteEnumerableToWriter(rows, writer);

            return builder.ToString();
        }

        private static async Task WriteEnumerableToWriter(IEnumerable<IEnumerable<string>> rows, TextWriter writer)
        {
            foreach (var row in rows)
                if (row == null)
                    await writer.WriteLineAsync();
                else
                    await writer.WriteLineAsync(string.Join(",", row.Select(EscapeCsvElement)));
        }

        private static string EscapeCsvElement(string element)
        {
            if (element == null)
                return null;

            if (element.Contains(QuoteChar) || element.Contains(BackslashRChar) ||
                element.Contains(BackslashNChar) || element.Contains(CommaChar))
                return QuoteChar + element.Replace("\"", "\"\"") + QuoteChar;

            return element;
        }
    }
}
