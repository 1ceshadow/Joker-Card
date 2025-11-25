using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 游戏内UI
/// 位置：Assets/Scripts/UI/GameUI.cs
/// 功能：显示游戏界面、手牌、小丑牌、玩家头像、出的牌等
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("玩家信息")]
    [SerializeField] private Image playerAvatarImage;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerMoneyText;

    [Header("手牌区域")]
    [SerializeField] private Transform handCardsParent;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Button playCardButton;
    [SerializeField] private Button betButton;
    [SerializeField] private Button discardButton;

    [Header("小丑牌区域")]
    [SerializeField] private Transform jokersParent;
    [SerializeField] private GameObject jokerItemPrefab;

    [Header("其他玩家区域")]
    [SerializeField] private Transform otherPlayersParent;
    [SerializeField] private GameObject playerAvatarPrefab;

    [Header("出的牌显示")]
    [SerializeField] private Transform centerCardsParent;
    [SerializeField] private TextMeshProUGUI currentScoreText;

    [Header("底池显示")]
    [SerializeField] private TextMeshProUGUI potText;

    [Header("结算面板")]
    [SerializeField] private ResultUI resultUI;

    private PlayerData localPlayer;
    private List<Card> selectedCards = new List<Card>();
    private List<GameObject> handCardObjects = new List<GameObject>();
    private Dictionary<uint, GameObject> playerAvatarObjects = new Dictionary<uint, GameObject>();
    private bool isMyTurn = false;
    private bool mustPlay = false;

    private void Start()
    {
        // 初始化按钮
        if (playCardButton != null)
            playCardButton.onClick.AddListener(OnPlayCardClicked);
        if (betButton != null)
            betButton.onClick.AddListener(OnBetClicked);
        if (discardButton != null)
            discardButton.onClick.AddListener(OnDiscardClicked);

        // 查找本地玩家
        FindLocalPlayer();
    }

    private void FindLocalPlayer()
    {
        PlayerData[] allPlayers = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
        foreach (PlayerData player in allPlayers)
        {
            if (player.isLocalPlayer)
            {
                localPlayer = player;
                UpdatePlayerInfo();
                UpdateHandCards();
                UpdateJokers();
                break;
            }
        }
    }

    private void Update()
    {
        if (localPlayer != null)
        {
            UpdatePlayerInfo();
            UpdateButtonStates();
        }
    }

    private void UpdatePlayerInfo()
    {
        if (localPlayer == null)
            return;

        if (playerNameText != null)
            playerNameText.text = localPlayer.playerName;

        if (playerMoneyText != null)
        {
            if (localPlayer.debt > 0)
                playerMoneyText.text = $"负债: {localPlayer.debt}";
            else
                playerMoneyText.text = $"资金: {localPlayer.money}";
        }

        // 更新头像
        if (playerAvatarImage != null)
        {
            Sprite avatarSprite = AvatarSpriteLoader.GetAvatarSprite(localPlayer.avatarId);
            if (avatarSprite != null)
                playerAvatarImage.sprite = avatarSprite;
        }
    }

    private void UpdateHandCards()
    {
        if (localPlayer == null || handCardsParent == null || cardPrefab == null)
            return;

        // 清除现有手牌
        foreach (GameObject obj in handCardObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        handCardObjects.Clear();

        // 创建手牌UI
        List<Card> handCards = localPlayer.GetHandCards();
        foreach (Card card in handCards)
        {
            GameObject cardObj = Instantiate(cardPrefab, handCardsParent);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.Initialize(card, OnCardClicked);
            }
            handCardObjects.Add(cardObj);
        }
    }

    private void UpdateJokers()
    {
        if (localPlayer == null || jokersParent == null || jokerItemPrefab == null)
            return;

        // 清除现有小丑牌
        foreach (Transform child in jokersParent)
        {
            Destroy(child.gameObject);
        }

        // 创建小丑牌UI
        List<JokerData> jokers = localPlayer.GetJokers();
        for (int i = 0; i < jokers.Count; i++)
        {
            GameObject jokerObj = Instantiate(jokerItemPrefab, jokersParent);
            JokerItemUI jokerUI = jokerObj.GetComponent<JokerItemUI>();
            if (jokerUI != null)
            {
                // 游戏内显示玩家拥有的小丑牌，不需要售卖功能
                jokerUI.InitializePlayerItem(jokers[i], i, null);
            }
        }
    }

    private void UpdateButtonStates()
    {
        if (localPlayer == null)
            return;

        bool hasSelectedCards = selectedCards.Count > 0;
        bool canPlay = hasSelectedCards && selectedCards.Count <= 5 && !localPlayer.hasPlayedCard && localPlayer.remainingPlayCount > 0;
        bool canDiscard = hasSelectedCards && localPlayer.remainingDiscardCount > 0;
        bool canBet = !localPlayer.hasPlayedCard && isMyTurn;

        // 如果只剩自己，必须出牌
        if (mustPlay)
        {
            canBet = false;
            canPlay = hasSelectedCards && selectedCards.Count <= 5;
        }

        if (playCardButton != null)
            playCardButton.interactable = canPlay && isMyTurn;
        if (discardButton != null)
            discardButton.interactable = canDiscard && isMyTurn;
        if (betButton != null)
            betButton.interactable = canBet && isMyTurn;
    }

    private void OnCardClicked(Card card, bool isSelected)
    {
        if (isSelected)
        {
            if (selectedCards.Count < 5)
                selectedCards.Add(card);
        }
        else
        {
            selectedCards.RemoveAll(c => c.suit == card.suit && c.rank == card.rank);
        }
        UpdateButtonStates();
    }

    private void OnJokerClicked(int index)
    {
        // 显示售卖按钮（需要实现）
    }

    private void OnPlayCardClicked()
    {
        if (localPlayer == null || GameManager.Instance == null)
            return;

        if (selectedCards.Count == 0 || selectedCards.Count > 5)
            return;

        // 发送出牌命令
        GameManager.Instance.CmdPlayCards(localPlayer.netId, selectedCards.ToArray());
        selectedCards.Clear();
        UpdateHandCards();
    }

    private void OnBetClicked()
    {
        // 显示押注窗口（需要实现）
        if (BettingUI.Instance != null)
        {
            BettingUI.Instance.ShowBettingWindow(localPlayer, OnBetConfirmed);
        }
    }

    private void OnBetConfirmed(int amount)
    {
        if (localPlayer == null || GameManager.Instance == null)
            return;

        GameManager.Instance.CmdBet(localPlayer.netId, amount);
    }

    private void OnDiscardClicked()
    {
        if (localPlayer == null || GameManager.Instance == null)
            return;

        if (selectedCards.Count == 0)
            return;

        // 发送弃牌命令
        GameManager.Instance.CmdDiscardCards(localPlayer.netId, selectedCards.ToArray());
        selectedCards.Clear();
        UpdateHandCards();
    }

    // 游戏事件回调
    public void OnGameStarted()
    {
        // 初始化其他玩家头像
        UpdateOtherPlayers();
    }

    public void OnPlayerTurnStarted(uint playerNetId, bool mustPlayCard)
    {
        isMyTurn = localPlayer != null && localPlayer.netId == playerNetId;
        mustPlay = mustPlayCard;
        UpdateButtonStates();
    }

    public void OnPlayerPlayedCards(uint playerNetId, List<Card> cards, int score)
    {
        // 在中心显示出的牌
        ShowPlayedCards(playerNetId, cards, score);
    }

    public void OnPlayerBet(uint playerNetId, int amount, int newPot)
    {
        if (potText != null)
            potText.text = $"底池: {newPot}";
    }

    public void OnPlayerDiscarded(uint playerNetId, int discardCount)
    {
        // 更新手牌显示
        if (localPlayer != null && localPlayer.netId == playerNetId)
        {
            UpdateHandCards();
        }
    }

    public void OnGameEnded(uint[] winnerNetIds, int[] scores, int finalPot)
    {
        // 显示结算面板
        if (resultUI != null)
        {
            resultUI.ShowResults(winnerNetIds, scores, finalPot);
        }
    }

    public void ShowStartFailed(string reason)
    {
        // 简单提示：目前没有全局弹窗系统，先使用 Debug.Log 并在结果 UI 上短暂显示（如果可用）
        Debug.LogWarning("游戏开始失败: " + reason);
        if (resultUI != null)
        {
            resultUI.ShowMessage(reason);
        }
    }

    private void ShowPlayedCards(uint playerNetId, List<Card> cards, int score)
    {
        // 清除中心区域的牌
        foreach (Transform child in centerCardsParent)
        {
            Destroy(child.gameObject);
        }

        // 显示出的牌
        if (cardPrefab != null)
        {
            foreach (Card card in cards)
            {
                GameObject cardObj = Instantiate(cardPrefab, centerCardsParent);
                CardUI cardUI = cardObj.GetComponent<CardUI>();
                if (cardUI != null)
                {
                    cardUI.Initialize(card, null);
                }
            }
        }

        // 显示分数
        if (currentScoreText != null)
            currentScoreText.text = $"分数: {score}";
    }

    private void UpdateOtherPlayers()
    {
        // 获取所有玩家并显示头像（逆时针排列）
        PlayerData[] allPlayers = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
        // 需要根据出牌顺序排列
        // 这里简化处理，实际需要从GameManager获取顺序
    }
}

