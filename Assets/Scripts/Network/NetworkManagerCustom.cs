using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// 自定义网络管理器
/// 位置：Assets/Scripts/Network/NetworkManagerCustom.cs
/// 功能：管理网络连接、房间创建、玩家加入等
/// 
/// 设计原则：
/// 1. NetworkManager 继承自 NetworkManager，不是 NetworkBehaviour，不能用 [ClientRpc]
/// 2. 使用静态事件通知 UI 更新
/// 3. 玩家列表直接从 NetworkServer.spawned 或 NetworkClient.spawned 获取
/// </summary>
public class NetworkManagerCustom : NetworkManager
{
    [Header("房间设置")]
    [SerializeField] private int maxPlayers = 5;
    [SerializeField] private int minPlayersToStart = 2;

    [Header("场景设置")]
    [SerializeField] private string roomScene = "CreateRoom";
    [SerializeField] private string gameScene = "GameScene";
    [SerializeField] private string mainMenuScene = "MainMenu";

    /// <summary>
    /// 是否为房主（Host）
    /// </summary>
    public bool IsHost { get; private set; } = false;

    /// <summary>
    /// 单例实例
    /// </summary>
    public static NetworkManagerCustom Instance { get; private set; }

    // ============ 静态事件（UI 订阅这些事件来更新） ============
    
    /// <summary>
    /// 玩家列表变化事件（玩家加入或离开时触发）
    /// </summary>
    public static event System.Action OnPlayersChanged;

    /// <summary>
    /// 连接成功事件
    /// </summary>
    public static event System.Action OnConnected;

    /// <summary>
    /// 断开连接事件
    /// </summary>
    public static event System.Action OnDisconnected;

    /// <summary>
    /// 连接失败事件（参数为错误信息）
    /// </summary>
    public static event System.Action<string> OnConnectionFailed;

    /// <summary>
    /// 房间创建成功事件（参数为房间 IP）
    /// </summary>
    public static event System.Action<string> OnRoomCreated;

    /// <summary>
    /// 是否正在尝试连接
    /// </summary>
    public bool IsConnecting { get; private set; } = false;

    // ============ 生命周期 ============

    public override void Awake()
    {
        // 单例检查
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[NetworkManager] 已存在实例，销毁重复的");
            Destroy(gameObject);
            return;
        }

        // 确保有 Transport
        if (transport == null)
        {
            transport = GetComponent<Transport>();
            if (transport == null)
            {
                transport = gameObject.AddComponent<TelepathyTransport>();
                Debug.Log("[NetworkManager] 已自动添加 TelepathyTransport");
            }
        }

