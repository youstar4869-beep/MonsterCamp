using UnityEngine;
using UnityEngine.UI;

// 난이도 선택 화면 (트레이너 선택 다음): 카드를 눌러 고르고 "시작"으로 새 런 시작.
// 잠긴 난이도는 자물쇠와 열리는 조건 표시. 들어올 때 보통이 골라져 있다
public class DifficultySelectScreen : MonoBehaviour
{
    [SerializeField] private RunManager _run;
    [SerializeField] private ScreenFlow _flow;
    [SerializeField] private MetaProgress _meta;
    [Tooltip("카드 순서대로")]
    [SerializeField] private DifficultyData[] _difficulties;
    [SerializeField] private TrainerCardView[] _cards;
    [SerializeField] private Text _messageText;

    private TrainerData _trainer;
    private int _selected;

    // 트레이너 선택 화면의 "선택 완료"
    public void Open(TrainerData trainer)
    {
        _trainer = trainer;
        _flow.Show(GameScreen.DifficultySelect);
    }

    private void OnEnable()
    {
        _messageText.text = "";
        for (int i = 0; i < _cards.Length; i++)
        {
            DifficultyData difficulty = _difficulties[i];
            _cards[i].Show(difficulty.DisplayName, $"{difficulty.FloorCount}층\n{difficulty.Description}");
            bool locked = !_meta.IsUnlocked(difficulty);
            _cards[i].SetLocked(locked, locked ? $"{difficulty.RequiredClear.DisplayName}\n클리어 시 열림" : "");
        }
        Select(0);
    }

    // 카드 버튼. 잠긴 난이도는 고를 수 없다
    public void Select(int index)
    {
        DifficultyData difficulty = _difficulties[index];
        if (!_meta.IsUnlocked(difficulty))
        {
            _messageText.text = $"{difficulty.RequiredClear.DisplayName} 난이도를 클리어하면 열려요";
            return;
        }

        _selected = index;
        _messageText.text = "";
        for (int i = 0; i < _cards.Length; i++)
        {
            _cards[i].SetSelected(i == index);
        }
    }

    // "시작" 버튼
    public void Confirm()
    {
        _run.StartRun(_trainer, _difficulties[_selected]);
    }
}
