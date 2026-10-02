using UnityEngine;
using UnityEngine.UI;

// 튜토리얼: 첫 판(또는 "다시 보기")의 1~2라운드에서 단계별로 안내한다.
// 화면을 어두운 판 4장으로 덮고, 눌러야 할 곳(구멍)만 비워서 밝게 보이게 한다.
// 그 곳을 눌러 조건을 만족해야 다음 단계로 넘어간다 (설명 단계는 "확인" 버튼).
public class TutorialController : MonoBehaviour
{
    private const float HolePadding = 0.006f;
    private const float BubbleHeight = 0.15f;
    private const float BubbleGap = 0.012f;
    // 건너뛰기 버튼을 가리지 않도록 말풍선 위쪽 한계
    private const float BubbleTopLimit = 0.87f;

    [SerializeField] private TutorialData _data;
    [SerializeField] private MetaProgress _meta;
    [SerializeField] private ShopScreen _shop;
    [SerializeField] private RectTransform _battleButton;
    [Tooltip("칸을 누르면 뜨는 정보 창 (구매는 창의 버튼으로)")]
    [SerializeField] private CreatureInfoPopup _infoPopup;

    [Header("화면 (캔버스 맨 위)")]
    [Tooltip("튜토리얼 화면 전체 (처음엔 숨김)")]
    [SerializeField] private RectTransform _overlay;
    [SerializeField] private RectTransform _blockTop;
    [SerializeField] private RectTransform _blockBottom;
    [SerializeField] private RectTransform _blockLeft;
    [SerializeField] private RectTransform _blockRight;
    [Tooltip("설명 단계에서 구멍 안도 못 누르게 막는 투명 판")]
    [SerializeField] private RectTransform _holeBlocker;
    [Tooltip("구멍 둘레의 노란 테두리")]
    [SerializeField] private RectTransform _frame;
    [SerializeField] private RectTransform _bubble;
    [SerializeField] private Text _bubbleText;
    [SerializeField] private GameObject _confirmButton;

    private bool _active;
    private bool _replayRequested;
    private int _stepIndex;
    private int _round;
    private bool _inBattle;
    private bool _confirmed;
    // 매 프레임 위치 계산에 재사용
    private readonly Vector3[] _corners = new Vector3[4];

    public bool IsActive => _active;

    // ---------- RunManager / 타이틀에서 호출 ----------

    // 타이틀의 "튜토리얼 다시 보기": 다음 새 판에서 튜토리얼을 보여 준다
    public void RequestReplay()
    {
        _replayRequested = true;
    }

    public void CancelReplay()
    {
        _replayRequested = false;
    }

    // 새 판 시작 시. 튜토리얼을 안 끝냈거나 다시 보기를 눌렀으면 시작
    public void BeginRun()
    {
        _active = _replayRequested || !_meta.TutorialCompleted;
        _replayRequested = false;
        _stepIndex = 0;
        _round = 0;
        _inBattle = false;
        _confirmed = false;
        Hide();
    }

    // 이어하기·판 종료 시: 튜토리얼을 끝내지 않은 채로 멈춘다
    public void Stop()
    {
        _active = false;
        Hide();
    }

    public void OnShopStarted(int round)
    {
        if (!_active)
        {
            return;
        }

        _round = round;
        _inBattle = false;
        // 1라운드 단계가 남은 채로 2라운드가 되면 2라운드 단계로 건너뛴다
        while (_stepIndex < _data.StepCount && RoundOf(CurrentStep) < round)
        {
            _stepIndex++;
        }
        _confirmed = false;
        CheckFinished();
    }

    public void OnBattleStarted()
    {
        _inBattle = true;
    }

    // 튜토리얼 중이면 정해 둔 상점 목록 (아니면 null → 랜덤)
    public CreatureData[] GetPresetShop(int round)
    {
        return _active ? _data.GetShop(round) : null;
    }

    // 튜토리얼 중이면 약한 적 팀 (아니면 null → 원래 적 팀)
    public EnemyTeamData GetEnemyTeam(int round)
    {
        return _active ? _data.GetEnemyTeam(round) : null;
    }

