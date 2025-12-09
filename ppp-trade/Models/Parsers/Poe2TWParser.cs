using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using ppp_trade.Enums;
using ppp_trade.Services;

namespace ppp_trade.Models.Parsers;

public class Poe2TWParser(CacheService cacheService) : IParser
{
    protected virtual string ItemRarityKeyword => "稀有度: ";

    protected virtual string ItemTypeKeyword => "物品種類: ";

    protected virtual string ItemRequirementKeyword => "需求:";

    protected virtual string ItemSocketKeyword => "插槽: ";

    protected virtual string ItemLevelKeyword => "物品等級: ";

    protected virtual string ItemGrantsSkillKeyword => "賦予技能: ";

    protected virtual string ItemSpiritKeyword => "精魂: ";

    protected virtual string SplitKeyword => "--------";

    protected virtual string ImplicitKeyword => "(implicit)";

    protected virtual string RuneKeyword => "(rune)";

    protected virtual string DesecratedKeyword => "(desecrated)";

    protected virtual string AugmentedKeyword => "(augmented)";

    private string EnchantKeyword => "(enchant)";

    protected virtual string StatCacheKey => "parser:poe2:stat_zh_tw";

    protected virtual string LocalKeyword => "(部分)";

    protected virtual string ReqLevelKeyword => "等級";

    protected virtual string ReqIntKeyword => "智慧";

    protected virtual string ReqDexKeyword => "敏捷";

    protected virtual string ReqStrKeyword => "力量";

    protected virtual Dictionary<string, ItemType> ItemTypeMap { get; set; } = new Dictionary<string, ItemType>
    {
        { "爪", ItemType.CLAW },
        { "匕首", ItemType.DAGGER },
        { "法杖", ItemType.WAND },
        { "單手劍", ItemType.ONE_HAND_SWORD },
        { "單手斧", ItemType.ONE_HAND_AXE },
        { "單手錘", ItemType.ONE_HAND_MACE },
        { "權杖", ItemType.SCEPTRE },
        { "長矛", ItemType.SPEAR },
        { "鏈錘", ItemType.FLAIL },

        { "弓", ItemType.BOW },
        { "長杖", ItemType.STAFF },
        { "雙手劍", ItemType.TWO_HAND_SWORD },
        { "雙手斧", ItemType.TWO_HAND_AXE },
        { "雙手錘", ItemType.TWO_HAND_MACE },
        { "細杖", ItemType.QUARTERSTAFF },
        { "魚竿", ItemType.FISHING_ROD },
        { "十字弓", ItemType.CROSSBOW },
        { "陷阱", ItemType.TRAP },

        { "箭袋", ItemType.QUIVER },
        { "盾牌", ItemType.SHIELD },
        { "盾", ItemType.SHIELD },
        { "輕盾", ItemType.BUCKLER },
        { "法器", ItemType.FOCI },

        { "頭部", ItemType.HELMET },
        { "胸甲", ItemType.BODY_ARMOUR },
        { "手套", ItemType.GLOVES },
        { "鞋子", ItemType.BOOTS },
        { "腰帶", ItemType.BELT },

        { "項鍊", ItemType.AMULET },
        { "戒指", ItemType.RING },

        { "可堆疊通貨", ItemType.STACKABLE_CURRENCY },
        { "可鑲嵌", ItemType.SOCKETABLE },
        { "碑牌", ItemType.TABLET },
        { "珠寶", ItemType.JEWEL },
        { "護符", ItemType.CHARMS },

        { "生命藥劑", ItemType.FLASK },
        { "魔力藥劑", ItemType.FLASK },

        { "換界石", ItemType.WAY_STONE },
        { "遺鑰", ItemType.VAULT_KEY }
    };

    protected virtual Dictionary<string, Func<Stat, string, ItemBase, (bool, int?, int?)>> SpecialCaseStat { get; } =
        new()
        {
            { "stat_3639275092", ParserHelper.TryResolveIncreasedAndDecreased }
        };

