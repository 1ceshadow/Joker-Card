using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

/// <summary>
/// 主菜单UI
/// 位置：Assets/Scripts/UI/MainMenu.cs
/// 功能：主菜单界面，显示玩家信息、商店、创建/加入房间按钮
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("玩家信息UI")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("按钮")]
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private Button exitButton;

    [Header("商店UI")]
    [SerializeField] private ShopUI shopUI;

    [Header("玩家信息输入窗口")]
    [SerializeField] private GameObject playerInfoWindow;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Transform avatarSelectionParent;
    [SerializeField] private GameObject avatarButtonPrefab;

    private int selectedAvatarId = 0;
    private PlayerDataManager dataManager;

    private void Start()
    {
        if (dataManager == null)
            dataManager = FindFirstObjectByType<PlayerDataManager>();

        // 初始化按钮事件
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        if (joinRoomButton != null)
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmPlayerInfo);

        // 检查玩家信息
        CheckPlayerInfo();

        // 初始化商店
        if (shopUI != null)
            shopUI.Initialize();
    }

    private void CheckPlayerInfo()
    {
        if (dataManager == null)
            return;

        PlayerSaveData saveData = dataManager.LoadPlayerData();
        if (saveData == null || string.IsNullOrEmpty(saveData.playerName))
        {
            // 显示玩家信息输入窗口
            ShowPlayerInfoWindow();
        }
        else
        {
            // 加载玩家信息
            LoadPlayerInfo(saveData);
        }
    }

    private void ShowPlayerInfoWindow()
    {
        if (playerInfoWindow != null)
            playerInfoWindow.SetActive(true);

        // 初始化头像选择
        if (avatarSelectionParent != null && avatarButtonPrefab != null)
        {
            // 清除现有按钮
            foreach (Transform child in avatarSelectionParent)
            {
                Destroy(child.gameObject);
            }

            // 创建头像选择按钮（假设有多个头像）
            for (int i = 0; i < 10; i++) // 假设有10个头像
            {
                GameObject buttonObj = Instantiate(avatarButtonPrefab, avatarSelectionParent);
                Button button = buttonObj.GetComponent<Button>();
                int avatarId = i;
                button.onClick.AddListener(() => OnAvatarSelected(avatarId));
            }
        }
    }

    private void OnAvatarSelected(int avatarId)
    {
        selectedAvatarId = avatarId;
        // 可以在这里更新预览
    }

    private void OnConfirmPlayerInfo()
    {
        string playerName = nameInputField != null ? nameInputField.text : "Player";
        if (string.IsNullOrEmpty(playerName))
            playerName = "Player";

        // 保存玩家信息
        if (dataManager != null)
        {
            PlayerSaveData saveData = new PlayerSaveData
            {
                playerName = playerName,
                avatarId = selectedAvatarId,
                money = 20,
                debt = 0,
                jokers = new System.Collections.Generic.List<JokerData>()
            };
            dataManager.SavePlayerData(saveData);
        }

        // 关闭窗口
        if (playerInfoWindow != null)
            playerInfoWindow.SetActive(false);

        // 加载玩家信息
        LoadPlayerInfo(dataManager?.LoadPlayerData());
    }

    private void LoadPlayerInfo(PlayerSaveData saveData)
    {
        if (saveData == null)
            return;

        if (playerNameText != null)
            playerNameText.text = saveData.playerName;

        if (moneyText != null)
        {
            if (saveData.debt > 0)
                moneyText.text = $"负债: {saveData.debt}";
            else
                moneyText.text = $"资金: {saveData.money}";
        }

        // 加载头像
        if (avatarImage != null)
        {
            Sprite avatarSprite = AvatarSpriteLoader.GetAvatarSprite(saveData.avatarId);
            if (avatarSprite != null)
                avatarImage.sprite = avatarSprite;
        }
    }

    private void OnCreateRoomClicked()
    {
        if (NetworkManagerCustom.Instance != null)
        {
            NetworkManagerCustom.Instance.CreateRoom();
            // 切换到房间场景
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameRoom");
        }
    }

    private void OnJoinRoomClicked()
    {
        // 切换到房间场景（加入房间界面）
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameRoom");
    }

    private void OnExitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void Update()
    {
        // 更新玩家信息显示
        if (dataManager != null)
        {
            PlayerSaveData saveData = dataManager.LoadPlayerData();
            if (saveData != null)
            {
                if (moneyText != null)
                {
                    if (saveData.debt > 0)
                        moneyText.text = $"负债: {saveData.debt}";
                    else
                        moneyText.text = $"资金: {saveData.money}";
                }
            }
        }
    }
}

