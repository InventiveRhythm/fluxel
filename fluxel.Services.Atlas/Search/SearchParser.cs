/*
using System;
using System.Text.RegularExpressions;

namespace fluxel.Search;

public static class SearchParser
{
    private static readonly Regex regex = new("""\b(?<k>\w+)(?<o>(=|(>|<)(=)?))(?<v>(".*"[!]?)|(\w*))""");

    public static void Parse<T, U>(T filters, string query)
        where T : SearchFilters<U>
    {
        var matches = regex.Matches(query);

        foreach (Match match in matches)
        {
            var key = match.Groups["k"].Value.ToLowerInvariant();
            var op = parseOperator(match.Groups["o"].Value);
            var value = match.Groups["v"].Value.ToLowerInvariant();

            if (filters.ParseKeyword(key, value, op))
                query = query.Replace(match.ToString(), "");
        }

        filters.SearchText = query;
    }

    private static SearchOperator parseOperator(string value)
    {
        switch (value)
        {
            case "=":
                return SearchOperator.Equal;

            case "<":
                return SearchOperator.Less;

            case "<=":
                return SearchOperator.LessOrEqual;

            case ">":
                return SearchOperator.Greater;

            case ">=":
                return SearchOperator.GreaterOrEqual;

            default:
                throw new ArgumentOutOfRangeException(nameof(value), $"Unsupported operator {value}");
        }
    }
}

public enum SearchOperator
{
    Equal,
    Less,
    LessOrEqual,
    Greater,
    GreaterOrEqual
}
*/
