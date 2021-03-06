using System.Collections.Generic;
using DbShell.Driver.Common.DbDiff;
using DbShell.Driver.Common.Utility;
using System.Runtime.Serialization;

namespace DbShell.Driver.Common.Structure
{
    public enum ForeignKeyAction
    {
        Undefined,
        NoAction,
        Cascade,
        Restrict,
        SetNull
    };

    [DataContract]
    public class ForeignKeyInfo : ColumnsConstraintInfo
    {
        [XmlAttrib("update_action")]
        [DataMember]
        public ForeignKeyAction OnUpdateAction { get; set; }

        [XmlAttrib("delete_action")]
        [DataMember]
        public ForeignKeyAction OnDeleteAction { get; set; }

        private List<ColumnReference> _refColumns = new List<ColumnReference>();

        public TableInfo RefTable { get; set; }
        [XmlCollection(typeof(ColumnReference))]
        [DataMember]
        public List<ColumnReference> RefColumns { get { return _refColumns; } }


        private string _refTableName;
        [XmlAttrib("ref_table")]
        [DataMember]
        public string RefTableName
        {
            get
            {
                if (RefTable != null) return RefTable.Name;
                return _refTableName;
            }
            set
            {
                _refTableName = value;
                RefTable = null;
            }
        }

        private string _refSchemaName;
        [XmlAttrib("ref_schema")]
        [DataMember]
        public string RefSchemaName
        {
            get
            {
                if (RefTable != null) return RefTable.Schema;
                return _refSchemaName;
            }
            set
            {
                _refSchemaName = value;
                RefTable = null;
            }
        }

        public NameWithSchema RefTableFullName
        {
            get { return new NameWithSchema(RefSchemaName, RefTableName); }
        }

        public ForeignKeyInfo(TableInfo table)
            :base(table)
        {
            
        }

        public override void Assign(DatabaseObjectInfo source)
        {
            base.Assign(source);

            var src = (ForeignKeyInfo) source;
            OnUpdateAction = src.OnUpdateAction;
            OnDeleteAction = src.OnDeleteAction;
            RefTableName = src.RefTableName;
            RefSchemaName = src.RefSchemaName;

            foreach(var col in src.RefColumns)
            {
                RefColumns.Add(col.Clone());
            }
        }

        public ForeignKeyInfo CloneForeignKey(TableInfo ownTable = null)
        {
            var res = new ForeignKeyInfo(ownTable ?? OwnerTable);
            res.Assign(this);
            return res;
        }

        public override DatabaseObjectInfo CloneObject(DatabaseObjectInfo owner)
        {
            return CloneForeignKey(owner as TableInfo);
        }

        public override DatabaseObjectType ObjectType
        {
            get { return DatabaseObjectType.ForeignKey; }
        }

        public override void AfterLoadLink()
        {
            base.AfterLoadLink();

            if (RefTable == null)
            {
                RefTable = OwnerDatabase.GetTable(new NameWithSchema(_refSchemaName, _refTableName));
                _refSchemaName = null;
                _refTableName = null;
            }

            if (RefTable != null)
            {
                foreach (var col in RefColumns)
                {
                    col.AfterLoadLink(RefTable);
                }
            }
            else
            {
                int x = 0;
            }
        }

        public override void SetDummyTable(NameWithSchema name)
        {
            var table = new TableInfo(null);
            table.FullName = name;
            table.ForeignKeys.Add(this);
        }
    }
}