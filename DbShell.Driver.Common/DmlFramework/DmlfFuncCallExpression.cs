using System.Collections.Generic;
using System.Xml;
using DbShell.Driver.Common.AbstractDb;
using System;

namespace DbShell.Driver.Common.DmlFramework
{
    public class DmlfFuncCallExpression : DmlfExpression
    {
        public List<DmlfExpression> Arguments = new List<DmlfExpression>();
        public string FuncName;

        public DmlfFuncCallExpression(string name, params DmlfExpression[] args)
        {
            FuncName = name;
            Arguments.AddRange(args);
        }

        public DmlfFuncCallExpression() { }
        public DmlfFuncCallExpression(XmlElement xml)
        {
            LoadFromXml(xml);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int res = 0;
                foreach(var arg in Arguments)
                {
                    res += arg.GetHashCode();
                }
                return res;
            }
        }

        public override bool DmlfEquals(DmlfBase obj)
        {
            var o = (DmlfFuncCallExpression) obj;
            if (o.Arguments.Count != Arguments.Count) return false;
            for (int i = 0; i < Arguments.Count; i++) if (!Arguments[i].DmlfEquals(o.Arguments[i])) return false;
            if (FuncName != o.FuncName) return false;
            return true;
        }

        public override void GenSql(ISqlDumper dmp)
        {
            dmp.Put(" %s(", FuncName);
            bool was = false;
            foreach (var arg in Arguments)
            {
                if (was) dmp.Put(",");
                was = true;
                arg.GenSql(dmp);
            }
            dmp.Put(")");
        }

        protected override string GetTypeName()
        {
            return "func";
        }

        public override object EvalExpression(IDmlfNamespace ns)
        {
            switch (FuncName.ToUpper())
            {
                case "LTRIM":
                    return Arguments[0].EvalExpression(ns)?.ToString()?.TrimStart();
                case "RTRIM":
                    return Arguments[0].EvalExpression(ns)?.ToString()?.TrimEnd();
                case "DATEPART":
                    string partName = Arguments[0].EvalExpression(ns)?.ToString();
                    var date = ExtractDate(Arguments[1].EvalExpression(ns));
                    switch (partName?.ToUpper() ?? "")
                    {
                        case "HOUR":
                            return date?.Hour;
                        case "MINUTE":
                            return date?.Minute;
                        case "SECOND":
                            return date?.Second;
                        case "YEAR":
                            return date?.Year;
                        case "MONTH":
                            return date?.Month;
                        case "DAY":
                            return date?.Day;
                    }
                    return 0;
                case "MONTH":
                    return ExtractDate(Arguments[0].EvalExpression(ns))?.Month;
                case "YEAR":
                    return ExtractDate(Arguments[0].EvalExpression(ns))?.Year;
            }
            return base.EvalExpression(ns);
        }
    }
}