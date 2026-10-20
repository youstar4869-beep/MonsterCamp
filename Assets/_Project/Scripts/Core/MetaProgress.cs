using System;
using UnityEngine;

// 판이 끝나도 남는 진행: 별, 해금, 조합식 발견 (meta.json)
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

    // 열리는 조건 난이도를 클리어했으면 열림
    public bool IsUnlocked(DifficultyData difficulty)
    {
        return difficulty.RequiredClear == null || Data.clearedDifficulties.Contains(difficulty.RequiredClear.name);
    }

    public void SetCleared(DifficultyData difficulty)
    {
        if (Data.clearedDifficulties.Contains(difficulty.name))
        {
            return;
        }
        Data.clearedDifficulties.Add(difficulty.name);
        SaveAndNotify();
    }

    public bool FusionUnlocked => Data.fusionUnlocked;

    public void SetFusionUnlocked(bool unlocked)
    {
        Data.fusionUnlocked = unlocked;
        SaveAndNotify();
    }

    public bool HiddenFusionUnlocked => Data.hiddenFusionUnlocked;

    public void SetHiddenFusionUnlocked(bool unlocked)
    {
        Data.hiddenFusionUnlocked = unlocked;
        SaveAndNotify();
    }

    // 테스트용: 히든 해금과 히든 조합식의 발견·만듦 기록을 지운다
    public void ResetHidden(System.Collections.Generic.IEnumerable<FusionRecipe> hiddenRecipes)
    {
        Data.hiddenFusionUnlocked = false;
        foreach (FusionRecipe recipe in hiddenRecipes)
        {
            Data.discoveredRecipes.Remove(recipe.name);
            Data.madeRecipes.Remove(recipe.name);
        }
        SaveAndNotify();
    }

    public bool IsDiscovered(FusionRecipe recipe) => Data.discoveredRecipes.Contains(recipe.name);
    public bool IsMade(FusionRecipe recipe) => Data.madeRecipes.Contains(recipe.name);
    public void SetDiscovered(FusionRecipe recipe) => AddRecord(Data.discoveredRecipes, recipe.name);
    public void SetMade(FusionRecipe recipe) => AddRecord(Data.madeRecipes, recipe.name);

    // 처음 ★★★ 일러스트 연출 (크리처마다 한 번)
    public bool IsLv3IllustrationSeen(CreatureData creature) => SeenLv3Illustrations.Contains(creature.name);
    public void SetLv3IllustrationSeen(CreatureData creature) => AddRecord(SeenLv3Illustrations, creature.name);

    // 테스트용: 본 기록을 지운다
    public void ResetLv3Illustrations()
    {
        SeenLv3Illustrations.Clear();
        SaveAndNotify();
    }

    // 예전 저장 파일에 목록이 없으면 빈 목록으로
    private System.Collections.Generic.List<string> SeenLv3Illustrations =>
        Data.seenLv3Illustrations ??= new System.Collections.Generic.List<string>();

    public bool TutorialCompleted => Data.tutorialCompleted;
    public bool FrontRowTipShown => Data.frontRowTipShown;

    public void SetFrontRowTipShown()
    {
        Data.frontRowTipShown = true;
        SaveAndNotify();
    }

    public void SetTutorialCompleted(bool completed)
    {
        Data.tutorialCompleted = completed;
        SaveAndNotify();
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

    private void AddRecord(System.Collections.Generic.List<string> records, string id)
    {
        if (records.Contains(id))
        {
            return;
        }
        records.Add(id);
        SaveAndNotify();
    }

    private void SaveAndNotify()
    {
        SaveSystem.SaveMeta(Data);
        Changed?.Invoke();
    }
}
