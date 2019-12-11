using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CsvUtilities
{
    /// <summary>
    /// Provides a set of methods for writing CSV strings and lists to <see cref="StreamWriter"/>.
    /// </summary>
    public class CsvWriter : IDisposable
    {
        private readonly StreamWriter _writer;

        /// <summary>
        /// Gets the character for \n.
        /// </summary>
        public static readonly char BackslashNChar = (char)CsvParser.BackslashNCode;

        /// <summary>
        /// Gets the character for \r.
        /// </summary>
        public static readonly char BackslashRChar = (char)CsvParser.BackslashRCode;

        /// <summary>
        /// Gets the character for escaping.
        /// </summary>
        public static readonly char EscapeChar = (char)CsvParser.EscapeCode;

        /// <summary>
        /// Gets the character for a delimiter.
        /// </summary>
        public static readonly char DelimiterChar = (char)CsvParser.DelimiterCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvWriter"/> class.
        /// </summary>
        /// <param name="fileName">The name of the file to write to.</param>
        public CsvWriter(string fileName)
        {
            _writer = new StreamWriter(fileName);
        }

        /// <summary>
        /// Writes a row of CSV strings to this instances file, with the <see cref="DelimiterChar"/> as a seperator.
        /// </summary>
        /// <param name="row">The strings to write to the file.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public async Task WriteRow(IEnumerable<string> row)
        {
            await WriteRowToWriter(row, _writer);
        }

        /// <summary>
        /// Writes a row of CSV strings to the given <see cref="StreamWriter"/>, with the <see cref="DelimiterChar"/> as a seperator.
        /// </summary>
        /// <param name="row">The strings to write to the writer.</param>
        /// <param name="writer">The writer to write to.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public static async Task WriteRowToWriter(IEnumerable<string> row, StreamWriter writer)
        {
            if (row == null)
                await writer.WriteLineAsync();
            else
                await writer.WriteLineAsync(string.Join($"{DelimiterChar}", row.Select(EscapeCsvElement)));
        }

        /// <summary>
        /// Writes CSV rows to this instances file.
        /// </summary>
        /// <param name="rows">The rows to write to the file.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public async Task WriteEnumerable(IEnumerable<IEnumerable<string>> rows)
        {
            await WriteEnumerableToWriter(rows, _writer);
        }

        /// <summary>
        /// Writes CSV rows to the given <see cref="StreamWriter"/>.
        /// </summary>
        /// <param name="rows">The rows to write to the writer.</param>
        /// <param name="writer">The writer to write to.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public static async Task WriteEnumerableToWriter(IEnumerable<IEnumerable<string>> rows, StreamWriter writer)
        {
            foreach (var row in rows)
                if (row == null)
                    await writer.WriteLineAsync();
                else
                    await writer.WriteLineAsync(string.Join($"{DelimiterChar}", row.Select(EscapeCsvElement)));
        }

        /// <summary>
        /// Copies a file to a writer.
        /// </summary>
        /// <param name="fileName">The file to read from.</param>
        /// <param name="writer">The writer to write to.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public static async Task CopyFileToWriter(string fileName, StreamWriter writer)
        {
            using (var comparisonReader = new StreamReader(fileName))
            {
                var read = -1;
                var buffer = new char[4096];
                while (read != 0)
                {
                    read = comparisonReader.Read(buffer, 0, buffer.Length);
                    await writer.WriteAsync(buffer, 0, read);
                }
            }
        }

        private static string EscapeCsvElement(string element)
        {
            if (element == null)
                return null;

            if (element.IndexOfAny(new[] { EscapeChar, BackslashNChar, BackslashRChar, DelimiterChar }) >= 0)
                return EscapeChar + element.Replace("\"", "\"\"") + EscapeChar;

            return element;
        }

        /// <summary>
        /// Disposes the <see cref="StreamWriter"/>.
        /// </summary>
        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}
