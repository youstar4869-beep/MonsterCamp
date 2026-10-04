using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 조합 창: 지금 만들 수 있는 조합 목록 → 하나 고르면 확인 ("재료 A ★★ + 재료 B ★★ → 결과 ★★").
// 창 바깥이나 닫기·취소를 누르면 닫힌다
public class FusionPanel : MonoBehaviour
{
    [SerializeField] private GameObject _listView;
    [Tooltip("조합 목록 줄 (위에서부터)")]
    [SerializeField] private Button[] _optionButtons;
    [SerializeField] private Text[] _optionTexts;
    [SerializeField] private GameObject _confirmView;
    [SerializeField] private Text _confirmText;

    private Action<int> _onPick;
    private Action _onConfirm;

    public void ShowList(IReadOnlyList<string> options, Action<int> onPick)
    {
        _onPick = onPick;
        for (int i = 0; i < _optionButtons.Length; i++)
        {
            bool used = i < options.Count;
            _optionButtons[i].gameObject.SetActive(used);
            _optionTexts[i].text = used ? options[i] : "";
        }
        _listView.SetActive(true);
        _confirmView.SetActive(false);
        gameObject.SetActive(true);
    }

    public void ShowConfirm(string message, Action onConfirm)
    {
        _onConfirm = onConfirm;
        _confirmText.text = message;
        _listView.SetActive(false);
        _confirmView.SetActive(true);
        gameObject.SetActive(true);
    }

    // 목록 줄 버튼
    public void Pick(int index)
    {
        _onPick?.Invoke(index);
    }

    // 확인 창의 "조합" 버튼
    public void Confirm()
    {
        Action onConfirm = _onConfirm;
        Close();
        onConfirm?.Invoke();
    }

    public void Close()
    {
        _onPick = null;
        _onConfirm = null;
        gameObject.SetActive(false);
    }
}
