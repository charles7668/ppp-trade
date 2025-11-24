using ppp_trade.Enums;

namespace ppp_trade.Services;

public class GameStringService
{
    public string? Get(GameString gameStringEnum)
    {
        return gameStringEnum switch
        {
            GameString.NORMAL => "普通",
            GameString.MAGIC => "魔法",
            GameString.RARE => "稀有",
            GameString.UNIQUE => "傳奇",
            _ => null
        };
    }
}