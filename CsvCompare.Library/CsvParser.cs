using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;

namespace CsvCompare.Library
{
    public class CsvParser
    {
        public static async Task<DataTable> DataTableFromCsvFileAsync(string file)
        {
            using (var reader = new StreamReader(file))
                return DataTableFromStringList(await GetListFromStream(reader));
        }

        private static async Task<DataTable> DataTableFromCsvStringAsync(string csv)
        {
            using (var reader = new StringReader(csv))
                return DataTableFromStringList(await GetListFromStream(reader));
        }

        private static async Task<List<List<string>>> GetListFromStream(TextReader reader)
        {
            var rows = new List<List<string>>();
            var parser = new CsvHelper.CsvParser(reader);
            while (true)
            {
                var row = await parser.ReadAsync();
                if (row == null)
                    break;
                rows.Add(row.ToList());
            }

            return rows;
        }

        public static DataTable DataTableFromStringList(List<List<string>> rows)
        {
            var headerColumns = rows[0];

            var dataTable = new DataTable();
            foreach (var headerColumn in headerColumns)
                dataTable.Columns.Add(headerColumn);

            for (var i = 1; i < rows.Count; i++)
            {
                var columns = rows[i].ToList();

                var dataRow = dataTable.NewRow();
                for (var j = 0; j < columns.Count; j++)
                    dataRow[j] = columns[j];

                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        public static async Task ComparisonResultsToCsvFileAsync(string filename, ComparisonResults results)
        {
            var rows = ComparisonResultsToStringList(results);

            using (var writer = new StreamWriter(filename))
                await SendListToStream(rows, writer);
        }

        public static async Task<string> ComparisonResultsToCsvStringAsync(ComparisonResults results)
        {
            var rows = ComparisonResultsToStringList(results);

            var builder = new StringBuilder();
            using (var writer = new StringWriter(builder))
                await SendListToStream(rows, writer);

            return builder.ToString();
        }

        private static async Task SendListToStream(List<List<string>> rows, TextWriter writer)
        {
            var csvWriter = new CsvWriter(writer);
            foreach (var row in rows)
            {
                foreach (var column in row)
                {
                    csvWriter.WriteField(column);
                }
                await csvWriter.NextRecordAsync();
            }
        }

        public static List<List<string>> ComparisonResultsToStringList(ComparisonResults results)
        {
            var rowValuesList = new List<List<string>>();

            rowValuesList.Add(new List<string> { "Differences" });
            BuildDifferences(results, rowValuesList);
            rowValuesList.Add(new List<string>());

            if (results.OrphanColumns1?.Count > 0)
            {
                rowValuesList.Add(new List<string> { "File 1 Extra Columns:", string.Join(", ", results.OrphanColumns1) });
                rowValuesList.Add(new List<string>());
            }

            if (results.OrphanColumns2?.Count > 0)
            {
                rowValuesList.Add(new List<string> { "File 2 Extra Columns:", string.Join(", ", results.OrphanColumns2) });
                rowValuesList.Add(new List<string>());
            }

            if (results.OrphanRows1?.Count > 0)
            {
                rowValuesList.Add(new List<string> { "File 1 Extra Rows:" });
                BuildOrphanRows(results.OrphanRows1, rowValuesList);
                rowValuesList.Add(new List<string>());
            }

            if (results.OrphanRows2?.Count > 0)
            {
                rowValuesList.Add(new List<string> { "File 2 Extra Rows:" });
                BuildOrphanRows(results.OrphanRows2, rowValuesList);
                rowValuesList.Add(new List<string>());
            }

            return rowValuesList;
        }

        private static void BuildDifferences(ComparisonResults results, List<List<string>> rows)
        {
            var columns = results.Differences.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            rows.Add(columns);

            foreach (DataRow dataRow in results.Differences.Rows)
                rows.Add(dataRow.ItemArray.Cast<string>().ToList());
        }

        private static void BuildOrphanRows(List<DataRow> orphanRows, List<List<string>> rows)
        {
            var columns = orphanRows[0].ItemArray.Cast<string>().ToList();
            rows.Add(columns);

            foreach (var dataRow in orphanRows)
                rows.Add(dataRow.ItemArray.Cast<string>().ToList());
        }
    }
}
