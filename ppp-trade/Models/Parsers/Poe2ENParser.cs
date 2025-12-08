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

    protected override Dictionary<string, Func<Stat, string, ItemBase, (bool, int?, int?)>> SpecialCaseStat { get; } =
        new()
        {
            { "stat_3639275092", ParserHelper.TryResolveIncreasedAndDecreasedEn }
        };
}