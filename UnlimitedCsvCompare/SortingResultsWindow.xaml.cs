using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UnlimitedCsv;

namespace UnlimitedCsvCompare
{
    /// <summary>
    /// Interaction logic for SortingResultsWindow.xaml
    /// </summary>
    public partial class SortingResultsWindow : Window
    {
        private string _file1Name;
        private List<string> _identifierColumns;

        public SortingResultsWindow(SettingsWindow settingsWindow)
        {
            try
            {
                InitializeComponent();

                ClearErrors();

                SettingsWindow = settingsWindow;
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
                ErrorLabel.Visibility = Visibility.Visible;
            }
        }

        public bool IsClosed { get; set; }
        public SettingsWindow SettingsWindow { get; set; }

        public void SetSettings(string file1Name, List<string> identifierColumns)
        {
            _file1Name = file1Name;
            _identifierColumns = identifierColumns;
        }

        public async Task CreateResults()
        {
            try
            {
                ClearErrors();
                CompareProgressWindow.Text = "";
                CompareProgressWindow.Visibility = Visibility.Visible;
                StartOverButton.Visibility = Visibility.Collapsed;

                CompareProgressWindow.Text = "Beginning Comparison.";

                var fileTime = DateTime.Now.ToFileTime();
                var file1SortedName = _file1Name.Replace(".csv", "_sorted.csv");

                var tempFolder = Path.Combine(Path.GetTempPath(), "UnlimitedCsvCompare");
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);

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
                
                StartOverButton.Visibility = Visibility.Visible;
            }
            catch (DuplicateIdentifierException ex)
            {
                var errorBuilder = new StringBuilder($"Error: A duplicate identifier was found while sorting.{Environment.NewLine}Identifiers:{Environment.NewLine}");
                for (int i = 0; i < ex.IdentifierNames.Count; i++)
                    errorBuilder.AppendLine($"{ex.IdentifierNames[i]} - {ex.IdentifierValues[i]}");

                ErrorLabel.Text = errorBuilder.ToString();
                ErrorLabel.Visibility = Visibility.Visible;
                StartOverButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
                ErrorLabel.Visibility = Visibility.Visible;
                StartOverButton.Visibility = Visibility.Visible;
            }
        }

        private void StartOver_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                CompareProgressWindow.Text = "";
                CompareProgressWindow.Visibility = Visibility.Visible;
                StartOverButton.Visibility = Visibility.Collapsed;

                SettingsWindow.Show();

                Hide();
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
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
                ErrorLabel.Text = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
                ErrorLabel.Visibility = Visibility.Visible;
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

        private void ClearErrors()
        {
            ErrorLabel.Text = "";
            ErrorLabel.Visibility = Visibility.Collapsed;
        }
    }
}
