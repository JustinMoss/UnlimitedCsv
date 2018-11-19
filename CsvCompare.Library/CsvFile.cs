using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CsvCompare.Library
{
    public class CsvFile
    {
        public static Task<DataTable> DataTableFromCsvFileAsync(string file)
        {
            return Task.Factory.StartNew(() => DataTableFromCsvFile(file));
        }

        public static DataTable DataTableFromCsvFile(string file)
        {
            var csv = File.ReadAllText(file);
            return DataTableFromCsvString(csv);
        }

        public static Task<DataTable> DataTableFromCsvStringAsync(string csv)
        {
            return Task.Factory.StartNew(() => DataTableFromCsvFile(csv));
        }

        private static DataTable DataTableFromCsvString(string csv)
        {
            var rows = Regex.Split(csv, "\r\n(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            var headerColumns = Regex.Split(rows[0], ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            var dataTable = new DataTable();

            foreach (var headerColumn in headerColumns)
                dataTable.Columns.Add(UnescapeText(headerColumn));

            for (var i = 1; i < rows.Length; i++)
            {
                var columns = Regex.Split(rows[i], ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

                var dataRow = dataTable.NewRow();
                for (var j = 0; j < columns.Length; j++)
                {
                    dataRow[j] = UnescapeText(columns[j]);
                }
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        public static Task<bool> ComparisonResultsToCsvFileAsync(string filename, ComparisonResults results)
        {
            return Task.Factory.StartNew(() => ComparisonResultsToCsvFile(filename, results));
        }

        public static bool ComparisonResultsToCsvFile(string filename, ComparisonResults results)
        {
            var csv = ComparisonResultsToCsvString(results);

            File.WriteAllText(filename, csv);

            return true;
        }

        public static Task<string> ComparisonResultsToCsvStringAsync(ComparisonResults results)
        {
            return Task.Factory.StartNew(() => ComparisonResultsToCsvString(results));
        }

        public static string ComparisonResultsToCsvString(ComparisonResults results)
        {
            var builder = new StringBuilder();

            builder.AppendLine("Differences");
            BuildDifferences(results, builder);
            builder.AppendLine();

            if (results.OrphanColumns1?.Count > 0)
            {
                builder.AppendLine($@"File 1 Extra Columns:,{EscapeText(string.Join(", ", results.OrphanColumns1.Select(EscapeText)))}");
                builder.AppendLine();
            }

            if (results.OrphanColumns2?.Count > 0)
            {
                builder.AppendLine($@"File 2 Extra Columns:,{EscapeText(string.Join(", ", results.OrphanColumns2.Select(EscapeText)))}");
                builder.AppendLine();
            }

            if (results.OrphanRows1?.Count > 0)
            {
                builder.AppendLine("File 1 Extra Rows:");
                BuildOrphanRows(results.OrphanRows1, builder);
                builder.AppendLine();
            }

            if (results.OrphanRows2?.Count > 0)
            {
                builder.AppendLine("File 2 Extra Rows:");
                BuildOrphanRows(results.OrphanRows2, builder);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void BuildDifferences(ComparisonResults results, StringBuilder builder)
        {
            var columns = results.Differences.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            builder.AppendLine(string.Join(",", columns.Select(EscapeText)));

            foreach (DataRow dataRow in results.Differences.Rows)
                DataRowToString(columns, builder, dataRow);
        }

        private static void BuildOrphanRows(List<DataRow> orphanRows, StringBuilder builder)
        {
            var columns = orphanRows[0].Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            builder.AppendLine(string.Join(",", columns.Select(EscapeText)));

            foreach (var dataRow in orphanRows)
                DataRowToString(columns, builder, dataRow);
        }

        private static void DataRowToString(List<string> columnNames, StringBuilder builder, DataRow dataRow)
        {
            builder.AppendLine(string.Join(",", columnNames.Select(c => EscapeText((string)dataRow[c]))));
        }

        private static string UnescapeText(string value)
        {
            // NOTE: If the delimiters or escape characters are made
            // generic, this might need to be a loop or builder.
            var returnValue = value.Replace("\"\"", "\"");

            return returnValue.StartsWith("\"") && returnValue.EndsWith("\"")
                ? returnValue.Substring(1, returnValue.Length - 2)
                : returnValue;
        }

        private static string EscapeText(string value)
        {
            // NOTE: If the delimiters or escape characters are made
            // generic, this might need to be a loop or builder.
            return (value.Contains(",") || value.Contains(Environment.NewLine))
                ? $@"""{value.Replace("\"", "\"\"")}"""
                : value.Replace("\"", "\"\"");
        }
    }
}
