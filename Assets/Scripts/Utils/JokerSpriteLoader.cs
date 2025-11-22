using UnityEngine;

/// <summary>
/// 小丑牌图片加载器
/// 位置：Assets/Scripts/Utils/JokerSpriteLoader.cs
/// 功能：从大图片中切分小丑牌（前6张）
/// </summary>
public static class JokerSpriteLoader
{
    private static Sprite[] jokerSprites;
    private static bool isLoaded = false;
    
    // 假设小丑牌图片是网格布局
    // 需要根据实际图片调整以下参数：
    // - 图片总大小
    // - 每张小丑牌的大小
    // - 网格布局（行数和列数）
    
    // 加载所有小丑牌图片，但6种类型只使用前6张
    // 假设图片是网格布局，需要根据实际图片调整列数和行数
    private const int COLUMNS = 10; // 列数（需要根据实际图片调整）
    private const int ROWS = 16;    // 行数（需要根据实际图片调整）
    private const int TOTAL_JOKERS = 160; // 总共160种小丑牌（COLUMNS * ROWS）
    
    /// <summary>
    /// 加载并切分小丑牌图片
    /// </summary>
    public static void LoadJokerSprites()
    {
        if (isLoaded && jokerSprites != null)
            return;
            
        Texture2D jokerTexture = Resources.Load<Texture2D>("Jokers/Jokers");
        if (jokerTexture == null)
        {
            Debug.LogError("小丑牌图片未找到：Assets/Resources/Jokers/Jokers.png");
            return;
        }
        
        // 计算每张小丑牌的大小
        int jokerWidth = jokerTexture.width / COLUMNS;
        int jokerHeight = jokerTexture.height / ROWS;
        
        // 加载所有小丑牌图片
        jokerSprites = new Sprite[TOTAL_JOKERS];
        
        // 小丑牌类型顺序（根据 GDD）：
        // 0: Joker (对应图片第1张，索引0)
        // 1: GreedyJoker (对应图片第2张，索引1)
        // 2: LustyJoker (对应图片第3张，索引2)
        // 3: WrathfulJoker (对应图片第4张，索引3)
        // 4: GluttonousJoker (对应图片第5张，索引4)
        // 5: JollyJoker (对应图片第6张，索引5)
        // 之后的小丑牌类型会使用索引6及以后的图片
        
        // 假设从左到右、从上到下排列
        int index = 0;
        for (int row = 0; row < ROWS && index < TOTAL_JOKERS; row++)
        {
            for (int col = 0; col < COLUMNS && index < TOTAL_JOKERS; col++)
            {
                int x = col * jokerWidth;
                int y = (ROWS - 1 - row) * jokerHeight; // Unity坐标从下往上，所以是 ROWS-1-row
                
                Rect rect = new Rect(x, y, jokerWidth, jokerHeight);
                jokerSprites[index] = Sprite.Create(jokerTexture, rect, new Vector2(0.5f, 0.5f), 100);
                
                // 设置 Sprite 名称，方便调试
                jokerSprites[index].name = $"Joker_{index}";
                
                index++;
            }
        }
        
        isLoaded = true;
        Debug.Log($"成功加载 {jokerSprites.Length} 张小丑牌图片");
    }
    
    /// <summary>
    /// 获取小丑牌 Sprite
    /// </summary>
    public static Sprite GetJokerSprite(JokerData.JokerType type)
    {
        if (jokerSprites == null || !isLoaded)
            LoadJokerSprites();
            
        if (jokerSprites == null)
            return null;
        
        // 根据类型获取索引（6种类型使用前6张图片）
        int typeIndex = GetJokerIndex(type);
        if (typeIndex >= 0 && typeIndex < jokerSprites.Length)
            return jokerSprites[typeIndex];
            
        Debug.LogWarning($"小丑牌索引超出范围: type={type}, index={typeIndex}, max={jokerSprites.Length - 1}");
        return null;
    }
    
    /// <summary>
    /// 根据小丑牌类型获取索引
    /// </summary>
    private static int GetJokerIndex(JokerData.JokerType type)
    {
        switch (type)
        {
            case JokerData.JokerType.Joker:
                return 0;
            case JokerData.JokerType.GreedyJoker:
                return 1;
            case JokerData.JokerType.LustyJoker:
                return 2;
            case JokerData.JokerType.WrathfulJoker:
                return 3;
            case JokerData.JokerType.GluttonousJoker:
                return 4;
            case JokerData.JokerType.JollyJoker:
                return 5;
            default:
                return -1;
        }
    }
    
    /// <summary>
    /// 预加载所有小丑牌（在游戏开始时调用）
    /// </summary>
    public static void PreloadAll()
    {
        LoadJokerSprites();
    }
}

