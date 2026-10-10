using System;
using System.Collections.Generic;

public enum BattleEventType
{
    // 공격함 (Unit = 공격한 크리처)
    Attack,
    // 피해를 받음 (Amount = 받은 피해, 보호막에 막힌 양 포함)
    Damage,
    // 회복함 (Amount = 회복량)
    Heal,
    // 보호막이 생김 (Amount = 보호막 양)
    Shield,
    // 쓰러짐
    Death,
    // 부활함
    Revive,
    // 스킬 발동 (Unit = 표시할 크리처, Source = 스킬 주인, Text = 화면에 띄울 짧은 글, 비어 있으면 표시 안 함)
    Skill,
    // 보스 광폭화 발동 (Unit 없음)
    BossEnrage
}

public struct BattleEvent
{
    public BattleEventType Type;
    public BattleSimUnit Unit;
    public BattleSimUnit Source;
    public int Amount;
    public string Text;
}

// 화면 없이 돌아가는 자동 전투 (UnityEngine을 쓰지 않음).
// 매 프레임 Tick(초)을 부르면 진행되고, 결과는 Event로 알린다. 같은 시드면 같은 결과.
// 규칙: 공격 속도마다 상대 앞줄(0~1)부터 공격, 앞줄이 모두 쓰러지면 뒷줄(2~4). 한 팀이 전멸하면 끝
public class BattleSimulation
{
    public const int FrontRowCount = 2;
    // 화상 피해 간격 (초)
    private const float BurnTickInterval = 1f;

    private enum DamageKind
    {
        Attack,
        Skill,
        Burn,
        Reflect
    }

    private readonly List<BattleSimUnit> _players = new List<BattleSimUnit>();
    private readonly List<BattleSimUnit> _enemies = new List<BattleSimUnit>();
    // [공격 타입, 방어 타입] 상성 배율
    private readonly float[,] _typeMultipliers;
    private readonly Random _random;
    private readonly List<BattleSimUnit> _targets = new List<BattleSimUnit>();
    // 여러 마리 스킬 대상 (저격·시작 기절·시작 묶기)
    private readonly List<BattleSimUnit> _skillTargets = new List<BattleSimUnit>();

    public event Action<BattleEvent> Event;

    public IReadOnlyList<BattleSimUnit> Players => _players;
    public IReadOnlyList<BattleSimUnit> Enemies => _enemies;
    public bool IsOver { get; private set; }
    public bool PlayerWon { get; private set; }
    public float ElapsedTime { get; private set; }

    // 보스 특수 능력 (적 팀 전체, 모두 0이면 없음)
    private readonly BossEffects _boss;
    private float _bossRegenTimer;
    private bool _bossEnraged;

    // 철벽: 전투 시작 몇 초 동안 적 전체 피해 무시
    public bool IsBossInvulnerable => ElapsedTime < _boss.InvulnerableSeconds;

    // typeMultipliers: [공격 타입, 방어 타입] (CreatureType 순서). seed가 같으면 같은 결과.
    // boss: 보스 특수 능력 (보통·어려움·보스 아닌 층은 default)
    public BattleSimulation(IEnumerable<BattleUnitSetup> players, IEnumerable<BattleUnitSetup> enemies,
        float[,] typeMultipliers, int seed, BossEffects boss = default)
    {
        _typeMultipliers = typeMultipliers;
        _random = new Random(seed);
        _boss = boss;
        _bossRegenTimer = boss.RegenInterval;
        foreach (BattleUnitSetup setup in players)
        {
            _players.Add(new BattleSimUnit(setup, true));
        }
        foreach (BattleUnitSetup setup in enemies)
        {
            BattleUnitSetup enemy = setup;
            // 가시 껍질·질풍: 가시 갑옷·공격 속도 보너스에 더해 기존 반사·공격 간격 계산을 그대로 쓴다
            if (boss.ThornsPercent > 0f)
            {
                enemy.Bonus.ThornsPercent += boss.ThornsPercent;
            }
            if (boss.AttackSpeedBonus > 0f)
            {
                enemy.Bonus.SpeedBonus += boss.AttackSpeedBonus;
            }
            _enemies.Add(new BattleSimUnit(enemy, false));
        }
    }

    // 전투 시작 시 스킬 (이벤트를 받을 준비가 된 뒤 한 번 부른다)
    public void Start()
    {
        // 최대 체력이 먼저 바뀌어야 비율 보호막이 맞으므로 종류별로 나눠서
        ApplyStartSkills(_players, _enemies, maxHealthPhase: true);
        ApplyStartSkills(_enemies, _players, maxHealthPhase: true);
        ApplyStartSkills(_players, _enemies, maxHealthPhase: false);
        ApplyStartSkills(_enemies, _players, maxHealthPhase: false);
        // 조합 시너지: 보호막·가속을 양쪽 다 준 뒤 시작 피해
        ApplyStartSynergy(_players);
        ApplyStartSynergy(_enemies);
        ApplyStartEnemyDamage(_players, _enemies);
        ApplyStartEnemyDamage(_enemies, _players);
        CheckOver();
    }

