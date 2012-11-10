﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DbShell.Common;
using DbShell.Core.Utility;
using DbShell.Driver.Common.AbstractDb;
using DbShell.Driver.Common.CommonDataLayer;
using DbShell.Driver.Common.CommonTypeSystem;
using DbShell.Driver.Common.Structure;

namespace DbShell.Core
{
    /// <summary>
    /// CSV data file
    /// </summary>
    public class CsvFile : ElementBase, ITabularDataSource, ITabularDataTarget
    {
        /// <summary>
        /// File name (should have .csv extension)
        /// </summary>
        public string Name { get; set; }

        private char _delimiter = ',';

        /// <summary>
        /// Gets or sets the column delimiter.
        /// </summary>
        /// <value>
        /// The column delimiter, by default ','
        /// </value>
        public char Delimiter
        {
            get { return _delimiter; }
            set { _delimiter = value; }
        }

        private char _quote = '"';

        /// <summary>
        /// Gets or sets the quoting character
        /// </summary>
        /// <value>
        /// The quoting character, by default '"'
        /// </value>
        public char Quote
        {
            get { return _quote; }
            set { _quote = value; }
        }

        private string _endOfLine = "\r\n";

        /// <summary>
        /// Gets or sets the Line separator
        /// </summary>
        /// <value>
        /// The end of line string, by default "\r\n"
        /// </value>
        public string EndOfLine
        {
            get { return _endOfLine; }
            set { _endOfLine = value; }
        }

        private char _escape = '"';

        /// <summary>
        /// Gets or sets the escape character
        /// </summary>
        /// <value>
        /// The escape character, used in quoted strings. By default '"'
        /// </value>
        public char Escape
        {
            get { return _escape; }
            set { _escape = value; }
        }

        private char _comment = '#';

        /// <summary>
        /// Gets or sets the line comment prefix
        /// </summary>
        /// <value>
        /// The line comment prefix. By default '#'
        /// </value>
        public char Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        private bool _hasHeaders = true;

        /// <summary>
        /// Gets or sets a value indicating whether CSV file has header row
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has header row; otherwise, <c>false</c>.
        /// </value>
        public bool HasHeaders
        {
            get { return _hasHeaders; }
            set { _hasHeaders = value; }
        }

        private CsvQuotingMode _quotingMode = CsvQuotingMode.OnlyIfNecessary;

        /// <summary>
        /// Gets or sets the quoting mode.
        /// </summary>
        /// <value>
        /// The quoting mode.
        /// </value>
        public CsvQuotingMode QuotingMode
        {
            get { return _quotingMode; }
            set { _quotingMode = value; }
        }

        protected Encoding _encoding = System.Text.Encoding.UTF8;

        /// <summary>
        /// Gets or sets the file encoding.
        /// </summary>
        /// <value>
        /// The encoding, by default UTF-8
        /// </value>
        public Encoding Encoding
        {
            get { return _encoding; }
            set { _encoding = value; }
        }

        private bool _trimSpaces = false;

        /// <summary>
        /// Gets or sets a value indicating whether trim spaces
        /// </summary>
        /// <value>
        ///   <c>true</c> if trim spaces; otherwise, <c>false</c>.
        /// </value>
        public bool TrimSpaces
        {
            get { return _trimSpaces; }
            set { _trimSpaces = value; }
        }

        private string GetName()
        {
            return Context.Replace(Name);
        }

        private LumenWorks.Framework.IO.Csv.CsvReader CreateCsvReader()
        {
            string name = Context.ResolveFile(GetName(), ResolveFileMode.Input);
            var textReader = new StreamReader(name, Encoding);
            var reader = new LumenWorks.Framework.IO.Csv.CsvReader(textReader, HasHeaders, Delimiter, Quote, Escape, Comment,
                                                                   TrimSpaces ? LumenWorks.Framework.IO.Csv.ValueTrimmingOptions.UnquotedOnly : LumenWorks.Framework.IO.Csv.ValueTrimmingOptions.None);
            return reader;
        }

        TableInfo ITabularDataSource.GetRowFormat()
        {
            using (var reader = CreateCsvReader())
            {
                return GetStructure(reader);
            }
        }

        ICdlReader ITabularDataSource.CreateReader()
        {
            var reader = CreateCsvReader();
            return new CsvReader(GetStructure(reader), reader);
        }

        bool ITabularDataTarget.AvailableRowFormat
        {
            get { return false; }
        }

        ICdlWriter ITabularDataTarget.CreateWriter(TableInfo rowFormat, CopyTableTargetOptions options)
        {
            var fs = System.IO.File.OpenWrite(Context.ResolveFile(GetName(), ResolveFileMode.Output));
            var fw = new StreamWriter(fs, Encoding);
            var writer = new CsvWriter(fw, Delimiter, Quote, Escape, Comment, QuotingMode, EndOfLine);
            if (HasHeaders)
            {
                writer.WriteRow(rowFormat.Columns.Select(c => c.Name));
            }
            return writer;
        }

        private TableInfo GetStructure(LumenWorks.Framework.IO.Csv.CsvReader reader)
        {
            var res = new TableInfo(null);
            if (HasHeaders)
            {
                foreach (string col in reader.GetFieldHeaders())
                {
                    res.Columns.Add(new ColumnInfo(res) {CommonType = new DbTypeString(), DataType = "nvarchar", Length = -1, Name = col});
                }
            }
            else
            {
                for (int i = 1; i <= reader.FieldCount; i++)
                {
                    res.Columns.Add(new ColumnInfo(res) { CommonType = new DbTypeString(), DataType = "nvarchar", Length = -1, Name = String.Format("#{0}", i) });
                }
            }
            return res;
        }


        TableInfo ITabularDataTarget.GetRowFormat()
        {
            throw new NotImplementedException();
        }
    }
}