using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 딜량 그래프 (전투 화면·결과 화면의 📊 버튼): 크리처별 가로 막대, 큰 순서.
// 탭: 준 피해 / 받은 피해 / 회복, 위쪽 내 팀 / 적 팀. 열려 있으면 0.5초마다 갱신.
// 다시 누르거나 바깥을 누르면 닫힘. 연 화면이 꺼지면 같이 닫힘. 저장하지 않는다 (지금·마지막 전투만)
public class DamageStatsPanel : MonoBehaviour
{
    private const float RefreshSeconds = 0.5f;

    // 크리처 한 마리의 집계 (BattleScreen.GetStats가 채운다)
    public struct Entry
    {
        public CreatureData Creature;
        public int Level;
        public int Dealt;
        public int Taken;
        public int Healed;
    }

    [Serializable]
    public class Row
    {
        public GameObject Root;
        public Image Icon;
        public Text Name;
        [Tooltip("막대 (anchorMax.x = 1등 대비 비율, 색 = 타입 색)")]
        public Image Bar;
        public Text Value;
    }

    [SerializeField] private BattleScreen _battle;
    [SerializeField] private UITheme _theme;
    [Tooltip("0 내 팀, 1 적 팀 (고른 쪽 밝게)")]
    [SerializeField] private Image[] _teamButtons;
    [Tooltip("0 준 피해, 1 받은 피해, 2 회복")]
    [SerializeField] private Image[] _tabButtons;
    [SerializeField] private Row[] _rows;
    [Tooltip("\"합계 n\" 또는 기록이 없을 때 안내")]
    [SerializeField] private Text _totalText;

    private readonly List<Entry> _entries = new List<Entry>();
    private Comparison<Entry> _compare;
    private GameObject _owner;
    private bool _playerTeam = true;
    private int _tab;

    public bool IsOpen => gameObject.activeSelf;

    // 📊 버튼: owner = 버튼이 있는 화면 (그 화면이 꺼지면 닫는다)
    public void Toggle(GameObject owner)
    {
        if (IsOpen)
        {
            Close();
            return;
        }
        _owner = owner;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Refresh();
        StartCoroutine(RefreshLoop());
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void SetTeam(int team)
    {
        _playerTeam = team == 0;
        Refresh();
    }

    public void SetTab(int tab)
    {
        _tab = tab;
        Refresh();
    }

    private IEnumerator RefreshLoop()
    {
        float next = Time.unscaledTime + RefreshSeconds;
        while (true)
        {
            yield return null;
            if (_owner != null && !_owner.activeInHierarchy)
            {
                Close();
                yield break;
            }
            if (Time.unscaledTime >= next)
            {
                next = Time.unscaledTime + RefreshSeconds;
                Refresh();
            }
        }
    }

    private int Value(Entry entry) => _tab == 0 ? entry.Dealt : _tab == 1 ? entry.Taken : entry.Healed;

    private void Refresh()
    {
        for (int i = 0; i < _teamButtons.Length; i++)
        {
            _teamButtons[i].color = (i == 0) == _playerTeam ? _theme.ButtonPrimary : _theme.ButtonInfo;
        }
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            _tabButtons[i].color = i == _tab ? _theme.ButtonPrimary : _theme.ButtonInfo;
        }

        bool hasBattle = _battle.GetStats(_playerTeam, _entries);
        _compare ??= (a, b) => Value(b).CompareTo(Value(a));
        _entries.Sort(_compare);

        int total = 0;
        foreach (Entry entry in _entries)
        {
            total += Value(entry);
        }
        int top = _entries.Count > 0 ? Value(_entries[0]) : 0;

        for (int i = 0; i < _rows.Length; i++)
        {
            Row row = _rows[i];
            bool used = i < _entries.Count;
            row.Root.SetActive(used);
            if (!used)
            {
                continue;
            }
            Entry entry = _entries[i];
            int value = Value(entry);
            row.Icon.sprite = entry.Creature.GetSprite(entry.Level);
            row.Name.text = $"{entry.Creature.DisplayName} {CreatureInstance.ToStars(entry.Level)}";
            row.Bar.color = _theme.TypeColor(entry.Creature.Type);
            var bar = row.Bar.rectTransform;
            bar.anchorMax = new Vector2(top > 0 ? (float)value / top : 0f, bar.anchorMax.y);
            float percent = total > 0 ? 100f * value / total : 0f;
            row.Value.text = $"{value:N0}  ({percent:0}%)";
        }

        _totalText.text = hasBattle ? $"합계 {total:N0}" : "아직 전투 기록이 없어요";
    }
}
