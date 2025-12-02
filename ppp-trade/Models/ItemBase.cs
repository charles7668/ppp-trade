using ppp_trade.Enums;

namespace ppp_trade.Models;

/// <summary>
/// Common property between poe1 and poe2
/// </summary>
public abstract class ItemBase
{
    public Rarity Rarity { get; set; }

    public string ItemName { get; set; } = null!;

    public string ItemBaseName { get; set; } = null!;

    public ItemType ItemType { get; set; }

    public IEnumerable<ItemRequirement> Requirements { get; set; } = [];

    public IEnumerable<ItemStat> Stats { get; set; } = [];

    public int ItemLevel { get; set; }

    public int? GemLevel { get; set; }
}