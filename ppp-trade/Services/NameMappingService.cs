using System.IO;
using System.Text.Json;
using ppp_trade.Enums;

namespace ppp_trade.Services;

public class NameMappingService(CacheService cacheService)
{
    public string? MapPoeNinjaCurrencyType(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.STACKABLE_CURRENCY => "Currency",
            ItemType.UNCUT_SKILL_GEM or ItemType.UNCUT_SPIRIT_GEM or ItemType.UNCUT_SUPPORT_GEM => "UncutGems",
            _ => null
        };
    }

    public async Task<string?> MapBaseItemNameAsync(string name, string forGame)
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

    public async Task<(string? uniqueName, string? uniqueBase)> MapUniqueNameAsync(string uniqueName,
        string uniqueBase, string forGame)
    {
        var dataFolder = forGame == "POE2" ? "datas\\poe2" : "datas\\poe";
        var uniqueNameCacheKey = forGame == "POE2" ? "unique:tw2en:unique" : "unique:poe2:tw2en:unique";
        if (!cacheService.TryGet(uniqueNameCacheKey, out Dictionary<string, (string, string, string)>? uniqueNameMap))
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
            uniqueNameMap = new Dictionary<string, (string, string, string)>();

            for (var i = 0; i < count; i++)
            {
                uniqueNameMap.Add(twNameList[i] + " " + twBaseList[i],
                    (enNameList[i] + " " + enBaseList[i], enNameList[i], enBaseList[i]));
            }

            cacheService.Set(uniqueNameCacheKey, uniqueNameMap);
        }

        return uniqueNameMap?.TryGetValue(uniqueName, out var target) is true
            ? (target.Item2, target.Item3)
            : (uniqueName, uniqueBase);
    }
}