    public void Tick(float deltaTime)
    {
        if (IsOver)
        {
            return;
        }

        ElapsedTime += deltaTime;
        TickBoss(deltaTime);
        TickUnits(_players, _enemies, deltaTime);
        TickUnits(_enemies, _players, deltaTime);
        TickAttacks(_players, _enemies, deltaTime);
        TickAttacks(_enemies, _players, deltaTime);
        CheckOver();
    }

    // ---------- 진행 ----------

    private void CheckOver()
    {
        if (!AnyAlive(_enemies))
        {
            IsOver = true;
            PlayerWon = true;
        }
        else if (!AnyAlive(_players))
        {
            IsOver = true;
            PlayerWon = false;
        }
    }

    // 보스 특수 능력: 광폭화(시간이 되면 한 번), 재생(N초마다 적 전체 회복)
    private void TickBoss(float deltaTime)
    {
        if (!_bossEnraged && _boss.EnrageAttackMultiplier > 0f && ElapsedTime >= _boss.EnrageAfterSeconds)
        {
            _bossEnraged = true;
            foreach (BattleSimUnit enemy in _enemies)
            {
                enemy.BossAttackMultiplier = _boss.EnrageAttackMultiplier;
            }
            Raise(BattleEventType.BossEnrage, null, null, 0, null);
        }

        if (_boss.RegenPercent > 0f && _boss.RegenInterval > 0f)
        {
            _bossRegenTimer -= deltaTime;
            while (_bossRegenTimer <= 0f)
            {
                _bossRegenTimer += _boss.RegenInterval;
                HealAll(_enemies, _boss.RegenPercent);
            }
        }
    }

    // 회복, 상태이상 시간, 화상, N초마다 스킬
    private void TickUnits(List<BattleSimUnit> units, List<BattleSimUnit> opponents, float deltaTime)
    {
        foreach (BattleSimUnit unit in units)
        {
            if (!unit.IsAlive)
            {
                continue;
            }

            // 풀 시너지·회복 열매 (이전과 같은 계산)
            if (unit.RegenPercent > 0f)
            {
                unit.RegenTimer -= deltaTime;
                while (unit.RegenTimer <= 0f)
                {
                    unit.RegenTimer += unit.RegenInterval;
                    Heal(unit, BattleSimUnit.Round(unit.MaxHealth * unit.RegenPercent));
                }
            }

            // 온천 (조합 시너지): 풀 시너지 회복과 따로
            if (unit.TeamRegenPercent > 0f)
            {
                unit.TeamRegenTimer -= deltaTime;
                while (unit.TeamRegenTimer <= 0f)
                {
                    unit.TeamRegenTimer += unit.TeamRegenInterval;
                    Heal(unit, BattleSimUnit.Round(unit.MaxHealth * unit.TeamRegenPercent));
                }
            }

            TickStatus(unit, deltaTime);
            if (!unit.IsAlive)
            {
                continue;
            }
            TickPeriodicSkill(unit, units, opponents, deltaTime);
        }
    }

    private void TickStatus(BattleSimUnit unit, float deltaTime)
    {
        unit.StunTimer = Math.Max(0f, unit.StunTimer - deltaTime);
        unit.RootTimer = Math.Max(0f, unit.RootTimer - deltaTime);
        unit.HasteTimer = Math.Max(0f, unit.HasteTimer - deltaTime);
        unit.SynergyHasteTimer = Math.Max(0f, unit.SynergyHasteTimer - deltaTime);

        if (unit.SlowAmount > 0f)
        {
            unit.SlowTimer -= deltaTime;
            if (unit.SlowTimer <= 0f)
            {
                unit.SlowAmount = 0f;
            }
        }
        if (unit.WeakenAmount > 0f)
        {
            unit.WeakenTimer -= deltaTime;
            if (unit.WeakenTimer <= 0f)
            {
                unit.WeakenAmount = 0f;
            }
        }

        if (unit.BurnTimer > 0f)
        {
            unit.BurnTimer -= deltaTime;
            unit.BurnTickTimer -= deltaTime;
            while (unit.BurnTickTimer <= 0f && unit.IsAlive)
            {
                unit.BurnTickTimer += BurnTickInterval;
                TakeDamage(unit, Math.Max(1, BattleSimUnit.Round(unit.BurnDamage)), unit.BurnSource, DamageKind.Burn);
            }
            if (unit.BurnTimer <= 0f)
            {
                unit.BurnTimer = 0f;
                unit.BurnDamage = 0f;
                unit.BurnSource = null;
            }
        }
    }

