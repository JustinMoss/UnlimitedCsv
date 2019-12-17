using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace UnlimitedCsv
{
    /// <summary>
    /// Provides a set of methods for parsing CSV text.
    /// </summary>
    public class CsvParser
    {
        /// <summary>
        /// Gets or sets the character code for \n.
        /// </summary>
        public static int BackslashNCode { get; set; } = 10;

        /// <summary>
        /// Gets or sets the character code for \r.
        /// </summary>
        public static int BackslashRCode { get; set; } = 13;

        /// <summary>
        /// Gets or sets the character code for the escape character. Defaults to a double quote.
        /// </summary>
        public static int EscapeCode { get; set; } = 34;

        /// <summary>
        /// Gets or sets the character code for the delimiter. Defaults to a comma.
        /// </summary>
        public static int DelimiterCode { get; set; } = 44;

        /// <summary>
        /// Creates a <see cref="DataTable"/> from CSV text.
        /// </summary>
        /// <param name="reader">The reader to get the csv text from.</param>
        /// <returns>A <see cref="DataTable"/> representation of the CSV.</returns>
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

        /// <summary>
        /// Fills in a given string array with the next csv row, stopping at the next <see cref="TokenType.Newline"/>.
        /// This does not use <see cref="GetNextRow(IEnumerator{Token})"/> to avoid the list of string creation for memory efficiency.
        /// </summary>
        /// <param name="tokens">The tokens to fill the row in from.</param>
        /// <param name="row">The row to fill from the the tokens.</param>
        public static void FillNextRow(IEnumerator<Token> tokens, string[] row)
        {
            var previousTokenType = TokenType.Newline;
            var i = 0;
            while (tokens.MoveNext())
            {
                switch (tokens.Current.TokenType)
                {
                    case TokenType.Newline:
                        if (previousTokenType == TokenType.Delimiter)
                            row[i] = null;
                        return;
                    case TokenType.Value:
                        if (previousTokenType == TokenType.Value)
                            throw new Exception("The csv is malformed. Please check for proper csv values.");
                        row[i] = tokens.Current.Value;
                        i++;
                        break;
                    case TokenType.Delimiter:
                        if (previousTokenType == TokenType.Delimiter)
                        {
                            row[i] = tokens.Current.Value;
                            i++;
                        }
                        break;
                }
                previousTokenType = tokens.Current.TokenType;
            }
        }

        /// <summary>
        /// Get the next CSV row as a list of strings.
        /// </summary>
        /// <param name="tokens">The tokens to get the next row from.</param>
        /// <returns>A list of CSV string values.</returns>
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

        /// <summary>
        /// Parses a CSV string into a list of CSV <see cref="Token"/>.
        /// </summary>
        /// <param name="value">The CSV string to parse.</param>
        /// <returns>A list of the parsed <see cref="Token"/></returns>
        public static IEnumerable<Token> Parse(string value)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < value.Length; i++)
            {
                int charCode = value[i];
                if (charCode == DelimiterCode)
                {
                    if (builder.Length > 0)
                        yield return GetNonQuotedStringValue(builder);
                    yield return new Token(TokenType.Delimiter);
                }
                else if (charCode == BackslashRCode && i + 1 < value.Length && value[i + 1] == BackslashNCode)
                {
                    if (builder.Length > 0)
                        yield return GetNonQuotedStringValue(builder);
                    i++;
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
                    yield return GetQuotedStringValue(value, ref i);
                }
                else
                {
                    builder.Append((char)charCode);
                }
            }
        }

        /// <summary>
        /// Parses CSV text into a list of CSV <see cref="Token"/>.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader"/> to get the CSV text from.</param>
        /// <returns>A list of the parsed <see cref="Token"/></returns>
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

        private static Token GetQuotedStringValue(string value, ref int i)
        {
            var builder = new StringBuilder();
            for (i++; i < value.Length; i++)
            {
                int code = value[i];
                if (code == EscapeCode)
                {
                    if (i == value.Length - 1 || value[i + 1] != EscapeCode)
                        break;
                    builder.Append((char)code);
                    i++;
                }
                else
                {
                    builder.Append((char)code);
                }
            }

            return new Token(TokenType.Value, builder.ToString());
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

        /// <summary>
        /// A struct representing a single CSV parsed token.
        /// </summary>
        public struct Token
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Token"/> class.
            /// </summary>
            /// <param name="tokenType">The <see cref="TokenType"/> type of this token.</param>
            /// <param name="value">The string value of this token. Optional.</param>
            public Token(TokenType tokenType, string value = null)
            {
                TokenType = tokenType;
                Value = value;
            }

            /// <summary>
            /// The <see cref="TokenType"/> type of this token.
            /// </summary>
            public TokenType TokenType { get; }

            /// <summary>
            /// The string value of this token. Optional.
            /// </summary>
            public string Value { get; }
        }

        /// <summary>
        /// Defines the types for CSV tokens.
        /// </summary>
        public enum TokenType
        {
            /// <summary>
            /// A delimiter token, based on the <see cref="DelimiterCode"/>.
            /// </summary>
            Delimiter,
            /// <summary>
            /// A new line token.
            /// </summary>
            Newline,
            /// <summary>
            /// A value token. The <see cref="Token.Value"/> has significance.
            /// </summary>
            Value
        }
    }
}
