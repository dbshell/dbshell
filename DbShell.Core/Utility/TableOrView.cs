﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DbShell.Core.RazorModels;
using DbShell.Driver.Common.AbstractDb;
using DbShell.Driver.Common.CommonDataLayer;
using DbShell.Driver.Common.Structure;
using DbShell.Driver.Common.Utility;
using DbShell.Driver.Common.Interfaces;

namespace DbShell.Core.Utility
{
    /// <summary>
    /// Table in database
    /// </summary>
    public abstract class TableOrView : ElementBase, ITabularDataSource, ITabularDataTarget, IListProvider, IModelProvider, ISingleValueDbShellObject
    {
        /// <summary>
        /// Table schema, can be ommited (eg. "dbo" on SQL server)
        /// </summary>
        [XamlProperty]
        public string Schema { get; set; }

        /// <summary>
        /// Table name
        /// </summary>
        [XamlProperty]
        public string Name { get; set; }

        /// <summary>
        /// Linked server name
        /// </summary>
        [XamlProperty]
        public string LinkedServerName { get; set; }

        /// <summary>
        /// Database name on linked server
        /// </summary>
        [XamlProperty]
        public string LinkedDatabaseName { get; set; }

        /// <summary>
        /// Explicit database name
        /// </summary>
        [XamlProperty]
        public string ExplicitDatabaseName { get; set; }

        // fill this if structure cannot be parsed
        public TableInfo StructureOverride;

        public LinkedDatabaseInfo LinkedInfo
        {
            get
            {
                if (!String.IsNullOrEmpty(ExplicitDatabaseName)) return new LinkedDatabaseInfo(ExplicitDatabaseName);
                return new LinkedDatabaseInfo(LinkedServerName, LinkedDatabaseName);
            }
            set
            {
                if (value == null)
                {
                    LinkedServerName = null;
                    LinkedDatabaseName = null;
                    ExplicitDatabaseName = null;
                }
                else
                {
                    LinkedServerName = value.LinkedServerName;
                    LinkedDatabaseName = value.LinkedDatabaseName;
                    ExplicitDatabaseName = value.ExplicitDatabaseName;
                }
            }
        }

        public NormalizedDatabaseConnectionInfo GetNormalizedConnectionInfo(IShellContext context)
        {
            return new NormalizedDatabaseConnectionInfo(new DatabaseConnectionInfoHolder
            {
                ProviderString = GetProviderString(context),
                LinkedDatabaseName = LinkedDatabaseName,
                LinkedServerName = LinkedServerName,
                ExplicitDatabaseName = ExplicitDatabaseName,
            });
        }

        DataFormatSettings ITabularDataSource.GetSourceFormat(IShellContext context)
        {
            return null;
        }

        protected abstract TableInfo GetRowFormat(IShellContext context);

        public NameWithSchema GetFullName(IShellContext context)
        {
            if (context == null) return new NameWithSchema(Schema, Name);
            return new NameWithSchema(context.Replace(Schema), context.Replace(Name));
        }

        TableInfo ITabularDataSource.GetRowFormat(IShellContext context)
        {
            return GetRowFormat(context);

        }

        ICdlReader ITabularDataSource.CreateReader(IShellContext context)
        {
            var fullName = GetFullName(context);
            var connection = GetConnectionProvider(context);
            var dda = connection.Factory.CreateDataAdapter();
            var conn = connection.Connect();
            var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 3600;
            var dialect = connection.Factory.CreateDialect();
            cmd.CommandText = "SELECT * FROM " + LinkedInfo + dialect.QuoteFullName(fullName);
            var reader = cmd.ExecuteReader();
            var result = dda.AdaptReader(reader, command: cmd);
            result.Disposing += () =>
                {
                    reader.Dispose();
                    conn.Dispose();
                };
            return result;
        }


        bool ITabularDataTarget.IsAvailableRowFormat(IShellContext context)
        {
            return true;
        }

        ICdlWriter ITabularDataTarget.CreateWriter(TableInfo inputRowFormat, CopyTableTargetOptions options, IShellContext context, DataFormatSettings sourceDataFormat)
        {
            return new TableWriter(context, GetConnectionProvider(context), GetFullName(context), inputRowFormat, options, StructureOverride, LinkedInfo, sourceDataFormat);
        }

        TableInfo ITabularDataTarget.GetRowFormat(IShellContext context)
        {
            return GetRowFormat(context);
        }

        IEnumerable IListProvider.GetList(IShellContext context)
        {
            using (var reader = ((ITabularDataSource)this).CreateReader(context))
            {
                while (reader.Read())
                {
                    yield return reader;
                }
            }
        }

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    return ((IListProvider)this).GetList().GetEnumerator();
        //}

        object IModelProvider.GetModel(IShellContext context)
        {
            return this;
        }

        void IModelProvider.InitializeTemplate(IRazorTemplate template, IShellContext context)
        {
            template.TabularData = this;
            template.Name = Name;
            template.Schema = Schema;
        }

        protected abstract string XamlExtensionName { get; }

        object ISingleValueDbShellObject.SingleValue
        {
            get => Name;
            set => Name = value?.ToString();
        }

        public override string ToXamlExtension()
        {
            if (!String.IsNullOrEmpty(LinkedServerName)) return null;
            if (!String.IsNullOrEmpty(LinkedDatabaseName)) return null;
            if (!String.IsNullOrEmpty(ExplicitDatabaseName)) return null;
            if (!String.IsNullOrEmpty(Connection)) return null;

            return $"{{{XamlExtensionName} '{GetFullName(null).ToStructuredString()}'}}";
        }
    }
}
