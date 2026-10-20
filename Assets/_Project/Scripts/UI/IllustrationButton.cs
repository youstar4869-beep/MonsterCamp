using UnityEngine;
using UnityEngine.UI;

// 도감의 "3성 일러스트" 버튼 (크리처 칸, 조합식 줄). 본 것은 누르면 일러스트, 못 본 것은 잠금 + "3성 달성 시 해금"
public class IllustrationButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Text _label;
    [SerializeField] private GameObject _lockIcon;

    // visible: 일러스트 파일이 있을 때만 보임, unlocked: 3성을 달성해 연출을 봤으면 true
    public void Set(bool visible, bool unlocked)
    {
        gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }
        _button.interactable = unlocked;
        _lockIcon.SetActive(!unlocked);
        _label.text = unlocked ? "3성 일러스트" : "3성 달성 시 해금";
    }
}