    private void TickPeriodicSkill(BattleSimUnit unit, List<BattleSimUnit> allies, List<BattleSimUnit> opponents,
        float deltaTime)
    {
        // 스킬 칸마다 따로 (조합 크리처는 두 칸)
        for (int i = 0; i < unit.Skills.Length; i++)
        {
            SkillSlot skill = unit.Skills[i];
            if (skill.Type != SkillType.PeriodicSelfHeal && skill.Type != SkillType.PeriodicSlowAllEnemies
                && skill.Type != SkillType.TeamHealthAndRegen)
            {
                continue;
            }
            if (skill.Every <= 0f)
            {
                continue;
            }

            unit.SkillTimers[i] -= deltaTime;
            while (unit.SkillTimers[i] <= 0f && unit.IsAlive)
            {
                unit.SkillTimers[i] += skill.Every;
                switch (skill.Type)
                {
                    case SkillType.PeriodicSelfHeal:
                        Heal(unit, BattleSimUnit.Round(unit.MaxHealth * skill.Value1));
                        RaiseSkill(unit, unit, "회복");
                        break;
                    case SkillType.PeriodicSlowAllEnemies:
                        foreach (BattleSimUnit enemy in opponents)
                        {
                            if (enemy.IsAlive)
                            {
                                ApplySlow(enemy, skill.Value1, skill.Value2);
                            }
                        }
                        RaiseSkill(unit, unit, "둔화 파동");
                        break;
                    case SkillType.TeamHealthAndRegen:
                        foreach (BattleSimUnit ally in allies)
                        {
                            if (ally.IsAlive)
                            {
                                Heal(ally, BattleSimUnit.Round(ally.MaxHealth * skill.Value2));
                            }
                        }
                        // 1초마다라서 글은 띄우지 않고 발동만 센다
                        RaiseSkill(unit, unit, null);
                        break;
                }
            }
        }
    }

    private void TickAttacks(List<BattleSimUnit> attackers, List<BattleSimUnit> defenders, float deltaTime)
    {
        foreach (BattleSimUnit attacker in attackers)
        {
            // 기절·묶기 중에는 공격 대기 시간도 멈춘다
            if (!attacker.IsAlive || attacker.IsDisabled)
            {
                continue;
            }

            attacker.Cooldown -= deltaTime;
            while (attacker.Cooldown <= 0f)
            {
                // 대상이 없으면(상대 전멸) 이 팀의 남은 공격도 멈춘다 (이전과 같은 규칙)
                if (!PerformAttack(attacker, attackers, defenders))
                {
                    return;
                }
                attacker.Cooldown += attacker.CurrentInterval;
                if (!attacker.IsAlive || attacker.IsDisabled)
                {
                    break;
                }
            }
        }
    }

    // ---------- 공격 ----------

    // 대상이 없으면 false
    private bool PerformAttack(BattleSimUnit attacker, List<BattleSimUnit> allies, List<BattleSimUnit> defenders)
    {
        BattleSimUnit primary = FindAttackTarget(attacker, defenders);
        if (primary == null)
        {
            return false;
        }

        attacker.AttackCount++;
        Raise(BattleEventType.Attack, attacker, attacker, 0, null);
        SkillSlot[] skills = attacker.Skills;

        // 이번 공격의 피해 배율 (스킬 칸마다)
        float multiplier = 1f;
        foreach (SkillSlot skill in skills)
        {
            if (skill.Type == SkillType.FirstAttackDamage && attacker.AttackCount == 1)
            {
                multiplier *= skill.Value1;
                RaiseSkill(attacker, attacker, "첫 공격");
            }
            else if (skill.Type == SkillType.EveryNthAttackDamage && IsNthAttack(attacker, skill))
            {
                multiplier *= skill.Value1;
                RaiseSkill(attacker, attacker, "강타");
            }
        }
        // 용광로 (조합 시너지): 확률로 피해 배율. 조합 시너지 글은 Source 없이 (스킬 발동 수에 안 들어가게)
        if (attacker.CritChance > 0f && Roll(attacker.CritChance))
        {
            multiplier *= attacker.CritMultiplier;
            RaiseSkill(attacker, null, "치명");
        }

        // 대상: 보통 1마리, 동시 공격이면 앞줄부터 여러 마리
        _targets.Clear();
        if (attacker.HasSkill(SkillType.MultiTarget))
        {
            CollectTargets(defenders, Math.Max(1, (int)attacker.GetSkill(SkillType.MultiTarget).Value1), _targets);
            if (_targets.Count > 1)
            {
                RaiseSkill(attacker, attacker, null);
            }
        }
        else
        {
            _targets.Add(primary);
        }

        foreach (BattleSimUnit target in _targets)
        {
            Hit(attacker, target, multiplier);
            if (!attacker.IsAlive)
            {
                return true;
            }
        }

        foreach (SkillSlot skill in skills)
        {
            ApplyOnHitSkill(attacker, skill, primary, defenders);
        }
        // 뇌우 (조합 시너지): 확률로 대상 기절
        if (attacker.StunChance > 0f && primary.IsAlive && Roll(attacker.StunChance))
        {
            primary.StunTimer = Math.Max(primary.StunTimer, attacker.StunSeconds);
            RaiseSkill(primary, null, "기절");
        }
        foreach (SkillSlot skill in skills)
        {
            if (IsNthAttack(attacker, skill))
            {
                ApplyNthAttackSkill(attacker, skill, allies, defenders);
            }
        }

        // 한 번 더 공격 (추가 공격은 N번째 공격 수에 들어가지 않고, 다시 한 번 더 공격하지 않는다)
        if (attacker.HasSkill(SkillType.OnHitChanceExtraAttack) && attacker.IsAlive
            && Roll(attacker.GetSkill(SkillType.OnHitChanceExtraAttack).Chance))
        {
            BattleSimUnit extraTarget = FindAttackTarget(attacker, defenders);
            if (extraTarget != null)
            {
                RaiseSkill(attacker, attacker, "연속");
                Hit(attacker, extraTarget, 1f);
            }
        }
        return true;
    }

