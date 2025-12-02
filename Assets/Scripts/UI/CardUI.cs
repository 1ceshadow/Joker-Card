using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 卡牌UI组件（简单显示版）
/// 位置：Assets/Scripts/UI/CardUI.cs
/// 功能：显示单张卡牌，处理点击选择（用于GameUI中的手牌显示）
/// </summary>
public class CardUI : MonoBehaviour
{
    [Header("卡牌显示")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private Image suitImage;

    private CardData cardData;
    private bool isSelected = false;
    private System.Action<CardData, bool> onCardClicked;
    private Vector3 originalPosition;

    private void Start()
    {
        originalPosition = transform.localPosition;
    }

    /// <summary>
    /// 初始化卡牌UI
    /// </summary>
    public void Initialize(CardData data, System.Action<CardData, bool> onClickCallback)
    {
        this.cardData = data;
        this.onCardClicked = onClickCallback;

        UpdateCardDisplay();
    }

    private void UpdateCardDisplay()
    {
        if (cardData == null)
            return;

        // 加载并显示卡牌图片
        if (cardImage != null)
        {
            Sprite cardSprite = CardSpriteLoader.GetCardSprite(cardData.suit, cardData.rank);
            if (cardSprite != null)
            {
                cardImage.sprite = cardSprite;
            }
            else
            {
                // 如果图片加载失败，显示背景
                cardImage.sprite = CardSpriteLoader.GetCardBackSprite();
            }
        }

        // 显示点数（如果图片中不包含，可以显示文本）
        if (rankText != null)
        {
            rankText.text = cardData.GetRankName();
            // 如果图片已包含点数，可以隐藏文本
            rankText.gameObject.SetActive(false); // 默认隐藏
        }

        // 显示花色（可以用颜色或图标）
        if (suitImage != null)
        {
            // 根据花色设置颜色（可选）
            Color suitColor = GetSuitColor(cardData.suit);
            suitImage.color = suitColor;
            // 如果图片已包含花色，可以隐藏
            suitImage.gameObject.SetActive(false);
        }
    }

    private Color GetSuitColor(CardData.Suit suit)
    {
        switch (suit)
        {
            case CardData.Suit.Spades:
            case CardData.Suit.Clubs:
                return Color.black;
            case CardData.Suit.Hearts:
            case CardData.Suit.Diamonds:
                return Color.red;
            default:
                return Color.white;
        }
    }

    public void OnCardClicked()
    {
        isSelected = !isSelected;

        // 向上移动表示选中
        if (isSelected)
        {
            transform.localPosition = originalPosition + Vector3.up * 20f;
        }
        else
        {
            transform.localPosition = originalPosition;
        }

        onCardClicked?.Invoke(cardData, isSelected);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (isSelected)
        {
            transform.localPosition = originalPosition + Vector3.up * 20f;
        }
        else
        {
            transform.localPosition = originalPosition;
        }
    }

    public CardData GetCardData()
    {
        return cardData;
    }
}

