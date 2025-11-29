namespace ppp_trade.Models.Parsers;

public interface IParser
{
    bool IsMatch(string text, string game);
    ItemBase? Parse(string text);
}