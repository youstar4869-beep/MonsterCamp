using UnityEngine;
using UnityEngine.UI;

// 타이틀: 진행 중인 판이 있으면 "이어하기", "새 게임"은 기존 판을 지우기 전에 한 번 묻는다.
// "튜토리얼 다시 보기"는 튜토리얼이 나오는 새 게임
public class TitleScreen : MonoBehaviour
{
    [SerializeField] private ScreenFlow _flow;
    [SerializeField] private RunManager _run;
    [SerializeField] private TutorialController _tutorial;
    [SerializeField] private GameObject _continueButton;
    [Tooltip("새 게임 확인 창 (처음엔 숨김)")]
    [SerializeField] private GameObject _confirmPanel;
    [SerializeField] private Text _messageText;

    private void OnEnable()
    {
        _confirmPanel.SetActive(false);
        _messageText.text = "";

        // 읽을 수 있는 저장 파일이 있을 때만 이어하기 (깨진 파일은 여기서 백업된다)
        bool canContinue = SaveSystem.TryLoadRun(out _, out bool wasBroken);
        _continueButton.SetActive(canContinue);
        if (wasBroken)
        {
            ShowBrokenMessage();
        }
    }

    // "이어하기"
    public void Continue()
    {
        if (!_run.ContinueRun())
        {
            _continueButton.SetActive(false);
            ShowBrokenMessage();
        }
    }

    // "새 게임"
    public void NewGame()
    {
        _tutorial.CancelReplay();
        OpenNewGame();
    }

    // "튜토리얼 다시 보기"
    public void ReplayTutorial()
    {
        _tutorial.RequestReplay();
        OpenNewGame();
    }

    private void OpenNewGame()
    {
        if (SaveSystem.HasRun)
        {
            _confirmPanel.SetActive(true);
            return;
        }
        _flow.Show(GameScreen.TrainerSelect);
    }

    // 확인 창 "새로 시작"
    public void ConfirmNewGame()
    {
        SaveSystem.DeleteRun();
        _confirmPanel.SetActive(false);
        _flow.Show(GameScreen.TrainerSelect);
    }

    // 확인 창 "취소"
    public void CancelNewGame()
    {
        _tutorial.CancelReplay();
        _confirmPanel.SetActive(false);
    }

    private void ShowBrokenMessage()
    {
        _messageText.text = "저장 파일을 읽을 수 없어 백업해 두었어요.\n새 게임으로 시작해 주세요.";
    }
}
