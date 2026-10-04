using UnityEngine;
using UnityEngine.UI;

// 조합식 한 줄 (도감 조합식 탭, 상점 조합식 창): 재료 A + 재료 B → 결과
// 못 찾은 조합식은 등급과 결과 실루엣만, 재료는 ???
public class FusionRecipeRowView : MonoBehaviour
{
    private static readonly Color FrameColor = new Color(1f, 0.85f, 0.3f, 1f);

    [SerializeField] private Text _gradeText;
    [SerializeField] private Image _iconA;
    [SerializeField] private Text _nameA;
    [SerializeField] private Image _iconB;
    [SerializeField] private Text _nameB;
    [SerializeField] private Image _iconResult;
    [SerializeField] private Text _nameResult;
    [SerializeField] private Text _infoText;
    [Tooltip("재료가 있을 때 켜는 밝은 테두리")]
    [SerializeField] private Outline _frame;

    public static bool IsKnown(MetaProgress meta, FusionRecipe recipe) => meta.IsDiscovered(recipe) || meta.IsMade(recipe);

    // highlight: 0 없음, 1 재료 1마리만 있음(반쯤), 2 만들 수 있음(강조)
    public void Show(FusionRecipe recipe, bool known, int highlight)
    {
        CreatureData result = recipe.Result;
        _gradeText.text = FusionRecipe.GetGradeName(recipe.Grade);
        _iconResult.sprite = result.GetSprite(1);
        _iconResult.color = known ? Color.white : Color.black;
        _iconA.gameObject.SetActive(known);
        _iconB.gameObject.SetActive(known);

        if (known)
        {
            _iconA.sprite = recipe.MaterialA.GetSprite(1);
            _iconB.sprite = recipe.MaterialB.GetSprite(1);
            _nameA.text = recipe.MaterialA.DisplayName;
            _nameB.text = recipe.MaterialB.DisplayName;
            _nameResult.text = result.DisplayName;
            string type = TypeSynergy.GetTypeName(result.Type);
            if (result.HasSecondType)
            {
                type += "/" + TypeSynergy.GetTypeName(result.SecondType);
            }
            _infoText.text = $"{type}  체력 {result.GetMaxHealth(1)}  공격 {result.GetAttack(1)}  공속 {result.AttackSpeed:0.##}\n{result.SkillDescription}";
        }
        else
        {
            _nameA.text = "???";
            _nameB.text = "???";
            _nameResult.text = "???";
            _infoText.text = "";
        }

        _frame.enabled = highlight > 0;
        Color color = FrameColor;
        color.a = highlight >= 2 ? 1f : 0.45f;
        _frame.effectColor = color;
    }
}
