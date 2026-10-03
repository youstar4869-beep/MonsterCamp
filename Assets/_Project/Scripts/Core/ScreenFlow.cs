using UnityEngine;

public enum GameScreen
{
    Title,
    TrainerSelect,
    Shop,
    Battle,
    Result,
    // 버튼에 숫자로 연결되어 있으므로 새 화면은 항상 끝에 추가
    Codex
}

// 화면(패널) 전환. 한 번에 한 화면만 켠다.
public class ScreenFlow : MonoBehaviour
{
    private const int TargetFrameRate = 60;

    // GameScreen 순서대로 넣는다.
    [SerializeField] private GameObject[] _screens;
    [Tooltip("라운드·목숨·골드 상단 표시. 상점과 전투 화면에서만 보인다")]
    [SerializeField] private GameObject _hud;
    [Tooltip("화면이 바뀔 때 짧은 페이드 (없으면 바로 바뀜)")]
    [SerializeField] private ScreenFader _fader;

    private GameScreen? _current;

    public GameScreen Current => _current ?? GameScreen.Title;

    private void Start()
    {
        // 모바일 기본은 30fps라 60으로 올린다 (PC 에디터에서는 영향 없음: vSync가 우선)
        Application.targetFrameRate = TargetFrameRate;
        Show(GameScreen.Title);
    }

    public void Show(GameScreen screen)
    {
        // 같은 화면을 다시 보여 줄 때는 페이드하지 않는다
        if (_fader != null && _current.HasValue && _current.Value != screen)
        {
            _fader.Play();
        }
        _current = screen;
        for (int i = 0; i < _screens.Length; i++)
        {
            _screens[i].SetActive(i == (int)screen);
        }
        _hud.SetActive(screen == GameScreen.Shop || screen == GameScreen.Battle);
        AudioManager.PlayBgm(screen == GameScreen.Battle ? Bgm.Battle : Bgm.Shop);
    }

    // 버튼 OnClick에서 호출 (인스펙터는 enum 인자를 지원하지 않아 int로 받는다)
    public void ShowScreen(int screen)
    {
        Show((GameScreen)screen);
    }
}
