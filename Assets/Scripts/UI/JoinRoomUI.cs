using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 加入房间 UI
/// 位置：Assets/Scripts/UI/JoinRoomUI.cs
/// 功能：输入 IP，加入房间，显示连接状态
/// </summary>
public class JoinRoomUI : MonoBehaviour
{
    [Header("加入房间UI")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button scanQRButton;
    [SerializeField] private Button backButton;

    [Header("状态显示")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject connectingIndicator; // 连接中的加载指示器

    private void OnEnable()
    {
        // 订阅事件
        NetworkManagerCustom.OnConnected += OnConnected;
        NetworkManagerCustom.OnConnectionFailed += OnConnectionFailed;
    }

    private void OnDisable()
    {
        // 取消订阅
        NetworkManagerCustom.OnConnected -= OnConnected;
        NetworkManagerCustom.OnConnectionFailed -= OnConnectionFailed;
    }

    private void Start()
    {
        // 初始化按钮
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);
        if (scanQRButton != null)
            scanQRButton.onClick.AddListener(OnScanQRClicked);

        // 初始状态
        SetStatus("请输入房间 IP 地址");
        SetConnecting(false);
    }

    private void OnBackClicked()
    {
        // 如果正在连接，先取消
        if (NetworkManagerCustom.Instance != null && NetworkManagerCustom.Instance.IsConnecting)
        {
            NetworkManagerCustom.Instance.StopClient();
        }
        
        // 返回主菜单
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private void OnConnectClicked()
    {
        string ip = ipInputField != null ? ipInputField.text.Trim() : "";
        
        if (string.IsNullOrEmpty(ip))
        {
            SetStatus("请输入有效的 IP 地址", true);
            return;
        }

        if (NetworkManagerCustom.Instance == null)
        {
            SetStatus("网络管理器未找到", true);
            return;
        }

        // 显示连接中状态
        SetStatus($"正在连接到 {ip}...");
        SetConnecting(true);
        
        // 尝试连接
        NetworkManagerCustom.Instance.JoinRoom(ip);
    }

    /// <summary>
    /// 连接成功
    /// </summary>
    private void OnConnected()
    {
        Debug.Log("[JoinRoomUI] 连接成功");
        SetStatus("连接成功！正在加入房间...");
        SetConnecting(false);
        // 场景会自动切换到 CreateRoom
    }

    /// <summary>
    /// 连接失败
    /// </summary>
    private void OnConnectionFailed(string reason)
    {
        Debug.LogWarning($"[JoinRoomUI] 连接失败: {reason}");
        SetStatus(reason, true);
        SetConnecting(false);
    }

    /// <summary>
    /// 设置状态文本
    /// </summary>
    private void SetStatus(string message, bool isError = false)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.white;
        }
    }

    /// <summary>
    /// 设置连接中状态
    /// </summary>
    private void SetConnecting(bool connecting)
    {
        if (connectButton != null)
            connectButton.interactable = !connecting;
        
        if (connectingIndicator != null)
            connectingIndicator.SetActive(connecting);
    }

    private void OnScanQRClicked()
    {
        // 打开二维码扫描（需要实现）
        SetStatus("二维码扫描功能开发中...");
    }
}

