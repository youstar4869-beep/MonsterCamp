using UnityEngine;
using UnityEngine.UI;

// 상점에서 띄우는 짧은 도움말 창 (어두운 바탕 + 제목 + 설명 + 확인).
// 확인이나 창 바깥을 누르면 닫힌다
public class TipPopup : MonoBehaviour
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _messageText;

    public bool IsOpen => gameObject.activeSelf;

    public void Show(string title, string message)
    {
        _titleText.text = title;
        _messageText.text = message;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
