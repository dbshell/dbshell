﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using DbShell.Core.Utility;
using DbShell.Driver.Common.AbstractDb;
using DbShell.Driver.Common.Sql;
using DbShell.Driver.Common.Utility;
using DbShell.Driver.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace DbShell.Core
{
    /// <summary>
    /// Job, which is aible to run SQL script, from file or given inline.
    /// </summary>
    public class Script : RunnableBase
    {
        /// <summary>
        /// Gets or sets the script file file.
        /// </summary>
        /// <value>
        /// The SQL file name. If this property is set, Command cannot be set.
        /// </value>
        [XamlProperty]
        public string File { get; set; }

        /// <summary>
        /// Gets or sets the SQL command.
        /// </summary>
        /// <value>
        /// The SQL command. If this property is set, File cannot be set.
        /// </value>
        [XamlProperty]
        public string Command { get; set; }

        /// <summary>
        /// Gets or sets the replace pattern.
        /// </summary>
        /// <value>
        /// The regular expression, which is used for replacing in scripts. By default, it is @"\$\{([^\}]+)\}"
        /// </value>
        [XamlProperty]
        public string ReplacePattern { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether use replacements.
        /// </summary>
        /// <value>
        ///   <c>true</c> if use replacements using ReplacePattern; otherwise, <c>false</c>. By default, true for inline Command, false for File
        /// </value>
        [XamlProperty]
        public bool? UseReplacements { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether use transaction.
        /// </summary>
        /// <value>
        ///   <c>true</c> if command executing is encosed in transaction; otherwise, <c>false</c>. Default is <c>false</c>
        /// </value>
        [XamlProperty]
        public bool UseTransactions { get; set; }

        /// <summary>
        /// timeout in seconds
        /// </summary>
        [XamlProperty]
        public int Timeout { get; set; }

        /// <summary>
        /// list of files
        /// </summary>
        [XamlProperty]
        public string[] Files { get; set; }

        /// <summary>
        /// file encoding
        /// </summary>
#if !NETSTANDARD2_0
        [TypeConverter(typeof(EncodingTypeConverter))]
#endif
        [XamlProperty]
        public Encoding FileEncoding { get; set; }

        public Script()
        {
            Timeout = 3600;
            FileEncoding = Encoding.UTF8;
        }

        private void RunScript(TextReader reader, DbConnection conn, DbTransaction tran, bool replace, bool logEachQuery, bool logCount, IShellContext context)
        {
            int count = 0;
            foreach (string item in GoSplitter.GoSplit(reader))
            {
                string sql = item;
                if (replace)
                {
                    sql = context.Replace(sql, ReplacePattern);
                }
                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandTimeout = Timeout;
                cmd.Transaction = tran;
                try
                {
                    if (logEachQuery)
                        context.GetLogger<Script>().LogInformation("DBSH-00064 Executing SQL command {sql}", sql);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception err)
                {
                    context.GetLogger<Script>().LogError(0, err, "DBSH-00065 Error when executing script {1}", sql);
                    throw;
                }
                count++;
            }
            if (logCount)
                context.GetLogger<Script>().LogInformation("DBSH-00073 Executed {0} commands", count);
        }

        protected override void DoRun(IShellContext context)
        {
            if (File != null && Command != null)
                throw new Exception("DBSH-00060 Both Script.File and Script.Command properties are set");
            if (File == null && Command == null && Files == null)
                throw new Exception("DBSH-00061 None of Script.File and Script.Command and Script.Files properties are set");

            var connection = GetConnectionProvider(context);
            using (var conn = connection.Connect())
            {
                DbTransaction tran = null;
                try
                {
                    if (UseTransactions)
                    {
                        context.Info("Opening transaction");
                        tran = conn.BeginTransaction();
                    }

                    // execute inline command
                    if (Command != null)
                    {
                        RunScript(new StringReader(Command), conn, tran, UseReplacements == null || UseReplacements == true, true, false, context);
                    }

                    // execute linked file
                    if (File != null)
                    {
                        string fn = context.ResolveFile(File, ResolveFileMode.Input);
                        using (var reader = new StreamReader(System.IO.File.OpenRead(fn), FileEncoding))
                        {
                            //context.GetLogger<Script>().LogInformation("DBSH-00067 Executing SQL file {file}", fn);
                            context.Info(String.Format("Executing SQL file {0}", fn));
                            RunScript(reader, conn, tran, UseReplacements == true, false, true, context);
                        }
                    }
                    if (Files != null)
                    {
                        foreach (string file in Files)
                        {
                            string fn = context.ResolveFile(file, ResolveFileMode.Input);
                            using (var reader = new StreamReader(System.IO.File.OpenRead(fn), FileEncoding))
                            {
                                context.Info(String.Format("Executing SQL file {0}", fn));
                                RunScript(reader, conn, tran, UseReplacements == true, false, true, context);
                            }
                        }
                    }

                    if (tran != null)
                    {
                        context.Info("Commiting transaction");
                        tran.Commit();
                    }
                }
                catch
                {
                    if (tran != null)
                    {
                        context.Info("Rollbacking transaction");
                        tran.Rollback();
                    }
                    throw;
                }
                finally
                {
                    if (tran != null)
                    {
                        tran.Dispose();
                    }
                }
            }
        }
    }
}
