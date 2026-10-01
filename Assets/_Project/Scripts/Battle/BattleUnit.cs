using UnityEngine;

// 전투 중 크리처 한 마리의 상태
public class BattleUnit
{
    public CreatureData Data { get; }
    // 0~1 앞줄, 2~4 뒷줄
    public int SlotIndex { get; }
    public int MaxHealth { get; }
    public int Health { get; private set; }
    public int Attack { get; }
    public float AttackInterval { get; }
    public float Cooldown { get; set; }
    public bool IsAlive => Health > 0;

    private readonly CreatureUnitView _view;

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
        _view.Show(data);
    }

    public void TakeDamage(int damage)
    {
        Health = Mathf.Max(0, Health - damage);
        _view.SetHealth((float)Health / MaxHealth);
        if (!IsAlive)
        {
            _view.Hide();
        }
    }
}
