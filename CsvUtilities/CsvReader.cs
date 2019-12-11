using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace CsvUtilities
{
    /// <summary>
    /// Provides a set of methods for reading CSV <see cref="CsvParser.Token"/> from files or strings.
    /// </summary>
    public class CsvReader : IDisposable
    {
        private readonly StreamReader _reader;
        private readonly IEnumerator<CsvParser.Token> _tokensEnumerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvReader"/> class.
        /// </summary>
        /// <param name="fileName">The name of the files to read the CSV <see cref="CsvParser.Token"/> from.</param>
        public CsvReader(string fileName)
        {
            _reader = new StreamReader(fileName);
            _tokensEnumerator = CsvParser.Parse(_reader).GetEnumerator();
        }

        /// <summary>
        /// Gets the new row of CSV value strings from the token list.
        /// </summary>
        /// <returns>A list of CSV value strings.</returns>
        public IList<string> GetNextRow()
        {
            return CsvParser.GetNextRow(_tokensEnumerator);
        }

        /// <summary>
        /// Reads the first row from a CSV string as column names.
        /// </summary>
        /// <param name="csv">The CSV string to read from.</param>
        /// <returns>An awaitable <see cref="Task"/> which results in a list of CSV header strings.</returns>
        public static Task<IList<string>> ReadColumnsNamesFromStringAsync(string csv)
            => Task.Run(() => ReadColumnsNamesFromString(csv));

        /// <summary>
        /// Reads the first row from a CSV string as column names.
        /// </summary>
        /// <param name="csv">The CSV string to read from.</param>
        /// <returns>A list of CSV header strings.</returns>
        public static IList<string> ReadColumnsNamesFromString(string csv)
        {
            using (var reader = new StringReader(csv))
            {
                var tokens = CsvParser.Parse(reader);
                return CsvParser.GetNextRow(tokens.GetEnumerator());
            }
        }

        /// <summary>
        /// Parses a csv string into a <see cref="DataTable"/>.
        /// </summary>
        /// <param name="csv">The CSV string to parse.</param>
        /// <returns>An awaitable <see cref="Task"/> that results in the parsed <see cref="DataTable"/>.</returns>
        public static Task<DataTable> ReadDataTableFromStringAsync(string csv)
            => Task.Run(() => ReadDataTableFromString(csv));

        /// <summary>
        /// Parses a CSV string into a <see cref="DataTable"/>.
        /// </summary>
        /// <param name="csv">The CSV string to parse.</param>
        /// <returns>The parsed <see cref="DataTable"/>.</returns>
        public static DataTable ReadDataTableFromString(string csv)
        {
            using (var reader = new StringReader(csv))
                return CsvParser.CreateDataTable(reader);
        }

        /// <summary>
        /// Reads the first row from a CSV file as column names.
        /// </summary>
        /// <param name="file">The CSV file to read from.</param>
        /// <returns>An awaitable <see cref="Task"/> which results in a list of CSV header strings.</returns>
        public static Task<IList<string>> ReadColumnsNamesFromFileAsync(string file)
            => Task.Run(() => ReadColumnsNamesFromFile(file));

        /// <summary>
        /// Reads the first row from a CSV file as column names.
        /// </summary>
        /// <param name="file">The CSV file to read from.</param>
        /// <returns>A list of CSV header strings.</returns>
        public static IList<string> ReadColumnsNamesFromFile(string file)
        {
            using (var reader = new StreamReader(file))
            {
                var tokens = CsvParser.Parse(reader);
                return CsvParser.GetNextRow(tokens.GetEnumerator());
            }
        }

        /// <summary>
        /// Parses a CSV file into a <see cref="DataTable"/>.
        /// </summary>
        /// <param name="file">The CSV file to parse.</param>
        /// <returns>An awaitable <see cref="Task"/> that results in the parsed <see cref="DataTable"/>.</returns>
        public static Task<DataTable> ReadDataTableFromFileAsync(string file)
            => Task.Run(() => ReadDataTableFromFile(file));

        /// <summary>
        /// Parses a CSV file into a <see cref="DataTable"/>.
        /// </summary>
        /// <param name="file">The CSV file to parse.</param>
        /// <returns>The parsed <see cref="DataTable"/>.</returns>
        public static DataTable ReadDataTableFromFile(string file)
        {
            using (var reader = new StreamReader(file))
                return CsvParser.CreateDataTable(reader);
        }

        /// <summary>
        /// Disposes the <see cref="StreamReader"/>.
        /// </summary>
        public void Dispose()
        {
            _reader?.Dispose();
        }
    }
}
