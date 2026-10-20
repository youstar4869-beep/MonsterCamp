using UnityEngine;
using UnityEngine.UI;

// 도감: 트레이너·크리처 목록, 잠긴 것은 자물쇠와 필요한 별, 별이 충분하면 "해금"
public class CodexScreen : MonoBehaviour
{
    [SerializeField] private MetaProgress _meta;
    [SerializeField] private TrainerData[] _trainers;
    [SerializeField] private CodexEntryView[] _trainerEntries;
    [Tooltip("가격 순, 같은 가격은 타입 순")]
    [SerializeField] private CreatureData[] _creatures;
    [SerializeField] private CodexEntryView[] _creatureEntries;
    [Header("조합식 탭 (조합 해금 전에는 탭 버튼이 안 보임)")]
    [SerializeField] private GameObject _creatureTab;
    [SerializeField] private GameObject _recipeTab;
    [SerializeField] private GameObject _recipeTabButton;
    [Tooltip("등급순 조합식 (줄과 같은 순서)")]
    [SerializeField] private FusionRecipe[] _recipes;
    [SerializeField] private FusionRecipeRowView[] _recipeRows;
    [SerializeField] private Text _recipeCountText;
    [Tooltip("목록 맨 아래 \"히든 n/5\" (히든 해금 전엔 숨김)")]
    [SerializeField] private Text _hiddenCountText;
    [Header("3성 일러스트 다시 보기")]
    [SerializeField] private Lv3Showcase _showcase;
    [Tooltip("위쪽 \"일러스트 n/전체\" (크리처 + 조합·히든 결과 중 일러스트 파일이 있는 것)")]
    [SerializeField] private Text _illustrationCountText;
    [Space]
    [SerializeField] private Text _starsText;
    [SerializeField] private Text _messageText;

    private bool _showRecipes;
    private int[] _recipeHighlights;

    private void OnEnable()
    {
        _messageText.text = "";
        _showRecipes = false;
        _meta.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        _meta.Changed -= Refresh;
    }

    // 트레이너 칸의 "해금" 버튼
    public void UnlockTrainer(int index)
    {
        TrainerData trainer = _trainers[index];
        _messageText.text = _meta.TryUnlock(trainer) ? $"{trainer.DisplayName} 해금!" : "별이 부족해요";
        Refresh();
    }

    // 크리처 칸의 "해금" 버튼
    public void UnlockCreature(int index)
    {
        CreatureData creature = _creatures[index];
        _messageText.text = _meta.TryUnlock(creature) ? $"{creature.DisplayName} 해금! 이제 상점에 나와요" : "별이 부족해요";
        Refresh();
    }

    // 크리처 칸 / 조합식 줄의 "3성 일러스트" 버튼 (본 것만 눌림)
    public void ViewCreatureIllustration(int index) => ViewIllustration(_creatures[index]);
    public void ViewRecipeIllustration(int index) => ViewIllustration(_recipes[index].Result);

    private void ViewIllustration(CreatureData creature)
    {
        if (_meta.IsLv3IllustrationSeen(creature))
        {
            _showcase.Preview(creature);
        }
    }

    // 탭 버튼: 0 크리처, 1 조합식
    public void ShowTab(int tab)
    {
        _showRecipes = tab == 1;
        Refresh();
    }

    private void Refresh()
    {
        _starsText.text = $"★ {_meta.Stars}";

        bool fusionUnlocked = _meta.FusionUnlocked;
        bool recipes = _showRecipes && fusionUnlocked;
        _recipeTabButton.SetActive(fusionUnlocked);
        _creatureTab.SetActive(!recipes);
        _recipeTab.SetActive(recipes);
        if (recipes)
        {
            _recipeHighlights ??= new int[_recipes.Length];
            FusionRecipeRowView.ShowAll(_meta, _recipes, _recipeRows, null, _recipeHighlights, _recipeCountText, _hiddenCountText, _showcase);
        }

        for (int i = 0; i < _trainerEntries.Length; i++)
        {
            TrainerData trainer = _trainers[i];
            int cost = _meta.GetCost(trainer);
            _trainerEntries[i].Show(trainer.DisplayName, null, trainer.Description);
            _trainerEntries[i].SetLocked(cost > 0, cost, _meta.Stars >= cost);
        }

        for (int i = 0; i < _creatureEntries.Length; i++)
        {
            CreatureData creature = _creatures[i];
            int cost = _meta.GetCost(creature);
            string info = $"{TypeSynergy.GetTypeName(creature.Type)} · {creature.Tier}골드";
            _creatureEntries[i].Show(creature.DisplayName, creature.Sprite, info);
            _creatureEntries[i].SetLocked(cost > 0, cost, _meta.Stars >= cost);
            _creatureEntries[i].SetIllustration(_showcase.HasIllustration(creature), _meta.IsLv3IllustrationSeen(creature), cost > 0);
        }

        // 수집 수: 크리처 + 조합·히든 결과 (일러스트 파일이 있는 것만)
        int seen = 0, total = 0;
        foreach (CreatureData creature in _creatures)
        {
            CountIllustration(creature, ref seen, ref total);
        }
        foreach (FusionRecipe recipe in _recipes)
        {
            CountIllustration(recipe.Result, ref seen, ref total);
        }
        _illustrationCountText.text = $"일러스트 {seen}/{total}";
    }

    private void CountIllustration(CreatureData creature, ref int seen, ref int total)
    {
        if (!_showcase.HasIllustration(creature))
        {
            return;
        }
        total++;
        seen += _meta.IsLv3IllustrationSeen(creature) ? 1 : 0;
    }
}
