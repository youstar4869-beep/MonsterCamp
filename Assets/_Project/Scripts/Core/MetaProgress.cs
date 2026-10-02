using System;
using UnityEngine;

// 판이 끝나도 남는 진행: 별, 해금 (meta.json)
public class MetaProgress : MonoBehaviour
{
    [SerializeField] private MetaConfig _config;

    private MetaSaveData _data;

    // 별·해금이 바뀌면 (도감 화면 갱신용)
    public event Action Changed;

    public MetaConfig Config => _config;
    public int Stars => Data.stars;

    // 다른 화면의 OnEnable이 먼저 불려도 쓸 수 있게 처음 쓸 때 읽는다
    private MetaSaveData Data => _data ??= SaveSystem.LoadMeta();

    private void Awake()
    {
        // meta.json이 없으면 기본값으로 만들어 둔다 (깨졌으면 백업 후 새로)
        SaveSystem.SaveMeta(Data);
    }

    // 해금에 필요한 별. 0이면 열려 있음
    public int GetCost(TrainerData trainer)
    {
        return Data.unlockedTrainers.Contains(trainer.name) ? 0 : _config.GetCost(trainer);
    }

    public int GetCost(CreatureData creature)
    {
        return Data.unlockedCreatures.Contains(creature.name) ? 0 : _config.GetCost(creature);
    }

    public bool IsUnlocked(TrainerData trainer) => GetCost(trainer) == 0;
    public bool IsUnlocked(CreatureData creature) => GetCost(creature) == 0;

    // 별이 충분하면 별을 쓰고 해금. 이미 열려 있거나 해금하면 true
    public bool TryUnlock(TrainerData trainer)
    {
        return TryUnlock(GetCost(trainer), trainer.name, Data.unlockedTrainers);
    }

    public bool TryUnlock(CreatureData creature)
    {
        return TryUnlock(GetCost(creature), creature.name, Data.unlockedCreatures);
    }

    public void AddStars(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        Data.stars += amount;
        SaveAndNotify();
    }

    private bool TryUnlock(int cost, string id, System.Collections.Generic.List<string> unlocked)
    {
        if (cost == 0)
        {
            return true;
        }
        if (Data.stars < cost)
        {
            return false;
        }

        Data.stars -= cost;
        unlocked.Add(id);
        SaveAndNotify();
        return true;
    }

    private void SaveAndNotify()
    {
        SaveSystem.SaveMeta(Data);
        Changed?.Invoke();
    }
}
