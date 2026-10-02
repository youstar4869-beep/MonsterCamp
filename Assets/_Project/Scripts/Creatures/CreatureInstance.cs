// 플레이어가 가진 크리처 한 마리 (종류 + 레벨 + 장착 아이템)
public class CreatureInstance
{
    public CreatureData Data { get; }
    public int Level { get; set; }
    // 크리처당 1개. 없으면 null
    public ItemData Item { get; set; }

    public CreatureInstance(CreatureData data, int level = 1)
    {
        Data = data;
        Level = level;
    }

    // ★, ★★, ★★★ (0 이하면 빈 문자열)
    public static string ToStars(int level)
    {
        return level > 0 ? new string('★', level) : "";
    }
}
