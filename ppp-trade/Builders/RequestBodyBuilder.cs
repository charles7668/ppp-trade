using System.IO;
using ppp_trade.Enums;
using ppp_trade.Models;
using ppp_trade.Services;

namespace ppp_trade.Builders;

public class RequestBodyBuilder(NameMappingService nameMappingService)
{
    public async Task<object?> BuildSearchBodyAsync(SearchRequestBase searchRequest, string forGame)
    {
        string? itemName = null;
        string? baseName = null;
        var item = searchRequest.Item!;
        if (item.Rarity == Rarity.UNIQUE)
        {
            if (searchRequest.ServerOption == ServerOption.INTERNATIONAL_SERVER)
            {
                (itemName, baseName) =
                    await nameMappingService.MapUniqueNameAsync(item.ItemName, item.ItemBaseName, forGame);
            }
            else
            {
                (itemName, baseName) = (item.ItemName, item.ItemBaseName);
            }

            if (itemName == null)
            {
                throw new FileNotFoundException("缺失傳奇道具文字相關檔案");
            }
        }
        else if (searchRequest.FilterItemBase)
        {
            if (searchRequest.ServerOption == ServerOption.INTERNATIONAL_SERVER)
            {
                baseName = await nameMappingService.MapBaseItemNameAsync(item.ItemBaseName, forGame);
            }
            else
            {
                baseName = item.ItemBaseName;
            }
        }

        var statsParam = GetStatsQueryParam(searchRequest).ToList();
        object? foulBornFilter = null;
        switch (forGame)
        {
            case "POE1":
                var poe1Request = (Poe1SearchRequest)searchRequest;
                foulBornFilter = poe1Request.FoulBorn == YesNoAnyOption.ANY
                    ? null
                    : new { option = poe1Request.FoulBorn == YesNoAnyOption.YES ? "true" : "false" };
                break;
            case "POE2":
                break;
            default:
                throw new ArgumentException("invalid game name");
        }

        var queryObj = new
        {
            status = new
            {
                option = searchRequest.TradeType
            },
            name = itemName,
            type = baseName,
            stats = statsParam.Count == 0 ? null : statsParam,
            filters = new
            {
                type_filters = new
                {
                    disabled = false,
                    filters = new
                    {
                        rarity = RarityToString(item.Rarity) == null
                            ? (object?)null
                            : new
                            {
                                option = RarityToString(item.Rarity)
                            },
                        category = ItemTypeToString(item.ItemType) == null
                            ? null
                            : new
                            {
                                option = ItemTypeToString(item.ItemType)
                            }
                    }
                },
                misc_filters = new
                {
                    disabled = false,
                    filters = new
                    {
                        corrupted = searchRequest.CorruptedState switch
                        {
                            CorruptedState.ANY => null,
                            _ => new
                            {
                                option = searchRequest.CorruptedState == CorruptedState.YES ? "yes" : "no"
                            }
                        },
                        foulborn_item = foulBornFilter,
                        gem_level = !searchRequest.FilterGemLevel
                            ? null
                            : new
                            {
                                min = searchRequest.GemLevelMin,
                                max = searchRequest.GemLevelMax
                            }
                    }
                },
                trade_filters = new
                {
                    filters = new
                    {
                        sale_type = new
                        {
                            option = "priced"
                        },
                        collapse = new
                        {
                            option = searchRequest.CollapseByAccount == CollapseByAccount.YES ? "yes" : "no"
                        }
                    }
                }
            }
        };
        var query = new
        {
            query = queryObj,
            sort = new
            {
                price = "asc"
            }
        };
        return query;
    }

    private IEnumerable<object> GetStatsQueryParam(SearchRequestBase searchRequest)
    {
        var statList = new List<object>();
        foreach (var stat in searchRequest.Stats)
        {
            statList.Add(new
            {
                id = stat.StatId,
                disabled = stat.Disabled,
                value = stat.MinValue == null && stat.MaxValue == null
                    ? null
                    : new
                    {
                        min = stat.MinValue,
                        max = stat.MaxValue
                    }
            });
        }


        return
        [
            new
            {
                type = "and",
                filters = statList
            }
        ];
    }

    private string? ItemTypeToString(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.HELMET => "armour.helmet",
            ItemType.ONE_HAND_AXE => "weapon.oneaxe",
            ItemType.ONE_HAND_MACE => "weapon.onemace",
            ItemType.ONE_HAND_SWORD => "weapon.onesword",
            ItemType.BOW => "weapon.bow",
            ItemType.CLAW => "weapon.claw",
            ItemType.DAGGER => "weapon.basedagger",
            ItemType.RUNE_DAGGER => "weapon.runedagger",
            ItemType.SCEPTRE => "weapon.sceptre",
            ItemType.STAFF => "weapon.staff",
            ItemType.TWO_HAND_AXE => "weapon.twoaxe",
            ItemType.TWO_HAND_MACE => "weapon.twomace",
            ItemType.TWO_HAND_SWORD => "weapon.twosword",
            ItemType.WAND => "weapon.wand",
            ItemType.WAR_STAFF => "weapon.warstaff",
            ItemType.FISHING_ROD => "weapon.rod",
            ItemType.BODY_ARMOUR => "armour.chest",
            ItemType.BOOTS => "armour.boots",
            ItemType.GLOVES => "armour.gloves",
            ItemType.SHIELD => "armour.shield",
            ItemType.QUIVER => "armour.quiver",
            ItemType.AMULET => "accessory.amulet",
            ItemType.BELT => "accessory.belt",
            ItemType.RING => "accessory.ring",
            ItemType.JEWEL => "jewel.base",
            ItemType.FLASK => "flask",
            ItemType.WAY_STONE => "map.waystone",
            ItemType.CORPSE => "corpse",
            ItemType.ACTIVE_GEM => "gem.activegem",
            ItemType.SUPPORT_GEM => "gem.supportgem",
            ItemType.CLUSTER_JEWEL => "jewel.cluster",
            ItemType.SPEAR => "weapon.spear",
            ItemType.FLAIL => "weapon.flail",
            ItemType.QUARTERSTAFF => "weapon.warstaff",
            ItemType.CROSSBOW => "weapon.crossbow",
            ItemType.FOCI => "armour.focus",
            ItemType.BUCKLER => "armour.buckler",
            ItemType.TABLET => "map.tablet",
            _ => null
        };
    }

    private string? RarityToString(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.NORMAL => "normal",
            Rarity.UNIQUE => "unique",
            Rarity.MAGIC => "magic",
            Rarity.RARE => "rare",
            _ => null
        };
    }
}