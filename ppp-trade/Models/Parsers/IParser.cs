namespace ppp_trade.Models.Parsers;

public interface IParser
{
    bool IsMatch(string text);
    object Parse(string text);
}