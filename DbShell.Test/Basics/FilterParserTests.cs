﻿using DbShell.Driver.Common.DmlFramework;
using DbShell.Driver.Common.Structure;
using DbShell.Driver.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace DbShell.Test.Basics
{
    public class FilterParserTests
    {
        private bool TestCondition(FilterParserType type, string condition, object testedValue)
        {
            var cond = FilterParserTool.ParseFilterExpression(type, new DmlfPlaceholderExpression(), condition);
            var ns = new DmlfSingleValueNamespace(testedValue);
            Assert.NotNull(cond);
            return cond.EvalCondition(ns);
        }

        private void StringTrue(string condition, string testedString)
        {
            Assert.True(TestCondition(FilterParserType.String, condition, testedString));
        }

        private void StringFalse(string condition, string testedString)
        {
            Assert.False(TestCondition(FilterParserType.String, condition, testedString));
        }

        private void NumberTrue(string condition, string testedString)
        {
            Assert.True(TestCondition(FilterParserType.Number, condition, testedString));
        }

        private void NumberFalse(string condition, string testedString)
        {
            Assert.False(TestCondition(FilterParserType.Number, condition, testedString));
        }

        private void DateTimeTrue(string condition, DateTime value, bool stringTest=true)
        {
            Assert.True(TestCondition(FilterParserType.DateTime, condition, value));
            if (stringTest)
            {
                Assert.True(TestCondition(FilterParserType.DateTime, condition, value.ToString("s")));
            }
        }

        private void DateTimeFalse(string condition, DateTime value, bool stringTest = true)
        {
            Assert.False(TestCondition(FilterParserType.DateTime, condition, value));
            if (stringTest)
            {
                Assert.False(TestCondition(FilterParserType.DateTime, condition, value.ToString("s")));
            }
        }

        private void LogicalTrue(string condition, string testedString)
        {
            Assert.True(TestCondition(FilterParserType.Logical, condition, testedString));
        }

        private void LogicalFalse(string condition, string testedString)
        {
            Assert.False(TestCondition(FilterParserType.Logical, condition, testedString));
        }

        [Fact]
        public void TestStringFilter()
        {
            StringTrue("'val'", "val");
            StringTrue("'val'", "val1");

            StringTrue("=val", "val");
            StringFalse("=val", "val1");

            StringFalse("^1 $3", "124");
            StringTrue("^1 $3", "123");
            StringTrue("^1, $3", "124");

            StringTrue("EMPTY, NULL", "   ");
            StringFalse("NOT EMPTY", "   ");
        }

        [Fact]
        public void TestNumberFilter()
        {
            NumberTrue("='1'", "1");
            NumberTrue("=1", "1");
            NumberTrue("1", "1");
            NumberFalse("3", "1");

            NumberTrue(">=1 <=3", "2");
            NumberFalse(">=1 <=3", "4");

            NumberTrue("1-4", "4");
            NumberFalse("1-4", "5");

            NumberTrue("1,2,3", "3");
            NumberFalse("1,2,3", "4");

            NumberTrue("-5--1", "-3");
            NumberFalse("-5--1", "-6");
        }

        [Fact]
        public void TestDateTimeFilter()
        {
            DateTimeTrue("TODAY", DateTime.Now);
            DateTimeTrue("YESTERDAY", DateTime.Now.AddDays(-1));
            DateTimeTrue("TOMORROW", DateTime.Now.AddDays(1));

            DateTimeFalse("TODAY", DateTime.Now.AddDays(-1));
            DateTimeFalse("YESTERDAY", DateTime.Now.AddDays(1));
            DateTimeFalse("TOMORROW", DateTime.Now);

            DateTimeTrue("NEXT WEEK, TODAY", DateTime.Now.AddDays(7));
            DateTimeTrue("NEXT WEEK, TODAY", DateTime.Now);
            DateTimeFalse("NEXT WEEK, TODAY", DateTime.Now.AddDays(-1));

            DateTimeTrue("1.3.2016", new DateTime(2016, 3, 1));
            DateTimeTrue("2016-03-01", new DateTime(2016, 3, 1));
            DateTimeTrue("3/1/2016", new DateTime(2016, 3, 1));

            DateTimeTrue("1.3. 10:01", new DateTime(DateTime.Now.Year, 3, 1, 10, 1, 30));
            DateTimeFalse("1.3. 10:01", new DateTime(DateTime.Now.Year, 3, 1, 10, 0, 30));
            DateTimeTrue("1.3. 10:*", new DateTime(DateTime.Now.Year, 3, 1, 10, 25, 0));

            DateTimeTrue("MON", new DateTime(2017, 3, 27));
            DateTimeTrue("2017", new DateTime(2017, 3, 27));
            DateTimeTrue("MON 2017", new DateTime(2017, 3, 27));
            DateTimeFalse("MON 2017", new DateTime(2017, 3, 28));
            DateTimeTrue("2017 JAN", new DateTime(2017, 1, 28));

            DateTimeTrue("2016-03-05 15:23:33", new DateTime(2016, 3, 5, 15, 23, 33, 35));
            DateTimeTrue("2016-03-05 15:23:33.35", new DateTime(2016, 3, 5, 15, 23, 33, 350), false);
            DateTimeFalse("2016-03-05 15:23:33.35", new DateTime(2016, 3, 5, 15, 23, 33, 35));
            DateTimeTrue("2016-03-05 15:23", new DateTime(2016, 3, 5, 15, 23, 46));
            DateTimeFalse("2016-03-05 15:23", new DateTime(2016, 3, 5, 15, 24, 0));

            DateTimeTrue(">=2016-03-05 15:23", new DateTime(2016, 3, 5, 15, 23, 46));
            DateTimeFalse(">2016-03-05 15:23", new DateTime(2016, 3, 5, 15, 23, 46));
        }

        [Fact]
        public void TestLogicalFilter()
        {
            LogicalTrue("TRUE", "1");
            LogicalTrue("1", "1");
            LogicalTrue("FALSE", "0");
            LogicalTrue("0", "0");

            LogicalFalse("TRUE", "0");
        }

        private void ObjectTrue(string condition, FilterableObjectData obj)
        {
            var cond = FilterParserTool.ParseObjectFilter(condition);
            Assert.NotNull(cond);
            Assert.True(cond.Match(obj));
        }
        private void ObjectFalse(string condition, FilterableObjectData obj)
        {
            var cond = FilterParserTool.ParseObjectFilter(condition);
            Assert.NotNull(cond);
            Assert.False(cond.Match(obj));
        }

        [Fact]
        public void TestObjectFilter()
        {
            ObjectTrue("table", new FilterableObjectData { Name = "Table1" });
            ObjectTrue("TN", new FilterableObjectData { Name = "TableName" });
            ObjectFalse("TN", new FilterableObjectData { Name = "TableMediumName" });
            ObjectTrue("=table", new FilterableObjectData { Name = "table" });
            ObjectFalse("=table", new FilterableObjectData { Name = "table1" });
            ObjectTrue("#col1", new FilterableObjectData { ContentText = "col1" });
            ObjectFalse("#col1", new FilterableObjectData { ContentText = "col2" });
        }
    }
}