using System.Collections.Generic;
using System.Data;

namespace CsvCompare.Library
{
    public class ComparisonResults
    {
        public ComparisonResults(DataTable differences, List<string> orphanColumns1, List<string> orphanColumns2, List<DataRow> orphanRows1, List<DataRow> orphanRows2)
        {
            Differences = differences;
            OrphanColumns1 = orphanColumns1;
            OrphanColumns2 = orphanColumns2;
            OrphanRows1 = orphanRows1;
            OrphanRows2 = orphanRows2;
        }

        public DataTable Differences { get; }
        public List<string> OrphanColumns1 { get; }
        public List<string> OrphanColumns2 { get; }
        public List<DataRow> OrphanRows1 { get; }
        public List<DataRow> OrphanRows2 { get; }
    }
}
