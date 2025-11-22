using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 小丑牌数据类
/// 位置：Assets/Scripts/Game/JokerData.cs
/// 功能：定义小丑牌的类型、效果、价格等
/// </summary>
[System.Serializable]
public class JokerData
{
    public enum JokerType
    {
        Joker,              // +4 Mult
        GreedyJoker,        // 每张方片 +3 Mult
        LustyJoker,         // 每张红桃 +3 Mult
        WrathfulJoker,      // 每张黑桃 +3 Mult
        GluttonousJoker,    // 每张梅花 +3 Mult
        JollyJoker          // 有对子 +8 Mult
    }

    public JokerType type;
    public string name;
    public int shopPrice;   // 商店价格
    public int sellPrice;   // 售卖价格

    // 默认构造函数（Mirror 网络序列化需要）
    public JokerData()
    {
        type = JokerType.Joker;
        name = "";
        shopPrice = 0;
        sellPrice = 0;
    }

    public JokerData(JokerType type)
    {
        this.type = type;
        InitializeJoker();
    }

    private void InitializeJoker()
    {
        switch (type)
        {
            case JokerType.Joker:
                name = "Joker";
                shopPrice = 3;
                sellPrice = 3;
                break;
            case JokerType.GreedyJoker:
                name = "Greedy Joker";
                shopPrice = 4;
                sellPrice = 3;
                break;
            case JokerType.LustyJoker:
                name = "Lusty Joker";
                shopPrice = 4;
                sellPrice = 3;
                break;
            case JokerType.WrathfulJoker:
                name = "Wrathful Joker";
                shopPrice = 4;
                sellPrice = 3;
                break;
            case JokerType.GluttonousJoker:
                name = "Gluttonous Joker";
                shopPrice = 4;
                sellPrice = 3;
                break;
            case JokerType.JollyJoker:
                name = "Jolly Joker";
                shopPrice = 6;
                sellPrice = 4;
                break;
        }
    }

    /// <summary>
    /// 计算小丑牌的加成（chips加成, mult加成）
    /// </summary>
    public (int chipsBonus, int multBonus) CalculateBonus(List<Card> cards)
    {
        int chipsBonus = 0;
        int multBonus = 0;

        switch (type)
        {
            case JokerType.Joker:
                multBonus = 4;
                break;

            case JokerType.GreedyJoker:
                // 每张方片 +3 Mult
                int diamondsCount = cards.Count(c => c.suit == Card.Suit.Diamonds);
                multBonus = diamondsCount * 3;
                break;

            case JokerType.LustyJoker:
                // 每张红桃 +3 Mult
                int heartsCount = cards.Count(c => c.suit == Card.Suit.Hearts);
                multBonus = heartsCount * 3;
                break;

            case JokerType.WrathfulJoker:
                // 每张黑桃 +3 Mult
                int spadesCount = cards.Count(c => c.suit == Card.Suit.Spades);
                multBonus = spadesCount * 3;
                break;

            case JokerType.GluttonousJoker:
                // 每张梅花 +3 Mult
                int clubsCount = cards.Count(c => c.suit == Card.Suit.Clubs);
                multBonus = clubsCount * 3;
                break;

            case JokerType.JollyJoker:
                // 有对子 +8 Mult
                if (Scoring.IsPair(cards))
                {
                    multBonus = 8;
                }
                break;
        }

        return (chipsBonus, multBonus);
    }

    /// <summary>
    /// 获取描述文本
    /// </summary>
    public string GetDescription()
    {
        switch (type)
        {
            case JokerType.Joker:
                return "+4 Mult";
            case JokerType.GreedyJoker:
                return "每张方片 +3 Mult";
            case JokerType.LustyJoker:
                return "每张红桃 +3 Mult";
            case JokerType.WrathfulJoker:
                return "每张黑桃 +3 Mult";
            case JokerType.GluttonousJoker:
                return "每张梅花 +3 Mult";
            case JokerType.JollyJoker:
                return "有对子 +8 Mult";
            default:
                return "";
        }
    }
}

