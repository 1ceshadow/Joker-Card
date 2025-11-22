using UnityEngine;

/// <summary>
/// 头像图片加载器
/// 位置：Assets/Scripts/Utils/AvatarSpriteLoader.cs
/// 功能：从大图片中切分头像
/// </summary>
public static class AvatarSpriteLoader
{
    private static Sprite[] avatarSprites;
    private static bool isLoaded = false;
    
    // 假设头像是网格布局
    // 需要根据实际图片调整以下参数：
    // - 图片总大小
    // - 每个头像的大小
    // - 网格布局（行数和列数）
    // - 头像总数
    
    // 示例：假设是 5列 x 2行 的网格，共10个头像
    private const int COLUMNS = 5; // 列数
    private const int ROWS = 2;    // 行数
    private const int TOTAL_AVATARS = 10; // 总共10个头像
    
    /// <summary>
    /// 加载并切分头像图片
    /// </summary>
    public static void LoadAvatarSprites()
    {
        if (isLoaded && avatarSprites != null)
            return;
            
        Texture2D avatarTexture = Resources.Load<Texture2D>("Avatars/Avatars");
        if (avatarTexture == null)
        {
            Debug.LogError("头像图片未找到：Assets/Resources/Avatars/Avatars.png");
            return;
        }
        
        // 计算每个头像的大小
        int avatarWidth = avatarTexture.width / COLUMNS;
        int avatarHeight = avatarTexture.height / ROWS;
        
        avatarSprites = new Sprite[TOTAL_AVATARS];
        
        // 假设从左到右、从上到下排列
        // 第1行：0, 1, 2, 3, 4
        // 第2行：5, 6, 7, 8, 9
        int index = 0;
        for (int row = 0; row < ROWS && index < TOTAL_AVATARS; row++)
        {
            for (int col = 0; col < COLUMNS && index < TOTAL_AVATARS; col++)
            {
                int x = col * avatarWidth;
                int y = (ROWS - 1 - row) * avatarHeight; // Unity坐标从下往上，所以是 ROWS-1-row
                
                Rect rect = new Rect(x, y, avatarWidth, avatarHeight);
                avatarSprites[index] = Sprite.Create(avatarTexture, rect, new Vector2(0.5f, 0.5f), 100);
                
                // 设置 Sprite 名称，方便调试
                avatarSprites[index].name = $"Avatar_{index}";
                
                index++;
            }
        }
        
        isLoaded = true;
        Debug.Log($"成功加载 {avatarSprites.Length} 个头像图片");
    }
    
    /// <summary>
    /// 获取头像 Sprite
    /// </summary>
    public static Sprite GetAvatarSprite(int avatarId)
    {
        if (avatarSprites == null || !isLoaded)
            LoadAvatarSprites();
            
        if (avatarSprites == null)
            return null;
            
        if (avatarId >= 0 && avatarId < avatarSprites.Length)
            return avatarSprites[avatarId];
            
        Debug.LogWarning($"头像索引超出范围: avatarId={avatarId}, max={avatarSprites.Length - 1}");
        return null;
    }
    
    /// <summary>
    /// 预加载所有头像（在游戏开始时调用）
    /// </summary>
    public static void PreloadAll()
    {
        LoadAvatarSprites();
    }
    
    /// <summary>
    /// 获取头像总数
    /// </summary>
    public static int GetAvatarCount()
    {
        if (avatarSprites == null || !isLoaded)
            LoadAvatarSprites();
            
        return avatarSprites != null ? avatarSprites.Length : 0;
    }
}