    // 이번 공격이 그 스킬의 Every번째 공격인지 (Every가 1 이상일 때)
    private static bool IsNthAttack(BattleSimUnit attacker, SkillSlot skill)
    {
        return skill.Every >= 1f && attacker.AttackCount % (int)skill.Every == 0;
    }

    // 공격 피해 1번 (공격력 × 상성 × 배율) + 반사
    private void Hit(BattleSimUnit attacker, BattleSimUnit target, float multiplier)
    {
        int damage = AttackDamage(attacker, target, multiplier);
        int dealt = TakeDamage(target, damage, attacker, DamageKind.Attack);

        // 반사(가시 갑옷, 수정고슴도치): 받은 피해의 일부를 공격자에게. 반사 피해는 다시 반사되지 않는다
        if (dealt > 0 && target.ThornsPercent > 0f && attacker.IsAlive)
        {
            if (target.HasSkill(SkillType.Thorns) || target.HasSkill(SkillType.DamageReductionThorns))
            {
                RaiseSkill(attacker, target, "반사");
            }
            TakeDamage(attacker, Math.Max(1, BattleSimUnit.Round(dealt * target.ThornsPercent)), target, DamageKind.Reflect);
        }
    }

    private int AttackDamage(BattleSimUnit attacker, BattleSimUnit target, float multiplier)
    {
        float typeMultiplier = _typeMultipliers[(int)attacker.Type, (int)target.Type];
        float damage = multiplier == 1f
            ? attacker.CurrentAttack * typeMultiplier
            : attacker.CurrentAttack * typeMultiplier * multiplier;
        return Math.Max(1, BattleSimUnit.Round(damage));
    }

    private void ApplyOnHitSkill(BattleSimUnit attacker, SkillSlot skill, BattleSimUnit target, List<BattleSimUnit> defenders)
    {
        switch (skill.Type)
        {
            case SkillType.OnHitSlow:
                if (target.IsAlive)
                {
                    bool isNew = target.SlowAmount < skill.Value1;
                    ApplySlow(target, skill.Value1, float.PositiveInfinity);
                    RaiseSkill(target, attacker, isNew ? "둔화" : null);
                }
                break;
            case SkillType.OnHitChanceRoot:
                if (target.IsAlive && Roll(skill.Chance))
                {
                    target.RootTimer = Math.Max(target.RootTimer, skill.Value2);
                    RaiseSkill(target, attacker, "묶기");
                }
                break;
            case SkillType.OnHitBurn:
                ApplyBurn(attacker, target, skill);
                break;
            case SkillType.OnHitBurnAll:
                // 피해 중에 목록이 바뀌지 않으므로 그대로 돈다 (화상은 다음 틱부터 피해)
                foreach (BattleSimUnit defender in defenders)
                {
                    ApplyBurn(attacker, defender, skill);
                }
                break;
            case SkillType.OnHitChanceStun:
                if (target.IsAlive && Roll(skill.Chance))
                {
                    target.StunTimer = Math.Max(target.StunTimer, skill.Value2);
                    RaiseSkill(target, attacker, "기절");
                }
                break;
            case SkillType.OnHitSplashAll:
                _skillTargets.Clear();
                foreach (BattleSimUnit defender in defenders)
                {
                    if (defender != target && defender.IsAlive)
                    {
                        _skillTargets.Add(defender);
                    }
                }
                for (int i = 0; i < _skillTargets.Count; i++)
                {
                    RaiseSkill(_skillTargets[i], attacker, "연쇄");
                    DealSkillDamage(attacker, _skillTargets[i], skill.Value1);
                }
                break;
            case SkillType.OnHitChanceWeaken:
                if (target.IsAlive && Roll(skill.Chance))
                {
                    ApplyWeaken(target, skill.Value1, skill.Value2);
                    RaiseSkill(target, attacker, "약화");
                }
                break;
            case SkillType.OnHitChanceSplash:
                if (Roll(skill.Chance))
                {
                    BattleSimUnit neighbor = FindNeighbor(defenders, target);
                    if (neighbor != null)
                    {
                        RaiseSkill(neighbor, attacker, "연쇄");
                        DealSkillDamage(attacker, neighbor, skill.Value1);
                    }
                }
                break;
        }
    }

    // 화상 (화산곰, 홍염제왕): 1초마다 공격력 ×Value1, Value2초 (다시 걸면 시간만 새로, 피해는 센 쪽)
    private void ApplyBurn(BattleSimUnit attacker, BattleSimUnit target, SkillSlot skill)
    {
        if (!target.IsAlive)
        {
            return;
        }
        bool wasBurning = target.BurnTimer > 0f;
        float damage = attacker.Attack * skill.Value1;
        if (damage >= target.BurnDamage)
        {
            target.BurnSource = attacker.HasSkill(SkillType.OnKillAttackUp) ? attacker : null;
        }
        target.BurnDamage = Math.Max(target.BurnDamage, damage);
        target.BurnTimer = skill.Value2;
        if (!wasBurning)
        {
            target.BurnTickTimer = BurnTickInterval;
        }
        RaiseSkill(target, attacker, wasBurning ? null : "화상");
    }

