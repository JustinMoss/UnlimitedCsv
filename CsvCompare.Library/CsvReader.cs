using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace CsvCompare.Library
{
    public class CsvReader
    {
        public static Task<IList<string>> ReadColumnsNamesFromStringAsync(string csv)
            => Task.Run(() => ReadColumnsNamesFromString(csv));

        public static IList<string> ReadColumnsNamesFromString(string csv)
        {
            using (var reader = new StringReader(csv))
            {
                var tokens = CsvParser.Parse(reader);
                return CsvParser.GetNextRow(tokens.GetEnumerator());
            }
        }

        public static Task<DataTable> ReadDataTableFromStringAsync(string csv)
            => Task.Run(() => ReadDataTableFromString(csv));

        public static DataTable ReadDataTableFromString(string csv)
        {
            using (var reader = new StringReader(csv))
                return CsvParser.CreateDataTable(reader);
        }

        public static Task<IList<string>> ReadColumnsNamesFromFileAsync(string file)
            => Task.Run(() => ReadColumnsNamesFromFile(file));

        public static IList<string> ReadColumnsNamesFromFile(string file)
        {
            using (var reader = new StreamReader(file))
            {
                var tokens = CsvParser.Parse(reader);
                return CsvParser.GetNextRow(tokens.GetEnumerator());
            }
        }

        public static Task<DataTable> ReadDataTableFromFileAsync(string file)
            => Task.Run(() => ReadDataTableFromFile(file));

        public static DataTable ReadDataTableFromFile(string file)
        {
            using (var reader = new StreamReader(file))
                return CsvParser.CreateDataTable(reader);
        }
    }
}
