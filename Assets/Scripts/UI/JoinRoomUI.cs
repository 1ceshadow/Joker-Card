using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// join房间UI
/// 位置：Assets/Scripts/UI/RoomUI2.cs
/// 功能：输入ip，加入房间
/// </summary>
public class JoinRoomUI : MonoBehaviour
{

    [Header("加入房间UI")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button scanQRButton;
    [SerializeField] private Button leaveRoomButton;


    private NetworkManagerCustom networkManager;


    private void Start()
    {
        if (networkManager == null)
            networkManager = FindFirstObjectByType<NetworkManagerCustom>();

        // 初始化按钮

        if (leaveRoomButton != null)
            leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);
        if (scanQRButton != null)
            scanQRButton.onClick.AddListener(OnScanQRClicked);

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
            Debug.Log("Joining room at IP: " + ip);
            // 必须使用 StartCoroutine 才能执行协程，否则协程内的代码不会运行
            networkManager.StartCoroutine(networkManager.JoinRoomAfterSceneLoad(ip));
        }
        else
        {
            if (string.IsNullOrEmpty(ip))
                Debug.LogWarning("JoinRoomUI: 请输入有效的 IP 地址");
            if (networkManager == null)
                Debug.LogError("JoinRoomUI: NetworkManagerCustom 未找到");
        }
    }

    private void OnScanQRClicked()
    {
        // 打开二维码扫描（需要实现）
        // QRCodeScanner.Instance?.StartScan();
    }

}

