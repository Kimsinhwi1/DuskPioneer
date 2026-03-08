using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using TMPro;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Phase 1 셋업: 시간 시스템, 도구 시스템 등을 Farm 씬에 추가.
/// 주의: 기존 Phase 0 오브젝트(PlayerController, 카메라)의 기존 로직은 건드리지 않음.
/// </summary>
public static class Phase1Setup
{
    private const string SO_DIR = "Assets/ScriptableObjects/Time";
    private const string SO_TOOL_DIR = "Assets/ScriptableObjects/Tools";
    private const string FONT_DIR = "Assets/Fonts";
    private const string KOREAN_FONT_PATH = "Assets/Fonts/MalgunGothic.ttf";
    private const string KOREAN_TMP_FONT_PATH = "Assets/Fonts/MalgunGothic SDF.asset";

    private const string NINJA_BASE = "Assets/Art/AssetPacks/NinjaAdventure/Ninja Adventure - Asset Pack";
    private const string PRINCESS_PATH = NINJA_BASE + "/Actor/Characters/Princess";
    private const string ITEMS_PATH = NINJA_BASE + "/Items";
    private const string TILESET_PATH = NINJA_BASE + "/Backgrounds/Tilesets";
    private const string TILE_DIR = "Assets/Tilemaps/Tiles";
    private const string SO_FARM_DIR = "Assets/ScriptableObjects/Farm";

