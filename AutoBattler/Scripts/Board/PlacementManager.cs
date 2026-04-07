using UnityEngine;

/// <summary>
/// After buying a unit from the shop, the player clicks any open player-side cell
/// to place it. This manager tracks the "pending" unit and handles that click.
/// 
/// Also handles drag-to-swap: click an occupied cell during Prep to pick it up,
/// then click another cell to move it there.
/// </summary>
public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    UnitData       pendingUnitData;    // unit waiting to be placed
    UnitController heldUnit;          // unit being repositioned
    HexCell        heldFromCell;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Called by ShopManager after purchase ─────────────────────

    public void SetPendingUnit(UnitData data)
    {
        pendingUnitData = data;
        heldUnit        = null;
        UIManager.Instance?.ShowPlacementHint(true, $"Click a blue cell to place {data.unitName}");
    }

    // ── Called by HexCell.OnMouseDown ────────────────────────────

    public void OnCellClicked(HexCell cell)
    {
        if (GameManager.Instance.CurrentPhase != GameManager.Phase.Prep) return;

        // Case 1: we have a freshly bought unit to place
        if (pendingUnitData != null)
        {
            if (!cell.IsPlayerSide)
            {
                Debug.Log("Place units on the blue side only.");
                return;
            }
            if (cell.IsOccupied)
            {
                Debug.Log("Cell is occupied — pick another.");
                return;
            }
            PlaceNewUnit(pendingUnitData, cell);
            pendingUnitData = null;
            UIManager.Instance?.ShowPlacementHint(false);
            return;
        }

        // Case 2: picking up an existing unit to move it
        if (heldUnit == null)
        {
            if (!cell.IsOccupied || !cell.IsPlayerSide) return;
            heldUnit     = cell.OccupiedBy;
            heldFromCell = cell;
            UIManager.Instance?.ShowPlacementHint(true, $"Click a cell to move {heldUnit.Data.unitName}");
            return;
        }

        // Case 3: placing the held unit on a new cell
        if (cell.IsPlayerSide)
        {
            if (cell.IsOccupied && cell != heldFromCell)
            {
                // Swap
                UnitController other = cell.OccupiedBy;
                heldFromCell.RemoveUnit();
                cell.RemoveUnit();
                cell.PlaceUnit(heldUnit);
                heldFromCell.PlaceUnit(other);
            }
            else if (!cell.IsOccupied)
            {
                heldFromCell.RemoveUnit();
                cell.PlaceUnit(heldUnit);
            }
        }

        heldUnit     = null;
        heldFromCell = null;
        UIManager.Instance?.ShowPlacementHint(false);
    }

    // ── Spawning ─────────────────────────────────────────────────

    void PlaceNewUnit(UnitData data, HexCell cell)
    {
        var prefab = data.prefab != null ? data.prefab : CreateCapsule(data.unitName);
        var go     = Instantiate(prefab, cell.transform.position, Quaternion.identity);
        var unit   = go.GetComponent<UnitController>();
        if (unit == null) unit = go.AddComponent<UnitController>();
        unit.Init(data, teamId: 0);
        cell.PlaceUnit(unit);
    }

    GameObject CreateCapsule(string unitName)
    {
        var go  = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = unitName;
        return go;
    }
}
