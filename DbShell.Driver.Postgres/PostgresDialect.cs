﻿using DbShell.Driver.Common.AbstractDb;
using System;
using System.Collections.Generic;
using System.Text;

namespace DbShell.Driver.Postgres
{
    public class PostgresDialect : DialectBase
    {
        public PostgresDialect()
            : base(PostgresDatabaseFactory.Instance)
        {
        }

        public override char QuoteIdentBegin
        {
            get { return '"'; }
        }

        public override char QuoteIdentEnd
        {
            get { return '"'; }
        }

        public override char StringEscapeChar
        {
            get { return '\''; }
        }
    }
}
