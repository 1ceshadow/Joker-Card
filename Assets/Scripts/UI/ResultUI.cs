using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 结算UI
/// 位置：Assets/Scripts/UI/ResultUI.cs
/// 功能：显示游戏结算结果，返回主菜单
/// </summary>
public class ResultUI : MonoBehaviour
{
    [Header("结算面板")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Transform resultListParent;
    [SerializeField] private GameObject resultItemPrefab;
    [SerializeField] private Button returnToMenuButton;
    [SerializeField] private TextMeshProUGUI potText;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI messageText;

    private void Start()
    {
        if (returnToMenuButton != null)
            returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    public void ShowResults(uint[] winnerNetIds, int[] scores, int finalPot)
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        // 清除现有结果
        foreach (Transform child in resultListParent)
        {
            Destroy(child.gameObject);
        }

        // 获取所有玩家
        PlayerData[] allPlayers = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
        Dictionary<uint, PlayerData> playerDict = allPlayers.ToDictionary(p => p.netId, p => p);
        Dictionary<uint, int> scoreDict = new Dictionary<uint, int>();
        
        // 创建分数字典
        for (int i = 0; i < allPlayers.Length && i < scores.Length; i++)
        {
            scoreDict[allPlayers[i].netId] = scores[i];
        }

        // 显示结果（按分数排序）
        List<PlayerData> sortedPlayers = allPlayers.OrderByDescending(p => scoreDict.ContainsKey(p.netId) ? scoreDict[p.netId] : 0).ToList();

        if (resultItemPrefab != null)
        {
            foreach (PlayerData player in sortedPlayers)
            {
                int score = scoreDict.ContainsKey(player.netId) ? scoreDict[player.netId] : 0;
                bool isWinner = winnerNetIds.Contains(player.netId);

                GameObject itemObj = Instantiate(resultItemPrefab, resultListParent);
                ResultItemUI itemUI = itemObj.GetComponent<ResultItemUI>();
                if (itemUI != null)
                {
                    itemUI.Initialize(player.playerName, score, isWinner);
                }
            }
        }

        // 显示底池
        if (potText != null)
            potText.text = $"底池: {finalPot}";

        // 显示获胜者
        if (winnerText != null)
        {
            List<string> winnerNames = new List<string>();
            foreach (uint winnerId in winnerNetIds)
            {
                if (playerDict.ContainsKey(winnerId))
                {
                    winnerNames.Add(playerDict[winnerId].playerName);
                }
            }
            if (winnerNames.Count > 0)
            {
                winnerText.text = $"获胜者: {string.Join(", ", winnerNames)}";
            }
        }
    }

    public void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
            // 不清除：保留消息，UI 可以自行控制隐藏
            if (resultPanel != null && !resultPanel.activeSelf)
                resultPanel.SetActive(true);
        }
        else
        {
            Debug.Log(message);
        }
    }

    private void OnReturnToMenuClicked()
    {
        // 断开连接
        if (NetworkManagerCustom.Instance != null)
        {
            NetworkManagerCustom.Instance.LeaveRoom();
        }

        // 返回主菜单
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}

