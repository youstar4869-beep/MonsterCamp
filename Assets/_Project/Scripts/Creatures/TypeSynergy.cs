using System.Collections.Generic;

// 팀의 타입별 마릿수 세기. 같은 크리처가 여러 마리면 1마리로 센다
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
                counts[(int)creature.Type]++;
            }
        }
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
