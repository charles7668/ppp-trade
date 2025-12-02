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
            _ => null
        };
    }

    private async Task<(string? uniqueName, string? uniqueBase)> MapUniqueNameAsync(string uniqueName,
        string uniqueBase, string forGame)
    {
        var dataFolder = forGame == "POE2" ? "datas\\poe2" : "datas\\poe";
        var fullNameMapCacheKey = forGame == "POE2" ? "unique:tw2en:full" : "unique:poe2:tw2en:full";
        if (!cacheService.TryGet(fullNameMapCacheKey, out Dictionary<string, string>? fullMap))
        {
            var enNameFile = Path.Combine(dataFolder, "unique_item_names_eng.json");
            var twNameFile = Path.Combine(dataFolder, "unique_item_names_tw.json");
            var enBaseFile = Path.Combine(dataFolder, "unique_item_bases_eng.json");
            var twBaseFile = Path.Combine(dataFolder, "unique_item_bases_tw.json");
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
            content = await File.ReadAllTextAsync(enBaseFile);
            var enBaseList = JsonSerializer.Deserialize<List<string>>(content)!;
            content = await File.ReadAllTextAsync(twBaseFile);
            var twBaseList = JsonSerializer.Deserialize<List<string>>(content)!;
            if (twNameList.Count != enNameList.Count ||
                twBaseList.Count != enBaseList.Count ||
                twNameList.Count != twBaseList.Count)
            {
                return (null, null);
            }

            var count = twNameList.Count;
            fullMap = new Dictionary<string, string>();

            for (var i = 0; i < count; i++)
            {
                fullMap.Add(twNameList[i] + ";" + twBaseList[i], enNameList[i] + ";" + enBaseList[i]);
            }

            cacheService.Set(fullNameMapCacheKey, fullMap);
        }

        string? target = null;
        fullMap?.TryGetValue(uniqueName + ";" + uniqueBase, out target);
        if (target == null)
        {
            return (uniqueName, uniqueBase);
        }

        var split = target.Split(';');
        return (split[0], split[1]);
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

    private async Task<string?> MapBaseItemNameAsync(string name, string forGame)
    {
        var dataFolder = forGame == "POE2" ? "datas\\poe2" : "datas\\poe";
        var baseMapCacheKey = $"{forGame}:white_item:tw2en:base";
        if (!cacheService.TryGet(baseMapCacheKey, out Dictionary<string, string>? baseMap))
        {
            var enBaseFile = Path.Combine(dataFolder, "items_en.txt");
            var twBaseFile = Path.Combine(dataFolder, "items_tw.txt");
            if (!File.Exists(enBaseFile) ||
                !File.Exists(twBaseFile))
            {
                return null;
            }

            var content = await File.ReadAllTextAsync(twBaseFile);
            var twBaseList = content.Replace("\r", "").Split('\n')
                .Where(x => !x.StartsWith("###") && !string.IsNullOrWhiteSpace(x))
                .ToList();
            content = await File.ReadAllTextAsync(enBaseFile);
            var enBaseList = content.Replace("\r", "").Split('\n')
                .Where(x => !x.StartsWith("###") && !string.IsNullOrWhiteSpace(x))
                .ToList();
            baseMap = new Dictionary<string, string>();
            if (twBaseList.Count != enBaseList.Count)
            {
                return name;
            }

            for (var i = 0; i < enBaseList.Count; i++)
            {
                baseMap.TryAdd(twBaseList[i], enBaseList[i]);
            }

            cacheService.Set(baseMapCacheKey, baseMap);
        }

        return baseMap?.GetValueOrDefault(name, name) ?? name;
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

    public async Task<object?> BuildSearchBodyAsync(SearchRequestBase searchRequest, string forGame)
    {
        string? itemName = null;
        string? baseName = null;
        var item = searchRequest.Item!;
        if (item.Rarity == Rarity.UNIQUE)
        {
            if (searchRequest.ServerOption == ServerOption.INTERNATIONAL_SERVER)
            {
                (itemName, baseName) = await MapUniqueNameAsync(item.ItemName, item.ItemBaseName, forGame);
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
                baseName = await MapBaseItemNameAsync(item.ItemBaseName, forGame);
            }
            else
            {
                baseName = item.ItemBaseName;
            }
        }

        List<object> statsParam = GetStatsQueryParam(searchRequest).ToList();
        object? corruptedFilter = null;
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
}