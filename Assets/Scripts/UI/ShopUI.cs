using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 商店UI
/// 位置：Assets/Scripts/UI/ShopUI.cs
/// 功能：显示商店中的小丑牌，购买功能
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("商店小丑牌列表")]
    [SerializeField] private Transform shopJokersParent;
    [SerializeField] private GameObject jokerItemPrefab;

    [Header("玩家小丑牌列表")]
    [SerializeField] private Transform playerJokersParent;
    
    [Header("商店信息UI")]
    [SerializeField] private TextMeshProUGUI moneyText;          // 玩家金币显示
    [SerializeField] private TextMeshProUGUI jokerCountText;     // 小丑牌数量显示
    [SerializeField] private Button refreshButton;               // 刷新商店按钮
    [SerializeField] private TextMeshProUGUI refreshCostText;    // 刷新费用显示
    [SerializeField] private TextMeshProUGUI messageText;        // 提示信息
    
    private float messageTimer = 0f;
    private const float MESSAGE_DISPLAY_TIME = 3f;

    private List<GameObject> shopJokerObjects = new List<GameObject>();
    private List<GameObject> playerJokerObjects = new List<GameObject>();
    
    // 是否在主菜单场景（决定使用本地数据还是网络数据）
    private bool isMainMenuScene = true;

    private void Start()
    {
        // 订阅事件
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopChanged += OnShopDataChanged;
            ShopManager.Instance.OnPurchaseResult += ShowMessage;
        }
        
        // 设置刷新按钮
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(OnRefreshShopClicked);
        }
        
        Initialize();
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopChanged -= OnShopDataChanged;
            ShopManager.Instance.OnPurchaseResult -= ShowMessage;
        }
    }
    
    private void Update()
    {
        // 消息自动隐藏
        if (messageTimer > 0)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0 && messageText != null)
            {
                messageText.gameObject.SetActive(false);
            }
        }
    }

    public void Initialize()
    {
        // 检测当前场景
        isMainMenuScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu";
        
        // 确保商店不为空
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.EnsureShopNotEmpty();
        }
        
        UpdateShopJokers();
        UpdatePlayerJokers();
        UpdateMoneyDisplay();
        UpdateRefreshCostDisplay();
    }
    
    /// <summary>
    /// 商店数据变化时刷新UI
    /// </summary>
    private void OnShopDataChanged()
    {
        UpdateShopJokers();
        UpdatePlayerJokers();
        UpdateMoneyDisplay();
    }
    
    /// <summary>
    /// 显示提示消息
    /// </summary>
    public void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            messageTimer = MESSAGE_DISPLAY_TIME;
        }
        Debug.Log($"[ShopUI] {message}");
    }
    
    /// <summary>
    /// 更新金币显示
    /// </summary>
    private void UpdateMoneyDisplay()
    {
        if (moneyText == null) return;
        
        if (isMainMenuScene)
        {
            PlayerDataManager dataManager = FindFirstObjectByType<PlayerDataManager>();
            if (dataManager != null)
            {
                PlayerSaveData saveData = dataManager.LoadPlayerData();
                if (saveData != null)
                {
                    if (saveData.debt > 0)
                        moneyText.text = $"金币: {saveData.money}  负债: {saveData.debt}";
                    else
                        moneyText.text = $"金币: {saveData.money}";
                }
            }
        }
        else
        {
            PlayerData localPlayer = GetLocalPlayer();
            if (localPlayer != null)
            {
                if (localPlayer.debt > 0)
                    moneyText.text = $"金币: {localPlayer.money}  负债: {localPlayer.debt}";
                else
                    moneyText.text = $"金币: {localPlayer.money}";
            }
        }
    }
    
    /// <summary>
    /// 更新刷新费用显示
    /// </summary>
    private void UpdateRefreshCostDisplay()
    {
        if (refreshCostText != null && ShopManager.Instance != null)
        {
            refreshCostText.text = $"刷新 ({ShopManager.Instance.RefreshCost}金币)";
        }
    }
    
    /// <summary>
    /// 刷新商店按钮点击
    /// </summary>
    private void OnRefreshShopClicked()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RefreshShop(free: false);
            // UI会通过事件自动更新
        }
    }

    public void UpdateShopJokers()
    {
        if (shopJokersParent == null || jokerItemPrefab == null)
        {
            Debug.LogWarning("ShopUI: shopJokersParent 或 jokerItemPrefab 未分配！");
            return;
        }

        // 清除现有小丑牌
        foreach (GameObject obj in shopJokerObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        shopJokerObjects.Clear();

        // 获取商店小丑牌
        if (ShopManager.Instance != null)
        {
            List<JokerData> shopJokers = ShopManager.Instance.GetShopJokers();
            Debug.Log($"商店小丑牌数量: {shopJokers.Count}");
            
            for (int i = 0; i < shopJokers.Count; i++)
            {
                GameObject jokerObj = Instantiate(jokerItemPrefab, shopJokersParent);
                if (jokerObj == null)
                {
                    Debug.LogError($"无法实例化小丑牌 Prefab: jokerItemPrefab 可能为 null");
                    continue;
                }
                
                // 确保有 RectTransform 并重置，让 GridLayoutGroup 完全控制布局
                RectTransform jokerRect = jokerObj.GetComponent<RectTransform>();
                if (jokerRect == null)
                {
                    // 如果 prefab 根对象没有 RectTransform，添加一个
                    jokerRect = jokerObj.AddComponent<RectTransform>();
                }
                // 重置 RectTransform 属性，避免影响 GridLayoutGroup
                jokerRect.localScale = Vector3.one;
                jokerRect.localPosition = Vector3.zero;
                jokerRect.anchorMin = Vector2.zero;
                jokerRect.anchorMax = Vector2.one;
                jokerRect.offsetMin = Vector2.zero;
                jokerRect.offsetMax = Vector2.zero;
                
                JokerItemUI jokerUI = jokerObj.GetComponent<JokerItemUI>();
                if (jokerUI != null)
                {
                    jokerUI.InitializeShopItem(shopJokers[i], i, OnBuyJoker);
                    Debug.Log($"已创建小丑牌 UI: {shopJokers[i].name}, index={i}");
                }
                else
                {
                    Debug.LogError($"JokerItemUI 组件未找到！请确保 jokerItemPrefab 包含 JokerItemUI 组件");
                }
                
                shopJokerObjects.Add(jokerObj);
            }
            
            Debug.Log($"商店小丑牌 UI 创建完成，共 {shopJokerObjects.Count} 个");
            
            // 强制刷新布局
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(shopJokersParent.GetComponent<RectTransform>());
        }
        else
        {
            Debug.LogError("ShopManager.Instance 为 null！");
        }
    }

    public void UpdatePlayerJokers()
    {
        if (playerJokersParent == null || jokerItemPrefab == null)
            return;

        // 清除现有小丑牌
        foreach (GameObject obj in playerJokerObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        playerJokerObjects.Clear();

        // 获取玩家小丑牌（需要从PlayerDataManager获取）
        PlayerDataManager dataManager = FindFirstObjectByType<PlayerDataManager>();
        if (dataManager != null)
        {
            PlayerSaveData saveData = dataManager.LoadPlayerData();
            if (saveData != null && saveData.jokers != null)
            {
                for (int i = 0; i < saveData.jokers.Count; i++)
                {
                    GameObject jokerObj = Instantiate(jokerItemPrefab, playerJokersParent);
                    
                    // 确保有 RectTransform 并重置，让 GridLayoutGroup 完全控制布局
                    RectTransform jokerRect = jokerObj.GetComponent<RectTransform>();
                    if (jokerRect == null)
                    {
                        jokerRect = jokerObj.AddComponent<RectTransform>();
                    }
                    jokerRect.localScale = Vector3.one;
                    jokerRect.localPosition = Vector3.zero;
                    jokerRect.anchorMin = Vector2.zero;
                    jokerRect.anchorMax = Vector2.one;
                    jokerRect.offsetMin = Vector2.zero;
                    jokerRect.offsetMax = Vector2.zero;
                    
                    JokerItemUI jokerUI = jokerObj.GetComponent<JokerItemUI>();
                    if (jokerUI != null)
                    {
                        jokerUI.InitializePlayerItem(saveData.jokers[i], i, OnSellJoker);
                    }
                    playerJokerObjects.Add(jokerObj);
                }
                
                // 强制刷新布局
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(playerJokersParent.GetComponent<RectTransform>());
            }
        }
        
        // 更新小丑牌数量显示
        UpdateJokerCountDisplay();
    }
    
    /// <summary>
    /// 更新小丑牌数量显示
    /// </summary>
    private void UpdateJokerCountDisplay()
    {
        if (jokerCountText == null) return;
        
        int currentCount = 0;
        int maxCount = ShopManager.Instance != null ? ShopManager.Instance.MaxPlayerJokers : 5;
        
        if (isMainMenuScene)
        {
            PlayerDataManager dataManager = FindFirstObjectByType<PlayerDataManager>();
            if (dataManager != null)
            {
                PlayerSaveData saveData = dataManager.LoadPlayerData();
                if (saveData != null && saveData.jokers != null)
                {
                    currentCount = saveData.jokers.Count;
                }
            }
        }
        else
        {
            PlayerData localPlayer = GetLocalPlayer();
            if (localPlayer != null)
            {
                currentCount = localPlayer.GetJokers().Count;
            }
        }
        
        jokerCountText.text = $"小丑牌: {currentCount}/{maxCount}";
        
        // 如果满了，变成红色警告
        if (currentCount >= maxCount)
        {
            jokerCountText.color = Color.red;
        }
        else
        {
            jokerCountText.color = Color.white;
        }
    }

    private void OnBuyJoker(int index)
    {
        if (isMainMenuScene)
        {
            // 主菜单场景：使用本地存档
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.BuyJokerLocal(index);
                // UI会通过事件自动更新
            }
        }
        else
        {
            // 游戏场景：使用网络数据
            PlayerData localPlayer = GetLocalPlayer();
            if (localPlayer == null)
            {
                ShowMessage("未找到本地玩家！");
                return;
            }

            if (ShopManager.Instance != null)
            {
                bool success = ShopManager.Instance.BuyJoker(index, localPlayer);
                if (success)
                {
                    UpdateShopJokers();
                    UpdatePlayerJokers();
                    UpdateMoneyDisplay();
                }
                else
                {
                    ShowMessage("购买失败！");
                }
            }
        }
    }

    private void OnSellJoker(int index)
    {
        if (isMainMenuScene)
        {
            // 主菜单场景：使用本地存档
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.SellJokerLocal(index);
                // UI会通过事件自动更新
            }
        }
        else
        {
            // 游戏场景：使用网络数据
            PlayerData localPlayer = GetLocalPlayer();
            if (localPlayer == null)
            {
                ShowMessage("未找到本地玩家！");
                return;
            }

            if (ShopManager.Instance != null)
            {
                bool success = ShopManager.Instance.SellJoker(index, localPlayer);
                if (success)
                {
                    UpdatePlayerJokers();
                    UpdateMoneyDisplay();
                }
                else
                {
                    ShowMessage("卖出失败！");
                }
            }
        }
    }

    private PlayerData GetLocalPlayer()
    {
        PlayerData[] allPlayers = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
        foreach (PlayerData player in allPlayers)
        {
            if (player.isLocalPlayer)
                return player;
        }
        return null;
    }
}

