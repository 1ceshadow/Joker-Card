using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 牌组管理类
/// 位置：Assets/Scripts/Cards/Deck.cs
/// 功能：管理52张扑克牌，洗牌、发牌、抽牌等操作
/// </summary>
public class Deck
{
    private List<Card> cards = new List<Card>();

    public Deck()
    {
        InitializeDeck();
    }

    /// <summary>
    /// 初始化标准52张牌（去掉大小王）
    /// </summary>
    private void InitializeDeck()
    {
        cards.Clear();
        foreach (Card.Suit suit in System.Enum.GetValues(typeof(Card.Suit)))
        {
            for (int rank = 2; rank <= 14; rank++) // 2-10, J(11), Q(12), K(13), A(14)
            {
                cards.Add(new Card(suit, rank));
            }
        }
    }

    /// <summary>
    /// 洗牌（随机打乱）
    /// </summary>
    public void Shuffle()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            Card temp = cards[i];
            int randomIndex = Random.Range(i, cards.Count);
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }

    /// <summary>
    /// 发指定数量的牌
    /// </summary>
    public List<Card> DealCards(int count)
    {
        List<Card> dealtCards = new List<Card>();
        for (int i = 0; i < count && cards.Count > 0; i++)
        {
            dealtCards.Add(cards[0]);
            cards.RemoveAt(0);
        }
        return dealtCards;
    }

    /// <summary>
    /// 抽一张牌
    /// </summary>
    public Card DrawCard()
    {
        if (cards.Count > 0)
        {
            Card card = cards[0];
            cards.RemoveAt(0);
            return card;
        }
        return null;
    }

    /// <summary>
    /// 将牌放回牌组（随机位置）
    /// </summary>
    public void ReturnCards(List<Card> cardsToReturn)
    {
        foreach (Card card in cardsToReturn)
        {
            int randomIndex = Random.Range(0, cards.Count + 1);
            cards.Insert(randomIndex, card);
        }
    }

    /// <summary>
    /// 将单张牌放回牌组
    /// </summary>
    public void ReturnCard(Card card)
    {
        if (card != null)
        {
            int randomIndex = Random.Range(0, cards.Count + 1);
            cards.Insert(randomIndex, card);
        }
    }

    /// <summary>
    /// 获取剩余牌数
    /// </summary>
    public int GetRemainingCount()
    {
        return cards.Count;
    }

    /// <summary>
    /// 检查牌组是否为空
    /// </summary>
    public bool IsEmpty()
    {
        return cards.Count == 0;
    }

    /// <summary>
    /// 重置牌组（重新初始化并洗牌）
    /// </summary>
    public void Reset()
    {
        InitializeDeck();
        Shuffle();
    }
}

