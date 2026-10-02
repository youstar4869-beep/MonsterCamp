using UnityEngine;
using UnityEngine.UI;

// 도감: 트레이너·크리처 목록, 잠긴 것은 자물쇠와 필요한 별, 별이 충분하면 "해금"
public class CodexScreen : MonoBehaviour
{
    [SerializeField] private MetaProgress _meta;
    [SerializeField] private TrainerData[] _trainers;
    [SerializeField] private CodexEntryView[] _trainerEntries;
    [Tooltip("티어 순, 같은 티어는 타입 순")]
    [SerializeField] private CreatureData[] _creatures;
    [SerializeField] private CodexEntryView[] _creatureEntries;
    [SerializeField] private Text _starsText;
    [SerializeField] private Text _messageText;

    private void OnEnable()
    {
        _messageText.text = "";
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

    private void Refresh()
    {
        _starsText.text = $"★ {_meta.Stars}";

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
            string info = $"{TypeSynergy.GetTypeName(creature.Type)} · 티어 {creature.Tier}";
            _creatureEntries[i].Show(creature.DisplayName, creature.Sprite, info);
            _creatureEntries[i].SetLocked(cost > 0, cost, _meta.Stars >= cost);
        }
    }
}
