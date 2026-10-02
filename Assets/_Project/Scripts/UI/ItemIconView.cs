using UnityEngine;
using UnityEngine.UI;

// 아이템 아이콘 (임시: 색 네모 + 한 글자). null이면 숨긴다
public class ItemIconView : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private Text _label;

    public void Show(ItemData item)
    {
        if (item == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _background.color = item.IconColor;
        _label.text = item.IconLabel;
        gameObject.SetActive(true);
    }
}
