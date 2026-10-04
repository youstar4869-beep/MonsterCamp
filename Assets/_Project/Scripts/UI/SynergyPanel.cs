using UnityEngine;
using UnityEngine.UI;

// 시너지 표: 5개 타입마다 지금 마릿수 / 다음 단계, 단계별 효과(SynergyConfig 값으로 만든 글).
// 아래에 조합 시너지 (MixedSynergyConfig 값으로 만든 글): 켜진 것 밝게, 하나 모자라면 흐리게 + 빠진 타입.
// 창 바깥이나 닫기 버튼을 누르면 닫힌다
public class SynergyPanel : MonoBehaviour
{
    // 안 켜진 단계 글의 진하기
    private const float OffAlpha = 0.35f;
    // 조합 시너지에서 두 타입 이상 모자라면 더 흐리게
    private const float FarAlpha = 0.18f;

    [SerializeField] private UITheme _theme;
    [Tooltip("CreatureType 순서 (불, 물, 풀, 번개, 바위)")]
    [SerializeField] private Text[] _typeTexts;
    [SerializeField] private Text[] _tier1Texts;
    [SerializeField] private Text[] _tier2Texts;
    [Tooltip("조합 시너지 순서 (MixedSynergyConfig): 이름 / 필요한 타입·효과")]
    [SerializeField] private Text[] _mixedNameTexts;
    [SerializeField] private Text[] _mixedEffectTexts;

    // counts: 타입별 마릿수 (TypeSynergy.CountUnique 결과)
    public void Show(int[] counts, SynergyConfig config)
    {
        for (int type = 0; type < _typeTexts.Length; type++)
        {
            var creatureType = (CreatureType)type;
            int count = counts[type];
            int activeTier = config.GetTier(count);

            _typeTexts[type].text = $"{TypeSynergy.GetTypeName(creatureType)} {count}/{config.GetNextThreshold(count)}";
            // 켜진 타입은 타입 색, 안 켜진 타입은 흰색
            _typeTexts[type].color = activeTier > 0 ? _theme.TypeColor(creatureType) : _theme.Text;
            SetTierText(_tier1Texts[type], config, creatureType, 1, activeTier);
            SetTierText(_tier2Texts[type], config, creatureType, 2, activeTier);
        }
        ShowMixed(counts, config.Mixed);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void ShowMixed(int[] counts, MixedSynergyConfig mixed)
    {
        bool rainbow = mixed != null && mixed.IsRainbowActive(counts);
        for (int i = 0; i < _mixedNameTexts.Length; i++)
        {
            if (mixed == null || i >= mixed.Count)
            {
                _mixedNameTexts[i].text = "";
                _mixedEffectTexts[i].text = "";
                continue;
            }

            int missing = mixed.GetMissingCount(i, counts);
            bool active = mixed.IsActive(i, counts);
            string state = active ? ""
                : missing == 0 && rainbow ? "  (무지개가 켜져 꺼짐)"
                : missing == 1 ? $"  ({mixed.GetMissingText(i, counts)} 필요)"
                : "";
            _mixedNameTexts[i].text = mixed.Get(i).Name;
            _mixedEffectTexts[i].text = $"{mixed.GetTypesText(i)}: {mixed.GetEffectText(i)}{state}";

            Color color = active ? _theme.Accent : _theme.Text;
            if (!active)
            {
                color.a = missing <= 1 ? OffAlpha : FarAlpha;
            }
            _mixedNameTexts[i].color = color;
            _mixedEffectTexts[i].color = color;
        }
    }

    // 지금 켜진 단계만 밝게 (2단계가 켜지면 1단계는 흐리게)
    private void SetTierText(Text text, SynergyConfig config, CreatureType type, int tier, int activeTier)
    {
        if (tier > config.TierCount)
        {
            text.text = "";
            return;
        }
        text.text = $"{tier}단계 ({config.GetThreshold(tier)}마리): {config.GetEffectText(type, tier)}";
        Color off = _theme.Text;
        off.a = OffAlpha;
        text.color = tier == activeTier ? _theme.Accent : off;
    }
}
