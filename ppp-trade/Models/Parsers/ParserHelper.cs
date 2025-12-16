using System.Diagnostics;
using System.Globalization;
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

    public static (bool, int?, int?) TryResolveMonsterGainCharge(Stat stat, string statText, ItemBase itemBase)
    {
        if (TryMatchStat(stat, statText, out var value, out var optionId))
        {
            return (true, value, optionId);
        }

        // if chance is 100% than stat text will remove chance text
        var regex = stat.Text.Replace("有 #% 機率", "");
        var match = Regex.Match(statText, regex);
        if (match.Success)
        {
            return (true, 100, null);
        }

        return (false, null, null);
    }

    public static (bool, int?, int?) TryResolveMonsterGainChargeEn(Stat stat, string statText, ItemBase itemBase)
    {
        if (TryMatchStat(stat, statText, out var value, out var optionId))
        {
            return (true, value, optionId);
        }

        // if chance is 100% than stat text will remove chance text
        var regex = stat.Text.Replace("have #% chance to ", "");
        var match = Regex.Match(statText, regex);
        if (match.Success)
        {
            return (true, 100, null);
        }

        return (false, null, null);
    }

    public static bool TryMatchStat(Stat stat, string statText, out int? outValue, out int? outOptionId)
    {
        outValue = null;
        List<(string, int?)> statTexts = [(stat.Text, null)];
        outOptionId = null;
        if (stat.Option is { Options.Count: > 0 })
        {
            statTexts = stat.Option.Options.Select(x => (stat.Text.Replace("#", x.Text), (int?)x.Id)).ToList();
        }

        foreach (var tryStatOption in statTexts)
        {
            foreach (var splitEntry in tryStatOption.Item1.Split('\n'))
            {
                var regex = splitEntry.Replace("(", "\\(");
                regex = regex.Replace(")", "\\)");
                regex = regex.Replace("+#", "([+-][\\d.]+)");
                regex = regex.Replace("#", "([+-]?[\\d.]+)");
                regex = $"^{regex}$";
                try
                {
                    var match = Regex.Match(statText, regex);
                    if (!match.Success)
                    {
                        if (!match.Success)
                        {
                            continue;
                        }
                    }

                    int? value = match.Groups.Count switch
                    {
                        3 => (int)((double.Parse(match.Groups[2].Value,
                                        CultureInfo.InvariantCulture) +
                                    double.Parse(match.Groups[1].Value,
                                        CultureInfo.InvariantCulture)) / 2),
                        > 1 => (int)double.Parse(match.Groups[1].Value,
                            CultureInfo.InvariantCulture),
                        _ => null
                    };

                    outValue = value;
                    outOptionId = tryStatOption.Item2;
                    return true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

        return false;
    }
}