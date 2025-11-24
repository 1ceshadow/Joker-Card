using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 游戏管理器
/// 位置：Assets/Scripts/Game/GameManager.cs
/// 功能：管理游戏流程、回合、出牌顺序等核心逻辑
/// </summary>
public class GameManager : NetworkBehaviour
{
    [Header("游戏参数")]
    [SerializeField] private int startingAnte = 5;        // 开始底金
    [SerializeField] private int handCardCount = 8;      // 手牌数量
    [SerializeField] private int maxPlayCardCount = 5;   // 最多出牌数
    [SerializeField] private int maxDiscardCount = 4;    // 最多弃牌数

    [Header("UI引用")]
    [SerializeField] private GameUI gameUI;

    // 游戏状态
    private Deck deck;
    private List<PlayerData> players = new List<PlayerData>();
    private List<PlayerData> playOrder = new List<PlayerData>(); // 出牌顺序
    private int currentPlayerIndex = 0;
    private int pot = 0; // 底池
    private bool gameStarted = false;
    private bool gameEnded = false;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (gameUI == null)
            gameUI = FindFirstObjectByType<GameUI>();
    }

    /// <summary>
    /// 开始游戏（客户端调用）
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdStartGame(NetworkConnectionToClient sender = null)
    {
        StartGame();
    }

    /// <summary>
    /// 开始游戏（服务器端）
    /// </summary>
    [Server]
    private void StartGame()
    {
        if (gameStarted)
            return;

        // 获取所有玩家（通过 NetworkManagerCustom 的 RoomState SyncList 映射 netId -> PlayerData）
        players = NetworkManagerCustom.Instance.GetRoomPlayers();
        if (players.Count < 2)
            return;

        gameStarted = true;
        gameEnded = false;

        // 1. 随机确定出牌顺序
        playOrder = new List<PlayerData>(players);
        for (int i = 0; i < playOrder.Count; i++)
        {
            PlayerData temp = playOrder[i];
            int randomIndex = Random.Range(i, playOrder.Count);
            playOrder[i] = playOrder[randomIndex];
            playOrder[randomIndex] = temp;
        }

        // 2. 所有人投入押金
        pot = 0;
        foreach (PlayerData player in players)
        {
            player.ResetGameState();
            if (player.money >= startingAnte)
            {
                player.SubtractMoney(startingAnte);
                pot += startingAnte;
            }
            else
            {
                // 允许欠债
                int debt = startingAnte - player.money;
                player.SubtractMoney(player.money);
                player.SubtractMoney(debt);
                pot += startingAnte;
            }
        }

        // 3. 初始化牌组并洗牌
        deck = new Deck();
        deck.Shuffle();

        // 4. 发牌
        foreach (PlayerData player in players)
        {
            List<Card> handCards = deck.DealCards(handCardCount);
            player.SetHandCards(handCards);
        }

        // 5. 开始第一个玩家的回合
        currentPlayerIndex = 0;
        RpcOnGameStarted(playOrder.Select(p => p.netId).ToArray());
        StartPlayerTurn();
    }

    /// <summary>
    /// 玩家出牌
    /// </summary>
    [Server]
    public void PlayCards(PlayerData player, List<Card> cardsToPlay)
    {
        if (!gameStarted || gameEnded)
            return;

        if (player != playOrder[currentPlayerIndex])
            return;

        if (player.hasPlayedCard)
            return;

        if (cardsToPlay == null || cardsToPlay.Count == 0 || cardsToPlay.Count > maxPlayCardCount)
            return;

        // 检查手牌是否包含这些牌（从JSON反序列化）
        List<Card> handCards = new List<Card>();
        if (!string.IsNullOrEmpty(player.handCardsJson))
        {
            CardListWrapper wrapper = JsonUtility.FromJson<CardListWrapper>(player.handCardsJson);
            handCards = wrapper.cards ?? new List<Card>();
        }
        if (!cardsToPlay.All(c => handCards.Any(h => h.suit == c.suit && h.rank == c.rank)))
            return;

        // 移除手牌中的这些牌
        foreach (Card card in cardsToPlay)
        {
            handCards.RemoveAll(c => c.suit == card.suit && c.rank == card.rank);
        }

        // 计算分数（从JSON反序列化小丑牌）
        List<JokerData> jokers = new List<JokerData>();
        if (!string.IsNullOrEmpty(player.jokersJson))
        {
            JokerListWrapper wrapper = JsonUtility.FromJson<JokerListWrapper>(player.jokersJson);
            jokers = wrapper.jokers ?? new List<JokerData>();
        }
        int score = Scoring.CalculateScore(cardsToPlay, jokers);
        player.currentScore = score;

        // 设置出的牌
        player.SetPlayedCards(cardsToPlay);

        // 剩余出牌次数 -1
        player.remainingPlayCount--;

        // 所有牌（包括出的和没出的）随机插入牌组
        List<Card> allCards = new List<Card>(handCards);
        allCards.AddRange(cardsToPlay);
        deck.ReturnCards(allCards);

        // 清空手牌
        player.SetHandCards(new List<Card>());
        player.hasPlayedCard = true;

        // 通知所有客户端
        RpcOnPlayerPlayedCards(player.netId, cardsToPlay.ToArray(), score);

        // 结束回合
        EndPlayerTurn();
    }

    /// <summary>
    /// 玩家押注
    /// </summary>
    [Server]
    public void Bet(PlayerData player, int amount)
    {
        if (!gameStarted || gameEnded)
            return;

        if (player != playOrder[currentPlayerIndex])
            return;

        // 检查场上玩家数
        int activePlayers = playOrder.Count(p => !p.hasPlayedCard);
        if (activePlayers < 2)
            return;

        // 检查金额
        if (amount <= 0)
            return;

        // 扣除资金（允许欠债）
        int totalCost = amount;
        int availableMoney = player.money;
        if (availableMoney < totalCost)
        {
            int debtAmount = totalCost - availableMoney;
            player.SubtractMoney(availableMoney);
            player.SubtractMoney(debtAmount);
        }
        else
        {
            player.SubtractMoney(amount);
        }

        // 增加底池
        pot += amount;

        // 通知所有客户端
        RpcOnPlayerBet(player.netId, amount, pot);

        // 结束回合
        EndPlayerTurn();
    }

    /// <summary>
    /// 玩家弃牌
    /// </summary>
    [Server]
    public void DiscardCards(PlayerData player, List<Card> cardsToDiscard)
    {
        if (!gameStarted || gameEnded)
            return;

        if (player != playOrder[currentPlayerIndex])
            return;

        if (player.remainingDiscardCount <= 0)
            return;

        if (cardsToDiscard == null || cardsToDiscard.Count == 0)
            return;

        // 检查单次弃牌数量限制
        if (cardsToDiscard.Count > maxDiscardCount)
            return;

        // 检查手牌（从JSON反序列化）
        List<Card> handCards = new List<Card>();
        if (!string.IsNullOrEmpty(player.handCardsJson))
        {
            CardListWrapper wrapper = JsonUtility.FromJson<CardListWrapper>(player.handCardsJson);
            handCards = wrapper.cards ?? new List<Card>();
        }
        if (!cardsToDiscard.All(c => handCards.Any(h => h.suit == c.suit && h.rank == c.rank)))
            return;

        // 移除手牌
        foreach (Card card in cardsToDiscard)
        {
            handCards.RemoveAll(c => c.suit == card.suit && c.rank == card.rank);
        }

        // 弃掉的牌放回牌组
        deck.ReturnCards(cardsToDiscard);

        // 立即摸等数量的牌
        List<Card> newCards = deck.DealCards(cardsToDiscard.Count);
        handCards.AddRange(newCards);

        // 更新手牌
        player.SetHandCards(handCards);

        // 剩余弃牌次数 -1
        player.remainingDiscardCount--;

        // 通知客户端
        RpcOnPlayerDiscarded(player.netId, cardsToDiscard.Count);

        // 弃牌后不结束回合，玩家可以继续操作
    }

    /// <summary>
    /// 开始玩家回合
    /// </summary>
    [Server]
    private void StartPlayerTurn()
    {
        if (currentPlayerIndex >= playOrder.Count)
        {
            CheckGameEnd();
            return;
        }

        PlayerData currentPlayer = playOrder[currentPlayerIndex];

        // 如果该玩家剩余出牌次数为0，跳过
        if (currentPlayer.remainingPlayCount == 0)
        {
            EndPlayerTurn();
            return;
        }

        // 如果只剩该玩家，强制出牌
        int activePlayers = playOrder.Count(p => !p.hasPlayedCard);
        bool mustPlay = activePlayers == 1;

        RpcOnPlayerTurnStarted(currentPlayer.netId, mustPlay);
    }

    /// <summary>
    /// 结束玩家回合
    /// </summary>
    [Server]
    private void EndPlayerTurn()
    {
        currentPlayerIndex++;
        StartPlayerTurn();
    }

    /// <summary>
    /// 检查游戏是否结束
    /// </summary>
    [Server]
    private void CheckGameEnd()
    {
        // 检查是否所有玩家都出完牌
        bool allPlayed = playOrder.All(p => p.hasPlayedCard || p.remainingPlayCount == 0);

        if (allPlayed)
        {
            EndGame();
        }
    }

    /// <summary>
    /// 结束游戏并结算
    /// </summary>
    [Server]
    private void EndGame()
    {
        gameEnded = true;

        // 找出最高分
        int maxScore = playOrder.Max(p => p.currentScore);
        List<PlayerData> winners = playOrder.Where(p => p.currentScore == maxScore).ToList();

        // 分配底池
        if (winners.Count > 0)
        {
            int sharePerPlayer = pot / winners.Count;
            int remainder = pot % winners.Count; // 余数归系统

            foreach (PlayerData winner in winners)
            {
                winner.AddMoney(sharePerPlayer);
            }
        }

        // 所有玩家还债
        foreach (PlayerData player in players)
        {
            if (player.money > 0 && player.debt > 0)
            {
                int payAmount = Mathf.Min(player.money, player.debt);
                player.PayDebt(payAmount);
                player.SubtractMoney(payAmount);
            }
        }

        // 通知客户端游戏结束
        uint[] winnerIds = winners.Select(w => w.netId).ToArray();
        int[] scores = playOrder.Select(p => p.currentScore).ToArray();
        RpcOnGameEnded(winnerIds, scores, pot);

        // 刷新商店
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RefreshShop();
        }
    }

    // RPC 方法
    [ClientRpc]
    private void RpcOnGameStarted(uint[] playerNetIds)
    {
        if (gameUI == null)
            gameUI = FindFirstObjectByType<GameUI>();
        if (gameUI != null)
            gameUI.OnGameStarted();
    }

    [ClientRpc]
    private void RpcOnPlayerTurnStarted(uint playerNetId, bool mustPlay)
    {
        if (gameUI == null)
            gameUI = FindFirstObjectByType<GameUI>();
        if (gameUI != null)
            gameUI.OnPlayerTurnStarted(playerNetId, mustPlay);
    }

    [ClientRpc]
    private void RpcOnPlayerPlayedCards(uint playerNetId, Card[] cards, int score)
    {
        if (gameUI == null)
            gameUI = FindFirstObjectByType<GameUI>();
        if (gameUI != null)
            gameUI.OnPlayerPlayedCards(playerNetId, cards.ToList(), score);
    }

    [ClientRpc]
    private void RpcOnPlayerBet(uint playerNetId, int amount, int newPot)
    {
        pot = newPot;
        if (gameUI == null)
            gameUI = FindFirstObjectByType<GameUI>();
        if (gameUI != null)
            gameUI.OnPlayerBet(playerNetId, amount, newPot);
    }

    [ClientRpc]
    private void RpcOnPlayerDiscarded(uint playerNetId, int discardCount)
    {
        if (gameUI == null)
            gameUI = FindFirstObjectByType<GameUI>();
        if (gameUI != null)
            gameUI.OnPlayerDiscarded(playerNetId, discardCount);
    }

    [ClientRpc]
    private void RpcOnGameEnded(uint[] winnerNetIds, int[] scores, int finalPot)
    {
        if (gameUI == null)
            gameUI = FindFirstObjectByType<GameUI>();
        if (gameUI != null)
            gameUI.OnGameEnded(winnerNetIds, scores, finalPot);
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

    // 命令方法
    [Command(requiresAuthority = false)]
    public void CmdPlayCards(uint playerNetId, Card[] cards, NetworkConnectionToClient sender = null)
    {
        if (sender == null) return;
        PlayerData playerData = sender.identity.GetComponent<PlayerData>();
        if (playerData != null && playerData.netId == playerNetId)
        {
            PlayCards(playerData, cards.ToList());
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdBet(uint playerNetId, int amount, NetworkConnectionToClient sender = null)
    {
        if (sender == null) return;
        PlayerData playerData = sender.identity.GetComponent<PlayerData>();
        if (playerData != null && playerData.netId == playerNetId)
        {
            Bet(playerData, amount);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdDiscardCards(uint playerNetId, Card[] cards, NetworkConnectionToClient sender = null)
    {
        if (sender == null) return;
        PlayerData playerData = sender.identity.GetComponent<PlayerData>();
        if (playerData != null && playerData.netId == playerNetId)
        {
            DiscardCards(playerData, cards.ToList());
        }
    }
}
