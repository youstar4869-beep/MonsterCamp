using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 메뉴 MonsterCamp > Build Main Scene 으로 Main 씬을 새로 만든다.
// 씬 파일을 직접 편집하지 않기 위한 생성 스크립트. 다시 실행하면 씬을 덮어쓴다.
public static class MainSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
    private const string ShopConfigPath = "Assets/_Project/Data/ShopConfig.asset";
    private const string CreatureFolder = "Assets/_Project/Data/Creatures";
    private const string TypeChartPath = "Assets/_Project/Data/TypeChart.asset";
    private const string RunConfigPath = "Assets/_Project/Data/RunConfig.asset";
    private const string SynergyConfigPath = "Assets/_Project/Data/SynergyConfig.asset";
    private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

    private static Font _font;

    [MenuItem("MonsterCamp/Build Main Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("Build Main Scene", ScenePath + " 를 덮어쓸까요?", "덮어쓰기", "취소"))
        {
            return;
        }

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateEventSystem();
        Transform canvas = CreateCanvas();

        var flow = new GameObject("ScreenFlow").AddComponent<ScreenFlow>();
        var run = new GameObject("RunManager").AddComponent<RunManager>();

        GameObject title = CreateScreen(canvas, "TitleScreen", "몬스터 캠프");
        GameObject trainer = CreateScreen(canvas, "TrainerSelectScreen", "트레이너 선택");
        GameObject shop = CreateScreen(canvas, "ShopScreen", "상점 · 배치");
        GameObject battle = CreateScreen(canvas, "BattleScreen", "전투");
        GameObject result = CreateScreen(canvas, "ResultScreen", "결과");

        // 화면들보다 나중에 만들어 가장 위에 그려지게 한다
        GameObject hud = CreateHud(canvas, out Text roundText, out Text livesText, out Text goldText);

        AddButton(title, "시작", flow, GameScreen.TrainerSelect);

        // 결과 화면: 클리어/게임 오버 + 설명 + 타이틀로
        Text resultTitle = result.transform.Find("Title").GetComponent<Text>();
        Text resultDetail = CreateText(result.transform, "Detail", "", 56, new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.72f));
        AddButton(result, "타이틀로", flow, GameScreen.Title);

        BattleScreen battleScreen = BuildBattleUI(battle, run);

        // 팀이 비었는지 확인한 뒤 전투로 넘어간다
        Button battleButton = CreateButton(shop, "전투 시작");
        ShopScreen shopScreen = BuildShopUI(shop, run, goldText);
        UnityEventTools.AddVoidPersistentListener(battleButton.onClick, shopScreen.StartBattle);

        // 새 런 시작 (1라운드 상점으로 이동은 RunManager가 한다)
        Button startRunButton = CreateButton(trainer, "선택 완료");
        UnityEventTools.AddVoidPersistentListener(startRunButton.onClick, run.StartRun);

        var runConfig = AssetDatabase.LoadAssetAtPath<RunConfig>(RunConfigPath);
        if (runConfig == null)
        {
            Debug.LogError("RunConfig 에셋이 없습니다. 먼저 MonsterCamp > Create Round Data 를 실행하세요.");
        }
        var serializedRun = new SerializedObject(run);
        serializedRun.FindProperty("_config").objectReferenceValue = runConfig;
        serializedRun.FindProperty("_flow").objectReferenceValue = flow;
        serializedRun.FindProperty("_shop").objectReferenceValue = shopScreen;
        serializedRun.FindProperty("_battle").objectReferenceValue = battleScreen;
        serializedRun.FindProperty("_roundText").objectReferenceValue = roundText;
        serializedRun.FindProperty("_livesText").objectReferenceValue = livesText;
        serializedRun.FindProperty("_resultTitle").objectReferenceValue = resultTitle;
        serializedRun.FindProperty("_resultDetail").objectReferenceValue = resultDetail;
        serializedRun.ApplyModifiedPropertiesWithoutUndo();

        // GameScreen enum 순서와 같아야 한다.
        var serializedFlow = new SerializedObject(flow);
        SetArray(serializedFlow.FindProperty("_screens"), new Object[] { title, trainer, shop, battle, result });
        serializedFlow.FindProperty("_hud").objectReferenceValue = hud;
        serializedFlow.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        Debug.Log("Main 씬 생성 완료: " + ScenePath);
    }

    private static void CreateCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0, 0, -10);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }

    private static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        // Player Settings의 Active Input Handling이 Input System이므로 이 모듈을 쓴다.
        go.AddComponent<InputSystemUIInputModule>();
    }

    private static Transform CreateCanvas()
    {
        var go = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go.transform;
    }

    // 전체 화면 패널 + 상단 제목 + 하단 버튼 영역(ButtonArea)
    private static GameObject CreateScreen(Transform canvas, string name, string titleText)
    {
        var screen = new GameObject(name, typeof(RectTransform), typeof(Image));
        screen.transform.SetParent(canvas, false);
        Stretch(screen.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        screen.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f);

        var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
        title.transform.SetParent(screen.transform, false);
        Stretch(title.GetComponent<RectTransform>(), new Vector2(0f, 0.75f), new Vector2(1f, 0.9f));
        SetupText(title.GetComponent<Text>(), titleText, 96);

        var buttonArea = new GameObject("ButtonArea", typeof(RectTransform), typeof(VerticalLayoutGroup));
        buttonArea.transform.SetParent(screen.transform, false);
        Stretch(buttonArea.GetComponent<RectTransform>(), new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.35f));
        var layout = buttonArea.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 40;
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return screen;
    }

    // 상단 표시: 라운드 / 목숨 / 골드 (상점·전투 화면에서만 보인다)
    private static GameObject CreateHud(Transform canvas, out Text roundText, out Text livesText, out Text goldText)
    {
        var hud = new GameObject("Hud", typeof(RectTransform), typeof(Image));
        hud.transform.SetParent(canvas, false);
        Stretch(hud.GetComponent<RectTransform>(), new Vector2(0f, 0.945f), Vector2.one);
        var background = hud.GetComponent<Image>();
        background.color = new Color(0.06f, 0.07f, 0.1f);
        background.raycastTarget = false;

        roundText = CreateText(hud.transform, "RoundText", "", 44, new Vector2(0.03f, 0f), new Vector2(0.42f, 1f));
        roundText.alignment = TextAnchor.MiddleLeft;
        livesText = CreateText(hud.transform, "LivesText", "", 44, new Vector2(0.42f, 0f), new Vector2(0.7f, 1f));
        livesText.color = new Color(1f, 0.45f, 0.5f);
        goldText = CreateText(hud.transform, "GoldText", "", 44, new Vector2(0.7f, 0f), new Vector2(0.97f, 1f));
        goldText.alignment = TextAnchor.MiddleRight;
        goldText.color = new Color(1f, 0.85f, 0.3f);

        hud.SetActive(false);
        return hud;
    }

    // 화면 전환 버튼
    private static Button AddButton(GameObject screen, string label, ScreenFlow flow, GameScreen target)
    {
        Button button = CreateButton(screen, label);
        UnityEventTools.AddIntPersistentListener(button.onClick, flow.ShowScreen, (int)target);
        return button;
    }

    // 하단 ButtonArea에 버튼만 만든다 (연결은 호출하는 쪽에서)
    private static Button CreateButton(GameObject screen, string label)
    {
        Transform area = screen.transform.Find("ButtonArea");

        var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(area, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 160);
        go.GetComponent<Image>().color = new Color(0.95f, 0.75f, 0.25f);

        var text = new GameObject("Text", typeof(RectTransform), typeof(Text));
        text.transform.SetParent(go.transform, false);
        Stretch(text.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        SetupText(text.GetComponent<Text>(), label, 60);
        text.GetComponent<Text>().color = Color.black;

        return go.GetComponent<Button>();
    }

    // 전투 화면 (세로 1920 기준, 위에서부터)
    // (상단 표시) / 적 팀 이름 / 적 뒷줄 3칸 / 적 앞줄 2칸 / 승패 문구 / 내 팀 앞줄 2칸 / 내 팀 뒷줄 3칸 / 배속 버튼
    // 두 팀의 앞줄이 가운데에서 마주 본다 (내 팀은 배치 화면과 같은 모양)
    private static BattleScreen BuildBattleUI(GameObject screen, RunManager run)
    {
        // 제목 자리는 상단 표시가 차지한다
        screen.transform.Find("Title").gameObject.SetActive(false);
        Stretch((RectTransform)screen.transform.Find("ButtonArea"), new Vector2(0.25f, 0.015f), new Vector2(0.75f, 0.105f));

        Color playerColor = new Color(0.22f, 0.32f, 0.45f);
        Color enemyColor = new Color(0.45f, 0.24f, 0.24f);

        Text enemyLabel = CreateText(screen.transform, "EnemyLabel", "적 팀", 40, new Vector2(0f, 0.895f), new Vector2(1f, 0.935f));
        Transform enemyBack = CreateRow(screen.transform, "EnemyBackRow", new Vector2(0.03f, 0.745f), new Vector2(0.97f, 0.89f));
        Transform enemyFront = CreateRow(screen.transform, "EnemyFrontRow", new Vector2(0.2f, 0.595f), new Vector2(0.8f, 0.74f));

        Text bannerText = CreateText(screen.transform, "BannerText", "", 110, new Vector2(0f, 0.475f), new Vector2(1f, 0.585f));
        bannerText.color = new Color(1f, 0.85f, 0.3f);

        Transform playerFront = CreateRow(screen.transform, "PlayerFrontRow", new Vector2(0.2f, 0.32f), new Vector2(0.8f, 0.465f));
        Transform playerBack = CreateRow(screen.transform, "PlayerBackRow", new Vector2(0.03f, 0.17f), new Vector2(0.97f, 0.315f));
        CreateText(screen.transform, "PlayerLabel", "내 팀", 40, new Vector2(0f, 0.125f), new Vector2(1f, 0.165f));

        var playerViews = new CreatureUnitView[5];
        var enemyViews = new CreatureUnitView[5];
        for (int i = 0; i < 5; i++)
        {
            bool front = i < 2;
            playerViews[i] = CreateUnitSlot(front ? playerFront : playerBack, "PlayerSlot" + i, playerColor);
            enemyViews[i] = CreateUnitSlot(front ? enemyFront : enemyBack, "EnemySlot" + i, enemyColor);
        }

        Button speedButton = CreateButton(screen, "1배속");
        Text speedLabel = speedButton.GetComponentInChildren<Text>();

        var battle = screen.AddComponent<BattleScreen>();
        var so = new SerializedObject(battle);
        so.FindProperty("_run").objectReferenceValue = run;
        var typeChart = AssetDatabase.LoadAssetAtPath<TypeChart>(TypeChartPath);
        if (typeChart == null)
        {
            Debug.LogError("TypeChart 에셋이 없습니다. 먼저 MonsterCamp > Create Initial Creature Data 를 실행하세요.");
        }
        so.FindProperty("_typeChart").objectReferenceValue = typeChart;
        so.FindProperty("_synergy").objectReferenceValue = GetOrCreateSynergyConfig();
        SetArray(so.FindProperty("_playerViews"), playerViews);
        SetArray(so.FindProperty("_enemyViews"), enemyViews);
        so.FindProperty("_enemyLabel").objectReferenceValue = enemyLabel;
        so.FindProperty("_bannerText").objectReferenceValue = bannerText;
        so.FindProperty("_speedLabel").objectReferenceValue = speedLabel;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(speedButton.onClick, battle.ToggleSpeed);
        return battle;
    }

    // 전투 칸: 바탕(빈 칸 표시) + 크리처(체력바, 그림, 이름). 크리처가 죽으면 크리처만 사라진다
    private static CreatureUnitView CreateUnitSlot(Transform row, string name, Color color)
    {
        var slot = new GameObject(name, typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(row, false);
        slot.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.35f);

        var unit = new GameObject("Unit", typeof(RectTransform), typeof(Image), typeof(CreatureUnitView));
        unit.transform.SetParent(slot.transform, false);
        Stretch(unit.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        unit.GetComponent<Image>().color = color;

        var healthBack = new GameObject("HealthBar", typeof(RectTransform), typeof(Image));
        healthBack.transform.SetParent(unit.transform, false);
        Stretch(healthBack.GetComponent<RectTransform>(), new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.95f));
        healthBack.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);

        var healthFill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        healthFill.transform.SetParent(healthBack.transform, false);
        Stretch(healthFill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        healthFill.GetComponent<Image>().color = new Color(0.3f, 0.9f, 0.35f);

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(unit.transform, false);
        Stretch(iconGo.GetComponent<RectTransform>(), new Vector2(0.15f, 0.26f), new Vector2(0.85f, 0.73f));
        var icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;

        // 체력바 바로 아래 레벨 별
        Text levelText = CreateText(unit.transform, "Level", "", 32, new Vector2(0f, 0.74f), new Vector2(1f, 0.85f));
        levelText.color = new Color(1f, 0.85f, 0.2f);

        Text nameText = CreateText(unit.transform, "Name", "", 30, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.24f));

        foreach (Image image in unit.GetComponentsInChildren<Image>())
        {
            image.raycastTarget = false;
        }
        slot.GetComponent<Image>().raycastTarget = false;

        var view = unit.GetComponent<CreatureUnitView>();
        var so = new SerializedObject(view);
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_levelText").objectReferenceValue = levelText;
        so.FindProperty("_healthFill").objectReferenceValue = healthFill.GetComponent<RectTransform>();
        so.ApplyModifiedPropertiesWithoutUndo();
        unit.SetActive(false);
        return view;
    }

    // 상점·배치 화면 (세로 1920 기준, 위에서부터)
    // (상단 표시: 라운드·목숨·골드) / 새로고침 / 상점 5칸 / 메시지 / 팀 앞줄 2칸 / 팀 뒷줄 3칸 / 대기석 6칸 / 전투 시작
    private static ShopScreen BuildShopUI(GameObject screen, RunManager run, Text goldText)
    {
        // 제목 자리는 상단 표시가 차지한다
        screen.transform.Find("Title").gameObject.SetActive(false);
        Stretch((RectTransform)screen.transform.Find("ButtonArea"), new Vector2(0.15f, 0.015f), new Vector2(0.85f, 0.105f));

        var refresh = new GameObject("RefreshButton", typeof(RectTransform), typeof(Image), typeof(Button));
        refresh.transform.SetParent(screen.transform, false);
        Stretch(refresh.GetComponent<RectTransform>(), new Vector2(0.3f, 0.875f), new Vector2(0.7f, 0.935f));
        refresh.GetComponent<Image>().color = new Color(0.4f, 0.75f, 0.95f);
        Text refreshLabel = CreateText(refresh.transform, "Text", "새로고침", 48, Vector2.zero, Vector2.one);
        refreshLabel.color = Color.black;

        // 상점 칸: 약 190 x 290
        Transform shopRow = CreateRow(screen.transform, "ShopSlots", new Vector2(0.03f, 0.715f), new Vector2(0.97f, 0.865f));
        var shopSlots = new CreatureSlotView[5];
        for (int i = 0; i < shopSlots.Length; i++)
        {
            shopSlots[i] = CreateSlot(shopRow, "ShopSlot" + i, "");
        }

        Text messageText = CreateText(screen.transform, "MessageText", "", 40,
            new Vector2(0f, 0.69f), new Vector2(1f, 0.713f));
        messageText.color = new Color(1f, 0.5f, 0.45f);

        // 팀 칸 바로 위: 지금 켜진 시너지
        Text synergyText = CreateText(screen.transform, "SynergyText", "", 34,
            new Vector2(0f, 0.667f), new Vector2(1f, 0.69f));
        synergyText.color = new Color(0.6f, 0.95f, 0.65f);

        // 팀 칸: 약 300 x 290 (가장 크게). 위가 앞줄
        Color teamColor = new Color(0.22f, 0.32f, 0.3f);
        Transform frontRow = CreateRow(screen.transform, "TeamFrontRow", new Vector2(0.2f, 0.515f), new Vector2(0.8f, 0.665f));
        Transform backRow = CreateRow(screen.transform, "TeamBackRow", new Vector2(0.03f, 0.355f), new Vector2(0.97f, 0.505f));
        var teamSlots = new CreatureSlotView[5];
        for (int i = 0; i < teamSlots.Length; i++)
        {
            bool front = i < 2;
            teamSlots[i] = CreateSlot(front ? frontRow : backRow, "TeamSlot" + i, front ? "앞줄" : "뒷줄");
            teamSlots[i].GetComponent<Image>().color = teamColor;
        }

        CreateText(screen.transform, "BenchLabel", "대기석 (끌어서 배치 / 눌러서 판매)", 38,
            new Vector2(0f, 0.31f), new Vector2(1f, 0.345f));

        // 대기석 칸: 약 160 x 330
        Transform benchRow = CreateRow(screen.transform, "BenchSlots", new Vector2(0.03f, 0.13f), new Vector2(0.97f, 0.305f));
        var benchSlots = new CreatureSlotView[6];
        for (int i = 0; i < benchSlots.Length; i++)
        {
            benchSlots[i] = CreateSlot(benchRow, "BenchSlot" + i, "");
        }

        // 끄는 그림은 가장 위에 그려지도록 마지막에 만든다
        var dragIconGo = new GameObject("DragIcon", typeof(RectTransform), typeof(Image));
        dragIconGo.transform.SetParent(screen.transform, false);
        var dragRect = dragIconGo.GetComponent<RectTransform>();
        dragRect.anchorMin = dragRect.anchorMax = new Vector2(0.5f, 0.5f);
        dragRect.sizeDelta = new Vector2(200, 200);
        var dragIcon = dragIconGo.GetComponent<Image>();
        dragIcon.raycastTarget = false;
        dragIcon.preserveAspect = true;
        dragIconGo.SetActive(false);

        var shop = screen.AddComponent<ShopScreen>();
        var so = new SerializedObject(shop);
        so.FindProperty("_config").objectReferenceValue = GetOrCreateShopConfig();
        so.FindProperty("_synergy").objectReferenceValue = GetOrCreateSynergyConfig();
        so.FindProperty("_synergyText").objectReferenceValue = synergyText;
        so.FindProperty("_run").objectReferenceValue = run;
        so.FindProperty("_goldText").objectReferenceValue = goldText;
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.FindProperty("_refreshLabel").objectReferenceValue = refreshLabel;
        SetArray(so.FindProperty("_shopSlots"), shopSlots);
        SetArray(so.FindProperty("_benchSlots"), benchSlots);
        SetArray(so.FindProperty("_teamSlots"), teamSlots);
        so.FindProperty("_dragIcon").objectReferenceValue = dragIcon;
        so.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < benchSlots.Length; i++)
        {
            AddDragHandler(benchSlots[i], shop, SlotArea.Bench, i);
        }
        for (int i = 0; i < teamSlots.Length; i++)
        {
            AddDragHandler(teamSlots[i], shop, SlotArea.Team, i);
        }

        UnityEventTools.AddVoidPersistentListener(refresh.GetComponent<Button>().onClick, shop.Refresh);
        for (int i = 0; i < shopSlots.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(shopSlots[i].GetComponent<Button>().onClick, shop.BuyAt, i);
        }
        for (int i = 0; i < benchSlots.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(benchSlots[i].GetComponent<Button>().onClick, shop.SellAt, i);
        }

        return shop;
    }

    // 상점 설정 에셋. 이미 있으면 그대로 쓴다 (조정한 값 보호).
    // 시너지 수치 에셋. 없으면 기본값(GAME_DESIGN.md 8-1)으로 만들고, 있으면 그대로 쓴다
    private static SynergyConfig GetOrCreateSynergyConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<SynergyConfig>(SynergyConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<SynergyConfig>();
            AssetDatabase.CreateAsset(config, SynergyConfigPath);
            AssetDatabase.SaveAssets();
        }
        return config;
    }

    private static ShopConfig GetOrCreateShopConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<ShopConfig>(ShopConfigPath);
        if (config != null)
        {
            return config;
        }

        string[] guids = AssetDatabase.FindAssets("t:CreatureData", new[] { CreatureFolder });
        if (guids.Length == 0)
        {
            Debug.LogError("크리처 데이터가 없습니다. 먼저 MonsterCamp > Create Initial Creature Data 를 실행하세요.");
        }

        config = ScriptableObject.CreateInstance<ShopConfig>();
        AssetDatabase.CreateAsset(config, ShopConfigPath);

        var so = new SerializedObject(config);
        SerializedProperty pool = so.FindProperty("_creaturePool");
        pool.arraySize = guids.Length;
        for (int i = 0; i < guids.Length; i++)
        {
            pool.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<CreatureData>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return config;
    }

    private static Transform CreateRow(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        var layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 16;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        return go.transform;
    }

    private static void AddDragHandler(CreatureSlotView slot, ShopScreen shop, SlotArea area, int index)
    {
        var handler = slot.gameObject.AddComponent<SlotDragHandler>();
        var so = new SerializedObject(handler);
        so.FindProperty("_shop").objectReferenceValue = shop;
        so.FindProperty("_area").enumValueIndex = (int)area;
        so.FindProperty("_index").intValue = index;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 배경 버튼 + 레벨 별 + 그림 + 이름 + 가격(또는 판매 안내) + 반짝임 덮개. 비어 있으면 emptyLabel 표시
    private static CreatureSlotView CreateSlot(Transform parent, string name, string emptyLabel)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CreatureSlotView));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.34f);

        Text levelText = CreateText(go.transform, "Level", "", 34, new Vector2(0f, 0.83f), new Vector2(1f, 0.98f));
        levelText.color = new Color(1f, 0.85f, 0.2f);

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(go.transform, false);
        Stretch(iconGo.GetComponent<RectTransform>(), new Vector2(0.15f, 0.4f), new Vector2(0.85f, 0.83f));
        var icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        Text nameText = CreateText(go.transform, "Name", "", 30, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.4f));
        Text priceText = CreateText(go.transform, "Price", "", 28, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.2f));
        priceText.color = new Color(1f, 0.85f, 0.3f);

        // 가장 위에 그려지도록 마지막에 만든다
        var flashGo = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        flashGo.transform.SetParent(go.transform, false);
        Stretch(flashGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var flash = flashGo.GetComponent<Image>();
        flash.color = Color.clear;
        flash.raycastTarget = false;

        var view = go.GetComponent<CreatureSlotView>();
        var so = new SerializedObject(view);
        so.FindProperty("_button").objectReferenceValue = go.GetComponent<Button>();
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_priceText").objectReferenceValue = priceText;
        so.FindProperty("_levelText").objectReferenceValue = levelText;
        so.FindProperty("_flash").objectReferenceValue = flash;
        so.FindProperty("_emptyLabel").stringValue = emptyLabel;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static Text CreateText(Transform parent, string name, string value, int size, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        var text = go.GetComponent<Text>();
        SetupText(text, value, size);
        // 칸이 좁아도 글자가 잘리지 않게
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 16;
        text.resizeTextMaxSize = size;
        return text;
    }

    private static void SetArray(SerializedProperty property, Object[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetupText(Text text, string value, int size)
    {
        text.text = value;
        text.font = _font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
