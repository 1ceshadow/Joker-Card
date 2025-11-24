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
    [SerializeField] private Image avatarPreviewImage; // 头像预览

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
        if (nameInputField != null)
            nameInputField.onValueChanged.AddListener(OnNameInputChanged);

        // 初始时禁用确认按钮
        if (confirmButton != null)
            confirmButton.interactable = false;

        // 检查玩家信息
        CheckPlayerInfo();

        // 初始化商店
        InitializeShop();
    }

    private void CheckPlayerInfo()
    {
        if (dataManager == null)
        {
            // 如果找不到 PlayerDataManager，创建一个
            GameObject managerObj = new GameObject("PlayerDataManager");
            dataManager = managerObj.AddComponent<PlayerDataManager>();
        }

        PlayerSaveData saveData = dataManager.LoadPlayerData();
        if (saveData == null || string.IsNullOrEmpty(saveData.playerName))
        {
            // 显示玩家信息输入窗口
            Debug.Log("玩家信息为空，显示输入窗口");
            ShowPlayerInfoWindow();
        }
        else
        {
            // 加载玩家信息
            Debug.Log($"加载玩家信息: {saveData.playerName}");
            LoadPlayerInfo(saveData);
        }
    }

    private void ShowPlayerInfoWindow()
    {
        if (playerInfoWindow != null)
        {
            playerInfoWindow.SetActive(true);
            Debug.Log("玩家信息窗口已显示");
        }
        else
        {
            Debug.LogError("playerInfoWindow 引用未分配！请在 Inspector 中分配。");
        }

        // 初始化头像选择
        if (avatarSelectionParent != null && avatarButtonPrefab != null)
        {
            // 清除现有按钮
            foreach (Transform child in avatarSelectionParent)
            {
                Destroy(child.gameObject);
            }

            // 确保有 Grid Layout Group
            UnityEngine.UI.GridLayoutGroup gridLayout = avatarSelectionParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (gridLayout == null)
            {
                gridLayout = avatarSelectionParent.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
                gridLayout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 5; // 5列，可以根据需要调整
                // 设置对齐方式：左上角对齐
                gridLayout.childAlignment = TextAnchor.UpperLeft;
                gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
                Debug.Log("已自动添加 GridLayoutGroup 到 avatarSelectionParent");
            }

            // 设置 avatarSelectionParent 的 RectTransform（横向拉伸并顶端对齐）
            RectTransform rectTransform = avatarSelectionParent.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 拉伸内容以匹配 ScrollRect 的宽度并从上方开始布局，便于 GridLayoutGroup 正常计算列宽
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1); // 横向拉伸到父对象宽度
                rectTransform.pivot = new Vector2(0.5f, 1f);
                // 将位置重置为 (0,0)
                rectTransform.anchoredPosition = Vector2.zero;
                // 保持当前高度（sizeDelta.y），将宽度置0以使用拉伸尺寸
                rectTransform.sizeDelta = new Vector2(0, rectTransform.sizeDelta.y);
            }

            // 确保父对象有 ScrollRect（用于滚动）
            UnityEngine.UI.ScrollRect scrollRect = avatarSelectionParent.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            if (scrollRect == null)
            {
                // 如果没有 ScrollRect，尝试在 avatarSelectionParent 的父对象上添加
                Transform parent = avatarSelectionParent.parent;
                if (parent != null)
                {
                    scrollRect = parent.GetComponent<UnityEngine.UI.ScrollRect>();
                    if (scrollRect == null)
                    {
                        // 自动添加 ScrollRect
                        scrollRect = parent.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
                        scrollRect.content = rectTransform;
                        scrollRect.horizontal = false; // 垂直滚动
                        scrollRect.vertical = true;
                        scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;
                        Debug.Log("已自动添加 ScrollRect 到 avatarSelectionParent 的父对象");
                    }
                }
            }
            else
            {
                // 确保 ScrollRect 的 content 指向 avatarSelectionParent
                if (scrollRect.content != rectTransform)
                {
                    scrollRect.content = rectTransform;
                }
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }

            // 获取头像总数
            int avatarCount = AvatarSpriteLoader.GetAvatarCount();
            if (avatarCount == 0)
                avatarCount = 10; // 默认10个

            // 创建头像选择按钮
            for (int i = 0; i < avatarCount; i++)
            {
                GameObject buttonObj = Instantiate(avatarButtonPrefab, avatarSelectionParent);
                Button button = buttonObj.GetComponent<Button>();
                
                // 设置头像按钮的图片
                Image buttonImage = buttonObj.GetComponent<Image>();
                if (buttonImage == null)
                    buttonImage = buttonObj.GetComponentInChildren<Image>();
                
                if (buttonImage != null)
                {
                    Sprite avatarSprite = AvatarSpriteLoader.GetAvatarSprite(i);
                    if (avatarSprite != null)
                        buttonImage.sprite = avatarSprite;
                }
                
                int avatarId = i;
                button.onClick.AddListener(() => OnAvatarSelected(avatarId));
            }
            
            // 默认选择第一个头像
            if (avatarCount > 0)
            {
                OnAvatarSelected(0);
            }
            
            // 强制刷新布局，确保 GridLayoutGroup/ContentSize 等生效，修复 ScrollRect 未更新导致的滚动问题
            Canvas.ForceUpdateCanvases();
            if (rectTransform != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }

            // 如果存在 ScrollRect，重置滚动位置到顶部并清除速度
            UnityEngine.UI.ScrollRect parentScroll = avatarSelectionParent.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            if (parentScroll != null)
            {
                parentScroll.verticalNormalizedPosition = 1f; // 顶部
                parentScroll.velocity = Vector2.zero;
            }
        }
    }

    private void OnAvatarSelected(int avatarId)
    {
        selectedAvatarId = avatarId;
        Debug.Log($"选择头像: {avatarId}");
        
        // 更新预览
        if (avatarPreviewImage != null)
        {
            Sprite avatarSprite = AvatarSpriteLoader.GetAvatarSprite(avatarId);
            if (avatarSprite != null)
            {
                avatarPreviewImage.sprite = avatarSprite;
                avatarPreviewImage.gameObject.SetActive(true);
            }
        }
    }
    
    private void OnNameInputChanged(string value)
    {
        // 检查用户名是否为空，控制确认按钮状态
        if (confirmButton != null)
        {
            confirmButton.interactable = !string.IsNullOrWhiteSpace(value);
        }
    }

    private void OnConfirmPlayerInfo()
    {
        string playerName = nameInputField != null ? nameInputField.text : "";
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("请输入玩家名称！");
            return;
        }
        
        playerName = playerName.Trim();

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
        Debug.Log("点击创建房间");
        
        // 先切换到房间场景
        try
        {
            NetworkManagerCustom.Instance.isHost = true;
            // 使用 NetworkManagerCustom 来运行协程（因为它不会被销毁）
            NetworkManagerCustom networkManager = NetworkManagerCustom.Instance;
            if (networkManager == null)
            {
                networkManager = FindFirstObjectByType<NetworkManagerCustom>();
            }
            
            if (networkManager != null)
            {
                // 让 NetworkManagerCustom 处理场景切换和创建房间
                networkManager.StartCoroutine(networkManager.CreateRoomAfterSceneLoad());
            }
            else
            {
                Debug.LogError("找不到 NetworkManagerCustom！");
            }
            
            UnityEngine.SceneManagement.SceneManager.LoadScene("CreateRoom");
        }
        catch (System.Exception e)
        {
            NetworkManagerCustom.Instance.isHost = false;
            Debug.LogError($"场景切换失败: {e.Message}。请确保 'CreateRoom' 场景已添加到 Build Settings！");
        }
    }

    private void OnJoinRoomClicked()
    {
        Debug.Log("点击加入房间");
        
        // 切换到房间场景（加入房间界面）
        try
        {
            NetworkManagerCustom.Instance.isHost = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene("JoinRoom");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"场景切换失败: {e.Message}。请确保 'GameRoom' 场景已添加到 Build Settings！");
        }
    }
    

    private void InitializeShop()
    {
        // 确保 ShopManager 存在
        if (ShopManager.Instance == null)
        {
            GameObject shopManagerObj = new GameObject("ShopManager");
            shopManagerObj.AddComponent<ShopManager>();
        }
        
        // 如果商店为空，刷新商店
        if (ShopManager.Instance != null && ShopManager.Instance.GetShopJokers().Count == 0)
        {
            ShopManager.Instance.RefreshShop();
            Debug.Log("商店已刷新，生成10张小丑牌");
        }
        
        // 初始化商店UI
        if (shopUI != null)
            shopUI.Initialize();
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