    private void ApplyNthAttackSkill(BattleSimUnit attacker, SkillSlot skill, List<BattleSimUnit> allies,
        List<BattleSimUnit> defenders)
    {
        switch (skill.Type)
        {
            case SkillType.EveryNthAttackHitAll:
                RaiseSkill(attacker, attacker, "광역");
                DamageAll(attacker, defenders, skill.Value1);
                break;
            case SkillType.EveryNthAttackHealLowest:
                BattleSimUnit lowest = FindLowestHealthRatio(allies);
                if (lowest != null)
                {
                    Heal(lowest, BattleSimUnit.Round(lowest.MaxHealth * skill.Value1));
                    RaiseSkill(lowest, attacker, "회복");
                }
                break;
            case SkillType.EveryNthAttackHitBackRow:
                // Value2 = 마릿수 (0이면 1마리)
                CollectBackRowTargets(defenders, Math.Max(1, (int)skill.Value2), _skillTargets);
                for (int i = 0; i < _skillTargets.Count; i++)
                {
                    RaiseSkill(_skillTargets[i], attacker, "저격");
                    DealSkillDamage(attacker, _skillTargets[i], skill.Value1);
                }
                break;
            case SkillType.EveryNthAttackHitAllHealTeam:
                RaiseSkill(attacker, attacker, "해일");
                DamageAll(attacker, defenders, skill.Value1);
                HealAll(allies, skill.Value2);
                break;
            case SkillType.EveryNthAttackSlowAll:
                foreach (BattleSimUnit defender in defenders)
                {
                    if (defender.IsAlive)
                    {
                        ApplySlow(defender, skill.Value1, skill.Value2);
                    }
                }
                RaiseSkill(attacker, attacker, "둔화 파동");
                break;
        }
    }

    private void DamageAll(BattleSimUnit attacker, List<BattleSimUnit> defenders, float attackRatio)
    {
        // 피해 중에 목록이 바뀌지 않도록 살아 있는 대상을 먼저 모은다
        _targets.Clear();
        foreach (BattleSimUnit defender in defenders)
        {
            if (defender.IsAlive)
            {
                _targets.Add(defender);
            }
        }
        for (int i = 0; i < _targets.Count; i++)
        {
            DealSkillDamage(attacker, _targets[i], attackRatio);
        }
    }

    // 스킬 피해: 공격력 × 비율 × 상성 (반사되지 않음)
    private void DealSkillDamage(BattleSimUnit attacker, BattleSimUnit target, float attackRatio)
    {
        if (!target.IsAlive)
        {
            return;
        }
        TakeDamage(target, AttackDamage(attacker, target, attackRatio), attacker, DamageKind.Skill);
    }

    // ---------- 피해·회복 ----------

    // 실제로 받은 피해(피해 감소 후, 보호막에 막힌 양 포함)를 돌려준다
    private int TakeDamage(BattleSimUnit target, int damage, BattleSimUnit source, DamageKind kind)
    {
        if (!target.IsAlive)
        {
            return 0;
        }

        // 철벽 (보스 특수 능력): 모든 피해 무시 (반사도 없음)
        if (!target.IsPlayer && IsBossInvulnerable)
        {
            return 0;
        }

        // 모래두더지: 처음 받는 공격 피해 무시 (Value1 = 무시할 공격 수, 0이면 1번)
        if (kind == DamageKind.Attack && target.HasSkill(SkillType.IgnoreFirstHit)
            && target.IgnoredHits < Math.Max(1, (int)target.GetSkill(SkillType.IgnoreFirstHit).Value1))
        {
            target.IgnoredHits++;
            RaiseSkill(target, target, "무시");
            return 0;
        }

        int reduced = Math.Max(1, BattleSimUnit.Round(damage * (1f - target.DamageReduction)));
        if (target.DamageReduction > 0f && (target.HasSkill(SkillType.DamageReduction)
                                            || target.HasSkill(SkillType.DamageReductionDeathStun)
                                            || target.HasSkill(SkillType.DamageReductionThorns)))
        {
            RaiseSkill(target, target, null);
        }

        int toHealth = reduced;
        if (target.Shield > 0)
        {
            int absorbed = Math.Min(target.Shield, reduced);
            target.Shield -= absorbed;
            toHealth -= absorbed;
        }
        target.Health = Math.Max(0, target.Health - toHealth);
        Raise(BattleEventType.Damage, target, source, reduced, null);

        if (!target.IsAlive)
        {
            HandleDeath(target, source);
            return reduced;
        }

        CheckLowHealth(target);
        return reduced;
    }

