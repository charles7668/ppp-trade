using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using ppp_trade.Enums;
using ppp_trade.Services;

namespace ppp_trade.Models.Parsers;

public class ChineseTradParser(CacheService cacheService) : IParser
{
    private const string RARITY_KEYWORD = "稀有度: ";
    private const string ITEM_TYPE_KEYWORD = "物品種類: ";
    private const string ITEM_REQUIREMENT_KEYWORD = "需求:";
    private const string ITEM_SOCKET_KEYWORD = "插槽: ";
    private const string ITEM_LEVEL_KEYWORD = "物品等級: ";
    private const string SPLIT_KEYWORD = "--------";
    private const string IMPLICIT_KEYWORD = "(implicit)";
    private const string CRAFTED_KEYWORD = "(crafted)";
    private const string STAT_TW_CACHE_KEY = "parser:stat_zh_tw";

    public bool IsMatch(string text)
    {
        return text.Contains(RARITY_KEYWORD);
    }

    public Item? Parse(string text)
    {
        #region Get stat data

        if (!cacheService.TryGet(STAT_TW_CACHE_KEY, out List<StatGroup>? statTw))
        {
            statTw = LoadStats("stats_zh_tw.json");
            cacheService.Set(STAT_TW_CACHE_KEY, statTw);
        }

        #endregion

        var lines = text.Split("\r\n");
        var indexOfRarity = Array.FindIndex(lines, l => l.StartsWith(RARITY_KEYWORD));
        if (indexOfRarity == -1)
        {
            return null;
        }

        var parsedItem = new Item();
        var parsingState = ParsingState.PARSING_UNKNOW;
        for (var i = 0; i < lines.Length; ++i)
        {
            var line = lines[i];
            switch (parsingState)
            {
                case ParsingState.PARSING_RARITY:
                    parsedItem.Rarity = ResolveRarity(line);
                    parsingState = ParsingState.PARSING_ITEM_NAME;
                    break;
                case ParsingState.PARSING_ITEM_NAME:
                    parsedItem.ItemName = line;
                    parsingState = parsedItem.Rarity == Rarity.CURRENCY
                        ? ParsingState.PARSING_UNKNOW
                        : ParsingState.PARSING_ITEM_BASE;
                    break;
                case ParsingState.PARSING_ITEM_BASE:
                    parsedItem.ItemBase = line;
                    parsingState = ParsingState.PARSING_UNKNOW;
                    break;
                case ParsingState.PARSING_ITEM_TYPE:
                    parsedItem.ItemType = ResolveItemType(line);
                    parsingState = ParsingState.PARSING_UNKNOW;
                    break;
                case ParsingState.PARSING_REQUIREMENT:
                    List<string> reqTexts = [];
                    i++;
                    while (i < lines.Length && lines[i] != SPLIT_KEYWORD)
                    {
                        reqTexts.Add(lines[i]);
                        i++;
                    }

                    parsedItem.Requirements = ResolveItemRequirements(reqTexts);
                    parsingState = ParsingState.PARSING_UNKNOW;
                    break;
                case ParsingState.PARSING_ITEM_LEVEL:
                    parsedItem.ItemLevel = int.Parse(line.Substring(ITEM_LEVEL_KEYWORD.Length,
                        line.Length - ITEM_LEVEL_KEYWORD.Length));
                    parsingState = ParsingState.PARSING_STAT;
                    break;
                case ParsingState.PARSING_STAT:
                    var hasImplicit = false;
                    List<string> statTexts = [];
                    if (line == SPLIT_KEYWORD)
                    {
                        i++;
                        if (lines[i].Contains(IMPLICIT_KEYWORD))
                        {
                            hasImplicit = true;
                        }

                        while (i < lines.Length && lines[i] != SPLIT_KEYWORD)
                        {
                            statTexts.Add(lines[i]);
                            i++;
                        }
                    }

                    if (hasImplicit && i < lines.Length && lines[i] == SPLIT_KEYWORD)
                    {
                        i++;

                        while (i < lines.Length && lines[i] != SPLIT_KEYWORD)
                        {
                            statTexts.Add(lines[i]);
                            i++;
                        }
                    }

                    parsedItem.Stats = ResolveStats(statTexts, statTw!);
                    parsingState = ParsingState.PARSING_UNKNOW;
                    break;
                case ParsingState.PARSING_LINK:
                    parsedItem.Link = ResolveLinkCount(line);
                    parsingState = ParsingState.PARSING_UNKNOW;
                    break;
                case ParsingState.PARSING_UNKNOW:
                    if (i == indexOfRarity)
                    {
                        i--;
                        parsingState = ParsingState.PARSING_RARITY;
                    }
                    else if (line.StartsWith(ITEM_TYPE_KEYWORD))
                    {
                        i--;
                        parsingState = ParsingState.PARSING_ITEM_TYPE;
                    }
                    else if (line.StartsWith(ITEM_REQUIREMENT_KEYWORD))
                    {
                        i--;
                        parsingState = ParsingState.PARSING_REQUIREMENT;
                    }
                    else if (line.StartsWith(ITEM_LEVEL_KEYWORD))
                    {
                        i--;
                        parsingState = ParsingState.PARSING_ITEM_LEVEL;
                    }
                    else if (line.StartsWith(ITEM_SOCKET_KEYWORD))
                    {
                        i--;
                        parsingState = ParsingState.PARSING_LINK;
                    }

                    break;
            }
        }

        return parsedItem;
    }

