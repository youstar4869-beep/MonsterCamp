using UnityEngine;
using UnityEngine.UI;

// 한 판(런) 진행: 난이도, 층, 목숨, 상점 → 전투 → 다음 층, 클리어/게임 오버, 저장/이어하기
public class RunManager : MonoBehaviour
{
    [SerializeField] private RunConfig _config;
    [SerializeField] private ScreenFlow _flow;
    [SerializeField] private ShopScreen _shop;
    [SerializeField] private BattleScreen _battle;
    [SerializeField] private MetaProgress _meta;
    [SerializeField] private TutorialController _tutorial;
    [Tooltip("상점에서 띄우는 도움말 (보스 전 팁, 마지막 층 재도전 안내)")]
    [SerializeField] private TipPopup _tip;
    [Tooltip("저장 파일의 트레이너 이름을 찾을 때 사용")]
    [SerializeField] private TrainerData[] _trainers;
    [Tooltip("저장 파일의 난이도 이름을 찾을 때 사용. 첫 번째(보통)는 예전 저장 이어하기·튜토리얼에 쓴다")]
    [SerializeField] private DifficultyData[] _difficulties;

    [Header("상단 표시")]
    [Tooltip("층·목숨·골드·이번 판 별")]
    [SerializeField] private HudView _hud;
    [Tooltip("상점·배치 화면 위쪽의 트레이너 이름")]
    [SerializeField] private Text _trainerText;

    [Header("결과 화면")]
    [SerializeField] private Text _resultTitle;
    [SerializeField] private Text _resultDetail;
    [Tooltip("이번 판에서 받은 별 / 전체 별")]
    [SerializeField] private Text _resultStars;

    private DifficultyData _difficulty;
    private int _floor;
    // 11층부터 적 팀 자동 생성 시드 (run.json에 저장)
    private int _runSeed;
    // 이번 판에서 받은 별 (판이 끝날 때 한꺼번에 meta.json에 더함)
    private int _runStars;
    private int _lives;
    private TrainerData _trainer;
    // 행운 동전: 이번 전투에서 이기면 받을 골드 / 다음 층에 더할 골드
    private int _winGold;
    private int _pendingGold;
    // 상점 단계일 때만 저장한다 (전투 중·판 밖에서는 저장하지 않음)
    private bool _inShop;
    // 마지막 층에서 져서 다시 도전하면 상점에서 안내 (BALANCE_REPORT.md 3-3)
    private bool _showRetryTip;

    private const string BossTipTitle = "보스 층!";
    private const string BossTipMessage = "다음 전투는 보스예요.\n앞줄에는 체력 높은 크리처를 두세요.";
    private const string RetryTipTitle = "다시 도전!";
    private const string RetryTipMessage = "같은 팀으로는 같은 결과가 나오기 쉬워요.\n팀 구성을 바꿔 다시 도전해 보세요.";
    private const string FusionUnlockTitle = "조합 해금!";
    private const string FusionUnlockMessage = "정해진 두 크리처를 ★★ 이상으로 모으면\n상점에서 더 강한 크리처로 조합할 수 있어요.";

    private bool IsBossFloor => _difficulty.IsBossFloor(_floor);
    private bool IsLastFloor => _floor >= _difficulty.FloorCount;

    // 난이도 선택 화면의 "시작"
    public void StartRun(TrainerData trainer, DifficultyData difficulty)
    {
        _trainer = trainer;
        _difficulty = difficulty;
        _floor = 1;
        _runSeed = new System.Random().Next();
        _lives = difficulty.Lives;
        _runStars = 0;
        _pendingGold = 0;
        _showRetryTip = false;
        _trainerText.text = $"트레이너: {trainer.DisplayName}";
        _shop.ResetRun(difficulty);
        // 첫 판(또는 다시 보기)이면 튜토리얼 시작. 튜토리얼은 보통 난이도 1~2층 기준
        if (difficulty == _difficulties[0])
        {
            _tutorial.BeginRun();
        }
        else
        {
            _tutorial.Stop();
        }
        StartFloor();
    }

