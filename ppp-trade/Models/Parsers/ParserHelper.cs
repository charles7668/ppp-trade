using System.Text.RegularExpressions;

namespace ppp_trade.Models.Parsers;

internal static class ParserHelper
{
    public static string TrimEndOfBraces(string input)
    {
        const string regex = @"\(.*?\)";
        return Regex.Replace(input, regex, "").Trim();
    }
}