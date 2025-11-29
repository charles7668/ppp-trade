namespace ppp_trade.Models.Parsers;

public class ParserFactory(IEnumerable<IParser> parsers)
{
    public IParser? GetParser(string text, string game)
    {
        return parsers.FirstOrDefault(x => x.IsMatch(text, game));
    }
}