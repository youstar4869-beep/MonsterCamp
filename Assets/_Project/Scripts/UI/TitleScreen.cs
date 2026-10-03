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
    [SerializeField] private AudioManager _audio;
    [Tooltip("효과음 켜기/끄기 버튼 글자")]
    [SerializeField] private Text _sfxLabel;
    [Tooltip("배경음 켜기/끄기 버튼 글자")]
    [SerializeField] private Text _bgmLabel;
    [Tooltip("효과음·배경음 음량 (0~1)")]
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _bgmSlider;

    private void OnEnable()
    {
        _confirmPanel.SetActive(false);
        _messageText.text = "";
        UpdateSfxLabel();
        UpdateBgmLabel();
        _sfxSlider.SetValueWithoutNotify(_audio.SfxVolume);
        _bgmSlider.SetValueWithoutNotify(_audio.BgmVolume);

        // 읽을 수 있는 저장 파일이 있을 때만 이어하기 (깨진 파일은 여기서 백업된다)
        bool canContinue = SaveSystem.TryLoadRun(out _, out bool wasBroken);
        _continueButton.SetActive(canContinue);
        if (wasBroken)
        {
            ShowBrokenMessage();
        }
    }

    // 음량 슬라이더로 바꾼 값을 타이틀을 떠날 때 저장
    private void OnDisable()
    {
        PlayerPrefs.Save();
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

    // "효과음 켜짐/꺼짐"
    public void ToggleSfx()
    {
        _audio.SetSfxEnabled(!_audio.SfxEnabled);
        AudioManager.Play(Sfx.Button);
        UpdateSfxLabel();
    }

    // "배경음 켜짐/꺼짐"
    public void ToggleBgm()
    {
        _audio.SetBgmEnabled(!_audio.BgmEnabled);
        UpdateBgmLabel();
    }

    // 효과음 음량 슬라이더
    public void SetSfxVolume(float volume)
    {
        _audio.SetSfxVolume(volume);
    }

    // 배경음 음량 슬라이더
    public void SetBgmVolume(float volume)
    {
        _audio.SetBgmVolume(volume);
    }

    private void UpdateBgmLabel()
    {
        _bgmLabel.text = _audio.BgmEnabled ? "배경음 켜짐" : "배경음 꺼짐";
    }

    private void UpdateSfxLabel()
    {
        _sfxLabel.text = _audio.SfxEnabled ? "효과음 켜짐" : "효과음 꺼짐";
    }

    private void ShowBrokenMessage()
    {
        _messageText.text = "저장 파일을 읽을 수 없어 백업해 두었어요.\n새 게임으로 시작해 주세요.";
    }
}
