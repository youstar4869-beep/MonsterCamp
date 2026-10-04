using System;
using UnityEngine;
using UnityEngine.UI;

// 보관함 아이템 정보 창: 그림, 이름, 설명, "판매 +NG" → 한 번 더 "정말 팔까요?"
// 창 바깥(어두운 바탕)이나 닫기 버튼을 누르면 닫힌다
public class ItemInfoPopup : MonoBehaviour
{
    [SerializeField] private ItemIconView _icon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _descriptionText;
    [SerializeField] private Text _messageText;

    [Header("버튼")]
    [Tooltip("판매 버튼 (오른쪽 아래)")]
    [SerializeField] private Button _sellButton;
    [SerializeField] private Text _sellLabel;
    [Tooltip("확인 단계의 실행 버튼 (왼쪽 아래)")]
    [SerializeField] private Button _confirmButton;
    [Tooltip("확인 단계의 취소 버튼. 판매 버튼과 같은 자리")]
    [SerializeField] private Button _cancelButton;

    private Action _onSell;
    private string _confirmQuestion;

    public void Show(ItemData item, int sellPrice, Action onSell)
    {
        _icon.Show(item);
        _nameText.text = item.DisplayName;
        _descriptionText.text = item.Description;
        _sellLabel.text = $"판매 +{sellPrice}G";
        _confirmQuestion = $"정말 팔까요? +{sellPrice}G";
        _onSell = onSell;
        OnCancelClicked();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // ---------- 버튼 ----------

    public void OnSellClicked()
    {
        _messageText.text = _confirmQuestion;
        _sellButton.gameObject.SetActive(false);
        _cancelButton.gameObject.SetActive(true);
        _confirmButton.gameObject.SetActive(true);
    }

    public void OnConfirmClicked()
    {
        _onSell?.Invoke();
    }

    public void OnCancelClicked()
    {
        _messageText.text = "";
        _confirmButton.gameObject.SetActive(false);
        _cancelButton.gameObject.SetActive(false);
        _sellButton.gameObject.SetActive(true);
    }
}