    // 체력이 처음 일정 비율 이하가 될 때 (트레이너 숲지기, 화로곰, 자갈갑옷)
    private void CheckLowHealth(BattleSimUnit unit)
    {
        // 숲지기: 이전과 같이 "아래로" (<)
        if (!unit.EmergencyHealUsed && unit.EmergencyHealPercent > 0f
                                    && unit.Health < unit.MaxHealth * unit.EmergencyHealThreshold)
        {
            unit.EmergencyHealUsed = true;
            Heal(unit, BattleSimUnit.Round(unit.MaxHealth * unit.EmergencyHealPercent));
        }

        if (unit.LowHealthTriggered)
        {
            return;
        }
        foreach (SkillSlot skill in unit.Skills)
        {
            if (skill.Type != SkillType.LowHealthAttackUp && skill.Type != SkillType.LowHealthShield)
            {
                continue;
            }
            if (unit.Health > unit.MaxHealth * skill.Value1)
            {
                return;
            }

            unit.LowHealthTriggered = true;
            if (skill.Type == SkillType.LowHealthAttackUp)
            {
                RaiseSkill(unit, unit, "분노");
            }
            else
            {
                AddShield(unit, BattleSimUnit.Round(unit.MaxHealth * skill.Value2));
                RaiseSkill(unit, unit, "보호막");
            }
            return;
        }
    }

    private void HandleDeath(BattleSimUnit unit, BattleSimUnit killer)
    {
        // 태양불사조: 처음 쓰러지면 한 번 부활
        if (unit.HasSkill(SkillType.Revive) && !unit.Revived)
        {
            unit.Revived = true;
            unit.Health = Math.Max(1, BattleSimUnit.Round(unit.MaxHealth * unit.GetSkill(SkillType.Revive).Value1));
            ClearStatus(unit);
            Raise(BattleEventType.Revive, unit, unit, unit.Health, null);
            RaiseSkill(unit, unit, "부활");
            return;
        }

        List<BattleSimUnit> allies = unit.IsPlayer ? _players : _enemies;
        List<BattleSimUnit> opponents = unit.IsPlayer ? _enemies : _players;

        // 녹림여제: 살아 있는 동안 쓰러진 다른 아군 1마리를 부활 (전투당 1번)
        foreach (BattleSimUnit ally in allies)
        {
            if (ally != unit && ally.IsAlive && !ally.AllyReviveUsed && ally.HasSkill(SkillType.ReviveAlly))
            {
                ally.AllyReviveUsed = true;
                unit.Health = Math.Max(1, BattleSimUnit.Round(unit.MaxHealth * ally.GetSkill(SkillType.ReviveAlly).Value1));
                ClearStatus(unit);
                Raise(BattleEventType.Revive, unit, ally, unit.Health, null);
                RaiseSkill(unit, ally, "부활");
                return;
            }
        }

        ClearStatus(unit);
        Raise(BattleEventType.Death, unit, killer, 0, null);

        foreach (SkillSlot skill in unit.Skills)
        {
            switch (skill.Type)
            {
                case SkillType.OnDeathHealTeam:
                    RaiseSkill(unit, unit, "치유");
                    HealAll(allies, skill.Value1);
                    break;
                case SkillType.DamageReductionDeathStun:
                    RaiseSkill(unit, unit, "지진");
                    foreach (BattleSimUnit enemy in opponents)
                    {
                        if (enemy.IsAlive)
                        {
                            enemy.StunTimer = Math.Max(enemy.StunTimer, skill.Value2);
                            RaiseSkill(enemy, unit, "기절");
                        }
                    }
                    break;
            }
        }

        // 불꽃늑대: 적을 쓰러뜨리면 공격 속도 누적 증가
        if (killer != null && killer.IsAlive && killer.HasSkill(SkillType.OnKillAttackSpeed) && killer.IsPlayer != unit.IsPlayer)
        {
            killer.KillStacks++;
            RaiseSkill(killer, killer, "가속");
        }
        // 홍염제왕: 적을 쓰러뜨리면 공격력 누적 증가
        if (killer != null && killer.IsAlive && killer.HasSkill(SkillType.OnKillAttackUp) && killer.IsPlayer != unit.IsPlayer)
        {
            killer.KillAttackStacks++;
            RaiseSkill(killer, killer, "공격력 증가");
        }
    }

    private void Heal(BattleSimUnit unit, int amount)
    {
        if (!unit.IsAlive)
        {
            return;
        }
        int before = unit.Health;
        unit.Health = Math.Min(unit.MaxHealth, unit.Health + Math.Max(1, amount));
        Raise(BattleEventType.Heal, unit, unit, unit.Health - before, null);
    }

    private void HealAll(List<BattleSimUnit> allies, float maxHealthRatio)
    {
        foreach (BattleSimUnit ally in allies)
        {
            if (ally.IsAlive)
            {
                Heal(ally, BattleSimUnit.Round(ally.MaxHealth * maxHealthRatio));
            }
        }
    }

    private void AddShield(BattleSimUnit unit, int amount)
    {
        if (!unit.IsAlive || amount <= 0)
        {
            return;
        }
        unit.Shield += amount;
        Raise(BattleEventType.Shield, unit, unit, amount, null);
    }

    // 둔화·약화: 중첩하지 않고 더 센 쪽만 (같거나 세면 시간 새로)
    private static void ApplySlow(BattleSimUnit unit, float amount, float duration)
    {
        if (amount < unit.SlowAmount)
        {
            return;
        }
        unit.SlowAmount = amount;
        unit.SlowTimer = Math.Max(unit.SlowTimer, duration);
    }

    private static void ApplyWeaken(BattleSimUnit unit, float amount, float duration)
    {
        if (amount < unit.WeakenAmount)
        {
            return;
        }
        unit.WeakenAmount = amount;
        unit.WeakenTimer = Math.Max(unit.WeakenTimer, duration);
    }

