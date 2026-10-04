using UnityEngine;

// 조합 등급 (GAME_DESIGN v0.2 2. 조합식)
public enum FusionGrade
{
    // 하급: 1골드 + 1골드
    Lower,
    // 중급: 2골드 + 2골드
    Middle,
    // 교차: 다른 타입 3골드 + 3골드
    Cross,
    // 상급: 4골드 + 5골드
    Upper
}

// 조합식 하나: 재료 A + 재료 B → 결과 (조합 버튼·발견·해금은 TODO 24 2단계)
[CreateAssetMenu(fileName = "FusionRecipe", menuName = "MonsterCamp/Fusion Recipe")]
public class FusionRecipe : ScriptableObject
{
    [SerializeField] private CreatureData _materialA;
    [SerializeField] private CreatureData _materialB;
    [SerializeField] private CreatureData _result;
    [SerializeField] private FusionGrade _grade;

    public CreatureData MaterialA => _materialA;
    public CreatureData MaterialB => _materialB;
    public CreatureData Result => _result;
    public FusionGrade Grade => _grade;

    public static string GetGradeName(FusionGrade grade)
    {
        switch (grade)
        {
            case FusionGrade.Lower: return "하급";
            case FusionGrade.Middle: return "중급";
            case FusionGrade.Cross: return "교차";
            default: return "상급";
        }
    }
}
