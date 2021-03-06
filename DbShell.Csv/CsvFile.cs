﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using DbShell.Core.Utility;
using DbShell.Driver.Common.AbstractDb;
using DbShell.Driver.Common.CommonDataLayer;
using DbShell.Driver.Common.CommonTypeSystem;
using DbShell.Driver.Common.Structure;
using DbShell.Driver.Common.Utility;
using DbShell.Driver.Common.Interfaces;
using Microsoft.Extensions.Logging;
using LumenWorks.Framework.IO.Csv;

namespace DbShell.Csv
{
    /// <summary>
    /// CSV data file
    /// </summary>
    public class CsvFile : ElementBase, ITabularDataSource, ITabularDataTarget
    {
        /// <summary>
        /// File name (should have .csv extension)
        /// </summary>
        [XamlProperty]
        public string Name { get; set; }

        private char _delimiter = ',';

        /// <summary>
        /// Gets or sets the column delimiter.
        /// </summary>
        /// <value>
        /// The column delimiter, by default ','
        /// </value>
        [XamlProperty]
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
        [XamlProperty]
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
        [XamlProperty]
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
        [XamlProperty]
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
        [XamlProperty]
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
        [XamlProperty]
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
        [XamlProperty]
#if !NETSTANDARD2_0
        [TypeConverter(typeof(EncodingTypeConverter))]
#endif
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
        [XamlProperty]
        public bool TrimSpaces
        {
            get { return _trimSpaces; }
            set { _trimSpaces = value; }
        }

        /// <summary>
        /// data format settings
        /// </summary>
        [XamlProperty]
        public DataFormatSettings DataFormat { get; set; }

        /// <summary>
        /// delimiter will be autodetected
        /// </summary>
        [XamlProperty]
        public bool AutoDetectDelimiter { get; set; }

        DataFormatSettings ITabularDataSource.GetSourceFormat(IShellContext context)
        {
            return DataFormat;
        }

        private string GetName(IShellContext context)
        {
            return context.Replace(Name);
        }

        private LumenWorks.Framework.IO.Csv.CsvReader CreateCsvReader(IShellContext context)
        {
            string name = GetName(context);
            name = context.ResolveFile(name, ResolveFileMode.Input);
            var textReader = new StreamReader(name, Encoding);
            return CreateCsvReader(textReader, name);
        }

        private LumenWorks.Framework.IO.Csv.CsvReader CreateCsvReader(TextReader textReader, string file = null)
        {
            char delim = Delimiter;
            if (file != null && AutoDetectDelimiter) delim = DetectDelimiter(file);
            var reader = new LumenWorks.Framework.IO.Csv.CsvReader(textReader, HasHeaders, delim, Quote, Escape, Comment,
                                                                   TrimSpaces ? LumenWorks.Framework.IO.Csv.ValueTrimmingOptions.UnquotedOnly : LumenWorks.Framework.IO.Csv.ValueTrimmingOptions.None);
            reader.DefaultParseErrorAction = ParseErrorAction.AdvanceToNextLine;
            return reader;
        }

        TableInfo ITabularDataSource.GetRowFormat(IShellContext context)
        {
            using (var reader = CreateCsvReader(context))
            {
                return GetStructure(reader);
            }
        }

        ICdlReader ITabularDataSource.CreateReader(IShellContext context)
        {
            var reader = CreateCsvReader(context);
            return new CsvReader(GetStructure(reader), reader, DataFormat);
        }

        // ignores File attribute and creates buffer reader
        public CsvReader CreateBufferCsvReader(TextReader textReader)
        {
            var reader = CreateCsvReader(textReader);
            return new CsvReader(GetStructure(reader), reader, DataFormat);
        }

        bool ITabularDataTarget.IsAvailableRowFormat(IShellContext context)
        {
            return false;
        }

        ICdlWriter ITabularDataTarget.CreateWriter(TableInfo rowFormat, CopyTableTargetOptions options, IShellContext context, DataFormatSettings sourceDataFormat)
        {
            string file = context.ResolveFile(GetName(context), ResolveFileMode.Output);
            context.GetLogger<CsvFile>().LogInformation("Writing file {file}", Path.GetFullPath(file));
            context.Info("Writing file " + Path.GetFullPath(file));
            var fs = System.IO.File.OpenWrite(file);
            var fw = new StreamWriter(fs, Encoding);
            var writer = new CsvWriter(fw, Delimiter, Quote, Escape, Comment, QuotingMode, EndOfLine, DataFormat);
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
                    res.Columns.Add(new ColumnInfo(res) { CommonType = new DbTypeString(), DataType = "nvarchar", Name = col });
                }
            }
            else
            {
                for (int i = 1; i <= reader.FieldCount; i++)
                {
                    res.Columns.Add(new ColumnInfo(res) { CommonType = new DbTypeString(), DataType = "nvarchar", Name = String.Format("#{0}", i) });
                }
            }
            return res;
        }

        public char DetectDelimiter(IShellContext context)
        {
            string name = GetName(context);
            name = context.ResolveFile(name, ResolveFileMode.Input);
            return DetectDelimiter(name);
        }

        private char DetectDelimiter(string file)
        {
            var counts = new Dictionary<char, List<int>>();
            counts[','] = new List<int>();
            counts['|'] = new List<int>();
            counts[';'] = new List<int>();
            counts['\t'] = new List<int>();

            using (var fs = System.IO.File.OpenRead(file))
            using (var reader = new StreamReader(fs, Encoding))
            {
                for (int i = 0; i < 100; i++)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    foreach (var item in counts)
                    {
                        item.Value.Add(line.Count(x => x == item.Key));
                    }
                }
            }

            return counts.MaxKey(x => x.Value.Sum()).Key;
        }

        TableInfo ITabularDataTarget.GetRowFormat(IShellContext context)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return String.Format("[CsvFile {0}]", Name);
        }

        public override string ToStringCtx(IShellContext context)
        {
            return String.Format("[CsvFile {0}]", context.Replace(Name));
        }
    }
}
