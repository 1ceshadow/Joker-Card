using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

/// <summary>
/// 房间内玩家头像UI
/// 位置：Assets/Scripts/UI/PlayerAvatarInRoom.cs
/// 功能：显示房间内玩家的头像和名称
/// </summary>
public class PlayerAvatarInRoom : MonoBehaviour
{
    [Header("显示组件")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI playerNameText;

    private PlayerData playerData;

    public void SetPlayerData(PlayerData player)
    {
        this.playerData = player;

        if (playerData != null)
        {
            UpdateDisplay();

            // 订阅数据变化事件
            playerData.OnDataChanged += UpdateDisplay;
        }
    }

    private void UpdateDisplay()
    {
        if (playerData == null)
            return;

        if (playerNameText != null)
            playerNameText.text = playerData.playerName;

        if (avatarImage != null)
        {
            Sprite avatarSprite = AvatarSpriteLoader.GetAvatarSprite(playerData.avatarId);
            if (avatarSprite != null)
                avatarImage.sprite = avatarSprite;
        }
    }

    private void OnDestroy()
    {
        if (playerData != null)
            playerData.OnDataChanged -= UpdateDisplay;
    }
}

