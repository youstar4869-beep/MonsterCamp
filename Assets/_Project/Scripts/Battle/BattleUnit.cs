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
    // 장착 아이템 (적은 null)
    public ItemData Item { get; }
    // 받은 피해의 이 비율만큼 공격자에게 되돌림 (가시 갑옷)
    public float ThornsPercent { get; private set; }

    private readonly CreatureUnitView _view;
    private float _damageReduction;
    private float _regenPercent;
    private float _regenInterval;
    private float _regenTimer;
    private float _emergencyHealThreshold;
    private float _emergencyHealPercent;
    private bool _emergencyHealUsed;

    public BattleUnit(CreatureData data, int slotIndex, int level, ItemData item, CreatureUnitView view)
    {
        Data = data;
        SlotIndex = slotIndex;
        Item = item;
        MaxHealth = data.GetMaxHealth(level);
        Health = MaxHealth;
        Attack = data.GetAttack(level);
        AttackInterval = 1f / data.AttackSpeed;
        Cooldown = AttackInterval;

        _view = view;
        _view.Show(data, level, item);
    }

    // 전투 시작 직후 한 번 적용 (체력은 가득 찬 상태 유지)
    public void ApplyBonus(BattleBonus bonus)
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
        _emergencyHealThreshold = bonus.EmergencyHealThreshold;
        _emergencyHealPercent = bonus.EmergencyHealPercent;
        _emergencyHealUsed = false;
        ThornsPercent = bonus.ThornsPercent;
    }

    // 실제로 받은 피해(피해 감소 적용 후)를 돌려준다
    public int TakeDamage(int damage)
    {
        int reduced = Mathf.Max(1, Mathf.RoundToInt(damage * (1f - _damageReduction)));
        Health = Mathf.Max(0, Health - reduced);
        if (!IsAlive)
        {
            _view.Hide();
            return reduced;
        }

        // 숲지기: 처음 일정 체력 아래로 떨어지면 한 번 회복
        if (!_emergencyHealUsed && _emergencyHealPercent > 0f && Health < MaxHealth * _emergencyHealThreshold)
        {
            _emergencyHealUsed = true;
            Heal(Mathf.RoundToInt(MaxHealth * _emergencyHealPercent));
            return reduced;
        }
        _view.SetHealth((float)Health / MaxHealth);
        return reduced;
    }

    private void Heal(int amount)
    {
        Health = Mathf.Min(MaxHealth, Health + Mathf.Max(1, amount));
        _view.SetHealth((float)Health / MaxHealth);
    }

    // 풀 시너지·회복 열매: 일정 시간마다 최대 체력의 일정 비율 회복
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
            Heal(Mathf.RoundToInt(MaxHealth * _regenPercent));
        }
    }
}
