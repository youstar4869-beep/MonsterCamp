using UnityEngine;
using UnityEngine.UI;

// 아이템 한 칸 (상점 아이템 칸, 아이템 보관함 칸)
public class ItemSlotView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private ItemIconView _icon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _priceText;
    [Tooltip("비어 있을 때 보여줄 글자 (예: 아이템)")]
    [SerializeField] private string _emptyLabel;

    public void Show(ItemData item, string priceLabel)
    {
        _icon.Show(item);
        _nameText.text = item.DisplayName;
        _priceText.text = priceLabel;
        _button.interactable = true;
    }

    public void Clear()
    {
        _icon.Show(null);
        _nameText.text = _emptyLabel;
        _priceText.text = "";
        _button.interactable = false;
    }
}
