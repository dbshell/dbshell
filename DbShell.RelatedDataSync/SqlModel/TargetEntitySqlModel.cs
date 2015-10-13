﻿using DbShell.Common;
using DbShell.Driver.Common.DmlFramework;
using DbShell.Driver.Common.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using DbShell.Driver.Common.AbstractDb;
using DbShell.Driver.Common.Sql;

namespace DbShell.RelatedDataSync.SqlModel
{
    public class TargetEntitySqlModel
    {
        private DataSyncSqlModel _dataSyncSqlModel;
        private Target _dbsh;
        public NameWithSchema TargetTable;
        public HashSet<SourceColumnSqlModel> RequiredSourceColumns = new HashSet<SourceColumnSqlModel>();
        public HashSet<SourceColumnSqlModel> KeySourceColumns = new HashSet<SourceColumnSqlModel>();
        public List<TargetColumnSqlModel> TargetColumns = new List<TargetColumnSqlModel>();
        public SourceJoinSqlModel SourceJoinModel;
        public bool RequiresGrouping;

        public TargetEntitySqlModel(DataSyncSqlModel dataSyncSqlModel, Target dbsh, IShellContext context)
        {
            this._dataSyncSqlModel = dataSyncSqlModel;
            this._dbsh = dbsh;
            TargetTable = new NameWithSchema(context.Replace(dbsh.TableSchema), context.Replace(dbsh.TableName));

            foreach (var col in dbsh.Columns)
            {
                var targetCol = new TargetColumnSqlModel(col);
                TargetColumns.Add(targetCol);

                foreach (string alias in ExtractColumnSources(col))
                {
                    var source = dataSyncSqlModel.SourceGraphModel[alias];
                    RequiredSourceColumns.Add(source);
                    if (col.IsKey) KeySourceColumns.Add(source);
                    targetCol.Sources.Add(source);
                }
            }

            if (!KeySourceColumns.Any())
            {
                throw new Exception($"DBSH-00000 Entity {dbsh.TableName} has no source for key");
            }

            SourceJoinModel = new SourceJoinSqlModel(this, dataSyncSqlModel.SourceGraphModel);

            RequiresGrouping = DetectGrouping();
        }

        private bool DetectGrouping()
        {
            // if primary source is not defined, group=TRUE
            if (SourceJoinModel.PrimarySource == null) return true;
            // if primary source has not key, group=TRUE
            if (!SourceJoinModel.PrimarySource.KeyColumns.Any()) return true;

            // all columns is source key must be in entity key
            foreach (var col in SourceJoinModel.PrimarySource.KeyColumns)
            {
                if (!KeySourceColumns.Any(x => x.Alias == col.Alias)) return true;
            }

            // source key is covered by this key => grouping is not required
            return false;
        }

        public Target Dbsh
        {
            get { return _dbsh; }
        }

        private IEnumerable<string> ExtractColumnSources(TargetColumn col)
        {
            if (col.RealValueType == TargetColumnValueType.Source)
            {
                yield return col.SourceName;
            }
        }

        private DmlfInsertSelect CompileInsert()
        {
            var res = new DmlfInsertSelect();
            res.TargetTable = TargetTable;
            res.Select = new DmlfSelect();
            res.Select.From.Add(SourceJoinModel.SourceJoin);

            foreach (var col in TargetColumns)
            {
                res.TargetColumns.Add(col.Name);
                var expr = col.CreateSourceExpression(SourceJoinModel, RequiresGrouping && !col.IsKey);
                res.Select.Columns.Add(new DmlfResultField
                {
                    Expr = expr,
                });
            }

            if (RequiresGrouping)
            {
                res.Select.GroupBy = new DmlfGroupByCollection();
                foreach(var col in TargetColumns.Where(x => x.IsKey))
                {
                    var expr = col.CreateSourceExpression(SourceJoinModel, false);
                    res.Select.GroupBy.Add(new DmlfGroupByItem
                    {
                        Expr = expr,
                    });
                }
            }

            var existSelect = new DmlfSelect();
            existSelect.SingleFrom.Source = new DmlfSource
            {
                TableOrView = TargetTable,
                Alias = "tested",
            };
            existSelect.SelectAll = true;
            CreateKeyCondition(existSelect, "tested");

            res.Select.AddAndCondition(new DmlfNotExistCondition { Select = existSelect });

            return res;
        }

        private void CreateKeyCondition(DmlfCommandBase cmd, string targetEntityAlias)
        {
            foreach (var column in TargetColumns.Where(x => x.IsKey))
            {
                var cond = new DmlfEqualCondition
                {
                    // target columns
                    LeftExpr = new DmlfColumnRefExpression
                    {
                        Column = new DmlfColumnRef
                        {
                            Source = new DmlfSource { Alias = targetEntityAlias },
                            ColumnName = column.Name,
                        }
                    },

                    // source column
                    RightExpr = column.CreateSourceExpression(SourceJoinModel, false),
                };
                cmd.AddAndCondition(cond);
            }
        }

        private DmlfUpdate CompileUpdate()
        {
            var res = new DmlfUpdate();
            res.UpdateTarget = new DmlfSource { Alias = "target" };
            res.From.Add(SourceJoinModel.SourceJoin);
            res.From.Add(new DmlfFromItem
            {
                Source = new DmlfSource
                {
                    Alias = "target",
                    TableOrView = TargetTable,
                }
            });
            CreateKeyCondition(res, "target");
            foreach (var column in TargetColumns.Where(x => !x.IsKey))
            {
                res.Columns.Add(new DmlfUpdateField
                {
                    TargetColumn = column.Name,
                    Expr = column.CreateSourceExpression(SourceJoinModel, false),
                });
            }
            if (!res.Columns.Any()) return null;
            return res;
        }

        private DmlfDelete CompileDelete()
        {
            var res = new DmlfDelete();
            res.DeleteTarget = new DmlfSource { Alias = "target" };
            res.SingleFrom.Source = new DmlfSource
            {
                Alias = "target",
                TableOrView = TargetTable,
            };
            var existSelect = new DmlfSelect();
            res.AddAndCondition(new DmlfNotExistCondition
            {
                Select = existSelect,
            });
            existSelect.SelectAll = true;
            existSelect.From.Add(SourceJoinModel.SourceJoin);
            CreateKeyCondition(existSelect, "target");
            return res;
        }

        public void Run(ISqlDumper dmp)
        {
            var insert = CompileInsert();
            if (insert != null)
            {
                insert.GenSql(dmp);
                dmp.EndCommand();
            }

            var update = CompileUpdate();
            if (update != null)
            {
                update.GenSql(dmp);
                dmp.EndCommand();
            }

            var delete = CompileDelete();
            if (delete != null)
            {
                delete.GenSql(dmp);
                dmp.EndCommand();
            }
        }

        public void Run(DbConnection conn, IDatabaseFactory factory)
        {
            var so = new ConnectionSqlOutputStream(conn, null, factory.CreateDialect());
            var dmp = factory.CreateDumper(so, new SqlFormatProperties());
            Run(dmp);
        }
    }
}
