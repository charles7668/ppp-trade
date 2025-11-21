namespace ppp_trade.Models.Parsers;

public interface IParser
{
    bool IsMatch(string text);
    Item? Parse(string text);
}