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

        public CsvWriter(string fileName)
        {
            _writer = new StreamWriter(fileName);
        }

        public const char BackslashNChar = (char)10;
        public const char BackslashRChar = (char)13;
        public const char QuoteChar = (char)34;
        public const char CommaChar = (char)44;
        
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

            if (element.Contains(QuoteChar) || element.Contains(BackslashRChar) ||
                element.Contains(BackslashNChar) || element.Contains(CommaChar))
                return QuoteChar + element.Replace("\"", "\"\"") + QuoteChar;

            return element;
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}