    [MenuItem("DuskPioneer/Phase 1 Step 1 - Time System Setup")]
    static void RunPhase1Step1()
    {
        EditorUtility.DisplayProgressBar("Phase 1", "Step 1/5: 한글 폰트 생성...", 0.05f);
        var koreanFont = GetOrCreateKoreanFont();

        EditorUtility.DisplayProgressBar("Phase 1", "Step 2/5: ScriptableObject 에셋 생성...", 0.15f);
        var timeSettings = CreateTimeSettingsAsset();
        var dayNightSettings = CreateDayNightSettingsAsset();

        EditorUtility.DisplayProgressBar("Phase 1", "Step 3/5: TimeManager 생성...", 0.35f);
        CreateTimeManager(timeSettings);

        EditorUtility.DisplayProgressBar("Phase 1", "Step 4/5: HUD Canvas 생성...", 0.55f);
        CreateTimeHUD(koreanFont);

        EditorUtility.DisplayProgressBar("Phase 1", "Step 5/5: DayNight 오버레이 생성...", 0.75f);
        CreateDayNightOverlay(dayNightSettings);

        EditorUtility.ClearProgressBar();

        // 씬 저장
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Phase1] Time System 셋업 완료! Play 버튼으로 테스트하세요.");
        Debug.Log("[Phase1] 참고: Phase 0을 다시 실행하면 이 셋업도 다시 실행해야 합니다.");
    }

    // ─────────────────────────────────────────
    //  한글 TMP 폰트 생성
    // ─────────────────────────────────────────

    /// <summary>
    /// 한글 지원 TMP_FontAsset을 생성하고 기본 폰트의 Fallback에 등록한다.
    /// Dynamic 모드로 생성하여 필요한 글리프를 런타임에 자동 생성.
    /// </summary>
    static TMP_FontAsset GetOrCreateKoreanFont()
    {
        // 이미 존재하면 로드
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KOREAN_TMP_FONT_PATH);
        if (existing != null)
        {
            Debug.Log("[Phase1] 한글 TMP 폰트 이미 존재. 기존 에셋 사용.");
            RegisterAsFallback(existing);
            return existing;
        }

        // 원본 TTF 로드
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(KOREAN_FONT_PATH);
        if (sourceFont == null)
        {
            Debug.LogError($"[Phase1] 한글 폰트 파일을 찾을 수 없습니다: {KOREAN_FONT_PATH}");
            return null;
        }

        EnsureDirectory(FONT_DIR);

        // Dynamic TMP_FontAsset 생성 (가장 단순한 오버로드 사용)
        var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
        if (fontAsset == null)
        {
            Debug.LogError("[Phase1] TMP_FontAsset 생성 실패!");
            return null;
        }

        fontAsset.atlasPopulationMode = TMPro.AtlasPopulationMode.Dynamic;
        fontAsset.name = "MalgunGothic SDF";

        AssetDatabase.CreateAsset(fontAsset, KOREAN_TMP_FONT_PATH);

        // Atlas 텍스처를 서브에셋으로 저장
        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                if (fontAsset.atlasTextures[i] != null)
                {
                    fontAsset.atlasTextures[i].name = $"MalgunGothic SDF Atlas {i}";
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);
                }
            }
        }

        // Material을 서브에셋으로 저장
        if (fontAsset.material != null)
        {
            fontAsset.material.name = "MalgunGothic SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Phase1] 한글 TMP 폰트 생성 완료: " + KOREAN_TMP_FONT_PATH);

        // 기본 폰트의 Fallback에 등록 (모든 TMP 텍스트에서 한글 자동 지원)
        RegisterAsFallback(fontAsset);

        return fontAsset;
    }

    /// <summary>
    /// 한글 폰트를 기본 TMP 폰트(LiberationSans SDF)의 Fallback 목록에 등록한다.
    /// 이렇게 하면 모든 TMP 텍스트에서 한글이 자동 지원된다.
    /// </summary>
    static void RegisterAsFallback(TMP_FontAsset koreanFont)
    {
        // 기본 TMP 폰트 로드 (Resources 폴더에 있음)
        var defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        if (defaultFont == null)
        {
            Debug.LogWarning("[Phase1] LiberationSans SDF를 찾을 수 없습니다. Fallback 등록 스킵.");
            return;
        }

        // 이미 Fallback에 등록되어 있으면 스킵
        if (defaultFont.fallbackFontAssetTable != null)
        {
            foreach (var fb in defaultFont.fallbackFontAssetTable)
            {
                if (fb != null && fb.name == koreanFont.name) return;
            }
        }

        // Fallback 목록에 추가
        if (defaultFont.fallbackFontAssetTable == null)
            defaultFont.fallbackFontAssetTable = new List<TMP_FontAsset>();

        defaultFont.fallbackFontAssetTable.Add(koreanFont);
        EditorUtility.SetDirty(defaultFont);
        AssetDatabase.SaveAssets();

        Debug.Log("[Phase1] 한글 폰트를 LiberationSans SDF의 Fallback에 등록 완료.");
    }

    // ─────────────────────────────────────────
    //  ScriptableObject 에셋 생성
    // ─────────────────────────────────────────

    static SO_TimeSettings CreateTimeSettingsAsset()
    {
        string path = SO_DIR + "/SO_TimeSettings.asset";

        // 이미 존재하면 로드
        var existing = AssetDatabase.LoadAssetAtPath<SO_TimeSettings>(path);
        if (existing != null)
        {
            Debug.Log("[Phase1] SO_TimeSettings 이미 존재. 기존 에셋 사용.");
            return existing;
        }

        EnsureDirectory(SO_DIR);

        var so = ScriptableObject.CreateInstance<SO_TimeSettings>();
        so.gameMinutesPerRealSecond = 1.6f;
        so.dayStartHour = 6;
        so.nightStartHour = 18;
        so.forceSleepHour = 2;
        so.daysPerSeason = 28;
        so.seasonsPerYear = 4;

        AssetDatabase.CreateAsset(so, path);
        Debug.Log("[Phase1] SO_TimeSettings 생성: " + path);
        return so;
    }

    static SO_DayNightSettings CreateDayNightSettingsAsset()
    {
        string path = SO_DIR + "/SO_DayNightSettings.asset";

        var existing = AssetDatabase.LoadAssetAtPath<SO_DayNightSettings>(path);
        if (existing != null)
        {
            Debug.Log("[Phase1] SO_DayNightSettings 이미 존재. 기존 에셋 사용.");
            return existing;
        }

        EnsureDirectory(SO_DIR);

        var so = ScriptableObject.CreateInstance<SO_DayNightSettings>();

        // 오버레이 색상 Gradient 구성
        // 자정=짙은 파랑, 새벽=주황, 낮=흰색, 황혼=주황, 밤=짙은 파랑
        var colorKeys = new GradientColorKey[]
        {
            new(new Color(0.05f, 0.05f, 0.2f), 0.00f),   // 00:00 자정 — 짙은 파랑
            new(new Color(0.05f, 0.05f, 0.2f), 0.208f),   // 05:00 — 짙은 파랑
            new(new Color(0.8f, 0.5f, 0.2f),   0.250f),   // 06:00 — 따뜻한 주황 (새벽)
            new(new Color(1f, 1f, 1f),          0.333f),   // 08:00 — 흰색 (낮)
            new(new Color(1f, 1f, 1f),          0.708f),   // 17:00 — 흰색 (낮)
            new(new Color(0.8f, 0.4f, 0.15f),   0.792f),   // 19:00 — 따뜻한 주황 (황혼)
            new(new Color(0.05f, 0.05f, 0.2f), 0.875f),   // 21:00 — 짙은 파랑 (밤)
            new(new Color(0.05f, 0.05f, 0.2f), 1.00f),    // 24:00 — 짙은 파랑
        };
        var alphaKeys = new GradientAlphaKey[]
        {
            new(1f, 0f),
            new(1f, 1f),
        };

        var gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);
        so.overlayColorOverDay = gradient;

        // 투명도 커브: 낮(8-17시)=0, 밤(21-5시)=0.55, 전환구간 점진적
        so.overlayAlphaCurve = new AnimationCurve(
            new Keyframe(0.000f, 0.55f),  // 00:00 — 밤 (어두움)
            new Keyframe(0.208f, 0.50f),  // 05:00 — 새벽 직전
            new Keyframe(0.292f, 0.15f),  // 07:00 — 새벽 전환
            new Keyframe(0.333f, 0.00f),  // 08:00 — 낮 (투명)
            new Keyframe(0.708f, 0.00f),  // 17:00 — 낮 (투명)
            new Keyframe(0.792f, 0.15f),  // 19:00 — 황혼 전환
            new Keyframe(0.875f, 0.50f),  // 21:00 — 밤 (어두움)
            new Keyframe(1.000f, 0.55f)   // 24:00 — 밤 (어두움)
        );

        AssetDatabase.CreateAsset(so, path);
        Debug.Log("[Phase1] SO_DayNightSettings 생성: " + path);
        return so;
    }

    // ─────────────────────────────────────────
    //  TimeManager 게임오브젝트
    // ─────────────────────────────────────────

    static void CreateTimeManager(SO_TimeSettings settings)
    {
        // 이미 존재하면 스킵
        if (Object.FindFirstObjectByType<TimeManager>() != null)
        {
            Debug.Log("[Phase1] TimeManager 이미 존재. 스킵.");
            return;
        }

        var go = new GameObject("TimeManager");
        var tm = go.AddComponent<TimeManager>();

        // SerializedObject로 private _settings 필드 할당
        var so = new SerializedObject(tm);
        so.FindProperty("_settings").objectReferenceValue = settings;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[Phase1] TimeManager 생성 완료.");
    }

    // ─────────────────────────────────────────
    //  HUD Canvas + TimeHUD
    // ─────────────────────────────────────────

    static void CreateTimeHUD(TMP_FontAsset koreanFont)
    {
        // 이미 존재하면 폰트만 교체
        var existingHUD = Object.FindFirstObjectByType<TimeHUD>();
        if (existingHUD != null)
        {
            // 기존 HUD의 폰트 교체
            if (koreanFont != null)
            {
                var existingText = existingHUD.GetComponentInChildren<TextMeshProUGUI>();
                if (existingText != null)
                {
                    existingText.font = koreanFont;
                    Debug.Log("[Phase1] 기존 TimeHUD 폰트를 한글 폰트로 교체.");
                }
            }
            return;
        }

        // Canvas 생성
        var canvasObj = new GameObject("HUD Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // TimePanel (배경 + TimeHUD 컴포넌트)
        var panelObj = new GameObject("TimePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);

        var panelRect = panelObj.AddComponent<RectTransform>();
        // 상단 중앙 앵커
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -10);
        panelRect.sizeDelta = new Vector2(400, 50);

        // 반투명 검정 배경
        var panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.4f);
        panelImage.raycastTarget = false;

        // TimeText (TextMeshProUGUI)
        var textObj = new GameObject("TimeText");
        textObj.transform.SetParent(panelObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "1년차 봄 1일 | 06:00";
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        // 한글 폰트 적용
        if (koreanFont != null)
        {
            tmp.font = koreanFont;
        }

        // TimeHUD 컴포넌트 추가 및 레퍼런스 연결
        var hud = panelObj.AddComponent<TimeHUD>();
        var hudSO = new SerializedObject(hud);
        hudSO.FindProperty("_timeText").objectReferenceValue = tmp;
        hudSO.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[Phase1] HUD Canvas + TimeHUD 생성 완료 (한글 폰트 적용).");
    }

    // ─────────────────────────────────────────
    //  DayNight 오버레이
    // ─────────────────────────────────────────

    static void CreateDayNightOverlay(SO_DayNightSettings settings)
    {
        // 이미 존재하면 스킵
        if (Object.FindFirstObjectByType<DayNightController>() != null)
        {
            Debug.Log("[Phase1] DayNightController 이미 존재. 스킵.");
            return;
        }

        // 별도 Canvas (sortingOrder=100, raycaster 없음)
        var canvasObj = new GameObject("DayNight Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // GraphicRaycaster 추가하지 않음 — 클릭 차단 방지

        // 전체 화면 오버레이 Image
        var overlayObj = new GameObject("NightOverlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);

        var overlayRect = overlayObj.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        var overlayImage = overlayObj.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 0); // 시작 시 투명
        overlayImage.raycastTarget = false; // 입력 차단 방지

        // DayNightController 컴포넌트
        var controller = overlayObj.AddComponent<DayNightController>();
        var controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("_settings").objectReferenceValue = settings;
        controllerSO.FindProperty("_overlayImage").objectReferenceValue = overlayImage;
        controllerSO.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[Phase1] DayNight Canvas + 오버레이 생성 완료.");
    }

    // ═══════════════════════════════════════════
    //  Phase 1 Step 2: 도구 시스템
    // ═══════════════════════════════════════════

    [MenuItem("DuskPioneer/Phase 1 Step 2 - Tool System Setup")]
    static void RunPhase1Step2()
    {
        EditorUtility.DisplayProgressBar("Phase 1 Step 2", "1/5: 도구 아이콘 Import 설정...", 0.10f);
        FixToolIconImports();

        EditorUtility.DisplayProgressBar("Phase 1 Step 2", "2/5: SO_ToolData 에셋 생성...", 0.25f);
        var toolAssets = CreateToolDataAssets();

        EditorUtility.DisplayProgressBar("Phase 1 Step 2", "3/5: Player에 컴포넌트 추가...", 0.45f);
        SetupPlayerToolComponents(toolAssets);

        EditorUtility.DisplayProgressBar("Phase 1 Step 2", "4/5: StaminaBar HUD 생성...", 0.65f);
        CreateStaminaHUD();

        EditorUtility.DisplayProgressBar("Phase 1 Step 2", "5/5: ToolBar HUD 생성...", 0.85f);
        CreateToolHUD(toolAssets);

        EditorUtility.ClearProgressBar();

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Phase1 Step2] 도구 시스템 셋업 완료! Play 버튼으로 테스트하세요.");
        Debug.Log("[Phase1 Step2] 1~4키로 도구 전환, Space로 도구 사용.");
    }

    // ─────────────────────────────────────────
    //  도구 아이콘 Import 설정
    // ─────────────────────────────────────────

    static void FixToolIconImports()
    {
        string[] iconNames = { "Hoe", "WateringCan", "Axe", "Shovel" };
        foreach (var name in iconNames)
        {
            string path = ITEMS_PATH + "/Tool/" + name + ".png";
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogWarning($"[Phase1 Step2] 아이콘 파일 없음: {path}"); continue; }

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        Debug.Log("[Phase1 Step2] 도구 아이콘 Import 설정 완료.");

        // Attack.png 재슬라이싱 (64x16 = 4x1 그리드, Phase0에서 4x4로 잘못 슬라이싱됨)
        ReSliceAttackPng();
    }

    /// <summary>
    /// Attack.png를 올바른 4x1 그리드로 재슬라이싱한다.
    /// (64x16 텍스처를 Phase0이 4x4로 잘못 잘랐기 때문)
    /// </summary>
    static void ReSliceAttackPng()
    {
        string path = PRINCESS_PATH + "/SeparateAnim/Attack.png";
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning($"[Phase1 Step2] Attack.png 없음: {path}"); return; }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return;

        int cols = 4;
        int rows = 1;  // 64x16 → 4열 1행
        float cellW = tex.width / (float)cols;   // 16
        float cellH = tex.height / (float)rows;  // 16

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = 16;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;

        string baseName = "Attack";
        var rects = new List<SpriteMetaData>();
        int index = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                rects.Add(new SpriteMetaData
                {
                    name = $"{baseName}_{index:D2}",
                    rect = new Rect(c * cellW, (rows - 1 - r) * cellH, cellW, cellH),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
                index++;
            }
        }

        ti.spritesheet = rects.ToArray();
        ti.SaveAndReimport();
        Debug.Log($"[Phase1 Step2] Attack.png 재슬라이싱 완료: {cols}x{rows} = {index}개 (각 {cellW}x{cellH}px)");
    }

    // ─────────────────────────────────────────
    //  SO_ToolData 에셋 4개 생성
    // ─────────────────────────────────────────

    static SO_ToolData[] CreateToolDataAssets()
    {
        EnsureDirectory(SO_TOOL_DIR);

        var toolDefs = new (string name, ToolType type, string iconFile, float staminaCost)[]
        {
            ("괭이", ToolType.Hoe, "Hoe", 5f),
            ("물뿌리개", ToolType.WateringCan, "WateringCan", 3f),
            ("도끼", ToolType.Axe, "Axe", 8f),
            ("삽", ToolType.Shovel, "Shovel", 5f),
        };

        var result = new SO_ToolData[toolDefs.Length];

        for (int i = 0; i < toolDefs.Length; i++)
        {
            var def = toolDefs[i];
            string assetPath = $"{SO_TOOL_DIR}/SO_{def.iconFile}.asset";

            // 이미 존재하면 로드
            var existing = AssetDatabase.LoadAssetAtPath<SO_ToolData>(assetPath);
            if (existing != null)
            {
                result[i] = existing;
                continue;
            }

            var so = ScriptableObject.CreateInstance<SO_ToolData>();
            so.toolName = def.name;
            so.toolType = def.type;
            so.staminaCost = def.staminaCost;
            so.animationSpeed = 10f;

            // 아이콘 로드
            string iconPath = ITEMS_PATH + "/Tool/" + def.iconFile + ".png";
            so.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

            AssetDatabase.CreateAsset(so, assetPath);
            result[i] = so;
            Debug.Log($"[Phase1 Step2] SO_ToolData 생성: {def.name} ({assetPath})");
        }

        AssetDatabase.SaveAssets();
        return result;
    }

    // ─────────────────────────────────────────
    //  Player에 StaminaManager + ToolController 추가
    // ─────────────────────────────────────────

    static void SetupPlayerToolComponents(SO_ToolData[] toolAssets)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[Phase1 Step2] 'Player' 태그가 있는 오브젝트를 찾을 수 없습니다!");
            return;
        }

        // StaminaManager 추가
        var stamina = player.GetComponent<StaminaManager>();
        if (stamina == null)
        {
            stamina = player.AddComponent<StaminaManager>();
            Debug.Log("[Phase1 Step2] StaminaManager 추가.");
        }

        // ToolController 추가
        var toolCtrl = player.GetComponent<ToolController>();
        if (toolCtrl == null)
        {
            toolCtrl = player.AddComponent<ToolController>();
            Debug.Log("[Phase1 Step2] ToolController 추가.");
        }

        // SO_ToolData 할당
        var so = new SerializedObject(toolCtrl);
        var toolsProp = so.FindProperty("tools");
        toolsProp.arraySize = toolAssets.Length;
        for (int i = 0; i < toolAssets.Length; i++)
        {
            toolsProp.GetArrayElementAtIndex(i).objectReferenceValue = toolAssets[i];
        }

        // Attack.png 스프라이트 할당
        // Attack.png = 64x16 = 4x1 그리드 → 방향별 1프레임: [Down, Up, Left, Right]
        var attackSprites = LoadSpritesOrdered(PRINCESS_PATH + "/SeparateAnim/Attack.png");
        if (attackSprites.Length >= 4)
        {
            // 4x1: sprites[0]=Down, [1]=Up, [2]=Left, [3]=Right
            SetSpriteArray(so, "attackDownSprites",  new Sprite[] { attackSprites[0] });
            SetSpriteArray(so, "attackUpSprites",    new Sprite[] { attackSprites[1] });
            SetSpriteArray(so, "attackLeftSprites",  new Sprite[] { attackSprites[2] });
            SetSpriteArray(so, "attackRightSprites", new Sprite[] { attackSprites[3] });

            Debug.Log($"[Phase1 Step2] Attack 스프라이트 {attackSprites.Length}개 로드, 방향별 1프레임 할당.");
        }
        else
        {
            Debug.LogWarning($"[Phase1 Step2] Attack.png 스프라이트 부족: {attackSprites.Length}개 (4개 필요). Phase 0 Setup을 먼저 실행하세요.");
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// SerializedObject를 통해 Sprite[] 필드에 값을 할당한다.
    /// </summary>
    static void SetSpriteArray(SerializedObject so, string propertyName, Sprite[] sprites)
    {
        var prop = so.FindProperty(propertyName);
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }

    // ─────────────────────────────────────────
    //  StaminaBar HUD (상단 우측)
    // ─────────────────────────────────────────

    static void CreateStaminaHUD()
    {
        // 이미 존재하면 삭제 후 재생성 (버그 수정 대응)
        var existing = Object.FindFirstObjectByType<StaminaHUD>();
        if (existing != null)
        {
            Debug.Log("[Phase1 Step2] 기존 StaminaHUD 삭제 후 재생성.");
            Object.DestroyImmediate(existing.gameObject);
        }

        // HUD Canvas 찾기
        var hudCanvas = FindHUDCanvas();
        if (hudCanvas == null) return;

        // StaminaBar 패널
        var barObj = new GameObject("StaminaBar");
        barObj.transform.SetParent(hudCanvas.transform, false);

        var barRect = barObj.AddComponent<RectTransform>();
        // 상단 우측
        barRect.anchorMin = new Vector2(1f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(1f, 1f);
        barRect.anchoredPosition = new Vector2(-20, -15);
        barRect.sizeDelta = new Vector2(200, 20);

        // 배경 (어두운 색)
        var bgImage = barObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.7f);
        bgImage.raycastTarget = false;

        // 채우기 바
        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barObj.transform, false);

        var fillRect = fillObj.AddComponent<RectTransform>();
        // 앵커 기반 스케일링: anchorMax.x를 StaminaHUD가 조절
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);  // 런타임에 StaminaHUD가 x를 변경
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);

        var fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.8f, 0.3f); // 초록색
        fillImage.raycastTarget = false;

        // 라벨
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(barObj.transform, false);

        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = "SP";
        labelTmp.fontSize = 14;
        labelTmp.color = Color.white;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.raycastTarget = false;

        // StaminaHUD 컴포넌트
        var hud = barObj.AddComponent<StaminaHUD>();
        var hudSO = new SerializedObject(hud);
        hudSO.FindProperty("_fillImage").objectReferenceValue = fillImage;
        hudSO.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[Phase1 Step2] StaminaBar HUD 생성 완료.");
    }

    // ─────────────────────────────────────────
    //  ToolBar HUD (하단 중앙, 4슬롯)
    // ─────────────────────────────────────────

    static void CreateToolHUD(SO_ToolData[] toolAssets)
    {
        // 이미 존재하면 스킵
        if (Object.FindFirstObjectByType<ToolHUD>() != null)
        {
            Debug.Log("[Phase1 Step2] ToolHUD 이미 존재. 스킵.");
            return;
        }

        var hudCanvas = FindHUDCanvas();
        if (hudCanvas == null) return;

        // ToolBar 패널
        var barObj = new GameObject("ToolBar");
        barObj.transform.SetParent(hudCanvas.transform, false);

        var barRect = barObj.AddComponent<RectTransform>();
        // 하단 중앙
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = new Vector2(0, 15);

        float slotSize = 50f;
        float spacing = 5f;
        int slotCount = 4;
        float totalWidth = slotCount * slotSize + (slotCount - 1) * spacing;
        barRect.sizeDelta = new Vector2(totalWidth, slotSize);

        // HorizontalLayoutGroup 추가
        var layout = barObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        var slotIcons = new Image[slotCount];
        var slotBorders = new Image[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            // 슬롯 컨테이너 (테두리 역할)
            var slotObj = new GameObject($"ToolSlot{i + 1}");
            slotObj.transform.SetParent(barObj.transform, false);

            var slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(slotSize, slotSize);

            var borderImage = slotObj.AddComponent<Image>();
            borderImage.color = new Color(1f, 1f, 1f, 0.5f); // 기본: 반투명 흰색
            borderImage.raycastTarget = false;

            slotBorders[i] = borderImage;

            // 아이콘
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);

            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            var iconImage = iconObj.AddComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;

            // 아이콘 설정
            if (i < toolAssets.Length && toolAssets[i] != null && toolAssets[i].icon != null)
            {
                iconImage.sprite = toolAssets[i].icon;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.color = new Color(1f, 1f, 1f, 0.2f); // 빈 슬롯
            }

            slotIcons[i] = iconImage;

            // 숫자키 표시
            var numObj = new GameObject("KeyNum");
            numObj.transform.SetParent(slotObj.transform, false);

            var numRect = numObj.AddComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0f, 1f);
            numRect.anchorMax = new Vector2(0f, 1f);
            numRect.pivot = new Vector2(0f, 1f);
            numRect.anchoredPosition = new Vector2(2, -2);
            numRect.sizeDelta = new Vector2(16, 16);

            var numTmp = numObj.AddComponent<TextMeshProUGUI>();
            numTmp.text = (i + 1).ToString();
            numTmp.fontSize = 12;
            numTmp.color = new Color(1f, 1f, 1f, 0.7f);
            numTmp.alignment = TextAlignmentOptions.TopLeft;
            numTmp.raycastTarget = false;
        }

        // ToolHUD 컴포넌트
        var hud = barObj.AddComponent<ToolHUD>();
        var hudSO = new SerializedObject(hud);

        var iconsProp = hudSO.FindProperty("_slotIcons");
        iconsProp.arraySize = slotCount;
        for (int i = 0; i < slotCount; i++)
            iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotIcons[i];

        var bordersProp = hudSO.FindProperty("_slotBorders");
        bordersProp.arraySize = slotCount;
        for (int i = 0; i < slotCount; i++)
            bordersProp.GetArrayElementAtIndex(i).objectReferenceValue = slotBorders[i];

        hudSO.ApplyModifiedPropertiesWithoutUndo();

        // 첫 번째 슬롯을 노란색으로 선택 표시
        slotBorders[0].color = Color.yellow;

        Debug.Log("[Phase1 Step2] ToolBar HUD 생성 완료 (4슬롯).");
    }

    // ═══════════════════════════════════════════
    //  Phase 1 Step 3: 농장 타일 상호작용
    // ═══════════════════════════════════════════

    [MenuItem("DuskPioneer/Phase 1 Step 3 - Farm Tile Setup")]
    static void RunPhase1Step3()
    {
        EditorUtility.DisplayProgressBar("Phase 1 Step 3", "1/4: 농장 타일 에셋 생성...", 0.15f);
        CreateFarmTileAssets();

        EditorUtility.DisplayProgressBar("Phase 1 Step 3", "2/4: SO_FarmSettings 생성...", 0.35f);
        var farmSettings = CreateFarmSettingsAsset();

        EditorUtility.DisplayProgressBar("Phase 1 Step 3", "3/4: FarmOverlay 타일맵 레이어 추가...", 0.55f);
        SetupFarmOverlayTilemap();

        EditorUtility.DisplayProgressBar("Phase 1 Step 3", "4/4: FarmManager 생성...", 0.75f);
        CreateFarmManagerObject(farmSettings);

        EditorUtility.ClearProgressBar();

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Phase1 Step3] 농장 타일 시스템 셋업 완료!");
        Debug.Log("[Phase1 Step3] 1키(괭이)→잔디에서 Space로 경작. 2키(물뿌리개)→경작지에서 Space로 물주기. 4키(삽)→경작지 되돌리기.");
    }

    // ─────────────────────────────────────────
    //  농장 타일 에셋 생성
    // ─────────────────────────────────────────

    /// <summary>
    /// TilledDirt, WateredDirt 타일 에셋을 생성한다.
    /// TilesetField.png의 스프라이트를 사용.
    /// </summary>
    static void CreateFarmTileAssets()
    {
        EnsureDirectory(TILE_DIR);

        var fieldSprites = LoadSpritesOrdered(TILESET_PATH + "/TilesetField.png");
        if (fieldSprites.Length < 10)
        {
            Debug.LogError("[Phase1 Step3] TilesetField 스프라이트 부족! Phase 0 Setup을 먼저 실행하세요.");
            return;
        }

        // TilledDirt — 인덱스 6: 진한 흙 타일 (row 1, col 1)
        string tilledPath = TILE_DIR + "/TilledDirt.asset";
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>(tilledPath) == null)
        {
            var tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
            tile.sprite = fieldSprites[6];
            tile.color = Color.white;
            AssetDatabase.CreateAsset(tile, tilledPath);
            Debug.Log("[Phase1 Step3] TilledDirt.asset 생성.");
        }

        // WateredDirt — 같은 스프라이트, 어두운 색상 틴트로 젖은 흙 표현
        string wateredPath = TILE_DIR + "/WateredDirt.asset";
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>(wateredPath) == null)
        {
            var tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
            tile.sprite = fieldSprites[6];
            tile.color = new Color(0.55f, 0.45f, 0.35f); // 어두운 틴트 = 젖은 흙
            AssetDatabase.CreateAsset(tile, wateredPath);
            Debug.Log("[Phase1 Step3] WateredDirt.asset 생성.");
        }

        AssetDatabase.SaveAssets();
    }

    // ─────────────────────────────────────────
    //  SO_FarmSettings 에셋 생성
    // ─────────────────────────────────────────

    static SO_FarmSettings CreateFarmSettingsAsset()
    {
        EnsureDirectory(SO_FARM_DIR);
        string path = SO_FARM_DIR + "/SO_FarmSettings.asset";

        var existing = AssetDatabase.LoadAssetAtPath<SO_FarmSettings>(path);
        if (existing != null)
        {
            Debug.Log("[Phase1 Step3] SO_FarmSettings 이미 존재. 기존 에셋 사용.");
            return existing;
        }

        var so = ScriptableObject.CreateInstance<SO_FarmSettings>();

        // 타일 에셋 할당
        so.grassTile = AssetDatabase.LoadAssetAtPath<TileBase>(TILE_DIR + "/Grass.asset");
        so.tilledTile = AssetDatabase.LoadAssetAtPath<TileBase>(TILE_DIR + "/TilledDirt.asset");
        so.wateredTile = AssetDatabase.LoadAssetAtPath<TileBase>(TILE_DIR + "/WateredDirt.asset");
        // plantedTile은 Step 4에서 할당

        if (so.grassTile == null)
            Debug.LogWarning("[Phase1 Step3] Grass.asset을 찾을 수 없습니다! Phase 0을 먼저 실행하세요.");

        AssetDatabase.CreateAsset(so, path);
        AssetDatabase.SaveAssets();
        Debug.Log("[Phase1 Step3] SO_FarmSettings 생성: " + path);
        return so;
    }

    // ─────────────────────────────────────────
    //  FarmOverlay 타일맵 레이어 추가
    // ─────────────────────────────────────────

    static void SetupFarmOverlayTilemap()
    {
        // Grid 오브젝트 찾기
        var gridObj = GameObject.Find("Grid");
        if (gridObj == null)
        {
            Debug.LogError("[Phase1 Step3] Grid 오브젝트를 찾을 수 없습니다! Phase 0을 먼저 실행하세요.");
            return;
        }

        // 이미 FarmOverlay가 있으면 스킵
        var existingOverlay = gridObj.transform.Find("FarmOverlay");
        if (existingOverlay != null)
        {
            Debug.Log("[Phase1 Step3] FarmOverlay 이미 존재. 스킵.");
        }
        else
        {
            // FarmOverlay 타일맵 생성
            var overlayObj = new GameObject("FarmOverlay");
            overlayObj.transform.SetParent(gridObj.transform);
            overlayObj.transform.localPosition = Vector3.zero;

            overlayObj.AddComponent<Tilemap>();
            var renderer = overlayObj.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 1;

            Debug.Log("[Phase1 Step3] FarmOverlay 타일맵 레이어 생성 (sortingOrder=1).");
        }

        // Water 타일맵의 sortingOrder를 2로 변경 (FarmOverlay 위에 렌더링)
        var waterObj = gridObj.transform.Find("Water");
        if (waterObj != null)
        {
            var waterRenderer = waterObj.GetComponent<TilemapRenderer>();
            if (waterRenderer != null && waterRenderer.sortingOrder < 2)
            {
                waterRenderer.sortingOrder = 2;
                Debug.Log("[Phase1 Step3] Water 타일맵 sortingOrder → 2로 변경.");
            }
        }
    }

    // ─────────────────────────────────────────
    //  FarmManager 게임오브젝트 생성
    // ─────────────────────────────────────────

    static void CreateFarmManagerObject(SO_FarmSettings settings)
    {
        // 이미 존재하면 스킵
        if (Object.FindFirstObjectByType<FarmManager>() != null)
        {
            Debug.Log("[Phase1 Step3] FarmManager 이미 존재. 스킵.");
            return;
        }

        var go = new GameObject("FarmManager");
        var fm = go.AddComponent<FarmManager>();

        // SerializedObject로 참조 할당
        var so = new SerializedObject(fm);
        so.FindProperty("_settings").objectReferenceValue = settings;

        // Ground 타일맵 찾기
        var gridObj = GameObject.Find("Grid");
        if (gridObj != null)
        {
            var groundObj = gridObj.transform.Find("Ground");
            if (groundObj != null)
                so.FindProperty("_groundTilemap").objectReferenceValue = groundObj.GetComponent<Tilemap>();

            var overlayObj = gridObj.transform.Find("FarmOverlay");
            if (overlayObj != null)
                so.FindProperty("_farmOverlayTilemap").objectReferenceValue = overlayObj.GetComponent<Tilemap>();
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[Phase1 Step3] FarmManager 생성 완료.");
    }

    // ─────────────────────────────────────────
    //  유틸리티
    // ─────────────────────────────────────────

    /// <summary>
    /// HUD Canvas를 찾아 반환한다. 없으면 에러 로그.
    /// </summary>
    static Canvas FindHUDCanvas()
    {
        // 이름으로 찾기
        var go = GameObject.Find("HUD Canvas");
        if (go != null) return go.GetComponent<Canvas>();

        Debug.LogError("[Phase1 Step2] HUD Canvas를 찾을 수 없습니다! Phase 1 Step 1을 먼저 실행하세요.");
        return null;
    }

    /// <summary>
    /// 슬라이싱된 스프라이트를 번호 순으로 정렬하여 로드한다.
    /// Phase0Setup의 동일 메서드와 같은 로직.
    /// </summary>
    static Sprite[] LoadSpritesOrdered(string path)
    {
        string baseName = Path.GetFileNameWithoutExtension(path);
        string namePattern = $@"^{System.Text.RegularExpressions.Regex.Escape(baseName)}_\d{{2}}$";

        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .Where(s => System.Text.RegularExpressions.Regex.IsMatch(s.name, namePattern))
            .OrderBy(s =>
            {
                var match = System.Text.RegularExpressions.Regex.Match(s.name, @"(\d+)$");
                return match.Success ? int.Parse(match.Groups[1].Value) : 0;
            })
            .ToArray();
    }

    static void EnsureDirectory(string assetPath)
    {
        string fullPath = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            AssetDatabase.Refresh();
        }
    }
}
