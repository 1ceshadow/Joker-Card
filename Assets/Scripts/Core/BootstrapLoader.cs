using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bootstrap 场景加载器
/// 位置：Assets/Scripts/Core/BootstrapLoader.cs
/// 功能：初始化全局持久对象，然后自动跳转到 MainMenu
/// 
/// 使用方法：
/// 1. 创建一个新的空场景叫 Bootstrap
/// 2. 在场景中创建一个空 GameObject 叫 "BootstrapLoader"
/// 3. 挂载此脚本
/// 4. 将 NetworkManager prefab 拖到 networkManagerPrefab 字段
/// 5. 在 Build Settings 中将 Bootstrap 设为场景 0
/// </summary>
public class BootstrapLoader : MonoBehaviour
{
    [Header("全局 Prefab 引用")]
    [Tooltip("NetworkManager 预制体")]
    [SerializeField] private GameObject networkManagerPrefab;

    [Header("设置")]
    [Tooltip("初始化完成后跳转的场景")]
    [SerializeField] private string targetScene = "MainMenu";

    [Tooltip("跳转前的延迟（秒），可用于显示 Logo")]
    [SerializeField] private float loadDelay = 0f;

    private void Start()
    {
        InitializeGlobalObjects();

        if (loadDelay > 0)
        {
            Invoke(nameof(LoadTargetScene), loadDelay);
        }
        else
        {
            LoadTargetScene();
        }
    }

    /// <summary>
    /// 初始化所有全局持久对象
    /// </summary>
    private void InitializeGlobalObjects()
    {
        // 检查 NetworkManager 是否已存在（防止重复创建）
        if (NetworkManagerCustom.Instance == null)
        {
            if (networkManagerPrefab != null)
            {
                GameObject nm = Instantiate(networkManagerPrefab);
                nm.name = "NetworkManager"; // 保持名字整洁
                Debug.Log("[Bootstrap] NetworkManager 已创建");
            }
            else
            {
                Debug.LogError("[Bootstrap] networkManagerPrefab 未设置！请在 Inspector 中指定");
            }
        }
        else
        {
            Debug.Log("[Bootstrap] NetworkManager 已存在，跳过创建");
        }

        // 这里可以添加其他全局对象的初始化
        // 例如：AudioManager, SaveManager 等
    }

    /// <summary>
    /// 加载目标场景
    /// </summary>
    private void LoadTargetScene()
    {
        Debug.Log($"[Bootstrap] 正在加载 {targetScene} 场景...");
        SceneManager.LoadScene(targetScene);
    }
}
