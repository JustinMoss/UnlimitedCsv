using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CsvUtilities;
using Microsoft.Win32;

namespace CsvCompare
{
    /// <summary>
    /// Interaction logic for ComparisonWindow.xaml
    /// </summary>
    public partial class ComparisonResultsWindow : Window
    {
        private string _file1Name;
        private string _file2Name;
        private List<string> _identifierColumns;
        private List<string> _exclusionColumns;
        private List<string> _inclusionColumns;
        private bool _ignoreCase;
        private bool _alreadySorted;

        private string _comparisonTempFile;
        private string _rowOrphans1TempFile;
        private string _rowOrphans2TempFile;
        private List<string> _columnOrphans1;
        private List<string> _columnOrphans2;

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
        public SettingsWindow SettingsWindow { get; set; }

        private void StartOver_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();
                CleanUpTempFiles();

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

                await CreatesResultsFile(saveFileName, _comparisonTempFile, _rowOrphans1TempFile, _rowOrphans2TempFile, _columnOrphans1, _columnOrphans2);

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

                CleanUpTempFiles();

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

        public void SetSettings(string file1Name, string file2Name, List<string> identifierColumns, List<string> exclusionColumns, List<string> inclusionColumns, bool ignoreCase, bool alreadySorted)
        {
            _file1Name = file1Name;
            _file2Name = file2Name;
            _identifierColumns = identifierColumns;
            _exclusionColumns = exclusionColumns;
            _inclusionColumns = inclusionColumns;
            _ignoreCase = ignoreCase;
            _alreadySorted = alreadySorted;
        }

        public async Task CreateResults()
        {
            try
            {
                ClearErrors();
                HideResultsWindows();
                ResetCompareWindow();
                ButtonGrid.Visibility = Visibility.Collapsed;

                CompareProgressWindow.Text = "Beginning Comparison.";

                var fileTime = DateTime.Now.ToFileTime();
                var file1SortedName = _alreadySorted ? _file1Name : _file1Name.Replace(".csv", "_sorted.csv");
                var file2SortedName = _alreadySorted ? _file2Name : _file2Name.Replace(".csv", "_sorted.csv");

                var tempFolder = Path.Combine(Path.GetTempPath(), "CsvCompare");
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);

                _comparisonTempFile = Path.Combine(tempFolder, $"comparison_{fileTime}_temp.csv");
                _rowOrphans1TempFile = Path.Combine(tempFolder, $"comparison_orphans1_{fileTime}_temp.csv");
                _rowOrphans2TempFile = Path.Combine(tempFolder, $"comparison_orphans2_{fileTime}_temp.csv");

                // Sort file 1
                if (_alreadySorted)
                {
                    CompareProgressWindow.Text += Environment.NewLine + "Skipping sorting files.";
                }
                else
                {
                    // Figure out if we have room to sort in memory or we need external merge
                    var memory = new PerformanceCounter("Memory", "Available MBytes");
                    var memoryValue = (long)memory.NextValue();
                    long maxFileSize = memoryValue / 10 * 1024 * 1024;

                    CompareProgressWindow.Text += Environment.NewLine + "Sorting File 1.";
                    var sorting1Stopwatch = new Stopwatch();
                    sorting1Stopwatch.Start();

                    if (new FileInfo(_file1Name).Length < maxFileSize)
                    {
                        CompareProgressWindow.Text += Environment.NewLine + "Using quick memory sort for small file.";
                        await Task.Run(() => CsvFileSorter.SortFileInMemory(_file1Name, _identifierColumns, file1SortedName));
                    }
                    else
                    {
                        CompareProgressWindow.Text += Environment.NewLine + "Using slow file sort for large file.";
                        await Task.Run(() => CsvFileSorter.ExternalMergeSort(_file1Name, maxFileSize, _identifierColumns, file1SortedName, tempFolder));
                    }

                    sorting1Stopwatch.Stop();
                    CompareProgressWindow.Text += Environment.NewLine + "Finished sorting File 1 in " + TimeSpanAsEnglish(sorting1Stopwatch.Elapsed);

                    // Sort file 2
                    CompareProgressWindow.Text += Environment.NewLine + "Sorting File 2.";
                    var sorting2Stopwatch = new Stopwatch();
                    sorting2Stopwatch.Start();

                    if (new FileInfo(_file2Name).Length < maxFileSize)
                    {
                        CompareProgressWindow.Text += Environment.NewLine + "Using quick memory sort for small file.";
                        await Task.Run(() => CsvFileSorter.SortFileInMemory(_file2Name, _identifierColumns, file2SortedName));
                    }
                    else
                    {
                        CompareProgressWindow.Text += Environment.NewLine + "Using slow file sort for large file.";
                        await Task.Run(() => CsvFileSorter.ExternalMergeSort(_file2Name, maxFileSize, _identifierColumns, file2SortedName, tempFolder));
                    }

                    sorting2Stopwatch.Stop();
                    CompareProgressWindow.Text += Environment.NewLine + "Finished sorting File 2 in " + TimeSpanAsEnglish(sorting2Stopwatch.Elapsed);
                }

                // Compare line by line.
                CompareProgressWindow.Text += Environment.NewLine + "Comparing the files.";
                var comparisonStopWatch = new Stopwatch();
                comparisonStopWatch.Start();
                var orphans = await Task.Run(() => CsvFileComparer.Compare(file1SortedName, file2SortedName, _identifierColumns, _inclusionColumns, _exclusionColumns, _comparisonTempFile, _rowOrphans1TempFile, _rowOrphans2TempFile, _ignoreCase));
                comparisonStopWatch.Stop();
                _columnOrphans1 = orphans.ColumnOrphans1;
                _columnOrphans2 = orphans.ColumnOrphans2;
                CompareProgressWindow.Text += Environment.NewLine + "Finished comparing the files in " + TimeSpanAsEnglish(comparisonStopWatch.Elapsed);

                ButtonGrid.Visibility = Visibility.Visible;

                if (new FileInfo(_comparisonTempFile).Length + 
                    new FileInfo(_rowOrphans1TempFile).Length +
                    new FileInfo(_rowOrphans2TempFile).Length < 10_000_000)
                {
                    // Small results, display results
                    SetupDisplayValues();
                    HideCompareWindow();
                }
                else
                {
                    ErrorLabel.Content = "Results file is too large for display. Click 'Export' for the comparison results.";
                    ErrorLabel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
                ErrorLabel.Visibility = Visibility.Visible;
            }
        }

