using ppp_trade.Enums;

namespace ppp_trade.Models.Parsers;

public class ChineseTradParser : IParser
{
    private const string RARITY_KEYWORD = "稀有度: ";
    private const string ITEM_TYPE_KEYWORD = "物品種類: ";

    public bool IsMatch(string text)
    {
        return text.Contains(RARITY_KEYWORD);
    }

    public Item? Parse(string text)
    {
        var lines = text.Split("\r\n");
        var indexOfRarity = Array.FindIndex(lines, l => l.StartsWith(RARITY_KEYWORD));
        if (indexOfRarity == -1)
            return null;

        var parsedItem = new Item();
        var parsingState = ParsingState.PARSING_UNKNOW;
        for (var i = 0; i < lines.Length; ++i)
        {
            var line = lines[i];
            switch (parsingState)
            {
                case ParsingState.PARSING_RARITY:
                    parsedItem.Rarity = ResolveRarity(line);
                    parsingState = ParsingState.PARSING_ITEM_NAME;
                    break;
                case ParsingState.PARSING_ITEM_NAME:
                    parsedItem.ItemName = line;
                    parsingState = ParsingState.PARSING_ITEM_BASE;
                    break;
                case ParsingState.PARSING_ITEM_BASE:
                    parsedItem.ItemBase = line;
                    break;
                case ParsingState.PARSING_ITEM_TYPE:
                    parsedItem.ItemType = ResolveItemType(line);
                    break;
                case ParsingState.PARSING_UNKNOW:
                    if (i == indexOfRarity)
                    {
                        i--;
                        parsingState = ParsingState.PARSING_RARITY;
                    }
                    else if (line.StartsWith(ITEM_TYPE_KEYWORD))
                    {
                        i--;
                        parsingState = ParsingState.PARSING_ITEM_TYPE;
                    }

                    break;
            }
        }

        return parsedItem;
    }

    private static ItemType ResolveItemType(string lineText)
    {
        var substr = lineText.Substring(ITEM_TYPE_KEYWORD.Length, lineText.Length - ITEM_TYPE_KEYWORD.Length).Trim();
        // todo 分析更多item type
        if (substr.StartsWith("異界地圖"))
            return ItemType.MAP;
        if (substr.StartsWith("契約書"))
            return ItemType.CONTRACT;
        if (substr.StartsWith("藍圖"))
            return ItemType.BLUEPRINT;

        return ItemType.OTHER;
    }

    private static Rarity ResolveRarity(string lineText)
    {
        var rarityStr = lineText.Substring(RARITY_KEYWORD.Length, lineText.Length - RARITY_KEYWORD.Length);
        switch (rarityStr)
        {
            // todo 補充除了稀有外的稀有度case, 目前手邊資訊不足
            case "稀有":
                return Rarity.RARE;
                break;
        }

        return Rarity.NORMAL;
    }

    private enum ParsingState
    {
        PARSING_ITEM_TYPE,
        PARSING_RARITY,
        PARSING_ITEM_NAME,
        PARSING_ITEM_BASE,
        PARSING_UNKNOW
    }
}