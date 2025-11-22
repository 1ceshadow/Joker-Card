using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 分数计算系统
/// 位置：Assets/Scripts/Cards/Scoring.cs
/// 功能：根据牌型计算分数（chips × multiplier）
/// </summary>
public static class Scoring
{
    // 牌型定义
    public enum HandType
    {
        HighCard,        // 单张/散牌
        Pair,            // 对子
        TwoPair,         // 两对
        ThreeOfAKind,    // 三条
        Straight,        // 顺子
        Flush,           // 同花
        FullHouse,       // 葫芦
        FourOfAKind,     // 四条
        StraightFlush    // 顺金
    }

    // 牌型基础数据
    private static readonly Dictionary<HandType, (int chips, int mult)> HandTypeData = new Dictionary<HandType, (int, int)>
    {
        { HandType.HighCard, (0, 1) },
        { HandType.Pair, (8, 2) },
        { HandType.TwoPair, (20, 2) },
        { HandType.ThreeOfAKind, (30, 3) },
        { HandType.Straight, (30, 4) },
        { HandType.Flush, (35, 4) },
        { HandType.FullHouse, (40, 4) },
        { HandType.FourOfAKind, (60, 7) },
        { HandType.StraightFlush, (100, 8) }
    };

    /// <summary>
    /// 计算分数
    /// </summary>
    /// <param name="cards">出的牌</param>
    /// <param name="jokers">小丑牌列表</param>
    /// <returns>最终分数</returns>
    public static int CalculateScore(List<Card> cards, List<JokerData> jokers)
    {
        if (cards == null || cards.Count == 0)
            return 0;

        // 1. 检测牌型
        HandType handType = DetectHandType(cards);
        var (baseChips, baseMult) = HandTypeData[handType];

        // 2. 计算基础chips = 牌型chips + 单张牌面值chips
        int totalChips = baseChips;
        foreach (Card card in cards)
        {
            totalChips += card.GetChipValue();
        }

        // 3. 计算基础mult = 牌型mult
        int totalMult = baseMult;

        // 4. 应用小丑牌加成
        if (jokers != null && jokers.Count > 0)
        {
            foreach (JokerData joker in jokers)
            {
                if (joker != null)
                {
                    var (chipsBonus, multBonus) = joker.CalculateBonus(cards);
                    totalChips += chipsBonus;
                    totalMult += multBonus;
                }
            }
        }

        // 5. 最终分数 = 总chips × 总mult（四舍五入）
        float finalScore = totalChips * totalMult;
        return Mathf.RoundToInt(finalScore);
    }

    /// <summary>
    /// 检测牌型（按优先级从高到低）
    /// </summary>
    public static HandType DetectHandType(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
            return HandType.HighCard;

        // 按点数排序
        List<Card> sortedCards = new List<Card>(cards);
        sortedCards.Sort((a, b) => a.rank.CompareTo(b.rank));

        bool isFlush = IsFlush(sortedCards);
        bool isStraight = IsStraight(sortedCards);

        // 顺金
        if (isStraight && isFlush)
            return HandType.StraightFlush;

        // 四条
        int fourOfAKind = GetFourOfAKind(sortedCards);
        if (fourOfAKind > 0)
            return HandType.FourOfAKind;

        // 葫芦
        if (IsFullHouse(sortedCards))
            return HandType.FullHouse;

        // 同花
        if (isFlush)
            return HandType.Flush;

        // 顺子
        if (isStraight)
            return HandType.Straight;

        // 三条
        int threeOfAKind = GetThreeOfAKind(sortedCards);
        if (threeOfAKind > 0)
            return HandType.ThreeOfAKind;

        // 两对
        if (IsTwoPair(sortedCards))
            return HandType.TwoPair;

        // 对子
        if (IsPair(sortedCards))
            return HandType.Pair;

        // 散牌
        return HandType.HighCard;
    }

    /// <summary>
    /// 检查是否为同花
    /// </summary>
    public static bool IsFlush(List<Card> cards)
    {
        if (cards.Count < 5)
            return false;
        Card.Suit firstSuit = cards[0].suit;
        return cards.All(c => c.suit == firstSuit);
    }

    /// <summary>
    /// 检查是否为顺子
    /// </summary>
    public static bool IsStraight(List<Card> cards)
    {
        if (cards.Count < 5)
            return false;

        List<int> ranks = cards.Select(c => c.rank).OrderBy(r => r).ToList();

        // 检查普通顺子
        bool isNormalStraight = true;
        for (int i = 1; i < ranks.Count; i++)
        {
            if (ranks[i] != ranks[i - 1] + 1)
            {
                isNormalStraight = false;
                break;
            }
        }

        // 检查A-2-3-4-5顺子（A作为1）
        if (!isNormalStraight && ranks.Contains(14))
        {
            List<int> lowAceRanks = ranks.Select(r => r == 14 ? 1 : r).OrderBy(r => r).ToList();
            bool isLowAceStraight = true;
            for (int i = 1; i < lowAceRanks.Count; i++)
            {
                if (lowAceRanks[i] != lowAceRanks[i - 1] + 1)
                {
                    isLowAceStraight = false;
                    break;
                }
            }
            return isLowAceStraight;
        }

        return isNormalStraight;
    }

    /// <summary>
    /// 检查是否为四条
    /// </summary>
    public static int GetFourOfAKind(List<Card> cards)
    {
        var rankGroups = cards.GroupBy(c => c.rank);
        foreach (var group in rankGroups)
        {
            if (group.Count() == 4)
                return group.Key;
        }
        return 0;
    }

    /// <summary>
    /// 检查是否为葫芦
    /// </summary>
    public static bool IsFullHouse(List<Card> cards)
    {
        if (cards.Count < 5)
            return false;
        var rankGroups = cards.GroupBy(c => c.rank).OrderByDescending(g => g.Count()).ToList();
        return rankGroups.Count >= 2 && rankGroups[0].Count() == 3 && rankGroups[1].Count() == 2;
    }

    /// <summary>
    /// 检查是否为三条
    /// </summary>
    public static int GetThreeOfAKind(List<Card> cards)
    {
        var rankGroups = cards.GroupBy(c => c.rank);
        foreach (var group in rankGroups)
        {
            if (group.Count() >= 3)
                return group.Key;
        }
        return 0;
    }

    /// <summary>
    /// 检查是否为两对
    /// </summary>
    public static bool IsTwoPair(List<Card> cards)
    {
        var rankGroups = cards.GroupBy(c => c.rank).Where(g => g.Count() >= 2).ToList();
        return rankGroups.Count >= 2;
    }

    /// <summary>
    /// 检查是否为对子
    /// </summary>
    public static bool IsPair(List<Card> cards)
    {
        var rankGroups = cards.GroupBy(c => c.rank);
        return rankGroups.Any(g => g.Count() >= 2);
    }
}

