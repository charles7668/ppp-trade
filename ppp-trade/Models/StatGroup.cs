namespace ppp_trade.Models;

public class StatGroup
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<Stat> Entries { get; set; } = [];
}