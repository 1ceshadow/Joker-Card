using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // 新增：用于场景切换

/// <summary>
/// 自定义网络管理器
/// 位置：Assets/Scripts/Network/NetworkManagerCustom.cs
/// 功能：管理网络连接、房间创建、玩家加入等
/// </summary>
public class NetworkManagerCustom : NetworkManager
{
    [Header("房间设置")]
    [SerializeField] private int maxPlayers = 5;
    [SerializeField] private int minPlayersToStart = 2;

    [Header("UI引用")]
    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private RoomUI roomUI;

    // room player netIds are stored in RoomState.roomPlayerIds (SyncList<uint>)
    public bool isHost = false;

    public static NetworkManagerCustom Instance { get; private set; }

    public override void Awake()
    {
        // 检查是否已经有 Instance（避免 Multiple NetworkManagers 警告）
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("NetworkManagerCustom 已存在，销毁重复的实例");
            Destroy(gameObject);
            return;
        }

        // 确保有 Transport 组件
        if (transport == null)
        {
            // 尝试查找现有的 Transport
            transport = GetComponent<Transport>();
            if (transport == null)
            {
                // 添加默认的 Telepathy Transport
                transport = gameObject.AddComponent<TelepathyTransport>();
                Debug.Log("已自动添加 TelepathyTransport 组件");
            }
        }