    private string UnidentifiedKeyword => "Unidentified";

    public virtual bool IsMatch(string text, string game)
    {
        return game == "POE2" && text.Contains(ItemRarityKeyword);
    }

    public virtual ItemBase? Parse(string text)
    {
        #region Get stat data

        var statGroups = GetStatGroups();

        #endregion

        var lines = text.Replace("\r", "").Split("\n");
        var indexOfRarity = Array.FindIndex(lines, l => l.StartsWith(ItemRarityKeyword));
        if (indexOfRarity == -1)
        {
            return null;
        }

        var parsedItem = new Poe2Item();
        var parsingState = ParsingState.PARSING_UNKNOW;
        List<string> tempItemNames = [];
        for (var i = 0; i < lines.Length; ++i)
        {
            var line = lines[i];
            if (line.Trim() == UnidentifiedKeyword)
            {
                parsedItem.Unidentified = true;
            }

            switch (parsingState)
            {
                case ParsingState.PARSING_RARITY:
                    parsedItem.Rarity = ResolveRarity(line);
                    parsingState = ParsingState.PARSING_ITEM_NAME;
                    break;
                case ParsingState.PARSING_ITEM_NAME:
                    tempItemNames.Add(line);
                    parsingState = ParsingState.PARSING_ITEM_BASE;
                    break;
                case ParsingState.PARSING_ITEM_BASE:
                    tempItemNames.Add(line.Trim());
                    parsingState = ParsingState.PARSING_UNKNOW;
                    break;
                case ParsingState.PARSING_ITEM_TYPE:
                    parsedItem.ItemType = ResolveItemType(line);
                    parsingState = ParsingState.PARSING_UNKNOW;
                    break;
                case ParsingState.PARSING_REQUIREMENT:
                    parsedItem.Requirements = ResolveItemRequirements(line);
                    parsingState = ParsingState.PARSING_UNKNOW;
                    break;
                case ParsingState.PARSING_ITEM_LEVEL:
                    parsedItem.ItemLevel = int.Parse(line.Substring(ItemLevelKeyword.Length,
                        line.Length - ItemLevelKeyword.Length));
                    parsingState = ParsingState.PARSING_RUNE_STAT;
                    break;
                case ParsingState.PARSING_RUNE_STAT:
                    if (line == SplitKeyword)
                    {
                        i++;
                        if (i < lines.Length && !lines[i].Contains(RuneKeyword))
                        {
                            i -= 2;
                        }
                        else
                        {
                            while (i < lines.Length && lines[i] != SplitKeyword)
                                i++;

                            i--;
                        }

                        parsingState = ParsingState.PARSING_GRANTS_SKILL;
                    }
                    else
                    {
                        parsingState = ParsingState.PARSING_UNKNOW;
                    }

                    break;
                case ParsingState.PARSING_GRANTS_SKILL:
                    if (line == SplitKeyword)
                    {
                        i++;
                        if (i < lines.Length && !lines[i].StartsWith(ItemGrantsSkillKeyword))
                        {
                            i -= 2;
                        }
                        else
                        {
                            parsedItem.GrantsSkill = lines[i].AsSpan(ItemGrantsSkillKeyword.Length).ToString();
                            while (i < lines.Length && lines[i] != SplitKeyword)
                                i++;

                            i--;
                        }

                        parsingState = ParsingState.PARSING_STAT;
                    }
                    else
                    {
                        parsingState = ParsingState.PARSING_UNKNOW;
                    }

                    break;
                case ParsingState.PARSING_STAT:
                    bool hasNextStatArea;
                    List<string> statTexts = [];
                    if (line != SplitKeyword)
                    {
                        parsingState = ParsingState.PARSING_UNKNOW;
                        break;
                    }

                    do
                    {
                        hasNextStatArea = false;
                        var needSkip = false;
                        i++;
                        if (lines[i] == UnidentifiedKeyword)
                        {
                            parsedItem.Unidentified = true;
                            break;
                        }

                        if (lines[i].Contains(ImplicitKeyword) || lines[i].Contains(EnchantKeyword))
                        {
                            hasNextStatArea = true;
                        }
                        else if (lines[i].StartsWith(ItemGrantsSkillKeyword))
                        {
                            parsedItem.GrantsSkill = lines[i].AsSpan(ItemGrantsSkillKeyword.Length).ToString();
                            needSkip = true;
                            hasNextStatArea = true;
                        }

                        while (i < lines.Length && lines[i] != SplitKeyword)
                        {
                            if (!needSkip)
                            {
                                statTexts.Add(lines[i]);
                            }

                            i++;
                        }
                    } while (hasNextStatArea);

                    var stats = ResolveStats(statTexts, statGroups, parsedItem);
                    parsedItem.Stats = TryMapLocalAndGlobal(parsedItem.ItemType, stats, statGroups);
                    parsingState = ParsingState.PARSING_UNKNOW;
                    break;
                case ParsingState.PARSING_SPIRIT:
                    parsedItem.Spirit =
                        int.Parse(line.Replace(AugmentedKeyword, "").AsSpan(ItemSpiritKeyword.Length));
                    parsingState = ParsingState.PARSING_UNKNOW;
                    break;
                case ParsingState.PARSING_SOCKETS:
                    var count = ResolveRuneSockets(line);

                    parsedItem.RuneSockets = count;
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
                    else if (line.StartsWith(ItemSpiritKeyword))
                    {
                        i--;
                        parsingState = ParsingState.PARSING_SPIRIT;
                    }
                    else if (line.StartsWith(ItemSocketKeyword))
                    {
                        i--;
                        parsingState = ParsingState.PARSING_SOCKETS;
                    }

                    break;
            }
        }

        ResolveItemName(tempItemNames, parsedItem);

        return parsedItem;
    }

