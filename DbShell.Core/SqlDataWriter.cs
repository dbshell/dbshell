﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DbShell.Driver.Common.Interfaces;
using DbShell.Core.Utility;
using DbShell.Driver.Common.AbstractDb;
using DbShell.Driver.Common.CommonDataLayer;
using DbShell.Driver.Common.Structure;
using DbShell.Driver.Common.Utility;

namespace DbShell.Core
{
    public class SqlDataWriter : ElementBase, ITabularDataTarget
    {
        /// <summary>
        ///  name of input file
        /// </summary>
        [XamlProperty]
        public string File { get; set; }

        [XamlProperty]
        public int? InsertSeparatorAfterRows { get; set; }

        public bool IsAvailableRowFormat(IShellContext context)
        {
            return false;
        }

        public ICdlWriter CreateWriter(TableInfo rowFormat, CopyTableTargetOptions options, IShellContext context, DataFormatSettings sourceDataFormat)
        {
            string file = context.ResolveFile(context.Replace(File), ResolveFileMode.Output);
            var fw = new StreamWriter(System.IO.File.OpenWrite(file));
            var provider = GetConnectionProvider(context);
            return new SqlFileWriter(fw, provider.Factory, InsertSeparatorAfterRows);
        }

        public TableInfo GetRowFormat(IShellContext context)
        {
            return null;
        }
    }
}
