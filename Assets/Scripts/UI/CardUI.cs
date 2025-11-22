using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 卡牌UI组件
/// 位置：Assets/Scripts/UI/CardUI.cs
/// 功能：显示单张卡牌，处理点击选择
/// </summary>
public class CardUI : MonoBehaviour
{
    [Header("卡牌显示")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private Image suitImage;

    private Card card;
    private bool isSelected = false;
    private System.Action<Card, bool> onCardClicked;
    private Vector3 originalPosition;

    private void Start()
    {
        originalPosition = transform.localPosition;
    }

    public void Initialize(Card card, System.Action<Card, bool> onClickCallback)
    {
        this.card = card;
        this.onCardClicked = onClickCallback;

        UpdateCardDisplay();
    }

    private void UpdateCardDisplay()
    {
        if (card == null)
            return;

        // 加载并显示卡牌图片
        if (cardImage != null)
        {
            Sprite cardSprite = CardSpriteLoader.GetCardSprite(card.suit, card.rank);
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
            // 如果图片已包含点数，可以隐藏文本
            // rankText.text = card.GetRankName();
            rankText.gameObject.SetActive(false); // 默认隐藏，如果图片已包含点数
        }

        // 显示花色（可以用颜色或图标）
        if (suitImage != null)
        {
            // 根据花色设置颜色（可选）
            Color suitColor = GetSuitColor(card.suit);
            suitImage.color = suitColor;
            // 如果图片已包含花色，可以隐藏
            suitImage.gameObject.SetActive(false);
        }
    }

    private Color GetSuitColor(Card.Suit suit)
    {
        switch (suit)
        {
            case Card.Suit.Spades:
            case Card.Suit.Clubs:
                return Color.black;
            case Card.Suit.Hearts:
            case Card.Suit.Diamonds:
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

        onCardClicked?.Invoke(card, isSelected);
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
}

