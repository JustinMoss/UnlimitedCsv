using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CsvCompare.Library
{
    public class CsvWriter : IDisposable
    {
        private readonly StreamWriter _writer;

        public static readonly char BackslashNChar = (char)CsvParser.BackslashNCode;
        public static readonly char BackslashRChar = (char)CsvParser.BackslashRCode;
        public static readonly char EscapeChar = (char)CsvParser.EscapeCode;
        public static readonly char DelimiterChar = (char)CsvParser.DelimiterCode;

        public CsvWriter(string fileName)
        {
            _writer = new StreamWriter(fileName);
        }

        public async Task WriteRow(IEnumerable<string> row)
        {
            await WriteRowToWriter(row, _writer);
        }

        public static async Task WriteRowToWriter(IEnumerable<string> row, StreamWriter writer)
        {
            if (row == null)
                await writer.WriteLineAsync();
            else
                await writer.WriteLineAsync(string.Join(",", row.Select(EscapeCsvElement)));
        }

        public async Task WriteEnumerable(IEnumerable<IEnumerable<string>> rows)
        {
            await WriteEnumerableToWriter(rows, _writer);
        }

        public static async Task WriteEnumerableToWriter(IEnumerable<IEnumerable<string>> rows, StreamWriter writer)
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

            if (element.IndexOfAny(new[] { EscapeChar, BackslashNChar, BackslashRChar, DelimiterChar }) >= 0)
                return EscapeChar + element.Replace("\"", "\"\"") + EscapeChar;

            return element;
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}
