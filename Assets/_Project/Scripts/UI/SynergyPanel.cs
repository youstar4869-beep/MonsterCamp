using UnityEngine;
using UnityEngine.UI;

// 시너지 표: 5개 타입마다 지금 마릿수 / 다음 단계, 단계별 효과(SynergyConfig 값으로 만든 글).
// 켜진 단계는 밝게, 안 켜진 단계는 흐리게. 창 바깥이나 닫기 버튼을 누르면 닫힌다
public class SynergyPanel : MonoBehaviour
{
    private static readonly Color OnColor = new Color(1f, 0.92f, 0.55f);
    private static readonly Color OffColor = new Color(1f, 1f, 1f, 0.35f);

    [Tooltip("CreatureType 순서 (불, 물, 풀, 번개, 바위)")]
    [SerializeField] private Text[] _typeTexts;
    [SerializeField] private Text[] _tier1Texts;
    [SerializeField] private Text[] _tier2Texts;

    // counts: 타입별 마릿수 (TypeSynergy.CountUnique 결과)
    public void Show(int[] counts, SynergyConfig config)
    {
        for (int type = 0; type < _typeTexts.Length; type++)
        {
            var creatureType = (CreatureType)type;
            int count = counts[type];
            int activeTier = config.GetTier(count);

            _typeTexts[type].text = $"{TypeSynergy.GetTypeName(creatureType)} {count}/{config.GetNextThreshold(count)}";
            _typeTexts[type].color = activeTier > 0 ? OnColor : Color.white;
            SetTierText(_tier1Texts[type], config, creatureType, 1, activeTier);
            SetTierText(_tier2Texts[type], config, creatureType, 2, activeTier);
        }
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // 지금 켜진 단계만 밝게 (2단계가 켜지면 1단계는 흐리게)
    private static void SetTierText(Text text, SynergyConfig config, CreatureType type, int tier, int activeTier)
    {
        if (tier > config.TierCount)
        {
            text.text = "";
            return;
        }
        text.text = $"{tier}단계 ({config.GetThreshold(tier)}마리): {config.GetEffectText(type, tier)}";
        text.color = tier == activeTier ? OnColor : OffColor;
    }
}
