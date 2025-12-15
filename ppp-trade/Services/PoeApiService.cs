using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ppp_trade.Models;

namespace ppp_trade.Services;

public class PoeApiService(CacheService cacheService)
{
    private const string UserAgent = "ppp-trade/1.0";
    private const string PoeNinjaLeagueMapCacheKey = "api:poe-ninja:league-map";
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private string _domain = "http://localhost";
    private string _game = "POE1";

    public async Task<JsonObject> FetchItems(IEnumerable<string> ids, string queryId)
    {
        var idStrings = string.Join(',', ids);
        var path = _game == "POE2"
            ? $"api/trade2/fetch/{idStrings}?query={queryId}"
            : $"api/trade/fetch/{idStrings}?query={queryId}";

        return await GetJsonAsync(GetFullUrl(path));
    }

    public async Task<JsonObject> GetCurrencyExchangeRate(string queryCurrencyName, string currencyType, string league,
        string forGame)
    {
        var cacheKey = $"{queryCurrencyName}:{league}:{forGame}";
        if (cacheService.TryGet(cacheKey, out JsonObject? value))
        {
            return value ?? new JsonObject();
        }

        var gameString = forGame == "POE2" ? "poe2" : "poe1";
        var normalizedCurrencyName = queryCurrencyName.Replace("'", "").Replace("(", "").Replace(")", "")
            .Replace(" ", "-").ToLower();
        var url =
            $"https://poe.ninja/{gameString}/api/economy/exchange/current/details?league={league}&type={currencyType}&id={normalizedCurrencyName}";

        var result = await GetJsonAsync(url);
        cacheService.Set(cacheKey, result, TimeSpan.FromHours(1));
        return result;
    }

    public async Task<string> GetPoeNinjaWebUrlAsync(string currencyType, string league, string detailsId)
    {
        var gameUrl = _game == "POE2" ? "poe2" : "poe1";
        if (!cacheService.TryGet(PoeNinjaLeagueMapCacheKey, out Dictionary<string, string>? leagueMap) ||
            leagueMap == null)
        {
            var dataObj = await GetJsonAsync($"https://poe.ninja/{gameUrl}/api/data/index-state");
            var economyLeagueArray = dataObj["economyLeagues"]?.AsArray();
            if (economyLeagueArray != null)
            {
                leagueMap = economyLeagueArray.Select(x => (x?["name"]?.ToString(), x?["url"]?.ToString()))
                    .Where(x => x is { Item1: not null, Item2: not null })
                    .ToDictionary(x => x.Item1!, x => x.Item2!);
                cacheService.Set(PoeNinjaLeagueMapCacheKey, leagueMap, TimeSpan.FromHours(1));
            }
            else
            {
                throw new InvalidOperationException("Failed to retrieve economy leagues from poe.ninja");
            }
        }

        if (!leagueMap.TryGetValue(league, out var leagueId))
        {
            throw new InvalidOperationException("League not found");
        }

        var normalizedCurrencyType = Regex.Replace(currencyType, "([a-z])([A-Z])", "$1-$2").ToLower();
        return $"https://poe.ninja/{gameUrl}/economy/{leagueId}/{normalizedCurrencyType}/{detailsId}";
    }

    public async Task<List<LeagueInfo>> GetLeaguesAsync()
    {
        var path = _game == "POE2" ? "api/trade2/data/leagues" : "api/trade/data/leagues";
        var response = await GetJsonAsync(GetFullUrl(path));

        const string field = "result";
        if (response[field] == null)
        {
            throw new InvalidOperationException("API response structure is invalid: missing 'result' property.");
        }

        var result = response[field]!.ToJsonString();
        return JsonSerializer.Deserialize<List<LeagueInfo>>(result, _jsonOptions) ??
               throw new InvalidOperationException("API returned null for league list.");
    }

    public string GetSearchWebsiteUrl(string queryId, string league)
    {
        var path = _game == "POE2"
            ? $"trade2/search/poe2/{league}/{queryId}"
            : $"trade/search/{league}/{queryId}";
        return GetFullUrl(path);
    }

    public async Task<JsonObject> GetTradeSearchResultAsync(string league, string query)
    {
        league = Uri.EscapeDataString(league);
        var path = _game == "POE2"
            ? $"api/trade2/search/poe2/{league}"
            : $"api/trade/search/{league}";

        using var client = CreateClient();
        var response = await client.PostAsync(GetFullUrl(path),
            new StringContent(query, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<JsonObject>(content, _jsonOptions) ??
               throw new InvalidOperationException("Empty response");
    }

    public void SwitchDomain(string domain)
    {
        _domain = domain;
    }

    public void SwitchGame(string game)
    {
        _game = game;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        client.DefaultRequestHeaders.Add("Accept", "*/*");
        return client;
    }

    private string GetFullUrl(string relativePath)
    {
        return $"{_domain.TrimEnd('/')}/{relativePath}";
    }

    private async Task<JsonObject> GetJsonAsync(string url)
    {
        var content = await GetStringAsync(url);
        return JsonSerializer.Deserialize<JsonObject>(content, _jsonOptions) ??
               throw new InvalidOperationException("Empty response");
    }

    private static async Task<string> GetStringAsync(string url)
    {
        using var client = CreateClient();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}