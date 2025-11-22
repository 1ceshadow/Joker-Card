using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 房间UI
/// 位置：Assets/Scripts/UI/RoomUI.cs
/// 功能：显示房间信息、玩家列表、IP和二维码、开始游戏按钮
/// </summary>
public class RoomUI : MonoBehaviour
{
    [Header("房间信息")]
    [SerializeField] private TextMeshProUGUI roomIPText;
    [SerializeField] private Image qrCodeImage;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveRoomButton;

    [Header("玩家列表")]
    [SerializeField] private Transform playerListParent;
    [SerializeField] private GameObject playerAvatarPrefab;

    [Header("加入房间UI")]
    [SerializeField] private GameObject joinRoomPanel;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button scanQRButton;

    private NetworkManagerCustom networkManager;
    private List<GameObject> playerAvatarObjects = new List<GameObject>();

    private void Start()
    {
        if (networkManager == null)
            networkManager = FindFirstObjectByType<NetworkManagerCustom>();

        // 初始化按钮
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);
        if (leaveRoomButton != null)
            leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);
        if (scanQRButton != null)
            scanQRButton.onClick.AddListener(OnScanQRClicked);

        // 延迟检查，等待网络状态稳定
        StartCoroutine(CheckNetworkStatus());
    }
    
    private System.Collections.IEnumerator CheckNetworkStatus()
    {
        // 等待更长时间，确保网络状态已初始化
        yield return new WaitForSeconds(0.5f);
        
        // 多次检查，因为网络启动需要时间
        int attempts = 0;
        while (attempts < 10)
        {
            // 检查是房主还是加入者
            if (NetworkServer.active)
            {
                // 房主：显示房间信息
                Debug.Log($"检测到房主（尝试 {attempts + 1}），显示房间信息");
                ShowHostUI();
                yield break; // 退出协程
            }
            else if (NetworkClient.active)
            {
                // 已连接的客户端：显示房间信息
                Debug.Log($"检测到客户端（尝试 {attempts + 1}），显示房间信息");
                ShowJoinUI();
                yield break; // 退出协程
            }
            
            attempts++;
            yield return new WaitForSeconds(0.2f);
        }
        
        // 如果10次尝试后仍未连接，显示加入界面
        Debug.Log("未检测到网络连接，显示加入界面");
        ShowJoinUI();
    }

    [Header("房主UI面板")]
    [SerializeField] private GameObject hostPanel; // 房主面板（包含IP、二维码、玩家列表等）

    private void ShowHostUI()
    {
        // 显示房主面板
        if (hostPanel != null)
            hostPanel.SetActive(true);
        
        // 隐藏加入面板
        if (joinRoomPanel != null)
            joinRoomPanel.SetActive(false);

        if (roomIPText != null && networkManager != null)
        {
            string ip = networkManager.GetServerIP();
            if (string.IsNullOrEmpty(ip))
                ip = networkManager.GetLocalIP();
            roomIPText.text = $"房间IP: {ip}";
        }

        // 生成二维码
        if (qrCodeImage != null && networkManager != null)
        {
            string ip = networkManager.GetServerIP();
            if (string.IsNullOrEmpty(ip))
                ip = networkManager.GetLocalIP();
            GenerateQRCode(ip);
        }
    }

    private void ShowJoinUI()
    {
        // 隐藏房主面板
        if (hostPanel != null)
            hostPanel.SetActive(false);
        
        // 显示加入面板
        if (joinRoomPanel != null)
            joinRoomPanel.SetActive(true);
    }
    
    public string GetLocalIP()
    {
        if (networkManager != null)
            return networkManager.GetServerIP();
        return "127.0.0.1";
    }

    public void OnRoomCreated(string ip)
    {
        Debug.Log($"房间已创建，IP: {ip}");
        ShowHostUI();
        
        if (roomIPText != null)
            roomIPText.text = $"房间IP: {ip}";

        GenerateQRCode(ip);
    }

    public void UpdatePlayerList(List<PlayerData> players)
    {
        // 清除现有玩家头像
        foreach (GameObject obj in playerAvatarObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        playerAvatarObjects.Clear();

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

        // 更新开始游戏按钮状态
        if (startGameButton != null && networkManager != null)
        {
            startGameButton.interactable = networkManager.CanStartGame();
        }
    }

    private void OnStartGameClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CmdStartGame();
            // 可以在这里切换到游戏场景或隐藏房间UI
        }
    }

    private void OnLeaveRoomClicked()
    {
        if (networkManager != null)
        {
            networkManager.LeaveRoom();
        }
        // 返回主菜单
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private void OnConnectClicked()
    {
        string ip = ipInputField != null ? ipInputField.text : "";
        if (!string.IsNullOrEmpty(ip) && networkManager != null)
        {
            networkManager.JoinRoom(ip);
        }
    }

    private void OnScanQRClicked()
    {
        // 打开二维码扫描（需要实现）
        // QRCodeScanner.Instance?.StartScan();
    }

    private void GenerateQRCode(string text)
    {
        // 使用ZXing生成二维码
        if (QRCodeGenerator.Instance != null)
        {
            Texture2D qrTexture = QRCodeGenerator.Instance.GenerateQRCode(text);
            if (qrTexture != null && qrCodeImage != null)
            {
                Sprite qrSprite = Sprite.Create(qrTexture, new Rect(0, 0, qrTexture.width, qrTexture.height), Vector2.one * 0.5f);
                qrCodeImage.sprite = qrSprite;
            }
        }
    }

    public void OnDisconnected()
    {
        // 显示断开连接提示
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}

