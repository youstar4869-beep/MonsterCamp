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

        GameObject title = CreateScreen(canvas, "TitleScreen", "몬스터 캠프");
        AddButton(title, "시작", flow, GameScreen.TrainerSelect);

        GameObject shop = CreateScreen(canvas, "ShopScreen", "상점 · 배치");
        AddButton(shop, "전투 시작", flow, GameScreen.Battle);
        ShopScreen shopScreen = BuildShopUI(shop);

        GameObject trainer = CreateScreen(canvas, "TrainerSelectScreen", "트레이너 선택");
        Button startRunButton = AddButton(trainer, "선택 완료", flow, GameScreen.Shop);
        // 화면을 켠 다음 새 런 시작 (순서 유지)
        UnityEventTools.AddVoidPersistentListener(startRunButton.onClick, shopScreen.StartRun);
        trainer.transform.SetSiblingIndex(title.transform.GetSiblingIndex() + 1);

        GameObject battle = CreateScreen(canvas, "BattleScreen", "전투");
        AddButton(battle, "전투 종료", flow, GameScreen.Result);

        GameObject result = CreateScreen(canvas, "ResultScreen", "결과");
        AddButton(result, "다음 라운드", flow, GameScreen.Shop);
        AddButton(result, "타이틀로", flow, GameScreen.Title);

        // GameScreen enum 순서와 같아야 한다.
        var serializedFlow = new SerializedObject(flow);
        SerializedProperty screens = serializedFlow.FindProperty("_screens");
        GameObject[] ordered = { title, trainer, shop, battle, result };
        screens.arraySize = ordered.Length;
        for (int i = 0; i < ordered.Length; i++)
        {
            screens.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
        }
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

    private static Button AddButton(GameObject screen, string label, ScreenFlow flow, GameScreen target)
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

        var button = go.GetComponent<Button>();
        UnityEventTools.AddIntPersistentListener(button.onClick, flow.ShowScreen, (int)target);
        return button;
    }

    // 상점·배치 화면 (세로 1920 기준, 위에서부터)
    // 제목 / 골드·새로고침 / 상점 5칸 / 메시지 / 팀 앞줄 2칸 / 팀 뒷줄 3칸 / 대기석 6칸 / 전투 시작
    private static ShopScreen BuildShopUI(GameObject screen)
    {
        Stretch((RectTransform)screen.transform.Find("Title"), new Vector2(0f, 0.935f), new Vector2(1f, 0.985f));
        Stretch((RectTransform)screen.transform.Find("ButtonArea"), new Vector2(0.15f, 0.015f), new Vector2(0.85f, 0.105f));

        Text goldText = CreateText(screen.transform, "GoldText", "골드", 56,
            new Vector2(0.05f, 0.875f), new Vector2(0.5f, 0.93f));
        goldText.alignment = TextAnchor.MiddleLeft;

        var refresh = new GameObject("RefreshButton", typeof(RectTransform), typeof(Image), typeof(Button));
        refresh.transform.SetParent(screen.transform, false);
        Stretch(refresh.GetComponent<RectTransform>(), new Vector2(0.55f, 0.875f), new Vector2(0.95f, 0.93f));
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

        Text messageText = CreateText(screen.transform, "MessageText", "", 44,
            new Vector2(0f, 0.68f), new Vector2(1f, 0.71f));
        messageText.color = new Color(1f, 0.5f, 0.45f);

        // 팀 칸: 약 300 x 290 (가장 크게). 위가 앞줄(적과 가까운 쪽)
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

    // 배경 버튼 + 그림 + 이름 + 가격(또는 판매 안내). 비어 있으면 emptyLabel 표시
    private static CreatureSlotView CreateSlot(Transform parent, string name, string emptyLabel)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CreatureSlotView));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.34f);

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(go.transform, false);
        Stretch(iconGo.GetComponent<RectTransform>(), new Vector2(0.15f, 0.4f), new Vector2(0.85f, 0.92f));
        var icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        Text nameText = CreateText(go.transform, "Name", "", 30, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.4f));
        Text priceText = CreateText(go.transform, "Price", "", 28, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.2f));
        priceText.color = new Color(1f, 0.85f, 0.3f);

        var view = go.GetComponent<CreatureSlotView>();
        var so = new SerializedObject(view);
        so.FindProperty("_button").objectReferenceValue = go.GetComponent<Button>();
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_priceText").objectReferenceValue = priceText;
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
