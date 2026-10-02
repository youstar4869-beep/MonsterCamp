using UnityEngine;
using UnityEngine.UI;

// 트레이너 선택 카드 1장: 이름, 설명, 선택 표시, 잠금 표시
public class TrainerCardView : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _descriptionText;
    [Tooltip("잠겼을 때 덮는 자물쇠 표시 (처음엔 숨김)")]
    [SerializeField] private GameObject _lockOverlay;
    [SerializeField] private Text _lockText;
    [SerializeField] private Color _normalColor = new Color(0.25f, 0.28f, 0.34f);
    [SerializeField] private Color _selectedColor = new Color(0.85f, 0.62f, 0.2f);

    public void Show(TrainerData trainer)
    {
        _nameText.text = trainer.DisplayName;
        _descriptionText.text = trainer.Description;
    }

    public void SetSelected(bool selected)
    {
        _background.color = selected ? _selectedColor : _normalColor;
    }

    // cost: 해금에 필요한 별
    public void SetLocked(bool locked, int cost)
    {
        _lockOverlay.SetActive(locked);
        _lockText.text = $"★{cost} 필요\n도감에서 해금";
    }
}
