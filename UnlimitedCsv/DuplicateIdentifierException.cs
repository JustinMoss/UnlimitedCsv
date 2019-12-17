using System;
using System.Collections.Generic;

namespace UnlimitedCsv
{
    /// <summary>
    /// Represents an error when an identifier already exists in another CSV row.
    /// </summary>
    public class DuplicateIdentifierException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateIdentifierException"/>.
        /// </summary>
        /// <param name="identifierNames">A list of the identifier column names.</param>
        /// <param name="identifierValues">A list of the identifier column values.</param>
        public DuplicateIdentifierException(List<string> identifierNames, List<string> identifierValues)
            : base()
        {
            IdentifierNames = identifierNames;
            IdentifierValues = identifierValues;
        }

        /// <summary>
        /// Gets a list of the identifier column names.
        /// </summary>
        public List<string> IdentifierNames { get; }

        /// <summary>
        /// Gets a list of the identifier column values.
        /// </summary>
        public List<string> IdentifierValues { get; }
    }
}