    private List<StatGroup> LoadStats(string fileName)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs", fileName);
        if (!File.Exists(path))
        {
            return [];
        }

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        return JsonSerializer.Deserialize<List<StatGroup>>(json, options) ?? [];
    }

    private static IEnumerable<ItemRequirement> ResolveItemRequirements(IEnumerable<string> reqTexts)
    {
        const string reqLevelKeyword = "等級: ";
        const string reqIntKeyword = "智慧: ";
        const string reqStrKeyword = "力量: ";
        const string reqDexKeyword = "敏捷: ";
        Dictionary<string, ItemRequirementType> typeMap = new()
        {
            { reqLevelKeyword, ItemRequirementType.LEVEL },
            { reqStrKeyword, ItemRequirementType.STR },
            { reqDexKeyword, ItemRequirementType.DEX },
            { reqIntKeyword, ItemRequirementType.INT }
        };
        List<ItemRequirement> results = [];
        foreach (var reqText in reqTexts)
        {
            var key = reqLevelKeyword;
            if (reqText.StartsWith(reqLevelKeyword))
            {
                key = reqLevelKeyword;
            }
            else if (reqText.StartsWith(reqIntKeyword))
            {
                key = reqIntKeyword;
            }
            else if (reqText.StartsWith(reqStrKeyword))
            {
                key = reqStrKeyword;
            }
            else if (reqText.StartsWith(reqDexKeyword))
            {
                key = reqDexKeyword;
            }

            var value = int.Parse(reqText.Substring(reqLevelKeyword.Length,
                reqText.Length - reqLevelKeyword.Length));
            results.Add(new ItemRequirement
            {
                ItemRequirementType = typeMap[key],
                Value = value
            });
        }

        return results;
    }

    private static ItemType ResolveItemType(string lineText)
    {
        var substr = lineText.Substring(ITEM_TYPE_KEYWORD.Length, lineText.Length - ITEM_TYPE_KEYWORD.Length).Trim();
        var typeMap = new Dictionary<string, ItemType>
        {
            { "爪", ItemType.CLAW },
            { "匕首", ItemType.DAGGER },
            { "法杖", ItemType.WAND },
            { "單手劍", ItemType.ONE_HAND_SWORD },
            { "單手斧", ItemType.ONE_HAND_AXE },
            { "單手錘", ItemType.ONE_HAND_MACE },
            { "權杖", ItemType.SCEPTRE },
            { "符紋匕首", ItemType.RUNE_DAGGER },

            { "弓", ItemType.BOW },
            { "長杖", ItemType.STAFF },
            { "雙手劍", ItemType.TWO_HAND_SWORD },
            { "雙手斧", ItemType.TWO_HAND_AXE },
            { "雙手錘", ItemType.TWO_HAND_MACE },
            { "魚竿", ItemType.FISHING_ROD },

            { "箭袋", ItemType.Quiver },
            { "盾牌", ItemType.SHIELD },

            { "頭部", ItemType.HELMET },
            { "胸甲", ItemType.BODY_ARMOUR },
            { "手套", ItemType.GLOVES },
            { "鞋子", ItemType.BOOTS },
            { "腰帶", ItemType.BELT },

            { "項鍊", ItemType.AMULET },
            { "戒指", ItemType.RING },

            { "異界地圖", ItemType.MAP },
            { "契約書", ItemType.CONTRACT },
            { "藍圖", ItemType.BLUEPRINT },
            { "可堆疊通貨", ItemType.STACKABLE_CURRENCY },
            { "命運卡", ItemType.DIVINATION_CARD },
            { "珠寶", ItemType.JEWEL },
            { "大型珠寶", ItemType.ABYSS_JEWEL },
            { "護身符", ItemType.TALISMAN }
        };

        return typeMap.GetValueOrDefault(substr, ItemType.OTHER);
    }

    private static int ResolveLinkCount(string line)
    {
        var socketInfoText = line.Substring(ITEM_SOCKET_KEYWORD.Length,
            line.Length - ITEM_SOCKET_KEYWORD.Length);
        socketInfoText = Regex.Replace(socketInfoText, "[A-Z]", "");
        var split = socketInfoText.Split(' ');
        return split.Select(linkText => linkText.Length).Prepend(0).Max() + 1;
    }

    private static Rarity ResolveRarity(string lineText)
    {
        var rarityStr = lineText.Substring(RARITY_KEYWORD.Length, lineText.Length - RARITY_KEYWORD.Length).Trim();
        var result = rarityStr switch
        {
            "普通" => Rarity.NORMAL,
            "魔法" => Rarity.MAGIC,
            "稀有" => Rarity.RARE,
            "傳奇" => Rarity.UNIQUE,
            "通貨" => Rarity.CURRENCY,
            "命運卡" => Rarity.DIVINATION_CARD,
            _ => Rarity.NORMAL
        };

        return result;
    }

    private static List<ItemStat> ResolveStats(IEnumerable<string> statTexts, List<StatGroup> stats)
    {
        List<ItemStat> result = [];

        foreach (var stat in statTexts)
        {
            if (stat.Trim().EndsWith(IMPLICIT_KEYWORD))
            {
                var group = stats.First(s => s.Id == "implicit");
                var (statEng, value) = FindState(group, stat);
                if (statEng != null)
                {
                    result.Add(new ItemStat
                    {
                        Stat = statEng,
                        Value = value
                    });
                }
            }
            else if (stat.Trim().EndsWith(CRAFTED_KEYWORD))
            {
                var group = stats.First(s => s.Id == "crafted");
                var (statEng, value) = FindState(group, stat);
                if (statEng != null)
                {
                    result.Add(new ItemStat
                    {
                        Stat = statEng,
                        Value = value
                    });
                }
            }
            else
            {
                var group = stats.First(s => s.Id == "explicit");
                var (statEng, value) = FindState(group, stat);
                if (statEng != null)
                {
                    result.Add(new ItemStat
                    {
                        Stat = statEng,
                        Value = value
                    });
                }
            }
        }

        return result;

        (Stat?, int?) FindState(StatGroup group, string stat)
        {
            foreach (var entry in group.Entries)
            {
                var regex = entry.Text.Replace("+#", "([+-]\\d+)");
                regex = regex.Replace("#", "(\\d+)");
                regex = $"^{regex}";
                try
                {
                    var match = Regex.Match(stat, regex);
                    if (!match.Success)
                    {
                        continue;
                    }

                    int? value = match.Groups.Count switch
                    {
                        3 => int.Parse(match.Groups[2].Value) + int.Parse(match.Groups[1].Value),
                        > 1 => int.Parse(match.Groups[1].Value),
                        _ => null
                    };

                    return (entry, value);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }

            return (null, null);
        }
    }

    private enum ParsingState
    {
        PARSING_ITEM_TYPE,
        PARSING_RARITY,
        PARSING_LINK,
        PARSING_ITEM_NAME,
        PARSING_ITEM_BASE,
        PARSING_REQUIREMENT,
        PARSING_ITEM_LEVEL,
        PARSING_STAT,
        PARSING_UNKNOW
    }
}