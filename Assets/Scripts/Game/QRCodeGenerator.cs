using UnityEngine;

/// <summary>
/// 二维码生成器
/// 位置：Assets/Scripts/Game/QRCodeGenerator.cs
/// 功能：使用ZXing生成二维码图片
/// 注意：需要导入ZXing库（通过Package Manager或Asset Store）
/// </summary>
public class QRCodeGenerator : MonoBehaviour
{
    public static QRCodeGenerator Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 生成二维码纹理
    /// </summary>
    public Texture2D GenerateQRCode(string text, int width = 256, int height = 256)
    {
        try
        {
            // 使用ZXing生成二维码
            // 注意：需要先导入ZXing库
            // 可以通过以下方式导入：
            // 1. Package Manager -> Add package from git URL: https://github.com/zxing/zxing-unity.git
            // 2. 或使用Asset Store中的ZXing插件

#if ZXING_AVAILABLE
            var writer = new ZXing.BarcodeWriter
            {
                Format = ZXing.BarcodeFormat.QR_CODE,
                Options = new ZXing.QrCode.QrCodeEncodingOptions
                {
                    Height = height,
                    Width = width
                }
            };

            var result = writer.Write(text);
            return result;
#else
            // 如果没有ZXing，返回一个占位纹理
            Debug.LogWarning("ZXing库未导入，无法生成二维码。请导入ZXing库。");
            return CreatePlaceholderTexture(width, height);
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"生成二维码失败: {e.Message}");
            return CreatePlaceholderTexture(width, height);
        }
    }

    /// <summary>
    /// 创建占位纹理（当ZXing不可用时）
    /// </summary>
    private Texture2D CreatePlaceholderTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}

