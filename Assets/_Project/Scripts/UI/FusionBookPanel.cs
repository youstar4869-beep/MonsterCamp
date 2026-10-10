using System;
using UnityEngine;
using UnityEngine.UI;

// 상점 조합식 창: 도감 조합식 탭과 같은 줄. 재료가 있는 조합식을 위로 올린다. 바깥이나 닫기를 누르면 닫힌다
public class FusionBookPanel : MonoBehaviour
{
    [Tooltip("등급순 조합식 (줄과 같은 순서)")]
    [SerializeField] private FusionRecipe[] _recipes;
    [SerializeField] private FusionRecipeRowView[] _rows;
    [SerializeField] private Text _countText;
    [Tooltip("목록 맨 아래 \"히든 n/5\" (히든 해금 전엔 숨김)")]
    [SerializeField] private Text _hiddenCountText;
    [SerializeField] private ScrollRect _scroll;

    private int[] _highlights;

    // getHighlight: 조합식 → 0/1/2 (FusionRecipeRowView.Show 참고). 못 찾은 조합식은 0으로 친다
    public void Show(MetaProgress meta, Func<FusionRecipe, int> getHighlight)
    {
        _highlights ??= new int[_recipes.Length];
        FusionRecipeRowView.ShowAll(meta, _recipes, _rows, getHighlight, _highlights, _countText, _hiddenCountText);

        int order = 0;
        for (int level = 2; level >= 0; level--)
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                if (_highlights[i] == level)
                {
                    _rows[i].transform.SetSiblingIndex(order++);
                }
            }
        }
        _hiddenCountText.transform.SetAsLastSibling();

        _scroll.verticalNormalizedPosition = 1f;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
