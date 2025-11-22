namespace ppp_trade.Models;

public class Stat
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public StatOption? Option { get; set; }
}

public class StatOption
{
    public List<StatOptionDetail> Options { get; set; } = [];
}

public class StatOptionDetail
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
}