using ppp_trade.Enums;

namespace ppp_trade.Services;

public class IconService
{
    public string? GetCurrencyIcon(Currency? currency)
    {
        var url = currency switch
        {
            Currency.ALT =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxNYWdpYyIsInNjYWxlIjoxfV0/6308fc8ca2/CurrencyRerollMagic.png",
            Currency.FUSING =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxTb2NrZXRMaW5rcyIsInNjYWxlIjoxfV0/c5e1959880/CurrencyRerollSocketLinks.png",
            Currency.ALCH =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lVcGdyYWRlVG9SYXJlIiwic2NhbGUiOjF9XQ/0c72cd1d44/CurrencyUpgradeToRare.png",
            Currency.CHAOS =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxSYXJlIiwic2NhbGUiOjF9XQ/46a2347805/CurrencyRerollRare.png",
            Currency.GCP =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lHZW1RdWFsaXR5Iiwic2NhbGUiOjF9XQ/dbe9678a28/CurrencyGemQuality.png",
            Currency.EXALTED =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lBZGRNb2RUb1JhcmUiLCJzY2FsZSI6MX1d/33f2656aea/CurrencyAddModToRare.png",
            Currency.CHROME =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxTb2NrZXRDb2xvdXJzIiwic2NhbGUiOjF9XQ/19c8ddae20/CurrencyRerollSocketColours.png",
            Currency.JEWELLERS =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxTb2NrZXROdW1iZXJzIiwic2NhbGUiOjF9XQ/ba411ff58a/CurrencyRerollSocketNumbers.png",
            Currency.ENGINEERS =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvRW5naW5lZXJzT3JiIiwic2NhbGUiOjF9XQ/114b671d41/EngineersOrb.png",
            Currency.INFUSED_ENGINEERS_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvSW5mdXNlZEVuZ2luZWVyc09yYiIsInNjYWxlIjoxfV0/55774baf2f/InfusedEngineersOrb.png",
            Currency.CHANCE =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lVcGdyYWRlUmFuZG9tbHkiLCJzY2FsZSI6MX1d/a3f9bf0917/CurrencyUpgradeRandomly.png",
            Currency.CHISEL =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lNYXBRdWFsaXR5Iiwic2NhbGUiOjF9XQ/0246313b99/CurrencyMapQuality.png",
            Currency.SCOUR =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lDb252ZXJ0VG9Ob3JtYWwiLCJzY2FsZSI6MX1d/a0981d67fe/CurrencyConvertToNormal.png",
            Currency.BLESSED =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lJbXBsaWNpdE1vZCIsInNjYWxlIjoxfV0/48e700cc20/CurrencyImplicitMod.png",
            Currency.REGRET =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lQYXNzaXZlU2tpbGxSZWZ1bmQiLCJzY2FsZSI6MX1d/32d499f562/CurrencyPassiveSkillRefund.png",
            Currency.REGAL =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lVcGdyYWRlTWFnaWNUb1JhcmUiLCJzY2FsZSI6MX1d/0ded706f57/CurrencyUpgradeMagicToRare.png",
            Currency.DIVINE =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lNb2RWYWx1ZXMiLCJzY2FsZSI6MX1d/ec48896769/CurrencyModValues.png",
            Currency.VAAL =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lWYWFsIiwic2NhbGUiOjF9XQ/593fe2e22e/CurrencyVaal.png",
            Currency.ANNUL =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQW5udWxsT3JiIiwic2NhbGUiOjF9XQ/0858a418ac/AnnullOrb.png",
            Currency.ORB_OF_BINDING =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQmluZGluZ09yYiIsInNjYWxlIjoxfV0/aac9579bd2/BindingOrb.png",
            Currency.ANCIENT_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQW5jaWVudE9yYiIsInNjYWxlIjoxfV0/83015d0dc9/AncientOrb.png",
            Currency.ORB_OF_HORIZONS =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvSG9yaXpvbk9yYiIsInNjYWxlIjoxfV0/0891338fb0/HorizonOrb.png",
            Currency.HARBINGERS_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvSGFyYmluZ2VyT3JiIiwic2NhbGUiOjF9XQ/0a26e01f15/HarbingerOrb.png",
            Currency.WISDOM =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lJZGVudGlmaWNhdGlvbiIsInNjYWxlIjoxfV0/c2d03ed3fd/CurrencyIdentification.png",
            Currency.PORTAL =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lQb3J0YWwiLCJzY2FsZSI6MX1d/d92d3478a0/CurrencyPortal.png",
            Currency.SCRAP =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lBcm1vdXJRdWFsaXR5Iiwic2NhbGUiOjF9XQ/fc4e26afbc/CurrencyArmourQuality.png",
            Currency.WHETSTONE =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lXZWFwb25RdWFsaXR5Iiwic2NhbGUiOjF9XQ/c9cd72719e/CurrencyWeaponQuality.png",
            Currency.BAUBLE =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lGbGFza1F1YWxpdHkiLCJzY2FsZSI6MX1d/59e57027e5/CurrencyFlaskQuality.png",
            Currency.TRANSMUTE =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lVcGdyYWRlVG9NYWdpYyIsInNjYWxlIjoxfV0/ded9e8ee63/CurrencyUpgradeToMagic.png",
            Currency.AUG =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lBZGRNb2RUb01hZ2ljIiwic2NhbGUiOjF9XQ/d879c15321/CurrencyAddModToMagic.png",
            Currency.MIRROR =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lEdXBsaWNhdGUiLCJzY2FsZSI6MX1d/8d7fea29d1/CurrencyDuplicate.png",
            Currency.ETERNAL =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lJbXByaW50T3JiIiwic2NhbGUiOjF9XQ/49500c70ba/CurrencyImprintOrb.png",
            Currency.ROGUES_MARKER =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvSGVpc3QvSGVpc3RDb2luQ3VycmVuY3kiLCJzY2FsZSI6MX1d/335e66630d/HeistCoinCurrency.png",
            Currency.CRUSADERS_EXALTED_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvSW5mbHVlbmNlIEV4YWx0cy9DcnVzYWRlck9yYiIsInNjYWxlIjoxfV0/8b48230188/CrusaderOrb.png",
            Currency.REDEEMERS_EXALTED_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvSW5mbHVlbmNlIEV4YWx0cy9FeXJpZU9yYiIsInNjYWxlIjoxfV0/8ec9b52d65/EyrieOrb.png",
            Currency.HUNTERS_EXALTED_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvSW5mbHVlbmNlIEV4YWx0cy9CYXNpbGlza09yYiIsInNjYWxlIjoxfV0/cd2131d564/BasiliskOrb.png",
            Currency.WARLORDS_EXALTED_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvSW5mbHVlbmNlIEV4YWx0cy9Db25xdWVyb3JPcmIiLCJzY2FsZSI6MX1d/57f0d85951/ConquerorOrb.png",
            Currency.AWAKENERS_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvVHJhbnNmZXJPcmIiLCJzY2FsZSI6MX1d/f3b1c1566f/TransferOrb.png",
            Currency.MAVENS_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvTWF2ZW5PcmIiLCJzY2FsZSI6MX1d/f307d80bfd/MavenOrb.png",
            Currency.FACETORS =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lHZW1FeHBlcmllbmNlIiwic2NhbGUiOjF9XQ/7011b1ed48/CurrencyGemExperience.png",
            Currency.PRIME_REGRADING_LENS =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQWx0ZXJuYXRlU2tpbGxHZW1DdXJyZW5jeSIsInNjYWxlIjoxfV0/d514645103/AlternateSkillGemCurrency.png",
            Currency.SECONDARY_REGRADING_LENS =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQWx0ZXJuYXRlU3VwcG9ydEdlbUN1cnJlbmN5Iiwic2NhbGUiOjF9XQ/bde7f354d4/AlternateSupportGemCurrency.png",
            Currency.TEMPERING_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvRGl2aW5lRW5jaGFudEJvZHlBcm1vdXJDdXJyZW5jeSIsInNjYWxlIjoxfV0/37681eda1c/DivineEnchantBodyArmourCurrency.png",
            Currency.TAILORING_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvRGl2aW5lRW5jaGFudFdlYXBvbkN1cnJlbmN5Iiwic2NhbGUiOjF9XQ/d417654a23/DivineEnchantWeaponCurrency.png",
            Currency.AWAKENED_SEXTANT =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQXRsYXNSYWRpdXNUaWVyMyIsInNjYWxlIjoxfV0/0561e8049e/AtlasRadiusTier3.png",
            Currency.ELEVATED_SEXTANT =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQXRsYXNSYWRpdXNUaWVyNCIsInNjYWxlIjoxfV0/3e53bafe61/AtlasRadiusTier4.png",
            Currency.SURVEYORS_COMPASS =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvU3VydmV5b3JzQ29tcGFzcyIsInNjYWxlIjoxfV0/e67bfaa9cf/SurveyorsCompass.png",
            Currency.ORB_OF_UNMAKING =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvUmVncmV0T3JiIiwic2NhbGUiOjF9XQ/beae1b00c7/RegretOrb.png",
            Currency.BLESSING_XOPH =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQnJlYWNoL0JyZWFjaFVwZ3JhZGVyRmlyZSIsInNjYWxlIjoxfV0/16a58db13d/BreachUpgraderFire.png",
            Currency.BLESSING_TUL =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQnJlYWNoL0JyZWFjaFVwZ3JhZGVyQ29sZCIsInNjYWxlIjoxfV0/3573fedbf3/BreachUpgraderCold.png",
            Currency.BLESSING_ESH =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQnJlYWNoL0JyZWFjaFVwZ3JhZGVyTGlnaHRuaW5nIiwic2NhbGUiOjF9XQ/3d1db83ad2/BreachUpgraderLightning.png",
            Currency.BLESSING_UUL_NETOL =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQnJlYWNoL0JyZWFjaFVwZ3JhZGVyUGh5c2ljYWwiLCJzY2FsZSI6MX1d/f230a19a13/BreachUpgraderPhysical.png",
            Currency.BLESSING_CHAYULA =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQnJlYWNoL0JyZWFjaFVwZ3JhZGVyQ2hhb3MiLCJzY2FsZSI6MX1d/45e8da717e/BreachUpgraderChaos.png",
            Currency.VEILED_CHAOS_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvVmVpbGVkQ2hhb3NPcmIiLCJzY2FsZSI6MX1d/fd913b89d0/VeiledChaosOrb.png",
            Currency.ENKINDLING_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvRXhwZWRpdGlvbi9GbGFza1BsYXRlIiwic2NhbGUiOjF9XQ/7c1a584a8d/FlaskPlate.png",
            Currency.INSTILLING_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvRXhwZWRpdGlvbi9GbGFza0luamVjdG9yIiwic2NhbGUiOjF9XQ/efc518b1be/FlaskInjector.png",
            Currency.SACRED_ORB =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvU2FjcmVkT3JiIiwic2NhbGUiOjF9XQ/0380fd0dba/SacredOrb.png",
            Currency.STACKED_DECK =>
                "/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvRGl2aW5hdGlvbi9EZWNrIiwic2NhbGUiOjF9XQ/8e83aea79a/Deck.png",
            _ => null
        };
        return url == null ? null : $"https://web.poecdn.com{url}";
    }
}