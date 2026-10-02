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
    // GameScreen 순서대로 넣는다.
    [SerializeField] private GameObject[] _screens;
    [Tooltip("라운드·목숨·골드 상단 표시. 상점과 전투 화면에서만 보인다")]
    [SerializeField] private GameObject _hud;

    private void Start()
    {
        Show(GameScreen.Title);
    }

    public void Show(GameScreen screen)
    {
        for (int i = 0; i < _screens.Length; i++)
        {
            _screens[i].SetActive(i == (int)screen);
        }
        _hud.SetActive(screen == GameScreen.Shop || screen == GameScreen.Battle);
    }

    // 버튼 OnClick에서 호출 (인스펙터는 enum 인자를 지원하지 않아 int로 받는다)
    public void ShowScreen(int screen)
    {
        Show((GameScreen)screen);
    }
}
