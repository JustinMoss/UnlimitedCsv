using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UnlimitedCsv;
using Microsoft.Win32;

namespace UnlimitedCsvCompare
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
        private SortingResultsWindow SortWindow { get; set; }
        private IList<string> File1ColumnNames { get; set; }
        private IList<string> File2ColumnNames { get; set; }
        private UtilityModes UtilityMode { get; set; } = UtilityModes.Compare;

        private enum UtilityModes
        {
            Sort,
            Compare
        }

        private void SortPathButton_Click(object sender, RoutedEventArgs e)
        {
            SortOrCompareGrid.Visibility = Visibility.Collapsed;

            UtilityMode = UtilityModes.Sort;
            SetComparisonUiVisibility(Visibility.Collapsed);
            SortOrCompareButton.Content = "Sort";

            FilesGrid.Visibility = Visibility.Visible;
            ButtonGrid.Visibility = Visibility.Collapsed;
            StartOverSoloButton.Visibility = Visibility.Visible;
        }

        private void ComparePathButton_Click(object sender, RoutedEventArgs e)
        {
            SortOrCompareGrid.Visibility = Visibility.Collapsed;

            UtilityMode = UtilityModes.Compare;
            SetComparisonUiVisibility(Visibility.Visible);
            SortOrCompareButton.Content = "Compare";

            FilesGrid.Visibility = Visibility.Visible;
            ButtonGrid.Visibility = Visibility.Collapsed;
            StartOverSoloButton.Visibility = Visibility.Visible;
        }

        private void SetComparisonUiVisibility(Visibility visibility)
        {
            File2Label.Visibility = visibility;
            File2TextBox.Visibility = visibility;
            BrowseFile2Button.Visibility = visibility;
            ExtraOutputButtonsGrid.Visibility = visibility;
            ExtraOutputLabel.Visibility = visibility;
            ExtraOutputSelectedList.Visibility = visibility;
            CompareExcludeGrid.Visibility = visibility;
            CompareExcludeLabel.Visibility = visibility;
            CompareExcludeSelectedList.Visibility = visibility;
            SkipSortCheckBox.Visibility = visibility;
            CaseInsensitiveCheckBox.Visibility = visibility;
        }

        private void BrowseFile1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                var fileName = GetFileName();
                if (fileName != null)
                    File1TextBox.Text = fileName;
            }
            catch (Exception ex)
            {
                SetError($"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}");
            }
        }

        private void BrowseFile2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                var fileName = GetFileName();
                if (fileName != null)
                    File2TextBox.Text = fileName;
            }
            catch (Exception ex)
            {
                SetError($"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}");
            }
        }

        private void SortOrCompare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                if (RowIdentifierSelectedList.Items.Count == 0)
                {
                    ErrorLabel.Content = "Please selected at least one column to use as an identifier.";
                    return;
                }

                DisableButtons();

                var rowIdentifierColumns = RowIdentifierSelectedList.Items.Cast<string>().ToList();

                if (UtilityMode == UtilityModes.Sort)
                {
                    if (SortWindow == null)
                        SortWindow = new SortingResultsWindow(this);

                    SortWindow.SetSettings(File1TextBox.Text, rowIdentifierColumns);
                    SortWindow.Show();
                    SortWindow.CreateResults();
                }
                if (UtilityMode == UtilityModes.Compare)
                {
                    var inclusionColumns = ExtraOutputSelectedList.Items.Cast<string>().ToList();
                    var exclusionColumns = CompareExcludeSelectedList.Items.Cast<string>().ToList();
                    var ignoreCase = CaseInsensitiveCheckBox.IsChecked ?? false;
                    var alreadySorted = SkipSortCheckBox.IsChecked ?? false;

                    if (ComparisonWindow == null)
                        ComparisonWindow = new ComparisonResultsWindow(this);

                    ComparisonWindow.SetSettings(File1TextBox.Text, File2TextBox.Text, rowIdentifierColumns, exclusionColumns, inclusionColumns, ignoreCase, alreadySorted);
                    ComparisonWindow.Show();
                    ComparisonWindow.CreateResults();
                }

                EnableButtons();
                Hide();
            }
            catch (ArgumentException ex) when (ex.Message == "An item with the same key has already been added.")
            {
                ErrorLabel.Content = "The identifier columns are not unique enough. Repeats founds between rows.";
                EnableButtons();
            }
            catch (Exception ex)
            {
                ErrorLabel.Content = $"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}";
                EnableButtons();
            }
        }

        private void AddRowIdentifierButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                var items = AvailableColumnsList.SelectedItems.Cast<object>().ToList();
                foreach (var item in items)
                {
                    RowIdentifierSelectedList.Items.Add(item);
                    AvailableColumnsList.Items.Remove(item);
                }

                RowIdentifierSelectedList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
            catch (Exception ex)
            {
                SetError($"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}");
            }
        }

        private void RemoveRowIdentifierButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                var items = RowIdentifierSelectedList.SelectedItems.Cast<object>().ToList();
                foreach (var item in items)
                {
                    AvailableColumnsList.Items.Add(item);
                    RowIdentifierSelectedList.Items.Remove(item);
                }

                AvailableColumnsList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
            catch (Exception ex)
            {
                SetError($"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}");
            }
        }

        private void AddExtraOutputButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                var items = AvailableColumnsList.SelectedItems.Cast<object>().ToList();
                foreach (var item in items)
                {
                    ExtraOutputSelectedList.Items.Add(item);
                    AvailableColumnsList.Items.Remove(item);
                }

                ExtraOutputSelectedList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
            catch (Exception ex)
            {
                SetError($"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}");
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
                    AvailableColumnsList.Items.Add(item);
                    ExtraOutputSelectedList.Items.Remove(item);
                }

                AvailableColumnsList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
            catch (Exception ex)
            {
                SetError($"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}");
            }
        }

        private void AddCompareExcludeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearErrors();

                var items = AvailableColumnsList.SelectedItems.Cast<object>().ToList();
                foreach (var item in items)
                {
                    CompareExcludeSelectedList.Items.Add(item);
                    AvailableColumnsList.Items.Remove(item);
                }

                CompareExcludeSelectedList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
            catch (Exception ex)
            {
                SetError($"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}");
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
                    AvailableColumnsList.Items.Add(item);
                    CompareExcludeSelectedList.Items.Remove(item);
                }

                AvailableColumnsList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
            catch (Exception ex)
            {
                SetError($"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}");
            }
        }

        private async void File1TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                ClearErrors();
                DisableButtons();

                if (!string.IsNullOrEmpty(File1TextBox.Text))
                {
                    File1ColumnNames = await CsvReader.ReadColumnsNamesFromFileAsync(File1TextBox.Text);

                    if (File2ColumnNames != null || UtilityMode == UtilityModes.Sort)
                        SetOptions();
                }
                else
                {
                    File1ColumnNames = null;
                }

                EnableButtons();
            }
            catch (FileNotFoundException)
            {
                File1ColumnNames = null;
                SetError("File 1 was not found. Please choose File 1 again.");
            }
            catch (Exception ex)
            {
                File1ColumnNames = null;
                SetError($"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}");
            }
        }

        private async void File2TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                ClearErrors();
                DisableButtons();

                if (!string.IsNullOrEmpty(File2TextBox.Text))
                {
                    File2ColumnNames = await CsvReader.ReadColumnsNamesFromFileAsync(File2TextBox.Text);

                    if (File1ColumnNames != null)
                        SetOptions();
                }
                else
                {
                    File2ColumnNames = null;
                }

                EnableButtons();
            }
            catch (FileNotFoundException)
            {
                File2ColumnNames = null;
                SetError("File 2 was not found. Please choose File 2 again.");
            }
            catch (Exception ex)
            {
                File2ColumnNames = null;
                SetError($"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}");
            }
        }

        private void BrowseFile_OnDrop(object sender, DragEventArgs e)
            => ((TextBox)sender).Text = ((string[])e.Data.GetData(DataFormats.FileDrop))?[0] ?? "";

        private void BrowseFile_OnDragEnter(object sender, DragEventArgs e)
            => e.Handled = true;

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                ClearErrors();

                IsClosed = true;
                if (!ComparisonWindow?.IsClosed ?? false)
                    ComparisonWindow.Close();
                if (!SortWindow?.IsClosed ?? false)
                    SortWindow.Close();
            }
            catch (Exception ex)
            {
                SetError($"Error: {ex.Message}{Environment.NewLine} Stack Trace: {ex.StackTrace}");
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

        private void SetOptions()
        {
            var commonColumns = UtilityMode == UtilityModes.Sort
                ? File1ColumnNames
                : File1ColumnNames.Intersect(File2ColumnNames, StringComparer.OrdinalIgnoreCase).ToList();

            if (commonColumns.Count == 0)
            {
                SetError("There were no common columns found between the two files. There is nothing to compare.");
                return;
            }

            //Preset first column as ID
            var firstColumnName = commonColumns[0];
            commonColumns.RemoveAt(0);

            AvailableColumnsList.Items.Clear();
            RowIdentifierSelectedList.Items.Clear();
            ExtraOutputSelectedList.Items.Clear();
            CompareExcludeSelectedList.Items.Clear();

            RowIdentifierSelectedList.Items.Add(firstColumnName);

            foreach (var column in commonColumns)
                AvailableColumnsList.Items.Add(column);

            AvailableColumnsList.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));

            OptionsGrid.Visibility = Visibility.Visible;
            ButtonGrid.Visibility = Visibility.Visible;
            StartOverSoloButton.Visibility = Visibility.Collapsed;
        }

        private void EnableButtons()
        {
            StartOverButton.IsEnabled = true;
            SortOrCompareButton.IsEnabled = true;
            CaseInsensitiveCheckBox.IsEnabled = true;
            SkipSortCheckBox.IsEnabled = true;
            AddRowIdentifierButton.IsEnabled = true;
            RemoveRowIdentifierButton.IsEnabled = true;
            AddCompareExcludeButton.IsEnabled = true;
            RemoveCompareExcludeButton.IsEnabled = true;
            AddExtraOutputButton.IsEnabled = true;
            RemoveExtraOutputButton.IsEnabled = true;
            BrowseFile1Button.IsEnabled = true;
            BrowseFile2Button.IsEnabled = true;
        }

        private void DisableButtons()
        {
            StartOverButton.IsEnabled = false;
            SortOrCompareButton.IsEnabled = false;
            CaseInsensitiveCheckBox.IsEnabled = false;
            SkipSortCheckBox.IsEnabled = false;
            AddRowIdentifierButton.IsEnabled = false;
            RemoveRowIdentifierButton.IsEnabled = false;
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

        private void SetError(string error)
        {
            EnableButtons();

            OptionsGrid.Visibility = Visibility.Collapsed;
            ButtonGrid.Visibility = Visibility.Collapsed;
            StartOverSoloButton.Visibility = Visibility.Visible;

            ErrorLabel.Content = error;
        }

        private void StartOverButton_Click(object sender, RoutedEventArgs e)
        {
            FilesGrid.Visibility = Visibility.Collapsed;
            OptionsGrid.Visibility = Visibility.Collapsed;
            ButtonGrid.Visibility = Visibility.Collapsed;
            StartOverSoloButton.Visibility = Visibility.Collapsed;

            File1TextBox.Text = "";
            File2TextBox.Text = "";

            SortOrCompareGrid.Visibility = Visibility.Visible;
        }
    }
}
