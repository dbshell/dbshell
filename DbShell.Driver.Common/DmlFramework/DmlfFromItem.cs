using System;
using System.Collections.Generic;
using System.Linq;
using DbShell.Driver.Common.AbstractDb;
using DbShell.Driver.Common.Structure;
using DbShell.Driver.Common.Utility;

namespace DbShell.Driver.Common.DmlFramework
{
    public class DmlfFromItem : DmlfBase
    {
        public DmlfFromItem()
        {
            Relations = new DmlfRelationCollection();
        }

        /// <summary>
        /// can be null (implicit source), than Handler.GetStructure(null) must be implemented
        /// </summary>
        [XmlSubElem]
        public DmlfSource Source { get; set; }
        [XmlCollection(typeof(DmlfRelation), "Relations")]
        public DmlfRelationCollection Relations { get; set; }

        /// <summary>
        /// finds column in joined tables
        /// </summary>
        /// <param name="expr">column expression</param>
        /// <returns>column reference or null, if not found</returns>
        public DmlfColumnRef FindColumn(string expr)
        {
            return null;
        }

        public override void ForEachChild(Action<IDmlfNode> action)
        {
            base.ForEachChild(action);
            if (Source != null) Source.ForEachChild(action);
            if (Relations != null) Relations.ForEachChild(action);
        }

        public override void GenSql(ISqlDumper dmp, IDmlfHandler handler)
        {
            var src = Source;
            if (src == null) src = handler.BaseTable;
            src.GenSqlDef(dmp, handler);
            dmp.Put(" ");
            Relations.GenSql(dmp, handler);
        }

        public DmlfSource FindSourceWithAlias(string alias)
        {
            if (Source != null && Source.Alias == alias) return Source;
            return Relations.FindSourceWithAlias(alias);
        }

        public IEnumerable<DmlfSource> GetAllSources()
        {
            if (Source != null) yield return Source;
            else yield return DmlfSource.BaseTable;
            foreach (var rel in Relations)
            {
                if (rel.Reference != null)
                {
                    yield return rel.Reference;
                }
            }
        }

        private DmlfSource DoAddOrFindRelation(DmlfSource baseSource, NameWithSchema baseTable, StructuredIdentifier relationJoined, StructuredIdentifier relationToJoin, DatabaseInfo db)
        {
            if (relationToJoin.IsEmpty) return baseSource;
            string relName = relationToJoin.First;
            string alias = String.Format("_REF{0}_{1}", relationJoined.NameItems.Select(x => "_" + x).CreateDelimitedText(""), relName);
            var source = FindSourceByAlias(alias);
            if (source == null)
            {
                var baseTableInfo = db.FindTable(baseTable);
                var fk = baseTableInfo.ForeignKeys.FirstOrDefault(x => System.String.Compare(x.ConstraintName, relName, StringComparison.OrdinalIgnoreCase) == 0);
                if (fk == null)
                {
                    var column = baseTableInfo.FindColumn(relName);
                    if (column != null) fk = column.GetForeignKeys().FirstOrDefault(x => x.Columns.Count == 1);
                }
                if (fk == null) return null;

                source = new DmlfSource
                    {
                        TableOrView = fk.RefTableFullName,
                        Alias = alias,
                    };
                var relation = new DmlfRelation
                    {
                        Reference = source,
                        JoinType = DmlfJoinType.Left,
                    };
                for (int i = 0; i < fk.Columns.Count; i++)
                {
                    relation.Conditions.Add(new DmlfEqualCondition
                        {
                            LeftExpr = new DmlfColumnRefExpression
                                {
                                    Column = new DmlfColumnRef
                                        {
                                            ColumnName = fk.Columns[0].RefColumnName,
                                            Source = baseSource,
                                        }
                                },
                            RightExpr = new DmlfColumnRefExpression
                                {
                                    Column = new DmlfColumnRef
                                        {
                                            ColumnName = fk.RefColumns[0].RefColumnName,
                                            Source = source,
                                        }
                                },
                        });
                    Relations.Add(relation);
                }
            }
            if (relationToJoin.IsEmpty) return source;
            return DoAddOrFindRelation(source, source.TableOrView, relationJoined/relationToJoin.First, relationToJoin.WithoutFirst, db);
        }

        private DmlfSource FindSourceByAlias(string alias)
        {
            foreach (var rel in Relations)
            {
                if (rel.Reference != null && rel.Reference.Alias == alias) return rel.Reference;
            }
            return null;
        }

        //TODO asi zrusit IDmlfHandler (potreba jen pro evaluate, to vytahnout do jineho interfface)
        public DmlfSource AddOrFindRelation(DmlfSource baseSource, NameWithSchema baseTable, StructuredIdentifier relationId, DatabaseInfo db)
        {
            return DoAddOrFindRelation(baseSource, baseTable, new StructuredIdentifier(), relationId, db);
        }

        public DmlfColumnRef GetColumnRef(DmlfSource baseSource, NameWithSchema baseTable, StructuredIdentifier columnId, DatabaseInfo db)
        {
            var relationId = columnId.WithoutLast;
            string column = columnId.Last;
            var source = AddOrFindRelation(baseSource, baseTable, relationId, db);
            if (source == null) return null;
            return new DmlfColumnRef
                {
                    ColumnName = column,
                    Source = source,
                };
        }
    }
}