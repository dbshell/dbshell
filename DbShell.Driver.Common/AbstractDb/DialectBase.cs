using System;
using System.Text;
using DbShell.Driver.Common.CommonTypeSystem;
using DbShell.Driver.Common.DbDiff;
using DbShell.Driver.Common.Sql;
using DbShell.Driver.Common.Structure;
using DbShell.Driver.Common.Utility;

namespace DbShell.Driver.Common.AbstractDb
{
    public class DialectBase : ISqlDialect
    {
        private HashSetEx<string> _keywords;
        private IDatabaseFactory _factory;

        public DialectBase(IDatabaseFactory factory)
        {
            _factory = factory;
        }

        public virtual HashSetEx<string> Keywords
        {
            get
            {
                var res = KeywordsProvider.InvokeGetKeywords(this);
                if (res != null) return res;
                if (_keywords == null) _keywords = LoadKeywords();
                return _keywords;
            }
        }

        public string StripComments(string content)
        {
            var sb = new StringBuilder();
            foreach (string line in content.Split('\n'))
            {
                int commentPos = line.IndexOf("--", System.StringComparison.Ordinal);
                string newLine = line;
                if (commentPos >= 0)
                {
                    newLine = line.Substring(0, commentPos);
                    if (String.IsNullOrWhiteSpace(newLine)) newLine = null;
                }
                if (newLine != null) sb.AppendLine(newLine);
            }
            return sb.ToString();
        }

        public IDatabaseFactory Factory
        {
            get { return _factory; }
        }

        public virtual Type SpecificTypeEnum
        {
            get { return typeof(DbTypeCode); }
        }

        //public virtual DbTypeBase CreateCommonType(ColumnInfo column)
        //{
        //    return new DbTypeString();
        //}

        protected virtual HashSetEx<string> LoadKeywords()
        {
            var res = new HashSetEx<string>();
            res.Add("SELECT"); res.Add("CREATE"); res.Add("UPDATE"); res.Add("DELETE");
            res.Add("DROP"); res.Add("ALTER"); res.Add("TABLE"); res.Add("VIEW");
            res.Add("FROM"); res.Add("WHERE"); res.Add("GROUP"); res.Add("ORDER"); res.Add("BY");
            res.Add("ASC"); res.Add("DESC"); res.Add("HAVING"); res.Add("INTO"); res.Add("ASC");
            res.Add("LEFT"); res.Add("RIGHT"); res.Add("INNER"); res.Add("OUTER");
            res.Add("CROSS"); res.Add("NATURAL"); res.Add("JOIN"); res.Add("ON");
            res.Add("DISTINCT"); res.Add("ALL"); res.Add("ANY");
            return res;
        }

        /// <summary>
        /// how ' character is quoted in string
        /// </summary>
        public virtual char StringEscapeChar
        {
            get { return '\\'; }
        }

        /// <summary>
        /// begin of quoted identifier
        /// </summary>
        public virtual char QuoteIdentBegin
        {
            get { return '"'; }
        }

        /// <summary>
        /// end of quoted identifier
        /// </summary>
        public virtual char QuoteIdentEnd
        {
            get { return '"'; }
        }

        public virtual string QuoteIdentifier(string ident)
        {
            //if (ident.IndexOf('.') >= 0) return ident;
            if (QuoteIdentBegin != '\0' && QuoteIdentEnd != '\0') return QuoteIdentBegin + ident + QuoteIdentEnd;
            return ident;
        }

        public string QuoteIdentifierIfNecessary(string ident)
        {
            if (SqlDumper.MustBeQuoted(ident, this)) return QuoteIdentifier(ident);
            return ident;
        }

        public virtual string UnquoteName(string name)
        {
            int dstart = 0, dend = 0;
            if (QuoteIdentBegin != '\0' && name.Length > 0 && name[0] == QuoteIdentBegin) dstart = 1;
            if (QuoteIdentEnd != '\0' && name.Length > dstart && name[name.Length - 1] == QuoteIdentEnd) dend = 1;
            if (dstart > 0 || dend > 0) return name.Substring(dstart, name.Length - dstart - dend);
            return name;
        }
    }
}