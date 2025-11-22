using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 结算结果项UI
/// 位置：Assets/Scripts/UI/ResultItemUI.cs
/// 功能：显示单个玩家的结算结果
/// </summary>
public class ResultItemUI : MonoBehaviour
{
    [Header("显示组件")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Image winnerBadge;

    public void Initialize(string playerName, int score, bool isWinner)
    {
        if (playerNameText != null)
            playerNameText.text = playerName;

        if (scoreText != null)
            scoreText.text = $"分数: {score}";

        if (winnerBadge != null)
            winnerBadge.gameObject.SetActive(isWinner);
    }
}

