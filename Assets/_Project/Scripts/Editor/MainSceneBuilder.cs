using System.IO;
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
    private const string TrainerFolder = "Assets/_Project/Data/Trainers";
    private const string DifficultyFolder = "Assets/_Project/Data/Difficulty";
    private static readonly string[] DifficultyIds = { "Normal", "Hard", "Impossible" };
    private const string ItemFolder = "Assets/_Project/Data/Items";
    private const string MetaConfigPath = "Assets/_Project/Data/MetaConfig.asset";
    private const string LockSpritePath = "Assets/_Project/Art/Placeholders/UI/Lock.png";
    private const string TutorialFolder = "Assets/_Project/Data/Tutorial";
    // 전투 스킬 글·데미지 숫자 풀 크기 (동시에 떠 있을 수 있는 글 수)
    private const int FloatingTextCount = 40;
    // 도감 크리처 칸 (1080x1920 기준)
    private const int CodexColumns = 5;
    private const float CodexCellWidth = 186f;
    private const string IllustrationResourcesFolder = IllustrationImporter.Folder + "Resources";
    private const float CodexRowHeight = 290f;
    private const string TutorialDataPath = "Assets/_Project/Data/Tutorial/TutorialData.asset";
    private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

    private static Font _font;
    // 색·글자 크기·둥근 모서리 (Data/UITheme)
    private static UITheme _theme;
    // 화면 배경 그림 (Data/BackgroundSet)
    private static BackgroundSet _backgrounds;

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

        _theme = UIThemeBuilder.GetOrCreate();
        // 한글 글꼴이 없으면 Unity 기본 글꼴 (기기 글꼴로 한글을 대신 그림)
        _font = _theme.Font != null ? _theme.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        // 톱니바퀴 그림은 씬을 만들기 전에 준비 (만드는 중에 에셋을 가져오면 연결이 끊길 수 있음)
        Sprite gearSprite = HudIconBuilder.Gear;
        _backgrounds = BackgroundSetBuilder.GetOrCreate();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateEventSystem();
        // 모든 화면은 안전 영역(노치·둥근 모서리 제외) 안에 만든다. 바깥은 카메라 배경색(테마 바탕색)
        Transform canvasRoot = CreateCanvas();
        Transform canvas = CreateSafeArea(canvasRoot);

        var flow = new GameObject("ScreenFlow").AddComponent<ScreenFlow>();
        var run = new GameObject("RunManager").AddComponent<RunManager>();
        var tutorial = new GameObject("Tutorial").AddComponent<TutorialController>();
        var meta = new GameObject("MetaProgress").AddComponent<MetaProgress>();
        var serializedMeta = new SerializedObject(meta);
        serializedMeta.FindProperty("_config").objectReferenceValue = GetOrCreateMetaConfig();
        serializedMeta.ApplyModifiedPropertiesWithoutUndo();
        var audio = new GameObject("AudioManager").AddComponent<AudioManager>();
        var serializedAudio = new SerializedObject(audio);
        serializedAudio.FindProperty("_config").objectReferenceValue = BgmBuilder.GetOrCreateConfig();
        serializedAudio.ApplyModifiedPropertiesWithoutUndo();

        // 배경: 시작화면 쪽은 Title, 상점은 Shop, 전투는 층마다 BattleScreen이 바꾼다 (처음엔 1층)
        Sprite titleBg = _backgrounds.Title;
        GameObject title = CreateScreen(canvas, "TitleScreen", "몬스터 캠프", titleBg);
        GameObject trainer = CreateScreen(canvas, "TrainerSelectScreen", "트레이너 선택", titleBg);
        GameObject shop = CreateScreen(canvas, "ShopScreen", "상점 · 배치", _backgrounds.Shop);
        GameObject battle = CreateScreen(canvas, "BattleScreen", "전투", _backgrounds.GetBattle(1, false));
        GameObject result = CreateScreen(canvas, "ResultScreen", "결과", titleBg);
        GameObject codex = CreateScreen(canvas, "CodexScreen", "도감", titleBg);
        GameObject difficulty = CreateScreen(canvas, "DifficultySelectScreen", "난이도 선택", titleBg);
        DifficultyData[] difficulties = LoadDifficulties();

        // 화면들보다 나중에 만들어 가장 위에 그려지게 한다
        HudView hud = CreateHud(canvas, difficulties, out Text goldText);
        // 정보 창·시너지 표: 화면·상단 표시보다 위, 튜토리얼보다 아래 (다른 화면에서도 다시 쓸 수 있게 캔버스에)
        CreatureInfoPopup infoPopup = CreateCreatureInfoPopup(canvas);
        ItemInfoPopup itemPopup = CreateItemInfoPopup(canvas);
        SynergyPanel synergyPanel = CreateSynergyPanel(canvas);
        FusionPanel fusionPanel = CreateFusionPanel(canvas);
        FusionBookPanel fusionBookPanel = CreateFusionBookPanel(canvas);
        // 도움말 창은 조합 창보다 위 ("새 조합식 발견!")
        TipPopup tipPopup = CreateTipPopup(canvas);

        // 처음 ★★★ 일러스트 연출: 노치까지 화면 전체를 덮도록 캔버스 전체에 (열 때 맨 위로 올라감). 도감 다시 보기도 같이 씀
        Lv3Showcase lv3Showcase = CreateLv3Showcase(canvasRoot, meta);

        BuildTitleUI(title, flow, run, tutorial);
        BuildCodexUI(codex, flow, meta, lv3Showcase);

        // 결과 화면: 클리어/게임 오버 + 설명 + 별 + 타이틀로
        Text resultTitle = result.transform.Find("Title").GetComponent<Text>();
        Text resultDetail = CreateText(result.transform, "Detail", "", _theme.HeadingSize, new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.72f));
        Text resultStars = CreateText(result.transform, "Stars", "", _theme.HeadingSize, new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.6f));
        resultStars.color = _theme.Gold;
        AddButton(result, "타이틀로", flow, GameScreen.Title);

        BattleScreen battleScreen = BuildBattleUI(battle, run);

        // 팀이 비었는지 확인한 뒤 전투로 넘어간다
        Button battleButton = CreateButton(shop, "전투 시작");
        ShopScreen shopScreen = BuildShopUI(shop, run, meta, hud, goldText, infoPopup, itemPopup, synergyPanel, fusionPanel, fusionBookPanel, tipPopup,
            out Text trainerText);
        UnityEventTools.AddVoidPersistentListener(battleButton.onClick, shopScreen.StartBattle);

        // 튜토리얼 화면은 모든 화면·상단 표시보다 위에 그려지도록 여기서 만든다
        BuildTutorialUI(canvas, tutorial, meta, shopScreen, (RectTransform)battleButton.transform, infoPopup);
        // ⚙ 버튼과 설정 창은 튜토리얼 중에도 누를 수 있게 그 위에 (씬에 하나)
        BuildSettingsUI(canvas, gearSprite, flow, run, audio);
        var serializedShop = new SerializedObject(shopScreen);
        serializedShop.FindProperty("_lv3Showcase").objectReferenceValue = lv3Showcase;
        serializedShop.ApplyModifiedPropertiesWithoutUndo();

        // 트레이너 → 난이도 → 새 런 시작 (1층 상점으로 이동은 RunManager가 한다)
        DifficultySelectScreen difficultySelect = BuildDifficultySelectUI(difficulty, run, flow, meta, difficulties);
        BuildTrainerSelectUI(trainer, difficultySelect, meta);

        var runConfig = AssetDatabase.LoadAssetAtPath<RunConfig>(RunConfigPath);
        if (runConfig == null)
        {
            Debug.LogError("RunConfig 에셋이 없습니다. 먼저 MonsterCamp > Create Round Data 를 실행하세요.");
        }
        var serializedRun = new SerializedObject(run);
        serializedRun.FindProperty("_config").objectReferenceValue = runConfig;
        SetArray(serializedRun.FindProperty("_trainers"), GetOrCreateTrainers());
        SetArray(serializedRun.FindProperty("_difficulties"), difficulties);
        serializedRun.FindProperty("_flow").objectReferenceValue = flow;
        serializedRun.FindProperty("_shop").objectReferenceValue = shopScreen;
        serializedRun.FindProperty("_battle").objectReferenceValue = battleScreen;
        serializedRun.FindProperty("_meta").objectReferenceValue = meta;
        serializedRun.FindProperty("_tutorial").objectReferenceValue = tutorial;
        serializedRun.FindProperty("_tip").objectReferenceValue = tipPopup;
        serializedRun.FindProperty("_resultStars").objectReferenceValue = resultStars;
        serializedRun.FindProperty("_hud").objectReferenceValue = hud;
        serializedRun.FindProperty("_trainerText").objectReferenceValue = trainerText;
        serializedRun.FindProperty("_resultTitle").objectReferenceValue = resultTitle;
        serializedRun.FindProperty("_resultDetail").objectReferenceValue = resultDetail;
        serializedRun.ApplyModifiedPropertiesWithoutUndo();

        // GameScreen enum 순서와 같아야 한다.
        var serializedFlow = new SerializedObject(flow);
        SetArray(serializedFlow.FindProperty("_screens"), new Object[] { title, trainer, shop, battle, result, codex, difficulty });
        serializedFlow.FindProperty("_hud").objectReferenceValue = hud.gameObject;
        // 페이드 판은 튜토리얼까지 모두 덮도록 맨 마지막에, 안전 영역 밖까지 캔버스 전체에 만든다
        serializedFlow.FindProperty("_fader").objectReferenceValue = CreateFader(canvasRoot);
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
        cam.backgroundColor = _theme.Background;
        // 소리를 듣는 곳. 씬에 하나만 있어야 한다 (빈 씬에서 만들므로 이 카메라 것 하나뿐)
        go.AddComponent<AudioListener>();
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

    // 캔버스 안의 안전 영역 판 (SafeAreaFitter가 실행 중 Screen.safeArea에 맞춘다)
    private static Transform CreateSafeArea(Transform canvas)
    {
        var go = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
        go.transform.SetParent(canvas, false);
        Stretch(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        return go.transform;
    }

    // 둥근 모서리 (radius: 1080x1920 기준 픽셀, 0이면 테마 기본값)
    private static void Round(Image image, float radius = 0f)
    {
        UIThemeBuilder.Round(image, _theme, radius);
    }

    // 전체 화면 패널 + 배경 그림(Background, 안에 어두운 판 Shade) + 상단 제목 + 하단 버튼 영역(ButtonArea)
    private static GameObject CreateScreen(Transform canvas, string name, string titleText, Sprite background)
    {
        var screen = new GameObject(name, typeof(RectTransform), typeof(Image));
        screen.transform.SetParent(canvas, false);
        Stretch(screen.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        screen.GetComponent<Image>().color = _theme.Background;

        // 배경은 맨 먼저 만들어 크리처·숫자·버튼보다 뒤에 그린다. 비율을 지킨 채 화면을 꽉 채우고(넘치는 쪽은 잘림),
        // 안전 영역 밖(노치 쪽)까지 넘쳐도 그대로 그린다. 그림이 점 필터라 6배로 커져도 픽셀이 또렷하다
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
        bg.transform.SetParent(screen.transform, false);
        Stretch(bg.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var bgImage = bg.GetComponent<Image>();
        bgImage.sprite = background;
        bgImage.raycastTarget = false;
        if (background == null)
        {
            bgImage.color = _theme.Background;
        }
        var fitter = bg.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = ReferenceResolution.x / ReferenceResolution.y;

        // 어두운 판은 배경 그림과 같은 크기로 (안전 영역 밖까지 똑같이 어둡게)
        var shade = new GameObject("Shade", typeof(RectTransform), typeof(Image));
        shade.transform.SetParent(bg.transform, false);
        Stretch(shade.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var shadeImage = shade.GetComponent<Image>();
        shadeImage.color = _theme.BackgroundShade;
        shadeImage.raycastTarget = false;

        var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
        title.transform.SetParent(screen.transform, false);
        Stretch(title.GetComponent<RectTransform>(), new Vector2(0f, 0.75f), new Vector2(1f, 0.9f));
        SetupText(title.GetComponent<Text>(), titleText, _theme.TitleSize);

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

    // 타이틀 화면: 제목 / 안내 문구 / 이어하기(저장된 판이 있을 때만) / 새 게임 + 새 게임 확인 창
    private static void BuildTitleUI(GameObject screen, ScreenFlow flow, RunManager run, TutorialController tutorial)
    {
        // 버튼 3개(이어하기, 새 게임, 도감)가 들어가도록 버튼 영역을 넓히고, 맨 아래에 작은 "튜토리얼 다시 보기"
        Stretch((RectTransform)screen.transform.Find("ButtonArea"), new Vector2(0.15f, 0.09f), new Vector2(0.85f, 0.41f));
        Button continueButton = CreateButton(screen, "이어하기");
        Button newGameButton = CreateButton(screen, "새 게임");
        AddButton(screen, "도감", flow, GameScreen.Codex);
        Button replayButton = CreateFreeButton(screen.transform, "튜토리얼 다시 보기",
            new Vector2(0.28f, 0.015f), new Vector2(0.72f, 0.08f), _theme.ButtonSecondary);
        replayButton.GetComponentInChildren<Text>().resizeTextMaxSize = _theme.SmallSize;

        Text messageText = CreateText(screen.transform, "MessageText", "", _theme.BodySize, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.5f));
        messageText.color = _theme.Negative;

        // 확인 창: 화면 전체를 어둡게 덮어 뒤의 버튼이 눌리지 않게 한다 (가장 위에 그려지도록 마지막에 만든다)
        var panel = new GameObject("ConfirmPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(screen.transform, false);
        Stretch(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        panel.GetComponent<Image>().color = _theme.Dim;

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(panel.transform, false);
        Stretch(box.GetComponent<RectTransform>(), new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.64f));
        box.GetComponent<Image>().color = _theme.Panel;
        Round(box.GetComponent<Image>(), 36f);

        CreateText(box.transform, "Question", "진행 중인 판이 지워져요.\n새로 시작할까요?", _theme.LargeSize,
            new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.92f));
        Button confirmButton = CreateFreeButton(box.transform, "새로 시작", new Vector2(0.06f, 0.1f), new Vector2(0.48f, 0.4f),
            _theme.ButtonDanger);
        Button cancelButton = CreateFreeButton(box.transform, "취소", new Vector2(0.52f, 0.1f), new Vector2(0.94f, 0.4f),
            _theme.ButtonSecondary);
        panel.SetActive(false);

        var title = screen.AddComponent<TitleScreen>();
        var so = new SerializedObject(title);
        so.FindProperty("_flow").objectReferenceValue = flow;
        so.FindProperty("_run").objectReferenceValue = run;
        so.FindProperty("_tutorial").objectReferenceValue = tutorial;
        so.FindProperty("_continueButton").objectReferenceValue = continueButton.gameObject;
        so.FindProperty("_confirmPanel").objectReferenceValue = panel;
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(continueButton.onClick, title.Continue);
        UnityEventTools.AddVoidPersistentListener(newGameButton.onClick, title.NewGame);
        UnityEventTools.AddVoidPersistentListener(confirmButton.onClick, title.ConfirmNewGame);
        UnityEventTools.AddVoidPersistentListener(cancelButton.onClick, title.CancelNewGame);
        UnityEventTools.AddVoidPersistentListener(replayButton.onClick, title.ReplayTutorial);
    }

    // 모든 화면 오른쪽 위 ⚙ 버튼 (상단 표시 줄 오른쪽 끝, 안전 영역 안) + 설정 창 (창 바깥을 누르면 닫힘)
    private static void BuildSettingsUI(Transform canvas, Sprite gearSprite, ScreenFlow flow, RunManager run, AudioManager audio)
    {
        Button gearButton = CreateFreeButton(canvas, "", new Vector2(0.905f, 0.948f), new Vector2(0.995f, 0.997f),
            _theme.ButtonSecondary);
        gearButton.name = "SettingsButton";
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(gearButton.transform, false);
        Stretch(iconGo.GetComponent<RectTransform>(), new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
        var icon = iconGo.GetComponent<Image>();
        icon.sprite = gearSprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        RectTransform root = CreateModalRoot(canvas, "SettingsPanel", out Button backgroundButton);
        RectTransform window = CreatePanel(root, "Window", _theme.Panel, true);
        Round(window.GetComponent<Image>(), 36f);
        Stretch(window, new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.72f));

        Text titleText = CreateText(window, "Title", "설정", _theme.HeadingSize, new Vector2(0.05f, 0.86f), new Vector2(0.72f, 0.97f));
        titleText.alignment = TextAnchor.MiddleLeft;
        Button closeButton = CreateFreeButton(window, "닫기", new Vector2(0.74f, 0.86f), new Vector2(0.96f, 0.97f),
            _theme.ButtonSecondary);

        Button sfxButton = CreateFreeButton(window, "효과음 켜짐", new Vector2(0.06f, 0.7f), new Vector2(0.94f, 0.81f),
            _theme.ButtonSecondary);
        Slider sfxSlider = CreateVolumeSlider(window, "효과음", 0.58f, 0.68f);
        Button bgmButton = CreateFreeButton(window, "배경음 켜짐", new Vector2(0.06f, 0.43f), new Vector2(0.94f, 0.54f),
            _theme.ButtonSecondary);
        Slider bgmSlider = CreateVolumeSlider(window, "배경음", 0.31f, 0.41f);
        Button exitButton = CreateFreeButton(window, "타이틀로 나가기", new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.19f),
            _theme.ButtonDanger);

        // 나가기 확인 창: 설정 창 전체를 어둡게 덮어 뒤의 버튼이 눌리지 않게 한다
        RectTransform confirmPanel = CreatePanel(root, "ConfirmPanel", _theme.Dim, true);
        RectTransform box = CreatePanel(confirmPanel, "Box", _theme.Panel, true);
        Round(box.GetComponent<Image>(), 36f);
        Stretch(box, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.62f));
        CreateText(box, "Question", "진행 상황은 저장됩니다.\n타이틀로 나갈까요?", _theme.LargeSize,
            new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.92f));
        Button confirmButton = CreateFreeButton(box, "나가기", new Vector2(0.06f, 0.1f), new Vector2(0.48f, 0.38f),
            _theme.ButtonPrimary);
        Button cancelButton = CreateFreeButton(box, "취소", new Vector2(0.52f, 0.1f), new Vector2(0.94f, 0.38f),
            _theme.ButtonSecondary);
        confirmPanel.gameObject.SetActive(false);

        var panel = root.gameObject.AddComponent<SettingsPanel>();
        var so = new SerializedObject(panel);
        so.FindProperty("_flow").objectReferenceValue = flow;
        so.FindProperty("_run").objectReferenceValue = run;
        so.FindProperty("_audio").objectReferenceValue = audio;
        so.FindProperty("_sfxLabel").objectReferenceValue = sfxButton.GetComponentInChildren<Text>();
        so.FindProperty("_bgmLabel").objectReferenceValue = bgmButton.GetComponentInChildren<Text>();
        so.FindProperty("_sfxSlider").objectReferenceValue = sfxSlider;
        so.FindProperty("_bgmSlider").objectReferenceValue = bgmSlider;
        so.FindProperty("_exitButton").objectReferenceValue = exitButton.gameObject;
        so.FindProperty("_confirmPanel").objectReferenceValue = confirmPanel.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(gearButton.onClick, panel.Open);
        UnityEventTools.AddVoidPersistentListener(backgroundButton.onClick, panel.Close);
        UnityEventTools.AddVoidPersistentListener(closeButton.onClick, panel.Close);
        UnityEventTools.AddVoidPersistentListener(sfxButton.onClick, panel.ToggleSfx);
        UnityEventTools.AddVoidPersistentListener(bgmButton.onClick, panel.ToggleBgm);
        UnityEventTools.AddPersistentListener(sfxSlider.onValueChanged, panel.SetSfxVolume);
        UnityEventTools.AddPersistentListener(bgmSlider.onValueChanged, panel.SetBgmVolume);
        UnityEventTools.AddVoidPersistentListener(exitButton.onClick, panel.AskExit);
        UnityEventTools.AddVoidPersistentListener(confirmButton.onClick, panel.ConfirmExit);
        UnityEventTools.AddVoidPersistentListener(cancelButton.onClick, panel.CancelExit);
        root.gameObject.SetActive(false);
    }

    // 음량 슬라이더 한 줄: 왼쪽 이름 + 막대(0~1, 처음 0.7). 손잡이는 크게 해서 손가락으로 잡기 쉽게
    private static Slider CreateVolumeSlider(Transform parent, string label, float yMin, float yMax)
    {
        var row = new GameObject(label + "Volume", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        Stretch(row.GetComponent<RectTransform>(), new Vector2(0.1f, yMin), new Vector2(0.9f, yMax));
        Text text = CreateLeftText(row.transform, "Label", _theme.BodySize, Vector2.zero, new Vector2(0.25f, 1f));
        text.text = label;

        var resources = new DefaultControls.Resources
        {
            standard = _theme.RoundedSprite,
            background = _theme.RoundedSprite,
            knob = _theme.RoundedSprite,
        };
        GameObject go = DefaultControls.CreateSlider(resources);
        go.name = "Slider";
        go.transform.SetParent(row.transform, false);
        Stretch(go.GetComponent<RectTransform>(), new Vector2(0.28f, 0f), Vector2.one);

        // 막대는 가운데 얇게, 손잡이는 줄 높이 대부분 x 70px
        var background = (RectTransform)go.transform.Find("Background");
        Stretch(background, new Vector2(0f, 0.35f), new Vector2(1f, 0.65f));
        background.GetComponent<Image>().color = _theme.Slot;
        Round(background.GetComponent<Image>(), 16f);
        var fillArea = (RectTransform)go.transform.Find("Fill Area");
        Stretch(fillArea, new Vector2(0f, 0.35f), new Vector2(1f, 0.65f));
        // 채움은 막대 왼쪽 끝에서 손잡이 가운데까지 (손잡이 영역과 같은 안쪽 여백)
        fillArea.offsetMin = new Vector2(35f, 0f);
        fillArea.offsetMax = new Vector2(-35f, 0f);
        Image fill = fillArea.Find("Fill").GetComponent<Image>();
        fill.rectTransform.offsetMin = new Vector2(-35f, 0f);
        fill.rectTransform.offsetMax = Vector2.zero;
        fill.color = _theme.Accent;
        Round(fill, 16f);
        var handleArea = (RectTransform)go.transform.Find("Handle Slide Area");
        Stretch(handleArea, new Vector2(0f, 0.08f), new Vector2(1f, 0.92f));
        handleArea.offsetMin = new Vector2(35f, 0f);
        handleArea.offsetMax = new Vector2(-35f, 0f);
        Image handle = handleArea.Find("Handle").GetComponent<Image>();
        handle.rectTransform.sizeDelta = new Vector2(70f, 0f);
        handle.color = _theme.ButtonPrimary;
        Round(handle, 24f);

        var slider = go.GetComponent<Slider>();
        slider.value = 0.7f;
        return slider;
    }

    // 위치를 직접 정하는 버튼 (ButtonArea 밖)
    private static Button CreateFreeButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        go.GetComponent<Image>().color = color;
        Round(go.GetComponent<Image>());
        Text text = CreateText(go.transform, "Text", label, _theme.LargeSize, Vector2.zero, Vector2.one);
        text.color = _theme.ButtonText;
        Button button = go.GetComponent<Button>();
        AddPressFeedback(button);
        return button;
    }

    // 누름 효과: 누르는 동안 더 어둡게 + 살짝 작게
    private static void AddPressFeedback(Button button)
    {
        ColorBlock colors = button.colors;
        colors.pressedColor = _theme.ButtonPressed;
        button.colors = colors;
        button.gameObject.AddComponent<ButtonPressFeedback>();
    }

    // 보이는 크기는 그대로 두고 눌리는 범위만 넓힌다 (투명한 판, 1080x1920 기준 픽셀).
    // 버튼이 작을 때 손가락으로 누르기 쉽게 48dp(약 120px) 이상으로 맞추는 데 쓴다
    private static void AddHitArea(Component target, float left, float bottom, float right, float top)
    {
        var go = new GameObject("HitArea", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(target.transform, false);
        go.transform.SetAsFirstSibling();
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-left, -bottom);
        rect.offsetMax = new Vector2(right, top);
        go.GetComponent<Image>().color = Color.clear;
    }

    // 크리처 정보 창: 어두운 바탕(누르면 닫힘) + 가운데 창
    // 창: 그림 / 이름 / 타입·가격·레벨 / 능력치 / 능력 / 장착 아이템 / 시너지 한 줄 / 안내 / 버튼(왼쪽 확인, 오른쪽 선택·취소)
    private static CreatureInfoPopup CreateCreatureInfoPopup(Transform canvas)
    {
        RectTransform root = CreateModalRoot(canvas, "CreatureInfoPopup", out Button backgroundButton);
        RectTransform window = CreatePanel(root, "Window", _theme.Panel, true);
        Round(window.GetComponent<Image>(), 36f);
        Stretch(window, new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.8f));

        Button closeButton = CreateFreeButton(window, "닫기", new Vector2(0.74f, 0.875f), new Vector2(0.97f, 0.985f),
            _theme.ButtonSecondary);
        closeButton.GetComponentInChildren<Text>().resizeTextMaxSize = _theme.BodySize;

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(window, false);
        Stretch(iconGo.GetComponent<RectTransform>(), new Vector2(0.04f, 0.63f), new Vector2(0.34f, 0.95f));
        var icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        Text nameText = CreateLeftText(window, "Name", _theme.HeadingSize, new Vector2(0.37f, 0.83f), new Vector2(0.72f, 0.95f));
        Text infoText = CreateLeftText(window, "Info", _theme.BodySize, new Vector2(0.37f, 0.73f), new Vector2(0.97f, 0.82f));
        infoText.color = _theme.Gold;
        Text statsText = CreateLeftText(window, "Stats", _theme.SmallSize, new Vector2(0.37f, 0.64f), new Vector2(0.97f, 0.72f));
        Text skillText = CreateLeftText(window, "Skill", _theme.SmallSize, new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.61f));
        // 장착 아이템 줄: 그림 / 이름·설명 / 해제 / 판매
        ItemIconView itemIcon = CreateItemIcon(window, new Vector2(0.04f, 0.365f), new Vector2(0.13f, 0.445f), _theme.BodySize);
        Text itemText = CreateLeftText(window, "Item", _theme.SmallSize, new Vector2(0.15f, 0.36f), new Vector2(0.56f, 0.45f));
        itemText.color = _theme.TextSub;
        Button unequipButton = CreateFreeButton(window, "해제", new Vector2(0.57f, 0.37f), new Vector2(0.74f, 0.44f),
            _theme.ButtonSecondary);
        unequipButton.GetComponentInChildren<Text>().resizeTextMaxSize = _theme.BodySize;
        Button itemSellButton = CreateFreeButton(window, "판매", new Vector2(0.76f, 0.37f), new Vector2(0.95f, 0.44f),
            _theme.ButtonPrimary);
        itemSellButton.GetComponentInChildren<Text>().resizeTextMaxSize = _theme.BodySize;
        unequipButton.gameObject.SetActive(false);
        itemSellButton.gameObject.SetActive(false);
        Text synergyText = CreateLeftText(window, "Synergy", _theme.SmallSize, new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.35f));
        synergyText.color = _theme.Positive;
        Text messageText = CreateText(window, "Message", "", _theme.BodySize, new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.25f));
        messageText.color = _theme.Negative;

        // 선택 버튼(오른쪽)과 확인 단계의 취소(같은 자리), 실행(반대쪽 왼쪽)
        Button actionButton = CreateFreeButton(window, "구매", new Vector2(0.52f, 0.03f), new Vector2(0.95f, 0.15f),
            _theme.ButtonPrimary);
        Button cancelButton = CreateFreeButton(window, "취소", new Vector2(0.52f, 0.03f), new Vector2(0.95f, 0.15f),
            _theme.ButtonSecondary);
        Button confirmButton = CreateFreeButton(window, "팔기", new Vector2(0.05f, 0.03f), new Vector2(0.48f, 0.15f),
            _theme.ButtonDanger);
        actionButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        confirmButton.gameObject.SetActive(false);

        var popup = root.gameObject.AddComponent<CreatureInfoPopup>();
        var so = new SerializedObject(popup);
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_infoText").objectReferenceValue = infoText;
        so.FindProperty("_statsText").objectReferenceValue = statsText;
        so.FindProperty("_skillText").objectReferenceValue = skillText;
        so.FindProperty("_itemText").objectReferenceValue = itemText;
        so.FindProperty("_itemIcon").objectReferenceValue = itemIcon;
        so.FindProperty("_unequipButton").objectReferenceValue = unequipButton;
        so.FindProperty("_itemSellButton").objectReferenceValue = itemSellButton;
        so.FindProperty("_itemSellLabel").objectReferenceValue = itemSellButton.GetComponentInChildren<Text>(true);
        so.FindProperty("_synergyText").objectReferenceValue = synergyText;
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.FindProperty("_closeButton").objectReferenceValue = closeButton.transform;
        so.FindProperty("_actionButton").objectReferenceValue = actionButton;
        so.FindProperty("_actionLabel").objectReferenceValue = actionButton.GetComponentInChildren<Text>(true);
        so.FindProperty("_confirmButton").objectReferenceValue = confirmButton;
        so.FindProperty("_confirmLabel").objectReferenceValue = confirmButton.GetComponentInChildren<Text>(true);
        so.FindProperty("_cancelButton").objectReferenceValue = cancelButton;
        so.FindProperty("_theme").objectReferenceValue = _theme;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(backgroundButton.onClick, popup.Close);
        UnityEventTools.AddVoidPersistentListener(closeButton.onClick, popup.Close);
        UnityEventTools.AddVoidPersistentListener(actionButton.onClick, popup.OnActionClicked);
        UnityEventTools.AddVoidPersistentListener(confirmButton.onClick, popup.OnConfirmClicked);
        UnityEventTools.AddVoidPersistentListener(cancelButton.onClick, popup.OnCancelClicked);
        UnityEventTools.AddVoidPersistentListener(unequipButton.onClick, popup.OnUnequipClicked);
        UnityEventTools.AddVoidPersistentListener(itemSellButton.onClick, popup.OnItemSellClicked);

        root.gameObject.SetActive(false);
        return popup;
    }

    // 보관함 아이템 창: 어두운 바탕(누르면 닫힘) + 작은 창
    // 창: 닫기 / 그림 / 이름 / 설명 / 안내 / 버튼(왼쪽 확인, 오른쪽 판매·취소)
    private static ItemInfoPopup CreateItemInfoPopup(Transform canvas)
    {
        RectTransform root = CreateModalRoot(canvas, "ItemInfoPopup", out Button backgroundButton);
        RectTransform window = CreatePanel(root, "Window", _theme.Panel, true);
        Round(window.GetComponent<Image>(), 36f);
        Stretch(window, new Vector2(0.12f, 0.36f), new Vector2(0.88f, 0.64f));

        Button closeButton = CreateFreeButton(window, "닫기", new Vector2(0.7f, 0.82f), new Vector2(0.96f, 0.97f),
            _theme.ButtonSecondary);
        closeButton.GetComponentInChildren<Text>().resizeTextMaxSize = _theme.BodySize;

        ItemIconView icon = CreateItemIcon(window, new Vector2(0.05f, 0.5f), new Vector2(0.3f, 0.95f), _theme.HugeSize);
        Text nameText = CreateLeftText(window, "Name", _theme.HeadingSize, new Vector2(0.34f, 0.66f), new Vector2(0.97f, 0.8f));
        Text descriptionText = CreateLeftText(window, "Description", _theme.SmallSize, new Vector2(0.34f, 0.5f), new Vector2(0.97f, 0.65f));
        descriptionText.color = _theme.TextSub;
        Text messageText = CreateText(window, "Message", "", _theme.BodySize, new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.47f));
        messageText.color = _theme.Negative;

        Button sellButton = CreateFreeButton(window, "판매", new Vector2(0.52f, 0.05f), new Vector2(0.95f, 0.27f),
            _theme.ButtonPrimary);
        Button cancelButton = CreateFreeButton(window, "취소", new Vector2(0.52f, 0.05f), new Vector2(0.95f, 0.27f),
            _theme.ButtonSecondary);
        Button confirmButton = CreateFreeButton(window, "팔기", new Vector2(0.05f, 0.05f), new Vector2(0.48f, 0.27f),
            _theme.ButtonDanger);
        cancelButton.gameObject.SetActive(false);
        confirmButton.gameObject.SetActive(false);

        var popup = root.gameObject.AddComponent<ItemInfoPopup>();
        var so = new SerializedObject(popup);
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_descriptionText").objectReferenceValue = descriptionText;
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.FindProperty("_sellButton").objectReferenceValue = sellButton;
        so.FindProperty("_sellLabel").objectReferenceValue = sellButton.GetComponentInChildren<Text>(true);
        so.FindProperty("_confirmButton").objectReferenceValue = confirmButton;
        so.FindProperty("_cancelButton").objectReferenceValue = cancelButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(backgroundButton.onClick, popup.Close);
        UnityEventTools.AddVoidPersistentListener(closeButton.onClick, popup.Close);
        UnityEventTools.AddVoidPersistentListener(sellButton.onClick, popup.OnSellClicked);
        UnityEventTools.AddVoidPersistentListener(confirmButton.onClick, popup.OnConfirmClicked);
        UnityEventTools.AddVoidPersistentListener(cancelButton.onClick, popup.OnCancelClicked);

        root.gameObject.SetActive(false);
        return popup;
    }

    // 시너지 표: 어두운 바탕(누르면 닫힘) + 가운데 창. 타입마다 한 줄: "불 3/4" / 1단계 / 2단계,
    // 아래에 조합 시너지 한 줄씩: 이름 / 필요한 타입·효과
    private static SynergyPanel CreateSynergyPanel(Transform canvas)
    {
        RectTransform root = CreateModalRoot(canvas, "SynergyPanel", out Button backgroundButton);
        RectTransform window = CreatePanel(root, "Window", _theme.Panel, true);
        Round(window.GetComponent<Image>(), 36f);
        Stretch(window, new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.93f));

        CreateText(window, "Title", "타입 시너지", _theme.HeadingSize, new Vector2(0.05f, 0.93f), new Vector2(0.72f, 0.99f));
        Button closeButton = CreateFreeButton(window, "닫기", new Vector2(0.75f, 0.93f), new Vector2(0.97f, 0.993f),
            _theme.ButtonSecondary);
        closeButton.GetComponentInChildren<Text>().resizeTextMaxSize = _theme.BodySize;

        int typeCount = TypeSynergy.TypeCount;
        var typeTexts = new Text[typeCount];
        var tier1Texts = new Text[typeCount];
        var tier2Texts = new Text[typeCount];
        const float top = 0.92f;
        const float rowHeight = 0.085f;
        for (int i = 0; i < typeCount; i++)
        {
            float rowTop = top - rowHeight * i;
            float rowBottom = rowTop - rowHeight + 0.006f;
            RectTransform row = CreatePanel(window, "Row" + i, new Color(1f, 1f, 1f, 0.05f), false);
            Round(row.GetComponent<Image>(), 16f);
            Stretch(row, new Vector2(0.03f, rowBottom), new Vector2(0.97f, rowTop));
            typeTexts[i] = CreateLeftText(row, "Type", _theme.LargeSize, new Vector2(0.03f, 0.1f), new Vector2(0.27f, 0.9f));
            tier1Texts[i] = CreateLeftText(row, "Tier1", _theme.TinySize, new Vector2(0.29f, 0.52f), new Vector2(0.98f, 0.95f));
            tier2Texts[i] = CreateLeftText(row, "Tier2", _theme.TinySize, new Vector2(0.29f, 0.05f), new Vector2(0.98f, 0.48f));
        }

        // 조합 시너지 (MixedSynergyConfig 6개)
        const int mixedCount = 6;
        const float mixedTitleTop = top - rowHeight * 5 - 0.005f;
        CreateText(window, "MixedTitle", "조합 시너지 (서로 다른 타입 각 1마리)", _theme.BodySize,
            new Vector2(0.05f, mixedTitleTop - 0.05f), new Vector2(0.95f, mixedTitleTop));
        var mixedNameTexts = new Text[mixedCount];
        var mixedEffectTexts = new Text[mixedCount];
        const float mixedTop = mixedTitleTop - 0.05f;
        const float mixedRowHeight = 0.055f;
        for (int i = 0; i < mixedCount; i++)
        {
            float rowTop = mixedTop - mixedRowHeight * i;
            float rowBottom = rowTop - mixedRowHeight + 0.005f;
            RectTransform row = CreatePanel(window, "MixedRow" + i, new Color(1f, 1f, 1f, 0.05f), false);
            Round(row.GetComponent<Image>(), 16f);
            Stretch(row, new Vector2(0.03f, rowBottom), new Vector2(0.97f, rowTop));
            mixedNameTexts[i] = CreateLeftText(row, "Name", _theme.SmallSize, new Vector2(0.03f, 0.05f), new Vector2(0.22f, 0.95f));
            mixedEffectTexts[i] = CreateLeftText(row, "Effect", _theme.TinySize, new Vector2(0.23f, 0.05f), new Vector2(0.99f, 0.95f));
        }

        Text note = CreateText(window, "Note", "같은 크리처는 1마리로, 5골드 크리처는 2마리로 세요. 5타입이 모두 있으면 무지개만 켜져요",
            _theme.TinySize, new Vector2(0.03f, 0.005f), new Vector2(0.97f, 0.045f));
        note.color = _theme.TextSub;

        var panel = root.gameObject.AddComponent<SynergyPanel>();
        var so = new SerializedObject(panel);
        SetArray(so.FindProperty("_typeTexts"), typeTexts);
        SetArray(so.FindProperty("_tier1Texts"), tier1Texts);
        SetArray(so.FindProperty("_tier2Texts"), tier2Texts);
        SetArray(so.FindProperty("_mixedNameTexts"), mixedNameTexts);
        SetArray(so.FindProperty("_mixedEffectTexts"), mixedEffectTexts);
        so.FindProperty("_theme").objectReferenceValue = _theme;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(backgroundButton.onClick, panel.Close);
        UnityEventTools.AddVoidPersistentListener(closeButton.onClick, panel.Close);

        root.gameObject.SetActive(false);
        return panel;
    }

    // 조합 창: 어두운 바탕(누르면 닫힘) + 가운데 창.
    // 목록(제목, 닫기, 조합 줄 6개) / 확인(제목, "재료 A ★★ + 재료 B ★★ → 결과 ★★", 조합·취소) 중 하나만 보인다
    private static FusionPanel CreateFusionPanel(Transform canvas)
    {
        const int optionCount = 6;
        RectTransform root = CreateModalRoot(canvas, "FusionPanel", out Button backgroundButton);

        RectTransform list = CreatePanel(root, "ListWindow", _theme.Panel, true);
        Round(list.GetComponent<Image>(), 36f);
        Stretch(list, new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.75f));
        CreateText(list, "Title", "조합", _theme.HeadingSize, new Vector2(0.05f, 0.87f), new Vector2(0.72f, 0.98f));
        Button closeButton = CreateFreeButton(list, "닫기", new Vector2(0.75f, 0.88f), new Vector2(0.97f, 0.98f),
            _theme.ButtonSecondary);
        closeButton.GetComponentInChildren<Text>().resizeTextMaxSize = _theme.BodySize;
        var optionButtons = new Button[optionCount];
        var optionTexts = new Text[optionCount];
        const float top = 0.85f;
        const float rowHeight = 0.135f;
        for (int i = 0; i < optionCount; i++)
        {
            float rowTop = top - rowHeight * i;
            optionButtons[i] = CreateFreeButton(list, "Option" + i, new Vector2(0.04f, rowTop - rowHeight + 0.015f),
                new Vector2(0.96f, rowTop), _theme.ButtonInfo);
            optionTexts[i] = optionButtons[i].GetComponentInChildren<Text>();
            optionTexts[i].resizeTextMaxSize = _theme.BodySize;
        }

        RectTransform confirm = CreatePanel(root, "ConfirmWindow", _theme.Panel, true);
        Round(confirm.GetComponent<Image>(), 36f);
        Stretch(confirm, new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.64f));
        Text confirmTitle = CreateText(confirm, "Title", "조합할까요?", _theme.HeadingSize, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.95f));
        confirmTitle.color = _theme.Accent;
        Text confirmText = CreateText(confirm, "Message", "", _theme.LargeSize, new Vector2(0.04f, 0.3f), new Vector2(0.96f, 0.76f));
        Button okButton = CreateFreeButton(confirm, "조합", new Vector2(0.53f, 0.06f), new Vector2(0.9f, 0.26f), _theme.ButtonPrimary);
        Button cancelButton = CreateFreeButton(confirm, "취소", new Vector2(0.1f, 0.06f), new Vector2(0.47f, 0.26f),
            _theme.ButtonSecondary);

        var panel = root.gameObject.AddComponent<FusionPanel>();
        var so = new SerializedObject(panel);
        so.FindProperty("_listView").objectReferenceValue = list.gameObject;
        SetArray(so.FindProperty("_optionButtons"), optionButtons);
        SetArray(so.FindProperty("_optionTexts"), optionTexts);
        so.FindProperty("_confirmView").objectReferenceValue = confirm.gameObject;
        so.FindProperty("_confirmText").objectReferenceValue = confirmText;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(backgroundButton.onClick, panel.Close);
        UnityEventTools.AddVoidPersistentListener(closeButton.onClick, panel.Close);
        UnityEventTools.AddVoidPersistentListener(cancelButton.onClick, panel.Close);
        UnityEventTools.AddVoidPersistentListener(okButton.onClick, panel.Confirm);
        for (int i = 0; i < optionCount; i++)
        {
            UnityEventTools.AddIntPersistentListener(optionButtons[i].onClick, panel.Pick, i);
        }
        root.gameObject.SetActive(false);
        return panel;
    }

    // 상점 도움말 창: 어두운 바탕(누르면 닫힘) + 가운데 창(제목, 설명, 확인)
    private static TipPopup CreateTipPopup(Transform canvas)
    {
        RectTransform root = CreateModalRoot(canvas, "TipPopup", out Button backgroundButton);
        RectTransform window = CreatePanel(root, "Window", _theme.Panel, true);
        Round(window.GetComponent<Image>(), 36f);
        Stretch(window, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.62f));

        Text titleText = CreateText(window, "Title", "", _theme.HeadingSize, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.94f));
        titleText.color = _theme.Accent;
        Text messageText = CreateText(window, "Message", "", _theme.BodySize, new Vector2(0.05f, 0.33f), new Vector2(0.95f, 0.72f));
        Button okButton = CreateFreeButton(window, "확인", new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.28f), _theme.ButtonPrimary);

        var popup = root.gameObject.AddComponent<TipPopup>();
        var so = new SerializedObject(popup);
        so.FindProperty("_titleText").objectReferenceValue = titleText;
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(backgroundButton.onClick, popup.Close);
        UnityEventTools.AddVoidPersistentListener(okButton.onClick, popup.Close);
        root.gameObject.SetActive(false);
        return popup;
    }

    // 처음 ★★★ 일러스트 연출: 어두운 바탕(누르면 닫힘) + 도는 빛줄기 + 9:16 일러스트(화면 안에 다 보이게) + 이름·"★★★ 달성!"
    private static Lv3Showcase CreateLv3Showcase(Transform canvasRoot, MetaProgress meta)
    {
        RectTransform root = CreatePanel(canvasRoot, "Lv3Showcase", new Color(0f, 0f, 0f, 0.88f), true);
        var tapButton = root.gameObject.AddComponent<Button>();
        tapButton.transition = Selectable.Transition.None;

        // 화면 가운데 큰 정사각형 (돌아도 모서리가 안 보이게 화면보다 크게)
        RectTransform rays = CreatePanel(root, "Rays", _theme.Gold, false);
        rays.anchorMin = rays.anchorMax = new Vector2(0.5f, 0.5f);
        rays.sizeDelta = new Vector2(2600f, 2600f);

        RectTransform art = CreatePanel(root, "Art", Color.white, false);
        art.GetComponent<Image>().preserveAspect = true;

        var captionGo = new GameObject("Caption", typeof(RectTransform), typeof(CanvasGroup));
        captionGo.transform.SetParent(root, false);
        Stretch((RectTransform)captionGo.transform, new Vector2(0f, 0.03f), new Vector2(1f, 0.19f));
        var caption = captionGo.GetComponent<CanvasGroup>();
        caption.blocksRaycasts = false;
        Text nameText = CreateText(captionGo.transform, "Name", "", _theme.HeadingSize + 16, new Vector2(0.05f, 0.45f), new Vector2(0.95f, 1f));
        Text starText = CreateText(captionGo.transform, "Stars", "★★★ 달성!", _theme.HeadingSize, new Vector2(0.05f, 0f), new Vector2(0.95f, 0.45f));
        starText.color = _theme.Gold;
        foreach (Text text in new[] { nameText, starText })
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);
        }

        var showcase = root.gameObject.AddComponent<Lv3Showcase>();
        var so = new SerializedObject(showcase);
        so.FindProperty("_meta").objectReferenceValue = meta;
        so.FindProperty("_dim").objectReferenceValue = root.GetComponent<Image>();
        so.FindProperty("_rays").objectReferenceValue = rays.GetComponent<Image>();
        so.FindProperty("_art").objectReferenceValue = art.GetComponent<Image>();
        so.FindProperty("_caption").objectReferenceValue = caption;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        // 일러스트가 있는 크리처 목록 (그림을 불러오지 않고 도감 버튼 표시·수집 수에 씀)
        var available = new System.Collections.Generic.List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { IllustrationResourcesFolder }))
        {
            string file = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
            if (file.EndsWith("_Lv3"))
            {
                available.Add(file.Substring(0, file.Length - 4));
            }
        }
        SerializedProperty availableProperty = so.FindProperty("_available");
        availableProperty.arraySize = available.Count;
        for (int i = 0; i < available.Count; i++)
        {
            availableProperty.GetArrayElementAtIndex(i).stringValue = available[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(tapButton.onClick, showcase.OnTap);
        root.gameObject.SetActive(false);
        return showcase;
    }

    // 화면이 바뀔 때 잠깐 덮는 어두운 판 (누르는 것은 막지 않음, 처음엔 꺼짐)
    private static ScreenFader CreateFader(Transform canvasRoot)
    {
        RectTransform panel = CreatePanel(canvasRoot, "ScreenFader", _theme.Background, false);
        var fader = panel.gameObject.AddComponent<ScreenFader>();
        var so = new SerializedObject(fader);
        so.FindProperty("_image").objectReferenceValue = panel.GetComponent<Image>();
        so.ApplyModifiedPropertiesWithoutUndo();
        panel.gameObject.SetActive(false);
        return fader;
    }

    // 화면 전체를 덮는 어두운 바탕. 바탕(창 바깥)을 누르면 backgroundButton이 눌린다
    private static RectTransform CreateModalRoot(Transform canvas, string name, out Button backgroundButton)
    {
        RectTransform root = CreatePanel(canvas, name, _theme.Dim, true);
        backgroundButton = root.gameObject.AddComponent<Button>();
        backgroundButton.transition = Selectable.Transition.None;
        return root;
    }

    // 왼쪽 정렬 글 (여러 줄이면 줄바꿈)
    private static Text CreateLeftText(Transform parent, string name, int size, Vector2 anchorMin, Vector2 anchorMax)
    {
        Text text = CreateText(parent, name, "", size, anchorMin, anchorMax);
        text.alignment = TextAnchor.MiddleLeft;
        return text;
    }

    // 튜토리얼 화면 (캔버스 맨 위): 어두운 판 4장 + 구멍 막이 + 노란 테두리 + 말풍선(확인) + 건너뛰기
    private static void BuildTutorialUI(Transform canvas, TutorialController tutorial, MetaProgress meta,
        ShopScreen shop, RectTransform battleButton, CreatureInfoPopup infoPopup)
    {
        BuildTutorialOverlay(canvas, tutorial, meta, shop, battleButton);
        var so = new SerializedObject(tutorial);
        so.FindProperty("_infoPopup").objectReferenceValue = infoPopup;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildTutorialOverlay(Transform canvas, TutorialController tutorial, MetaProgress meta,
        ShopScreen shop, RectTransform battleButton)
    {
        var overlay = new GameObject("TutorialOverlay", typeof(RectTransform));
        overlay.transform.SetParent(canvas, false);
        var overlayRect = overlay.GetComponent<RectTransform>();
        Stretch(overlayRect, Vector2.zero, Vector2.one);

        Color dark = _theme.Dim;
        RectTransform blockTop = CreatePanel(overlay.transform, "BlockTop", dark, true);
        RectTransform blockBottom = CreatePanel(overlay.transform, "BlockBottom", dark, true);
        RectTransform blockLeft = CreatePanel(overlay.transform, "BlockLeft", dark, true);
        RectTransform blockRight = CreatePanel(overlay.transform, "BlockRight", dark, true);
        RectTransform holeBlocker = CreatePanel(overlay.transform, "HoleBlocker", Color.clear, true);

        // 테두리: 구멍 바깥쪽에 두께 8의 노란 선 4개
        var frame = new GameObject("Frame", typeof(RectTransform));
        frame.transform.SetParent(overlay.transform, false);
        var frameRect = frame.GetComponent<RectTransform>();
        Color frameColor = _theme.Accent;
        const float thickness = 8f;
        SetBorder(CreatePanel(frame.transform, "Top", frameColor, false), new Vector2(0f, 1f), Vector2.one,
            new Vector2(-thickness, 0f), new Vector2(thickness, thickness));
        SetBorder(CreatePanel(frame.transform, "Bottom", frameColor, false), Vector2.zero, new Vector2(1f, 0f),
            new Vector2(-thickness, -thickness), new Vector2(thickness, 0f));
        SetBorder(CreatePanel(frame.transform, "Left", frameColor, false), Vector2.zero, new Vector2(0f, 1f),
            new Vector2(-thickness, 0f), Vector2.zero);
        SetBorder(CreatePanel(frame.transform, "Right", frameColor, false), new Vector2(1f, 0f), Vector2.one,
            Vector2.zero, new Vector2(thickness, 0f));

        // 말풍선: 설명 + (설명 단계에서만) 확인 버튼
        RectTransform bubble = CreatePanel(overlay.transform, "Bubble", _theme.Panel, true);
        Round(bubble.GetComponent<Image>(), 28f);
        Text bubbleText = CreateText(bubble, "Text", "", _theme.BodySize, new Vector2(0.04f, 0.06f), new Vector2(0.74f, 0.94f));
        bubbleText.alignment = TextAnchor.MiddleLeft;
        Button confirmButton = CreateFreeButton(bubble, "확인", new Vector2(0.77f, 0.18f), new Vector2(0.97f, 0.82f),
            _theme.ButtonPrimary);

        // 오른쪽 위 건너뛰기 (항상 맨 위)
        Button skipButton = CreateFreeButton(overlay.transform, "건너뛰기", new Vector2(0.76f, 0.88f), new Vector2(0.98f, 0.945f),
            _theme.ButtonSecondary);
        skipButton.GetComponentInChildren<Text>().resizeTextMaxSize = _theme.BodySize;

        overlay.SetActive(false);

        var so = new SerializedObject(tutorial);
        so.FindProperty("_data").objectReferenceValue = GetOrCreateTutorialData();
        so.FindProperty("_meta").objectReferenceValue = meta;
        so.FindProperty("_shop").objectReferenceValue = shop;
        so.FindProperty("_battleButton").objectReferenceValue = battleButton;
        so.FindProperty("_overlay").objectReferenceValue = overlayRect;
        so.FindProperty("_blockTop").objectReferenceValue = blockTop;
        so.FindProperty("_blockBottom").objectReferenceValue = blockBottom;
        so.FindProperty("_blockLeft").objectReferenceValue = blockLeft;
        so.FindProperty("_blockRight").objectReferenceValue = blockRight;
        so.FindProperty("_holeBlocker").objectReferenceValue = holeBlocker;
        so.FindProperty("_frame").objectReferenceValue = frameRect;
        so.FindProperty("_bubble").objectReferenceValue = bubble;
        so.FindProperty("_bubbleText").objectReferenceValue = bubbleText;
        so.FindProperty("_confirmButton").objectReferenceValue = confirmButton.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(confirmButton.onClick, tutorial.Confirm);
        UnityEventTools.AddVoidPersistentListener(skipButton.onClick, tutorial.Skip);
    }

    // 색 판 (위치는 튜토리얼이 정한다). blocksInput: 뒤의 버튼이 눌리지 않게 막는지
    private static RectTransform CreatePanel(Transform parent, string name, Color color, bool blocksInput)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = blocksInput;
        var rect = go.GetComponent<RectTransform>();
        Stretch(rect, Vector2.zero, Vector2.one);
        return rect;
    }

    private static void SetBorder(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    // 튜토리얼 문구·정해 둔 상점·약한 적 팀. 없으면 기본값으로 만들고, 있으면 그대로 쓴다 (고친 문구 보호)
    // 정보 창으로 구매·판매하게 바뀐 뒤의 문구
    private const string NewBuyMessage = "상점에서 크리처를 사 볼까요?\n빛나는 칸을 눌러 정보를 보고, 구매 버튼을 누르세요.";
    private const string NewMergeMessage = "불똥이를 2마리 더 사서 3마리를 모아 보세요.\n칸을 누르고 구매 버튼을 누르면 돼요.";
    private const string NewSellMessage = "크리처를 누르면 정보 창이 떠요.\n판매 버튼을 누르고 한 번 더 확인하면 팔 수 있어요.";

    // 예전 기본 문구 → 새 문구 (직접 고친 문구는 그대로 둔다)
    private static readonly (string oldMessage, string newMessage)[] TutorialMessageUpdates =
    {
        ("상점에서 크리처를 사 볼까요?\n빛나는 칸을 눌러 불똥이를 사세요.", NewBuyMessage),
        ("불똥이를 2마리 더 사서\n3마리를 모아 보세요.", NewMergeMessage),
        ("대기석의 크리처를 누르면 팔아서\n골드를 돌려받을 수 있어요.", NewSellMessage),
    };

    private static TutorialData GetOrCreateTutorialData()
    {
        var data = AssetDatabase.LoadAssetAtPath<TutorialData>(TutorialDataPath);
        if (data != null)
        {
            UpdateTutorialMessages(data);
            return data;
        }

        Directory.CreateDirectory(TutorialFolder);
        AssetDatabase.Refresh();

        data = ScriptableObject.CreateInstance<TutorialData>();
        AssetDatabase.CreateAsset(data, TutorialDataPath);
        var so = new SerializedObject(data);

        (TutorialStep step, string message)[] steps =
        {
            (TutorialStep.BuyCreature, NewBuyMessage),
            (TutorialStep.PlaceInTeam, "산 크리처는 아래 대기석에 들어가요.\n손가락으로 끌어서 팀 칸(앞줄)에 놓으세요."),
            (TutorialStep.StartBattle, "팀이 준비됐어요!\n전투 시작을 누르세요."),
            (TutorialStep.BattleInfo, "전투는 자동으로 진행돼요.\n크리처마다 공격 속도에 맞춰 상대 앞줄부터 공격해요."),
            (TutorialStep.MergeInfo, "같은 크리처 3마리를 모으면\n1마리로 합쳐져 더 강해져요! (★ → ★★)"),
            (TutorialStep.Merge, NewMergeMessage),
            (TutorialStep.SellInfo, NewSellMessage),
            (TutorialStep.SynergyInfo, "같은 타입 크리처 2마리를 팀에 두면 여기에 시너지가 켜져요. (예: 불 2/4)\n이제 마음껏 플레이해 보세요!"),
        };
        SerializedProperty stepList = so.FindProperty("_steps");
        stepList.arraySize = steps.Length;
        for (int i = 0; i < steps.Length; i++)
        {
            SerializedProperty element = stepList.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("Step").enumValueIndex = (int)steps[i].step;
            element.FindPropertyRelative("Message").stringValue = steps[i].message;
        }

        so.FindProperty("_mergeCreature").objectReferenceValue = LoadCreature("Embit");
        SetArray(so.FindProperty("_round1Shop"), LoadCreatures("Embit", "Sproutee", "Plopper", "Pebblet", "Zappy"));
        SetArray(so.FindProperty("_round2Shop"), LoadCreatures("Embit", "Zappy", "Embit", "Plopper", "Sproutee"));
        SetArray(so.FindProperty("_enemyTeams"), new Object[]
        {
            CreateTutorialEnemyTeam("TutorialRound01", "연습 상대", "Sproutee"),
            CreateTutorialEnemyTeam("TutorialRound02", "연습 상대 2", "Pebblet"),
        });

        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return data;
    }

    // 이미 있는 튜토리얼 문구 중 예전 기본 문구 그대로인 것만 새 문구로 바꾼다
    private static void UpdateTutorialMessages(TutorialData data)
    {
        var so = new SerializedObject(data);
        SerializedProperty steps = so.FindProperty("_steps");
        int updated = 0;
        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty message = steps.GetArrayElementAtIndex(i).FindPropertyRelative("Message");
            foreach ((string oldMessage, string newMessage) in TutorialMessageUpdates)
            {
                if (message.stringValue == oldMessage)
                {
                    message.stringValue = newMessage;
                    updated++;
                }
            }
        }
        if (updated > 0)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[튜토리얼] 문구 {updated}개를 정보 창 흐름에 맞게 바꿨습니다");
        }
    }

    // 약한 적 팀: 앞줄 첫 칸에 Lv1 크리처 1마리
    private static EnemyTeamData CreateTutorialEnemyTeam(string id, string displayName, string creatureId)
    {
        string path = $"{TutorialFolder}/{id}.asset";
        var team = AssetDatabase.LoadAssetAtPath<EnemyTeamData>(path);
        if (team != null)
        {
            return team;
        }

        team = ScriptableObject.CreateInstance<EnemyTeamData>();
        AssetDatabase.CreateAsset(team, path);
        var so = new SerializedObject(team);
        so.FindProperty("_displayName").stringValue = displayName;
        SerializedProperty members = so.FindProperty("_members");
        members.arraySize = 5;
        for (int i = 0; i < members.arraySize; i++)
        {
            SerializedProperty member = members.GetArrayElementAtIndex(i);
            member.FindPropertyRelative("Creature").objectReferenceValue = i == 0 ? LoadCreature(creatureId) : null;
            member.FindPropertyRelative("Level").intValue = 1;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return team;
    }

    private static CreatureData LoadCreature(string id)
    {
        return AssetDatabase.LoadAssetAtPath<CreatureData>($"{CreatureFolder}/{id}.asset");
    }

    private static Object[] LoadCreatures(params string[] ids)
    {
        var creatures = new Object[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            creatures[i] = LoadCreature(ids[i]);
        }
        return creatures;
    }

    // 도감 화면: 제목 / 별 / 트레이너 3칸 / 크리처 35칸(위아래 스크롤, 줄 = 가격, 같은 가격은 타입 순) / 안내 문구 / 뒤로
    private static void BuildCodexUI(GameObject screen, ScreenFlow flow, MetaProgress meta, Lv3Showcase showcase)
    {
        Stretch((RectTransform)screen.transform.Find("Title"), new Vector2(0f, 0.925f), new Vector2(1f, 0.985f));
        Stretch((RectTransform)screen.transform.Find("ButtonArea"), new Vector2(0.25f, 0.015f), new Vector2(0.75f, 0.1f));

        // 별 + "크리처 / 조합식" 탭 (조합식 탭은 조합 해금 전에는 숨김)
        Text starsText = CreateText(screen.transform, "StarsText", "", _theme.LargeSize, new Vector2(0.02f, 0.88f), new Vector2(0.3f, 0.925f));
        starsText.color = _theme.Gold;
        Button creatureTabButton = CreateFreeButton(screen.transform, "크리처", new Vector2(0.32f, 0.88f), new Vector2(0.64f, 0.925f), _theme.ButtonInfo);
        Button recipeTabButton = CreateFreeButton(screen.transform, "조합식", new Vector2(0.66f, 0.88f), new Vector2(0.98f, 0.925f), _theme.ButtonInfo);
        creatureTabButton.GetComponentInChildren<Text>().resizeTextMaxSize = _theme.BodySize;
        recipeTabButton.GetComponentInChildren<Text>().resizeTextMaxSize = _theme.BodySize;

        // 탭 하나가 켜지면 다른 하나는 꺼진다. 앵커는 화면 전체 기준 그대로
        Transform creatureTab = CreateTabRoot(screen.transform, "CreatureTab");
        Transform recipeTab = CreateTabRoot(screen.transform, "RecipeTab");
        FusionRecipe[] recipes = GetSortedRecipes();
        Text recipeCountText = CreateText(recipeTab, "RecipeCount", "", _theme.SmallSize, new Vector2(0.03f, 0.85f), new Vector2(0.97f, 0.88f));
        recipeCountText.color = _theme.Gold;
        Transform recipeContent = CreateVerticalScroll(recipeTab, "RecipeScroll", new Vector2(0.03f, 0.135f), new Vector2(0.97f, 0.85f));
        FusionRecipeRowView[] recipeRows = CreateFusionRecipeRows(recipeContent, recipes.Length);
        Text hiddenCountText = CreateHiddenCountText(recipeContent);
        recipeTab.gameObject.SetActive(false);

        // 두 탭 공통: 오른쪽 위 "일러스트 n/전체"
        Text illustrationCountText = CreateText(screen.transform, "IllustrationCount", "", _theme.SmallSize, new Vector2(0.5f, 0.85f), new Vector2(0.97f, 0.88f));
        illustrationCountText.alignment = TextAnchor.MiddleRight;
        illustrationCountText.color = _theme.Gold;

        Text trainerLabel = CreateText(creatureTab, "TrainerLabel", "트레이너", _theme.SmallSize, new Vector2(0.03f, 0.85f), new Vector2(0.97f, 0.88f));
        trainerLabel.alignment = TextAnchor.MiddleLeft;
        Transform trainerRow = CreateRow(creatureTab, "TrainerRow", new Vector2(0.03f, 0.7f), new Vector2(0.97f, 0.85f));
        TrainerData[] trainers = GetOrCreateTrainers();
        var trainerEntries = new CodexEntryView[trainers.Length];
        for (int i = 0; i < trainers.Length; i++)
        {
            trainerEntries[i] = CreateCodexEntry(trainerRow, "Trainer" + i, false);
        }

        Text creatureLabel = CreateText(creatureTab, "CreatureLabel", "크리처", _theme.SmallSize, new Vector2(0.03f, 0.665f), new Vector2(0.97f, 0.695f));
        creatureLabel.alignment = TextAnchor.MiddleLeft;
        // 크리처 35종은 위아래로 스크롤. 가격이 바뀌면 새 줄, 한 줄에 최대 5칸 (1·2골드 2줄, 3·4·5골드 1줄)
        CreatureData[] creatures = GetSortedCreatures();
        Transform scrollContent = CreateVerticalScroll(creatureTab, "CreatureScroll",
            new Vector2(0.03f, 0.135f), new Vector2(0.97f, 0.66f));
        var creatureEntries = new CodexEntryView[creatures.Length];
        Transform row = null;
        int rowTier = 0;
        int column = 0;
        for (int i = 0; i < creatures.Length; i++)
        {
            if (row == null || column >= CodexColumns || creatures[i].Tier != rowTier)
            {
                row = CreateScrollRow(scrollContent, "Row" + creatures[i].Tier + "Gold");
                rowTier = creatures[i].Tier;
                column = 0;
            }
            creatureEntries[i] = CreateCodexEntry(row, "Creature" + i, true);
            creatureEntries[i].gameObject.AddComponent<LayoutElement>().preferredWidth = CodexCellWidth;
            column++;
        }

        Text messageText = CreateText(screen.transform, "MessageText", "", _theme.SmallSize, new Vector2(0f, 0.1f), new Vector2(1f, 0.13f));
        messageText.color = _theme.Positive;

        AddButton(screen, "뒤로", flow, GameScreen.Title);

        var codex = screen.AddComponent<CodexScreen>();
        var so = new SerializedObject(codex);
        so.FindProperty("_meta").objectReferenceValue = meta;
        SetArray(so.FindProperty("_trainers"), trainers);
        SetArray(so.FindProperty("_trainerEntries"), trainerEntries);
        SetArray(so.FindProperty("_creatures"), creatures);
        SetArray(so.FindProperty("_creatureEntries"), creatureEntries);
        so.FindProperty("_creatureTab").objectReferenceValue = creatureTab.gameObject;
        so.FindProperty("_recipeTab").objectReferenceValue = recipeTab.gameObject;
        so.FindProperty("_recipeTabButton").objectReferenceValue = recipeTabButton.gameObject;
        SetArray(so.FindProperty("_recipes"), recipes);
        SetArray(so.FindProperty("_recipeRows"), recipeRows);
        so.FindProperty("_recipeCountText").objectReferenceValue = recipeCountText;
        so.FindProperty("_hiddenCountText").objectReferenceValue = hiddenCountText;
        so.FindProperty("_showcase").objectReferenceValue = showcase;
        so.FindProperty("_illustrationCountText").objectReferenceValue = illustrationCountText;
        so.FindProperty("_starsText").objectReferenceValue = starsText;
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddIntPersistentListener(creatureTabButton.onClick, codex.ShowTab, 0);
        UnityEventTools.AddIntPersistentListener(recipeTabButton.onClick, codex.ShowTab, 1);
        for (int i = 0; i < trainerEntries.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(trainerEntries[i].GetComponentInChildren<Button>(true).onClick, codex.UnlockTrainer, i);
        }
        for (int i = 0; i < creatureEntries.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(creatureEntries[i].GetComponentInChildren<Button>(true).onClick, codex.UnlockCreature, i);
            UnityEventTools.AddIntPersistentListener(IllustrationButtonOf(creatureEntries[i]).onClick, codex.ViewCreatureIllustration, i);
        }
        for (int i = 0; i < recipeRows.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(IllustrationButtonOf(recipeRows[i]).onClick, codex.ViewRecipeIllustration, i);
        }
    }

    private static Button IllustrationButtonOf(Component parent) =>
        parent.GetComponentInChildren<IllustrationButton>(true).GetComponent<Button>();

    // 도감 "3성 일러스트" 버튼 (왼쪽에 자물쇠, 잠겼으면 "3성 달성 시 해금")
    private static IllustrationButton CreateIllustrationButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        Button button = CreateFreeButton(parent, "3성 일러스트", anchorMin, anchorMax, _theme.ButtonInfo);
        Text label = button.GetComponentInChildren<Text>();
        label.resizeTextMaxSize = _theme.TinySize;
        label.fontSize = _theme.TinySize;
        Stretch(label.rectTransform, new Vector2(0.2f, 0f), new Vector2(0.98f, 1f));

        var lockGo = new GameObject("Lock", typeof(RectTransform), typeof(Image));
        lockGo.transform.SetParent(button.transform, false);
        Stretch(lockGo.GetComponent<RectTransform>(), new Vector2(0.03f, 0.15f), new Vector2(0.2f, 0.85f));
        var lockImage = lockGo.GetComponent<Image>();
        lockImage.sprite = GetOrCreateLockSprite();
        lockImage.preserveAspect = true;
        lockImage.raycastTarget = false;

        var view = button.gameObject.AddComponent<IllustrationButton>();
        var so = new SerializedObject(view);
        so.FindProperty("_button").objectReferenceValue = button;
        so.FindProperty("_label").objectReferenceValue = label;
        so.FindProperty("_lockIcon").objectReferenceValue = lockGo;
        so.ApplyModifiedPropertiesWithoutUndo();
        button.gameObject.SetActive(false);
        return view;
    }

    // 일러스트를 해금한 크리처 그림 위 작은 ★
    private static GameObject CreateIllustrationMark(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        Text mark = CreateText(parent, "IllustrationMark", "★", _theme.SmallSize, anchorMin, anchorMax);
        mark.color = _theme.Gold;
        mark.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        mark.gameObject.SetActive(false);
        return mark.gameObject;
    }

    // 화면 전체 크기의 빈 묶음 (탭 하나를 통째로 켜고 끄는 용도)
    private static Transform CreateTabRoot(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        return go.transform;
    }

    // 조합식 목록 맨 아래 "히든 n/5" 줄
    private static Text CreateHiddenCountText(Transform content)
    {
        Text text = CreateText(content, "HiddenCount", "", _theme.BodySize, Vector2.zero, Vector2.one);
        text.color = _theme.Accent;
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = 120f;
        return text;
    }

    // 조합식 전부, 등급순(하급→중급→교차→상급→히든), 같은 등급은 이름순
    private static FusionRecipe[] GetSortedRecipes()
    {
        string[] guids = AssetDatabase.FindAssets("t:FusionRecipe");
        var recipes = new FusionRecipe[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            recipes[i] = AssetDatabase.LoadAssetAtPath<FusionRecipe>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }
        recipes = System.Array.FindAll(recipes, r => r != null);
        System.Array.Sort(recipes, (a, b) => a.Hidden != b.Hidden ? a.Hidden.CompareTo(b.Hidden)
            : a.Grade != b.Grade ? a.Grade.CompareTo(b.Grade) : string.CompareOrdinal(a.name, b.name));
        return recipes;
    }

    // 조합식 줄 count개 (도감 탭과 상점 창이 같이 씀). 줄 하나 약 1000 x 270
    private static FusionRecipeRowView[] CreateFusionRecipeRows(Transform content, int count)
    {
        var rows = new FusionRecipeRowView[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Recipe" + i, typeof(RectTransform), typeof(Image), typeof(FusionRecipeRowView),
                typeof(LayoutElement), typeof(Outline));
            go.transform.SetParent(content, false);
            go.GetComponent<Image>().color = _theme.Slot;
            go.GetComponent<LayoutElement>().preferredHeight = 270f;
            var frame = go.GetComponent<Outline>();
            frame.effectDistance = new Vector2(5f, -5f);
            frame.enabled = false;

            Text gradeText = CreateText(go.transform, "Grade", "", _theme.TinySize, new Vector2(0.01f, 0.84f), new Vector2(0.2f, 0.98f));
            gradeText.color = _theme.Accent;
            Image iconA = CreateRecipeIcon(go.transform, "IconA", 0.03f, 0.2f);
            Text nameA = CreateText(go.transform, "NameA", "", _theme.TinySize, new Vector2(0.01f, 0.04f), new Vector2(0.22f, 0.24f));
            CreateText(go.transform, "Plus", "+", _theme.HeadingSize, new Vector2(0.21f, 0.4f), new Vector2(0.26f, 0.7f));
            Image iconB = CreateRecipeIcon(go.transform, "IconB", 0.27f, 0.44f);
            Text nameB = CreateText(go.transform, "NameB", "", _theme.TinySize, new Vector2(0.25f, 0.04f), new Vector2(0.46f, 0.24f));
            CreateText(go.transform, "Arrow", "→", _theme.HeadingSize, new Vector2(0.45f, 0.4f), new Vector2(0.5f, 0.7f));
            Image iconResult = CreateRecipeIcon(go.transform, "IconResult", 0.51f, 0.68f);
            Text nameResult = CreateText(go.transform, "NameResult", "", _theme.TinySize, new Vector2(0.49f, 0.04f), new Vector2(0.7f, 0.24f));
            nameResult.color = _theme.Gold;
            Text infoText = CreateLeftText(go.transform, "Info", _theme.TinySize, new Vector2(0.71f, 0.27f), new Vector2(0.99f, 0.95f));
            infoText.color = _theme.TextSub;
            // 도감에서만 보임 (상점 조합식 창에선 숨김)
            IllustrationButton illustration = CreateIllustrationButton(go.transform, new Vector2(0.72f, 0.04f), new Vector2(0.98f, 0.25f));
            GameObject illustrationMark = CreateIllustrationMark(go.transform, new Vector2(0.62f, 0.78f), new Vector2(0.7f, 0.98f));

            rows[i] = go.GetComponent<FusionRecipeRowView>();
            var so = new SerializedObject(rows[i]);
            so.FindProperty("_gradeText").objectReferenceValue = gradeText;
            so.FindProperty("_iconA").objectReferenceValue = iconA;
            so.FindProperty("_nameA").objectReferenceValue = nameA;
            so.FindProperty("_iconB").objectReferenceValue = iconB;
            so.FindProperty("_nameB").objectReferenceValue = nameB;
            so.FindProperty("_iconResult").objectReferenceValue = iconResult;
            so.FindProperty("_nameResult").objectReferenceValue = nameResult;
            so.FindProperty("_infoText").objectReferenceValue = infoText;
            so.FindProperty("_frame").objectReferenceValue = frame;
            so.FindProperty("_illustration").objectReferenceValue = illustration;
            so.FindProperty("_illustrationMark").objectReferenceValue = illustrationMark;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return rows;
    }

    private static Image CreateRecipeIcon(Transform parent, string name, float xMin, float xMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), new Vector2(xMin, 0.26f), new Vector2(xMax, 0.84f));
        var image = go.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    // 상점 조합식 창: 어두운 바탕(누르면 닫힘) + 가운데 큰 창(제목, 발견 n/17, 닫기, 스크롤 목록)
    private static FusionBookPanel CreateFusionBookPanel(Transform canvas)
    {
        RectTransform root = CreateModalRoot(canvas, "FusionBookPanel", out Button backgroundButton);
        RectTransform window = CreatePanel(root, "Window", _theme.Panel, true);
        Round(window.GetComponent<Image>(), 36f);
        Stretch(window, new Vector2(0.03f, 0.1f), new Vector2(0.97f, 0.9f));
        CreateText(window, "Title", "조합식", _theme.HeadingSize, new Vector2(0.04f, 0.92f), new Vector2(0.36f, 0.99f));
        Text countText = CreateText(window, "Count", "", _theme.BodySize, new Vector2(0.36f, 0.92f), new Vector2(0.72f, 0.99f));
        countText.color = _theme.Gold;
        Button closeButton = CreateFreeButton(window, "닫기", new Vector2(0.75f, 0.925f), new Vector2(0.97f, 0.985f),
            _theme.ButtonSecondary);
        closeButton.GetComponentInChildren<Text>().resizeTextMaxSize = _theme.BodySize;
        Transform content = CreateVerticalScroll(window, "RecipeScroll", new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.91f));
        FusionRecipe[] recipes = GetSortedRecipes();
        FusionRecipeRowView[] rows = CreateFusionRecipeRows(content, recipes.Length);
        Text hiddenCountText = CreateHiddenCountText(content);

        var panel = root.gameObject.AddComponent<FusionBookPanel>();
        var so = new SerializedObject(panel);
        SetArray(so.FindProperty("_recipes"), recipes);
        SetArray(so.FindProperty("_rows"), rows);
        so.FindProperty("_countText").objectReferenceValue = countText;
        so.FindProperty("_hiddenCountText").objectReferenceValue = hiddenCountText;
        so.FindProperty("_scroll").objectReferenceValue = content.parent.GetComponent<ScrollRect>();
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(backgroundButton.onClick, panel.Close);
        UnityEventTools.AddVoidPersistentListener(closeButton.onClick, panel.Close);
        root.gameObject.SetActive(false);
        return panel;
    }

    // 위아래 스크롤 목록. 돌려주는 Transform에 줄을 넣는다 (줄 높이만큼 자동으로 길어짐)
    private static Transform CreateVerticalScroll(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var scroll = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        scroll.transform.SetParent(parent, false);
        var scrollRect = scroll.GetComponent<RectTransform>();
        Stretch(scrollRect, anchorMin, anchorMax);
        // 빈 곳을 끌어도 스크롤되도록 거의 투명한 바탕
        scroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(scroll.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 14;
        layout.padding = new RectOffset(0, 0, 8, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollView = scroll.GetComponent<ScrollRect>();
        scrollView.content = contentRect;
        scrollView.viewport = scrollRect;
        scrollView.horizontal = false;
        scrollView.vertical = true;
        scrollView.movementType = ScrollRect.MovementType.Clamped;
        scrollView.scrollSensitivity = 40;
        return content.transform;
    }

    // 스크롤 목록의 한 줄 (칸은 가운데 정렬, 칸 너비는 칸의 LayoutElement)
    private static Transform CreateScrollRow(Transform content, string name)
    {
        var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(content, false);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        row.GetComponent<LayoutElement>().preferredHeight = CodexRowHeight;
        return row.transform;
    }

    // 도감 한 칸. withIcon: 크리처 칸(그림 있음) / 트레이너 칸(이름 + 설명)
    private static CodexEntryView CreateCodexEntry(Transform parent, string name, bool withIcon)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CodexEntryView));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = _theme.Slot;
        Round(go.GetComponent<Image>());

        Image icon = null;
        Text nameText;
        Text infoText;
        if (withIcon)
        {
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            Stretch(iconGo.GetComponent<RectTransform>(), new Vector2(0.15f, 0.47f), new Vector2(0.85f, 0.93f));
            icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            nameText = CreateText(go.transform, "Name", "", _theme.TinySize, new Vector2(0.02f, 0.34f), new Vector2(0.98f, 0.46f));
            infoText = CreateText(go.transform, "Info", "", _theme.TinySize, new Vector2(0.02f, 0.25f), new Vector2(0.98f, 0.34f));
        }
        else
        {
            nameText = CreateText(go.transform, "Name", "", _theme.BodySize, new Vector2(0.04f, 0.7f), new Vector2(0.96f, 0.95f));
            infoText = CreateText(go.transform, "Info", "", _theme.TinySize, new Vector2(0.04f, 0.3f), new Vector2(0.96f, 0.7f));
        }
        infoText.color = _theme.TextSub;

        // 잠금 덮개는 해금 버튼보다 먼저 만들어 버튼은 어두워지지 않게 한다
        // 크리처 칸은 그림 위에, 트레이너 칸은 오른쪽 위에 자물쇠
        Vector2 lockMin = withIcon ? new Vector2(0.3f, 0.55f) : new Vector2(0.8f, 0.6f);
        Vector2 lockMax = withIcon ? new Vector2(0.7f, 0.88f) : new Vector2(0.96f, 0.95f);
        GameObject lockOverlay = CreateLockOverlay(go.transform, lockMin, lockMax, out _, Vector2.zero, Vector2.zero, _theme.TinySize);

        Button unlockButton = CreateFreeButton(go.transform, "해금", new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.23f),
            _theme.ButtonPrimary);
        Text unlockLabel = unlockButton.GetComponentInChildren<Text>();
        unlockLabel.resizeTextMaxSize = _theme.TinySize;
        unlockLabel.fontSize = _theme.TinySize;
        // 버튼 높이가 약 60px라 위쪽(이름·설명 글자 쪽)으로 눌리는 범위를 넓힌다
        AddHitArea(unlockButton, 0f, 6f, 0f, 60f);

        // 크리처 칸: 해금 버튼 자리에 3성 일러스트 버튼 (크리처가 열린 뒤), 그림 오른쪽 위에 ★
        IllustrationButton illustration = null;
        GameObject illustrationMark = null;
        if (withIcon)
        {
            illustration = CreateIllustrationButton(go.transform, new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.23f));
            AddHitArea(illustration, 0f, 6f, 0f, 60f);
            illustrationMark = CreateIllustrationMark(go.transform, new Vector2(0.76f, 0.8f), new Vector2(0.98f, 0.98f));
        }

        var view = go.GetComponent<CodexEntryView>();
        var so = new SerializedObject(view);
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_infoText").objectReferenceValue = infoText;
        so.FindProperty("_lockOverlay").objectReferenceValue = lockOverlay;
        so.FindProperty("_unlockButton").objectReferenceValue = unlockButton;
        so.FindProperty("_unlockLabel").objectReferenceValue = unlockLabel;
        so.FindProperty("_illustration").objectReferenceValue = illustration;
        so.FindProperty("_illustrationMark").objectReferenceValue = illustrationMark;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    // 잠금 덮개: 부모 전체를 어둡게 + 자물쇠 그림 + 글자 (처음엔 숨김, 눌림은 막지 않는다)
    private static GameObject CreateLockOverlay(Transform parent, Vector2 lockMin, Vector2 lockMax,
        out Text label, Vector2 labelMin, Vector2 labelMax, int fontSize)
    {
        var overlay = new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(parent, false);
        Stretch(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var dim = overlay.GetComponent<Image>();
        dim.color = _theme.Dim;
        Round(dim);
        dim.raycastTarget = false;

        var lockGo = new GameObject("Lock", typeof(RectTransform), typeof(Image));
        lockGo.transform.SetParent(overlay.transform, false);
        Stretch(lockGo.GetComponent<RectTransform>(), lockMin, lockMax);
        var lockImage = lockGo.GetComponent<Image>();
        lockImage.sprite = GetOrCreateLockSprite();
        lockImage.preserveAspect = true;
        lockImage.raycastTarget = false;

        label = CreateText(overlay.transform, "LockText", "", fontSize, labelMin, labelMax);
        label.color = _theme.Gold;

        overlay.SetActive(false);
        return overlay;
    }

    // 자물쇠 그림 (임시): 흰색 자물쇠 64x64 PNG를 코드로 그린다
    private static Sprite GetOrCreateLockSprite()
    {
        if (!File.Exists(LockSpritePath))
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(32f, 34f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 몸통 (아래 사각형, 가운데 열쇠 구멍)
                    bool body = x >= 12 && x < 52 && y >= 4 && y < 34;
                    bool keyhole = x >= 29 && x < 35 && y >= 12 && y < 24;
                    // 고리 (위쪽 반원 띠)
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    bool shackle = y >= 34 && distance >= 10f && distance <= 16f;
                    bool filled = (body && !keyhole) || shackle;
                    texture.SetPixel(x, y, filled ? Color.white : Color.clear);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(LockSpritePath));
            File.WriteAllBytes(LockSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(LockSpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(LockSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(LockSpritePath);
    }

    // 도감 순서: 가격(등급) 순, 같은 가격은 타입 순. 조합 전용 크리처는 일반 목록에서 뺀다
    private static CreatureData[] GetSortedCreatures()
    {
        string[] guids = AssetDatabase.FindAssets("t:CreatureData", new[] { CreatureFolder });
        var creatures = new CreatureData[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            creatures[i] = AssetDatabase.LoadAssetAtPath<CreatureData>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }
        creatures = System.Array.FindAll(creatures, c => c != null && !c.FusionOnly);
        System.Array.Sort(creatures, (a, b) => a.Tier != b.Tier ? a.Tier.CompareTo(b.Tier) : a.Type.CompareTo(b.Type));
        return creatures;
    }

    // 메타 성장 설정. 없으면 기본값(GAME_DESIGN.md 9-2)으로 만들고, 있으면 그대로 쓴다 (조정한 값 보호)
    private static MetaConfig GetOrCreateMetaConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<MetaConfig>(MetaConfigPath);
        if (config != null)
        {
            return config;
        }

        config = ScriptableObject.CreateInstance<MetaConfig>();
        AssetDatabase.CreateAsset(config, MetaConfigPath);
        var so = new SerializedObject(config);

        SerializedProperty trainers = so.FindProperty("_lockedTrainers");
        trainers.arraySize = 1;
        trainers.GetArrayElementAtIndex(0).FindPropertyRelative("Trainer").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TrainerData>($"{TrainerFolder}/Merchant.asset");
        trainers.GetArrayElementAtIndex(0).FindPropertyRelative("Cost").intValue = 15;

        // 5골드 5종을 각 별 10개로 잠근다 (1~5골드 개편, CREATURE_TIERS.md)
        string[] lockedCreatures = { "SunPhoenix", "AbyssDragon", "ElderTreant", "VoltQilin", "MountainTortoise" };
        SerializedProperty creatures = so.FindProperty("_lockedCreatures");
        creatures.arraySize = lockedCreatures.Length;
        for (int i = 0; i < lockedCreatures.Length; i++)
        {
            SerializedProperty element = creatures.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("Creature").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<CreatureData>($"{CreatureFolder}/{lockedCreatures[i]}.asset");
            element.FindPropertyRelative("Cost").intValue = 10;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return config;
    }

    // 트레이너 선택 화면: 제목 / 카드 3장(이름·설명, 눌러서 선택) / 안내 문구 / 선택 완료
    private static void BuildTrainerSelectUI(GameObject screen, DifficultySelectScreen difficultySelect, MetaProgress meta)
    {
        TrainerData[] trainers = GetOrCreateTrainers();
        TrainerCardView[] cards = CreateSelectCards(screen, "TrainerCard", trainers.Length, out Button[] cardButtons, out Text messageText);

        Button confirmButton = CreateButton(screen, "선택 완료");

        var select = screen.AddComponent<TrainerSelectScreen>();
        var so = new SerializedObject(select);
        so.FindProperty("_difficultySelect").objectReferenceValue = difficultySelect;
        so.FindProperty("_meta").objectReferenceValue = meta;
        SetArray(so.FindProperty("_trainers"), trainers);
        SetArray(so.FindProperty("_cards"), cards);
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < cardButtons.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(cardButtons[i].onClick, select.Select, i);
        }
        UnityEventTools.AddVoidPersistentListener(confirmButton.onClick, select.Confirm);
    }

    // 난이도 선택 화면 (트레이너 선택 다음): 카드 3장 + 뒤로(트레이너 선택) + 시작
    private static DifficultySelectScreen BuildDifficultySelectUI(GameObject screen, RunManager run, ScreenFlow flow,
        MetaProgress meta, DifficultyData[] difficulties)
    {
        TrainerCardView[] cards = CreateSelectCards(screen, "DifficultyCard", difficulties.Length, out Button[] cardButtons, out Text messageText);

        AddButton(screen, "뒤로", flow, GameScreen.TrainerSelect);
        Button confirmButton = CreateButton(screen, "시작");

        var select = screen.AddComponent<DifficultySelectScreen>();
        var so = new SerializedObject(select);
        so.FindProperty("_run").objectReferenceValue = run;
        so.FindProperty("_flow").objectReferenceValue = flow;
        so.FindProperty("_meta").objectReferenceValue = meta;
        SetArray(so.FindProperty("_difficulties"), difficulties);
        SetArray(so.FindProperty("_cards"), cards);
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < cardButtons.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(cardButtons[i].onClick, select.Select, i);
        }
        UnityEventTools.AddVoidPersistentListener(confirmButton.onClick, select.Confirm);
        return select;
    }

    // 선택 화면 카드 count장 (이름, 설명, 잠금 덮개) + 아래 안내 글
    private static TrainerCardView[] CreateSelectCards(GameObject screen, string namePrefix, int count,
        out Button[] cardButtons, out Text messageText)
    {
        Stretch((RectTransform)screen.transform.Find("Title"), new Vector2(0f, 0.8f), new Vector2(1f, 0.92f));

        var cardArea = new GameObject("Cards", typeof(RectTransform), typeof(VerticalLayoutGroup));
        cardArea.transform.SetParent(screen.transform, false);
        Stretch(cardArea.GetComponent<RectTransform>(), new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.78f));
        var layout = cardArea.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 36;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        var cards = new TrainerCardView[count];
        cardButtons = new Button[count];
        for (int i = 0; i < count; i++)
        {
            var card = new GameObject(namePrefix + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TrainerCardView));
            card.transform.SetParent(cardArea.transform, false);
            var background = card.GetComponent<Image>();
            Round(background, 32f);

            Text nameText = CreateText(card.transform, "Name", "", _theme.HeadingSize, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.92f));
            Text descriptionText = CreateText(card.transform, "Description", "", _theme.SmallSize, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.55f));
            descriptionText.color = _theme.TextSub;

            // 잠겼을 때: 카드 전체를 어둡게 + 오른쪽에 자물쇠와 열리는 조건
            GameObject lockOverlay = CreateLockOverlay(card.transform, new Vector2(0.8f, 0.5f), new Vector2(0.96f, 0.92f),
                out Text lockText, new Vector2(0.62f, 0.06f), new Vector2(0.98f, 0.48f), 32);

            cards[i] = card.GetComponent<TrainerCardView>();
            var cardSo = new SerializedObject(cards[i]);
            cardSo.FindProperty("_background").objectReferenceValue = background;
            cardSo.FindProperty("_nameText").objectReferenceValue = nameText;
            cardSo.FindProperty("_descriptionText").objectReferenceValue = descriptionText;
            cardSo.FindProperty("_lockOverlay").objectReferenceValue = lockOverlay;
            cardSo.FindProperty("_lockText").objectReferenceValue = lockText;
            cardSo.FindProperty("_theme").objectReferenceValue = _theme;
            cardSo.ApplyModifiedPropertiesWithoutUndo();
            background.color = _theme.Slot;
            cardButtons[i] = card.GetComponent<Button>();
        }

        messageText = CreateText(screen.transform, "MessageText", "", _theme.BodySize, new Vector2(0f, 0.2f), new Vector2(1f, 0.26f));
        messageText.color = _theme.Negative;
        return cards;
    }

    // 난이도 에셋 3개 (보통, 어려움, 불가능 순서. 수치는 에셋에서 조정)
    private static DifficultyData[] LoadDifficulties()
    {
        var difficulties = new DifficultyData[DifficultyIds.Length];
        for (int i = 0; i < DifficultyIds.Length; i++)
        {
            difficulties[i] = AssetDatabase.LoadAssetAtPath<DifficultyData>($"{DifficultyFolder}/{DifficultyIds[i]}.asset");
            if (difficulties[i] == null)
            {
                Debug.LogError($"난이도 에셋이 없습니다: {DifficultyFolder}/{DifficultyIds[i]}.asset");
            }
        }
        return difficulties;
    }

    // 트레이너 3명 에셋. 없는 것만 기본값(GAME_DESIGN.md 5번)으로 만든다 (조정한 값 보호)
    private static TrainerData[] GetOrCreateTrainers()
    {
        Directory.CreateDirectory(TrainerFolder);
        AssetDatabase.Refresh();

        return new[]
        {
            GetOrCreateTrainer("FlameTamer", "불꽃 조련사", "불 타입 공격력 +20%",
                TrainerAbility.TypeAttackBonus, CreatureType.Fire, 0.2f),
            GetOrCreateTrainer("ForestKeeper", "숲지기", "풀 타입이 전투 중 체력이 처음 50% 아래로 떨어지면\n최대 체력의 30%를 한 번 회복",
                TrainerAbility.TypeLowHealthHeal, CreatureType.Grass, 0.3f),
            GetOrCreateTrainer("Merchant", "상인", "라운드마다 골드 +1",
                TrainerAbility.RoundGoldBonus, CreatureType.Fire, 1f),
        };
    }

    private static TrainerData GetOrCreateTrainer(string id, string name, string description,
        TrainerAbility ability, CreatureType targetType, float value)
    {
        string path = $"{TrainerFolder}/{id}.asset";
        var trainer = AssetDatabase.LoadAssetAtPath<TrainerData>(path);
        if (trainer != null)
        {
            return trainer;
        }

        trainer = ScriptableObject.CreateInstance<TrainerData>();
        AssetDatabase.CreateAsset(trainer, path);
        var so = new SerializedObject(trainer);
        so.FindProperty("_displayName").stringValue = name;
        so.FindProperty("_description").stringValue = description;
        so.FindProperty("_ability").enumValueIndex = (int)ability;
        so.FindProperty("_targetType").enumValueIndex = (int)targetType;
        so.FindProperty("_value").floatValue = value;
        so.FindProperty("_healthThreshold").floatValue = 0.5f;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return trainer;
    }

    // 상단 정보바 (상점·전투 화면에서만 보인다): [깃발 12/30층 보스] [하트 3개] [동전 골드 ?] [별 이번 판 별]
    // 아이콘은 32x32 픽셀 그림 (HudIconBuilder). goldText: 누르면 골드 규칙 안내 (상점이 연결)
    private static HudView CreateHud(Transform canvas, DifficultyData[] difficulties, out Text goldText)
    {
        // 아이콘 그림은 먼저 만든다 (만드는 중에 에셋을 가져오면 아래 연결이 끊길 수 있음)
        Sprite flagSprite = HudIconBuilder.Flag;
        Sprite heartSprite = HudIconBuilder.Heart;
        Sprite emptyHeartSprite = HudIconBuilder.EmptyHeart;
        Sprite coinSprite = HudIconBuilder.Coin;
        Sprite starSprite = HudIconBuilder.Star;

        var hud = new GameObject("Hud", typeof(RectTransform), typeof(Image), typeof(HudView));
        hud.transform.SetParent(canvas, false);
        Stretch(hud.GetComponent<RectTransform>(), new Vector2(0f, 0.945f), Vector2.one);
        var background = hud.GetComponent<Image>();
        background.color = _theme.Bar;
        background.raycastTarget = false;

        CreateHudIcon(hud.transform, "RoundIcon", flagSprite, 0.015f, 0.085f);
        Text roundText = CreateText(hud.transform, "RoundText", "", _theme.BodySize, new Vector2(0.09f, 0f), new Vector2(0.225f, 1f));
        // "100/100층"도 한 줄에 들어가게 줄여서 맞춘다
        roundText.resizeTextForBestFit = true;
        roundText.resizeTextMinSize = 20;
        roundText.resizeTextMaxSize = _theme.BodySize;
        roundText.alignment = TextAnchor.MiddleLeft;
        // 보스 층 표시: 빨간 둥근 판 + "보스"
        var bossTag = new GameObject("BossTag", typeof(RectTransform), typeof(Image));
        bossTag.transform.SetParent(hud.transform, false);
        Stretch(bossTag.GetComponent<RectTransform>(), new Vector2(0.228f, 0.2f), new Vector2(0.322f, 0.8f));
        var bossImage = bossTag.GetComponent<Image>();
        bossImage.color = _theme.ButtonDanger;
        bossImage.raycastTarget = false;
        Round(bossImage, 20f);
        Text bossText = CreateText(bossTag.transform, "Text", "보스", _theme.SmallSize, Vector2.zero, Vector2.one);
        bossText.color = _theme.ButtonText;
        bossTag.SetActive(false);

        // 목숨 최대치만큼 하트 (난이도 중 가장 많은 목숨)
        int maxLives = 1;
        foreach (DifficultyData difficulty in difficulties)
        {
            maxLives = Mathf.Max(maxLives, difficulty != null ? difficulty.Lives : 3);
        }
        // 하트 영역: 보스 표시와 골드 아이콘 사이 고정 (하트 3개 너비). HudView.SetLives가 이번 판 최대 목숨 수로 나눠 놓는다
        var heartArea = new GameObject("Hearts", typeof(RectTransform));
        heartArea.transform.SetParent(hud.transform, false);
        Stretch(heartArea.GetComponent<RectTransform>(), new Vector2(0.33f, 0f), new Vector2(0.534f, 1f));
        var hearts = new Image[maxLives];
        for (int i = 0; i < maxLives; i++)
        {
            hearts[i] = CreateHudIcon(heartArea.transform, "Heart" + i, heartSprite,
                (float)i / maxLives, (float)(i + 1) / maxLives);
        }

        CreateHudIcon(hud.transform, "GoldIcon", coinSprite, 0.54f, 0.61f);
        goldText = CreateText(hud.transform, "GoldText", "", _theme.HeadingSize, new Vector2(0.615f, 0f), new Vector2(0.725f, 1f));
        goldText.color = _theme.Gold;
        // 누르면 규칙 안내가 나온다는 작은 표시
        Text goldHelp = CreateText(hud.transform, "GoldHelpMark", "?", _theme.SmallSize, new Vector2(0.725f, 0.15f), new Vector2(0.76f, 0.85f));
        goldHelp.color = _theme.TextSub;

        CreateHudIcon(hud.transform, "StarIcon", starSprite, 0.77f, 0.84f);
        Text starsText = CreateText(hud.transform, "StarsText", "", _theme.BodySize, new Vector2(0.845f, 0f), new Vector2(0.9f, 1f));
        starsText.alignment = TextAnchor.MiddleLeft;
        starsText.color = _theme.Accent;

        var view = hud.GetComponent<HudView>();
        var so = new SerializedObject(view);
        so.FindProperty("_roundText").objectReferenceValue = roundText;
        so.FindProperty("_bossTag").objectReferenceValue = bossTag;
        SetArray(so.FindProperty("_hearts"), hearts);
        so.FindProperty("_heartSprite").objectReferenceValue = heartSprite;
        so.FindProperty("_emptyHeartSprite").objectReferenceValue = emptyHeartSprite;
        so.FindProperty("_goldText").objectReferenceValue = goldText;
        so.FindProperty("_starsText").objectReferenceValue = starsText;
        so.ApplyModifiedPropertiesWithoutUndo();

        view.enabled = false;
        hud.SetActive(false);
        return view;
    }

    // 정보바 아이콘: 가로 xMin~xMax 칸 안에 정사각형으로 (픽셀 그림이라 비율 유지)
    private static Image CreateHudIcon(Transform parent, string name, Sprite sprite, float xMin, float xMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), new Vector2(xMin, 0.14f), new Vector2(xMax, 0.86f));
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
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
        go.GetComponent<Image>().color = _theme.ButtonPrimary;
        Round(go.GetComponent<Image>(), 32f);

        var text = new GameObject("Text", typeof(RectTransform), typeof(Text));
        text.transform.SetParent(go.transform, false);
        Stretch(text.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        SetupText(text.GetComponent<Text>(), label, _theme.HeadingSize);
        text.GetComponent<Text>().color = _theme.ButtonText;

        Button button = go.GetComponent<Button>();
        AddPressFeedback(button);
        return button;
    }

    // 전투 화면 (세로 1920 기준, 위에서부터)
    // (상단 표시) / 적 팀 이름 / 적 뒷줄 3칸 / 적 앞줄 2칸 / 승패 문구 / 내 팀 앞줄 2칸 / 내 팀 뒷줄 3칸 / 배속 버튼
    // 두 팀의 앞줄이 가운데에서 마주 본다 (내 팀은 배치 화면과 같은 모양)
    private static BattleScreen BuildBattleUI(GameObject screen, RunManager run)
    {
        // 제목 자리는 상단 표시가 차지한다
        screen.transform.Find("Title").gameObject.SetActive(false);
        Stretch((RectTransform)screen.transform.Find("ButtonArea"), new Vector2(0.25f, 0.015f), new Vector2(0.75f, 0.105f));

        // 하늘 구름: 배경 그림 위쪽 1/4(180x320 중 위 80줄)에 겹쳐서 옆으로 흐른다. 어두운 판보다 아래
        var background = (RectTransform)screen.transform.Find("Background");
        var clouds = new GameObject("Clouds", typeof(RectTransform), typeof(RawImage), typeof(CloudScroller));
        clouds.transform.SetParent(background, false);
        clouds.transform.SetSiblingIndex(0);
        Stretch(clouds.GetComponent<RectTransform>(), new Vector2(0f, 0.75f), Vector2.one);
        var cloudImage = clouds.GetComponent<RawImage>();
        cloudImage.texture = _backgrounds.Clouds != null ? _backgrounds.Clouds.texture : null;
        cloudImage.raycastTarget = false;
        clouds.SetActive(cloudImage.texture != null);

        Color playerColor = _theme.PlayerUnit;
        Color enemyColor = _theme.EnemyUnit;

        Text enemyLabel = CreateText(screen.transform, "EnemyLabel", "적 팀", _theme.BodySize, new Vector2(0f, 0.912f), new Vector2(1f, 0.945f));
        // 적 팀 이름 아래 작게 보스 능력 이름 (보스 능력이 없으면 숨김)
        Text bossAbilityLabel = CreateText(screen.transform, "BossAbilityLabel", "", _theme.SmallSize, new Vector2(0f, 0.89f), new Vector2(1f, 0.914f));
        bossAbilityLabel.color = _theme.ButtonDanger;
        bossAbilityLabel.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        bossAbilityLabel.gameObject.SetActive(false);
        Transform enemyBack = CreateRow(screen.transform, "EnemyBackRow", new Vector2(0.03f, 0.745f), new Vector2(0.97f, 0.89f));
        Transform enemyFront = CreateRow(screen.transform, "EnemyFrontRow", new Vector2(0.2f, 0.595f), new Vector2(0.8f, 0.74f));

        Text bannerText = CreateText(screen.transform, "BannerText", "", _theme.HugeSize, new Vector2(0f, 0.475f), new Vector2(1f, 0.585f));
        bannerText.color = _theme.Accent;
        bannerText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        // 승리·패배·보스 등장 글이 튀어나오게 (움직일 때만 켜짐)
        var bannerPop = bannerText.gameObject.AddComponent<PopText>();
        var popSo = new SerializedObject(bannerPop);
        popSo.FindProperty("_text").objectReferenceValue = bannerText;
        popSo.ApplyModifiedPropertiesWithoutUndo();
        bannerPop.enabled = false;

        // 보스 전투 시작 때 "보스 능력: 이름 — 설명" (보스 등장 글 아래, 능력이 둘이면 두 줄)
        Text bossAbilityText = CreateText(screen.transform, "BossAbilityText", "", _theme.BodySize, new Vector2(0.02f, 0.455f), new Vector2(0.98f, 0.51f));
        bossAbilityText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        var bossAbilityPop = bossAbilityText.gameObject.AddComponent<PopText>();
        var abilityPopSo = new SerializedObject(bossAbilityPop);
        abilityPopSo.FindProperty("_text").objectReferenceValue = bossAbilityText;
        abilityPopSo.ApplyModifiedPropertiesWithoutUndo();
        bossAbilityPop.enabled = false;

        Transform playerFront = CreateRow(screen.transform, "PlayerFrontRow", new Vector2(0.2f, 0.32f), new Vector2(0.8f, 0.465f));
        Transform playerBack = CreateRow(screen.transform, "PlayerBackRow", new Vector2(0.03f, 0.17f), new Vector2(0.97f, 0.315f));
        CreateText(screen.transform, "PlayerLabel", "내 팀", _theme.BodySize, new Vector2(0f, 0.125f), new Vector2(1f, 0.165f));

        var playerViews = new CreatureUnitView[5];
        var enemyViews = new CreatureUnitView[5];
        for (int i = 0; i < 5; i++)
        {
            bool front = i < 2;
            playerViews[i] = CreateUnitSlot(front ? playerFront : playerBack, "PlayerSlot" + i, playerColor, true);
            enemyViews[i] = CreateUnitSlot(front ? enemyFront : enemyBack, "EnemySlot" + i, enemyColor, false);
        }

        // 스킬 글·데미지 숫자 풀: 크리처 칸보다 위에 그려지도록 칸 다음에 만든다 (전투 중 새로 만들지 않고 돌려 씀)
        FloatingTextPool floatingTexts = CreateFloatingTextPool(screen.transform, FloatingTextCount);

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
        so.FindProperty("_theme").objectReferenceValue = _theme;
        so.FindProperty("_backgrounds").objectReferenceValue = _backgrounds;
        so.FindProperty("_background").objectReferenceValue = background.GetComponent<Image>();
        SetArray(so.FindProperty("_playerViews"), playerViews);
        SetArray(so.FindProperty("_enemyViews"), enemyViews);
        so.FindProperty("_enemyLabel").objectReferenceValue = enemyLabel;
        so.FindProperty("_bannerText").objectReferenceValue = bannerText;
        so.FindProperty("_bannerPop").objectReferenceValue = bannerPop;
        so.FindProperty("_bossAbilityPop").objectReferenceValue = bossAbilityPop;
        so.FindProperty("_bossAbilityLabel").objectReferenceValue = bossAbilityLabel;
        so.FindProperty("_speedLabel").objectReferenceValue = speedLabel;
        so.FindProperty("_floatingTexts").objectReferenceValue = floatingTexts;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(speedButton.onClick, battle.ToggleSpeed);
        return battle;
    }

    // 떠오르는 글 count개를 미리 만들어 둔 풀 (처음엔 모두 숨김)
    private static FloatingTextPool CreateFloatingTextPool(Transform parent, int count)
    {
        var pool = new GameObject("FloatingTexts", typeof(RectTransform), typeof(FloatingTextPool));
        pool.transform.SetParent(parent, false);
        Stretch(pool.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        var texts = new Text[count];
        for (int i = 0; i < count; i++)
        {
            // 위치는 띄울 때 정하므로 가운데 기준 고정 크기
            Text text = CreateText(pool.transform, "Text" + i, "", _theme.BodySize, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            text.rectTransform.sizeDelta = new Vector2(320f, 70f);
            text.fontStyle = FontStyle.Bold;
            text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            text.gameObject.SetActive(false);
            texts[i] = text;
        }

        var component = pool.GetComponent<FloatingTextPool>();
        var so = new SerializedObject(component);
        SetArray(so.FindProperty("_texts"), texts);
        so.ApplyModifiedPropertiesWithoutUndo();
        return component;
    }

    // 전투 칸: 바탕(빈 칸 표시) + 크리처(체력바, 그림, 이름, 맞으면 깜빡이는 흰 판). 크리처가 죽으면 크리처만 사라진다
    // facesUp: 공격할 때 튀어나가는 쪽 (내 팀 위, 적 팀 아래)
    private static CreatureUnitView CreateUnitSlot(Transform row, string name, Color color, bool facesUp)
    {
        var slot = new GameObject(name, typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(row, false);
        slot.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.35f);
        Round(slot.GetComponent<Image>());

        var unit = new GameObject("Unit", typeof(RectTransform), typeof(Image), typeof(CreatureUnitView));
        unit.transform.SetParent(slot.transform, false);
        Stretch(unit.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        unit.GetComponent<Image>().color = color;
        Round(unit.GetComponent<Image>());

        var healthBack = new GameObject("HealthBar", typeof(RectTransform), typeof(Image));
        healthBack.transform.SetParent(unit.transform, false);
        Stretch(healthBack.GetComponent<RectTransform>(), new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.95f));
        healthBack.GetComponent<Image>().color = _theme.HealthBarBack;

        // 체력 칸 뒤 흰 막대: 체력이 줄면 잠깐 뒤에 천천히 따라 줄어든다
        var healthTrail = new GameObject("Trail", typeof(RectTransform), typeof(Image));
        healthTrail.transform.SetParent(healthBack.transform, false);
        Stretch(healthTrail.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        healthTrail.GetComponent<Image>().color = _theme.HealthTrail;

        var healthFill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        healthFill.transform.SetParent(healthBack.transform, false);
        Stretch(healthFill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        healthFill.GetComponent<Image>().color = _theme.HealthBar;

        // 보호막: 체력 칸 뒤에 이어지는 흰 칸 (보호막이 있을 때만 보임)
        var shieldFill = new GameObject("Shield", typeof(RectTransform), typeof(Image));
        shieldFill.transform.SetParent(healthBack.transform, false);
        Stretch(shieldFill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        shieldFill.GetComponent<Image>().color = _theme.HealthTrail;

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(unit.transform, false);
        Stretch(iconGo.GetComponent<RectTransform>(), new Vector2(0.15f, 0.26f), new Vector2(0.85f, 0.73f));
        var icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;

        // 체력바 바로 아래 레벨 별
        Text levelText = CreateText(unit.transform, "Level", "", _theme.SmallSize, new Vector2(0f, 0.74f), new Vector2(1f, 0.85f));
        levelText.color = _theme.Accent;

        Text nameText = CreateText(unit.transform, "Name", "", _theme.TinySize, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.24f));

        foreach (Image image in unit.GetComponentsInChildren<Image>())
        {
            image.raycastTarget = false;
        }
        slot.GetComponent<Image>().raycastTarget = false;

        // 오른쪽 위 작은 아이템 아이콘 (처음엔 숨김)
        ItemIconView itemIcon = CreateItemIcon(unit.transform, new Vector2(0.74f, 0.6f), new Vector2(0.97f, 0.84f), _theme.TinySize);
        Image typeBar = CreateTypeBar(unit.transform);

        // 맞았을 때 깜빡이는 흰 판: 칸 전체를 덮도록 마지막에 만든다 (평소엔 투명)
        var hitFlash = new GameObject("HitFlash", typeof(RectTransform), typeof(Image));
        hitFlash.transform.SetParent(unit.transform, false);
        Stretch(hitFlash.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var hitFlashImage = hitFlash.GetComponent<Image>();
        hitFlashImage.color = Color.clear;
        hitFlashImage.raycastTarget = false;
        Round(hitFlashImage);
        hitFlash.transform.SetSiblingIndex(hitFlash.transform.parent.childCount - 1);

        // 보스 철벽 중 반투명 흰 테두리 (칸 바탕 그림에 Outline, 평소엔 꺼짐)
        var guardFrame = unit.AddComponent<Outline>();
        guardFrame.effectColor = new Color(1f, 1f, 1f, 0.6f);
        guardFrame.effectDistance = new Vector2(6f, -6f);
        guardFrame.enabled = false;

        var view = unit.GetComponent<CreatureUnitView>();
        var so = new SerializedObject(view);
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_levelText").objectReferenceValue = levelText;
        so.FindProperty("_healthFill").objectReferenceValue = healthFill.GetComponent<RectTransform>();
        so.FindProperty("_shieldFill").objectReferenceValue = shieldFill.GetComponent<RectTransform>();
        so.FindProperty("_healthTrail").objectReferenceValue = healthTrail.GetComponent<RectTransform>();
        so.FindProperty("_hitFlash").objectReferenceValue = hitFlashImage;
        so.FindProperty("_facesUp").boolValue = facesUp;
        so.FindProperty("_typeBar").objectReferenceValue = typeBar;
        so.FindProperty("_theme").objectReferenceValue = _theme;
        so.FindProperty("_itemIcon").objectReferenceValue = itemIcon;
        so.FindProperty("_guardFrame").objectReferenceValue = guardFrame;
        so.ApplyModifiedPropertiesWithoutUndo();
        unit.SetActive(false);
        return view;
    }

    // 상점·배치 화면 (세로 1920 기준, 위에서부터)
    // (상단 표시: 라운드·목숨·골드) / 트레이너·새로고침 / 상점 크리처 5칸 + 아이템 1칸 / 메시지 / 시너지
    // / 팀 앞줄 2칸 / 팀 뒷줄 3칸 / 대기석 6칸 / 아이템 보관함 3칸 + 전투 시작
    private static ShopScreen BuildShopUI(GameObject screen, RunManager run, MetaProgress meta, HudView hud, Text goldText,
        CreatureInfoPopup infoPopup, ItemInfoPopup itemPopup, SynergyPanel synergyPanel, FusionPanel fusionPanel, FusionBookPanel fusionBookPanel, TipPopup tipPopup, out Text trainerText)
    {
        // 새로고침 버튼 왼쪽: 지금 트레이너 이름 (작게)
        trainerText = CreateText(screen.transform, "TrainerText", "", _theme.TinySize,
            new Vector2(0.02f, 0.875f), new Vector2(0.29f, 0.935f));
        trainerText.alignment = TextAnchor.MiddleLeft;
        trainerText.color = _theme.TextSub;

        // 제목 자리는 상단 표시가 차지한다
        screen.transform.Find("Title").gameObject.SetActive(false);
        // 전투 시작 버튼은 오른쪽 아래 (왼쪽 아래는 아이템 보관함)
        Stretch((RectTransform)screen.transform.Find("ButtonArea"), new Vector2(0.56f, 0.015f), new Vector2(0.97f, 0.115f));

        var refresh = new GameObject("RefreshButton", typeof(RectTransform), typeof(Image), typeof(Button));
        refresh.transform.SetParent(screen.transform, false);
        Stretch(refresh.GetComponent<RectTransform>(), new Vector2(0.3f, 0.87f), new Vector2(0.7f, 0.935f));
        refresh.GetComponent<Image>().color = _theme.ButtonInfo;
        Round(refresh.GetComponent<Image>());
        AddPressFeedback(refresh.GetComponent<Button>());
        Text refreshLabel = CreateText(refresh.transform, "Text", "새로고침", _theme.LargeSize, Vector2.zero, Vector2.one);
        refreshLabel.color = _theme.ButtonText;

        // 상점 칸: 크리처 5칸 + 아이템 1칸, 각 약 155 x 290
        Transform shopRow = CreateRow(screen.transform, "ShopSlots", new Vector2(0.03f, 0.715f), new Vector2(0.97f, 0.865f));
        var shopSlots = new CreatureSlotView[5];
        for (int i = 0; i < shopSlots.Length; i++)
        {
            shopSlots[i] = CreateSlot(shopRow, "ShopSlot" + i, "");
        }
        ItemSlotView shopItemSlot = CreateItemSlot(shopRow, "ShopItemSlot", "");
        shopItemSlot.GetComponent<Image>().color = _theme.ItemSlot;

        Text messageText = CreateText(screen.transform, "MessageText", "", _theme.BodySize,
            new Vector2(0f, 0.69f), new Vector2(1f, 0.713f));
        messageText.color = _theme.Negative;

        // 팀 칸 바로 위: 지금 켜진 시너지 (눌러도 시너지 표가 열림) + 오른쪽 "시너지" 버튼
        Text synergyText = CreateText(screen.transform, "SynergyText", "", _theme.SmallSize,
            new Vector2(0.02f, 0.665f), new Vector2(0.8f, 0.692f));
        synergyText.color = _theme.Positive;
        synergyText.raycastTarget = true;
        Button synergyTextButton = synergyText.gameObject.AddComponent<Button>();
        synergyTextButton.targetGraphic = synergyText;
        // 시너지 줄은 높이가 약 50px라 위쪽(메시지 줄, 눌리지 않음)으로 눌리는 범위를 넓힌다 (아래는 팀 칸)
        AddHitArea(synergyText, 0f, 0f, 0f, 40f);
        Button synergyButton = CreateFreeButton(screen.transform, "시너지",
            new Vector2(0.82f, 0.664f), new Vector2(0.98f, 0.694f), _theme.ButtonGood);
        synergyButton.GetComponentInChildren<Text>().resizeTextMaxSize = _theme.SmallSize;
        // 시너지 버튼 아래·위는 비어 있어(팀 앞줄은 0.2~0.8) 눌리는 범위를 약 140px로
        AddHitArea(synergyButton, 0f, 46f, 0f, 38f);
        // 시너지 버튼 왼쪽: "조합 N" (보통 클리어 전에는 꺼 둠, 켜지면 시너지 줄이 버튼 왼쪽까지 줄어듦)
        Button fusionButton = CreateFreeButton(screen.transform, "조합",
            new Vector2(0.65f, 0.664f), new Vector2(0.8f, 0.694f), _theme.ButtonPrimary);
        Text fusionLabel = fusionButton.GetComponentInChildren<Text>();
        fusionLabel.resizeTextMaxSize = _theme.SmallSize;
        AddHitArea(fusionButton, 0f, 0f, 0f, 38f);
        fusionButton.gameObject.SetActive(false);
        // 조합 버튼 왼쪽: 조합식 목록 (이모지는 글꼴에 없어 글자로)
        Button fusionBookButton = CreateFreeButton(screen.transform, "조합식",
            new Vector2(0.53f, 0.664f), new Vector2(0.64f, 0.694f), _theme.ButtonInfo);
        fusionBookButton.GetComponentInChildren<Text>().resizeTextMaxSize = _theme.SmallSize;
        AddHitArea(fusionBookButton, 0f, 0f, 0f, 38f);
        fusionBookButton.gameObject.SetActive(false);

        // 팀 칸: 약 300 x 290 (가장 크게). 위가 앞줄
        Color teamColor = _theme.TeamSlot;
        Transform frontRow = CreateRow(screen.transform, "TeamFrontRow", new Vector2(0.2f, 0.515f), new Vector2(0.8f, 0.665f));
        Transform backRow = CreateRow(screen.transform, "TeamBackRow", new Vector2(0.03f, 0.355f), new Vector2(0.97f, 0.505f));
        var teamSlots = new CreatureSlotView[5];
        for (int i = 0; i < teamSlots.Length; i++)
        {
            bool front = i < 2;
            teamSlots[i] = CreateSlot(front ? frontRow : backRow, "TeamSlot" + i, front ? "앞줄" : "뒷줄");
            teamSlots[i].GetComponent<Image>().color = teamColor;
        }

        CreateText(screen.transform, "BenchLabel", "대기석 (끌어서 배치 / 눌러서 정보·판매)", _theme.BodySize,
            new Vector2(0f, 0.31f), new Vector2(1f, 0.345f));

        // 대기석 칸: 약 160 x 330
        Transform benchRow = CreateRow(screen.transform, "BenchSlots", new Vector2(0.03f, 0.13f), new Vector2(0.97f, 0.305f));
        var benchSlots = new CreatureSlotView[6];
        for (int i = 0; i < benchSlots.Length; i++)
        {
            benchSlots[i] = CreateSlot(benchRow, "BenchSlot" + i, "");
        }

        // 아이템 보관함 3칸 (왼쪽 아래): 크리처에게 끌어서 장착
        Transform itemRow = CreateRow(screen.transform, "ItemSlots", new Vector2(0.03f, 0.015f), new Vector2(0.53f, 0.115f));
        var itemSlots = new ItemSlotView[3];
        for (int i = 0; i < itemSlots.Length; i++)
        {
            itemSlots[i] = CreateItemSlot(itemRow, "ItemSlot" + i, "보관함");
        }

        // 끄는 그림은 가장 위에 그려지도록 마지막에 만든다 (아이템 그림이 없으면 색 네모 + 글자)
        var dragIconGo = new GameObject("DragIcon", typeof(RectTransform), typeof(Image));
        dragIconGo.transform.SetParent(screen.transform, false);
        var dragRect = dragIconGo.GetComponent<RectTransform>();
        dragRect.anchorMin = dragRect.anchorMax = new Vector2(0.5f, 0.5f);
        dragRect.sizeDelta = new Vector2(200, 200);
        var dragIcon = dragIconGo.GetComponent<Image>();
        dragIcon.raycastTarget = false;
        dragIcon.preserveAspect = true;
        Text dragLabel = CreateText(dragIconGo.transform, "Label", "", _theme.HugeSize, Vector2.zero, Vector2.one);
        dragLabel.color = Color.black;
        dragIconGo.SetActive(false);

        // 상단 골드 바로 아래: 라운드 시작 때 잠깐 보이는 "+10 +이자 2" (상단 표시 아래로 삐져나오게)
        // 길면 ("+10 +이자 3 (최대 50)") 두 줄까지 (새로고침 버튼 오른쪽 빈 곳)
        Text goldIncomeText = CreateText(goldText.transform.parent, "GoldIncomeText", "", _theme.SmallSize,
            new Vector2(0.71f, -1.25f), new Vector2(0.995f, 0f));
        goldIncomeText.alignment = TextAnchor.MiddleRight;
        goldIncomeText.color = _theme.Positive;
        goldIncomeText.gameObject.SetActive(false);

        var shop = screen.AddComponent<ShopScreen>();

        // 상단 골드 글자를 누르면 골드 규칙 안내
        goldText.raycastTarget = true;
        Button goldButton = goldText.gameObject.AddComponent<Button>();
        goldButton.targetGraphic = goldText;
        UnityEventTools.AddVoidPersistentListener(goldButton.onClick, shop.ShowGoldHelp);
        // 동전 그림과 "?"까지, 상단 표시 높이(약 105px)보다 아래로 조금 더 눌리게
        AddHitArea(goldText, 90f, 20f, 40f, 0f);

        var so = new SerializedObject(shop);
        so.FindProperty("_config").objectReferenceValue = GetOrCreateShopConfig();
        so.FindProperty("_synergy").objectReferenceValue = GetOrCreateSynergyConfig();
        so.FindProperty("_synergyText").objectReferenceValue = synergyText;
        so.FindProperty("_infoPopup").objectReferenceValue = infoPopup;
        so.FindProperty("_itemPopup").objectReferenceValue = itemPopup;
        so.FindProperty("_synergyPanel").objectReferenceValue = synergyPanel;
        so.FindProperty("_run").objectReferenceValue = run;
        so.FindProperty("_meta").objectReferenceValue = meta;
        so.FindProperty("_hud").objectReferenceValue = hud;
        so.FindProperty("_goldIncomeText").objectReferenceValue = goldIncomeText;
        so.FindProperty("_messageText").objectReferenceValue = messageText;
        so.FindProperty("_refreshLabel").objectReferenceValue = refreshLabel;
        SetArray(so.FindProperty("_shopSlots"), shopSlots);
        SetArray(so.FindProperty("_benchSlots"), benchSlots);
        SetArray(so.FindProperty("_teamSlots"), teamSlots);
        so.FindProperty("_shopItemSlot").objectReferenceValue = shopItemSlot;
        SetArray(so.FindProperty("_itemSlots"), itemSlots);
        so.FindProperty("_dragIcon").objectReferenceValue = dragIcon;
        so.FindProperty("_dragLabel").objectReferenceValue = dragLabel;
        so.FindProperty("_fusionButton").objectReferenceValue = fusionButton;
        so.FindProperty("_fusionLabel").objectReferenceValue = fusionLabel;
        so.FindProperty("_fusionPanel").objectReferenceValue = fusionPanel;
        so.FindProperty("_fusionBookButton").objectReferenceValue = fusionBookButton;
        so.FindProperty("_fusionBookPanel").objectReferenceValue = fusionBookPanel;
        so.FindProperty("_tip").objectReferenceValue = tipPopup;
        so.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < benchSlots.Length; i++)
        {
            AddDragHandler(benchSlots[i], shop, SlotArea.Bench, i);
        }
        for (int i = 0; i < teamSlots.Length; i++)
        {
            AddDragHandler(teamSlots[i], shop, SlotArea.Team, i);
        }
        for (int i = 0; i < itemSlots.Length; i++)
        {
            AddDragHandler(itemSlots[i], shop, SlotArea.Items, i);
        }

        UnityEventTools.AddVoidPersistentListener(refresh.GetComponent<Button>().onClick, shop.Refresh);
        UnityEventTools.AddVoidPersistentListener(shopItemSlot.GetComponent<Button>().onClick, shop.BuyItem);
        UnityEventTools.AddVoidPersistentListener(synergyTextButton.onClick, shop.OpenSynergyPanel);
        UnityEventTools.AddVoidPersistentListener(synergyButton.onClick, shop.OpenSynergyPanel);
        UnityEventTools.AddVoidPersistentListener(fusionButton.onClick, shop.OpenFusionPanel);
        UnityEventTools.AddVoidPersistentListener(fusionBookButton.onClick, shop.OpenFusionBook);
        // 크리처 칸을 누르면 정보 창 (구매·판매는 창의 버튼으로)
        for (int i = 0; i < shopSlots.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(shopSlots[i].GetComponent<Button>().onClick, shop.ShowShopInfo, i);
        }
        for (int i = 0; i < benchSlots.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(benchSlots[i].GetComponent<Button>().onClick, shop.ShowBenchInfo, i);
        }
        for (int i = 0; i < teamSlots.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(teamSlots[i].GetComponent<Button>().onClick, shop.ShowTeamInfo, i);
        }
        // 보관함 아이템을 누르면 아이템 창 (판매)
        for (int i = 0; i < itemSlots.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(itemSlots[i].GetComponent<Button>().onClick, shop.ShowItemInfo, i);
        }

        return shop;
    }

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

    // 상점 설정 에셋. 이미 있으면 그대로 쓴다 (조정한 값 보호).
    // 아이템 목록이 비어 있을 때만 아이템 6종을 채운다
    private static ShopConfig GetOrCreateShopConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<ShopConfig>(ShopConfigPath);
        if (config == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:CreatureData", new[] { CreatureFolder });
            if (guids.Length == 0)
            {
                Debug.LogError("크리처 데이터가 없습니다. 먼저 MonsterCamp > Create Initial Creature Data 를 실행하세요.");
            }

            config = ScriptableObject.CreateInstance<ShopConfig>();
            AssetDatabase.CreateAsset(config, ShopConfigPath);

            var creatures = new Object[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                creatures[i] = AssetDatabase.LoadAssetAtPath<CreatureData>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }
            var newSo = new SerializedObject(config);
            SetArray(newSo.FindProperty("_creaturePool"), creatures);
            newSo.ApplyModifiedPropertiesWithoutUndo();
        }

        var so = new SerializedObject(config);
        SerializedProperty itemPool = so.FindProperty("_itemPool");
        if (itemPool.arraySize == 0)
        {
            SetArray(itemPool, GetOrCreateItems());
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssets();
        return config;
    }

    // 아이템 6종 에셋. 없는 것만 기본값(GAME_DESIGN.md 9-1)으로 만든다 (조정한 값 보호)
    private static Object[] GetOrCreateItems()
    {
        Directory.CreateDirectory(ItemFolder);
        AssetDatabase.Refresh();

        return new Object[]
        {
            GetOrCreateItem("SharpClaw", "날카로운 발톱", "발", new Color(0.95f, 0.45f, 0.35f),
                "공격력 +20%", ItemEffect.AttackBonus, 0.2f),
            GetOrCreateItem("HardShell", "단단한 껍질", "껍", new Color(0.65f, 0.5f, 0.35f),
                "체력 +25%", ItemEffect.HealthBonus, 0.25f),
            GetOrCreateItem("ThunderFeather", "번개 깃털", "깃", new Color(0.55f, 0.8f, 1f),
                "공격 속도 +20%", ItemEffect.SpeedBonus, 0.2f),
            GetOrCreateItem("HealingBerry", "회복 열매", "열", new Color(0.45f, 0.85f, 0.45f),
                "전투 중 1초마다 최대 체력 3% 회복", ItemEffect.Regen, 0.03f),
            GetOrCreateItem("ThornArmor", "가시 갑옷", "가", new Color(0.7f, 0.5f, 0.9f),
                "받은 피해의 20%를 공격자에게 되돌림", ItemEffect.Thorns, 0.2f),
            GetOrCreateItem("LuckyCoin", "행운 동전", "동", new Color(1f, 0.8f, 0.2f),
                "전투에서 이기면 골드 +1 (다음 라운드)", ItemEffect.WinGold, 1f),
        };
    }

    private static ItemData GetOrCreateItem(string id, string name, string iconLabel, Color iconColor,
        string description, ItemEffect effect, float value)
    {
        string path = $"{ItemFolder}/{id}.asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item != null)
        {
            return item;
        }

        item = ScriptableObject.CreateInstance<ItemData>();
        AssetDatabase.CreateAsset(item, path);
        var so = new SerializedObject(item);
        so.FindProperty("_displayName").stringValue = name;
        so.FindProperty("_iconLabel").stringValue = iconLabel;
        so.FindProperty("_iconColor").colorValue = iconColor;
        so.FindProperty("_description").stringValue = description;
        so.FindProperty("_effect").enumValueIndex = (int)effect;
        so.FindProperty("_value").floatValue = value;
        so.FindProperty("_price").intValue = 3;
        so.FindProperty("_sellPrice").intValue = 2;
        so.FindProperty("_icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Project/Art/Items/{id}.png");
        so.ApplyModifiedPropertiesWithoutUndo();
        return item;
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

    // 끌기 처리 (CanvasGroup은 RequireComponent로 같이 붙는다)
    private static void AddDragHandler(Component slot, ShopScreen shop, SlotArea area, int index)
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
        go.GetComponent<Image>().color = _theme.Slot;
        Round(go.GetComponent<Image>());

        Text levelText = CreateText(go.transform, "Level", "", _theme.SmallSize, new Vector2(0f, 0.83f), new Vector2(1f, 0.98f));
        levelText.color = _theme.Accent;

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(go.transform, false);
        Stretch(iconGo.GetComponent<RectTransform>(), new Vector2(0.15f, 0.4f), new Vector2(0.85f, 0.83f));
        var icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        Text nameText = CreateText(go.transform, "Name", "", _theme.TinySize, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.4f));
        Text priceText = CreateText(go.transform, "Price", "", _theme.TinySize, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.2f));
        priceText.color = _theme.Gold;

        // 오른쪽 위 작은 아이템 아이콘 (장착했을 때만 보임)
        ItemIconView itemIcon = CreateItemIcon(go.transform, new Vector2(0.7f, 0.64f), new Vector2(0.97f, 0.86f), _theme.TinySize);

        Image typeBar = CreateTypeBar(go.transform);

        // 가장 위에 그려지도록 마지막에 만든다
        var flashGo = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        flashGo.transform.SetParent(go.transform, false);
        Stretch(flashGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var flash = flashGo.GetComponent<Image>();
        flash.color = Color.clear;
        flash.raycastTarget = false;
        Round(flash);

        var view = go.GetComponent<CreatureSlotView>();
        var so = new SerializedObject(view);
        so.FindProperty("_button").objectReferenceValue = go.GetComponent<Button>();
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_priceText").objectReferenceValue = priceText;
        so.FindProperty("_levelText").objectReferenceValue = levelText;
        so.FindProperty("_itemIcon").objectReferenceValue = itemIcon;
        so.FindProperty("_flash").objectReferenceValue = flash;
        so.FindProperty("_typeBar").objectReferenceValue = typeBar;
        so.FindProperty("_theme").objectReferenceValue = _theme;
        so.FindProperty("_emptyLabel").stringValue = emptyLabel;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    // 아이템 칸: 배경 버튼 + 아이콘(그림, 없으면 색 네모 + 글자) + 이름 + 가격. 비어 있으면 emptyLabel 표시
    private static ItemSlotView CreateItemSlot(Transform parent, string name, string emptyLabel)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(ItemSlotView));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = _theme.Slot;
        Round(go.GetComponent<Image>());

        ItemIconView icon = CreateItemIcon(go.transform, new Vector2(0.22f, 0.42f), new Vector2(0.78f, 0.92f), _theme.HeadingSize);
        Text nameText = CreateText(go.transform, "Name", "", _theme.TinySize, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.42f));
        Text priceText = CreateText(go.transform, "Price", "", _theme.TinySize, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.2f));
        priceText.color = _theme.Gold;

        var view = go.GetComponent<ItemSlotView>();
        var so = new SerializedObject(view);
        so.FindProperty("_button").objectReferenceValue = go.GetComponent<Button>();
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_priceText").objectReferenceValue = priceText;
        so.FindProperty("_emptyLabel").stringValue = emptyLabel;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    // 칸 아래 가는 막대 (크리처 타입 색, 색은 실행 중 칸이 정한다)
    private static Image CreateTypeBar(Transform parent)
    {
        var go = new GameObject("TypeBar", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), new Vector2(0.2f, 0.012f), new Vector2(0.8f, 0.032f));
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        Round(image, 4f);
        image.enabled = false;
        return image;
    }

    // 아이템 아이콘: 그림 (없으면 색 네모 + 한 글자). 처음엔 숨김
    private static ItemIconView CreateItemIcon(Transform parent, Vector2 anchorMin, Vector2 anchorMax, int fontSize)
    {
        var go = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image), typeof(ItemIconView));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        var background = go.GetComponent<Image>();
        background.raycastTarget = false;
        Round(background, 12f);

        Text label = CreateText(go.transform, "Label", "", fontSize, Vector2.zero, Vector2.one);
        label.color = Color.black;

        var pictureGo = new GameObject("Picture", typeof(RectTransform), typeof(Image));
        pictureGo.transform.SetParent(go.transform, false);
        Stretch(pictureGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var picture = pictureGo.GetComponent<Image>();
        picture.raycastTarget = false;
        picture.preserveAspect = true;
        picture.enabled = false;

        var view = go.GetComponent<ItemIconView>();
        var so = new SerializedObject(view);
        so.FindProperty("_background").objectReferenceValue = background;
        so.FindProperty("_label").objectReferenceValue = label;
        so.FindProperty("_picture").objectReferenceValue = picture;
        so.ApplyModifiedPropertiesWithoutUndo();
        go.SetActive(false);
        return view;
    }

    private static Text CreateText(Transform parent, string name, string value, int size, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        var text = go.GetComponent<Text>();
        SetupText(text, value, size);
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
        text.color = _theme.Text;
        text.raycastTarget = false;
        // 칸이 좁아도 글자가 잘리거나 사라지지 않게 (한글 글꼴은 줄 높이가 커서 칸에 안 맞으면 줄이 통째로 안 보인다)
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 16;
        text.resizeTextMaxSize = size;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