    // ---------- 버튼 ----------

    // 말풍선 "확인"
    public void Confirm()
    {
        _confirmed = true;
    }

    // 오른쪽 위 "건너뛰기"
    public void Skip()
    {
        Complete();
    }

    // ---------- 진행 ----------

    private TutorialStep CurrentStep => _data.GetStep(_stepIndex);

    private void LateUpdate()
    {
        if (!_active)
        {
            return;
        }

        // "전투 시작" 단계는 전투가 시작되면(상점이 아니게 되면) 보이지 않는 단계가 되므로,
        // 보이는지보다 끝났는지를 먼저 본다 (아니면 다음 단계인 전투 설명이 나오지 않는다)
        if (!IsStepVisible() && !IsStepDone())
        {
            Hide();
            return;
        }

        if (IsStepDone())
        {
            _stepIndex++;
            _confirmed = false;
            if (CheckFinished())
            {
                return;
            }
            if (!IsStepVisible())
            {
                Hide();
                return;
            }
        }

        ShowStep();
    }

    // 지금 라운드·단계(상점/전투)에서 보여 줄 단계인지
    private bool IsStepVisible()
    {
        TutorialStep step = CurrentStep;
        bool battleStep = step == TutorialStep.BattleInfo;
        return RoundOf(step) == _round && battleStep == _inBattle;
    }

    private bool IsStepDone()
    {
        switch (CurrentStep)
        {
            case TutorialStep.BuyCreature: return _shop.HasOwnedCreature;
            case TutorialStep.PlaceInTeam: return _shop.HasTeamCreature;
            case TutorialStep.StartBattle: return _inBattle;
            case TutorialStep.Merge: return _shop.HasMergedCreature;
            default: return _confirmed;
        }
    }

    private bool CheckFinished()
    {
        if (_stepIndex < _data.StepCount)
        {
            return false;
        }
        Complete();
        return true;
    }

    private void Complete()
    {
        _meta.SetTutorialCompleted(true);
        Stop();
    }

    private static int RoundOf(TutorialStep step)
    {
        return step <= TutorialStep.BattleInfo ? 1 : 2;
    }

    // ---------- 화면 ----------

    private void ShowStep()
    {
        TutorialStep step = CurrentStep;
        bool infoStep = step == TutorialStep.BattleInfo || step == TutorialStep.MergeInfo
                        || step == TutorialStep.SellInfo || step == TutorialStep.SynergyInfo;
        // 전투 설명은 전투가 보이도록 어둡게 하지 않는다
        bool dim = step != TutorialStep.BattleInfo;

        _overlay.gameObject.SetActive(true);
        _bubbleText.text = _data.GetMessage(_stepIndex);
        _confirmButton.SetActive(infoStep);

        if (!dim || !TryGetHole(step, out Rect hole))
        {
            SetBlockers(dim, new Rect(0.5f, 0.5f, 0f, 0f));
            _frame.gameObject.SetActive(false);
            _holeBlocker.gameObject.SetActive(false);
            SetAnchors(_bubble, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.42f + BubbleHeight));
            return;
        }

