using System;
using System.Data;
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
        public ComparisonResultsWindow(ComparisonResults results, SettingsWindow settingsWindow)
        {
            try
            {
                InitializeComponent();

                ClearErrors();

                SettingsWindow = settingsWindow;

                SetResults(results);
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
                    return; //TODO: throw some error

                await CsvFile.WriteToFileAsync(saveFileName, Results);

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

        public void SetResults(ComparisonResults results)
        {
            try
            {
                ClearErrors();

                Results = results;

                if (Results.Differences.Rows.Count > 0)
                {
                    NoDifferencesLabel.Visibility = Visibility.Collapsed;
                    DifferencesDataGrid.ItemsSource = Results.Differences.DefaultView;
                    DifferencesDataGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    NoDifferencesLabel.Visibility = Visibility.Visible;
                    DifferencesDataGrid.Visibility = Visibility.Collapsed;
                }

                if (results.OrphanColumns1?.Count > 0)
                {
                    File1OrphansLabel.Content = $"File 1 Extra Columns: {string.Join(", ", results.OrphanColumns1)}";
                    File1OrphansLabel.Visibility = Visibility.Visible;
                }
                else
                {
                    File1OrphansLabel.Visibility = Visibility.Collapsed;
                }

                if (results.OrphanColumns2?.Count > 0)
                {
                    File2OrphansLabel.Content = $"File 2 Extra Columns: {string.Join(", ", results.OrphanColumns2)}";
                    File2OrphansLabel.Visibility = Visibility.Visible;
                }
                else
                {
                    File2OrphansLabel.Visibility = Visibility.Collapsed;
                }

                if (results.OrphanRows1?.Count > 0)
                {
                    var table = new DataTable();
                    foreach (DataColumn tableColumn in results.OrphanRows1[0].Table.Columns)
                        table.Columns.Add(tableColumn.ColumnName);
                    foreach (var dataRow in results.OrphanRows1)
                        table.ImportRow(dataRow);

                    File1ExtraRowsGrid.ItemsSource = table.DefaultView;

                    File1ExtraRowsLabel.Visibility = Visibility.Visible;
                    File1ExtraRowsGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    File1ExtraRowsLabel.Visibility = Visibility.Collapsed;
                    File1ExtraRowsGrid.Visibility = Visibility.Collapsed;
                }

                if (results.OrphanRows2?.Count > 0)
                {
                    var table = new DataTable();
                    foreach (DataColumn tableColumn in results.OrphanRows2[0].Table.Columns)
                        table.Columns.Add(tableColumn.ColumnName);
                    foreach (var dataRow in results.OrphanRows2)
                        table.ImportRow(dataRow);

                    File2ExtraRowsGrid.ItemsSource = table.DefaultView;
                    File2ExtraRowsLabel.Visibility = Visibility.Visible;
                    File2ExtraRowsGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    File2ExtraRowsLabel.Visibility = Visibility.Collapsed;
                    File2ExtraRowsGrid.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
                ErrorLabel.Visibility = Visibility.Visible;
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
