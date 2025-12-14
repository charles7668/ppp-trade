using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ppp_trade.Models;

namespace ppp_trade.Services;

public class PoeApiService(CacheService cacheService)
{
    private readonly string _poeNinjaLeagueMapCacheKey = "api:poe-ninja:league-map";
    private string _domain = "http://localhost";
    private string _game = "POE1";

    public async Task<JsonObject> FetchItems(IEnumerable<string> ids, string queryId)
    {
        var idStrings = string.Join(',', ids);
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "ppp-trade/1.0");
        client.DefaultRequestHeaders.Add("Accept", "*/*");
        var normalizeDomain = _domain.TrimEnd('/') + "/";
        var requestUrl = $"{normalizeDomain}api/trade/fetch/{idStrings}?query={queryId}";
        if (_game == "POE2")
        {
            requestUrl = $"{normalizeDomain}api/trade2/fetch/{idStrings}?query={queryId}";
        }

        var response = await client.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<JsonObject>(content, options) ??
               throw new InvalidOperationException("Empty response");
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
        var reqUrl =
            $"https://poe.ninja/{gameString}/api/economy/exchange/current/details?league={league}&type={currencyType}&id={normalizedCurrencyName}";
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "ppp-trade/1.0");
        client.DefaultRequestHeaders.Add("Accept", "*/*");
        var response = await client.GetAsync(reqUrl);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<JsonObject>(content, options) ??
                     throw new InvalidOperationException("Empty response");
        cacheService.Set(cacheKey, result, TimeSpan.FromHours(1));
        return result;
    }

    public async Task<string> GetPoeNinjaWebUrlAsync(string currencyType, string league, string detailsId)
    {
        var gameUrl = _game == "POE2" ? "poe2" : "poe1";
        if (!cacheService.TryGet(_poeNinjaLeagueMapCacheKey, out Dictionary<string, string>? leagueMap) ||
            leagueMap == null)
        {
            using var poeNinjaClient = new HttpClient();
            poeNinjaClient.DefaultRequestHeaders.Add("User-Agent", "ppp-trade/1.0");
            var poeNinjaResponse = await poeNinjaClient.GetAsync($"https://poe.ninja/{gameUrl}/api/data/index-state");
            poeNinjaResponse.EnsureSuccessStatusCode();
            var dataContents = await poeNinjaResponse.Content.ReadAsStringAsync();
            var dataObj = JsonSerializer.Deserialize<JsonObject>(dataContents);
            var economyLeagueArray = dataObj?["economyLeagues"]?.AsArray();
            if (economyLeagueArray != null)
            {
                leagueMap = economyLeagueArray.Select(x => (x?["name"]?.ToString(), x?["url"]?.ToString()))
                    .Where(x => x is { Item1: not null, Item2: not null })
                    .ToDictionary(x => x.Item1!, x => x.Item2!);
                cacheService.Set(_poeNinjaLeagueMapCacheKey, leagueMap, TimeSpan.FromHours(1));
            }
            else
            {
                throw new InvalidOperationException("Failed to retrieve economy leagues from poe.ninja");
            }
        }

        if (!leagueMap!.ContainsKey(league))
        {
            throw new InvalidOperationException("League not found");
        }

        var normalizedCurrencyType = Regex.Replace(currencyType, "([a-z])([A-Z])", "$1-$2").ToLower();
        return $"https://poe.ninja/{gameUrl}/economy/{leagueMap[league]}/{normalizedCurrencyType}/{detailsId}";
    }

    public async Task<List<LeagueInfo>> GetLeaguesAsync()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "ppp-trade/1.0");
        var normalizeDomain = _domain.TrimEnd('/') + "/";
        var requestUrl = $"{normalizeDomain}api/trade/data/leagues";
        if (_game == "POE2")
        {
            requestUrl = $"{normalizeDomain}api/trade2/data/leagues";
        }

        var response = await client.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        using var doc = JsonDocument.Parse(content);
        if (!doc.RootElement.TryGetProperty("result", out var resultElement))
        {
            throw new InvalidOperationException("API response structure is invalid: missing 'result' property.");
        }

        var result = resultElement.GetRawText();
        return JsonSerializer.Deserialize<List<LeagueInfo>>(result, options) ??
               throw new InvalidOperationException("API returned null for league list.");
    }

    public string GetSearchWebsiteUrl(string queryId, string league)
    {
        var normalizeDomain = _domain.TrimEnd('/') + "/";
        var url = $"{normalizeDomain}trade/search/{league}/{queryId}";
        if (_game == "POE2")
        {
            url = $"{normalizeDomain}trade2/search/poe2/{league}/{queryId}";
        }

        return url;
    }

    public async Task<JsonObject> GetTradeSearchResultAsync(string league, string query)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "ppp-trade/1.0");
        client.DefaultRequestHeaders.Add("Accept", "*/*");
        var normalizeDomain = _domain.TrimEnd('/') + "/";
        var reqContent = new StringContent(query, Encoding.UTF8, "application/json");
        league = Uri.EscapeDataString(league);
        var requestUrl = $"{normalizeDomain}api/trade/search/{league}";
        if (_game == "POE2")
        {
            requestUrl = $"{normalizeDomain}api/trade2/search/poe2/{league}";
        }

        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = reqContent
        };
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<JsonObject>(content, options) ??
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
}