        SetBlockers(true, hole);
        _frame.gameObject.SetActive(true);
        SetAnchors(_frame, hole.min, hole.max);
        _holeBlocker.gameObject.SetActive(infoStep);
        SetAnchors(_holeBlocker, hole.min, hole.max);
        PlaceBubble(hole);
    }

    // 비출 곳 (캔버스 기준 0~1 좌표). 없으면 false (전체를 어둡게)
    private bool TryGetHole(TutorialStep step, out Rect hole)
    {
        RectTransform target = null;
        RectTransform second = null;

        // 정보 창이 열려 있으면: 구매 단계는 창의 "구매" 버튼, 그 밖에는 "닫기" 버튼을 비춘다
        if (_infoPopup.IsOpen)
        {
            bool buyStep = step == TutorialStep.BuyCreature || step == TutorialStep.Merge;
            hole = ToOverlayRect(buyStep ? _infoPopup.ActionButtonRect : _infoPopup.CloseButtonRect);
            hole = Rect.MinMaxRect(hole.xMin - HolePadding, hole.yMin - HolePadding,
                hole.xMax + HolePadding, hole.yMax + HolePadding);
            return true;
        }

        switch (step)
        {
            case TutorialStep.BuyCreature:
            case TutorialStep.Merge:
                int offer = _shop.FindOfferIndex(_data.MergeCreature);
                target = offer >= 0 ? _shop.ShopSlotRect(offer) : _shop.ShopRowRect;
                break;
            case TutorialStep.PlaceInTeam:
                // 대기석의 크리처와 앞줄 첫 칸을 함께 비춘다 (끌어서 놓을 수 있게)
                int bench = _shop.FindFirstBenchIndex();
                target = _shop.TeamSlotRect(0);
                second = bench >= 0 ? _shop.BenchSlotRect(bench) : null;
                break;
            case TutorialStep.StartBattle:
                target = _battleButton;
                break;
            case TutorialStep.MergeInfo:
                target = _shop.ShopRowRect;
                break;
            case TutorialStep.SellInfo:
                target = _shop.BenchRowRect;
                break;
            case TutorialStep.SynergyInfo:
                target = _shop.SynergyTextRect;
                break;
        }

        if (target == null)
        {
            hole = default;
            return false;
        }

        hole = ToOverlayRect(target);
        if (second != null)
        {
            Rect other = ToOverlayRect(second);
            hole = Rect.MinMaxRect(Mathf.Min(hole.xMin, other.xMin), Mathf.Min(hole.yMin, other.yMin),
                Mathf.Max(hole.xMax, other.xMax), Mathf.Max(hole.yMax, other.yMax));
        }
        hole = Rect.MinMaxRect(hole.xMin - HolePadding, hole.yMin - HolePadding,
            hole.xMax + HolePadding, hole.yMax + HolePadding);
        return true;
    }

    // 대상의 화면 위치를 튜토리얼 화면 기준 0~1 좌표로
    private Rect ToOverlayRect(RectTransform target)
    {
        target.GetWorldCorners(_corners);
        Rect area = _overlay.rect;
        Vector2 min = _overlay.InverseTransformPoint(_corners[0]);
        Vector2 max = _overlay.InverseTransformPoint(_corners[2]);
        return Rect.MinMaxRect(
            (min.x - area.xMin) / area.width, (min.y - area.yMin) / area.height,
            (max.x - area.xMin) / area.width, (max.y - area.yMin) / area.height);
    }

    // 구멍을 뺀 나머지를 어두운 판 4장으로 덮는다
    private void SetBlockers(bool visible, Rect hole)
    {
        _blockTop.gameObject.SetActive(visible);
        _blockBottom.gameObject.SetActive(visible);
        _blockLeft.gameObject.SetActive(visible);
        _blockRight.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        SetAnchors(_blockTop, new Vector2(0f, hole.yMax), Vector2.one);
        SetAnchors(_blockBottom, Vector2.zero, new Vector2(1f, hole.yMin));
        SetAnchors(_blockLeft, new Vector2(0f, hole.yMin), new Vector2(hole.xMin, hole.yMax));
        SetAnchors(_blockRight, new Vector2(hole.xMax, hole.yMin), new Vector2(1f, hole.yMax));
    }

    // 구멍이 화면 위쪽이면 아래에, 아래쪽이면 위에 말풍선
    private void PlaceBubble(Rect hole)
    {
        float bottom;
        if (hole.center.y > 0.5f)
        {
            bottom = Mathf.Max(0.02f, hole.yMin - BubbleGap - BubbleHeight);
        }
        else
        {
            bottom = Mathf.Min(BubbleTopLimit - BubbleHeight, hole.yMax + BubbleGap);
        }
        SetAnchors(_bubble, new Vector2(0.06f, bottom), new Vector2(0.94f, bottom + BubbleHeight));
    }

    private void Hide()
    {
        if (_overlay.gameObject.activeSelf)
        {
            _overlay.gameObject.SetActive(false);
        }
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
