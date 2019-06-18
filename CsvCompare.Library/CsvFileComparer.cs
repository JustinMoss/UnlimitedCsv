using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CsvCompare.Library
{
    public class CsvFileComparer
    {
        public static async Task<(List<string> ColumnOrphans1, List<string> ColumnOrphans2)> Compare(
            string file1Name, string file2Name, List<string> identifierColumns, List<string> inclusionColumns, 
            List<string> exclusionColumns, string comparisonTempFile, string rowOrphans1TempFile, string rowOrphans2TempFile,
            bool ignoreCase)
        {
            var stringCompareType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            
            List<string> columnOrphans1;
            List<string> columnOrphans2;
            using (var reader1 = new CsvReader(file1Name))
            using (var reader2 = new CsvReader(file2Name))
            using (var comparisonTempWriter = new CsvWriter(comparisonTempFile))
            using (var rowOrphans1TempWriter = new CsvWriter(rowOrphans1TempFile))
            using (var rowOrphans2TempWriter = new CsvWriter(rowOrphans2TempFile))
            {
                var headers1 = reader1.GetNextRow();
                var headers2 = reader2.GetNextRow();

                var compareColumnIndex = new List<(string Name, int Index1, int Index2)>();
                var compareColumnsSet = new HashSet<string>();
                for (var i = 0; i < headers1.Count; i++)
                {
                    for (var j = 0; j < headers2.Count; j++)
                    {
                        if (!string.Equals(headers1[i], headers2[j], StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!exclusionColumns.Contains(headers1[i]))
                        {
                            compareColumnIndex.Add((headers1[i], i, j));
                            compareColumnsSet.Add(headers1[i]);
                        }
                        break;
                    }
                }

                columnOrphans1 = headers1.Where(h => !compareColumnsSet.Contains(h) && !exclusionColumns.Contains(h)).ToList();
                columnOrphans2 = headers2.Where(h => !compareColumnsSet.Contains(h) && !exclusionColumns.Contains(h)).ToList();

                // Find identifer columns
                var identifierLocations1 = new List<int>();
                for (var i = 0; i < headers1.Count; i++)
                    if (identifierColumns.Contains(headers1[i]))
                        identifierLocations1.Add(i);
                var identifierLocations2 = new List<int>();
                for (var i = 0; i < headers2.Count; i++)
                    if (identifierColumns.Contains(headers2[i]))
                        identifierLocations2.Add(i);

                // Find inclusion columns
                var inclusionColumnLocations = new List<int>();
                for (var i = 0; i < headers1.Count; i++)
                    if (inclusionColumns.Contains(headers1[i]))
                        inclusionColumnLocations.Add(i);

                // Set up differences columns
                var differencesColumns = identifierColumns.Select(i => i).ToList();
                foreach (var inclusionColumn in inclusionColumns)
                    differencesColumns.Add(inclusionColumn);
                differencesColumns.Add("Column Name");
                differencesColumns.Add("Value 1");
                differencesColumns.Add("Value 2");

                await comparisonTempWriter.WriteRow(differencesColumns);
                await rowOrphans1TempWriter.WriteRow(headers1);
                await rowOrphans2TempWriter.WriteRow(headers2);

                // Begin Comparison Loop
                var row1 = reader1.GetNextRow();
                var row2 = reader2.GetNextRow();

                while (row1 != null && row2 != null)
                {
                    var key1 = string.Concat(identifierLocations1.Select(i => row1[i]));
                    var key2 = string.Concat(identifierLocations2.Select(i => row2[i]));
                    var keyCompare = string.Compare(key1, key2, stringCompareType);
                    if (keyCompare == 0)
                    {
                        // Matched row, time to compare
                        // Loop to find difference
                        foreach (var (name, index1, index2) in compareColumnIndex)
                        {
                            if (string.Equals(row1[index1], row2[index2], stringCompareType))
                                continue;

                            // Output those to differences
                            var differencesRow = identifierLocations1.Select(i => row1[i]).ToList();
                            foreach (var inclusionColumnLocation in inclusionColumnLocations)
                                differencesRow.Add(row1[inclusionColumnLocation]);
                            differencesRow.Add(name);
                            differencesRow.Add(row1[index1]);
                            differencesRow.Add(row2[index2]);

                            await comparisonTempWriter.WriteRow(differencesRow);
                        }

                        row1 = reader1.GetNextRow();
                        row2 = reader2.GetNextRow();
                    }
                    else if (keyCompare < 0)
                    {
                        // Output to extra rows file for file 1
                        await rowOrphans1TempWriter.WriteRow(row1);
                        // No match, key 1 is lower, increment file 1
                        row1 = reader1.GetNextRow();
                    }
                    else
                    {
                        // Output to extra rows file for file 2
                        await rowOrphans2TempWriter.WriteRow(row2);
                        // No match, key 2 is lower, increment file 2
                        row2 = reader2.GetNextRow();
                    }
                }
            }

            return (columnOrphans1, columnOrphans2);
        }
    }
}
