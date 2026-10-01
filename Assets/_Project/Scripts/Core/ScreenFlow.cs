using UnityEngine;

public enum GameScreen
{
    Title,
    TrainerSelect,
    Shop,
    Battle,
    Result
}

// 화면(패널) 전환. 한 번에 한 화면만 켠다.
public class ScreenFlow : MonoBehaviour
{
    // GameScreen 순서대로 넣는다.
    [SerializeField] private GameObject[] _screens;

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
    }

    // 버튼 OnClick에서 호출 (인스펙터는 enum 인자를 지원하지 않아 int로 받는다)
    public void ShowScreen(int screen)
    {
        Show((GameScreen)screen);
    }
}
