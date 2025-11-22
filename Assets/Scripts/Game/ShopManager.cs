using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    // 商店中的小丑牌列表
    private List<JokerData> shopJokers = new List<JokerData>();

    public static ShopManager Instance { get; private set; }

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
    /// 刷新商店（随机生成10张小丑牌）
    /// </summary>
    public void RefreshShop()
    {
        shopJokers.Clear();
        List<JokerData.JokerType> allTypes = System.Enum.GetValues(typeof(JokerData.JokerType)).Cast<JokerData.JokerType>().ToList();

        for (int i = 0; i < shopJokerCount; i++)
        {
            // 随机选择一种类型（可重复）
            JokerData.JokerType randomType = allTypes[Random.Range(0, allTypes.Count)];
            shopJokers.Add(new JokerData(randomType));
        }
    }

    /// <summary>
    /// 购买小丑牌
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
        if (playerData.money < joker.shopPrice)
        {
            // 允许欠债
            int totalCost = joker.shopPrice;
            int availableMoney = playerData.money;
            int debtAmount = totalCost - availableMoney;

            playerData.SubtractMoney(availableMoney);
            playerData.SubtractMoney(debtAmount); // 这会增加债务
        }
        else
        {
            playerData.SubtractMoney(joker.shopPrice);
        }

        // 添加小丑牌到玩家
        playerJokers.Add(joker);
        playerData.SetJokers(playerJokers);

        // 从商店移除
        shopJokers.RemoveAt(index);

        return true;
    }

    /// <summary>
    /// 售卖小丑牌
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
}