    private static void ClearStatus(BattleSimUnit unit)
    {
        unit.StunTimer = 0f;
        unit.RootTimer = 0f;
        unit.BurnTimer = 0f;
        unit.BurnDamage = 0f;
        unit.BurnSource = null;
        unit.SlowAmount = 0f;
        unit.SlowTimer = 0f;
        unit.WeakenAmount = 0f;
        unit.WeakenTimer = 0f;
    }

    // ---------- 전투 시작 스킬 ----------

    private void ApplyStartSkills(List<BattleSimUnit> allies, List<BattleSimUnit> opponents, bool maxHealthPhase)
    {
        foreach (BattleSimUnit unit in allies)
        {
            if (!unit.IsAlive)
            {
                continue;
            }
            foreach (SkillSlot skill in unit.Skills)
            {
                ApplyStartSkill(unit, skill, allies, opponents, maxHealthPhase);
            }
        }
    }

    private void ApplyStartSkill(BattleSimUnit unit, SkillSlot skill, List<BattleSimUnit> allies,
        List<BattleSimUnit> opponents, bool maxHealthPhase)
    {
        switch (skill.Type)
        {
            case SkillType.BattleStartTeamMaxHealth:
            case SkillType.TeamHealthAndRegen:
                if (maxHealthPhase)
                {
                    foreach (BattleSimUnit ally in allies)
                    {
                        if (ally.IsAlive)
                        {
                            ally.MaxHealth = BattleSimUnit.Round(ally.MaxHealth * (1f + skill.Value1));
                            ally.Health = ally.MaxHealth;
                        }
                    }
                    RaiseSkill(unit, unit, "체력 증가");
                }
                break;
            case SkillType.AttackSpeedUp:
                // 상시 효과 (공격 간격에 이미 반영). 발동만 센다
                if (maxHealthPhase)
                {
                    RaiseSkill(unit, unit, null);
                }
                break;
            case SkillType.BattleStartTeamShield:
                if (!maxHealthPhase)
                {
                    foreach (BattleSimUnit ally in allies)
                    {
                        AddShield(ally, BattleSimUnit.Round(ally.MaxHealth * skill.Value1));
                        if (ally.IsAlive)
                        {
                            RaiseSkill(ally, unit, "보호막");
                        }
                    }
                }
                break;
            case SkillType.BattleStartSelfShield:
                if (!maxHealthPhase)
                {
                    AddShield(unit, BattleSimUnit.Round(unit.MaxHealth * skill.Value1));
                    RaiseSkill(unit, unit, "보호막");
                }
                break;
            case SkillType.BattleStartHaste:
                if (!maxHealthPhase)
                {
                    unit.HasteTimer = skill.Value2;
                    // 첫 공격까지의 대기 시간에도 가속 적용
                    unit.Cooldown = unit.CurrentInterval;
                    RaiseSkill(unit, unit, "가속");
                }
                break;
            case SkillType.BattleStartStunFront:
                // Value2 = 마릿수 (0이면 1마리), 앞줄부터
                if (!maxHealthPhase)
                {
                    _skillTargets.Clear();
                    CollectTargets(opponents, Math.Max(1, (int)skill.Value2), _skillTargets);
                    foreach (BattleSimUnit front in _skillTargets)
                    {
                        front.StunTimer = Math.Max(front.StunTimer, skill.Value1);
                        RaiseSkill(front, unit, "기절");
                    }
                }
                break;
            case SkillType.BattleStartRootBack:
                // Value2 = 마릿수 (0이면 1마리), 뒷줄부터
                if (!maxHealthPhase)
                {
                    CollectBackRowTargets(opponents, Math.Max(1, (int)skill.Value2), _skillTargets);
                    foreach (BattleSimUnit back in _skillTargets)
                    {
                        back.RootTimer = Math.Max(back.RootTimer, skill.Value1);
                        RaiseSkill(back, unit, "묶기");
                    }
                }
                break;
        }
    }

    // 조합 시너지 시작 효과: 수정숲 보호막, 무지개 가속
    private void ApplyStartSynergy(List<BattleSimUnit> allies)
    {
        foreach (BattleSimUnit unit in allies)
        {
            if (!unit.IsAlive)
            {
                continue;
            }
            BattleBonus bonus = unit.Setup.Bonus;
            if (bonus.StartShieldPercent > 0f)
            {
                AddShield(unit, BattleSimUnit.Round(unit.MaxHealth * bonus.StartShieldPercent));
                RaiseSkill(unit, null, "보호막");
            }
            if (bonus.StartHasteBonus > 0f && bonus.StartHasteSeconds > 0f)
            {
                unit.SynergyHasteTimer = bonus.StartHasteSeconds;
                // 첫 공격까지의 대기 시간에도 가속 적용
                unit.Cooldown = unit.CurrentInterval;
            }
        }
    }