        private void SetupDisplayValues()
        {
            var comparisonData = CsvReader.ReadDataTableFromFile(_comparisonTempFile);
            var rowOrphans1Data = CsvReader.ReadDataTableFromFile(_rowOrphans1TempFile);
            var rowOrphans2Data = CsvReader.ReadDataTableFromFile(_rowOrphans2TempFile);

            if (comparisonData.Rows.Count > 0)
            {
                DifferencesLabel.Content = "Differences:";
                DifferencesLabel.Visibility = Visibility.Visible;
                DifferencesDataGrid.ItemsSource = comparisonData.DefaultView;
                DifferencesDataGrid.Visibility = Visibility.Visible;
            }
            else
            {
                DifferencesLabel.Content = "Differences: None";
                DifferencesLabel.Visibility = Visibility.Collapsed;
                DifferencesDataGrid.Visibility = Visibility.Collapsed;
            }

            if (rowOrphans1Data.Rows.Count > 0)
            {
                File1ExtraRowsGrid.ItemsSource = rowOrphans1Data.DefaultView;

                File1ExtraRowsLabel.Content = "File 1 Extra Rows:";
                File1ExtraRowsLabel.Visibility = Visibility.Visible;
                File1ExtraRowsGrid.Visibility = Visibility.Visible;
            }
            else
            {
                File1ExtraRowsLabel.Content = "File 1 Extra Rows: None";
                File1ExtraRowsLabel.Visibility = Visibility.Collapsed;
                File1ExtraRowsGrid.Visibility = Visibility.Collapsed;
            }

            if (rowOrphans2Data.Rows.Count > 0)
            {
                File2ExtraRowsGrid.ItemsSource = rowOrphans2Data.DefaultView;

                File2ExtraRowsLabel.Content = "File 2 Extra Rows:";
                File2ExtraRowsLabel.Visibility = Visibility.Visible;
                File2ExtraRowsGrid.Visibility = Visibility.Visible;
            }
            else
            {
                File2ExtraRowsLabel.Content = "File 2 Extra Rows: None";
                File2ExtraRowsLabel.Visibility = Visibility.Collapsed;
                File2ExtraRowsGrid.Visibility = Visibility.Collapsed;
            }

            File1OrphansLabel.Visibility = Visibility.Visible;
            File1OrphansLabel.Content = _columnOrphans1.Count > 0
                ? $"File 1 Extra Columns: {string.Join(", ", _columnOrphans1)}"
                : "File 1 Extra Columns: None";

            File2OrphansLabel.Visibility = Visibility.Visible;
            File2OrphansLabel.Content = _columnOrphans1.Count > 0
                ? $"File 2 Extra Columns: {string.Join(", ", _columnOrphans1)}"
                : "File 2 Extra Columns: None";
        }

