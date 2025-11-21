namespace ppp_trade.Models.Parsers;

internal class ChineseTradParser : IParser
{
    public bool IsMatch(string text)
    {
        return text.Contains("稀有度: ");
    }

    public object Parse(string text)
    {
        throw new NotImplementedException();
    }
}