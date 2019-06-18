using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace CsvCompare.Library
{
    public class CsvParser
    {
        public static int BackslashNCode { get; set; } = 10;
        public static int BackslashRCode { get; set; } = 13;
        public static int EscapeCode { get; set; } = 34;
        public static int DelimiterCode { get; set; } = 44;

        public static DataTable CreateDataTable(TextReader reader)
        {
            var parseTokens = Parse(reader);
            var enumerator = parseTokens.GetEnumerator();

            var columnNames = GetNextRow(enumerator);

            var dataTable = new DataTable();
            foreach (var columnName in columnNames)
                dataTable.Columns.Add(columnName);

            IList<string> row;
            while ((row = GetNextRow(enumerator)) != null)
            {
                var dataRow = dataTable.NewRow();
                for (var i = 0; i < row.Count; i++)
                    dataRow[i] = row[i];
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        public static IList<string> GetNextRow(IEnumerator<Token> tokens)
        {
            var values = new List<string>();
            var previousTokenType = TokenType.Newline;

            while (tokens.MoveNext())
            {
                switch (tokens.Current.TokenType)
                {
                    case TokenType.Newline:
                        if (previousTokenType == TokenType.Delimiter)
                            values.Add(null);
                        return values;
                    case TokenType.Value:
                        if (previousTokenType == TokenType.Value)
                            throw new Exception("The csv is malformed. Please check for proper csv values.");
                        values.Add(tokens.Current.Value);
                        break;
                    case TokenType.Delimiter:
                        if (previousTokenType == TokenType.Delimiter)
                            values.Add(null);
                        break;
                }
                previousTokenType = tokens.Current.TokenType;
            }

            return values.Count > 0
                ? values
                : null;
        }

        public static IEnumerable<Token> Parse(TextReader reader)
        {
            int charCode;
            var builder = new StringBuilder();
            while ((charCode = reader.Read()) != -1)
            {
                if (charCode == DelimiterCode)
                {
                    if (builder.Length > 0)
                        yield return GetNonQuotedStringValue(builder);
                    yield return new Token(TokenType.Delimiter);
                }
                else if (charCode == BackslashRCode && reader.Peek() == BackslashNCode)
                {
                    if (builder.Length > 0)
                        yield return GetNonQuotedStringValue(builder);
                    reader.Read();
                    yield return new Token(TokenType.Newline);
                }
                else if (charCode == BackslashNCode)
                {
                    if (builder.Length > 0)
                        yield return GetNonQuotedStringValue(builder);
                    yield return new Token(TokenType.Newline);
                }
                else if (charCode == EscapeCode)
                {
                    yield return GetQuotedStringValue(reader);
                }
                else
                {
                    builder.Append((char)charCode);
                }
            }
        }

        private static Token GetQuotedStringValue(TextReader reader)
        {
            var builder = new StringBuilder();
            int code;
            while ((code = reader.Read()) != -1)
            {
                if (code == EscapeCode)
                {
                    if (reader.Peek() == EscapeCode)
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

        public struct Token
        {
            public Token(TokenType tokenType, string value = null)
            {
                TokenType = tokenType;
                Value = value;
            }

            public TokenType TokenType { get; }
            public string Value { get; }
        }

        public enum TokenType
        {
            Delimiter,
            Newline,
            Value
        }
    }
}
