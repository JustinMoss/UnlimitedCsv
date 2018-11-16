using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
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

            string csv = File.ReadAllText(filename);

            var differencesColumnNames = new List<string>();
            foreach (DataColumn column in results.Differences.Columns)
                differencesColumnNames.Add(column.ColumnName.Contains(",") ? $@"""{column.ColumnName}""" : column.ColumnName);

            var builder = new StringBuilder();

            builder.AppendLine("Differences");
            builder.AppendLine(string.Join(",", differencesColumnNames));
            builder.Append(csv);

            builder.AppendLine();

            if (results.OrphanColumns1?.Count > 0)
            {
                builder.AppendLine($@"File 1 Extra Columns:,""{string.Join(", ", results.OrphanColumns1)}""");
                builder.AppendLine();
            }

            if (results.OrphanColumns2?.Count > 0)
            {
                builder.AppendLine($@"File 2 Extra Columns:,""{string.Join(", ", results.OrphanColumns2)}""");
                builder.AppendLine();
            }

            if (results.OrphanRows1?.Count > 0)
            {
                builder.AppendLine("File 1 Extra Rows:");

                var extraRows1ColumnNames = new List<string>();
                foreach (DataColumn column in results.OrphanRows1[0].Table.Columns)
                    extraRows1ColumnNames.Add(column.ColumnName.Contains(",") ? $@"""{column.ColumnName}""" : column.ColumnName);

                builder.AppendLine(string.Join(",", extraRows1ColumnNames));

                foreach (var dataRow in results.OrphanRows1)
                {
                    var rowValues = new List<string>();
                    foreach (var extraRows1ColumnName in extraRows1ColumnNames)
                    {
                        rowValues.Add((string)dataRow[extraRows1ColumnName]);
                    }
                    builder.AppendLine(string.Join(",", rowValues));
                }

                builder.AppendLine();
            }

            if (results.OrphanRows2?.Count > 0)
            {
                builder.AppendLine("File 2 Extra Rows:");

                var extraRows2ColumnNames = new List<string>();
                foreach (DataColumn column in results.OrphanRows2[0].Table.Columns)
                    extraRows2ColumnNames.Add(column.ColumnName.Contains(",") ? $@"""{column.ColumnName}""" : column.ColumnName);

                builder.AppendLine(string.Join(",", extraRows2ColumnNames));

                foreach (var dataRow in results.OrphanRows2)
                {
                    var rowValues = new List<string>();
                    foreach (var extraRows2ColumnName in extraRows2ColumnNames)
                    {
                        rowValues.Add((string)dataRow[extraRows2ColumnName]);
                    }
                    builder.AppendLine(string.Join(",", rowValues));
                }

                builder.AppendLine();
            }

            File.WriteAllText(filename, builder.ToString());
        }
    }
}
