namespace ppp_trade.Models;

public class Poe2Item : ItemBase
{
    public int RuneSockets { get; set; }

    public int Spirit { get; set; }

    public string? GrantsSkill { get; set; }
}