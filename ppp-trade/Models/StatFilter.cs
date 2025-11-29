namespace ppp_trade.Models;

public class StatFilter
{
    public bool Disabled { get; set; }

    public int? MinValue { get; set; }

    public int? MaxValue { get; set; }

    public string? StatId { get; set; }
}