using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 자동 전투: 공격 속도마다 상대 앞줄부터 공격, 한 팀 전멸 시 종료
public class BattleScreen : MonoBehaviour
{
    private const int FrontRowCount = 2;

    [SerializeField] private RunManager _run;
    [SerializeField] private TypeChart _typeChart;
    [SerializeField] private SynergyConfig _synergy;
    [Tooltip("0~1 앞줄, 2~4 뒷줄")]
    [SerializeField] private CreatureUnitView[] _playerViews;
    [SerializeField] private CreatureUnitView[] _enemyViews;
    [Tooltip("적 팀 이름 (보스면 '보스:' 표시)")]
    [SerializeField] private Text _enemyLabel;
    [SerializeField] private Text _bannerText;
    [SerializeField] private Text _speedLabel;
    [Tooltip("승리/패배 표시 후 다음으로 넘어가기까지 (초)")]
    [SerializeField] private float _resultDelay = 1.5f;
    [SerializeField] private float _fastSpeed = 2f;

    private readonly List<BattleUnit> _players = new List<BattleUnit>();
    private readonly List<BattleUnit> _enemies = new List<BattleUnit>();
    private readonly List<CreatureData> _teamCreatures = new List<CreatureData>();
    private readonly int[] _typeCounts = new int[TypeSynergy.TypeCount];
    private float _speed = 1f;
    private bool _running;

    // RunManager에서 호출. playerTeam은 팀 칸 순서 (빈 칸은 null).
    // 매 전투 새 BattleUnit을 만들므로 체력은 항상 가득 찬 상태로 시작한다
    // trainer는 내 팀에만 적용 (적 팀은 시너지만)
    public void StartBattle(CreatureInstance[] playerTeam, EnemyTeamData enemyTeam, TrainerData trainer)
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
            EnemyTeamData.Member member = i < enemyTeam.SlotCount ? enemyTeam.GetMember(i) : default;
            Spawn(member.Creature, Mathf.Max(1, member.Level), i, _enemyViews, _enemies);
        }

        // 타입 시너지는 내 팀·적 팀 똑같이, 트레이너 능력은 내 팀만 (시너지와 겹쳐서 적용)
        ApplyBonuses(_players, trainer);
        ApplyBonuses(_enemies, null);

        _enemyLabel.text = enemyTeam.IsBoss ? $"보스: {enemyTeam.DisplayName}" : enemyTeam.DisplayName;
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
        TickRegen(_players, deltaTime);
        TickRegen(_enemies, deltaTime);
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

    // 팀의 타입별 마릿수(같은 크리처는 1마리)에 따라 해당 타입 크리처에게 보너스
    // 트레이너 능력은 시너지 보너스에 더해진다 (예: 불 시너지 +15% + 불꽃 조련사 +20% = +35%)
    private void ApplyBonuses(List<BattleUnit> units, TrainerData trainer)
    {
        _teamCreatures.Clear();
        foreach (BattleUnit unit in units)
        {
            _teamCreatures.Add(unit.Data);
        }
        TypeSynergy.CountUnique(_teamCreatures, _typeCounts);

        foreach (BattleUnit unit in units)
        {
            int tier = _synergy.GetTier(_typeCounts[(int)unit.Data.Type]);
            BattleBonus bonus = _synergy.GetBonus(unit.Data.Type, tier);
            if (trainer != null)
            {
                trainer.AddBattleBonus(unit.Data.Type, ref bonus);
            }
            unit.ApplyBonus(bonus);
        }
    }

    private static void TickRegen(List<BattleUnit> units, float deltaTime)
    {
        foreach (BattleUnit unit in units)
        {
            unit.TickRegen(deltaTime);
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
        _bannerText.text = won ? "승리!" : "패배... 목숨 -1";
        yield return new WaitForSeconds(_resultDelay);
        _run.OnBattleEnd(won);
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
