using UnityEngine;

/// <summary>
/// 资源初始化器
/// 位置：Assets/Scripts/Utils/ResourceInitializer.cs
/// 功能：在游戏开始时预加载所有资源
/// </summary>
public class ResourceInitializer : MonoBehaviour
{
    private void Awake()
    {
        // 预加载所有资源
        CardSpriteLoader.PreloadAll();
        JokerSpriteLoader.PreloadAll();
        AvatarSpriteLoader.PreloadAll();
        
        Debug.Log("资源初始化完成");
    }
}

