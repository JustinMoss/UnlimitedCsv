using System.Data;
using FileHelpers;

namespace CsvCompare.Library
{
    public class CsvFile
    {
        public static DataTable ReadFromFile(string file)
        {
            return CsvEngine.CsvToDataTable(file, ',');
        }

        public static void WriteToFile(string filename, ComparisonResults results)
        {
            CsvEngine.DataTableToCsv(results.Differences, filename);
        }
    }
}
