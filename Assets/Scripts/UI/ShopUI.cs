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

    private List<GameObject> shopJokerObjects = new List<GameObject>();
    private List<GameObject> playerJokerObjects = new List<GameObject>();

    public void Initialize()
    {
        UpdateShopJokers();
        UpdatePlayerJokers();
    }

    public void UpdateShopJokers()
    {
        if (shopJokersParent == null || jokerItemPrefab == null)
            return;

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
            for (int i = 0; i < shopJokers.Count; i++)
            {
                GameObject jokerObj = Instantiate(jokerItemPrefab, shopJokersParent);
                JokerItemUI jokerUI = jokerObj.GetComponent<JokerItemUI>();
                if (jokerUI != null)
                {
                    jokerUI.InitializeShopItem(shopJokers[i], i, OnBuyJoker);
                }
                shopJokerObjects.Add(jokerObj);
            }
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
                    JokerItemUI jokerUI = jokerObj.GetComponent<JokerItemUI>();
                    if (jokerUI != null)
                    {
                        jokerUI.InitializePlayerItem(saveData.jokers[i], i, OnSellJoker);
                    }
                    playerJokerObjects.Add(jokerObj);
                }
            }
        }
    }

    private void OnBuyJoker(int index)
    {
        // 获取本地玩家
        PlayerData localPlayer = GetLocalPlayer();
        if (localPlayer == null)
            return;

        // 购买小丑牌
        if (ShopManager.Instance != null)
        {
            bool success = ShopManager.Instance.BuyJoker(index, localPlayer);
            if (success)
            {
                UpdateShopJokers();
                UpdatePlayerJokers();
            }
        }
    }

    private void OnSellJoker(int index)
    {
        // 获取本地玩家
        PlayerData localPlayer = GetLocalPlayer();
        if (localPlayer == null)
            return;

        // 售卖小丑牌
        if (ShopManager.Instance != null)
        {
            bool success = ShopManager.Instance.SellJoker(index, localPlayer);
            if (success)
            {
                UpdatePlayerJokers();
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

