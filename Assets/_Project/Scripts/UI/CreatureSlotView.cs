using UnityEngine;
using UnityEngine.UI;

// 크리처 한 칸 (상점 칸, 대기석 칸)
public class CreatureSlotView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _priceText;

    public void Show(CreatureData creature, string priceLabel)
    {
        _icon.sprite = creature.Sprite;
        _icon.enabled = true;
        _nameText.text = creature.DisplayName;
        _priceText.text = priceLabel;
        _button.interactable = true;
    }

    public void Clear()
    {
        _icon.enabled = false;
        _nameText.text = "";
        _priceText.text = "";
        _button.interactable = false;
    }
}
