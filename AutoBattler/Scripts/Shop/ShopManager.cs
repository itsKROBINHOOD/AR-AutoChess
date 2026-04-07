using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Maintains a pool of available UnitData and rolls a 4-slot shop each prep phase.
/// Purchased units go to the PlacementManager's pending slot.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Settings")]
    [SerializeField] UnitData[] unitPool;   // all available unit types
    [SerializeField] int        shopSlots = 4;

    public List<UnitData> CurrentShop { get; private set; } = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RollShop()
    {
        CurrentShop.Clear();
        for (int i = 0; i < shopSlots; i++)
        {
            if (unitPool.Length == 0) break;
            CurrentShop.Add(unitPool[Random.Range(0, unitPool.Length)]);
        }
        UIManager.Instance?.RefreshShop();
    }

    /// Returns true and deducts gold if the player can afford it.
    /// The bought unit is handed to PlacementManager to await placement.
    public bool TryBuy(int shopIndex)
    {
        if (shopIndex < 0 || shopIndex >= CurrentShop.Count) return false;

        UnitData unit = CurrentShop[shopIndex];
        if (!GameManager.Instance.TrySpend(unit.cost))
        {
            Debug.Log("Not enough gold.");
            return false;
        }

        // Remove from shop so it can't be bought twice
        CurrentShop.RemoveAt(shopIndex);
        UIManager.Instance?.RefreshShop();

        // Hand unit to placement system
        PlacementManager.Instance.SetPendingUnit(unit);
        return true;
    }
}