    // 증기화산 (조합 시너지): 팀에서 가장 큰 비율로 적 전체에 한 번 (최대 체력 비율, 피해 감소·보호막 적용)
    private void ApplyStartEnemyDamage(List<BattleSimUnit> allies, List<BattleSimUnit> opponents)
    {
        float percent = 0f;
        foreach (BattleSimUnit unit in allies)
        {
            if (unit.IsAlive)
            {
                percent = Math.Max(percent, unit.Setup.Bonus.StartEnemyDamagePercent);
            }
        }
        if (percent <= 0f)
        {
            return;
        }

        _targets.Clear();
        foreach (BattleSimUnit opponent in opponents)
        {
            if (opponent.IsAlive)
            {
                _targets.Add(opponent);
            }
        }
        for (int i = 0; i < _targets.Count; i++)
        {
            BattleSimUnit target = _targets[i];
            TakeDamage(target, Math.Max(1, BattleSimUnit.Round(target.MaxHealth * percent)), null, DamageKind.Skill);
        }
    }

    // ---------- 대상 찾기 ----------

    // 일반 공격 대상. 사냥꾼(보스 능력)이면 적은 가장 비싼 아군(골드 × 별, 같으면 앞 칸)부터
    private BattleSimUnit FindAttackTarget(BattleSimUnit attacker, List<BattleSimUnit> defenders)
    {
        if (!_boss.Hunter || attacker.IsPlayer)
        {
            return FindTarget(defenders);
        }
        BattleSimUnit best = null;
        foreach (BattleSimUnit unit in defenders)
        {
            if (unit.IsAlive && (best == null || unit.Setup.Tier * unit.Setup.Level > best.Setup.Tier * best.Setup.Level))
            {
                best = unit;
            }
        }
        return best;
    }

    // 살아 있는 앞줄 중 앞 칸부터, 앞줄이 없으면 뒷줄 (목록은 칸 순서)
    private static BattleSimUnit FindTarget(List<BattleSimUnit> defenders)
    {
        BattleSimUnit backRowTarget = null;
        foreach (BattleSimUnit unit in defenders)
        {
            if (!unit.IsAlive)
            {
                continue;
            }
            if (unit.IsFrontRow)
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

    // 뒷줄의 살아 있는 앞 칸부터 count마리. 뒷줄이 없으면 FindTarget 1마리
    private static void CollectBackRowTargets(List<BattleSimUnit> defenders, int count, List<BattleSimUnit> result)
    {
        result.Clear();
        foreach (BattleSimUnit unit in defenders)
        {
            if (result.Count < count && unit.IsAlive && !unit.IsFrontRow)
            {
                result.Add(unit);
            }
        }
        if (result.Count == 0)
        {
            BattleSimUnit front = FindTarget(defenders);
            if (front != null)
            {
                result.Add(front);
            }
        }
    }

    // 공격 순서대로 count마리 (앞줄 칸 순서 → 뒷줄 칸 순서)
    private static void CollectTargets(List<BattleSimUnit> defenders, int count, List<BattleSimUnit> result)
    {
        foreach (BattleSimUnit unit in defenders)
        {
            if (result.Count < count && unit.IsAlive && unit.IsFrontRow)
            {
                result.Add(unit);
            }
        }
        foreach (BattleSimUnit unit in defenders)
        {
            if (result.Count < count && unit.IsAlive && !unit.IsFrontRow)
            {
                result.Add(unit);
            }
        }
    }

    // 옆 적: 같은 줄에서 칸 번호가 가장 가까운 살아 있는 적 (같으면 작은 칸)
    private static BattleSimUnit FindNeighbor(List<BattleSimUnit> defenders, BattleSimUnit target)
    {
        BattleSimUnit best = null;
        int bestDistance = int.MaxValue;
        foreach (BattleSimUnit unit in defenders)
        {
            if (unit == target || !unit.IsAlive || unit.IsFrontRow != target.IsFrontRow)
            {
                continue;
            }
            int distance = Math.Abs(unit.SlotIndex - target.SlotIndex);
            if (distance < bestDistance)
            {
                best = unit;
                bestDistance = distance;
            }
        }
        return best;
    }

    private static BattleSimUnit FindLowestHealthRatio(List<BattleSimUnit> allies)
    {
        BattleSimUnit lowest = null;
        float lowestRatio = float.MaxValue;
        foreach (BattleSimUnit unit in allies)
        {
            if (!unit.IsAlive)
            {
                continue;
            }
            float ratio = (float)unit.Health / unit.MaxHealth;
            if (ratio < lowestRatio)
            {
                lowest = unit;
                lowestRatio = ratio;
            }
        }
        return lowest;
    }

    private static bool AnyAlive(List<BattleSimUnit> units)
    {
        foreach (BattleSimUnit unit in units)
        {
            if (unit.IsAlive)
            {
                return true;
            }
        }
        return false;
    }

    // ---------- 기타 ----------

    private bool Roll(float chance)
    {
        return chance > 0f && _random.NextDouble() < chance;
    }

    private void RaiseSkill(BattleSimUnit unit, BattleSimUnit source, string text)
    {
        Raise(BattleEventType.Skill, unit, source, 0, text);
    }

    private void Raise(BattleEventType type, BattleSimUnit unit, BattleSimUnit source, int amount, string text)
    {
        Event?.Invoke(new BattleEvent { Type = type, Unit = unit, Source = source, Amount = amount, Text = text });
    }
}
