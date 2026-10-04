using System.Collections.Generic;

// 조합 규칙 (GAME_DESIGN v0.2 1. 조합 규칙). 상점 화면과 밸런스 시뮬레이터가 같이 쓴다
// - 레시피 재료 두 마리가 대기석·팀에 모두 ★★ 이상이면 만들 수 있음
// - 결과 레벨 = 두 재료 중 낮은 레벨, 결과 자리 = 재료 중 팀 칸에 있던 자리 우선, 없으면 대기석
// - 재료가 낀 아이템은 보관함으로, 보관함이 꽉 차면 아이템 가격만큼 골드로 돌려준다
public static class Fusion
{
    public const int MinLevel = 2;

    // 조합에 쓸 재료 두 마리의 자리와 결과 레벨
    public struct Plan
    {
        public bool TeamA;
        public int IndexA;
        public bool TeamB;
        public int IndexB;
        public int Level;
    }

    // 지금 만들 수 있으면 true. 재료가 여러 마리면 결과 레벨을 맞출 수 있는 가장 낮은 레벨을 쓴다
    // (★★ + ★★★ 두 마리 중에서 ★★와 짝이면 ★★를 쓰고 ★★★는 남긴다). 같은 레벨이면 팀 칸 먼저
    public static bool TryPlan(FusionRecipe recipe, CreatureInstance[] team, CreatureInstance[] bench, out Plan plan)
    {
        plan = default;
        int maxA = MaxLevel(recipe.MaterialA, team, bench);
        int maxB = MaxLevel(recipe.MaterialB, team, bench);
        if (maxA < MinLevel || maxB < MinLevel)
        {
            return false;
        }

        plan.Level = System.Math.Min(maxA, maxB);
        FindLowest(recipe.MaterialA, plan.Level, team, bench, out plan.TeamA, out plan.IndexA);
        FindLowest(recipe.MaterialB, plan.Level, team, bench, out plan.TeamB, out plan.IndexB);
        return true;
    }

    // 대기석·팀에 이 크리처가 있으면 true (레벨 상관없이)
    public static bool Has(CreatureData data, CreatureInstance[] team, CreatureInstance[] bench) =>
        MaxLevel(data, team, bench) > 0;

    public static CreatureInstance GetA(Plan plan, CreatureInstance[] team, CreatureInstance[] bench) =>
        (plan.TeamA ? team : bench)[plan.IndexA];

    public static CreatureInstance GetB(Plan plan, CreatureInstance[] team, CreatureInstance[] bench) =>
        (plan.TeamB ? team : bench)[plan.IndexB];

    // 재료 두 마리를 빼고 결과를 놓는다. resultInTeam/resultIndex: 결과 자리, refundGold: 보관함이 꽉 차서 골드로 돌려줄 값
    public static CreatureInstance Make(FusionRecipe recipe, Plan plan, CreatureInstance[] team, CreatureInstance[] bench,
        ItemData[] items, out bool resultInTeam, out int resultIndex, out int refundGold)
    {
        CreatureInstance a = GetA(plan, team, bench);
        CreatureInstance b = GetB(plan, team, bench);
        refundGold = ReturnItem(a, items) + ReturnItem(b, items);

        (plan.TeamA ? team : bench)[plan.IndexA] = null;
        (plan.TeamB ? team : bench)[plan.IndexB] = null;
        if (plan.TeamA || !plan.TeamB)
        {
            resultInTeam = plan.TeamA;
            resultIndex = plan.IndexA;
        }
        else
        {
            resultInTeam = true;
            resultIndex = plan.IndexB;
        }

        var result = new CreatureInstance(recipe.Result, plan.Level);
        (resultInTeam ? team : bench)[resultIndex] = result;
        return result;
    }

    // 지금 만들 수 있는 조합식을 result에 (조합식 목록 순서)
    public static void CollectAvailable(IReadOnlyList<FusionRecipe> recipes, CreatureInstance[] team,
        CreatureInstance[] bench, List<FusionRecipe> result)
    {
        result.Clear();
        if (recipes == null)
        {
            return;
        }
        foreach (FusionRecipe recipe in recipes)
        {
            if (recipe != null && TryPlan(recipe, team, bench, out _))
            {
                result.Add(recipe);
            }
        }
    }

    // 낀 아이템을 보관함 빈 칸으로. 빈 칸이 없으면 아이템 가격을 돌려준다 (돌려줄 골드)
    private static int ReturnItem(CreatureInstance material, ItemData[] items)
    {
        ItemData item = material.Item;
        if (item == null)
        {
            return 0;
        }
        material.Item = null;
        int empty = System.Array.IndexOf(items, null);
        if (empty < 0)
        {
            return item.Price;
        }
        items[empty] = item;
        return 0;
    }

    private static int MaxLevel(CreatureData data, CreatureInstance[] team, CreatureInstance[] bench)
    {
        int max = 0;
        for (int pass = 0; pass < 2; pass++)
        {
            foreach (CreatureInstance creature in pass == 0 ? team : bench)
            {
                if (creature != null && creature.Data == data && creature.Level > max)
                {
                    max = creature.Level;
                }
            }
        }
        return max;
    }

    // level 이상에서 가장 낮은 레벨 한 마리 (팀 칸 먼저)
    private static void FindLowest(CreatureData data, int level, CreatureInstance[] team, CreatureInstance[] bench,
        out bool inTeam, out int index)
    {
        inTeam = false;
        index = -1;
        int best = int.MaxValue;
        for (int pass = 0; pass < 2; pass++)
        {
            CreatureInstance[] slots = pass == 0 ? team : bench;
            for (int i = 0; i < slots.Length; i++)
            {
                CreatureInstance creature = slots[i];
                if (creature != null && creature.Data == data && creature.Level >= level && creature.Level < best)
                {
                    best = creature.Level;
                    inTeam = pass == 0;
                    index = i;
                }
            }
        }
    }
}
