using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Spawns enemy units on their side, signals all units to start fighting,
/// polls every 0.5 s for end condition, then reports result to GameManager.
/// </summary>
public class CombatManager : MonoBehaviour
{
    [Header("Enemy Setup")]
    [SerializeField] UnitData[] enemyPool;   // drag UnitData assets here in Inspector
    [SerializeField] int        enemiesPerRound = 3;

    List<UnitController> spawnedEnemies = new();

    // ── Public API ───────────────────────────────────────────────

    public void BeginCombat()
    {
        SpawnEnemies();
        StartCoroutine(CombatLoop());
    }

    public void CleanupAfterBattle()
    {
        StopAllCoroutines();

        // Destroy any remaining enemies
        foreach (var e in spawnedEnemies)
            if (e != null) Destroy(e.gameObject);
        spawnedEnemies.Clear();

        // Reset surviving player units
        foreach (var u in BoardManager.Instance.GetPlayerUnits())
            u.ResetAfterBattle();
    }

    // ── Enemy spawning ───────────────────────────────────────────

    void SpawnEnemies()
    {
        spawnedEnemies.Clear();
        var emptyCells = BoardManager.Instance.GetEnemyCells()
                             .Where(c => !c.IsOccupied)
                             .OrderBy(_ => Random.value)
                             .Take(enemiesPerRound)
                             .ToList();

        // Scale difficulty: enemies get stronger each round
        int round = GameManager.Instance.Round;
        int hpBonus  = (round - 1) * 20;
        int dmgBonus = (round - 1) * 5;

        foreach (var cell in emptyCells)
        {
            if (enemyPool.Length == 0) break;
            var data   = enemyPool[Random.Range(0, enemyPool.Length)];
            var unit   = SpawnUnit(data, 1, cell);
            // Apply scaling (simple direct modification via a wrapper)
            unit.ApplyScaling(hpBonus, dmgBonus);
            spawnedEnemies.Add(unit);
        }
    }

    UnitController SpawnUnit(UnitData data, int teamId, HexCell cell)
    {
        var prefab = data.prefab != null ? data.prefab : CreateFallbackPrimitive(data);
        var go     = Instantiate(prefab, cell.transform.position, Quaternion.identity);
        var unit   = go.GetComponent<UnitController>();
        if (unit == null) unit = go.AddComponent<UnitController>();
        unit.Init(data, teamId);
        cell.PlaceUnit(unit);
        return unit;
    }

    // If no prefab is assigned, spawn a coloured capsule so the game still runs
    GameObject CreateFallbackPrimitive(UnitData data)
    {
        var go  = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = data.unitName;
        // UnitController will tint the renderer
        return go;
    }

    // ── Combat loop ──────────────────────────────────────────────

    IEnumerator CombatLoop()
    {
        // Tell every unit to start fighting
        foreach (var u in BoardManager.Instance.GetPlayerUnits()) u.StartCombat();
        foreach (var u in BoardManager.Instance.GetEnemyUnits())  u.StartCombat();

        // Poll every half-second for end condition (cheap and simple)
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            bool playerAlive = BoardManager.Instance.GetPlayerUnits().Any(u => u.IsAlive);
            bool enemyAlive  = BoardManager.Instance.GetEnemyUnits().Any(u => u.IsAlive);

            if (!playerAlive || !enemyAlive)
            {
                // Stop all units
                foreach (var u in BoardManager.Instance.GetPlayerUnits()) u.StopCombat();
                foreach (var u in BoardManager.Instance.GetEnemyUnits())  u.StopCombat();

                int surviving = spawnedEnemies.Count(e => e != null && e.IsAlive);
                GameManager.Instance.OnBattleEnd(playerWon: !enemyAlive, survivingEnemies: surviving);
                yield break;
            }
        }
    }
}
