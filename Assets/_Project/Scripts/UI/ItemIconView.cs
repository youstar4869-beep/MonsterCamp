using UnityEngine;
using UnityEngine.UI;

// 아이템 아이콘: 그림이 있으면 그림, 없으면 색 네모 + 한 글자. null이면 숨긴다
public class ItemIconView : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private Text _label;
    [Tooltip("아이템 그림 (색 네모 위에 겹쳐 둔다)")]
    [SerializeField] private Image _picture;

    public void Show(ItemData item)
    {
        if (item == null)
        {
            gameObject.SetActive(false);
            return;
        }

        bool hasPicture = item.Icon != null && _picture != null;
        _background.enabled = !hasPicture;
        _label.enabled = !hasPicture;
        _background.color = item.IconColor;
        _label.text = item.IconLabel;
        if (_picture != null)
        {
            _picture.sprite = item.Icon;
            _picture.enabled = hasPicture;
        }
        gameObject.SetActive(true);
    }
}
