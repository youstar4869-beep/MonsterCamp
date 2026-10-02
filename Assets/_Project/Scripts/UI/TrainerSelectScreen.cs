using UnityEngine;
using UnityEngine.UI;

// 트레이너 선택 화면: 카드를 눌러 고르고 "선택 완료"로 새 런 시작
public class TrainerSelectScreen : MonoBehaviour
{
    [SerializeField] private RunManager _run;
    [SerializeField] private MetaProgress _meta;
    [Tooltip("카드 순서대로")]
    [SerializeField] private TrainerData[] _trainers;
    [SerializeField] private TrainerCardView[] _cards;
    [SerializeField] private Text _messageText;

    private int _selected = -1;

    // 화면에 들어올 때마다 선택을 비운다
    private void OnEnable()
    {
        _selected = -1;
        _messageText.text = "";
        for (int i = 0; i < _cards.Length; i++)
        {
            _cards[i].Show(_trainers[i]);
            _cards[i].SetSelected(false);
            _cards[i].SetLocked(!_meta.IsUnlocked(_trainers[i]), _meta.GetCost(_trainers[i]));
        }
    }

    // 카드 버튼. 잠긴 트레이너는 고를 수 없다
    public void Select(int index)
    {
        if (!_meta.IsUnlocked(_trainers[index]))
        {
            _messageText.text = "잠긴 트레이너예요. 타이틀의 도감에서 해금하세요";
            return;
        }

        _selected = index;
        _messageText.text = "";
        for (int i = 0; i < _cards.Length; i++)
        {
            _cards[i].SetSelected(i == index);
        }
    }

    // "선택 완료" 버튼
    public void Confirm()
    {
        if (_selected < 0)
        {
            _messageText.text = "트레이너를 골라 주세요";
            return;
        }
        _run.StartRun(_trainers[_selected]);
    }
}
