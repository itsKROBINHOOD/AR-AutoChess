using UnityEngine;

/// <summary>
/// All static data for one unit type. Create via Assets > AutoBattler > Unit Data.
/// </summary>
[CreateAssetMenu(menuName = "AutoBattler/Unit Data", fileName = "NewUnit")]
public class UnitData : ScriptableObject
{
    [Header("Identity")]
    public string unitName  = "Unit";
    public int    cost      = 1;       // gold cost in shop
    public Sprite portrait;            // shown in shop button
    public Color  teamColor = Color.white; // tint on the 3D model

    [Header("Combat Stats")]
    public int   maxHp         = 100;
    public float attackDamage  = 20f;
    public float attackSpeed   = 1f;   // attacks per second
    public int   attackRange   = 1;    // in cells (1 = melee, 2+ = ranged)
    public float moveSpeed     = 2f;   // cells per second

    [Header("Prefab")]
    public GameObject prefab;          // the 3D character prefab
}
