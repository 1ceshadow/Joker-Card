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

        // 检查是房主还是加入者
        if (NetworkServer.active)
        {
            // 房主：显示房间信息
            ShowHostUI();
        }
        else
        {
            // 加入者：显示加入界面
            ShowJoinUI();
        }
    }

    private void ShowHostUI()
    {
        if (joinRoomPanel != null)
            joinRoomPanel.SetActive(false);

        if (roomIPText != null && networkManager != null)
        {
            string ip = networkManager.GetServerIP();
            roomIPText.text = $"房间IP: {ip}";
        }

        // 生成二维码
        if (qrCodeImage != null && networkManager != null)
        {
            string ip = networkManager.GetServerIP();
            GenerateQRCode(ip);
        }
    }

    private void ShowJoinUI()
    {
        if (joinRoomPanel != null)
            joinRoomPanel.SetActive(true);
    }

    public void OnRoomCreated(string ip)
    {
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

