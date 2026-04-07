using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Central UI controller.
/// Wires up: gold label, HP label, round label, shop buttons, start-battle button,
/// placement hint text, and game-over panel.
///
/// Scene setup:
///   Canvas (Screen Space Overlay)
///   ├── TopBar
///   │   ├── GoldText   (TMP)
///   │   ├── HPText     (TMP)
///   │   └── RoundText  (TMP)
///   ├── ShopPanel
///   │   └── ShopSlot × 4  (Button with child NameText + CostText + Portrait Image)
///   ├── BottomBar
///   │   ├── StartBattleButton (Button)
///   │   └── RerollButton      (Button)
///   ├── PlacementHint  (TMP, hidden by default)
///   └── GameOverPanel  (hidden by default)
///       └── GameOverText (TMP)
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Top Bar")]
    [SerializeField] TMP_Text goldText;
    [SerializeField] TMP_Text hpText;
    [SerializeField] TMP_Text roundText;

    [Header("Shop")]
    [SerializeField] List<ShopSlotUI> shopSlots;   // 4 slot UI objects
    [SerializeField] GameObject       shopPanel;

    [Header("Bottom Bar")]
    [SerializeField] Button startBattleButton;
    [SerializeField] Button rerollButton;

    [Header("Placement Hint")]
    [SerializeField] TMP_Text placementHint;

    [Header("Game Over")]
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TMP_Text   gameOverText;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        startBattleButton?.onClick.AddListener(GameManager.Instance.StartBattle);
        rerollButton?.onClick.AddListener(OnRerollClicked);

        placementHint?.gameObject.SetActive(false);
        gameOverPanel?.SetActive(false);
    }

    // ── Refresh ──────────────────────────────────────────────────

    public void RefreshAll()
    {
        RefreshGold();
        RefreshHP();
        RefreshRound();
        RefreshShop();
    }

    public void RefreshGold()
    {
        if (goldText)  goldText.text  = $"Gold: {GameManager.Instance.Gold}";
    }

    void RefreshHP()
    {
        if (hpText)    hpText.text    = $"HP: {GameManager.Instance.PlayerHP}";
    }

    void RefreshRound()
    {
        if (roundText) roundText.text = $"Round {GameManager.Instance.Round}";
    }

    public void RefreshShop()
    {
        var shop = ShopManager.Instance?.CurrentShop;
        for (int i = 0; i < shopSlots.Count; i++)
        {
            bool hasItem = shop != null && i < shop.Count;
            shopSlots[i].gameObject.SetActive(hasItem);
            if (hasItem) shopSlots[i].Setup(shop[i], i);
        }
    }

    // ── Phase visibility ─────────────────────────────────────────

    public void ShowPrepUI(bool show)
    {
        shopPanel?.SetActive(show);
        rerollButton?.gameObject.SetActive(show);
        startBattleButton?.gameObject.SetActive(show);
    }

    public void ShowPlacementHint(bool show, string message = "")
    {
        if (placementHint == null) return;
        placementHint.gameObject.SetActive(show);
        placementHint.text = message;
    }

    public void ShowGameOver()
    {
        gameOverPanel?.SetActive(true);
        if (gameOverText) gameOverText.text = "GAME OVER\nYou ran out of HP!";
    }

    // ── Button callbacks ─────────────────────────────────────────

    void OnRerollClicked()
    {
        if (!GameManager.Instance.TrySpend(2)) return;
        ShopManager.Instance.RollShop();
    }
}
