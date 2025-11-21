namespace ppp_trade.Models.Parsers;

public class ParserFactory
{
    public ParserFactory(IEnumerable<IParser> parsers)
    {
        _parsers = parsers;
    }

    private readonly IEnumerable<IParser> _parsers;

    public IParser? GetParser(string text)
    {
        return _parsers.FirstOrDefault(x => x.IsMatch(text));
    }
}