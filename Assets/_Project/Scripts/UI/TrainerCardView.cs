using UnityEngine;
using UnityEngine.UI;

// 트레이너·난이도 선택 카드 1장: 이름, 설명, 선택 표시, 잠금 표시
public class TrainerCardView : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _descriptionText;
    [Tooltip("잠겼을 때 덮는 자물쇠 표시 (처음엔 숨김)")]
    [SerializeField] private GameObject _lockOverlay;
    [SerializeField] private Text _lockText;
    [Tooltip("평소 = 칸 색, 고르면 = 칸 색과 주요 버튼 색 사이")]
    [SerializeField] private UITheme _theme;

    public void Show(TrainerData trainer)
    {
        Show(trainer.DisplayName, trainer.Description);
    }

    public void Show(string displayName, string description)
    {
        _nameText.text = displayName;
        _descriptionText.text = description;
    }

    public void SetSelected(bool selected)
    {
        _background.color = selected ? Color.Lerp(_theme.Slot, _theme.ButtonPrimary, 0.75f) : _theme.Slot;
    }

    // cost: 해금에 필요한 별
    public void SetLocked(bool locked, int cost)
    {
        SetLocked(locked, $"★{cost} 필요\n도감에서 해금");
    }

    public void SetLocked(bool locked, string message)
    {
        _lockOverlay.SetActive(locked);
        _lockText.text = message;
    }
}
