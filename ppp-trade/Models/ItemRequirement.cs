using ppp_trade.Enums;

namespace ppp_trade.Models;

public struct ItemRequirement
{
    public ItemRequirementType ItemRequirementType { get; set; }

    public int Value { get; set; }
}