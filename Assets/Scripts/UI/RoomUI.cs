using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// 房间 UI
/// 位置：Assets/Scripts/UI/RoomUI.cs
/// 功能：显示房间信息、玩家列表、IP 和二维码、开始游戏按钮
/// 
/// 设计原则：
/// 1. 订阅 NetworkManagerCustom 的静态事件
/// 2. 不使用 Update 轮询，完全事件驱动
/// 3. 玩家列表从 NetworkManager 获取
/// </summary>
public class RoomUI : MonoBehaviour
{
    [Header("房间信息")]
    [SerializeField] private TextMeshProUGUI roomIPText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Image qrCodeImage;

    [Header("按钮")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveRoomButton;

    [Header("玩家列表")]
    [SerializeField] private Transform playerListParent;
    [SerializeField] private GameObject playerAvatarPrefab;

    // 缓存的玩家头像对象
    private List<GameObject> playerAvatarObjects = new List<GameObject>();

    // ============ 生命周期 ============

    private void OnEnable()
    {
        // 订阅事件
        NetworkManagerCustom.OnPlayersChanged += RefreshPlayerList;
        NetworkManagerCustom.OnRoomCreated += OnRoomCreated;
        NetworkManagerCustom.OnConnected += OnConnected;
        NetworkManagerCustom.OnDisconnected += OnDisconnected;
    }

    private void OnDisable()
    {
        // 取消订阅
        NetworkManagerCustom.OnPlayersChanged -= RefreshPlayerList;
        NetworkManagerCustom.OnRoomCreated -= OnRoomCreated;
        NetworkManagerCustom.OnConnected -= OnConnected;
        NetworkManagerCustom.OnDisconnected -= OnDisconnected;
    }

    private void Start()
    {
        InitializeUI();
        
        // 初始刷新一次
        RefreshPlayerList();
    }

    // ============ 初始化 ============

    private void InitializeUI()
    {
        // 绑定按钮
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        if (leaveRoomButton != null)
        {
            leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
        }

        // 显示 IP
        UpdateIPDisplay();

        // 更新开始按钮状态
        UpdateStartButtonState();
    }

    // ============ 事件回调 ============

    /// <summary>
    /// 房间创建成功
    /// </summary>
    private void OnRoomCreated(string ip)
    {
        Debug.Log($"[RoomUI] 房间已创建，IP: {ip}");

        if (roomIPText != null)
        {
            roomIPText.text = $"房间 IP: {ip}";
        }

        GenerateQRCode(ip);
        UpdateStartButtonState();
    }

    /// <summary>
    /// 连接成功（客户端）
    /// </summary>
    private void OnConnected()
    {
        Debug.Log("[RoomUI] 已连接到房间");
        UpdateIPDisplay();
        
        // 延迟刷新玩家列表（等待 spawn）
        StartCoroutine(DelayedRefresh());
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    private void OnDisconnected()
    {
        Debug.Log("[RoomUI] 已断开连接");
    }

    private System.Collections.IEnumerator DelayedRefresh()
    {
        yield return new WaitForSeconds(0.3f);
        RefreshPlayerList();
    }

    // ============ UI 更新 ============

    /// <summary>
    /// 刷新玩家列表
    /// </summary>
    public void RefreshPlayerList()
    {
        // 清除现有头像
        foreach (GameObject obj in playerAvatarObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        playerAvatarObjects.Clear();

        // 获取玩家列表
        List<PlayerData> players = GetPlayers();

        Debug.Log($"[RoomUI] 刷新玩家列表，玩家数: {players.Count}");

        // 更新玩家数量显示
        if (playerCountText != null)
        {
            playerCountText.text = $"玩家: {players.Count}/5";
        }

        // 创建玩家头像
        if (playerListParent != null && playerAvatarPrefab != null)
        {
            foreach (PlayerData player in players)
            {
                GameObject avatarObj = Instantiate(playerAvatarPrefab, playerListParent);
                
                PlayerAvatarInRoom avatar = avatarObj.GetComponent<PlayerAvatarInRoom>();
                if (avatar != null)
                {
                    avatar.SetPlayerData(player);
                }
                
                playerAvatarObjects.Add(avatarObj);
            }
        }

        // 更新开始按钮状态
        UpdateStartButtonState();
    }

    /// <summary>
    /// 获取玩家列表
    /// </summary>
    private List<PlayerData> GetPlayers()
    {
        // 优先从 NetworkManager 获取
        if (NetworkManagerCustom.Instance != null)
        {
            return NetworkManagerCustom.Instance.GetAllPlayers();
        }

        // 备用：直接查找场景中的 PlayerData
        PlayerData[] allPlayers = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
        return new List<PlayerData>(allPlayers);
    }

    /// <summary>
    /// 更新 IP 显示
    /// </summary>
    private void UpdateIPDisplay()
    {
        if (roomIPText == null || NetworkManagerCustom.Instance == null)
            return;

        if (NetworkManagerCustom.Instance.IsHost)
        {
            string ip = NetworkManagerCustom.Instance.GetLocalIP();
            roomIPText.text = $"房间 IP: {ip}";
            GenerateQRCode(ip);
        }
        else
        {
            roomIPText.text = $"已加入: {NetworkManagerCustom.Instance.networkAddress}";
        }
    }

    /// <summary>
    /// 更新开始按钮状态
    /// </summary>
    private void UpdateStartButtonState()
    {
        if (startGameButton == null)
            return;

        bool isHost = NetworkManagerCustom.Instance != null && NetworkManagerCustom.Instance.IsHost;
        bool canStart = NetworkManagerCustom.Instance != null && NetworkManagerCustom.Instance.CanStartGame();

        // 只有房主能看到开始按钮
        startGameButton.gameObject.SetActive(isHost);
        startGameButton.interactable = canStart;
    }

    // ============ 按钮回调 ============

    private void OnStartGameClicked()
    {
        Debug.Log("[RoomUI] 点击开始游戏");

        if (NetworkManagerCustom.Instance != null && NetworkServer.active)
        {
            NetworkManagerCustom.Instance.StartGame();
        }
    }

    private void OnLeaveRoomClicked()
    {
        Debug.Log("[RoomUI] 点击离开房间");

        if (NetworkManagerCustom.Instance != null)
        {
            // NetworkManager.LeaveRoom() 会处理场景切换
            NetworkManagerCustom.Instance.LeaveRoom();
        }
        else
        {
            // 如果 NetworkManager 不存在，直接切换场景
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    // ============ 工具方法 ============

    /// <summary>
    /// 生成二维码
    /// </summary>
    private void GenerateQRCode(string text)
    {
        if (qrCodeImage == null)
            return;

        if (QRCodeGenerator.Instance != null)
        {
            Texture2D qrTexture = QRCodeGenerator.Instance.GenerateQRCode(text);
            if (qrTexture != null)
            {
                Sprite qrSprite = Sprite.Create(
                    qrTexture,
                    new Rect(0, 0, qrTexture.width, qrTexture.height),
                    Vector2.one * 0.5f
                );
                qrCodeImage.sprite = qrSprite;
            }
        }
    }

    // ============ 兼容旧代码 ============

    /// <summary>
    /// 兼容旧代码：UpdatePlayerList
    /// </summary>
    public void UpdatePlayerList(List<PlayerData> players)
    {
        // 直接调用 RefreshPlayerList，忽略传入的参数
        RefreshPlayerList();
    }

    /// <summary>
    /// 兼容旧代码：OnRoomCreated (无参数版本)
    /// </summary>
    public void OnRoomCreated()
    {
        UpdateIPDisplay();
    }
}

