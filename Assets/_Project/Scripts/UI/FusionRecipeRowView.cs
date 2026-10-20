using System;
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
    [Tooltip("결과 크리처 3성 일러스트 버튼과 결과 그림 위 작은 ★ (도감에서만, 상점 조합식 창에선 숨김)")]
    [SerializeField] private IllustrationButton _illustration;
    [SerializeField] private GameObject _illustrationMark;

    public static bool IsKnown(MetaProgress meta, FusionRecipe recipe) => meta.IsDiscovered(recipe) || meta.IsMade(recipe);

    // 줄 목록 채우기 (도감 조합식 탭·상점 조합식 창 공용). 일반 조합식은 countText에 "발견 n/m".
    // 히든 조합은 발견 전엔 줄을 숨기고 맨 아래 hiddenText에 "히든 n/5"만 (히든 해금 전엔 그것도 숨김).
    // getHighlight가 null이면 강조 없음. highlights[i]에 줄마다 강조 값 (숨긴 줄·못 찾은 줄은 0)
    // showcase가 있으면(도감) 찾은 조합식 결과의 3성 일러스트 버튼·★ 표시
    public static void ShowAll(MetaProgress meta, FusionRecipe[] recipes, FusionRecipeRowView[] rows,
        Func<FusionRecipe, int> getHighlight, int[] highlights, Text countText, Text hiddenText, Lv3Showcase showcase = null)
    {
        bool hiddenUnlocked = meta.HiddenFusionUnlocked;
        int discovered = 0, normal = 0, hiddenDiscovered = 0, hidden = 0;
        for (int i = 0; i < recipes.Length; i++)
        {
            FusionRecipe recipe = recipes[i];
            bool known = IsKnown(meta, recipe);
            bool visible = !recipe.Hidden || (hiddenUnlocked && known);
            if (recipe.Hidden)
            {
                hidden++;
                hiddenDiscovered += known ? 1 : 0;
            }
            else
            {
                normal++;
                discovered += known ? 1 : 0;
            }
            highlights[i] = visible && known && getHighlight != null ? getHighlight(recipe) : 0;
            rows[i].gameObject.SetActive(visible);
            if (visible)
            {
                rows[i].Show(recipe, known, highlights[i]);
                bool hasArt = showcase != null && known && showcase.HasIllustration(recipe.Result);
                bool seen = hasArt && meta.IsLv3IllustrationSeen(recipe.Result);
                if (rows[i]._illustration != null)
                {
                    rows[i]._illustration.Set(hasArt, seen);
                    rows[i]._illustrationMark.SetActive(seen);
                }
            }
        }
        countText.text = $"발견 {discovered}/{normal}";
        hiddenText.gameObject.SetActive(hiddenUnlocked && hidden > 0);
        hiddenText.text = $"히든 {hiddenDiscovered}/{hidden}";
    }

    // highlight: 0 없음, 1 재료 1마리만 있음(반쯤), 2 만들 수 있음(강조)
    public void Show(FusionRecipe recipe, bool known, int highlight)
    {
        CreatureData result = recipe.Result;
        _gradeText.text = recipe.GradeName;
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
