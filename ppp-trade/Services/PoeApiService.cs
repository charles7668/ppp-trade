using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ppp_trade.Models;

namespace ppp_trade.Services;

public class PoeApiService
{
    private string _domain = "http://localhost";

    public async Task<JsonObject> FetchItems(IEnumerable<string> ids, string queryId)
    {
        var idStrings = string.Join(',', ids);
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "ppp-trade/1.0");
        client.DefaultRequestHeaders.Add("Accept", "*/*");
        var normalizeDomain = _domain.TrimEnd('/') + "/";
        var requestUrl = $"{normalizeDomain}api/trade/fetch/{idStrings}?query={queryId}";
        var response = await client.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<JsonObject>(content, options) ??
               throw new InvalidOperationException("Empty response");
    }

    public async Task<List<LeagueInfo>> GetLeaguesAsync()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "ppp-trade/1.0");
        var normalizeDomain = _domain.TrimEnd('/') + "/";
        var response = await client.GetAsync($"{normalizeDomain}api/trade/data/leagues");
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
        return $"{normalizeDomain}trade/search/{league}/{queryId}";
    }

    public async Task<JsonObject> GetTradeSearchResultAsync(string league, string query)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "ppp-trade/1.0");
        client.DefaultRequestHeaders.Add("Accept", "*/*");
        var normalizeDomain = _domain.TrimEnd('/') + "/";
        var reqContent = new StringContent(query, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{normalizeDomain}api/trade/search/{league}")
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
}