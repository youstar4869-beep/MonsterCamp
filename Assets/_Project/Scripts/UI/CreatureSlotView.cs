using UnityEngine;
using UnityEngine.UI;

// 크리처 한 칸 (상점 칸, 대기석 칸, 팀 칸)
public class CreatureSlotView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _priceText;
    [Tooltip("비어 있을 때 보여줄 글자 (예: 앞줄)")]
    [SerializeField] private string _emptyLabel;

    public void Show(CreatureData creature, string priceLabel)
    {
        _icon.sprite = creature.Sprite;
        _icon.enabled = true;
        SetFaded(false);
        _nameText.text = creature.DisplayName;
        _priceText.text = priceLabel;
        _button.interactable = true;
    }

    public void Clear()
    {
        _icon.enabled = false;
        _nameText.text = _emptyLabel;
        _priceText.text = "";
        _button.interactable = false;
    }

    // 끌고 있는 동안 원래 칸의 그림을 흐리게
    public void SetFaded(bool faded)
    {
        _icon.color = new Color(1f, 1f, 1f, faded ? 0.35f : 1f);
    }
}
