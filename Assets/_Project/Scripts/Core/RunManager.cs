using UnityEngine;
using UnityEngine.UI;

// 한 판(런) 진행: 라운드, 목숨, 상점 → 전투 → 다음 라운드, 클리어/게임 오버, 저장/이어하기
public class RunManager : MonoBehaviour
{
    [SerializeField] private RunConfig _config;
    [SerializeField] private ScreenFlow _flow;
    [SerializeField] private ShopScreen _shop;
    [SerializeField] private BattleScreen _battle;
    [Tooltip("저장 파일의 트레이너 이름을 찾을 때 사용")]
    [SerializeField] private TrainerData[] _trainers;

    [Header("상단 표시")]
    [SerializeField] private Text _roundText;
    [SerializeField] private Text _livesText;
    [Tooltip("상점·배치 화면 위쪽의 트레이너 이름")]
    [SerializeField] private Text _trainerText;

    [Header("결과 화면")]
    [SerializeField] private Text _resultTitle;
    [SerializeField] private Text _resultDetail;

    private int _round;
    private int _lives;
    private TrainerData _trainer;
    // 행운 동전: 이번 전투에서 이기면 받을 골드 / 다음 라운드에 더할 골드
    private int _winGold;
    private int _pendingGold;
    // 상점 단계일 때만 저장한다 (전투 중·판 밖에서는 저장하지 않음)
    private bool _inShop;

    private void Awake()
    {
        // meta.json이 없으면 기본값으로 만들어 둔다 (깨졌으면 백업 후 새로)
        SaveSystem.SaveMeta(SaveSystem.LoadMeta());
    }

    // 트레이너 선택 화면의 "선택 완료"
    public void StartRun(TrainerData trainer)
    {
        _trainer = trainer;
        _round = 1;
        _lives = _config.MaxLives;
        _pendingGold = 0;
        _trainerText.text = $"트레이너: {trainer.DisplayName}";
        _shop.ResetRun();
        StartRound();
    }

    // 타이틀의 "이어하기". 저장 파일을 쓸 수 없으면 false (깨진 파일은 백업됨)
    public bool ContinueRun()
    {
        if (!SaveSystem.TryLoadRun(out RunSaveData data, out _))
        {
            return false;
        }

        TrainerData trainer = FindTrainer(data.trainer);
        if (trainer == null || data.round < 1 || data.round > _config.RoundCount
            || data.lives < 1 || data.lives > _config.MaxLives)
        {
            Debug.LogWarning("[Save] run.json 내용이 올바르지 않아 백업합니다.");
            SaveSystem.BackupBrokenRun();
            return false;
        }

        _trainer = trainer;
        _round = data.round;
        _lives = data.lives;
        _pendingGold = 0;
        _trainerText.text = $"트레이너: {trainer.DisplayName}";

        // 저장된 상점 단계 그대로 (전투 중에 꺼졌어도 그 라운드 상점부터)
        _flow.Show(GameScreen.Shop);
        _shop.LoadSave(data);
        _inShop = true;
        UpdateHud();
        return true;
    }

    // 상점의 "전투 시작" (팀이 비어 있지 않은 것은 상점에서 확인)
    public void BeginBattle(CreatureInstance[] team)
    {
        _inShop = false;

        // 전투에 나가는 팀 칸 크리처의 행운 동전만 센다 (여러 개면 더함)
        _winGold = 0;
        foreach (CreatureInstance creature in team)
        {
            if (creature?.Item != null)
            {
                _winGold += creature.Item.WinGold;
            }
        }

        _flow.Show(GameScreen.Battle);
        _battle.StartBattle(team, _config.GetEnemyTeam(_round), _trainer);
    }

    // 전투 화면이 승패 표시를 끝낸 뒤 호출
    public void OnBattleEnd(bool won)
    {
        bool lastRound = _round >= _config.RoundCount;
        // 골드는 라운드 시작 때 다시 채우므로, 이겨서 받은 골드는 다음 라운드 골드에 더한다
        _pendingGold = won ? _winGold : 0;

        if (won)
        {
            if (lastRound)
            {
                ShowResult(true);
                return;
            }
            _round++;
            StartRound();
            return;
        }

        _lives--;
        if (_lives <= 0)
        {
            ShowResult(false);
            return;
        }

        // 지더라도 다음 라운드로. 마지막 라운드에서 지면 마지막 라운드를 다시
        if (!lastRound)
        {
            _round++;
        }
        StartRound();
    }

    private void StartRound()
    {
        _flow.Show(GameScreen.Shop);
        // 상인: 라운드 골드 +1, 행운 동전: 지난 전투 승리 시 +1
        _shop.StartRound(_trainer.RoundGoldBonus + _pendingGold);
        _pendingGold = 0;
        _inShop = true;
        UpdateHud();
        // 상점 단계가 시작될 때 자동 저장
        SaveRun();
    }

    private void ShowResult(bool cleared)
    {
        // 판이 끝나면 진행 중인 판 저장을 지운다
        _inShop = false;
        SaveSystem.DeleteRun();
        UpdateHud();
        _resultTitle.text = cleared ? "클리어!" : "게임 오버";
        _resultDetail.text = cleared
            ? $"{_config.RoundCount}라운드 보스까지 승리했어요"
            : $"{_round}라운드에서 목숨을 모두 잃었어요";
        _flow.Show(GameScreen.Result);
    }

    // Android에서 앱이 내려갈 때
    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveRun();
        }
    }

    // 에디터에서 Play를 멈출 때도 저장 (테스트용, 상점 단계일 때만)
    private void OnApplicationQuit()
    {
        SaveRun();
    }

    private void SaveRun()
    {
        if (!_inShop)
        {
            return;
        }

        var data = new RunSaveData
        {
            trainer = _trainer.name,
            round = _round,
            lives = _lives
        };
        _shop.WriteSave(data);
        SaveSystem.SaveRun(data);
    }

    private TrainerData FindTrainer(string id)
    {
        foreach (TrainerData trainer in _trainers)
        {
            if (trainer != null && trainer.name == id)
            {
                return trainer;
            }
        }
        return null;
    }

    private void UpdateHud()
    {
        string boss = _config.GetEnemyTeam(_round).IsBoss ? " 보스" : "";
        _roundText.text = $"라운드 {_round}/{_config.RoundCount}{boss}";
        _livesText.text = "목숨 " + new string('♥', _lives) + new string('♡', _config.MaxLives - _lives);
    }
}
