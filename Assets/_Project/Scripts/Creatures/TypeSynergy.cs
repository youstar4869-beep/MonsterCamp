using System.Collections.Generic;

// 팀의 타입별 마릿수 세기. 같은 크리처가 여러 마리면 1번만 센다.
// 5골드 크리처는 2마리로 센다. 상점 시너지 표시·전투·봇이 모두 이 함수를 쓰고,
// 타입 시너지(마릿수)와 조합 시너지(타입이 1마리 이상 있는지)가 모두 이 결과를 본다
public static class TypeSynergy
{
    public static readonly int TypeCount = System.Enum.GetValues(typeof(CreatureType)).Length;

    private static readonly HashSet<CreatureData> Counted = new HashSet<CreatureData>();

    // counts[(int)type]에 결과를 채운다 (길이 TypeCount). null은 건너뛴다
    public static void CountUnique(IEnumerable<CreatureData> creatures, int[] counts)
    {
        System.Array.Clear(counts, 0, counts.Length);
        Counted.Clear();
        foreach (CreatureData creature in creatures)
        {
            if (creature != null && Counted.Add(creature))
            {
                // TODO 24: 조합 크리처는 두 번째 타입도 여기서 더한다 (타입 세는 곳은 여기 한 군데)
                counts[(int)creature.Type] += GetSynergyCount(creature);
            }
        }
    }

    // 시너지에서 몇 마리로 세는지 (5골드 = 2마리)
    public static int GetSynergyCount(CreatureData creature)
    {
        return creature.Tier >= CreatureData.MaxTier ? 2 : 1;
    }

    public static string GetTypeName(CreatureType type)
    {
        switch (type)
        {
            case CreatureType.Fire: return "불";
            case CreatureType.Water: return "물";
            case CreatureType.Grass: return "풀";
            case CreatureType.Electric: return "번개";
            default: return "바위";
        }
    }
}
