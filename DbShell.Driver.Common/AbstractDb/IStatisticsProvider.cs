﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using DbShell.Driver.Common.Statistics;

namespace DbShell.Driver.Common.AbstractDb
{
    public interface IStatisticsProvider
    {
        TableSizes GetTableSizes(DbConnection conn);
    }
}
