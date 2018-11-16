using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CsvCompare.Library;
using Microsoft.Win32;

namespace CsvCompare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseFile1_Click(object sender, RoutedEventArgs e)
        {
            ClearErrors();

            var fileName = GetFileName();
            if (fileName == null)
                ErrorLabel.Content = "Error when choosing File 1. Please try again.";
            else
                File1TextBox.Text = fileName;
        }

        private void BrowseFile2_Click(object sender, RoutedEventArgs e)
        {
            ClearErrors();

            var fileName = GetFileName();
            if (fileName == null)
                ErrorLabel.Content = "Error when choosing File 2. Please try again.";
            else
                File2TextBox.Text = fileName;
        }

        private void Compare_Click(object sender, RoutedEventArgs e)
        {
            var fileName1 = File1TextBox.Text;
            var fileName2 = File2TextBox.Text;
            var inclusionColumns = 
                string.IsNullOrEmpty(ColumnInclusionList.Text)
                    ? null
                    : ColumnInclusionList.Text.Split(',');
            var exclusionColumns =
                string.IsNullOrEmpty(ColumnExclusionList.Text)
                    ? null
                    : ColumnExclusionList.Text.Split(',');

            var comparer = new CsvComparer(fileName1, fileName2, exclusionColumns, inclusionColumns);
            var results = comparer.Compare();
        }

        private string GetFileName()
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "Csv Files (.csv)|*.csv"
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
