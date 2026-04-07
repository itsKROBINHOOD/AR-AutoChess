using UnityEngine;

/// <summary>
/// Central singleton. Drives the three-phase loop:
///   Prep  →  Battle  →  (repeat)
/// Everything else listens to phase-change events.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum Phase { Prep, Battle }
    public Phase CurrentPhase { get; private set; } = Phase.Prep;

    [Header("References")]
    [SerializeField] CombatManager combatManager;
    [SerializeField] ShopManager   shopManager;
    [SerializeField] UIManager     uiManager;

    [Header("Settings")]
    [SerializeField] int startingGold = 10;

    public int Gold { get; private set; }
    public int PlayerHP { get; private set; } = 20;
    public int Round    { get; private set; } = 0;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        Gold = startingGold;
        uiManager.RefreshAll();
        EnterPrep();
    }

    // ── Phase transitions ────────────────────────────────────────

    public void EnterPrep()
    {
        CurrentPhase = Phase.Prep;
        Round++;
        shopManager.RollShop();
        uiManager.ShowPrepUI(true);
        uiManager.RefreshAll();
        Debug.Log($"[GM] Prep phase — Round {Round}");
    }

    /// Called by the "Start Battle" button
    public void StartBattle()
    {
        if (CurrentPhase != Phase.Prep) return;
        if (BoardManager.Instance.GetPlayerUnits().Count == 0)
        {
            Debug.LogWarning("Place at least one unit before battling.");
            return;
        }
        CurrentPhase = Phase.Battle;
        uiManager.ShowPrepUI(false);
        combatManager.BeginCombat();
        Debug.Log($"[GM] Battle phase — Round {Round}");
    }

    /// Called by CombatManager when the battle resolves
    public void OnBattleEnd(bool playerWon, int survivingEnemies)
    {
        if (!playerWon)
        {
            PlayerHP -= survivingEnemies;
            Debug.Log($"[GM] Player lost — HP now {PlayerHP}");
        }
        else
        {
            Debug.Log("[GM] Player won the round!");
        }

        // Grant round gold
        AddGold(5);
        uiManager.RefreshAll();

        if (PlayerHP <= 0)
        {
            uiManager.ShowGameOver();
            return;
        }

        // Return surviving player units to bench, clear enemies
        combatManager.CleanupAfterBattle();
        EnterPrep();
    }

    // ── Economy ──────────────────────────────────────────────────

    public bool TrySpend(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        uiManager.RefreshGold();
        return true;
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        uiManager.RefreshGold();
    }
}
