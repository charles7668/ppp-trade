using ppp_trade.Enums;

namespace ppp_trade.Models;

public struct Item
{
    public Rarity Rarity { get; set; }

    public string ItemName { get; set; }

    public string ItemBase { get; set; }

    public ItemType ItemType { get; set; }

    public IEnumerable<ItemRequirement> Requirements { get; set; }

    public IEnumerable<ItemStat> Stats { get; set; }

    public int ItemLevel { get; set; }

    public int Link { get; set; }
}