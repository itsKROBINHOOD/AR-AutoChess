using UnityEngine;

/// <summary>
/// One cell on the board. Knows its coord, whether it's on the player side,
/// and which unit (if any) is standing on it.
/// </summary>
public class HexCell : MonoBehaviour
{
    public Vector2Int    Coord        { get; private set; }
    public bool          IsPlayerSide { get; private set; }
    public UnitController OccupiedUnit { get; private set; }
    public bool          IsOccupied   => OccupiedUnit != null;

    // Visual feedback materials
    [SerializeField] Renderer cellRenderer;
    [SerializeField] Material matDefault;
    [SerializeField] Material matPlayerSide;
    [SerializeField] Material matHover;
    [SerializeField] Material matOccupied;

    public void Init(Vector2Int coord, bool isPlayerSide)
    {
        Coord        = coord;
        IsPlayerSide = isPlayerSide;
        RefreshVisual();
    }

    public void PlaceUnit(UnitController unit)
    {
        OccupiedUnit = unit;
        unit.transform.position = transform.position;
        unit.CurrentCell = this;
        RefreshVisual();
    }

    public void RemoveUnit()
    {
        OccupiedUnit = null;
        RefreshVisual();
    }

    public void SetHover(bool on)
    {
        if (cellRenderer == null) return;
        cellRenderer.material = on ? matHover : (IsOccupied ? matOccupied
                                                : IsPlayerSide ? matPlayerSide
                                                : matDefault);
    }

    void RefreshVisual()
    {
        if (cellRenderer == null) return;
        cellRenderer.material = IsOccupied   ? matOccupied
                              : IsPlayerSide ? matPlayerSide
                              : matDefault;
    }

    // Allow clicking the cell directly (for simple mouse/touch placement)
    void OnMouseDown()
    {
        PlacementManager.Instance?.OnCellClicked(this);
    }
}