    protected virtual List<StatGroup> GetStatGroups()
    {
        if (!cacheService.TryGet(StatCacheKey, out List<StatGroup>? statTw))
        {
            statTw = LoadStats("stats_tw.json");
            cacheService.Set(StatCacheKey, statTw);
        }

        return statTw ?? [];
    }

    private List<StatGroup> LoadStats(string fileName)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "datas\\poe2", fileName);
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

    private void ResolveItemName(IEnumerable<string> itemNameTexts, ItemBase parsingItem)
    {
        var itemNameTextList = itemNameTexts.Select(x => x.Trim()).ToList();
        if (itemNameTextList.Count != 2)
        {
            return;
        }

        if (parsingItem.Rarity is Rarity.UNIQUE or Rarity.RARE && !parsingItem.Unidentified)
        {
            parsingItem.ItemName = itemNameTextList[0] + " " + itemNameTextList[1];
            parsingItem.ItemBaseName = itemNameTextList[1];
            return;
        }

        if (parsingItem.Rarity is Rarity.MAGIC)
        {
            var (itemName, itemBaseName) = ResolveMagicItemName(itemNameTextList[0]);
            parsingItem.ItemName = itemName;
            parsingItem.ItemBaseName = itemBaseName;
            return;
        }

        parsingItem.ItemName = itemNameTextList[0];
        parsingItem.ItemBaseName = itemNameTextList[0];
    }

    protected virtual IEnumerable<ItemRequirement> ResolveItemRequirements(string line)
    {
        var replaceAugmented =
            line.Replace(AugmentedKeyword, "").AsSpan(ItemRequirementKeyword.Length);
        Span<Range> reqList = stackalloc Range[6];
        replaceAugmented.Split(reqList, ',');
        List<ItemRequirement> itemRequirements = [];
        foreach (var range in reqList)
        {
            var req = replaceAugmented.Slice(range.Start.Value, range.End.Value - range.Start.Value)
                .ToString();
            string[] keywords = [ReqLevelKeyword, ReqIntKeyword, ReqDexKeyword, ReqStrKeyword];
            foreach (var keyword in keywords)
            {
                if (req.Contains(keyword))
                {
                    var realReq = req.Replace(keyword, "").Trim();
                    itemRequirements.Add(new ItemRequirement
                    {
                        ItemRequirementType = keyword switch
                        {
                            _ when keyword == ReqLevelKeyword => ItemRequirementType.LEVEL,
                            _ when keyword == ReqIntKeyword => ItemRequirementType.INT,
                            _ when keyword == ReqDexKeyword => ItemRequirementType.DEX,
                            _ when keyword == ReqStrKeyword => ItemRequirementType.STR,
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        Value = int.Parse(realReq)
                    });
                }
            }
        }

        return itemRequirements;
    }

    protected virtual ItemType ResolveItemType(string lineText)
    {
        var substr = lineText.Substring(ItemTypeKeyword.Length).Trim();
        return ItemTypeMap.GetValueOrDefault(substr, ItemType.OTHER);
    }

    protected virtual (string, string) ResolveMagicItemName(string nameText)
    {
        var index1 = nameText.IndexOf('之');
        var index2 = nameText.IndexOf('的');
        var lastIndex = Math.Max(index1, index2);
        var baseName = nameText;
        if (lastIndex != -1)
        {
            baseName = nameText.Substring(lastIndex + 1);
        }

        return (nameText, baseName);
    }

    protected virtual List<ItemStat> ResolvePseudoStats(IEnumerable<ItemStat> itemStats, StatGroup pseudoStatGroup)
    {
        var mapping = new Dictionary<string, List<(string mapTo, double ratio)>>
        {
            // cold resistance
            {
                "stat_4220027924", [
                    ("pseudo.pseudo_total_cold_resistance", 1),
                    ("pseudo.pseudo_total_elemental_resistance", 1),
                    ("pseudo.pseudo_total_resistance", 1)
                ]
            },
            // lightning resistance
            {
                "stat_1671376347", [
                    ("pseudo.pseudo_total_lightning_resistance", 1),
                    ("pseudo.pseudo_total_elemental_resistance", 1),
                    ("pseudo.pseudo_total_resistance", 1)
                ]
            },
            // fire resistance
            {
                "stat_3372524247", [
                    ("pseudo.pseudo_total_fire_resistance", 1),
                    ("pseudo.pseudo_total_elemental_resistance", 1),
                    ("pseudo.pseudo_total_resistance", 1)
                ]
            },
            // all element resistance
            {
                "stat_2901986750", [
                    ("pseudo.pseudo_total_cold_resistance", 1),
                    ("pseudo.pseudo_total_lightning_resistance", 1),
                    ("pseudo.pseudo_total_fire_resistance", 1),
                    ("pseudo.pseudo_total_elemental_resistance", 3),
                    ("pseudo.pseudo_total_resistance", 3)
                ]
            }
        };

        var pseudoDict = new Dictionary<string, int>();

        foreach (var stat in itemStats)
        {
            var statId = stat.Stat.Id.Split('.')[1];
            if (!mapping.TryGetValue(statId, out var value))
            {
                continue;
            }

            foreach (var pseudoId in value)
            {
                pseudoDict.TryAdd(pseudoId.mapTo, 0);

                if (stat.Value != null)
                {
                    pseudoDict[pseudoId.mapTo] += (int)(stat.Value * pseudoId.ratio);
                }
            }
        }

        return pseudoDict.Select(p => new ItemStat
        {
            Stat = pseudoStatGroup.Entries.First(x => x.Id == p.Key),
            Value = p.Value
        }).ToList();
    }

    protected virtual Rarity ResolveRarity(string lineText)
    {
        var rarityStr = lineText.Substring(ItemRarityKeyword.Length).Trim();
        return rarityStr switch
        {
            "普通" => Rarity.NORMAL,
            "魔法" => Rarity.MAGIC,
            "稀有" => Rarity.RARE,
            "傳奇" => Rarity.UNIQUE,
            "通貨" => Rarity.CURRENCY,
            _ => Rarity.NORMAL
        };
    }

    protected virtual int ResolveRuneSockets(string line)
    {
        var span = line.AsSpan(ItemSocketKeyword.Length);
        var count = 0;
        foreach (var s in span)
        {
            if (s == 'S')
            {
                count++;
            }
        }

        return count;
    }

    protected virtual List<ItemStat> ResolveStats(IEnumerable<string> statTexts, List<StatGroup> stats,
        ItemBase parsingItem)
    {
        List<ItemStat> result = [];
        foreach (var statText in statTexts)
        {
            StatGroup group;
            var trimText = statText.Trim();
            if (trimText.EndsWith(ImplicitKeyword))
            {
                group = stats.First(s => s.Id == "implicit");
            }
            else if (trimText.EndsWith(EnchantKeyword))
            {
                group = stats.First(s => s.Id == "enchant");
            }
            else if (trimText.EndsWith(DesecratedKeyword))
            {
                group = stats.First(s => s.Id == "desecrated");
            }
            else
            {
                group = stats.First(s => s.Id == "explicit");
            }

            var (stat, value, optionId) = FindState(group, statText);
            if (stat != null)
            {
                result.Add(new ItemStat
                {
                    Stat = stat,
                    Value = value,
                    OptionId = optionId
                });
            }
        }

        result = result.DistinctBy(x => x.Stat.Id).ToList();

        var pseudoStats = ResolvePseudoStats(result, stats.First(s => s.Id == "pseudo"));

        result.InsertRange(0, pseudoStats);

        return result;

        (Stat?, int?, int?) FindState(StatGroup group, string stat)
        {
            var matchResult = TryMatch();
            if (matchResult is (null, null, null))
            {
                stat = $"{stat} {LocalKeyword}";
                matchResult = TryMatch(true);
            }

            return matchResult;

            (Stat?, int?, int? ) TryMatch(bool matchLocal = false)
            {
                foreach (var entry in group.Entries)
                {
                    var statId = entry.Id.Split('.')[1];
                    if (SpecialCaseStat.TryGetValue(statId, out var specialFunc))
                    {
                        var (matched, value, optionId) = specialFunc(entry, stat, parsingItem);
                        if (!matched)
                        {
                            continue;
                        }

                        return (entry, value, optionId);
                    }

                    var realItemStat = stat;
                    if (!matchLocal)
                    {
                        realItemStat = ParserHelper.TrimEndOfBraces(realItemStat);
                    }

                    if (TryMatchStat(entry, realItemStat, out var matchedValue, out var matchedOptionId))
                    {
                        return (entry, matchedValue, matchedOptionId);
                    }
                }

                return (null, null, null);
            }
        }
    }

    protected virtual IEnumerable<ItemStat> TryMapLocalAndGlobal(ItemType itemType, IEnumerable<ItemStat> stats,
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
                "weapon", new Dictionary<string, string>()
            },
            {
                "armour", new Dictionary<string, string>()
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
                ItemType.QUARTERSTAFF => "weapon",
                ItemType.SPEAR => "weapon",
                ItemType.FLAIL => "weapon",
                ItemType.CROSSBOW => "weapon",
                ItemType.TRAP => "weapon",
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

    protected static bool TryMatchStat(Stat stat, string statText, out int? outValue, out int? outOptionId)
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

    private enum ParsingState
    {
        PARSING_ITEM_TYPE,
        PARSING_RARITY,
        PARSING_SPIRIT,
        PARSING_ITEM_NAME,
        PARSING_ITEM_BASE,
        PARSING_SOCKETS,
        PARSING_REQUIREMENT,
        PARSING_ITEM_LEVEL,
        PARSING_STAT,
        PARSING_RUNE_STAT,
        PARSING_GRANTS_SKILL,
        PARSING_UNKNOW
    }
}