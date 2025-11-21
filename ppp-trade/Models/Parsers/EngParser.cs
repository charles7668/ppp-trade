namespace ppp_trade.Models.Parsers;

internal class EngParser : IParser
{
    public bool IsMatch(string text)
    {
        return text.Contains("Item Class: ");
    }

    public Item? Parse(string text)
    {
        throw new NotImplementedException();
    }
}