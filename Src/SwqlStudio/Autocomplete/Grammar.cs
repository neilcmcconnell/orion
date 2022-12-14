using System.Linq;

namespace SwqlStudio.Autocomplete
{
    public class Grammar
    {
        public static readonly string[] General =
        {
                "all",
                "any",
                "and",
                "as",
                "asc",
                "between",
                "class",
                "desc",
                "distinct",
                "exists",
                "false",
                "full",
                "group",
                "having",
                "in",
                "inner",
                "into",
                "is",
                "isa",
                "from",
                "join",
                "left",
                "like",
                "not",
                "null",
                "or",
                "outer",
                "right",
                "select",
                "set",
                "some",
                "true",
                "union",
                "where",
                "end",
                "when",
                "then",
                "else",
                "case",
                "on",
                "top",
                "return",
                "xml",
                "raw",
                "auto",
                "with",
                "limitation",
                "rows",
                "to",
                "order",
                "by",
                "desc",
                "totalrows",
                "noplancache",
                "queryplan",
                "querystats"

        };

        public static readonly string[] Functions =
            {
                "toutc",
                "tolocal",
                "getdate",
                "getutcdate",
                "datetime",
                "isnull",
                "tostring",
                "escapeswisurivalue",
                "splitstringtoarray",
                "floor",
                "round",
                "ceiling",
                "yeardiff",
                "monthdiff",
                "weekdiff",
                "daydiff",
                "hourdiff",
                "minutediff",
                "seconddiff",
                "milliseconddiff",
                "year",
                "quarterofyear",
                "dayofyear",
                "month",
                "week",
                "day",
                "hour",
                "minute",
                "second",
                "millisecond",
                "uriequals",
                "arraycontains",
                "datetrunc",
                "changetimezone",
                "toupper",
                "tolower",
                "concat",
                "substring",
                "adddate",
                "addyear",
                "addmonth",
                "addweek",
                "addday",
                "addhour",
                "addminute",
                "addsecond",
                "addmillisecond",
                "arraylength",
                "arrayvalueat",
                "filter"   // new in 2020.4
            };

        public static readonly string[] AggregateFunctions =
            {
                "min",
                "max",
                "avg",
                "count",
                "sum"
            };

        public static readonly string[] All = CreateOrdered();

        private static string[] CreateOrdered()
        {
            var source = General.ToList();
            source.AddRange(Functions);
            source.AddRange(AggregateFunctions);

            return source.Select(x => x.ToUpper())
                .OrderBy(x => x)
                .ToArray();
        }
    }
}
