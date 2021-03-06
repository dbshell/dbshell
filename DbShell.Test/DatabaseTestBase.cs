﻿using System;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using DbShell.All;
using DbShell.Core;
using DbShell.Core.Runtime;
using DbShell.Driver.Common.AbstractDb;
using DbShell.Driver.Common.Sql;
using DbShell.Driver.Common.Structure;
using DbShell.Test.EngineProviders;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace DbShell.Test
{
    public class DatabaseTestBase
    {
        protected IDatabaseEngineProvider _engineProvider;

        protected string ProviderConnectionString => _engineProvider.ProviderConnectionString;
        protected DbConnection OpenConnection() => _engineProvider.OpenConnection();

        protected void Initialize(string engine)
        {
            _engineProvider = EngineProviderFactory.GetProvider(engine);
            _engineProvider.CreateDatabase();
        }

        protected IDatabaseFactory DatabaseFactory
        {
            get
            {
                var provider = ConnectionProvider.FromString(DbShellUtility.BuildDefaultServiceProvider(), ProviderConnectionString);
                return provider.Factory;
            }
        }

        protected ShellRunner CreateRunner()
        {
            var runner = new ShellRunner(TestUtility.BuildServiceProvider());
            runner.Context.SetDefaultConnection(_engineProvider.ProviderConnectionString);
            return runner;
        }

        public DatabaseTestBase()
        {
        }

        public void RunScript(string sql)
        {
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public object ExecuteScalar(string sql)
        {
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    return cmd.ExecuteScalar();
                }
            }
        }

        public void AssertIsNotNull(string sql)
        {
            object value = ExecuteScalar(sql);
            Assert.True(value != null && value != DBNull.Value);
        }

        public void AssertIsNull(string sql)
        {
            object value = ExecuteScalar(sql);
            Assert.True(value == null || value == DBNull.Value);
        }

        public void AssertIsValue(string svalue, string sql)
        {
            object value = ExecuteScalar(sql);
            Assert.True(value?.ToString() == svalue);
        }

        public void AssertExists(string sql)
        {
            string existSql = $"select case when exists({sql}) then 1 else 0 end";
            AssertIsValue("1", existSql);
        }

        protected DatabaseInfo FullAnalyse()
        {
            using (var conn = OpenConnection())
            {
                var factory = DatabaseFactory;
                var analyser = factory.CreateAnalyser();
                analyser.Connection = conn;
                analyser.FullAnalysis();
                return analyser.Structure;
            }
        }

        protected DatabaseChangeSet GetModifications(DatabaseInfo dbInfo)
        {
            using (var conn = OpenConnection())
            {
                var factory = DatabaseFactory;
                var analyser = factory.CreateAnalyser();
                analyser.Connection = conn;
                analyser.Structure = dbInfo;
                analyser.GetModifications();
                return analyser.ChangeSet;
            }
        }

        protected DatabaseInfo IncrementalAnalysis(DatabaseInfo dbInfo, DatabaseChangeSet changeSet)
        {
            using (var conn = OpenConnection())
            {
                var factory = DatabaseFactory;
                var analyser = factory.CreateAnalyser();
                analyser.Connection = conn;
                analyser.Structure = dbInfo;
                analyser.ChangeSet = changeSet;
                analyser.IncrementalAnalysis();
                return analyser.Structure;
            }
        }

        protected DatabaseInfo GetModificationsAndIncrementalAnalysis(DatabaseInfo dbInfo)
        {
            var changeSet = GetModifications(dbInfo);
            return IncrementalAnalysis(dbInfo, changeSet);
        }

        protected string GenerateSqlScript(Action<ISqlDumper> dmpFunc)
        {
            var sw = new StringWriter();
            var sqlo = new SqlOutputStream(DatabaseFactory.CreateDialect(), sw, new SqlFormatProperties());
            var dmp = DatabaseFactory.CreateDumper(sqlo, new SqlFormatProperties());
            dmpFunc(dmp);
            return sw.ToString();
        }

        protected void RunSqlScript(Action<ISqlDumper> dmpFunc)
        {
            using (var conn = OpenConnection())
            {
                var sqlo = new ConnectionSqlOutputStream(conn, null, DatabaseFactory.CreateDialect());
                var dmp = DatabaseFactory.CreateDumper(sqlo, new SqlFormatProperties());
                dmpFunc(dmp);
            }
        }
    }
}
