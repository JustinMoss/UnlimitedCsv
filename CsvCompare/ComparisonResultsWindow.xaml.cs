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
            InitializeComponent();

            SettingsWindow = settingsWindow;

            SetResults(results);
        }

        public bool IsClosed { get; set; }
        public ComparisonResults Results { get; set; }
        public SettingsWindow SettingsWindow { get; set; }

        private void StartOver_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow.Show();

            Hide();
        }

        private void ExportResults_Click(object sender, RoutedEventArgs e)
        {
            var saveFileName = GetSaveFileName();

            if (saveFileName == null)
                return; //TODO: throw some error

            CsvFile.WriteToFile(saveFileName, Results);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            IsClosed = true;
            if (!SettingsWindow.IsClosed)
                SettingsWindow.Close();
        }

        public void SetResults(ComparisonResults results)
        {
            Results = results;

            ResultsDataGrid.ItemsSource = Results.Differences.DefaultView;

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
    }
}
