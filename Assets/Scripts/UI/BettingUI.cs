using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

/// <summary>
/// 押注UI
/// 位置：Assets/Scripts/UI/BettingUI.cs
/// 功能：显示押注窗口，输入押注金额
/// </summary>
public class BettingUI : MonoBehaviour
{
    [Header("押注窗口")]
    [SerializeField] private GameObject bettingWindow;
    [SerializeField] private TMP_InputField amountInputField;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI maxAmountText;

    private System.Action<int> onBetConfirmed;
    private PlayerData currentPlayer;

    public static BettingUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
        if (amountInputField != null)
            amountInputField.onValueChanged.AddListener(OnAmountChanged);

        if (bettingWindow != null)
            bettingWindow.SetActive(false);
    }

    public void ShowBettingWindow(PlayerData player, System.Action<int> callback)
    {
        currentPlayer = player;
        onBetConfirmed = callback;

        if (bettingWindow != null)
            bettingWindow.SetActive(true);

        if (maxAmountText != null && player != null)
        {
            maxAmountText.text = $"最大金额: {player.money} (可欠债)";
        }

        if (amountInputField != null)
        {
            amountInputField.text = "";
        }
    }

    private void OnAmountChanged(string value)
    {
        UpdateConfirmButton();
    }

    private void UpdateConfirmButton()
    {
        if (confirmButton == null || amountInputField == null)
            return;

        int amount = 0;
        if (int.TryParse(amountInputField.text, out amount))
        {
            confirmButton.interactable = amount > 0;
        }
        else
        {
            confirmButton.interactable = false;
        }
    }

    private void OnConfirmClicked()
    {
        if (amountInputField == null)
            return;

        int amount = 0;
        if (int.TryParse(amountInputField.text, out amount) && amount > 0)
        {
            onBetConfirmed?.Invoke(amount);
            HideBettingWindow();
        }
    }

    private void OnCancelClicked()
    {
        HideBettingWindow();
    }

    private void HideBettingWindow()
    {
        if (bettingWindow != null)
            bettingWindow.SetActive(false);
        currentPlayer = null;
        onBetConfirmed = null;
    }
}

