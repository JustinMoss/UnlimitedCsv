using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using FileHelpers;

namespace CsvCompare.Library
{
    public class CsvComparer
    {
        public CsvComparer(string file1, string file2, IEnumerable<string> excludeColumns = null, IEnumerable<string> includeColumns = null)
        {
            DataTable1 = CsvEngine.CsvToDataTable(file1, ',');
            DataTable2 = CsvEngine.CsvToDataTable(file2, ',');

            DataDictionary1 = new Dictionary<string, DataRow>();
            DataDictionary2 = new Dictionary<string, DataRow>();

            ExcludeColumns = excludeColumns?.Select(c => c.Trim()) ?? new List<string>();
            IncludeColumns = includeColumns?.Select(c => c.Trim()) ?? new List<string>();

            foreach (DataRow row in DataTable1.Rows)
                DataDictionary1.Add((string)row[0], row);

            foreach (DataRow row in DataTable2.Rows)
                DataDictionary2.Add((string)row[0], row);
        }

        public CsvComparer(DataTable table1, DataTable table2, IEnumerable<string> excludeColumns = null, IEnumerable<string> includeColumns = null)
        {
            DataTable1 = table1;
            DataTable2 = table2;

            DataDictionary1 = new Dictionary<string, DataRow>();
            DataDictionary2 = new Dictionary<string, DataRow>();

            ExcludeColumns = excludeColumns?.Select(c => c.Trim()) ?? new List<string>();
            IncludeColumns = includeColumns?.Select(c => c.Trim()) ?? new List<string>();

            foreach (DataRow row in DataTable1.Rows)
                DataDictionary1.Add((string)row[0], row);

            foreach (DataRow row in DataTable2.Rows)
                DataDictionary2.Add((string)row[0], row);
        }

        public DataTable DataTable1 { get; }
        public DataTable DataTable2 { get; }

        public Dictionary<string, DataRow> DataDictionary1 { get; }
        public Dictionary<string, DataRow> DataDictionary2 { get; }

        public IEnumerable<string> ExcludeColumns { get; }
        public IEnumerable<string> IncludeColumns { get; }

        public DataTable Compare()
        {
            var existingColumns = new List<string>();
            var orphanColumns1 = new List<string>();
            var orphanColumns2 = new List<string>();
            var orphanRows1 = new List<string>();
            var orphanRows2 = new List<string>();

            // Look for orphaned columns. We can't guarantee one is a subset of the
            // other so we should loop both ways.
            foreach (DataColumn column in DataTable1.Columns)
                if (!DataTable2.Columns.Contains(column.ColumnName))
                    orphanColumns1.Add(column.ColumnName);
                else
                    existingColumns.Add(column.ColumnName);

            foreach (DataColumn column in DataTable2.Columns)
                if (!DataTable1.Columns.Contains(column.ColumnName))
                    orphanColumns2.Add(column.ColumnName);

            var differences = new DataTable();

            differences.Columns.Add(existingColumns[0]);

            foreach (var column in IncludeColumns)
                differences.Columns.Add(column);

            differences.Columns.Add("Column Name");
            differences.Columns.Add("Value 1");
            differences.Columns.Add("Value 2");

            foreach (var key1 in DataDictionary1.Keys)
                if (DataDictionary2.ContainsKey(key1))
                    CompareRows(DataDictionary1[key1], DataDictionary2[key1], differences, existingColumns);
                else
                    orphanRows1.Add(key1);

            foreach (var key2 in DataDictionary2.Keys)
                if (!DataDictionary1.ContainsKey(key2))
                    orphanRows2.Add(key2);

            return differences;
        }

        private void CompareRows(DataRow dataRow1, DataRow dataRow2, DataTable differences, List<string> existingColumns)
        {
            foreach (var column in existingColumns)
            {
                var value1 = (string)dataRow1[column];
                var value2 = (string)dataRow2[column];
                if (value1 == value2)
                    continue;

                var row = differences.NewRow();
                row[existingColumns[0]] = dataRow1[0];

                foreach (var includeColumn in IncludeColumns)
                    row[includeColumn] = dataRow1[includeColumn];

                row["Column Name"] = column;
                row["Value 1"] = dataRow1[column];
                row["Value 2"] = dataRow2[column];

                differences.Rows.Add(row);
            }
        }
    }
}
