using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates a flat hex grid and converts between axial coords and world positions.
/// Board is split: rows 0-2 = player side, rows 3-4 = enemy side.
/// </summary>
public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [Header("Board Layout")]
    [SerializeField] int   columns    = 7;
    [SerializeField] int   rows       = 5;   // 0-2 player, 3-4 enemy
    [SerializeField] float cellSize   = 1.1f;
    [SerializeField] int   playerRows = 3;   // rows available to player

    [Header("Prefabs")]
    [SerializeField] GameObject cellPrefab;

    // q = column, r = row
    Dictionary<Vector2Int, HexCell> cells = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        GenerateGrid();
    }

    void GenerateGrid()
    {
        for (int r = 0; r < rows; r++)
        for (int q = 0; q < columns; q++)
        {
            var coord   = new Vector2Int(q, r);
            var worldPos = CoordToWorld(coord);
            var go      = Instantiate(cellPrefab, worldPos, Quaternion.identity, transform);
            go.name     = $"Cell_{q}_{r}";
            var cell    = go.GetComponent<HexCell>();
            bool isPlayerSide = r < playerRows;
            cell.Init(coord, isPlayerSide);
            cells[coord] = cell;
        }
    }

    // Offset hex layout (pointy-top)
    public Vector3 CoordToWorld(Vector2Int coord)
    {
        float x = cellSize * coord.x + (coord.y % 2 == 1 ? cellSize * 0.5f : 0f);
        float z = cellSize * coord.y * 0.866f;   // sqrt(3)/2
        return transform.position + new Vector3(x, 0, z);
    }

    public HexCell GetCell(Vector2Int coord)
        => cells.TryGetValue(coord, out var c) ? c : null;

    public HexCell GetCell(int q, int r) => GetCell(new Vector2Int(q, r));

    /// Returns all cells on the player's side that are empty
    public List<HexCell> GetOpenPlayerCells()
    {
        var list = new List<HexCell>();
        foreach (var c in cells.Values)
            if (c.IsPlayerSide && !c.IsOccupied) list.Add(c);
        return list;
    }

    /// Returns all cells on the enemy side
    public List<HexCell> GetEnemyCells()
    {
        var list = new List<HexCell>();
        foreach (var c in cells.Values)
            if (!c.IsPlayerSide) list.Add(c);
        return list;
    }

    public List<UnitController> GetPlayerUnits()
    {
        var list = new List<UnitController>();
        foreach (var c in cells.Values)
            if (c.IsPlayerSide && c.IsOccupied) list.Add(c.OccupiedUnit);
        return list;
    }

    public List<UnitController> GetEnemyUnits()
    {
        var list = new List<UnitController>();
        foreach (var c in cells.Values)
            if (!c.IsPlayerSide && c.IsOccupied) list.Add(c.OccupiedUnit);
        return list;
    }

    /// Find cell closest to a world position (for drag-and-drop snapping)
    public HexCell GetClosestPlayerCell(Vector3 worldPos)
    {
        HexCell best     = null;
        float   bestDist = float.MaxValue;
        foreach (var c in cells.Values)
        {
            if (!c.IsPlayerSide) continue;
            float d = Vector3.Distance(worldPos, c.transform.position);
            if (d < bestDist) { bestDist = d; best = c; }
        }
        return best;
    }
}
