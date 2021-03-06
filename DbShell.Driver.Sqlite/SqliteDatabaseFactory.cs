﻿using System;
using System.Collections.Generic;
using System.Data.Common;
#if !NETSTANDARD2_0
using System.Data.SQLite;
#else
using SQLiteConnection = Microsoft.Data.Sqlite.SqliteConnection;
#endif
using System.Linq;
using System.Text;
using DbShell.Driver.Common.AbstractDb;
using DbShell.Driver.Common.DbDiff;
using DbShell.Driver.Common.Utility;

namespace DbShell.Driver.Sqlite
{
    public class SqliteDatabaseFactory : DatabaseFactoryBase
    {
        public static readonly SqliteDatabaseFactory InternalInstance = new SqliteDatabaseFactory(GenericServicesProvider.InternalInstance);

        public SqliteDatabaseFactory(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override string[] Identifiers
        {
            get { return new string[] { "sqlite" }; }
        }

        public override DbConnection CreateConnection(string connectionString)
        {
            return new SQLiteConnection(connectionString);
        }

        public override Type[] ConnectionTypes
        {
            get { return new Type[] { typeof(SQLiteConnection) }; }
        }

        public override ISqlDumper CreateDumper(ISqlOutputStream stream, SqlFormatProperties props)
        {
            return new SqliteSqlDumper(stream, this, props);
        }

        public override ILiteralFormatter CreateLiteralFormatter()
        {
            return new SqliteLiteralFormatter(this);
        }

        public override ISqlDialect CreateDialect()
        {
            return new SqliteDialect(this);
        }

        public override DatabaseAnalyser CreateAnalyser()
        {
            return new SqliteAnalyser
            {
                Factory = this,
            };
        }

        public override SqlDialectCaps DialectCaps
        {
            get
            {
                var res = base.DialectCaps;
                res.MultiCommand = true;
                res.ForeignKeys = true;
                res.Uniques = false;
                res.MultipleSchema = false;
                res.MultipleDatabase = false;
                res.UncheckedReferences = true;
                res.NestedTransactions = true;
                res.AnonymousPrimaryKey = true;
                res.RangeSelect = true;
                res.AllowDeleteFrom = false;
                res.AllowUpdateFrom = false;
                res.SupportsKeyInfo = false;
                res.RowId = "rowid";
                res.EnableConstraintsPerTable = false;
                res.AllowSchemaOnlyReader = false;
                res.ComputedColumns = false;
                return res;
            }
        }

        public override SqlDumperCaps DumperCaps
        {
            get
            {
                return new SqlDumperCaps
                {
                    AllFlags = false,
                    CreateTable = true,
                    DropTable = true,
                    RenameTable = true,
                    RecreateTable = true,
                    AddIndex = true,
                    DropIndex = true,
                    AddColumn = true,
                };
            }
        }
    }
}
