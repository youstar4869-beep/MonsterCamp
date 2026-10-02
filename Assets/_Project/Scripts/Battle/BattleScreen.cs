using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 전투 화면: 계산은 BattleSimulation(화면 없는 일반 C#)이 하고, 여기서는 매 프레임 Tick하고
// 결과 이벤트(피해, 회복, 사망, 스킬)를 받아 보여 주기만 한다
public class BattleScreen : MonoBehaviour
{
    // 데미지·회복 숫자 크기: 상성 유리는 크게, 불리는 작게 (색은 UITheme)
    private const float DamageScale = 1.1f;
    private const float StrongDamageScale = 1.5f;
    private const float WeakDamageScale = 0.8f;
    private const float HealScale = 0.9f;
    // 숫자 글은 미리 만들어 두고 돌려 쓴다 (맞을 때마다 새 문자열을 만들지 않게)
    private const int CachedNumberCount = 1000;
    private static readonly string[] DamageTexts = new string[CachedNumberCount];
    private static readonly string[] HealTexts = new string[CachedNumberCount];

    [SerializeField] private RunManager _run;
    [SerializeField] private TypeChart _typeChart;
    [SerializeField] private SynergyConfig _synergy;
    [Tooltip("데미지 숫자·스킬 글 색")]
    [SerializeField] private UITheme _theme;
    [Tooltip("0~1 앞줄, 2~4 뒷줄")]
    [SerializeField] private CreatureUnitView[] _playerViews;
    [SerializeField] private CreatureUnitView[] _enemyViews;
    [Tooltip("스킬 발동 시 크리처 위에 뜨는 짧은 글")]
    [SerializeField] private FloatingTextPool _floatingTexts;
    [Tooltip("적 팀 이름 (보스면 '보스:' 표시)")]
    [SerializeField] private Text _enemyLabel;
    [SerializeField] private Text _bannerText;
    [SerializeField] private Text _speedLabel;
    [Tooltip("승리/패배 표시 후 다음으로 넘어가기까지 (초)")]
    [SerializeField] private float _resultDelay = 1.5f;
    [SerializeField] private float _fastSpeed = 2f;
    [Tooltip("전투마다 시드를 Console에 찍는다 (같은 시드면 같은 결과)")]
    [SerializeField] private bool _logSeed;

    private readonly List<BattleSetupFactory.Member> _playerMembers = new List<BattleSetupFactory.Member>();
    private readonly List<BattleSetupFactory.Member> _enemyMembers = new List<BattleSetupFactory.Member>();
    private readonly Dictionary<BattleSimUnit, CreatureUnitView> _unitViews = new Dictionary<BattleSimUnit, CreatureUnitView>();
    private BattleSimulation _simulation;
    private float[,] _typeMultipliers;
    private float _speed = 1f;
    private bool _running;

    // RunManager에서 호출. playerTeam은 팀 칸 순서 (빈 칸은 null).
    // 매 전투 새로 계산하므로 체력은 항상 가득 찬 상태로 시작한다.
    // trainer는 내 팀에만 적용 (적 팀은 시너지만)
    public void StartBattle(CreatureInstance[] playerTeam, EnemyTeamData enemyTeam, TrainerData trainer)
    {
        StopAllCoroutines();
        _floatingTexts.HideAll();
        _typeMultipliers ??= BattleSetupFactory.CreateTypeMultipliers(_typeChart);

        _playerMembers.Clear();
        for (int i = 0; i < _playerViews.Length; i++)
        {
            CreatureInstance creature = i < playerTeam.Length ? playerTeam[i] : null;
            AddMember(_playerMembers, creature?.Data, creature?.Level ?? 1, creature?.Item, i, _playerViews);
        }

        _enemyMembers.Clear();
        for (int i = 0; i < _enemyViews.Length; i++)
        {
            EnemyTeamData.Member member = i < enemyTeam.SlotCount ? enemyTeam.GetMember(i) : default;
            AddMember(_enemyMembers, member.Creature, Mathf.Max(1, member.Level), null, i, _enemyViews);
        }

        // 타입 시너지는 내 팀·적 팀 똑같이, 트레이너 능력·아이템은 내 팀만 (시너지와 겹쳐서 적용)
        List<BattleUnitSetup> players = BattleSetupFactory.CreateTeam(_playerMembers, _synergy, trainer);
        List<BattleUnitSetup> enemies = BattleSetupFactory.CreateTeam(_enemyMembers, _synergy, null);

        int seed = System.Environment.TickCount;
        if (_logSeed)
        {
            Debug.Log($"[전투] 시드 {seed}");
        }
        _simulation = new BattleSimulation(players, enemies, _typeMultipliers, seed);
        _simulation.Event += OnBattleEvent;

        _unitViews.Clear();
        BindViews(_simulation.Players, _playerMembers, _playerViews);
        BindViews(_simulation.Enemies, _enemyMembers, _enemyViews);

        _simulation.Start();
        RefreshAllViews();

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

        _simulation.Tick(Time.deltaTime * _speed);

        // 기절·묶기 중에는 흐리게
        foreach (KeyValuePair<BattleSimUnit, CreatureUnitView> pair in _unitViews)
        {
            if (pair.Key.IsAlive)
            {
                pair.Value.SetDisabled(pair.Key.IsDisabled);
            }
        }

        if (_simulation.IsOver)
        {
            StartCoroutine(Finish(_simulation.PlayerWon));
        }
    }

    // ---------- 결과 이벤트 → 화면 ----------

    private void OnBattleEvent(BattleEvent battleEvent)
    {
        if (!_unitViews.TryGetValue(battleEvent.Unit, out CreatureUnitView view))
        {
            return;
        }

        switch (battleEvent.Type)
        {
            case BattleEventType.Attack:
                view.PlayAttack();
                break;
            case BattleEventType.Damage:
                RefreshView(battleEvent.Unit, view);
                view.PlayHit();
                ShowDamage(battleEvent, view);
                break;
            case BattleEventType.Heal:
                RefreshView(battleEvent.Unit, view);
                if (battleEvent.Amount > 0)
                {
                    _floatingTexts.Show(NumberText(HealTexts, "+", battleEvent.Amount), view.HitTextPosition, _theme.Heal, HealScale);
                }
                break;
            case BattleEventType.Shield:
                RefreshView(battleEvent.Unit, view);
                break;
            case BattleEventType.Revive:
                view.PlayRevive();
                RefreshView(battleEvent.Unit, view);
                break;
            case BattleEventType.Death:
                view.PlayDeath();
                break;
            case BattleEventType.Skill:
                if (!string.IsNullOrEmpty(battleEvent.Text) && view.gameObject.activeInHierarchy)
                {
                    _floatingTexts.Show(battleEvent.Text, view.SkillTextPosition, ColorOf(battleEvent.Text));
                }
                break;
        }
    }

    // 데미지 숫자: 때린 크리처와 맞은 크리처의 상성으로 크기·색 (화상 등 때린 쪽이 없으면 보통)
    private void ShowDamage(BattleEvent battleEvent, CreatureUnitView view)
    {
        float multiplier = battleEvent.Source != null
            ? _typeMultipliers[(int)battleEvent.Source.Type, (int)battleEvent.Unit.Type]
            : 1f;
        Color color = multiplier > 1f ? _theme.DamageStrong : multiplier < 1f ? _theme.DamageWeak : _theme.DamageNormal;
        float scale = multiplier > 1f ? StrongDamageScale : multiplier < 1f ? WeakDamageScale : DamageScale;
        _floatingTexts.Show(NumberText(DamageTexts, "", battleEvent.Amount), view.HitTextPosition, color, scale);
    }

    private static string NumberText(string[] cache, string prefix, int value)
    {
        if (value < 0 || value >= cache.Length)
        {
            return prefix + value;
        }
        return cache[value] ??= prefix + value;
    }

    // 상태이상은 빨강, 회복·보호막·강화는 초록, 그 밖의 스킬은 노랑
    private Color ColorOf(string text)
    {
        switch (text)
        {
            case "기절":
            case "묶기":
            case "화상":
            case "둔화":
            case "약화":
                return _theme.SkillDebuff;
            case "회복":
            case "치유":
            case "보호막":
            case "부활":
            case "가속":
            case "체력 증가":
            case "분노":
                return _theme.SkillBuff;
            default:
                return _theme.Skill;
        }
    }

    private void RefreshAllViews()
    {
        foreach (KeyValuePair<BattleSimUnit, CreatureUnitView> pair in _unitViews)
        {
            RefreshView(pair.Key, pair.Value);
        }
    }

    private static void RefreshView(BattleSimUnit unit, CreatureUnitView view)
    {
        view.SetHealth((float)unit.Health / unit.MaxHealth, (float)unit.Shield / unit.MaxHealth);
    }

    // ---------- 준비 ----------

    // 빈 칸(creature == null)이면 칸만 비워 둔다. 크리처가 있으면 칸 순서대로 넣는다
    private static void AddMember(List<BattleSetupFactory.Member> members, CreatureData creature, int level, ItemData item,
        int slotIndex, CreatureUnitView[] views)
    {
        if (creature == null)
        {
            views[slotIndex].Hide();
            return;
        }
        members.Add(new BattleSetupFactory.Member { Creature = creature, Level = level, Item = item, SlotIndex = slotIndex });
        views[slotIndex].Show(creature, level, item);
    }

    private void BindViews(IReadOnlyList<BattleSimUnit> units, List<BattleSetupFactory.Member> members, CreatureUnitView[] views)
    {
        for (int i = 0; i < units.Count; i++)
        {
            _unitViews[units[i]] = views[members[i].SlotIndex];
        }
    }

    private IEnumerator Finish(bool won)
    {
        _running = false;
        _bannerText.text = won ? "승리!" : "패배... 목숨 -1";
        yield return new WaitForSeconds(_resultDelay);
        _run.OnBattleEnd(won);
    }

    private void UpdateSpeedLabel()
    {
        _speedLabel.text = _speed > 1f ? $"{_fastSpeed:0.#}배속" : "1배속";
    }
}
