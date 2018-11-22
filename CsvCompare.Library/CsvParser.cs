using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace CsvCompare.Library
{
    public class CsvParser
    {
        public const int BackslashNCode = 10;
        public const int BackslashRCode = 13;
        public const int QuoteCode = 34;
        public const int CommaCode = 44;

        public static DataTable CreateDataTableFromReader(TextReader reader)
        {
            var parseTokens = ParseCsvToTokens(reader);
            var enumerator = parseTokens.GetEnumerator();

            var columnNames = GetNextLine(enumerator);

            var dataTable = new DataTable();
            foreach (var columnName in columnNames)
                dataTable.Columns.Add(columnName);

            IList<string> row;
            while ((row = GetNextLine(enumerator)) != null)
            {
                var dataRow = dataTable.NewRow();
                for (var i = 0; i < row.Count; i++)
                    dataRow[i] = row[i];
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        public static IEnumerable<IEnumerable<string>> BuildResultsEnumerable(ComparisonResults results)
        {
            yield return GetHardCodedEnumerable("Differences:");
            foreach (var difference in BuildDifferences(results))
                yield return difference;

            yield return null;

            if (results.OrphanColumns1?.Count > 0)
            {
                yield return GetHardCodedEnumerable("File 1 Extra Columns:", string.Join(", ", results.OrphanColumns1));
                yield return null;
            }

            if (results.OrphanColumns2?.Count > 0)
            {
                yield return GetHardCodedEnumerable("File 2 Extra Columns:", string.Join(", ", results.OrphanColumns2));
                yield return null;
            }

            if (results.OrphanRows1?.Count > 0)
            {
                yield return GetHardCodedEnumerable("File 1 Extra Rows:");
                yield return results.CommonColumns;
                foreach (var orphanRow in BuildOrphanRows(results.OrphanRows1))
                    yield return orphanRow;
                yield return null;
            }

            if (results.OrphanRows2?.Count > 0)
            {
                yield return GetHardCodedEnumerable("File 2 Extra Rows:");
                yield return results.CommonColumns;
                foreach (var orphanRow in BuildOrphanRows(results.OrphanRows2))
                    yield return orphanRow;
                yield return null;
            }
        }

        private static IEnumerable<string> GetHardCodedEnumerable(params string[] values) => values;

        private static IEnumerable<IEnumerable<string>> BuildDifferences(ComparisonResults results)
        {
            var columns = results.Differences.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
            yield return columns;

            foreach (DataRow dataRow in results.Differences.Rows)
                yield return dataRow.ItemArray.Select(i => i as string);
        }

        private static IEnumerable<IEnumerable<string>> BuildOrphanRows(List<DataRow> orphanRows)
        {
            var columns = orphanRows[0].ItemArray.Select(i => i as string);
            yield return columns;

            foreach (var dataRow in orphanRows)
                yield return dataRow.ItemArray.Select(i => i as string);
        }

        private static IEnumerable<Token> ParseCsvToTokens(TextReader reader)
        {
            int charCode;
            var builder = new StringBuilder();
            while ((charCode = reader.Read()) != -1)
            {
                switch (charCode)
                {
                    case CommaCode:
                        if (builder.Length > 0)
                            yield return GetNonQuotedStringValue(builder);
                        yield return new Token(TokenType.Delimiter);
                        break;
                    case BackslashRCode when reader.Peek() == BackslashNCode:
                        if (builder.Length > 0)
                            yield return GetNonQuotedStringValue(builder);
                        reader.Read();
                        yield return new Token(TokenType.Newline);
                        break;
                    case BackslashNCode:
                        if (builder.Length > 0)
                            yield return GetNonQuotedStringValue(builder);
                        yield return new Token(TokenType.Newline);
                        break;
                    case QuoteCode:
                        yield return GetQuotedStringValue(reader);
                        break;
                    default:
                        builder.Append((char)charCode);
                        break;
                }
            }
        }

        private static Token GetQuotedStringValue(TextReader reader)
        {
            var builder = new StringBuilder();
            int code;
            while ((code = reader.Read()) != -1)
            {
                if (code == QuoteCode)
                {
                    if (reader.Peek() == QuoteCode)
                    {
                        reader.Read();
                        builder.Append((char)code);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    builder.Append((char)code);
                }
            }

            return new Token(TokenType.Value, builder.ToString());
        }

        private static Token GetNonQuotedStringValue(StringBuilder builder)
        {
            var token = new Token(TokenType.Value, builder.ToString());
            builder.Clear();
            return token;
        }

        private static IList<string> GetNextLine(IEnumerator<Token> tokens)
        {
            var values = new List<string>();
            var previousTokenType = TokenType.Newline;

            while (tokens.MoveNext())
            {
                switch (tokens.Current.TokenType)
                {
                    case TokenType.Newline:
                        return values;
                    case TokenType.Value:
                        if (previousTokenType == TokenType.Value)
                            throw new Exception("The csv is malformed. Please check for proper csv values.");
                        values.Add(tokens.Current.Value);
                        break;
                    case TokenType.Delimiter:
                        break;
                }
                previousTokenType = tokens.Current.TokenType;
            }

            return values.Count > 0
                ? values
                : null;
        }

        private struct Token
        {
            public Token(TokenType tokenType, string value = null)
            {
                TokenType = tokenType;
                Value = value;
            }

            public TokenType TokenType { get; }
            public string Value { get; }
        }

        private enum TokenType
        {
            Delimiter,
            Newline,
            Value
        }
    }
}
