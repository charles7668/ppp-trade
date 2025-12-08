using System.Text.RegularExpressions;

namespace ppp_trade.Models.Parsers;

internal static class ParserHelper
{
    public static string TrimEndOfBraces(string input)
    {
        const string regex = @"\(.*?\)";
        return Regex.Replace(input, regex, "").Trim();
    }

    public static (bool, int?, int?) TryResolveIncreasedAndDecreased(Stat stat, string statText, ItemBase itemBase)
    {
        // try match normal case
        var regex = stat.Text.Replace("#", "(\\d+)");
        var match = Regex.Match(statText, regex);
        if (match.Success)
        {
            return (true, int.Parse(match.Groups[1].Value), null);
        }

        regex = stat.Text.Replace("增加", "減少").Replace("#", "(\\d+)");
        match = Regex.Match(statText, regex);
        if (match.Success)
        {
            return (true, int.Parse(match.Groups[1].Value) * -1, null);
        }

        return (false, null, null);
    }

    public static (bool, int?, int?) TryResolveIncreasedAndDecreasedEn(Stat stat, string statText, ItemBase itemBase)
    {
        // try match normal case
        var regex = stat.Text.Replace("#", "(\\d+)");
        var match = Regex.Match(statText, regex);
        if (match.Success)
        {
            return (true, int.Parse(match.Groups[1].Value), null);
        }

        regex = stat.Text.Replace("increased", "decreased").Replace("#", "(\\d+)");
        match = Regex.Match(statText, regex);
        if (match.Success)
        {
            return (true, int.Parse(match.Groups[1].Value) * -1, null);
        }

        return (false, null, null);
    }
}