    // 타이틀의 "이어하기". 저장 파일을 쓸 수 없으면 false (깨진 파일은 백업됨)
    public bool ContinueRun()
    {
        // 이어하기에서는 튜토리얼을 보여 주지 않는다
        _tutorial.Stop();
        if (!SaveSystem.TryLoadRun(out RunSaveData data, out _))
        {
            return false;
        }

        // 예전 저장(라운드만 있음)은 보통 난이도 같은 층으로
        if (data.floor == 0 && string.IsNullOrEmpty(data.difficulty))
        {
            data.floor = data.round;
            data.difficulty = _difficulties[0].name;
        }

        TrainerData trainer = FindTrainer(data.trainer);
        DifficultyData difficulty = FindDifficulty(data.difficulty);
        if (trainer == null || difficulty == null || data.floor < 1 || data.floor > difficulty.FloorCount
            || data.lives < 1 || data.lives > difficulty.Lives)
        {
            Debug.LogWarning("[Save] run.json 내용이 올바르지 않아 백업합니다.");
            SaveSystem.BackupBrokenRun();
            return false;
        }

        _trainer = trainer;
        _difficulty = difficulty;
        _floor = data.floor;
        _runSeed = data.seed;
        _lives = data.lives;
        _runStars = Mathf.Max(0, data.starsEarned);
        _pendingGold = 0;
        _trainerText.text = $"트레이너: {trainer.DisplayName}";

        // 저장된 상점 단계 그대로 (전투 중에 꺼졌어도 그 층 상점부터)
        _flow.Show(GameScreen.Shop);
        _shop.LoadSave(data, difficulty);
        _inShop = true;
        UpdateHud();
        ShowShopTips();
        return true;
    }

    // 상점의 "전투 시작" (팀이 비어 있지 않은 것은 상점에서 확인)
    public void BeginBattle(CreatureInstance[] team)
    {
        // 전투 중에 나가거나 꺼져도 전투 직전 상점 상태로 이어하게 저장
        SaveRun();
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

        // 튜토리얼 중 1~2층은 약한 적 팀
        EnemyTeamData enemy = _tutorial.GetEnemyTeam(_floor);
        if (enemy == null)
        {
            enemy = EnemyTeamGenerator.GetTeam(_config, _shop.Config, _battle.Synergy.Mixed, _difficulty, _floor, _runSeed);
        }
        _flow.Show(GameScreen.Battle);
        _battle.StartBattle(team, enemy, _trainer, _floor, IsBossFloor, _difficulty.GetEnemyStatMultiplier(_floor),
            _difficulty.EnemyMixedSynergy, _difficulty.GetBossAbilities(_floor));
        _tutorial.OnBattleStarted();
    }

    // 전투 화면이 승패 표시를 끝낸 뒤 호출
    public void OnBattleEnd(bool won)
    {
        bool lastFloor = IsLastFloor;
        // 이겨서 받은 골드(행운 동전)는 다음 층 시작 때 층 골드와 함께 더한다
        _pendingGold = won ? _winGold : 0;

        if (won)
        {
            // 별: 이길 때마다 +1, 보스 층 +3, 클리어는 난이도마다 (판이 끝날 때 meta.json에 더함)
            MetaConfig rules = _meta.Config;
            _runStars += rules.StarsPerWin;
            if (lastFloor)
            {
                _runStars += _difficulty.ClearStars;
            }
            else if (IsBossFloor)
            {
                _runStars += rules.BossBonus;
            }

            if (lastFloor)
            {
                _meta.SetCleared(_difficulty);
                // 보통을 클리어하면 조합 해금
                bool unlockFusion = _difficulty == _difficulties[0] && !_meta.FusionUnlocked;
                if (unlockFusion)
                {
                    _meta.SetFusionUnlocked(true);
                }
                ShowResult(true);
                if (unlockFusion)
                {
                    _tip.Show(FusionUnlockTitle, FusionUnlockMessage);
                }
                return;
            }
            _floor++;
            StartFloor();
            return;
        }

        _lives--;
        if (_lives <= 0)
        {
            ShowResult(false);
            return;
        }

        // 지더라도 다음 층으로. 마지막 층에서 지면 마지막 층을 다시
        if (!lastFloor)
        {
            _floor++;
        }
        else
        {
            _showRetryTip = true;
        }
        StartFloor();
    }

