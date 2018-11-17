using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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
        private DataTable File1Data { get; set; }
        private DataTable File2Data { get; set; }

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

        private async void Compare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();
                DisableButtons();

                var inclusionColumns = ExtraOutputSelectedList.Items.Cast<string>().ToList();
                var exclusionColumns = CompareExcludeSelectedList.Items.Cast<string>().ToList();

                var results = await GetComparisonResultsAsync(File1TextBox.Text, File2TextBox.Text, inclusionColumns, exclusionColumns);

                if (ComparisonWindow == null)
                    ComparisonWindow = new ComparisonResultsWindow(results, this);
                else
                    ComparisonWindow.SetResults(results);

                ComparisonWindow.Show();

                EnableButtons();
                Hide();
            }
            catch (Exception ex)
            {
                EnableButtons();
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
            }
        }

        private void AddExtraOutputButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                var items = ExtraOutputOptionsList.SelectedItems.Cast<object>().ToList();
                foreach (var item in items)
                {
                    ExtraOutputSelectedList.Items.Add(item);
                    ExtraOutputOptionsList.Items.Remove(item);
                }

                ExtraOutputOptionsList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
                ExtraOutputSelectedList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
            }
        }

        private void RemoveExtraOutputButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                var items = ExtraOutputSelectedList.SelectedItems.Cast<object>().ToList();
                foreach (var item in items)
                {
                    ExtraOutputOptionsList.Items.Add(item);
                    ExtraOutputSelectedList.Items.Remove(item);
                }

                ExtraOutputOptionsList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
                ExtraOutputSelectedList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
            }
        }

        private void AddCompareExcludeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                var items = CompareExcludeOptionsList.SelectedItems.Cast<object>().ToList();
                foreach (var item in items)
                {
                    CompareExcludeSelectedList.Items.Add(item);
                    CompareExcludeOptionsList.Items.Remove(item);
                }

                CompareExcludeSelectedList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
                CompareExcludeOptionsList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
            }
        }

        private void RemoveCompareExcludeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                var items = CompareExcludeSelectedList.SelectedItems.Cast<object>().ToList();
                foreach (var item in items)
                {
                    CompareExcludeOptionsList.Items.Add(item);
                    CompareExcludeSelectedList.Items.Remove(item);
                }

                CompareExcludeSelectedList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
                CompareExcludeOptionsList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
            }
        }

        private async void File1TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                ClearErrors();
                DisableButtons();

                File1Data = await CsvFile.ReadFromFileAsync(File1TextBox.Text);

                if (File2Data != null)
                    SetInclusionExclusionOptions();

                EnableButtons();
            }
            catch (Exception ex)
            {
                File1Data = null;
                EnableButtons();
                ExclusionInclusionGrid.Visibility = Visibility.Collapsed;
                CompareButton.Visibility = Visibility.Collapsed;
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
            }
        }

        private async void File2TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                ClearErrors();
                DisableButtons();

                File2Data = await CsvFile.ReadFromFileAsync(File2TextBox.Text);

                if (File1Data != null)
                    SetInclusionExclusionOptions();

                EnableButtons();
            }
            catch (Exception ex)
            {
                File2Data = null;
                EnableButtons();
                ExclusionInclusionGrid.Visibility = Visibility.Collapsed;
                CompareButton.Visibility = Visibility.Collapsed;
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

        private void SetInclusionExclusionOptions()
        {
            var columns1 = new List<string>();
            var columns2 = new List<string>();
            var commonColumns = new List<string>();

            foreach (DataColumn column in File1Data.Columns)
                columns1.Add(column.ColumnName);
            foreach (DataColumn column in File2Data.Columns)
                columns2.Add(column.ColumnName);

            //First Column is ID column, it's already added and not allowed to be removed
            columns1.RemoveAt(0);
            columns2.RemoveAt(0);

            commonColumns = columns1.Intersect(columns2, StringComparer.OrdinalIgnoreCase).ToList();

            ExtraOutputOptionsList.Items.Clear();
            ExtraOutputSelectedList.Items.Clear();
            CompareExcludeOptionsList.Items.Clear();
            CompareExcludeSelectedList.Items.Clear();

            foreach (var column in commonColumns)
            {
                ExtraOutputOptionsList.Items.Add(column);
                CompareExcludeOptionsList.Items.Add(column);
            }

            ExtraOutputOptionsList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            CompareExcludeOptionsList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));

            CompareButton.Visibility = Visibility.Visible;
            ExclusionInclusionGrid.Visibility = Visibility.Visible;
        }

        private Task<ComparisonResults> GetComparisonResultsAsync(string file1, string file2, List<string> inclusionColumns, List<string> exclusionColumns)
        {
            return Task.Factory.StartNew(()
                => new CsvComparer(file1, file2, exclusionColumns, inclusionColumns).Compare());
        }

        private void EnableButtons()
        {
            CompareButton.IsEnabled = true;
            AddCompareExcludeButton.IsEnabled = true;
            RemoveCompareExcludeButton.IsEnabled = true;
            AddExtraOutputButton.IsEnabled = true;
            RemoveExtraOutputButton.IsEnabled = true;
            BrowseFile1Button.IsEnabled = true;
            BrowseFile2Button.IsEnabled = true;
        }

        private void DisableButtons()
        {
            CompareButton.IsEnabled = false;
            AddCompareExcludeButton.IsEnabled = false;
            RemoveCompareExcludeButton.IsEnabled = false;
            AddExtraOutputButton.IsEnabled = false;
            RemoveExtraOutputButton.IsEnabled = false;
            BrowseFile1Button.IsEnabled = false;
            BrowseFile2Button.IsEnabled = false;
        }

        private void ClearErrors()
        {
            ErrorLabel.Content = "";
        }
    }
}
