﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DbShell.Core.DataSetModels;
using DbShell.Core.Utility;

namespace DbShell.Core.DataSet
{
    public class DataSet : DataSetItemBase
    {
        public string Name { get; set; }

        protected override void DoRun()
        {
            var dbs = GetDatabaseStructure();
            Context.SetVariable(DataSetVariableName, new DataSetModel(dbs, Context, Connection.Factory));
        }
    }
}