        private static async Task CreatesResultsFile(string saveFileName, string comparisonTempFile, string rowOrphans1TempFile, string rowOrphans2TempFile, List<string> columnOrphans1, List<string> columnOrphans2)
        {
            // Merge output results
            using (var resultsWriter = new StreamWriter(saveFileName))
            {
                // Differences
                await CsvWriter.WriteRowToWriter(new[] { "Differences: " }, resultsWriter);
                await CsvWriter.CopyFileToWriter(comparisonTempFile, resultsWriter);

                // Extra Rows 1
                await CsvWriter.WriteRowToWriter(null, resultsWriter);
                await CsvWriter.WriteRowToWriter(new[] { "File 1 Extra Rows:" }, resultsWriter);
                await CsvWriter.CopyFileToWriter(rowOrphans1TempFile, resultsWriter);

                // Extra Rows 2
                await CsvWriter.WriteRowToWriter(null, resultsWriter);
                await CsvWriter.WriteRowToWriter(new[] { "File 2 Extra Rows:" }, resultsWriter);
                await CsvWriter.CopyFileToWriter(rowOrphans2TempFile, resultsWriter);

                // Extra Columns 1
                await CsvWriter.WriteRowToWriter(null, resultsWriter);
                columnOrphans1.Insert(0, "File 1 Extra Columns:");
                await CsvWriter.WriteRowToWriter(columnOrphans1, resultsWriter);

                // Extra Columns 2
                await CsvWriter.WriteRowToWriter(null, resultsWriter);
                columnOrphans2.Insert(0, "File 2 Extra Columns:");
                await CsvWriter.WriteRowToWriter(columnOrphans2, resultsWriter);
            }
        }

        private static string TimeSpanAsEnglish(TimeSpan timeSpan)
        {
            var intervals = new List<string>();
            if (timeSpan.Days > 0)
                intervals.Add(timeSpan.Hours + " days");
            if (timeSpan.Hours > 0)
                intervals.Add(timeSpan.Hours + " hours");
            if (timeSpan.Minutes > 0)
                intervals.Add(timeSpan.Minutes + " minutes");
            if (timeSpan.Seconds > 0)
                intervals.Add(timeSpan.Seconds + " seconds");
            if (timeSpan.Milliseconds > 0)
                intervals.Add(timeSpan.Milliseconds + " milliseconds");
            return string.Join(", ", intervals);
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

        private void HideResultsWindows()
        {
            DifferencesLabel.Visibility = Visibility.Collapsed;
            DifferencesDataGrid.Visibility = Visibility.Collapsed;
            File1ExtraRowsLabel.Visibility = Visibility.Collapsed;
            File1ExtraRowsGrid.Visibility = Visibility.Collapsed;
            File2ExtraRowsLabel.Visibility = Visibility.Collapsed;
            File2ExtraRowsGrid.Visibility = Visibility.Collapsed;
            File1OrphansLabel.Visibility = Visibility.Collapsed;
            File2OrphansLabel.Visibility = Visibility.Collapsed;
        }

        private void HideCompareWindow()
        {
            CompareProgressWindow.Visibility = Visibility.Collapsed;
        }

        private void ResetCompareWindow()
        {
            CompareProgressWindow.Text = "";
            CompareProgressWindow.Visibility = Visibility.Visible;
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

        private void CleanUpTempFiles()
        {
            if (File.Exists(_comparisonTempFile))
                File.Delete(_comparisonTempFile);
            if (File.Exists(_rowOrphans1TempFile))
                File.Delete(_rowOrphans1TempFile);
            if (File.Exists(_rowOrphans2TempFile))
                File.Delete(_rowOrphans2TempFile);
        }
    }
}
