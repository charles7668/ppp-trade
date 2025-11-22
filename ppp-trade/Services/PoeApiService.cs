using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ppp_trade.Models;

namespace ppp_trade.Services;

public class PoeApiService
{
    private string _domain = "http://localhost";

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

    public async Task<JsonObject> GetTradeSearchResultAsync(string league, object query)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "ppp-trade/1.0");
        var normalizeDomain = _domain.TrimEnd('/') + "/";
        var response = await client.PostAsync($"{normalizeDomain}api/trade/search/{league}",
            new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json"));
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
