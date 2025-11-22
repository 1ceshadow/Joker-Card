using UnityEngine;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// 玩家数据（网络同步）
/// 位置：Assets/Scripts/Network/PlayerData.cs
/// 功能：同步玩家信息、手牌、资金、小丑牌等数据
/// </summary>
public class PlayerData : NetworkBehaviour
{
    [Header("玩家信息")]
    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string playerName = "Player";

    [SyncVar(hook = nameof(OnAvatarIdChanged))]
    public int avatarId = 0;

    [SyncVar(hook = nameof(OnMoneyChanged))]
    public int money = 20; // 起始资金

    [SyncVar(hook = nameof(OnDebtChanged))]
    public int debt = 0; // 欠债

    [Header("游戏状态")]
    [SyncVar]
    public bool hasPlayedCard = false; // 是否已出牌

    [SyncVar]
    public int remainingPlayCount = 1; // 剩余出牌次数

    [SyncVar]
    public int remainingDiscardCount = 3; // 剩余弃牌次数

    [SyncVar]
    public int currentScore = 0; // 当前得分

    [Header("手牌")]
    [SyncVar(hook = nameof(OnHandCardsChanged))]
    public string handCardsJson = ""; // 手牌JSON

    [Header("小丑牌")]
    [SyncVar(hook = nameof(OnJokersChanged))]
    public string jokersJson = ""; // 小丑牌JSON

    [Header("出的牌")]
    [SyncVar(hook = nameof(OnPlayedCardsChanged))]
    public string playedCardsJson = ""; // 出的牌JSON

    // 本地缓存
    private List<Card> handCards = new List<Card>();
    private List<JokerData> jokers = new List<JokerData>();
    private List<Card> playedCards = new List<Card>();

    // 事件
    public System.Action OnDataChanged;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isLocalPlayer)
        {
            LoadPlayerData();
        }
    }

    [Server]
    public void SetPlayerInfo(string name, int avatarId, int money, int debt, List<JokerData> jokers)
    {
        this.playerName = name;
        this.avatarId = avatarId;
        this.money = money;
        this.debt = debt;
        this.jokersJson = JsonUtility.ToJson(new JokerListWrapper { jokers = jokers });
        this.jokers = jokers;
    }

    [Server]
    public void SetHandCards(List<Card> cards)
    {
        handCards = cards;
        handCardsJson = JsonUtility.ToJson(new CardListWrapper { cards = cards });
    }

    [Server]
    public void SetPlayedCards(List<Card> cards)
    {
        playedCards = cards;
        playedCardsJson = JsonUtility.ToJson(new CardListWrapper { cards = cards });
    }

    [Server]
    public void SetJokers(List<JokerData> jokersList)
    {
        jokers = jokersList;
        jokersJson = JsonUtility.ToJson(new JokerListWrapper { jokers = jokersList });
    }

    [Server]
    public void AddMoney(int amount)
    {
        money += amount;
    }

    [Server]
    public void SubtractMoney(int amount)
    {
        money -= amount;
        if (money < 0)
        {
            debt += Mathf.Abs(money);
            money = 0;
        }
    }

    [Server]
    public void PayDebt(int amount)
    {
        if (debt > 0)
        {
            int payAmount = Mathf.Min(amount, debt);
            debt -= payAmount;
        }
    }

    [Server]
    public void ResetGameState()
    {
        hasPlayedCard = false;
        remainingPlayCount = 1;
        remainingDiscardCount = 3;
        currentScore = 0;
        handCards.Clear();
        playedCards.Clear();
        handCardsJson = "";
        playedCardsJson = "";
    }

    // 获取本地手牌（仅本地玩家）
    public List<Card> GetHandCards()
    {
        if (isLocalPlayer)
            return handCards;
        return new List<Card>();
    }

    // 获取小丑牌
    public List<JokerData> GetJokers()
    {
        return jokers;
    }

    // 获取出的牌
    public List<Card> GetPlayedCards()
    {
        return playedCards;
    }

    // Hook 回调
    private void OnPlayerNameChanged(string oldValue, string newValue)
    {
        OnDataChanged?.Invoke();
    }

    private void OnAvatarIdChanged(int oldValue, int newValue)
    {
        OnDataChanged?.Invoke();
    }

    private void OnMoneyChanged(int oldValue, int newValue)
    {
        OnDataChanged?.Invoke();
    }

    private void OnDebtChanged(int oldValue, int newValue)
    {
        OnDataChanged?.Invoke();
    }

    private void OnHandCardsChanged(string oldValue, string newValue)
    {
        if (!string.IsNullOrEmpty(newValue))
        {
            CardListWrapper wrapper = JsonUtility.FromJson<CardListWrapper>(newValue);
            handCards = wrapper.cards ?? new List<Card>();
        }
        OnDataChanged?.Invoke();
    }

    private void OnJokersChanged(string oldValue, string newValue)
    {
        if (!string.IsNullOrEmpty(newValue))
        {
            JokerListWrapper wrapper = JsonUtility.FromJson<JokerListWrapper>(newValue);
            jokers = wrapper.jokers ?? new List<JokerData>();
        }
        OnDataChanged?.Invoke();
    }

    private void OnPlayedCardsChanged(string oldValue, string newValue)
    {
        if (!string.IsNullOrEmpty(newValue))
        {
            CardListWrapper wrapper = JsonUtility.FromJson<CardListWrapper>(newValue);
            playedCards = wrapper.cards ?? new List<Card>();
        }
        OnDataChanged?.Invoke();
    }

    private void LoadPlayerData()
    {
        PlayerSaveData saveData = PlayerDataManager.Instance.LoadPlayerData();
        if (saveData != null)
        {
            CmdSetPlayerInfo(saveData.playerName, saveData.avatarId, saveData.money, saveData.debt, saveData.jokers);
        }
    }

    [Command]
    private void CmdSetPlayerInfo(string name, int avatarId, int money, int debt, List<JokerData> jokers)
    {
        SetPlayerInfo(name, avatarId, money, debt, jokers);
    }

    // JSON 包装类
    [System.Serializable]
    private class CardListWrapper
    {
        public List<Card> cards;
    }

    [System.Serializable]
    private class JokerListWrapper
    {
        public List<JokerData> jokers;
    }
}

