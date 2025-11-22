using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 卡牌图片加载器
/// 位置：Assets/Scripts/Utils/CardSpriteLoader.cs
/// 功能：从大图片中切分52张卡牌
/// </summary>
public static class CardSpriteLoader
{
    private static Sprite[] cardSprites;
    private static Sprite cardBackSprite;
    private static bool isLoaded = false;
    
    // 卡牌排列：从左到右 2-10-JQKA (13列)，从上到下 红桃-梅花-方块-黑桃 (4行)
    // 图片大小：923x380
    // 每张卡牌大小：923/13 = 71px, 380/4 = 95px
    
    /// <summary>
    /// 加载并切分卡牌图片
    /// </summary>
    public static void LoadCardSprites()
    {
        if (isLoaded && cardSprites != null)
            return;
            
        Texture2D cardTexture = Resources.Load<Texture2D>("PlayingCards/PlayingCards");
        if (cardTexture == null)
        {
            Debug.LogError("卡牌图片未找到：Assets/Resources/PlayingCards/PlayingCards.png");
            return;
        }
        
        cardSprites = new Sprite[52];
        int cardWidth = 71;  // 923 / 13
        int cardHeight = 95; // 380 / 4
        
        // 花色顺序：红桃(0), 梅花(1), 方块(2), 黑桃(3)
        // 点数顺序：2(0), 3(1), 4(2), 5(3), 6(4), 7(5), 8(6), 9(7), 10(8), J(9), Q(10), K(11), A(12)
        // 注意：Unity 的坐标原点在左下角，所以需要从下往上读取
        int index = 0;
        for (int suit = 0; suit < 4; suit++)
        {
            for (int rank = 0; rank < 13; rank++)
            {
                int x = rank * cardWidth;
                int y = (3 - suit) * cardHeight; // 从下往上，所以是 3-suit
                
                Rect rect = new Rect(x, y, cardWidth, cardHeight);
                cardSprites[index] = Sprite.Create(cardTexture, rect, new Vector2(0.5f, 0.5f), 100);
                
                // 设置 Sprite 名称，方便调试
                cardSprites[index].name = $"Card_{suit}_{rank + 2}";
                
                index++;
            }
        }
        
        isLoaded = true;
        Debug.Log($"成功加载 {cardSprites.Length} 张卡牌图片");
    }
    
    /// <summary>
    /// 获取卡牌 Sprite
    /// </summary>
    public static Sprite GetCardSprite(Card.Suit suit, int rank)
    {
        if (cardSprites == null || !isLoaded)
            LoadCardSprites();
            
        if (cardSprites == null)
            return null;
            
        // 计算索引：suit * 13 + (rank - 2)
        int suitIndex = (int)suit;
        int rankIndex = rank - 2; // rank: 2-14, index: 0-12
        
        if (rankIndex < 0 || rankIndex > 12)
        {
            Debug.LogWarning($"无效的卡牌点数: {rank}");
            return null;
        }
        
        int spriteIndex = suitIndex * 13 + rankIndex;
        if (spriteIndex >= 0 && spriteIndex < cardSprites.Length)
            return cardSprites[spriteIndex];
            
        Debug.LogWarning($"卡牌索引超出范围: suit={suitIndex}, rank={rankIndex}, index={spriteIndex}");
        return null;
    }
    
    /// <summary>
    /// 加载卡牌背景
    /// </summary>
    public static void LoadCardBack()
    {
        if (cardBackSprite != null)
            return;
            
        // 尝试加载单张图片
        cardBackSprite = Resources.Load<Sprite>("PlayingCards/Cardsback");
        
        // 如果失败，尝试加载多张并随机选择
        if (cardBackSprite == null)
        {
            Sprite[] backs = Resources.LoadAll<Sprite>("PlayingCards/Cardsback");
            if (backs != null && backs.Length > 0)
            {
                cardBackSprite = backs[Random.Range(0, backs.Length)];
                Debug.Log($"从 {backs.Length} 张背景中随机选择了一张");
            }
        }
        
        if (cardBackSprite == null)
        {
            Debug.LogWarning("卡牌背景图片未找到：Assets/Resources/PlayingCards/Cardsback");
        }
    }
    
    /// <summary>
    /// 获取卡牌背景 Sprite
    /// </summary>
    public static Sprite GetCardBackSprite()
    {
        if (cardBackSprite == null)
            LoadCardBack();
        return cardBackSprite;
    }
    
    /// <summary>
    /// 预加载所有卡牌（在游戏开始时调用）
    /// </summary>
    public static void PreloadAll()
    {
        LoadCardSprites();
        LoadCardBack();
    }
}

