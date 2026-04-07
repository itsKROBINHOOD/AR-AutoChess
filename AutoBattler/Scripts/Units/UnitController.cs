using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Lives on the unit GameObject at runtime.
/// Handles movement, targeting, and attacking during the Battle phase.
/// During Prep, just sits on its cell doing nothing.
/// </summary>
public class UnitController : MonoBehaviour
{
    // ── Data ─────────────────────────────────────────────────────
    public UnitData Data   { get; private set; }
    public int      TeamId { get; private set; }   // 0 = player, 1 = enemy
    public HexCell  CurrentCell { get; set; }

    // ── Runtime stats ────────────────────────────────────────────
    public int   CurrentHp     { get; private set; }
    public int   MaxHp         { get; private set; }
    public bool  IsAlive       => CurrentHp > 0;

    // ── Combat state ─────────────────────────────────────────────
    UnitController target;
    float          attackCooldown;
    bool           combatActive;

    // ── Components ───────────────────────────────────────────────
    Renderer[]    renderers;
    HPBarUI       hpBar;

    // ─────────────────────────────────────────────────────────────

    public void Init(UnitData data, int teamId)
    {
        Data      = data;
        TeamId    = teamId;
        MaxHp     = data.maxHp;
        CurrentHp = MaxHp;

        renderers = GetComponentsInChildren<Renderer>();
        ApplyTeamColor();

        hpBar = GetComponentInChildren<HPBarUI>();
        hpBar?.Init(this);
    }

    void ApplyTeamColor()
    {
        Color c = TeamId == 0 ? Data.teamColor : Color.red;
        foreach (var r in renderers)
            r.material.color = c;
    }

    // ── Combat ───────────────────────────────────────────────────

    public void StartCombat() => combatActive = true;
    public void StopCombat()  => combatActive = false;

    void Update()
    {
        if (!combatActive || !IsAlive) return;

        attackCooldown -= Time.deltaTime;

        // Find target
        target = FindTarget();

        if (target == null) return;

        float dist = CellDistance(CurrentCell, target.CurrentCell);

        if (dist <= Data.attackRange)
        {
            // In range — attack
            FaceTarget(target.transform.position);
            if (attackCooldown <= 0f)
            {
                DoAttack();
                attackCooldown = 1f / Data.attackSpeed;
            }
        }
        else
        {
            // Move toward target
            MoveToward(target.CurrentCell);
        }
    }

    UnitController FindTarget()
    {
        // Nearest living enemy
        List<UnitController> enemies = TeamId == 0
            ? BoardManager.Instance.GetEnemyUnits()
            : BoardManager.Instance.GetPlayerUnits();

        UnitController best = null;
        float          bestDist = float.MaxValue;
        foreach (var e in enemies)
        {
            if (!e.IsAlive) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < bestDist) { bestDist = d; best = e; }
        }
        return best;
    }

    void DoAttack()
    {
        if (target == null || !target.IsAlive) return;
        target.TakeDamage(Mathf.RoundToInt(Data.attackDamage + bonusDamage));
        // Simple visual flash on attacker
        StartCoroutine(AttackFlash());
    }

    IEnumerator AttackFlash()
    {
        foreach (var r in renderers) r.material.color = Color.yellow;
        yield return new WaitForSeconds(0.1f);
        ApplyTeamColor();
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        CurrentHp -= amount;
        CurrentHp  = Mathf.Max(0, CurrentHp);
        hpBar?.Refresh();
        if (CurrentHp <= 0) Die();
    }

    void Die()
    {
        combatActive = false;
        CurrentCell?.RemoveUnit();
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Simple shrink-and-disappear
        float t = 0f;
        Vector3 startScale = transform.localScale;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / 0.4f);
            yield return null;
        }
        Destroy(gameObject);
    }

    // ── Movement ─────────────────────────────────────────────────

    void MoveToward(HexCell targetCell)
    {
        if (targetCell == null) return;

        // Find the adjacent cell closest to the target that is free
        HexCell step = GetBestStep(targetCell);
        if (step == null || step.IsOccupied) return;

        // Move unit physically
        float speed = Data.moveSpeed * Time.deltaTime;
        Vector3 dest = step.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, dest, speed);
        FaceTarget(dest);

        // Snap cell assignment when we arrive
        if (Vector3.Distance(transform.position, dest) < 0.05f)
        {
            CurrentCell?.RemoveUnit();
            step.PlaceUnit(this);
        }
    }

    HexCell GetBestStep(HexCell goal)
    {
        // Try all 6 neighbours; pick the open one closest to goal
        Vector2Int[] dirs = {
            new(1,0), new(-1,0), new(0,1), new(0,-1),
            new(1,1), new(-1,-1)
        };

        HexCell best    = null;
        float   bestD   = float.MaxValue;
        foreach (var d in dirs)
        {
            var nb = BoardManager.Instance.GetCell(CurrentCell.Coord + d);
            if (nb == null || nb.IsOccupied) continue;
            float dist = Vector3.Distance(nb.transform.position, goal.transform.position);
            if (dist < bestD) { bestD = dist; best = nb; }
        }
        return best;
    }

    void FaceTarget(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 10f);
    }

    float CellDistance(HexCell a, HexCell b)
    {
        if (a == null || b == null) return float.MaxValue;
        return Vector2Int.Distance(a.Coord, b.Coord);
    }

    // ── Scaling (applied by CombatManager each round) ────────────

    int bonusDamage;

    public void ApplyScaling(int extraHp, int extraDamage)
    {
        MaxHp       += extraHp;
        CurrentHp    = MaxHp;
        bonusDamage  = extraDamage;
        hpBar?.Refresh();
    }

    // ── Helpers for post-battle cleanup ──────────────────────────

    public void ResetAfterBattle()
    {
        CurrentHp    = MaxHp;
        combatActive = false;
        attackCooldown = 0;
        hpBar?.Refresh();
        ApplyTeamColor();
        transform.localScale = Vector3.one;
    }
}