    private void StartFloor()
    {
        _flow.Show(GameScreen.Shop);
        // 상인: 층 골드 +1, 행운 동전: 지난 전투 승리 시 +1
        // 튜토리얼 중이면 정해 둔 상점 목록 (아니면 null → 랜덤).
        // 튜토리얼 층은 골드를 정해 둔 만큼만 주도록 남은 골드·이자를 넘기지 않는다
        CreatureData[] presetShop = _tutorial.GetPresetShop(_floor);
        _shop.StartRound(_floor, _trainer.RoundGoldBonus + _pendingGold, presetShop, presetShop == null);
        _tutorial.OnShopStarted(_floor);
        _pendingGold = 0;
        _inShop = true;
        UpdateHud();
        ShowShopTips();
        // 상점 단계가 시작될 때 자동 저장
        SaveRun();
    }

    // 상점 도움말: 마지막 층 재도전이면 팀을 바꿔 보라는 안내 (매번),
    // 마지막이 아닌 보스 층 상점이면 앞줄 팁 (계정마다 한 번, 튜토리얼 중에는 안 보임)
    private void ShowShopTips()
    {
        if (_showRetryTip)
        {
            _showRetryTip = false;
            _tip.Show(RetryTipTitle, RetryTipMessage);
            return;
        }
        if (!_tutorial.IsActive && !_meta.FrontRowTipShown && !IsLastFloor && IsBossFloor)
        {
            _tip.Show(BossTipTitle, BossTipMessage);
            _meta.SetFrontRowTipShown();
        }
    }

    private void ShowResult(bool cleared)
    {
        _inShop = false;
        _tutorial.Stop();
        // 이번 판에서 받은 별을 한꺼번에 meta.json에 더한 뒤 진행 중인 판 저장을 지운다
        _meta.AddStars(_runStars);
        SaveSystem.DeleteRun();
        _resultStars.text = $"이번 판 별 +{_runStars}\n전체 별 {_meta.Stars}";
        UpdateHud();
        _resultTitle.text = cleared ? $"{_difficulty.DisplayName} 클리어!" : "게임 오버";
        _resultDetail.text = cleared
            ? $"{_difficulty.FloorCount}층 보스까지 승리했어요"
            : $"{_floor}층에서 목숨을 모두 잃었어요";
        _flow.Show(GameScreen.Result);
    }

    // 설정 창 "타이틀로 나가기": 상점이면 지금 상태를 저장 (전투 중이면 BeginBattle에서 저장한 것 그대로)
    public void ExitToTitle()
    {
        SaveRun();
        _inShop = false;
        _tutorial.Stop();
        _tip.Close();
        _shop.ClosePopups();
        _flow.Show(GameScreen.Title);
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
            difficulty = _difficulty.name,
            floor = _floor,
            seed = _runSeed,
            lives = _lives,
            starsEarned = _runStars
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

    private DifficultyData FindDifficulty(string id)
    {
        foreach (DifficultyData difficulty in _difficulties)
        {
            if (difficulty != null && difficulty.name == id)
            {
                return difficulty;
            }
        }
        return null;
    }

    private void UpdateHud()
    {
        _hud.SetFloor(_floor, _difficulty.FloorCount, IsBossFloor);
        _hud.SetLives(_lives);
        _hud.SetStars(_runStars);
    }
}
