using System;
using System.Linq;
using System.Windows;
using CsvCompare.Library;
using Microsoft.Win32;

namespace CsvCompare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        public bool IsClosed { get; set; }
        private ComparisonResultsWindow ComparisonWindow { get; set; }

        private void BrowseFile1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                var fileName = GetFileName();
                if (fileName == null)
                    ErrorLabel.Content = "Error when choosing File 1. Please try again.";
                else
                    File1TextBox.Text = fileName;
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
            }
        }

        private void BrowseFile2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                var fileName = GetFileName();
                if (fileName == null)
                    ErrorLabel.Content = "Error when choosing File 2. Please try again.";
                else
                    File2TextBox.Text = fileName;
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
            }
        }

        private void Compare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                var fileName1 = File1TextBox.Text;
                var fileName2 = File2TextBox.Text;
                var inclusionColumns =
                    string.IsNullOrEmpty(ColumnInclusionList.Text)
                        ? null
                        : ColumnInclusionList.Text.Split(',').ToList();
                var exclusionColumns =
                    string.IsNullOrEmpty(ColumnExclusionList.Text)
                        ? null
                        : ColumnExclusionList.Text.Split(',').ToList();

                var comparer = new CsvComparer(fileName1, fileName2, exclusionColumns, inclusionColumns);
                var results = comparer.Compare();

                if (ComparisonWindow == null)
                    ComparisonWindow = new ComparisonResultsWindow(results, this);
                else
                    ComparisonWindow.SetResults(results);

                ComparisonWindow.Show();

                Hide();
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                ClearErrors();

                IsClosed = true;
                if (!ComparisonWindow?.IsClosed ?? false)
                    ComparisonWindow.Close();
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
            }
        }

        private string GetFileName()
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "CSV Files |*.csv"
            };

            return dialog.ShowDialog(this) == true
                ? dialog.FileName
                : null;
        }

        private void ClearErrors()
        {
            ErrorLabel.Content = "";
        }
    }
}
