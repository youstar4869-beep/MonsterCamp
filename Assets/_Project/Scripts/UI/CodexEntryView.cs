using UnityEngine;
using UnityEngine.UI;

// 도감 한 칸 (트레이너 또는 크리처): 그림, 이름, 설명, 잠금 표시, 해금 버튼
public class CodexEntryView : MonoBehaviour
{
    [Tooltip("트레이너 칸은 그림이 없어 비워 둔다")]
    [SerializeField] private Image _icon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _infoText;
    [Tooltip("잠겼을 때 덮는 자물쇠 표시")]
    [SerializeField] private GameObject _lockOverlay;
    [SerializeField] private Button _unlockButton;
    [SerializeField] private Text _unlockLabel;
    [Tooltip("크리처 칸만: 3성 일러스트 버튼 (해금 버튼 자리, 크리처가 열려 있을 때)과 그림 위 작은 ★")]
    [SerializeField] private IllustrationButton _illustration;
    [SerializeField] private GameObject _illustrationMark;

    public void Show(string displayName, Sprite sprite, string info)
    {
        _nameText.text = displayName;
        _infoText.text = info;
        if (_icon != null)
        {
            _icon.sprite = sprite;
        }
    }

    // cost: 필요한 별, affordable: 지금 별로 살 수 있는지
    public void SetLocked(bool locked, int cost, bool affordable)
    {
        _lockOverlay.SetActive(locked);
        _unlockButton.gameObject.SetActive(locked);
        _unlockButton.interactable = affordable;
        _unlockLabel.text = $"★{cost} 해금";
    }

    // hasArt: 일러스트 파일이 있음, seen: 3성 연출을 봄(해금), creatureLocked: 크리처가 아직 별로 안 열림
    public void SetIllustration(bool hasArt, bool seen, bool creatureLocked)
    {
        _illustration.Set(hasArt && !creatureLocked, seen);
        _illustrationMark.SetActive(hasArt && seen);
    }
}
