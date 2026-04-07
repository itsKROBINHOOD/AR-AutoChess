using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One slot in the shop panel.
/// Attach to a Button GameObject that has:
///   ├── PortraitImage  (Image component, optional)
///   ├── NameText       (TMP_Text)
///   └── CostText       (TMP_Text)
/// </summary>
public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] Image    portraitImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text costText;

    Button button;
    int    slotIndex;

    void Awake()
    {
        button = GetComponent<Button>();
        button?.onClick.AddListener(OnClicked);
    }

    public void Setup(UnitData data, int index)
    {
        slotIndex = index;

        if (nameText)    nameText.text    = data.unitName;
        if (costText)    costText.text    = $"{data.cost}G";
        if (portraitImage && data.portrait != null)
        {
            portraitImage.sprite  = data.portrait;
            portraitImage.enabled = true;
        }
        else if (portraitImage)
        {
            portraitImage.enabled = false;
        }

        // Grey out if can't afford
        bool canAfford = GameManager.Instance.Gold >= data.cost;
        if (button) button.interactable = canAfford;
        if (nameText) nameText.color = canAfford ? Color.white : Color.grey;
    }

    void OnClicked() => ShopManager.Instance?.TryBuy(slotIndex);
}
