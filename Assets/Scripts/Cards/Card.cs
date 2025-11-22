using UnityEngine;
using System;

/// <summary>
/// 卡牌数据类
/// 位置：Assets/Scripts/Cards/Card.cs
/// 功能：定义卡牌的基本属性（花色、点数）
/// </summary>
[System.Serializable]
public class Card : IComparable<Card>
{
    public enum Suit
    {
        Spades = 0,    // 黑桃
        Hearts = 1,    // 红桃
        Diamonds = 2,  // 方片
        Clubs = 3      // 梅花
    }

    public Suit suit;
    public int rank; // 2-14 (2-10, J=11, Q=12, K=13, A=14)

    public Card(Suit suit, int rank)
    {
        this.suit = suit;
        this.rank = rank;
    }

    public Card() { }

    // 获取牌面值（用于分数计算，A=14, K=13, Q=12, J=11, 2-10=2-10）
    public int GetChipValue()
    {
        return rank;
    }

    // 获取花色名称
    public string GetSuitName()
    {
        switch (suit)
        {
            case Suit.Spades: return "黑桃";
            case Suit.Hearts: return "红桃";
            case Suit.Diamonds: return "方片";
            case Suit.Clubs: return "梅花";
            default: return "";
        }
    }

    // 获取点数名称
    public string GetRankName()
    {
        switch (rank)
        {
            case 11: return "J";
            case 12: return "Q";
            case 13: return "K";
            case 14: return "A";
            default: return rank.ToString();
        }
    }

    // 用于排序
    public int CompareTo(Card other)
    {
        if (other == null) return 1;
        if (rank != other.rank)
            return rank.CompareTo(other.rank);
        return suit.CompareTo(other.suit);
    }

    // 深拷贝
    public Card Clone()
    {
        return new Card(suit, rank);
    }
}

