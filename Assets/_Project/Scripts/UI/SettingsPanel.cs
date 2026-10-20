using UnityEngine;
using UnityEngine.UI;

// 설정 창 (모든 화면 오른쪽 위 ⚙): 효과음·배경음 켜기/끄기와 음량, 런 중이면 "타이틀로 나가기".
// 전투 중에 열면 Time.timeScale = 0으로 멈추고, 닫으면 원래대로 (배경음·UI는 timeScale과 상관없이 동작)
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private ScreenFlow _flow;
    [SerializeField] private RunManager _run;
    [SerializeField] private AudioManager _audio;
    [SerializeField] private MetaProgress _meta;
    [Tooltip("3성 일러스트 연출 켜기/끄기 버튼 글자")]
    [SerializeField] private Text _lv3Label;
    [Tooltip("\"다시 보지 않기 초기화\" 버튼 (체크한 크리처가 없으면 흐리게)")]
    [SerializeField] private Button _lv3ResetButton;
    [SerializeField] private Text _lv3ResetLabel;
    [Tooltip("효과음 켜기/끄기 버튼 글자")]
    [SerializeField] private Text _sfxLabel;
    [Tooltip("배경음 켜기/끄기 버튼 글자")]
    [SerializeField] private Text _bgmLabel;
    [Tooltip("효과음·배경음 음량 (0~1)")]
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _bgmSlider;
    [Tooltip("타이틀로 나가기 버튼 (상점·전투 화면에서만 보임)")]
    [SerializeField] private GameObject _exitButton;
    [Tooltip("나가기 확인 창 (처음엔 숨김)")]
    [SerializeField] private GameObject _confirmPanel;

    private bool _paused;
    private float _pausedTimeScale;

    // ⚙ 버튼
    public void Open()
    {
        GameScreen screen = _flow.Current;
        _exitButton.SetActive(screen == GameScreen.Shop || screen == GameScreen.Battle);
        _confirmPanel.SetActive(false);
        UpdateSfxLabel();
        UpdateBgmLabel();
        UpdateLv3Labels();
        _sfxSlider.SetValueWithoutNotify(_audio.SfxVolume);
        _bgmSlider.SetValueWithoutNotify(_audio.BgmVolume);

        if (screen == GameScreen.Battle && !_paused)
        {
            _paused = true;
            _pausedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        gameObject.SetActive(true);
    }

    // "닫기"와 창 바깥
    public void Close()
    {
        gameObject.SetActive(false);
    }

    // 음량 슬라이더로 바꾼 값을 닫을 때 저장하고, 멈췄던 전투를 다시 움직인다
    private void OnDisable()
    {
        PlayerPrefs.Save();
        if (_paused)
        {
            _paused = false;
            Time.timeScale = _pausedTimeScale;
        }
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

    // "3성 일러스트 연출 켜짐/꺼짐"
    public void ToggleLv3Showcase()
    {
        _meta.SetLv3ShowcaseEnabled(!_meta.Lv3ShowcaseEnabled);
        UpdateLv3Labels();
    }

    // "다시 보지 않기 초기화": 체크한 크리처도 다시 연출이 나옴
    public void ResetLv3Skips()
    {
        _meta.ResetLv3ShowcaseSkips();
        UpdateLv3Labels();
    }

    private void UpdateLv3Labels()
    {
        _lv3Label.text = _meta.Lv3ShowcaseEnabled ? "3성 일러스트 연출 켜짐" : "3성 일러스트 연출 꺼짐";
        int count = _meta.Lv3ShowcaseSkipCount;
        _lv3ResetButton.interactable = count > 0;
        _lv3ResetLabel.text = count > 0 ? $"다시 보지 않기 초기화 ({count}마리)" : "다시 보지 않기 없음";
    }

    public void SetSfxVolume(float volume)
    {
        _audio.SetSfxVolume(volume);
    }

    public void SetBgmVolume(float volume)
    {
        _audio.SetBgmVolume(volume);
    }

    // "타이틀로 나가기" → 확인 창
    public void AskExit()
    {
        _confirmPanel.SetActive(true);
    }

    // 확인 창 "나가기": 창을 닫아 원래 배속으로 돌린 뒤 저장하고 타이틀로
    public void ConfirmExit()
    {
        Close();
        _run.ExitToTitle();
    }

    // 확인 창 "취소"
    public void CancelExit()
    {
        _confirmPanel.SetActive(false);
    }

    private void UpdateSfxLabel()
    {
        _sfxLabel.text = _audio.SfxEnabled ? "효과음 켜짐" : "효과음 꺼짐";
    }

    private void UpdateBgmLabel()
    {
        _bgmLabel.text = _audio.BgmEnabled ? "배경음 켜짐" : "배경음 꺼짐";
    }
}
