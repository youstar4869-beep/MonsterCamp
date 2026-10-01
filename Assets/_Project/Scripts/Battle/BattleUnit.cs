using UnityEngine;

// 전투 중 크리처 한 마리의 상태
public class BattleUnit
{
    public CreatureData Data { get; }
    // 0~1 앞줄, 2~4 뒷줄
    public int SlotIndex { get; }
    public int MaxHealth { get; private set; }
    public int Health { get; private set; }
    public int Attack { get; private set; }
    public float AttackInterval { get; private set; }
    public float Cooldown { get; set; }
    public bool IsAlive => Health > 0;

    private readonly CreatureUnitView _view;
    private float _damageReduction;
    private float _regenPercent;
    private float _regenInterval;
    private float _regenTimer;

    public BattleUnit(CreatureData data, int slotIndex, int level, CreatureUnitView view)
    {
        Data = data;
        SlotIndex = slotIndex;
        MaxHealth = data.GetMaxHealth(level);
        Health = MaxHealth;
        Attack = data.GetAttack(level);
        AttackInterval = 1f / data.AttackSpeed;
        Cooldown = AttackInterval;

        _view = view;
        _view.Show(data, level);
    }

    // 전투 시작 직후 한 번 적용 (체력은 가득 찬 상태 유지)
    public void ApplySynergy(SynergyBonus bonus)
    {
        MaxHealth = Mathf.RoundToInt(MaxHealth * (1f + bonus.HealthBonus));
        Health = MaxHealth;
        Attack = Mathf.RoundToInt(Attack * (1f + bonus.AttackBonus));
        AttackInterval /= 1f + bonus.SpeedBonus;
        Cooldown = AttackInterval;
        _damageReduction = bonus.DamageReduction;
        _regenPercent = bonus.RegenPercent;
        _regenInterval = bonus.RegenInterval;
        _regenTimer = _regenInterval;
    }

    public void TakeDamage(int damage)
    {
        int reduced = Mathf.Max(1, Mathf.RoundToInt(damage * (1f - _damageReduction)));
        Health = Mathf.Max(0, Health - reduced);
        _view.SetHealth((float)Health / MaxHealth);
        if (!IsAlive)
        {
            _view.Hide();
        }
    }

    // 풀 시너지: 일정 시간마다 최대 체력의 일정 비율 회복
    public void TickRegen(float deltaTime)
    {
        if (_regenPercent <= 0f || !IsAlive)
        {
            return;
        }

        _regenTimer -= deltaTime;
        while (_regenTimer <= 0f)
        {
            _regenTimer += _regenInterval;
            int amount = Mathf.Max(1, Mathf.RoundToInt(MaxHealth * _regenPercent));
            Health = Mathf.Min(MaxHealth, Health + amount);
            _view.SetHealth((float)Health / MaxHealth);
        }
    }
}
