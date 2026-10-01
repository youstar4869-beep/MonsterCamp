using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 자동 전투: 공격 속도마다 상대 앞줄부터 공격, 한 팀 전멸 시 종료
public class BattleScreen : MonoBehaviour
{
    private const int FrontRowCount = 2;
    // 테스트 적 팀 레벨 (라운드별 적 팀은 TODO 8)
    private const int TestEnemyLevel = 1;

    [SerializeField] private ScreenFlow _flow;
    [SerializeField] private TypeChart _typeChart;
    [Tooltip("테스트용 적 팀 (0~1 앞줄, 2~4 뒷줄). 라운드별 적 팀은 TODO 8에서")]
    [SerializeField] private CreatureData[] _testEnemyTeam;
    [Tooltip("0~1 앞줄, 2~4 뒷줄")]
    [SerializeField] private CreatureUnitView[] _playerViews;
    [SerializeField] private CreatureUnitView[] _enemyViews;
    [SerializeField] private Text _bannerText;
    [SerializeField] private Text _speedLabel;
    [SerializeField] private Text _resultTitle;
    [Tooltip("승리/패배 표시 후 결과 화면으로 넘어가기까지 (초)")]
    [SerializeField] private float _resultDelay = 1.5f;
    [SerializeField] private float _fastSpeed = 2f;

    private readonly List<BattleUnit> _players = new List<BattleUnit>();
    private readonly List<BattleUnit> _enemies = new List<BattleUnit>();
    private float _speed = 1f;
    private bool _running;

    // 상점에서 호출. playerTeam은 팀 칸 순서 (빈 칸은 null)
    public void StartBattle(CreatureInstance[] playerTeam)
    {
        StopAllCoroutines();

        _players.Clear();
        for (int i = 0; i < _playerViews.Length; i++)
        {
            CreatureInstance creature = i < playerTeam.Length ? playerTeam[i] : null;
            Spawn(creature?.Data, creature?.Level ?? 1, i, _playerViews, _players);
        }

        _enemies.Clear();
        for (int i = 0; i < _enemyViews.Length; i++)
        {
            CreatureData creature = i < _testEnemyTeam.Length ? _testEnemyTeam[i] : null;
            Spawn(creature, TestEnemyLevel, i, _enemyViews, _enemies);
        }

        _bannerText.text = "";
        UpdateSpeedLabel();
        _running = true;
    }

    public void ToggleSpeed()
    {
        _speed = _speed > 1f ? 1f : _fastSpeed;
        UpdateSpeedLabel();
    }

    private void Update()
    {
        if (!_running)
        {
            return;
        }

        float deltaTime = Time.deltaTime * _speed;
        Tick(_players, _enemies, deltaTime);
        Tick(_enemies, _players, deltaTime);

        if (!AnyAlive(_enemies))
        {
            StartCoroutine(Finish(true));
        }
        else if (!AnyAlive(_players))
        {
            StartCoroutine(Finish(false));
        }
    }

    private void Tick(List<BattleUnit> attackers, List<BattleUnit> defenders, float deltaTime)
    {
        foreach (BattleUnit attacker in attackers)
        {
            if (!attacker.IsAlive)
            {
                continue;
            }

            attacker.Cooldown -= deltaTime;
            while (attacker.Cooldown <= 0f)
            {
                BattleUnit target = FindTarget(defenders);
                if (target == null)
                {
                    return;
                }

                target.TakeDamage(CalculateDamage(attacker, target));
                attacker.Cooldown += attacker.AttackInterval;
            }
        }
    }

    // 살아 있는 앞줄 중 앞 칸부터, 앞줄이 없으면 뒷줄
    private static BattleUnit FindTarget(List<BattleUnit> defenders)
    {
        BattleUnit backRowTarget = null;
        foreach (BattleUnit unit in defenders)
        {
            if (!unit.IsAlive)
            {
                continue;
            }
            if (unit.SlotIndex < FrontRowCount)
            {
                return unit;
            }
            if (backRowTarget == null)
            {
                backRowTarget = unit;
            }
        }
        return backRowTarget;
    }

    // 공격력 × 타입 상성
    private int CalculateDamage(BattleUnit attacker, BattleUnit target)
    {
        float multiplier = _typeChart.GetMultiplier(attacker.Data.Type, target.Data.Type);
        return Mathf.Max(1, Mathf.RoundToInt(attacker.Attack * multiplier));
    }

    private static bool AnyAlive(List<BattleUnit> units)
    {
        foreach (BattleUnit unit in units)
        {
            if (unit.IsAlive)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerator Finish(bool won)
    {
        _running = false;
        _bannerText.text = won ? "승리!" : "패배...";
        yield return new WaitForSeconds(_resultDelay);
        _resultTitle.text = won ? "승리" : "패배";
        _flow.Show(GameScreen.Result);
    }

    // 빈 칸(creature == null)이면 칸만 비워 둔다
    private static void Spawn(CreatureData creature, int level, int slotIndex, CreatureUnitView[] views, List<BattleUnit> units)
    {
        if (creature == null)
        {
            views[slotIndex].Hide();
            return;
        }
        units.Add(new BattleUnit(creature, slotIndex, level, views[slotIndex]));
    }

    private void UpdateSpeedLabel()
    {
        _speedLabel.text = _speed > 1f ? $"{_fastSpeed:0.#}배속" : "1배속";
    }
}
