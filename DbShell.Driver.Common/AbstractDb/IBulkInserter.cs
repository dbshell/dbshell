﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using DbShell.Driver.Common.CommonDataLayer;
using DbShell.Driver.Common.Structure;
using DbShell.Driver.Common.Utility;

namespace DbShell.Driver.Common.AbstractDb
{
    public interface IBulkInserter
    {
        TableInfo DestinationTable { get; set; }
        int BatchSize { get; set; }
        string DatabaseName { get; set; }
        LinkedDatabaseInfo LinkedInfo { get; set; }
        DbConnection Connection { get; set; }
        IDatabaseFactory Factory { get; set; }
        void Run(ICdlReader reader);
        CopyTableTargetOptions CopyOptions { get; set; }
        DataFormatSettings SourceDataFormat { get; set; }
        IServiceProvider ServiceProvider { get; set; }
        IMessageLogger MessageLogger { get; set; }
    }
}
