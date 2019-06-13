using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using CsvCompare.Library;
using Microsoft.Win32;

namespace CsvCompare
{
    /// <summary>
    /// Interaction logic for ComparisonWindow.xaml
    /// </summary>
    public partial class ComparisonResultsWindow : Window
    {
        public ComparisonResultsWindow(SettingsWindow settingsWindow)
        {
            try
            {
                InitializeComponent();

                ClearErrors();

                SettingsWindow = settingsWindow;
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
                ErrorLabel.Visibility = Visibility.Visible;
            }
        }

        public bool IsClosed { get; set; }
        public ComparisonResults Results { get; set; }
        public SettingsWindow SettingsWindow { get; set; }

        private void StartOver_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                SettingsWindow.Show();

                Hide();
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
                ErrorLabel.Visibility = Visibility.Visible;
            }
        }

        private async void ExportResults_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();
                DisableButtons();

                var saveFileName = GetSaveFileName();

                if (saveFileName == null)
                    return;

                await CsvWriter.WriteComparisonResultsToFileAsync(saveFileName, Results);

                EnableButtons();
            }
            catch (Exception ex)
            {
                EnableButtons();
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
                ErrorLabel.Visibility = Visibility.Visible;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                ClearErrors();

                IsClosed = true;
                if (!SettingsWindow.IsClosed)
                    SettingsWindow.Close();
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
                ErrorLabel.Visibility = Visibility.Visible;
            }
        }

        public async Task SetSettings(string file1Name, string file2Name, List<string> identifierColumns, List<string> exclusionColumns, List<string> inclusionColumns)
        {
            try
            {
                ClearErrors();

                // Check File Length
                var file1Length = new FileInfo(file1Name).Length;
                var file2Length = new FileInfo(file2Name).Length;

                //if (file1Length + file2Length < 100000000)
                //{
                //    // Small files, use old simple method
                //    await CompareSmallFiles(file1Name, file2Name, identifierColumns, exclusionColumns, inclusionColumns);
                //    return;
                //}

                //var file1SortedName = file1Name.Replace(".csv", "_sorted.csv");
                //var file2SortedName = file2Name.Replace(".csv", "_sorted.csv");
                var file1SortedName = file1Name;
                var file2SortedName = file2Name;
                var comparisonTempFile = "_comparison_temp.csv";
                var rowOrphans1TempFile = "_comparison_orphans1_temp.csv";
                var rowOrphans2TempFile = "_comparison_orphans2_temp.csv";
                List<string> columnOrphans1;
                List<string> columnOrphans2;

                // Sort file 1
                //var file1SortStopwatch = new Stopwatch();
                //file1SortStopwatch.Start();
                //var identifierLocations1 = await SortFileInMemory(file1Name, identifierColumns, file1SortedName);
                //file1SortStopwatch.Stop();

                //// Sort file 2
                //var file2SortStopwatch = new Stopwatch();
                //file2SortStopwatch.Start();
                //var identifierLocations2 = await SortFileInMemory(file2Name, identifierColumns, file2SortedName);
                //file2SortStopwatch.Stop();

                var comparisonStopwatch = new Stopwatch();
                comparisonStopwatch.Start();
                // Compare line by line.
                using (var reader1 = new StreamReader(file1SortedName))
                using (var reader2 = new StreamReader(file2SortedName))
                using (var comparisonTempWriter = new StreamWriter(comparisonTempFile))
                using (var rowOrphans1TempWriter = new StreamWriter(rowOrphans1TempFile))
                using (var rowOrphans2TempWriter = new StreamWriter(rowOrphans2TempFile))
                {
                    var reader1Tokens = CsvParser.Parse(reader1);
                    var enumerator1 = reader1Tokens.GetEnumerator();
                    var headers1 = CsvParser.GetNextRow(enumerator1);

                    var reader2Tokens = CsvParser.Parse(reader2);
                    var enumerator2 = reader2Tokens.GetEnumerator();
                    var headers2 = CsvParser.GetNextRow(enumerator2);

                    //TODO: Remove this section when sorting is added back.
                    var identifierLocations1 = new List<int>();
                    for (var i = 0; i < headers1.Count; i++)
                        if (identifierColumns.Contains(headers1[i]))
                            identifierLocations1.Add(i);
                    var identifierLocations2 = new List<int>();
                    for (var i = 0; i < headers2.Count; i++)
                        if (identifierColumns.Contains(headers2[i]))
                            identifierLocations2.Add(i);

                    var commonColumnsIndex = new List<(string Name, int Index1, int Index2)>();
                    var commonColumnsSet = new HashSet<string>();
                    for (var i = 0; i < headers1.Count; i++)
                    {
                        for (var j = 0; j < headers2.Count; j++)
                        {
                            if (string.Equals(headers1[i], headers2[j], StringComparison.OrdinalIgnoreCase))
                            {
                                commonColumnsIndex.Add((headers1[i], i, j));
                                commonColumnsSet.Add(headers1[i]);
                                break;
                            }
                        }
                    }

                    columnOrphans1 = headers1.Where(h => !commonColumnsSet.Contains(h)).ToList();
                    columnOrphans2 = headers2.Where(h => !commonColumnsSet.Contains(h)).ToList();

                    // Set up output columns
                    var differencesColumns = identifierColumns.Select(i => i).ToList();
                    differencesColumns.Add("Column Name");
                    differencesColumns.Add("Value 1");
                    differencesColumns.Add("Value 2");
                    await CsvWriter.WriteRowToWriter(differencesColumns, comparisonTempWriter);
                    await CsvWriter.WriteRowToWriter(headers1, rowOrphans1TempWriter);
                    await CsvWriter.WriteRowToWriter(headers2, rowOrphans2TempWriter);

                    // Begin Comparison Loop
                    var row1 = CsvParser.GetNextRow(enumerator1);
                    var row2 = CsvParser.GetNextRow(enumerator2);

                    while (row1 != null && row2 != null)
                    {
                        var key1 = string.Concat(identifierLocations1.Select(i => row1[i]));
                        var key2 = string.Concat(identifierLocations2.Select(i => row2[i]));
                        var keyCompare = string.CompareOrdinal(key1, key2);
                        if (keyCompare == 0)
                        {
                            // Loop to find difference
                            foreach (var commonColumnIndex in commonColumnsIndex)
                            {
                                if (row1[commonColumnIndex.Index1] != row2[commonColumnIndex.Index2])
                                {
                                    // Output those to differences
                                    var differencesRow = identifierLocations1.Select(i => row1[i]).ToList();
                                    differencesRow.Add(commonColumnIndex.Name);
                                    differencesRow.Add(row1[commonColumnIndex.Index1]);
                                    differencesRow.Add(row2[commonColumnIndex.Index2]);

                                    await CsvWriter.WriteRowToWriter(differencesRow, comparisonTempWriter);
                                }
                            }

                            // Matched row, time to compare
                            row1 = CsvParser.GetNextRow(enumerator1);
                            row2 = CsvParser.GetNextRow(enumerator2);
                        }
                        else if (keyCompare < 0)
                        {
                            // Output to extra rows file for file 1
                            await CsvWriter.WriteRowToWriter(row1, rowOrphans1TempWriter);
                            // No match, key 1 is lower, increment file 1
                            row1 = CsvParser.GetNextRow(enumerator1);
                        }
                        else
                        {
                            // Output to extra rows file for file 2
                            await CsvWriter.WriteRowToWriter(row2, rowOrphans2TempWriter);
                            // No match, key 2 is lower, increment file 2
                            row2 = CsvParser.GetNextRow(enumerator2);
                        }
                    }
                }
                comparisonStopwatch.Stop();

                // Merge output results
                using (var resultsWriter = new StreamWriter("_comparison_results.csv"))
                {
                    // Differences
                    await CsvWriter.WriteRowToWriter(new [] {"Differences: "}, resultsWriter);
                    using (var comparisonReader = new StreamReader(comparisonTempFile))
                    {
                        var read = -1;
                        var buffer = new char[4096];
                        while (read != 0)
                        {
                            read = comparisonReader.Read(buffer, 0, buffer.Length);
                            resultsWriter.Write(buffer, 0, read);
                        }
                    }

                    // Extra Rows 1
                    await CsvWriter.WriteRowToWriter(null, resultsWriter);
                    await CsvWriter.WriteRowToWriter(new[] { "File 1 Extra Rows:" }, resultsWriter);
                    using (var extraRows1Reader = new StreamReader(rowOrphans1TempFile))
                    {
                        var read = -1;
                        var buffer = new char[4096];
                        while (read != 0)
                        {
                            read = extraRows1Reader.Read(buffer, 0, buffer.Length);
                            resultsWriter.Write(buffer, 0, read);
                        }
                    }

                    // Extra Rows 2
                    await CsvWriter.WriteRowToWriter(null, resultsWriter);
                    await CsvWriter.WriteRowToWriter(new[] { "File 2 Extra Rows:" }, resultsWriter);
                    using (var extraRows2Reader = new StreamReader(rowOrphans2TempFile))
                    {
                        var read = -1;
                        var buffer = new char[4096];
                        while (read != 0)
                        {
                            read = extraRows2Reader.Read(buffer, 0, buffer.Length);
                            resultsWriter.Write(buffer, 0, read);
                        }
                    }

                    // Extra Columns 1
                    await CsvWriter.WriteRowToWriter(null, resultsWriter);
                    columnOrphans1.Insert(0, "File 1 Extra Columns:");
                    await CsvWriter.WriteRowToWriter(columnOrphans1, resultsWriter);

                    // Extra Columns 2
                    await CsvWriter.WriteRowToWriter(null, resultsWriter);
                    columnOrphans2.Insert(0, "File 2 Extra Columns:");
                    await CsvWriter.WriteRowToWriter(columnOrphans2, resultsWriter);
                }

                //var file1SortTime = file1SortStopwatch.ElapsedMilliseconds;
                //var file2SortTime = file2SortStopwatch.ElapsedMilliseconds;
                var comparisonTime = comparisonStopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
                ErrorLabel.Visibility = Visibility.Visible;
            }
        }

        private static async Task<List<int>> SortFileInMemory(string fileName, List<string> identifierColumns, string fileSortedName)
        {
            using (var reader = new StreamReader(fileName))
            {
                // Determine identifier column locations
                var parsedTokens = CsvParser.Parse(reader);
                var enumerator = parsedTokens.GetEnumerator();

                var headers = CsvParser.GetNextRow(enumerator);

                var identifierLocations = new List<int>();
                for (var i = 0; i < headers.Count; i++)
                    if (identifierColumns.Contains(headers[i]))
                        identifierLocations.Add(i);

                var sorted = new SortedDictionary<string, IList<string>>();
                while (true)
                {
                    var row = CsvParser.GetNextRow(enumerator);
                    if (row == null) break;

                    var key = string.Concat(identifierLocations.Select(i => row[i]));
                    sorted.Add(key, row);
                }

                var output = sorted.ToList().Select(kv => kv.Value).ToList();
                output.Insert(0, headers);

                using (var writer = new StreamWriter(fileSortedName))
                    await CsvWriter.WriteEnumerableToWriter(output, writer);

                return identifierLocations;
            }
        }

        private async Task CompareSmallFiles(string file1Name, string file2Name, List<string> identifierColumns, List<string> exclusionColumns, List<string> inclusionColumns)
        {
            var file1Data = await CsvReader.ReadDataTableFromFileAsync(file1Name);
            var file2Data = await CsvReader.ReadDataTableFromFileAsync(file2Name);

            var comparer = new CsvComparer(file1Data, file2Data, identifierColumns, exclusionColumns, inclusionColumns);

            Results = await comparer.CompareAsync();

            if (Results.Differences.Rows.Count > 0)
            {
                DifferencesLabel.Content = "Differences:";
                DifferencesDataGrid.ItemsSource = Results.Differences.DefaultView;
                DifferencesDataGrid.Visibility = Visibility.Visible;
            }
            else
            {
                DifferencesLabel.Content = "Differences: None";
                DifferencesDataGrid.Visibility = Visibility.Collapsed;
            }

            File1OrphansLabel.Content = Results.OrphanColumns1?.Count > 0
                ? $"File 1 Extra Columns: {string.Join(", ", Results.OrphanColumns1)}"
                : "File 1 Extra Columns: None";

            File2OrphansLabel.Content = Results.OrphanColumns2?.Count > 0
                ? $"File 2 Extra Columns: {string.Join(", ", Results.OrphanColumns2)}"
                : "File 2 Extra Columns: None";

            if (Results.OrphanRows1?.Count > 0)
            {
                var table = new DataTable();
                foreach (DataColumn tableColumn in Results.OrphanRows1[0].Table.Columns)
                    table.Columns.Add(tableColumn.ColumnName);
                foreach (var dataRow in Results.OrphanRows1)
                    table.ImportRow(dataRow);

                File1ExtraRowsGrid.ItemsSource = table.DefaultView;

                File1ExtraRowsLabel.Content = "File 1 Extra Rows:";
                File1ExtraRowsGrid.Visibility = Visibility.Visible;
            }
            else
            {
                File1ExtraRowsLabel.Content = "File 1 Extra Rows: None";
                File1ExtraRowsGrid.Visibility = Visibility.Collapsed;
            }

            if (Results.OrphanRows2?.Count > 0)
            {
                var table = new DataTable();
                foreach (DataColumn tableColumn in Results.OrphanRows2[0].Table.Columns)
                    table.Columns.Add(tableColumn.ColumnName);
                foreach (var dataRow in Results.OrphanRows2)
                    table.ImportRow(dataRow);

                File2ExtraRowsGrid.ItemsSource = table.DefaultView;

                File2ExtraRowsLabel.Content = "File 2 Extra Rows:";
                File2ExtraRowsGrid.Visibility = Visibility.Visible;
            }
            else
            {
                File2ExtraRowsLabel.Content = "File 2 Extra Rows: None";
                File2ExtraRowsGrid.Visibility = Visibility.Collapsed;
            }
        }

        private string GetSaveFileName()
        {
            var dialog = new SaveFileDialog()
            {
                Filter = "CSV Files |*.csv"
            };

            return dialog.ShowDialog(this) == true
                ? dialog.FileName
                : null;
        }

        private void EnableButtons()
        {
            StartOverButton.IsEnabled = true;
            ExportResultButton.IsEnabled = true;
        }

        private void DisableButtons()
        {
            StartOverButton.IsEnabled = false;
            ExportResultButton.IsEnabled = false;
        }

        private void ClearErrors()
        {
            ErrorLabel.Content = "";
            ErrorLabel.Visibility = Visibility.Collapsed;
        }
    }
}
