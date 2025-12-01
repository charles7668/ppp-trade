using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using ppp_trade.Enums;
using ppp_trade.Services;

namespace ppp_trade.Models.Parsers;

public class ChineseTradParser(CacheService cacheService) : IParser
{
    protected virtual string RarityKeyword => "稀有度: ";

    protected virtual string ItemTypeKeyword => "物品種類: ";

    protected virtual string ItemRequirementKeyword => "需求:";

    protected virtual string ItemSocketKeyword => "插槽: ";

    protected virtual string ItemLevelKeyword => "物品等級: ";

    protected virtual string SplitKeyword => "--------";

    protected virtual string ImplicitKeyword => "(implicit)";

    protected virtual string CraftedKeyword => "(crafted)";

    protected virtual string AugmentedKeyword => "(augmented)";

    private static string StatTwCacheKey => "parser:stat_zh_tw";

    protected virtual string FoulBornKeyword => "穢生 ";

    protected virtual string LocalKeyword => "(部分)";

    private static string[] FlaskSplitKeywords => ["之", "的"];

    protected virtual string ReqLevelKeyword => "等級: ";

    protected virtual string ReqIntKeyword => "智慧: ";

    protected virtual string ReqStrKeyword => "力量: ";

    protected virtual string ReqDexKeyword => "敏捷: ";

