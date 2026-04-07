using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space canvas HP bar.
/// Attach this prefab as a child of the unit prefab, or add it at runtime.
/// The bar always faces the main camera (billboard).
///
/// Setup: create a World Space Canvas child → add a background Image and
/// a foreground Image named "Fill". Assign them in the Inspector.
/// </summary>
public class HPBarUI : MonoBehaviour
{
    [SerializeField] Image fillImage;          // the green/red fill
    [SerializeField] Vector3 offset = new Vector3(0, 1.4f, 0);

    UnitController unit;

    public void Init(UnitController u)
    {
        unit = u;
        Refresh();
    }

    public void Refresh()
    {
        if (unit == null || fillImage == null) return;
        float pct = (float)unit.CurrentHp / unit.MaxHp;
        fillImage.fillAmount = pct;
        fillImage.color      = Color.Lerp(Color.red, Color.green, pct);
    }

    void LateUpdate()
    {
        // Keep bar above the unit and facing camera
        transform.position = unit != null
            ? unit.transform.position + offset
            : transform.position;

        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0);
        }
    }
}
