﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbShell.Driver.Common.Structure
{
    public class FunctionInfo : ProgrammableInfo
    {
        public string ResultType { get; set; }

        public FunctionInfo(DatabaseInfo database)
            : base(database)
        {
        }

        public override DatabaseObjectType ObjectType
        {
            get { return DatabaseObjectType.Function; }
        }

        public FunctionInfo Clone(DatabaseInfo ownerDb = null)
        {
            var res = new FunctionInfo(ownerDb ?? OwnerDatabase);
            res.Assign(this);
            return res;
        }
    }
}
