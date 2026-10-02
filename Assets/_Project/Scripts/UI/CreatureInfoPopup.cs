using System;
using UnityEngine;
using UnityEngine.UI;

// 크리처 정보 창 (어느 화면에서나 다시 쓸 수 있게 화면 로직과 분리).
// 그림, 이름, 타입·가격·레벨, 지금 레벨 능력치(시너지·아이템 제외), 능력 설명, 장착 아이템, 시너지 한 줄,
// 그리고 선택 버튼 하나(구매/판매 등). 확인이 필요한 버튼은 한 번 더 묻는다.
// 창 바깥(어두운 바탕)이나 닫기 버튼을 누르면 닫힌다.
public class CreatureInfoPopup : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _infoText;
    [SerializeField] private Text _statsText;
    [SerializeField] private Text _skillText;
    [SerializeField] private Text _itemText;
    [SerializeField] private Text _synergyText;
    [SerializeField] private Text _messageText;
    [Tooltip("타입·가격 줄을 타입 색으로")]
    [SerializeField] private UITheme _theme;

    [Header("버튼")]
    [SerializeField] private RectTransform _closeButton;
    [Tooltip("구매·판매 같은 선택 버튼 (오른쪽 아래)")]
    [SerializeField] private Button _actionButton;
    [SerializeField] private Text _actionLabel;
    [Tooltip("확인 단계의 실행 버튼. 선택 버튼과 다른 자리(왼쪽 아래)에 둔다")]
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Text _confirmLabel;
    [Tooltip("확인 단계의 취소 버튼. 선택 버튼과 같은 자리라서 연속으로 눌러도 실행되지 않는다")]
    [SerializeField] private Button _cancelButton;

    private Action _onAction;
    private string _confirmQuestion;

    public bool IsOpen => gameObject.activeSelf;
    public RectTransform ActionButtonRect => (RectTransform)_actionButton.transform;
    public RectTransform CloseButtonRect => _closeButton;

    // 창을 연다. 선택 버튼은 SetAction으로 따로 붙인다 (없으면 숨김)
    public void Show(CreatureData creature, int level, ItemData item, string synergyLine)
    {
        level = Mathf.Clamp(level, 1, CreatureData.MaxLevel);
        _icon.sprite = creature.GetSprite(level);
        _nameText.text = creature.DisplayName;
        _infoText.text = $"{TypeSynergy.GetTypeName(creature.Type)} · {creature.Tier}골드 · Lv{level} {CreatureInstance.ToStars(level)}";
        _infoText.color = _theme.TypeColor(creature.Type);
        _statsText.text = $"체력 {creature.GetMaxHealth(level)}   공격력 {creature.GetAttack(level)}   공격 속도 {creature.AttackSpeed:0.##}/초";
        _skillText.text = $"능력: {creature.SkillDescription}";
        _itemText.text = item != null ? $"장착: {item.DisplayName} — {item.Description}" : "장착한 아이템 없음";
        _synergyText.text = synergyLine;

        ClearAction();
        gameObject.SetActive(true);
    }

    // 선택 버튼. confirmQuestion이 있으면 누를 때 한 번 더 묻는다 (예: "정말 팔까요? +3G")
    public void SetAction(string label, bool interactable, Action onAction,
        string confirmQuestion = null, string confirmLabel = null)
    {
        _onAction = onAction;
        _confirmQuestion = confirmQuestion;
        _actionLabel.text = label;
        _confirmLabel.text = confirmLabel ?? label;
        _actionButton.interactable = interactable;
        _actionButton.gameObject.SetActive(true);
        _confirmButton.gameObject.SetActive(false);
        _cancelButton.gameObject.SetActive(false);
        _messageText.text = "";
    }

    public void ClearAction()
    {
        _onAction = null;
        _confirmQuestion = null;
        _actionButton.gameObject.SetActive(false);
        _confirmButton.gameObject.SetActive(false);
        _cancelButton.gameObject.SetActive(false);
        _messageText.text = "";
    }

    public void ShowMessage(string message)
    {
        _messageText.text = message;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // ---------- 버튼 ----------

    // 선택 버튼: 확인이 필요하면 확인 단계로, 아니면 바로 실행
    public void OnActionClicked()
    {
        if (string.IsNullOrEmpty(_confirmQuestion))
        {
            _onAction?.Invoke();
            return;
        }

        // 확인 단계: 선택 버튼 자리에는 "취소", 실행 버튼은 반대쪽에
        _messageText.text = _confirmQuestion;
        _actionButton.gameObject.SetActive(false);
        _cancelButton.gameObject.SetActive(true);
        _confirmButton.gameObject.SetActive(true);
    }

    public void OnConfirmClicked()
    {
        _onAction?.Invoke();
    }

    public void OnCancelClicked()
    {
        _messageText.text = "";
        _confirmButton.gameObject.SetActive(false);
        _cancelButton.gameObject.SetActive(false);
        _actionButton.gameObject.SetActive(true);
    }
}
