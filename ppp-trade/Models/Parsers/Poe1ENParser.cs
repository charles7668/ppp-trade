using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using ppp_trade.Enums;
using ppp_trade.Services;

namespace ppp_trade.Models.Parsers;

internal class Poe1ENParser(CacheService cacheService) : Poe1TWParser(cacheService)
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

    protected override string ClusterJewelKeyword => "Cluster Jewel";

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
        { "Jewels", ItemType.JEWEL },
        { "Abyss Jewels", ItemType.ABYSS_JEWEL },

        { "Utility Flasks", ItemType.FLASK },
        { "Life Flasks", ItemType.FLASK },
        { "Mana Flasks", ItemType.FLASK },

        { "Corpses", ItemType.CORPSE },

        { "Skill Gems", ItemType.ACTIVE_GEM },
        { "Support Gems", ItemType.SUPPORT_GEM }
    };

    protected override Dictionary<string, Rarity> RarityMap => new()
    {
        { "Normal", Rarity.NORMAL },
        { "Magic", Rarity.MAGIC },
        { "Rare", Rarity.RARE },
        { "Unique", Rarity.UNIQUE },
        { "Currency", Rarity.CURRENCY },
        { "Divination Cards", Rarity.DIVINATION_CARD },
        { "Gem", Rarity.GEM }
    };

    protected override Dictionary<string, Func<Stat, string, ItemBase, (bool, int?, int?)>> SpecialCaseStat { get; } =
        new()
        {
            { "stat_700317374", ParserHelper.TryResolveIncreasedAndDecreasedEn },
            { "stat_3338298622", ParserHelper.TryResolveIncreasedAndDecreasedEn },
            { "stat_4016885052", TryResolveAdditionalProjectile },
            { "stat_1001829678", TryResolveStaffStats },
            { "stat_687813731", ParserHelper.TryResolveMonsterGainChargeEn },
            { "stat_406353061", ParserHelper.TryResolveMonsterGainChargeEn },
            { "stat_962720646", ParserHelper.TryResolveMonsterGainChargeEn }
        };

    protected override List<StatGroup> GetStatGroups()
    {
        if (!_cacheService.TryGet(StatEnCacheKey, out List<StatGroup>? statEn))
        {
            statEn = LoadStats("stats_eng.json");
            _cacheService.Set(StatEnCacheKey, statEn);
        }

        return statEn ?? [];
    }

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

    protected override (string, string) ResolveMagicItemName(string nameText)
    {
        const string cacheKey = "poe1:item_base:en";
        if (!_cacheService.TryGet(cacheKey, out HashSet<string>? itemBaseHashSet))
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "datas\\poe", "items_en.txt");
            if (!File.Exists(path))
            {
                return (nameText, nameText);
            }

            var contents = File.ReadAllText(path);
            itemBaseHashSet = [];
            foreach (var content in contents.Split('\n'))
            {
                if (content.StartsWith("###"))
                {
                    continue;
                }

                itemBaseHashSet.Add(content.Trim().TrimEnd('\r').Trim());
            }

            _cacheService.Set(cacheKey, itemBaseHashSet);
        }

        var index = nameText.IndexOf(" of ", StringComparison.Ordinal);
        var tempText = nameText;
        if (index > 0)
        {
            tempText = nameText.Substring(0, index).Trim();
        }

        index = -1;
        do
        {
            var check = tempText.Substring(index + 1).Trim();
            if (itemBaseHashSet!.Contains(check))
            {
                return (nameText, check);
            }

            tempText = check;
            index = tempText.IndexOf(' ');
        } while (index > 0);

        return (nameText, nameText);
    }

    private static (bool, int?, int?) TryResolveAdditionalProjectile(Stat stat, string statText, ItemBase parsingItem)
    {
        var regex = stat.Text.Replace(" an ", " (\\d+) ");
        var match = Regex.Match(statText, regex);
        if (match.Success)
        {
            return (true, int.Parse(match.Groups[1].Value), null);
        }

        return (false, null, null);
    }

    private static (bool, int?, int?) TryResolveStaffStats(Stat stat, string statText, ItemBase parsingItem)
    {
        if (parsingItem.ItemType != ItemType.STAFF && parsingItem.ItemType != ItemType.WAR_STAFF)
        {
            return (false, null, null);
        }

        var realItemStat = ParserHelper.TrimEndOfBraces(statText);
        realItemStat += " (Staves)";
        if (TryMatchStat(stat, realItemStat, out var value, out var optionId))
        {
            return (true, value, optionId);
        }

        return (false, null, null);
    }
}