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
    private const string FOUL_BORN_KEYWORD = "穢生 ";
    private const string LOCAL_KEYWORD = "(部分)";
    private readonly string[] _flaskSplitKeywords = ["之", "的"];

    public bool IsMatch(string text, string game)
    {
        return game == "POE1" && text.Contains(RARITY_KEYWORD);
    }

    public ItemBase? Parse(string text)
    {
        #region Get stat data

        if (!cacheService.TryGet(STAT_TW_CACHE_KEY, out List<StatGroup>? statTw))
        {
            statTw = LoadStats("stats_zh_tw.json");
            cacheService.Set(STAT_TW_CACHE_KEY, statTw);
        }

        #endregion

        var lines = text.Replace("\r", "").Split("\n");
        var indexOfRarity = Array.FindIndex(lines, l => l.StartsWith(RARITY_KEYWORD));
        if (indexOfRarity == -1)
        {
            return null;
        }

        var parsedItem = new Poe1Item();
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
                    if (line.StartsWith(FOUL_BORN_KEYWORD))
                    {
                        parsedItem.IsFoulBorn = true;
                    }

                    if (parsedItem.ItemType == ItemType.FLASK &&
                        (parsedItem.Rarity != Rarity.NORMAL || parsedItem.Rarity != Rarity.UNIQUE))
                    {
                        foreach (var flaskSplitKeyword in _flaskSplitKeywords)
                        {
                            var replace = line.Replace(FOUL_BORN_KEYWORD, "");
                            var split = replace.Split(flaskSplitKeyword);
                            if (split.Length != 2)
                            {
                                continue;
                            }

                            parsedItem.ItemName = split[0] + flaskSplitKeyword;
                            parsedItem.ItemBaseName = split[1];
                            break;
                        }

                        parsingState = ParsingState.PARSING_UNKNOW;
                        break;
                    }

                    parsedItem.ItemName = line.Replace(FOUL_BORN_KEYWORD, "");
                    parsingState = parsedItem.Rarity == Rarity.CURRENCY
                        ? ParsingState.PARSING_UNKNOW
                        : ParsingState.PARSING_ITEM_BASE;
                    break;
                case ParsingState.PARSING_ITEM_BASE:
                    parsedItem.ItemBaseName = line;
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

                    List<ItemStat> stats = ResolveStats(statTexts, statTw!);
                    parsedItem.Stats = TryMapLocalAndGlobal(parsedItem.ItemType, stats, statTw!);
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
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "datas\\poe", fileName);
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

            { "箭袋", ItemType.QUIVER },
            { "盾牌", ItemType.SHIELD },
            { "盾", ItemType.SHIELD },

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
            { "護身符", ItemType.TALISMAN },

            { "功能藥劑", ItemType.FLASK },
            { "生命藥劑", ItemType.FLASK }
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

    private static List<ItemStat> ResolvePseudoStats(IEnumerable<ItemStat> itemStats, StatGroup pseudoStatGroup)
    {
        var mapping = new Dictionary<string, List<string>>
        {
            {
                // cold resistance
                "explicit.stat_4220027924",
                [
                    "pseudo.pseudo_total_cold_resistance",
                    "pseudo.pseudo_total_elemental_resistance",
                    "pseudo.pseudo_total_resistance"
                ]
            },
            {
                // fire resistance
                "explicit.stat_3372524247",
                [
                    "pseudo.pseudo_total_fire_resistance",
                    "pseudo.pseudo_total_elemental_resistance",
                    "pseudo.pseudo_total_resistance"
                ]
            },
            {
                // lightning resistance
                "explicit.stat_1671376347",
                [
                    "pseudo.pseudo_total_lightning_resistance",
                    "pseudo.pseudo_total_elemental_resistance",
                    "pseudo.pseudo_total_resistance"
                ]
            }
        };

        var pseudoDict = new Dictionary<string, int>();

        foreach (var stat in itemStats)
        {
            var statId = stat.Stat.Id;
            if (!mapping.TryGetValue(statId, out List<string>? value))
            {
                continue;
            }

            foreach (var pseudoId in value)
            {
                pseudoDict.TryAdd(pseudoId, 0);

                if (stat.Value != null)
                {
                    pseudoDict[pseudoId] += (int)stat.Value;
                }
            }
        }

        return pseudoDict.Select(p => new ItemStat
        {
            Stat = pseudoStatGroup.Entries.First(x => x.Id == p.Key),
            Value = p.Value
        }).ToList();
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

    private static IEnumerable<ItemStat> TryMapLocalAndGlobal(ItemType itemType, IEnumerable<ItemStat> stats,
        List<StatGroup> statGroups)
    {
        var genre = GetGenre(itemType);
        if (genre == null)
        {
            return stats;
        }

        var map = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "weapon", new Dictionary<string, string>
                {
                    { "stat_681332047", "stat_210067635" }, // #% increased Attack Speed (Local)
                    { "stat_960081730", "stat_1940865751" }, // Adds # to # Physical Damage (Local)
                    { "stat_321077055", "stat_709508406" }, // Adds # to # Fire Damage (Local)
                    { "stat_1334060246", "stat_3336890334" }, // Adds # to # Lightning Damage (Local)
                    { "stat_2387423236", "stat_1037193709" }, // Adds # to # Cold Damage (Local) 
                    { "stat_3531280422", "stat_2223678961" }, // Adds # to # Chaos Damage (Local)
                    { "stat_3593843976", "stat_55876295" }, // #% of Physical Attack Damage Leeched as Life (Local)
                    { "stat_3237948413", "stat_669069897" } // #% of Physical Attack Damage Leeched as Mana (Local)
                }
            },
            {
                "armour", new Dictionary<string, string>
                {
                    { "stat_2144192055", "stat_53045048" }, // +# to Evasion Rating (Local)
                    { "stat_2106365538", "stat_124859000" }, // #% increased Evasion Rating (Local)
                    { "stat_809229260", "stat_3484657501" }, // +# to Armour (Local)
                    { "stat_2866361420", "stat_1062208444" }, // #% increased Armour (Local)
                    { "stat_3489782002", "stat_4052037485" } // +# to maximum Energy Shield (Local)
                }
            }
        };
        var result = new List<ItemStat>();
        foreach (var stat in stats)
        {
            if (map.ContainsKey(genre))
            {
                var split = stat.Stat.Id.Split('.');
                var statType = split[0];
                var statId = split[1];
                if (map[genre].TryGetValue(statId, out var value))
                {
                    var targetStatId = statType + "." + value;
                    var targetGroup = statGroups.FirstOrDefault(x => x.Id == statType);
                    var targetStat = targetGroup?.Entries.FirstOrDefault(x => x.Id == targetStatId);
                    if (targetStat != null)
                    {
                        result.Add(new ItemStat
                        {
                            Value = stat.Value,
                            Stat = targetStat
                        });
                        continue;
                    }
                }
            }

            result.Add(stat);
        }

        return result;

        string? GetGenre(ItemType type)
        {
            return type switch
            {
                ItemType.CLAW => "weapon",
                ItemType.DAGGER => "weapon",
                ItemType.WAND => "weapon",
                ItemType.ONE_HAND_SWORD => "weapon",
                ItemType.ONE_HAND_AXE => "weapon",
                ItemType.ONE_HAND_MACE => "weapon",
                ItemType.SCEPTRE => "weapon",
                ItemType.RUNE_DAGGER => "weapon",
                ItemType.BOW => "weapon",
                ItemType.STAFF => "weapon",
                ItemType.TWO_HAND_SWORD => "weapon",
                ItemType.TWO_HAND_AXE => "weapon",
                ItemType.TWO_HAND_MACE => "weapon",
                ItemType.FISHING_ROD => "weapon",
                ItemType.HELMET => "armour",
                ItemType.BODY_ARMOUR => "armour",
                ItemType.GLOVES => "armour",
                ItemType.BOOTS => "armour",
                _ => null
            };
        }
    }

    private static List<ItemStat> ResolveStats(IEnumerable<string> statTexts, List<StatGroup> stats)
    {
        List<ItemStat> result = [];

        foreach (var statText in statTexts)
        {
            if (statText.Trim().EndsWith(IMPLICIT_KEYWORD))
            {
                var group = stats.First(s => s.Id == "implicit");
                var (statEng, value) = FindState(group, statText);
                if (statEng != null)
                {
                    result.Add(new ItemStat
                    {
                        Stat = statEng,
                        Value = value
                    });
                }
            }
            else if (statText.Trim().EndsWith(CRAFTED_KEYWORD))
            {
                var group = stats.First(s => s.Id == "crafted");
                var (state, value) = FindState(group, statText);
                if (state != null)
                {
                    result.Add(new ItemStat
                    {
                        Stat = state,
                        Value = value
                    });
                }
            }
            else
            {
                var group = stats.First(s => s.Id == "explicit");
                var (state, value) = FindState(group, statText);
                if (state != null)
                {
                    result.Add(new ItemStat
                    {
                        Stat = state,
                        Value = value
                    });
                }
            }
        }

        result = result.DistinctBy(x => x.Stat.Id).ToList();

        List<ItemStat> pseudoStats = ResolvePseudoStats(result, stats.First(s => s.Id == "pseudo"));

        result.InsertRange(0, pseudoStats);

        return result;

        (Stat?, int?) FindState(StatGroup group, string stat)
        {
            (Stat?, int?) matchResult = TryMatch();
            if (matchResult is (null, null))
            {
                stat = $"{stat} {LOCAL_KEYWORD}";
                matchResult = TryMatch(true);
            }

            return matchResult;

            (Stat?, int?) TryMatch(bool matchLocal = false)
            {
                foreach (var entry in group.Entries)
                {
                    foreach (var splitEntry in entry.Text.Split('\n'))
                    {
                        string regex;
                        var realItemStat = stat;
                        if (!matchLocal)
                        {
                            regex = @"\(.*?\)";
                            realItemStat = Regex.Replace(stat, regex, "").Trim();
                        }

                        regex = splitEntry.Replace("(", "\\(");
                        regex = regex.Replace(")", "\\)");
                        regex = regex.Replace("+#", "([+-]\\d+)");
                        regex = regex.Replace("#", "(\\d+)");
                        regex = $"^{regex}$";
                        try
                        {
                            var match = Regex.Match(realItemStat, regex);
                            if (!match.Success)
                            {
                                if (!match.Success)
                                {
                                    continue;
                                }
                            }

                            int? value = match.Groups.Count switch
                            {
                                3 => (int.Parse(match.Groups[2].Value) + int.Parse(match.Groups[1].Value)) / 2,
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
                }

                return (null, null);
            }
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