        base.Awake();

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[NetworkManager] 初始化完成");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ============ 服务器端回调 ============

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[Server] 服务器已启动");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("[Server] 服务器已停止");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // 创建玩家对象
        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);

        PlayerData playerData = player.GetComponent<PlayerData>();
        if (playerData != null)
        {
            Debug.Log($"[Server] 玩家加入，connId={conn.connectionId}, netId={playerData.netId}");
        }

        // 触发事件通知 UI 更新
        OnPlayersChanged?.Invoke();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"[Server] 玩家断开，connId={conn.connectionId}");
        
        base.OnServerDisconnect(conn);

        // 触发事件通知 UI 更新
        OnPlayersChanged?.Invoke();
    }

    // ============ 客户端回调 ============

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[Client] 客户端已启动");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("[Client] 客户端已停止");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[Client] 已连接到服务器");
        
        IsConnecting = false;
        OnConnected?.Invoke();
    }

    /// <summary>
    /// 客户端连接失败时调用
    /// </summary>
    public override void OnClientError(TransportError error, string reason)
    {
        base.OnClientError(error, reason);
        Debug.LogError($"[Client] 连接错误: {error}, {reason}");
        
        IsConnecting = false;
        OnConnectionFailed?.Invoke($"连接失败: {reason}");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("[Client] 与服务器断开连接");

        // 如果是在连接过程中断开，说明连接失败
        if (IsConnecting)
        {
            IsConnecting = false;
            OnConnectionFailed?.Invoke("无法连接到服务器，请检查 IP 地址是否正确");
            return; // 不自动切换场景，让 UI 处理
        }

        OnDisconnected?.Invoke();

        // 不在这里切换场景，让调用者处理
    }

    /// <summary>
    /// 客户端场景加载完成时调用
    /// </summary>
    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
        Debug.Log($"[Client] 场景已切换: {SceneManager.GetActiveScene().name}");
        
        // 延迟触发玩家列表更新（等待 spawn 完成）
        StartCoroutine(DelayedPlayersChanged());
    }

    private System.Collections.IEnumerator DelayedPlayersChanged()
    {
        yield return new WaitForSeconds(0.2f);
        OnPlayersChanged?.Invoke();
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 创建房间（作为 Host）
    /// </summary>
    public void CreateRoom()
    {
        Debug.Log("[NetworkManager] 创建房间");

        IsHost = true;
        
        // 设置在线场景为房间场景
        onlineScene = roomScene;
        
        // 启动 Host
        StartHost();

        // 触发房间创建事件
        string ip = GetLocalIP();
        OnRoomCreated?.Invoke(ip);
        
        Debug.Log($"[NetworkManager] Host 已启动，IP: {ip}");
    }

    /// <summary>
    /// 加入房间（作为 Client）
    /// </summary>
    public void JoinRoom(string ip)
    {
        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogError("[NetworkManager] IP 地址不能为空");
            OnConnectionFailed?.Invoke("IP 地址不能为空");
            return;
        }

        Debug.Log($"[NetworkManager] 加入房间，IP: {ip}");

        IsHost = false;
        IsConnecting = true;
        networkAddress = ip;
        
        // 设置在线场景
        onlineScene = roomScene;
        
        // 启动客户端
        StartClient();
        
        // 启动超时检测
        StartCoroutine(ConnectionTimeout(10f));
    }

    /// <summary>
    /// 连接超时检测
    /// </summary>
    private System.Collections.IEnumerator ConnectionTimeout(float timeout)
    {
        float elapsed = 0f;
        while (elapsed < timeout && IsConnecting)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (IsConnecting)
        {
            Debug.LogWarning("[NetworkManager] 连接超时");
            IsConnecting = false;
            StopClient();
            OnConnectionFailed?.Invoke("连接超时，请检查 IP 地址和网络");
        }
    }

    /// <summary>
    /// 离开房间
    /// </summary>
    public void LeaveRoom()
    {
        Debug.Log("[NetworkManager] 离开房间");

        bool wasHost = IsHost;
        IsHost = false;
        IsConnecting = false;

        if (NetworkServer.active && NetworkClient.active)
        {
            // Host 模式：先停止
            StopHost();
        }
        else if (NetworkClient.active)
        {
            // 纯客户端
            StopClient();
        }

        // 主动离开时，直接切换到主菜单
        // 使用协程延迟一帧，确保网络已完全停止
        StartCoroutine(ReturnToMainMenu());
    }

    private System.Collections.IEnumerator ReturnToMainMenu()
    {
        yield return null; // 等待一帧
        SceneManager.LoadScene(mainMenuScene);
    }

    /// <summary>
    /// 开始游戏（切换到游戏场景）
    /// </summary>
    [Server]
    public void StartGame()
    {
        if (!CanStartGame())
        {
            Debug.LogWarning("[Server] 无法开始游戏：玩家数量不符合要求");
            return;
        }

        Debug.Log("[Server] 开始游戏，切换场景");
        ServerChangeScene(gameScene);
    }

    /// <summary>
    /// 检查是否可以开始游戏
    /// </summary>
    public bool CanStartGame()
    {
        int playerCount = GetPlayerCount();
        return playerCount >= minPlayersToStart && playerCount <= maxPlayers;
    }

    /// <summary>
    /// 获取当前玩家数量
    /// </summary>
    public int GetPlayerCount()
    {
        if (NetworkServer.active)
        {
            return NetworkServer.connections.Count;
        }
        return 0;
    }

    /// <summary>
    /// 获取所有玩家数据（服务器端）
    /// </summary>
    public List<PlayerData> GetAllPlayers()
    {
        List<PlayerData> players = new List<PlayerData>();

        if (NetworkServer.active)
        {
            // 服务器端：从 spawned 获取
            foreach (var kvp in NetworkServer.spawned)
            {
                PlayerData pd = kvp.Value.GetComponent<PlayerData>();
                if (pd != null)
                {
                    players.Add(pd);
                }
            }
        }
        else if (NetworkClient.active)
        {
            // 客户端：从场景中查找
            PlayerData[] allPlayers = Object.FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            players.AddRange(allPlayers);
        }

        return players;
    }

    /// <summary>
    /// 获取本机 IP 地址
    /// </summary>
    public string GetLocalIP()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    // 优先返回局域网 IP
                    string ipStr = ip.ToString();
                    if (ipStr.StartsWith("192.168.") || ipStr.StartsWith("10.") || ipStr.StartsWith("172."))
                    {
                        return ipStr;
                    }
                }
            }
            // 如果没有找到局域网 IP，返回任意 IPv4
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NetworkManager] 获取 IP 失败: {e.Message}");
        }
        return "127.0.0.1";
    }

    /// <summary>
    /// 获取服务器 IP（如果是 Host 返回本机 IP，否则返回连接的服务器地址）
    /// </summary>
    public string GetServerIP()
    {
        if (NetworkServer.active)
        {
            return GetLocalIP();
        }
        return networkAddress;
    }

    // ============ 兼容旧代码的属性 ============
    
    /// <summary>
    /// 兼容旧代码：isHost 属性
    /// </summary>
    public bool isHost
    {
        get => IsHost;
        set => IsHost = value;
    }

    /// <summary>
    /// 兼容旧代码：GetRoomPlayers 方法
    /// </summary>
    public List<PlayerData> GetRoomPlayers() => GetAllPlayers();
}

