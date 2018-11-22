using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace CsvCompare.Library
{
    public class CsvReader
    {
        public static Task<DataTable> ReadStringToDataTableAsync(string csv)
            => Task.Run(() => ReadStringToDataTable(csv));

        public static DataTable ReadStringToDataTable(string csv)
        {
            using (var reader = new StringReader(csv))
                return CsvParser.CreateDataTableFromReader(reader);
        }

        public static Task<DataTable> ReadFileToDataTableAsync(string file)
            => Task.Run(() => ReadFileToDataTable(file));

        public static DataTable ReadFileToDataTable(string file)
        {
            using (var reader = new StreamReader(file))
                return CsvParser.CreateDataTableFromReader(reader);
        }
    }
}