        base.Awake();
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("NetworkManagerCustom 实例已创建并标记为持久化");
        }
    }

    public override void Start()
    {
        base.Start();
        if (mainMenu == null)
            mainMenu = FindFirstObjectByType<MainMenu>();
        if (roomUI == null)
        {
            Debug.Log("开始寻找RoomUI");
            roomUI = FindFirstObjectByType<RoomUI>();
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);

        PlayerData playerData = player.GetComponent<PlayerData>();
        if (playerData != null)
        {
            Debug.Log($"玩家已加入，netId={playerData.netId}");
            
            // 立即更新服务器端的 RoomUI
            // 使用 GetRoomPlayers() 直接从 spawned 字典中查询，无需依赖 RoomState SyncList
            UpdateRoomUI();
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        PlayerData playerData = conn.identity.GetComponent<PlayerData>();
        if (playerData != null)
        {
            Debug.Log($"玩家已离开，netId={playerData.netId}");
            
            // 立即更新服务器端的 RoomUI
            UpdateRoomUI();
        }
        
        base.OnServerDisconnect(conn);
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("客户端连接成功");

        // 只有客户端时才需要更新 UI（host 兼具 server 和 client）
        if (!NetworkServer.active)
        {
            Debug.Log("客户端已连接到服务器，等待加载 CreateRoom 场景...");
            // 场景加载后 RoomUI 会触发 UpdatePlayerList，在 UpdateRoomUIClientAfterConnect 中处理
            StartCoroutine(UpdateRoomUIClientAfterConnect());
        }
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("客户端断开连接");

        // 如果还有其他网络连接，等待一下再切换场景
        if (!NetworkClient.active && !NetworkServer.active)
        {
            Debug.Log("网络已完全断开，返回主菜单");
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void CreateRoom()
    {
        Debug.Log("NetworkManagerCustom.CreateRoom() 被调用");

        // 确保 Transport 存在
        if (transport == null)
        {
            transport = GetComponent<Transport>();
            if (transport == null)
            {
                transport = gameObject.AddComponent<TelepathyTransport>();
                Debug.Log("已自动添加 TelepathyTransport 组件");
            }
        }

        // 启动 Host（服务器+客户端）
        isHost = true;
        StartHost();

        Debug.Log($"StartHost() 已调用，NetworkServer.active={NetworkServer.active}, NetworkClient.active={NetworkClient.active}");

        // 延迟更新 UI，等待网络启动
        StartCoroutine(UpdateRoomUIAfterHostStart());
    }

    /// <summary>
    /// 房主场景切换到 CreateRoom 后的初始化（启动 Host）
    /// </summary>
    public System.Collections.IEnumerator CreateRoomAfterSceneLoad()
    {
        Debug.Log("房主: 场景加载完成，准备启动 Host");
        
        // 等待场景加载完成
        yield return new WaitForSeconds(0.2f);

        // 启动 Host
        CreateRoom();

        // 等待服务器启动
        int attempts = 0;
        while (!NetworkServer.active && attempts < 50)
        {
            yield return new WaitForSeconds(0.1f);
            attempts++;
        }

        if (!NetworkServer.active)
        {
            Debug.LogError("房主: Host 启动失败！");
            yield break;
        }

        Debug.Log("房主: Host 已启动，现在等待 RoomUI 加载完成...");

        // 等待 RoomUI 加载完成
        int uiAttempts = 0;
        RoomUI roomUI = null;
        while (roomUI == null && uiAttempts < 50)
        {
            roomUI = FindFirstObjectByType<RoomUI>();
            if (roomUI == null)
            {
                yield return new WaitForSeconds(0.1f);
                uiAttempts++;
            }
        }

        if (roomUI != null)
        {
            string ip = GetLocalIP();
            Debug.Log($"房主: RoomUI 已加载，设置房间 IP: {ip}");
            roomUI.OnRoomCreated(ip);
        }
        else
        {
            Debug.LogWarning("房主: 未能找到 RoomUI 组件");
        }
    }

    private System.Collections.IEnumerator UpdateRoomUIAfterHostStart()
    {
        // 这个协程在 CreateRoom() 后被调用
        // 当新客户端加入时，更新房主端的 RoomUI
        
        int attempts = 0;
        while (!NetworkServer.active && attempts < 20)
        {
            yield return new WaitForSeconds(0.1f);
            attempts++;
        }

        if (NetworkServer.active)
        {
            RoomUI roomUI = FindFirstObjectByType<RoomUI>();
            if (roomUI != null)
            {
                // 立即更新一次玩家列表
                UpdateRoomUI();
                Debug.Log("房主: 房间已启动，已更新 RoomUI");
            }
        }
    }



    // 客户端在 CreateRoom 场景加载后更新 RoomUI（同步玩家列表）
    private System.Collections.IEnumerator UpdateRoomUIClientAfterConnect()
    {
        int uiAttempts = 0;
        RoomUI ui = null;
        
        // 等待 RoomUI 出现（场景加载后）
        while (ui == null && uiAttempts < 50)
        {
            ui = FindFirstObjectByType<RoomUI>();
            if (ui == null)
            {
                yield return new WaitForSeconds(0.1f);
                uiAttempts++;
            }
        }

        if (ui == null)
        {
            Debug.LogWarning("客户端: 找不到 RoomUI 组件，无法更新玩家列表");
            yield break;
        }

        Debug.Log("客户端: 找到 RoomUI，等待玩家对象生成...");

        // 等待玩家对象被 spawn 到客户端
        // Mirror 的玩家对象会自动从服务器 spawn 到客户端
        int playerAttempts = 0;
        List<PlayerData> players = new List<PlayerData>();
        
        while (playerAttempts < 50)
        {
            players.Clear();
            PlayerData[] allPlayers = GameObject.FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            if (allPlayers.Length > 0)
            {
                players.AddRange(allPlayers);
                Debug.Log($"客户端: 找到 {players.Count} 个玩家，更新 RoomUI");
                ui.UpdatePlayerList(players);
                yield break;
            }
            
            playerAttempts++;
            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogWarning("客户端: 等待玩家对象超时");
    }

    public System.Collections.IEnumerator JoinRoomAfterSceneLoad(string ip)
    {
        Debug.Log("NetworkManagerCustom: 开始加入房间，IP=" + ip);
        JoinRoom(ip);

        // 等待客户端连接到服务器
        int attempts = 0;
        while (!NetworkClient.active && attempts < 50)
        {
            yield return new WaitForSeconds(0.1f);
            attempts++;
        }

        if (!NetworkClient.active)
        {
            Debug.LogError("NetworkManagerCustom: 客户端连接失败，IP=" + ip + " 可能不正确或服务器未运行");
            yield break;
        }

        Debug.Log("NetworkManagerCustom: 客户端已连接，现在加载 CreateRoom 场景与房主同步");
        // 客户端加入后立即加载 CreateRoom 场景（和房主的场景相同），此时网络已建立
        SceneManager.LoadScene("CreateRoom");
    }

    public void JoinRoom(string ip)
    {
        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogError("JoinRoom: IP 地址不能为空");
            return;
        }

        isHost = false;
        networkAddress = ip;
        Debug.Log("NetworkManagerCustom: 正在以客户端模式连接到 " + ip);
        StartClient();
    }

    public void LeaveRoom()
    {
        Debug.Log("LeaveRoom 被调用");
        
        if (NetworkServer.active && NetworkClient.active)
        {
            // Host 模式：同时停止服务器和客户端
            Debug.Log("停止 Host");
            StopHost();
        }
        else if (NetworkClient.active)
        {
            // 纯客户端模式：只停止客户端
            Debug.Log("停止 Client");
            StopClient();
        }
        
        isHost = false;
        
        // 销毁 NetworkManager 以避免 Multiple NetworkManagers 警告
        // （当下次进入时会重新创建新的 NetworkManager）
        Destroy(gameObject);
    }

    public bool CanStartGame()
    {
        // 直接从 spawned 字典中计数玩家，而不是依赖 RoomState SyncList
        int playerCount = GetRoomPlayers().Count;
        bool canStart = playerCount >= minPlayersToStart && playerCount <= maxPlayers;
        return canStart;
    }

    private void UpdateRoomUI()
    {
        if (roomUI != null && NetworkServer.active)
        {
            // 直接使用 GetRoomPlayers() 获取所有玩家
            List<PlayerData> players = GetRoomPlayers();
            roomUI.UpdatePlayerList(players);
            Debug.Log($"房间 UI 已更新，当前玩家数={players.Count}");
        }
    }

    public string GetLocalIP()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    public string GetServerIP()
    {
        if (NetworkServer.active)
        {
            return GetLocalIP();
        }
        return networkAddress;
    }

    // Helper: return server-side list of PlayerData constructed from RoomState.roomPlayerIds
    public List<PlayerData> GetRoomPlayers()
    {
        List<PlayerData> players = new List<PlayerData>();
        RoomState roomState = GetComponent<RoomState>();
        if (roomState == null)
            return players;

        foreach (uint netId in roomState.roomPlayerIds)
        {
            if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity))
            {
                PlayerData pd = identity.GetComponent<PlayerData>();
                if (pd != null)
                    players.Add(pd);
            }
        }

        return players;
    }
}

