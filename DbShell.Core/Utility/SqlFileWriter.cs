﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DbShell.Driver.Common.AbstractDb;
using DbShell.Driver.Common.CommonDataLayer;
using DbShell.Driver.Common.Utility;

namespace DbShell.Core.Utility
{
    public class SqlFileWriter : ICdlWriter
    {
        private TextWriter _stream;
        private IDatabaseFactory _factory;
        private int? _insertSeparatorAfterRows;
        private int _rowsAfterSeparator;

        public SqlFileWriter(TextWriter stream, IDatabaseFactory factory, int? insertSeparatorAfterRows)
        {
            _stream = stream;
            _factory = factory;
            _insertSeparatorAfterRows = insertSeparatorAfterRows;
        }

        public void Dispose()
        {
            if (Disposing != null)
            {
                Disposing();
                Disposing = null;
            }
            _stream.Dispose();
        }

        public event Action Disposing;

        public void Write(ICdlRecord row)
        {
            if (_insertSeparatorAfterRows.HasValue && _rowsAfterSeparator > _insertSeparatorAfterRows.Value)
            {
                _stream.Write("GO\n");
                _rowsAfterSeparator = 0;
            }
            var fmt = _factory.CreateLiteralFormatter();
            _stream.Write("INSERT INTO [TABLE_NAME] (");
            _stream.Write(row.GetFieldNames().Select(x => "[" + x + "]").CreateDelimitedText(", "));
            _stream.Write(") VALUES (");
            for (int i = 0; i < row.FieldCount; i++)
            {
                if (i > 0) _stream.Write(", ");
                row.ReadValue(i);
                fmt.ReadFrom(row);
                _stream.Write(fmt.GetText());
            }
            _stream.Write(");\n");
            _rowsAfterSeparator++;
        }
    }
}
