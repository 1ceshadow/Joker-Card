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
        StartHost();
        if (roomUI != null)
            roomUI.OnRoomCreated(GetLocalIP());
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

    private string GetLocalIP()
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
        return networkAddress;
    }
}

