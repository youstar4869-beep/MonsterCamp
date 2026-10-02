using UnityEngine;

// 화면 공통 모양: 색(바탕, 패널, 버튼, 강조, 골드, 타입 5색, 전투 숫자)과 글꼴·글자 크기, 둥근 모서리 그림.
// MainSceneBuilder가 씬을 만들 때 이 값을 쓰고, 실행 중 색을 바꾸는 화면(전투 숫자, 시너지 표, 트레이너 카드)도 이 에셋을 본다.
// 색을 바꾼 뒤에는 Build Main Scene을 다시 해야 씬에 반영된다 (실행 중 색은 바로 반영)
[CreateAssetMenu(fileName = "UITheme", menuName = "MonsterCamp/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("바탕·패널")]
    [SerializeField] private Color _background = new Color(0.11f, 0.12f, 0.17f);
    [Tooltip("상단 정보바")]
    [SerializeField] private Color _bar = new Color(0.06f, 0.07f, 0.1f);
    [Tooltip("창(정보 창, 시너지 표, 확인 창)")]
    [SerializeField] private Color _panel = new Color(0.17f, 0.19f, 0.26f);
    [Tooltip("크리처·아이템 칸, 카드")]
    [SerializeField] private Color _slot = new Color(0.24f, 0.27f, 0.35f);
    [Tooltip("상점 팀 칸")]
    [SerializeField] private Color _teamSlot = new Color(0.2f, 0.32f, 0.3f);
    [Tooltip("전투 내 팀 칸")]
    [SerializeField] private Color _playerUnit = new Color(0.22f, 0.32f, 0.47f);
    [Tooltip("전투 적 팀 칸")]
    [SerializeField] private Color _enemyUnit = new Color(0.47f, 0.24f, 0.26f);
    [Tooltip("상점 아이템 칸")]
    [SerializeField] private Color _itemSlot = new Color(0.33f, 0.26f, 0.42f);
    [Tooltip("창 뒤를 어둡게 덮는 판")]
    [SerializeField] private Color _dim = new Color(0f, 0f, 0f, 0.68f);

    [Header("버튼")]
    [Tooltip("주요 버튼 (전투 시작, 구매, 확인)")]
    [SerializeField] private Color _buttonPrimary = new Color(0.98f, 0.76f, 0.26f);
    [Tooltip("보조 버튼 (닫기, 취소, 건너뛰기)")]
    [SerializeField] private Color _buttonSecondary = new Color(0.72f, 0.74f, 0.8f);
    [Tooltip("위험한 버튼 (팔기, 새로 시작)")]
    [SerializeField] private Color _buttonDanger = new Color(0.96f, 0.5f, 0.42f);
    [Tooltip("새로고침")]
    [SerializeField] private Color _buttonInfo = new Color(0.42f, 0.76f, 0.96f);
    [Tooltip("시너지 버튼")]
    [SerializeField] private Color _buttonGood = new Color(0.47f, 0.78f, 0.52f);
    [Tooltip("버튼 위 글자")]
    [SerializeField] private Color _buttonText = new Color(0.1f, 0.1f, 0.12f);
    [Tooltip("누르는 동안 버튼 색에 곱함")]
    [SerializeField] private Color _buttonPressed = new Color(0.7f, 0.7f, 0.7f);

    [Header("글자")]
    [SerializeField] private Color _text = Color.white;
    [Tooltip("설명 같은 덜 중요한 글")]
    [SerializeField] private Color _textSub = new Color(0.82f, 0.84f, 0.9f);
    [Tooltip("강조 (레벨 별, 튜토리얼 테두리, 켜진 시너지)")]
    [SerializeField] private Color _accent = new Color(1f, 0.85f, 0.25f);
    [SerializeField] private Color _gold = new Color(1f, 0.83f, 0.3f);
    [Tooltip("좋은 소식 (시너지 줄, 들어온 골드)")]
    [SerializeField] private Color _positive = new Color(0.55f, 0.97f, 0.6f);
    [Tooltip("경고 (골드 부족 등 안내 문구)")]
    [SerializeField] private Color _negative = new Color(1f, 0.52f, 0.47f);
    [SerializeField] private Color _heart = new Color(1f, 0.42f, 0.5f);

    [Header("타입 5색 (불, 물, 풀, 번개, 바위 순)")]
    [SerializeField] private Color[] _typeColors =
    {
        new Color(1f, 0.45f, 0.3f),
        new Color(0.35f, 0.65f, 1f),
        new Color(0.42f, 0.85f, 0.4f),
        new Color(1f, 0.85f, 0.25f),
        new Color(0.72f, 0.6f, 0.45f),
    };

    [Header("전투")]
    [SerializeField] private Color _healthBar = new Color(0.3f, 0.9f, 0.35f);
    [SerializeField] private Color _healthBarBack = new Color(0.08f, 0.08f, 0.1f);
    [Tooltip("체력이 줄면 뒤따라오는 막대, 보호막")]
    [SerializeField] private Color _healthTrail = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private Color _damageNormal = Color.white;
    [Tooltip("상성 유리")]
    [SerializeField] private Color _damageStrong = new Color(1f, 0.6f, 0.2f);
    [Tooltip("상성 불리")]
    [SerializeField] private Color _damageWeak = new Color(0.75f, 0.75f, 0.78f);
    [SerializeField] private Color _heal = new Color(0.45f, 1f, 0.5f);
    [Tooltip("스킬 글: 상태이상(기절·화상…)")]
    [SerializeField] private Color _skillDebuff = new Color(1f, 0.45f, 0.4f);
    [Tooltip("스킬 글: 회복·보호막·강화")]
    [SerializeField] private Color _skillBuff = new Color(0.5f, 1f, 0.55f);
    [Tooltip("스킬 글: 그 밖")]
    [SerializeField] private Color _skill = new Color(1f, 0.9f, 0.35f);

    [Header("글꼴·글자 크기 (1080x1920 기준)")]
    [Tooltip("한글이 들어 있는 글꼴. 비어 있으면 Unity 기본 글꼴")]
    [SerializeField] private Font _font;
    [Tooltip("승리·패배 같은 큰 알림")]
    [SerializeField] private int _hugeSize = 120;
    [Tooltip("화면 제목")]
    [SerializeField] private int _titleSize = 96;
    [Tooltip("큰 버튼, 창 제목")]
    [SerializeField] private int _headingSize = 60;
    [Tooltip("작은 버튼, 이름")]
    [SerializeField] private int _largeSize = 50;
    [SerializeField] private int _bodySize = 42;
    [SerializeField] private int _smallSize = 34;
    [Tooltip("칸 안의 이름·가격")]
    [SerializeField] private int _tinySize = 28;

    [Header("모양")]
    [Tooltip("둥근 사각형 (9-slice). MonsterCamp > UI 테마 그림 만들기로 생성")]
    [SerializeField] private Sprite _roundedSprite;
    [Tooltip("둥근 모서리 반지름 (픽셀, 1080x1920 기준)")]
    [SerializeField] private float _cornerRadius = 22f;

    public Color Background => _background;
    public Color Bar => _bar;
    public Color Panel => _panel;
    public Color Slot => _slot;
    public Color TeamSlot => _teamSlot;
    public Color PlayerUnit => _playerUnit;
    public Color EnemyUnit => _enemyUnit;
    public Color ItemSlot => _itemSlot;
    public Color Dim => _dim;
    public Color ButtonPrimary => _buttonPrimary;
    public Color ButtonSecondary => _buttonSecondary;
    public Color ButtonDanger => _buttonDanger;
    public Color ButtonInfo => _buttonInfo;
    public Color ButtonGood => _buttonGood;
    public Color ButtonText => _buttonText;
    public Color ButtonPressed => _buttonPressed;
    public Color Text => _text;
    public Color TextSub => _textSub;
    public Color Accent => _accent;
    public Color Gold => _gold;
    public Color Positive => _positive;
    public Color Negative => _negative;
    public Color Heart => _heart;
    public Color HealthBar => _healthBar;
    public Color HealthBarBack => _healthBarBack;
    public Color HealthTrail => _healthTrail;
    public Color DamageNormal => _damageNormal;
    public Color DamageStrong => _damageStrong;
    public Color DamageWeak => _damageWeak;
    public Color Heal => _heal;
    public Color SkillDebuff => _skillDebuff;
    public Color SkillBuff => _skillBuff;
    public Color Skill => _skill;
    public Font Font => _font;
    public int HugeSize => _hugeSize;
    public int TitleSize => _titleSize;
    public int HeadingSize => _headingSize;
    public int LargeSize => _largeSize;
    public int BodySize => _bodySize;
    public int SmallSize => _smallSize;
    public int TinySize => _tinySize;
    public Sprite RoundedSprite => _roundedSprite;
    public float CornerRadius => _cornerRadius;

    public Color TypeColor(CreatureType type)
    {
        int index = (int)type;
        return _typeColors != null && index < _typeColors.Length ? _typeColors[index] : _text;
    }
}