    protected virtual Dictionary<string, ItemType> ItemTypeMap => new()
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
        { "征戰長杖", ItemType.WAR_STAFF },
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
        { "生命藥劑", ItemType.FLASK },
        { "魔力藥劑", ItemType.FLASK }
    };

    protected virtual Dictionary<string, Rarity> RarityMap => new()
    {
        { "普通", Rarity.NORMAL },
        { "魔法", Rarity.MAGIC },
        { "稀有", Rarity.RARE },
        { "傳奇", Rarity.UNIQUE },
        { "通貨", Rarity.CURRENCY },
        { "命運卡", Rarity.DIVINATION_CARD }
    };

    public virtual bool IsMatch(string text, string game)
    {
        return game == "POE1" && text.Contains(RarityKeyword);
    }

    public virtual ItemBase? Parse(string text)
    {
        #region Get stat data

        if (!cacheService.TryGet(StatTwCacheKey, out List<StatGroup>? statTw))
        {
            statTw = LoadStats("stats_zh_tw.json");
            cacheService.Set(StatTwCacheKey, statTw);
        }

        #endregion

        var lines = text.Replace("\r", "").Split("\n");
        var indexOfRarity = Array.FindIndex(lines, l => l.StartsWith(RarityKeyword));
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
                    if (line.StartsWith(FoulBornKeyword))
                    {
                        parsedItem.IsFoulBorn = true;
                    }

                    if (parsedItem is { ItemType: ItemType.FLASK, Rarity: not (Rarity.NORMAL or Rarity.UNIQUE) })
                    {
                        foreach (var flaskSplitKeyword in FlaskSplitKeywords)
                        {
                            var replace = line.Replace(FoulBornKeyword, "");
                            var split = replace.Split(flaskSplitKeyword);
                            if (split.Length != 2)
                            {
                                continue;
                            }

                            parsedItem.ItemName = replace;
                            parsedItem.ItemBaseName = split[1];
                            break;
                        }

                        parsingState = ParsingState.PARSING_UNKNOW;
                        break;
                    }

                    parsedItem.ItemName = line.Replace(FoulBornKeyword, "");
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
                    while (i < lines.Length && lines[i] != SplitKeyword)
                    {
                        reqTexts.Add(lines[i]);
                        i++;
                    }

                    parsedItem.Requirements = ResolveItemRequirements(reqTexts);
                    parsingState = ParsingState.PARSING_UNKNOW;
                    break;
                case ParsingState.PARSING_ITEM_LEVEL:
                    parsedItem.ItemLevel = int.Parse(line.Substring(ItemLevelKeyword.Length));
                    parsingState = ParsingState.PARSING_STAT;
                    break;
                case ParsingState.PARSING_STAT:
                    var hasImplicit = false;
                    List<string> statTexts = [];
                    if (line == SplitKeyword)
                    {
                        i++;
                        if (lines[i].Contains(ImplicitKeyword))
                        {
                            hasImplicit = true;
                        }

                        while (i < lines.Length && lines[i] != SplitKeyword)
                        {
                            statTexts.Add(lines[i]);
                            i++;
                        }
                    }

                    if (hasImplicit && i < lines.Length && lines[i] == SplitKeyword)
                    {
                        i++;

                        while (i < lines.Length && lines[i] != SplitKeyword)
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
                    else if (line.StartsWith(ItemTypeKeyword))
                    {
                        i--;
                        parsingState = ParsingState.PARSING_ITEM_TYPE;
                    }
                    else if (line.StartsWith(ItemRequirementKeyword))
                    {
                        i--;
                        parsingState = ParsingState.PARSING_REQUIREMENT;
                    }
                    else if (line.StartsWith(ItemLevelKeyword))
                    {
                        i--;
                        parsingState = ParsingState.PARSING_ITEM_LEVEL;
                    }
                    else if (line.StartsWith(ItemSocketKeyword))
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

    protected IEnumerable<ItemRequirement> ResolveItemRequirements(IEnumerable<string> reqTexts)
    {
        Dictionary<string, ItemRequirementType> typeMap = new()
        {
            { ReqLevelKeyword, ItemRequirementType.LEVEL },
            { ReqStrKeyword, ItemRequirementType.STR },
            { ReqDexKeyword, ItemRequirementType.DEX },
            { ReqIntKeyword, ItemRequirementType.INT }
        };
        List<ItemRequirement> results = [];
        foreach (var reqText in reqTexts)
        {
            var key = ReqLevelKeyword;
            if (reqText.StartsWith(ReqLevelKeyword))
            {
                key = ReqLevelKeyword;
            }
            else if (reqText.StartsWith(ReqIntKeyword))
            {
                key = ReqIntKeyword;
            }
            else if (reqText.StartsWith(ReqStrKeyword))
            {
                key = ReqStrKeyword;
            }
            else if (reqText.StartsWith(ReqDexKeyword))
            {
                key = ReqDexKeyword;
            }

            var value = int.Parse(reqText.Replace(AugmentedKeyword, "").Substring(ReqLevelKeyword.Length).Trim());
            results.Add(new ItemRequirement
            {
                ItemRequirementType = typeMap[key],
                Value = value
            });
        }

        return results;
    }

    protected ItemType ResolveItemType(string lineText)
    {
        var substr = lineText.Substring(ItemTypeKeyword.Length).Trim();
        return ItemTypeMap.GetValueOrDefault(substr, ItemType.OTHER);
    }

    protected int ResolveLinkCount(string line)
    {
        var socketInfoText = line.Substring(ItemSocketKeyword.Length);
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

    protected Rarity ResolveRarity(string lineText)
    {
        var rarityStr = lineText.Substring(RarityKeyword.Length).Trim();
        return RarityMap.GetValueOrDefault(rarityStr, Rarity.NORMAL);
    }

    protected static IEnumerable<ItemStat> TryMapLocalAndGlobal(ItemType itemType, IEnumerable<ItemStat> stats,
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

    protected List<ItemStat> ResolveStats(IEnumerable<string> statTexts, List<StatGroup> stats)
    {
        List<ItemStat> result = [];

        foreach (var statText in statTexts)
        {
            if (statText.Trim().EndsWith(ImplicitKeyword))
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
            else if (statText.Trim().EndsWith(CraftedKeyword))
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
                stat = $"{stat} {LocalKeyword}";
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
                        regex = regex.Replace("+#", "([+-][\\d.]+)");
                        regex = regex.Replace("#", "([\\d.]+)");
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
                                3 => (int)((double.Parse(match.Groups[2].Value,
                                                System.Globalization.CultureInfo.InvariantCulture) +
                                            double.Parse(match.Groups[1].Value,
                                                System.Globalization.CultureInfo.InvariantCulture)) / 2),
                                > 1 => (int)double.Parse(match.Groups[1].Value,
                                    System.Globalization.CultureInfo.InvariantCulture),
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

    protected enum ParsingState
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