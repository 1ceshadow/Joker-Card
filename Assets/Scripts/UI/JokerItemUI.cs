using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 小丑牌UI组件
/// 位置：Assets/Scripts/UI/JokerItemUI.cs
/// 功能：显示小丑牌信息，处理购买/售卖
/// </summary>
public class JokerItemUI : MonoBehaviour
{
    [Header("小丑牌显示")]
    [SerializeField] private Image jokerImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI priceText;

    [Header("按钮")]
    [SerializeField] private Button actionButton;
    [SerializeField] private Button sellButton;

    private JokerData jokerData;
    private int index;
    private System.Action<int> onBuyCallback;
    private System.Action<int> onSellCallback;
    private bool isShopItem = false;

    public void InitializeShopItem(JokerData joker, int index, System.Action<int> onBuy)
    {
        this.jokerData = joker;
        this.index = index;
        this.onBuyCallback = onBuy;
        this.isShopItem = true;

        UpdateDisplay();
    }

    public void InitializePlayerItem(JokerData joker, int index, System.Action<int> onSell)
    {
        this.jokerData = joker;
        this.index = index;
        this.onSellCallback = onSell;
        this.isShopItem = false;

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (jokerData == null)
            return;

        if (nameText != null)
            nameText.text = jokerData.name;

        if (descriptionText != null)
            descriptionText.text = jokerData.GetDescription();

        if (isShopItem)
        {
            if (priceText != null)
                priceText.text = $"价格: {jokerData.shopPrice}";

            if (actionButton != null)
            {
                actionButton.GetComponentInChildren<TextMeshProUGUI>().text = "购买";
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(OnBuyClicked);
            }

            if (sellButton != null)
                sellButton.gameObject.SetActive(false);
        }
        else
        {
            if (priceText != null)
                priceText.text = $"售卖: {jokerData.sellPrice}";

            if (actionButton != null)
                actionButton.gameObject.SetActive(false);

            if (sellButton != null)
            {
                // 如果有售卖回调，显示售卖按钮；否则隐藏（游戏内不需要售卖）
                if (onSellCallback != null)
                {
                    sellButton.gameObject.SetActive(true);
                    sellButton.onClick.RemoveAllListeners();
                    sellButton.onClick.AddListener(OnSellClicked);
                }
                else
                {
                    sellButton.gameObject.SetActive(false);
                }
            }
        }

        // 加载小丑牌图片
        if (jokerImage != null)
        {
            Sprite jokerSprite = JokerSpriteLoader.GetJokerSprite(jokerData.type);
            if (jokerSprite != null)
                jokerImage.sprite = jokerSprite;
            else
                Debug.LogWarning($"小丑牌图片未找到: {jokerData.type}");
        }
    }

    private void OnBuyClicked()
    {
        onBuyCallback?.Invoke(index);
    }

    private void OnSellClicked()
    {
        onSellCallback?.Invoke(index);
    }

    public void OnJokerClicked()
    {
        // 显示/隐藏售卖按钮
        if (!isShopItem && sellButton != null)
        {
            sellButton.gameObject.SetActive(!sellButton.gameObject.activeSelf);
        }
    }
}

