using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// 商店管理器
/// 位置：Assets/Scripts/Game/ShopManager.cs
/// 功能：管理商店中的小丑牌，刷新、购买、售卖等
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("商店设置")]
    [SerializeField] private int shopJokerCount = 10; // 商店显示小丑牌个数
    [SerializeField] private int maxPlayerJokers = 5;  // 玩家最多拥有小丑牌数
    [SerializeField] private int refreshCost = 5;      // 刷新商店费用

    // 商店中的小丑牌列表
    private List<JokerData> shopJokers = new List<JokerData>();
    
    // 商店数据保存路径
    private string shopSavePath;

    // 事件：商店数据变化时触发
    public System.Action OnShopChanged;
    public System.Action<string> OnPurchaseResult; // 购买结果回调（成功/失败消息）

    public static ShopManager Instance { get; private set; }
    
    public int RefreshCost => refreshCost;
    public int MaxPlayerJokers => maxPlayerJokers;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            shopSavePath = Path.Combine(Application.persistentDataPath, "shopdata.json");
            LoadShopData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 刷新商店（随机生成10张小丑牌）
    /// </summary>
    /// <param name="free">是否免费刷新（首次或特殊情况）</param>
    /// <returns>是否刷新成功</returns>
    public bool RefreshShop(bool free = false)
    {
        // 如果不是免费刷新，需要扣费
        if (!free)
        {
            PlayerDataManager dataManager = PlayerDataManager.Instance;
            if (dataManager == null)
            {
                OnPurchaseResult?.Invoke("数据管理器未初始化！");
                return false;
            }
            
            PlayerSaveData saveData = dataManager.LoadPlayerData();
            if (saveData == null)
            {
                OnPurchaseResult?.Invoke("玩家数据未找到！");
                return false;
            }
            
            if (saveData.money < refreshCost)
            {
                OnPurchaseResult?.Invoke($"金币不足！需要 {refreshCost} 金币");
                return false;
            }
            
            // 扣除刷新费用
            saveData.money -= refreshCost;
            dataManager.SavePlayerData(saveData);
        }
        
        // 生成新的商店物品
        shopJokers.Clear();
        List<JokerData.JokerType> allTypes = System.Enum.GetValues(typeof(JokerData.JokerType)).Cast<JokerData.JokerType>().ToList();

        for (int i = 0; i < shopJokerCount; i++)
        {
            // 随机选择一种类型（可重复）
            JokerData.JokerType randomType = allTypes[Random.Range(0, allTypes.Count)];
            shopJokers.Add(new JokerData(randomType));
        }
        
        // 保存商店数据
        SaveShopData();
        OnShopChanged?.Invoke();
        
        if (!free)
        {
            OnPurchaseResult?.Invoke($"商店已刷新！花费 {refreshCost} 金币");
        }
        
        return true;
    }

    /// <summary>
    /// 购买小丑牌（主菜单场景，操作本地存档）
    /// </summary>
    public bool BuyJokerLocal(int index)
    {
        if (index < 0 || index >= shopJokers.Count)
        {
            OnPurchaseResult?.Invoke("无效的商品索引！");
            return false;
        }

        PlayerDataManager dataManager = PlayerDataManager.Instance;
        if (dataManager == null)
        {
            OnPurchaseResult?.Invoke("数据管理器未初始化！");
            return false;
        }

        PlayerSaveData saveData = dataManager.LoadPlayerData();
        if (saveData == null)
        {
            OnPurchaseResult?.Invoke("玩家数据未找到！");
            return false;
        }

        JokerData joker = shopJokers[index];

        // 检查玩家是否已有5张小丑牌
        if (saveData.jokers.Count >= maxPlayerJokers)
        {
            OnPurchaseResult?.Invoke($"小丑牌已满！最多持有 {maxPlayerJokers} 张");
            return false;
        }

        // 检查玩家资金
        if (saveData.money < joker.shopPrice)
        {
            OnPurchaseResult?.Invoke($"金币不足！需要 {joker.shopPrice} 金币，当前 {saveData.money} 金币");
            return false;
        }

        // 扣除资金
        saveData.money -= joker.shopPrice;
        
        // 添加小丑牌到玩家
        saveData.jokers.Add(joker);
        
        // 保存玩家数据
        dataManager.SavePlayerData(saveData);

        // 从商店移除
        shopJokers.RemoveAt(index);
        
        // 保存商店数据
        SaveShopData();
        
        OnShopChanged?.Invoke();
        OnPurchaseResult?.Invoke($"购买成功！{joker.name} (-{joker.shopPrice} 金币)");
        
        return true;
    }
    
    /// <summary>
    /// 购买小丑牌（网络游戏中，操作PlayerData）
    /// </summary>
    public bool BuyJoker(int index, PlayerData playerData)
    {
        if (index < 0 || index >= shopJokers.Count)
            return false;

        if (playerData == null)
            return false;

        JokerData joker = shopJokers[index];
        List<JokerData> playerJokers = playerData.GetJokers();

        // 检查玩家是否已有5张小丑牌
        if (playerJokers.Count >= maxPlayerJokers)
            return false;

        // 检查玩家资金
        // 游戏/网络规则：购买时不应在客户端/服务端中产生新的欠债（借钱仅在主菜单）
        if (!playerData.TrySubtractMoney(joker.shopPrice))
        {
            Debug.LogWarning($"BuyJoker: 玩家 {playerData.playerName} 余额不足，购买被拒绝。");
            return false;
        }

        // 添加小丑牌到玩家
        playerJokers.Add(joker);
        playerData.SetJokers(playerJokers);

        // 从商店移除
        shopJokers.RemoveAt(index);
        
        SaveShopData();
        OnShopChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 售卖小丑牌（主菜单场景，操作本地存档）
    /// </summary>
    public bool SellJokerLocal(int index)
    {
        PlayerDataManager dataManager = PlayerDataManager.Instance;
        if (dataManager == null)
        {
            OnPurchaseResult?.Invoke("数据管理器未初始化！");
            return false;
        }

        PlayerSaveData saveData = dataManager.LoadPlayerData();
        if (saveData == null)
        {
            OnPurchaseResult?.Invoke("玩家数据未找到！");
            return false;
        }

        if (index < 0 || index >= saveData.jokers.Count)
        {
            OnPurchaseResult?.Invoke("无效的小丑牌索引！");
            return false;
        }

        JokerData joker = saveData.jokers[index];
        int sellPrice = joker.sellPrice;

        // 增加资金（优先还债）
        if (saveData.debt > 0)
        {
            int payDebt = Mathf.Min(sellPrice, saveData.debt);
            saveData.debt -= payDebt;
            sellPrice -= payDebt;
            if (payDebt > 0)
            {
                Debug.Log($"自动还债 {payDebt} 金币");
            }
        }
        saveData.money += sellPrice;

        // 移除小丑牌
        string jokerName = joker.name;
        int originalSellPrice = joker.sellPrice;
        saveData.jokers.RemoveAt(index);
        
        // 保存数据
        dataManager.SavePlayerData(saveData);
        
        OnShopChanged?.Invoke();
        OnPurchaseResult?.Invoke($"售出成功！{jokerName} (+{originalSellPrice} 金币)");
        
        return true;
    }
    
    /// <summary>
    /// 售卖小丑牌（网络游戏中，操作PlayerData）
    /// </summary>
    public bool SellJoker(int index, PlayerData playerData)
    {
        if (playerData == null)
            return false;

        List<JokerData> playerJokers = playerData.GetJokers();
        if (index < 0 || index >= playerJokers.Count)
            return false;

        JokerData joker = playerJokers[index];
        int sellPrice = joker.sellPrice;

        // 增加资金
        playerData.AddMoney(sellPrice);

        // 移除小丑牌
        playerJokers.RemoveAt(index);
        playerData.SetJokers(playerJokers);
        
        OnShopChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 获取商店中的小丑牌列表
    /// </summary>
    public List<JokerData> GetShopJokers()
    {
        return new List<JokerData>(shopJokers);
    }

    /// <summary>
    /// 设置商店小丑牌（用于加载保存数据）
    /// </summary>
    public void SetShopJokers(List<JokerData> jokers)
    {
        shopJokers = jokers ?? new List<JokerData>();
    }
    
    /// <summary>
    /// 保存商店数据到本地
    /// </summary>
    private void SaveShopData()
    {
        try
        {
            ShopSaveData saveData = new ShopSaveData { jokers = shopJokers };
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(shopSavePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存商店数据失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 从本地加载商店数据
    /// </summary>
    private void LoadShopData()
    {
        if (!File.Exists(shopSavePath))
        {
            // 首次运行，免费刷新商店
            RefreshShop(free: true);
            return;
        }
        
        try
        {
            string json = File.ReadAllText(shopSavePath);
            ShopSaveData saveData = JsonUtility.FromJson<ShopSaveData>(json);
            if (saveData != null && saveData.jokers != null && saveData.jokers.Count > 0)
            {
                shopJokers = saveData.jokers;
            }
            else
            {
                // 数据无效，免费刷新
                RefreshShop(free: true);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载商店数据失败: {e.Message}");
            RefreshShop(free: true);
        }
    }
    
    /// <summary>
    /// 检查商店是否为空，如果为空则免费刷新
    /// </summary>
    public void EnsureShopNotEmpty()
    {
        if (shopJokers == null || shopJokers.Count == 0)
        {
            RefreshShop(free: true);
        }
    }
    
    /// <summary>
    /// 商店数据保存类
    /// </summary>
    [System.Serializable]
    private class ShopSaveData
    {
        public List<JokerData> jokers = new List<JokerData>();
    }
}

