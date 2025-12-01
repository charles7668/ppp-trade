using System.IO;
using System.Text.Json;
using ppp_trade.Enums;
using ppp_trade.Services;

namespace ppp_trade.Models.Parsers;

internal class EnParser(CacheService cacheService) : ChineseTradParser(cacheService)
{
    private readonly CacheService _cacheService = cacheService;

    protected override string RarityKeyword { get; } = "Rarity: ";

    protected override string ItemTypeKeyword => "Item Class: ";

    protected override string ItemRequirementKeyword => "Requirements:";

    protected override string ItemSocketKeyword => "Sockets: ";

    protected override string ItemLevelKeyword => "Item Level: ";

    protected override string SplitKeyword => "--------";

    protected override string ImplicitKeyword => "(implicit)";

    protected override string CraftedKeyword => "(crafted)";

    protected override string AugmentedKeyword => "(augmented)";

    protected override string FoulBornKeyword => "Foulborn ";

    protected override string LocalKeyword => "(local)";

    protected override string ReqLevelKeyword => "Level: ";

    protected override string ReqIntKeyword => "Int: ";

    protected override string ReqStrKeyword => "Str: ";

    protected override string ReqDexKeyword => "Dex: ";

    private static string StatEnCacheKey => "parser:stat_en";

    protected override Dictionary<string, ItemType> ItemTypeMap => new()
    {
        { "Claws", ItemType.CLAW },
        { "Daggers", ItemType.DAGGER },
        { "Wands", ItemType.WAND },
        { "One Hand Swords", ItemType.ONE_HAND_SWORD },
        { "Thrusting One Hand Swords", ItemType.ONE_HAND_SWORD },
        { "One Hand Axes", ItemType.ONE_HAND_AXE },
        { "One Hand Maces", ItemType.ONE_HAND_MACE },
        { "Sceptres", ItemType.SCEPTRE },
        { "Rune Daggers", ItemType.RUNE_DAGGER },

        { "Bows", ItemType.BOW },
        { "Staves", ItemType.STAFF },
        { "Two Hand Swords", ItemType.TWO_HAND_SWORD },
        { "Two Hand Axes", ItemType.TWO_HAND_AXE },
        { "Two Hand Maces", ItemType.TWO_HAND_MACE },
        { "Warstaves", ItemType.WAR_STAFF },
        { "Fishing Rods", ItemType.FISHING_ROD },

        { "Quivers", ItemType.QUIVER },
        { "Shields", ItemType.SHIELD },

        { "Helmets", ItemType.HELMET },
        { "Body Armours", ItemType.BODY_ARMOUR },
        { "Gloves", ItemType.GLOVES },
        { "Boots", ItemType.BOOTS },
        { "Belts", ItemType.BELT },

        { "Amulets", ItemType.AMULET },
        { "Rings", ItemType.RING },

        { "Maps", ItemType.MAP },
        { "Contracts", ItemType.CONTRACT },
        { "Blueprints", ItemType.BLUEPRINT },
        { "Stackable Currency", ItemType.STACKABLE_CURRENCY },
        { "Divination Cards", ItemType.DIVINATION_CARD },
        { "Jewels ", ItemType.JEWEL },
        { "Abyss Jewels", ItemType.ABYSS_JEWEL },

        { "Utility Flasks", ItemType.FLASK },
        { "Life Flasks", ItemType.FLASK },
        { "Mana Flasks", ItemType.FLASK },
    };

    protected override Dictionary<string, Rarity> RarityMap => new()
    {
        { "Normal", Rarity.NORMAL },
        { "Magic", Rarity.MAGIC },
        { "Rare", Rarity.RARE },
        { "Unique", Rarity.UNIQUE },
        { "Currency", Rarity.CURRENCY },
        { "Divination Cards", Rarity.DIVINATION_CARD }
    };

    public override bool IsMatch(string text, string game)
    {
        return game == "POE1" && text.Contains("Item Class: ");
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

    public override ItemBase? Parse(string text)
    {
        #region Get stat data

        if (!_cacheService.TryGet(StatEnCacheKey, out List<StatGroup>? statEn))
        {
            statEn = LoadStats("stats_eng.json");
            _cacheService.Set(StatEnCacheKey, statEn);
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
                        var removeFoulBorn = line.Replace(FoulBornKeyword, "");
                        if (removeFoulBorn.Contains(" of "))
                        {
                            parsedItem.ItemName = removeFoulBorn;
                            var splitWords = line.Split(' ');
                            var tempBaseName = "";
                            for (int x = 1; x < splitWords.Length; x++)
                            {
                                if (splitWords[x] == "of")
                                {
                                    break;
                                }

                                tempBaseName += splitWords[x] + " ";
                            }

                            parsedItem.ItemBaseName = tempBaseName.Trim();
                        }
                        else
                        {
                            parsedItem.ItemName = removeFoulBorn;
                            parsedItem.ItemBaseName = removeFoulBorn;
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

                    var stats = ResolveStats(statTexts, statEn!);
                    parsedItem.Stats = TryMapLocalAndGlobal(parsedItem.ItemType, stats, statEn!);
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
}