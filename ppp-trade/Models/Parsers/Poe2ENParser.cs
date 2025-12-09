using ppp_trade.Enums;
using ppp_trade.Services;

namespace ppp_trade.Models.Parsers;

public class Poe2EnParser(CacheService cacheService) : Poe2TWParser(cacheService)
{
    protected override string ItemRarityKeyword => "Rarity: ";

    protected override string ItemTypeKeyword => "Item Class: ";

    protected override string ItemRequirementKeyword => "Requirements:";

    protected override string ItemSocketKeyword => "Sockets: ";

    protected override string ItemLevelKeyword => "Item Level: ";

    protected override string ItemGrantsSkillKeyword => "Grants Skill: ";

    protected override string ItemSpiritKeyword => "Spirit: ";

    protected override string StatCacheKey => "parser:poe2:stat_en";

    protected override string LocalKeyword => "(local)";

    protected override string ReqLevelKeyword => "Level";

    protected override string ReqIntKeyword => "Int";

    protected override string ReqDexKeyword => "Dex";

    protected override string ReqStrKeyword => "Str";

    protected override Dictionary<string, ItemType> ItemTypeMap { get; set; } = new()
    {
        { "Claws", ItemType.CLAW },
        { "Daggers", ItemType.DAGGER },
        { "Wands", ItemType.WAND },
        { "One Hand Swords", ItemType.ONE_HAND_SWORD },
        { "One Hand Axes", ItemType.ONE_HAND_AXE },
        { "One Hand Maces", ItemType.ONE_HAND_MACE },
        { "Sceptres", ItemType.SCEPTRE },
        { "Spears", ItemType.SPEAR },
        { "Flails", ItemType.FLAIL },

        { "Bows", ItemType.BOW },
        { "Staves", ItemType.STAFF },
        { "Two Hand Swords", ItemType.TWO_HAND_SWORD },
        { "Two Hand Axes", ItemType.TWO_HAND_AXE },
        { "Two Hand Maces", ItemType.TWO_HAND_MACE },
        { "Quarterstaves", ItemType.QUARTERSTAFF },
        { "Fishing Rods", ItemType.FISHING_ROD },
        { "Crossbows", ItemType.CROSSBOW },
        { "Traps", ItemType.TRAP },

        { "Quivers", ItemType.QUIVER },
        { "Shields", ItemType.SHIELD },
        { "Bucklers", ItemType.BUCKLER },
        { "Foci", ItemType.FOCI },

        { "Helmets", ItemType.HELMET },
        { "Body Armours", ItemType.BODY_ARMOUR },
        { "Gloves", ItemType.GLOVES },
        { "Boots", ItemType.BOOTS },
        { "Belts", ItemType.BELT },

        { "Amulets", ItemType.AMULET },
        { "Rings", ItemType.RING },

        { "Stackable Currency", ItemType.STACKABLE_CURRENCY },
        { "Socketable", ItemType.SOCKETABLE },
        { "Tablet", ItemType.TABLET },
        { "Jewels", ItemType.JEWEL },
        { "Charms", ItemType.CHARMS },

        { "Life Flasks", ItemType.FLASK },
        { "Mana Flasks", ItemType.FLASK },

        { "Waystones", ItemType.WAY_STONE },
        { "Vault Keys", ItemType.VAULT_KEY }
    };

    protected override Dictionary<string, Func<Stat, string, ItemBase, (bool, int?, int?)>> SpecialCaseStat { get; } =
        new()
        {
            { "stat_3639275092", ParserHelper.TryResolveIncreasedAndDecreasedEn }
        };
}