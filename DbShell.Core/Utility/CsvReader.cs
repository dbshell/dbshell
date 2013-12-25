﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DbShell.Driver.Common.CommonDataLayer;
using DbShell.Driver.Common.Structure;
using DbShell.Driver.Common.Utility;

namespace DbShell.Core.Utility
{
    public class CsvReader : ArrayDataRecord, ICdlReader
    {
        private LumenWorks.Framework.IO.Csv.CsvReader _reader;
        private string[] _array;

        public CsvReader(TableInfo structure, LumenWorks.Framework.IO.Csv.CsvReader reader)
            : base(structure)
        {
            _reader = reader;
            _array = new string[structure.ColumnCount];
        }

        public bool Read()
        {
            if (_reader.ReadNextRecord())
            {
                _reader.CopyCurrentRecordTo(_array);
                for (int i = 0; i < _array.Length; i++)
                {
                    _values[i] = _array[i];
                }
                return true;
            }
            return false;
        }

        public bool NextResult()
        {
            return false;
        }

        public event Action Disposing;

        public void Dispose()
        {
            if (Disposing != null)
            {
                Disposing();
                Disposing = null;
            }
            _reader.Dispose();
        }
    }
}
