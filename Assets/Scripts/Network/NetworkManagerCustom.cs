using UnityEngine;
using Mirror;
using System.Collections.Generic;

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

    // 当前房间内的玩家列表
    public List<PlayerData> roomPlayers = new List<PlayerData>();

    public static NetworkManagerCustom Instance { get; private set; }

    public override void Awake()
    {
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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void Start()
    {
        base.Start();
        if (mainMenu == null)
            mainMenu = FindFirstObjectByType<MainMenu>();
        if (roomUI == null)
            roomUI = FindFirstObjectByType<RoomUI>();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);
        
        PlayerData playerData = player.GetComponent<PlayerData>();
        if (playerData != null)
        {
            roomPlayers.Add(playerData);
            UpdateRoomUI();
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        PlayerData playerData = conn.identity.GetComponent<PlayerData>();
        if (playerData != null)
        {
            roomPlayers.Remove(playerData);
            UpdateRoomUI();
        }
        base.OnServerDisconnect(conn);
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        if (roomUI != null)
            roomUI.OnDisconnected();
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
        StartHost();
        
        Debug.Log($"StartHost() 已调用，NetworkServer.active={NetworkServer.active}, NetworkClient.active={NetworkClient.active}");
        
        // 延迟更新 UI，等待网络启动
        StartCoroutine(UpdateRoomUIAfterHostStart());
    }
    
    /// <summary>
    /// 场景切换后创建房间的协程（由 MainMenu 调用）
    /// </summary>
    public System.Collections.IEnumerator CreateRoomAfterSceneLoad()
    {
        // 等待场景加载完成
        yield return new WaitForSeconds(0.3f);
        
        // 创建房间（启动Host）
        Debug.Log("NetworkManagerCustom: 开始创建房间（启动 Host）");
        CreateRoom();
        
        // 等待网络启动（给更多时间）
        yield return new WaitForSeconds(0.5f);
        
        // 查找 RoomUI 并更新显示
        RoomUI roomUI = FindFirstObjectByType<RoomUI>();
        int roomUIAttempts = 0;
        while (roomUI == null && roomUIAttempts < 10)
        {
            yield return new WaitForSeconds(0.1f);
            roomUI = FindFirstObjectByType<RoomUI>();
            roomUIAttempts++;
        }
        
        if (roomUI != null)
        {
            string ip = GetLocalIP();
            Debug.Log($"NetworkManagerCustom: 更新 RoomUI，IP={ip}");
            roomUI.OnRoomCreated(ip);
        }
        else
        {
            Debug.LogWarning("NetworkManagerCustom: 找不到 RoomUI 组件");
        }
    }

    
    private System.Collections.IEnumerator UpdateRoomUIAfterHostStart()
    {
        // 等待网络启动
        int attempts = 0;
        while (!NetworkServer.active && attempts < 50)
        {
            yield return new WaitForSeconds(0.1f);
            attempts++;
        }
        
        if (NetworkServer.active)
        {
            Debug.Log("Host 已启动，更新 RoomUI");
            if (roomUI != null)
            {
                roomUI.OnRoomCreated(GetLocalIP());
            }
            else
            {
                // 如果 roomUI 为 null，尝试查找
                roomUI = FindFirstObjectByType<RoomUI>();
                if (roomUI != null)
                {
                    roomUI.OnRoomCreated(GetLocalIP());
                }
            }
        }
        else
        {
            Debug.LogError("Host 启动失败！");
        }
    }

    public void JoinRoom(string ip)
    {
        networkAddress = ip;
        StartClient();
    }

    public void LeaveRoom()
    {
        if (NetworkServer.active && NetworkClient.active)
        {
            StopHost();
        }
        else if (NetworkClient.active)
        {
            StopClient();
        }
    }

    public bool CanStartGame()
    {
        return roomPlayers.Count >= minPlayersToStart && roomPlayers.Count <= maxPlayers;
    }

    private void UpdateRoomUI()
    {
        if (roomUI != null && NetworkServer.active)
        {
            roomUI.UpdatePlayerList(roomPlayers);
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
}

