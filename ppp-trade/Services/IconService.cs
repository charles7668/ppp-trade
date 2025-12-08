using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ppp_trade.Services;

public class IconService(CacheService cacheService)
{
    public string? GetCurrencyIcon(string currency, string forGame)
    {
        var cacheKey = $"{forGame}:currency:icons";
        if (cacheService.TryGet(cacheKey, out Dictionary<string, string>? iconDictionary))
        {
            return iconDictionary?[currency];
        }

        var currencyFile = Path.Combine("datas", forGame == "POE1" ? "poe" : "poe2", "currency.json");
        var jsonText = File.ReadAllText(currencyFile);
        var jObj = JsonSerializer.Deserialize<JsonArray>(jsonText);
        iconDictionary = new Dictionary<string, string>();
        foreach (var entry in jObj?.SelectMany(x => x?["entries"]!.AsArray()!)!)
        {
            var text = entry?["id"]?.ToString();
            var img = entry?["image"]?.ToString();
            if (text is not null && img is not null)
            {
                iconDictionary.Add(text, img);
            }
        }

        cacheService.Set(cacheKey, iconDictionary);
        if (!iconDictionary.TryGetValue(currency, out var imgUrl))
        {
            return null;
        }

        return "https://web.poecdn.com" + imgUrl;
    }
}