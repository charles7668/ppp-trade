using System.IO;
using System.Text.Json;
using ppp_trade.Enums;
using ppp_trade.Models;
using ppp_trade.Services;

namespace ppp_trade.Builders;

public class RequestBodyBuilder(CacheService cacheService)
{
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
            _ => null
        };
    }


    private async Task<(string? uniqueName, string? uniqueBase)> MapUniqueNameAsync(string legendName,
        string legendBase)
    {
        var nameMapCacheKey = "unique:tw2en:name";
        var baseMapCacheKey = "unique:tw2en:base";
        cacheService.TryGet(baseMapCacheKey, out Dictionary<string, string>? baseMap);
        if (!cacheService.TryGet(nameMapCacheKey, out Dictionary<string, string>? nameMap))
        {
            var enNameFile = Path.Combine("datas\\poe", "unique_item_names_eng.json");
            var twNameFile = Path.Combine("datas\\poe", "unique_item_names_tw.json");
            var enBaseFile = Path.Combine("datas\\poe", "unique_item_bases_eng.json");
            var twBaseFile = Path.Combine("datas\\poe", "unique_item_bases_tw.json");
            if (!File.Exists(enNameFile) ||
                !File.Exists(twNameFile) ||
                !File.Exists(enBaseFile) ||
                !File.Exists(twBaseFile))
            {
                return (null, null);
            }

            var content = await File.ReadAllTextAsync(enNameFile);
            var enNameList = JsonSerializer.Deserialize<List<string>>(content)!;
            content = await File.ReadAllTextAsync(twNameFile);
            var twNameList = JsonSerializer.Deserialize<List<string>>(content)!;
            nameMap = new Dictionary<string, string>();
            for (var i = 0; i < twNameList.Count; i++)
            {
                nameMap.Add(twNameList[i], enNameList[i]);
            }

            cacheService.Set(nameMapCacheKey, nameMap);

            content = await File.ReadAllTextAsync(enBaseFile);
            var enBaseList = JsonSerializer.Deserialize<List<string>>(content)!;
            content = await File.ReadAllTextAsync(twBaseFile);
            var twBaseList = JsonSerializer.Deserialize<List<string>>(content)!;
            baseMap = new Dictionary<string, string>();
            for (var i = 0; i < twBaseList.Count; i++)
            {
                baseMap.Add(twBaseList[i], enBaseList[i]);
            }

            cacheService.Set(baseMapCacheKey, baseMap);
        }

        return (nameMap![legendName], baseMap![legendBase]);
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

    private async Task<string?> MapBaseItemNameAsync(string name)
    {
        var baseMapCacheKey = "white_item:tw2en:base";
        if (!cacheService.TryGet(baseMapCacheKey, out Dictionary<string, string>? baseMap))
        {
            var enBaseFile = Path.Combine("datas\\poe", "items_en.txt");
            var twBaseFile = Path.Combine("datas\\poe", "items_tw.txt");
            if (!File.Exists(enBaseFile) ||
                !File.Exists(twBaseFile))
            {
                return null;
            }

            var content = await File.ReadAllTextAsync(twBaseFile);
            List<string> twBaseList = content.Replace("\r", "").Split('\n')
                .Where(x => !x.StartsWith("###") && !string.IsNullOrWhiteSpace(x))
                .ToList();
            content = await File.ReadAllTextAsync(enBaseFile);
            List<string> enBaseList = content.Replace("\r", "").Split('\n')
                .Where(x => !x.StartsWith("###") && !string.IsNullOrWhiteSpace(x))
                .ToList();
            baseMap = new Dictionary<string, string>();
            if (twBaseList.Count != enBaseList.Count)
            {
                return null;
            }

            for (var i = 0; i < enBaseList.Count; i++)
            {
                baseMap.TryAdd(twBaseList[i], enBaseList[i]);
            }

            cacheService.Set(baseMapCacheKey, baseMap);
        }

        return baseMap![name];
    }

    private IEnumerable<object> GetStatsQueryParam(SearchRequest searchRequest)
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

    public async Task<object?> BuildSearchBodyAsync(SearchRequest searchRequest)
    {
        string? itemName = null;
        string? baseName = null;
        var item = searchRequest.Item!.Value;
        if (item.Rarity == Rarity.UNIQUE)
        {
            if (searchRequest.ServerOption == ServerOption.INTERNATIONAL_SERVER)
            {
                (itemName, baseName) = await MapUniqueNameAsync(item.ItemName, item.ItemBase);
            }
            else
            {
                (itemName, baseName) = (item.ItemName, item.ItemBase);
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
                baseName = await MapBaseItemNameAsync(item.ItemBase);
            }
            else
            {
                baseName = item.ItemBase;
            }
        }

        List<object> statsParam = GetStatsQueryParam(searchRequest).ToList();

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
                        rarity = new
                        {
                            option = RarityToString(item.Rarity)
                        },
                        category = new
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
                            CorruptedState.YES => "yes",
                            _ => "no"
                        },
                        foulborn_item = searchRequest.FoulBorn == YesNoAnyOption.ANY
                            ? null
                            : new { option = searchRequest.FoulBorn == YesNoAnyOption.YES ? "true" : "false" }
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
}