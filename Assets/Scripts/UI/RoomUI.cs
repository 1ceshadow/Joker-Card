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

        if (roomIPText != null && networkManager != null)
        {
            string ip = networkManager.GetServerIP();
            if (string.IsNullOrEmpty(ip))
                ip = networkManager.GetLocalIP();
            if (NetworkManagerCustom.Instance.isHost)
            {
                roomIPText.text = $"房间IP: {ip}";
            }
            else
            {
                roomIPText.text = $"已加入房间: {networkManager.networkAddress}";
            }
        }

        // 生成二维码
        if (qrCodeImage != null && networkManager != null)
        {
            string ip = networkManager.GetServerIP();
            if (string.IsNullOrEmpty(ip))
                ip = networkManager.GetLocalIP();
            GenerateQRCode(ip);
        }

        // 开始按钮根据玩家数启用
        if (startGameButton != null)
            startGameButton.interactable = networkManager.CanStartGame();

        // 延迟检查，等待网络状态稳定
        // 如果是房主检测服务器激活状态，显示HostUI

            //StartCoroutine(CheckNetworkStatus());
    }


    public void OnRoomCreated(string ip)
    {
        Debug.Log($"房间已创建，IP: {ip}");
        //ShowHostUI();

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
        if (startGameButton != null && networkManager != null) // && NetworkServer.active
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

}

