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

// 조합식 하나: 재료 A + 재료 B → 결과. 히든 조합은 등급 칸을 상급으로 두고 _hidden으로 구분
[CreateAssetMenu(fileName = "FusionRecipe", menuName = "MonsterCamp/Fusion Recipe")]
public class FusionRecipe : ScriptableObject
{
    [SerializeField] private CreatureData _materialA;
    [SerializeField] private CreatureData _materialB;
    [SerializeField] private CreatureData _result;
    [SerializeField] private FusionGrade _grade;
    [Tooltip("히든 조합: 재료가 조합 크리처 2마리, 어려움 클리어 후 해금. 해금 전엔 만들 수도, 발견할 수도 없음")]
    [SerializeField] private bool _hidden;

    public CreatureData MaterialA => _materialA;
    public CreatureData MaterialB => _materialB;
    public CreatureData Result => _result;
    public FusionGrade Grade => _grade;
    public bool Hidden => _hidden;

    public string GradeName => _hidden ? "히든" : GetGradeName(_grade);

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
