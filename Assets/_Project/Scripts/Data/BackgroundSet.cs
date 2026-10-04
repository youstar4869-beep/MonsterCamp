using UnityEngine;

// 화면별 배경 그림: 시작화면(타이틀·트레이너·난이도·도감·결과), 상점, 전투 층 구간별 일반·보스 그림, 전투 하늘 구름.
// MonsterCamp > 배경 세트 만들기 (또는 Build Main Scene)가 Art/Backgrounds 그림으로 채운다
[CreateAssetMenu(fileName = "BackgroundSet", menuName = "MonsterCamp/Background Set")]
public class BackgroundSet : ScriptableObject
{
    [System.Serializable]
    public struct FloorRange
    {
        public int StartFloor;
        public int EndFloor;
        public Sprite Normal;
        [Tooltip("보스 층 (붉은 하늘)")]
        public Sprite Boss;
    }

    [SerializeField] private Sprite _title;
    [SerializeField] private Sprite _shop;
    [Tooltip("전투 화면 위쪽에 겹쳐 흐르는 구름 (좌우 이어지는 그림, Wrap Mode U = Repeat)")]
    [SerializeField] private Sprite _clouds;
    [Tooltip("층 구간 (시작 층~끝 층, 끝 층 포함). 어느 구간에도 없으면 가장 가까운 구간")]
    [SerializeField] private FloorRange[] _battleRanges;

    public Sprite Title => _title;
    public Sprite Shop => _shop;
    public Sprite Clouds => _clouds;

    public Sprite GetBattle(int floor, bool isBoss)
    {
        if (_battleRanges == null || _battleRanges.Length == 0)
        {
            return null;
        }

        FloorRange range = floor < _battleRanges[0].StartFloor ? _battleRanges[0] : _battleRanges[_battleRanges.Length - 1];
        foreach (FloorRange candidate in _battleRanges)
        {
            if (floor >= candidate.StartFloor && floor <= candidate.EndFloor)
            {
                range = candidate;
                break;
            }
        }
        return isBoss && range.Boss != null ? range.Boss : range.Normal;
    }
}
