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

        // Attack 스프라이트: ToolController에서 더 이상 사용하지 않음 (8방향 전환 후 제거됨)
        Debug.Log("[Phase1 Step2] Attack 스프라이트 할당 건너뜀 (8방향 캐릭터에서는 CharacterSwapSetup 사용).");

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

    // ═══════════════════════════════════════════
    //  Phase 1 Step 4: 작물 시스템
    // ═══════════════════════════════════════════

    private const string SO_CROP_DIR = "Assets/ScriptableObjects/Crops";
    private const string FOOD_PATH = NINJA_BASE + "/Items/Food";
    private const string ANIMATED_PLANT_PATH = NINJA_BASE + "/Backgrounds/Animated/Plant/SpriteSheet16x16.png";

    [MenuItem("DuskPioneer/Phase 1 Step 4 - Crop System Setup")]
    static void RunPhase1Step4()
    {
        EditorUtility.DisplayProgressBar("Phase 1 Step 4", "1/4: 작물 스프라이트 Import 설정...", 0.15f);
        FixCropSpriteImports();

        EditorUtility.DisplayProgressBar("Phase 1 Step 4", "2/4: 식물 스프라이트 시트 슬라이싱...", 0.35f);
        SlicePlantSpriteSheet();

        EditorUtility.DisplayProgressBar("Phase 1 Step 4", "3/4: SO_CropData 에셋 생성...", 0.55f);
        var cropAssets = CreateCropDataAssets();

        EditorUtility.DisplayProgressBar("Phase 1 Step 4", "4/4: FarmManager에 작물 데이터 할당...", 0.80f);
        AssignCropsToFarmManager(cropAssets);

        EditorUtility.ClearProgressBar();

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Phase1 Step4] 작물 시스템 셋업 완료!");
        Debug.Log("[Phase1 Step4] 괭이→Space 경작 → E키 씨앗심기 → 물주기+Sleep 반복 → E키 수확. Q키로 씨앗 전환.");
    }

    // ─────────────────────────────────────────
    //  작물 스프라이트 Import 설정
    // ─────────────────────────────────────────

    /// <summary>
    /// 씨앗/수확 아이템 스프라이트의 Import 설정을 픽셀아트에 맞게 수정.
    /// </summary>
    static void FixCropSpriteImports()
    {
        string[] spriteFiles =
        {
            FOOD_PATH + "/Seed1.png",
            FOOD_PATH + "/Seed2.png",
            FOOD_PATH + "/Seed3.png",
            FOOD_PATH + "/SeedBig1.png",
            FOOD_PATH + "/SeedBig2.png",
            FOOD_PATH + "/SeedBig3.png",
            FOOD_PATH + "/Nut.png",
            FOOD_PATH + "/Nut2.png",
            FOOD_PATH + "/TeaLeaf.png",
        };

        foreach (var path in spriteFiles)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }

        Debug.Log("[Phase1 Step4] 작물 스프라이트 Import 설정 완료.");
    }

    // ─────────────────────────────────────────
    //  식물 스프라이트 시트 슬라이싱
    // ─────────────────────────────────────────

    /// <summary>
    /// Animated/Plant/SpriteSheet16x16.png를 4프레임으로 슬라이싱.
    /// 새싹/성장 단계 스프라이트로 활용.
    /// </summary>
    static void SlicePlantSpriteSheet()
    {
        var ti = AssetImporter.GetAtPath(ANIMATED_PLANT_PATH) as TextureImporter;
        if (ti == null)
        {
            Debug.LogWarning("[Phase1 Step4] 식물 스프라이트 시트를 찾을 수 없습니다: " + ANIMATED_PLANT_PATH);
            return;
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ANIMATED_PLANT_PATH);
        if (tex == null) return;

        int cols = tex.width / 16; // 64/16 = 4프레임
        float cellW = 16f;
        float cellH = 16f;

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = 16;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;

        var rects = new List<SpriteMetaData>();
        for (int c = 0; c < cols; c++)
        {
            rects.Add(new SpriteMetaData
            {
                name = $"Plant_{c:D2}",
                rect = new Rect(c * cellW, 0, cellW, cellH),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            });
        }

        ti.spritesheet = rects.ToArray();
        ti.SaveAndReimport();
        Debug.Log($"[Phase1 Step4] 식물 스프라이트 시트 슬라이싱 완료: {cols}프레임");
    }

    // ─────────────────────────────────────────
    //  SO_CropData 에셋 3개 생성
    // ─────────────────────────────────────────

    /// <summary>
    /// 무(3일), 당근(4일), 감자(5일) 작물 데이터를 생성한다.
    /// </summary>
    static SO_CropData[] CreateCropDataAssets()
    {
        EnsureDirectory(SO_CROP_DIR);

        // 식물 성장 스프라이트 로드 (슬라이싱된 Plant_00~03)
        // LoadSpritesOrdered는 파일명 기반 패턴이므로, 직접 이름으로 로드
        var allPlantSprites = AssetDatabase.LoadAllAssetsAtPath(ANIMATED_PLANT_PATH)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();
        Sprite plantSprout = allPlantSprites.Length > 0 ? allPlantSprites[0] : null;
        Sprite plantGrown = allPlantSprites.Length > 1 ? allPlantSprites[1] : null;
        Sprite plantMature = allPlantSprites.Length > 2 ? allPlantSprites[2] : null;
        if (allPlantSprites.Length == 0)
            Debug.LogWarning("[Phase1 Step4] 식물 스프라이트를 로드할 수 없습니다! SlicePlantSpriteSheet가 먼저 실행되었는지 확인하세요.");

        // 씨앗 스프라이트 로드
        var seed1 = AssetDatabase.LoadAssetAtPath<Sprite>(FOOD_PATH + "/Seed1.png");
        var seed2 = AssetDatabase.LoadAssetAtPath<Sprite>(FOOD_PATH + "/Seed2.png");
        var seed3 = AssetDatabase.LoadAssetAtPath<Sprite>(FOOD_PATH + "/Seed3.png");

        // 수확물 스프라이트 (SeedBig = 수확된 작물 아이콘)
        var harvest1 = AssetDatabase.LoadAssetAtPath<Sprite>(FOOD_PATH + "/SeedBig1.png");
        var harvest2 = AssetDatabase.LoadAssetAtPath<Sprite>(FOOD_PATH + "/SeedBig2.png");
        var harvest3 = AssetDatabase.LoadAssetAtPath<Sprite>(FOOD_PATH + "/SeedBig3.png");

        // 작물 정의: (이름, 성장일, 씨앗스프라이트, 수확스프라이트, 성장단계스프라이트 배열)
        var cropDefs = new (string name, int days, Sprite seed, Sprite harvested, Sprite[] stages)[]
        {
            // 무 (Turnip): 3일, 3단계 (씨앗→새싹→수확가능)
            ("무", 3, seed1, harvest1, new[] { seed1, plantSprout, plantMature }),
            // 당근 (Carrot): 4일, 4단계 (씨앗→새싹→성장→수확가능)
            ("당근", 4, seed2, harvest2, new[] { seed2, plantSprout, plantGrown, plantMature }),
            // 감자 (Potato): 5일, 4단계 (씨앗→새싹→성장→수확가능)
            ("감자", 5, seed3, harvest3, new[] { seed3, plantSprout, plantGrown, plantMature }),
        };

        var result = new SO_CropData[cropDefs.Length];

        for (int i = 0; i < cropDefs.Length; i++)
        {
            var def = cropDefs[i];
            string assetPath = $"{SO_CROP_DIR}/SO_Crop_{def.name}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<SO_CropData>(assetPath);
            if (existing != null)
            {
                // 기존 에셋 갱신 (스프라이트가 null이었을 수 있으므로)
                existing.cropName = def.name;
                existing.growthDays = def.days;
                existing.growthStageSprites = def.stages;
                existing.harvestedSprite = def.harvested;
                EditorUtility.SetDirty(existing);
                result[i] = existing;
                Debug.Log($"[Phase1 Step4] SO_CropData 갱신: {def.name} (성장 {def.days}일, {def.stages.Length}단계)");
                continue;
            }

            var so = ScriptableObject.CreateInstance<SO_CropData>();
            so.cropName = def.name;
            so.growthDays = def.days;
            so.growthStageSprites = def.stages;
            so.harvestedSprite = def.harvested;

            AssetDatabase.CreateAsset(so, assetPath);
            result[i] = so;
            Debug.Log($"[Phase1 Step4] SO_CropData 생성: {def.name} (성장 {def.days}일, {def.stages.Length}단계)");
        }

        AssetDatabase.SaveAssets();
        return result;
    }

    // ─────────────────────────────────────────
    //  FarmManager에 작물 데이터 할당
    // ─────────────────────────────────────────

    /// <summary>
    /// FarmManager의 _crops 배열에 SO_CropData를 할당한다.
    /// </summary>
    static void AssignCropsToFarmManager(SO_CropData[] crops)
    {
        var fm = Object.FindFirstObjectByType<FarmManager>();
        if (fm == null)
        {
            Debug.LogError("[Phase1 Step4] FarmManager를 찾을 수 없습니다! Step 3을 먼저 실행하세요.");
            return;
        }

        var so = new SerializedObject(fm);
        var cropsProp = so.FindProperty("_crops");
        cropsProp.arraySize = crops.Length;
        for (int i = 0; i < crops.Length; i++)
        {
            cropsProp.GetArrayElementAtIndex(i).objectReferenceValue = crops[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[Phase1 Step4] FarmManager에 작물 {crops.Length}종 할당 완료.");
    }

    // ═══════════════════════════════════════════
    //  Phase 1 Step 5: 인벤토리 시스템
    // ═══════════════════════════════════════════

    private const string SO_ITEM_DIR = "Assets/ScriptableObjects/Items";
    private const string TOOL_SPRITE_PATH = NINJA_BASE + "/Items/Tool";

    [MenuItem("DuskPioneer/Phase 1 Step 5 - Inventory System Setup")]
    static void RunPhase1Step5()
    {
        EditorUtility.DisplayProgressBar("Phase 1 Step 5", "1/5: 도구 스프라이트 Import 설정...", 0.10f);
        FixToolSpriteImports();

        EditorUtility.DisplayProgressBar("Phase 1 Step 5", "2/5: SO_ItemData 에셋 생성...", 0.25f);
        var itemAssets = CreateItemDataAssets();

        EditorUtility.DisplayProgressBar("Phase 1 Step 5", "3/5: Player에 Inventory 컴포넌트 추가...", 0.45f);
        SetupInventoryComponent(itemAssets);

        EditorUtility.DisplayProgressBar("Phase 1 Step 5", "4/5: 인벤토리 UI 생성...", 0.65f);
        CreateInventoryUI();

        EditorUtility.DisplayProgressBar("Phase 1 Step 5", "5/5: 씬 저장...", 0.90f);

        EditorUtility.ClearProgressBar();

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Phase1 Step5] 인벤토리 시스템 셋업 완료!");
        Debug.Log("[Phase1 Step5] Tab/I키: 인벤토리 열기/닫기. 방향키: 슬롯 이동. X키: 아이템 버리기.");
    }

    // ─────────────────────────────────────────
    //  도구 스프라이트 Import 설정
    // ─────────────────────────────────────────

    static void FixToolSpriteImports()
    {
        string[] toolFiles = { "Hoe.png", "WateringCan.png", "Shovel.png", "Axe.png" };
        foreach (var file in toolFiles)
        {
            string path = TOOL_SPRITE_PATH + "/" + file;
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
        Debug.Log("[Phase1 Step5] 도구 스프라이트 Import 설정 완료.");
    }

    // ─────────────────────────────────────────
    //  SO_ItemData 에셋 생성 (씨앗 3, 작물 3, 도구 4)
    // ─────────────────────────────────────────

    /// <summary>
    /// 씨앗, 작물, 도구 아이템 데이터를 생성한다.
    /// 반환: [0-2]=씨앗, [3-5]=작물, [6-9]=도구
    /// </summary>
    static SO_ItemData[] CreateItemDataAssets()
    {
        EnsureDirectory(SO_ITEM_DIR);

        // 작물 SO_CropData 로드
        var cropTurnip = AssetDatabase.LoadAssetAtPath<SO_CropData>(SO_CROP_DIR + "/SO_Crop_무.asset");
        var cropCarrot = AssetDatabase.LoadAssetAtPath<SO_CropData>(SO_CROP_DIR + "/SO_Crop_당근.asset");
        var cropPotato = AssetDatabase.LoadAssetAtPath<SO_CropData>(SO_CROP_DIR + "/SO_Crop_감자.asset");

        // 스프라이트 로드
        var seed1 = AssetDatabase.LoadAssetAtPath<Sprite>(FOOD_PATH + "/Seed1.png");
        var seed2 = AssetDatabase.LoadAssetAtPath<Sprite>(FOOD_PATH + "/Seed2.png");
        var seed3 = AssetDatabase.LoadAssetAtPath<Sprite>(FOOD_PATH + "/Seed3.png");
        var harvest1 = AssetDatabase.LoadAssetAtPath<Sprite>(FOOD_PATH + "/SeedBig1.png");
        var harvest2 = AssetDatabase.LoadAssetAtPath<Sprite>(FOOD_PATH + "/SeedBig2.png");
        var harvest3 = AssetDatabase.LoadAssetAtPath<Sprite>(FOOD_PATH + "/SeedBig3.png");

        var hoeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TOOL_SPRITE_PATH + "/Hoe.png");
        var waterCanSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TOOL_SPRITE_PATH + "/WateringCan.png");
        var shovelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TOOL_SPRITE_PATH + "/Shovel.png");
        var axeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TOOL_SPRITE_PATH + "/Axe.png");

        // 아이템 정의 목록
        var defs = new (string name, string desc, ItemType type, Sprite icon, int maxStack, int sellPrice, SO_CropData cropData)[]
        {
            // 씨앗 [0-2]
            ("무 씨앗", "무를 심을 수 있는 씨앗.", ItemType.Seed, seed1, 99, 5, cropTurnip),
            ("당근 씨앗", "당근을 심을 수 있는 씨앗.", ItemType.Seed, seed2, 99, 8, cropCarrot),
            ("감자 씨앗", "감자를 심을 수 있는 씨앗.", ItemType.Seed, seed3, 99, 10, cropPotato),
            // 작물 [3-5]
            ("무", "수확한 무. 팔거나 요리 재료로 사용.", ItemType.Crop, harvest1, 99, 15, null),
            ("당근", "수확한 당근. 팔거나 요리 재료로 사용.", ItemType.Crop, harvest2, 99, 25, null),
            ("감자", "수확한 감자. 팔거나 요리 재료로 사용.", ItemType.Crop, harvest3, 99, 35, null),
            // 도구 [6-9]
            ("괭이", "땅을 경작하는 도구.", ItemType.Tool, hoeSprite, 1, 0, null),
            ("물뿌리개", "작물에 물을 주는 도구.", ItemType.Tool, waterCanSprite, 1, 0, null),
            ("삽", "경작지를 되돌리는 도구.", ItemType.Tool, shovelSprite, 1, 0, null),
            ("도끼", "나무를 벨 수 있는 도구.", ItemType.Tool, axeSprite, 1, 0, null),
        };

        var result = new SO_ItemData[defs.Length];

        for (int i = 0; i < defs.Length; i++)
        {
            var def = defs[i];
            string safeName = def.name.Replace(" ", "_");
            string assetPath = $"{SO_ITEM_DIR}/SO_Item_{safeName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<SO_ItemData>(assetPath);
            if (existing != null)
            {
                // 기존 에셋 갱신
                existing.itemName = def.name;
                existing.description = def.desc;
                existing.itemType = def.type;
                existing.icon = def.icon;
                existing.maxStack = def.maxStack;
                existing.sellPrice = def.sellPrice;
                existing.cropData = def.cropData;
                EditorUtility.SetDirty(existing);
                result[i] = existing;
                Debug.Log($"[Phase1 Step5] SO_ItemData 갱신: {def.name}");
                continue;
            }

            var so = ScriptableObject.CreateInstance<SO_ItemData>();
            so.itemName = def.name;
            so.description = def.desc;
            so.itemType = def.type;
            so.icon = def.icon;
            so.maxStack = def.maxStack;
            so.sellPrice = def.sellPrice;
            so.cropData = def.cropData;

            AssetDatabase.CreateAsset(so, assetPath);
            result[i] = so;
            Debug.Log($"[Phase1 Step5] SO_ItemData 생성: {def.name}");
        }

        AssetDatabase.SaveAssets();
        return result;
    }

    // ─────────────────────────────────────────
    //  Player에 Inventory 컴포넌트 추가
    // ─────────────────────────────────────────

    /// <summary>
    /// Player에 Inventory 컴포넌트를 추가하고, 시작 아이템과 수확물 매핑을 설정한다.
    /// itemAssets: [0-2]=씨앗, [3-5]=작물, [6-9]=도구
    /// </summary>
    static void SetupInventoryComponent(SO_ItemData[] itemAssets)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[Phase1 Step5] Player를 찾을 수 없습니다!");
            return;
        }

        var inv = player.GetComponent<Inventory>();
        if (inv == null)
            inv = player.AddComponent<Inventory>();

        var so = new SerializedObject(inv);

        // 시작 아이템: 씨앗 3종 × 5개씩
        var startItemsProp = so.FindProperty("_startItems");
        startItemsProp.arraySize = 3;
        startItemsProp.GetArrayElementAtIndex(0).objectReferenceValue = itemAssets[0]; // 무 씨앗
        startItemsProp.GetArrayElementAtIndex(1).objectReferenceValue = itemAssets[1]; // 당근 씨앗
        startItemsProp.GetArrayElementAtIndex(2).objectReferenceValue = itemAssets[2]; // 감자 씨앗

        var startQtyProp = so.FindProperty("_startQuantities");
        startQtyProp.arraySize = 3;
        startQtyProp.GetArrayElementAtIndex(0).intValue = 5;
        startQtyProp.GetArrayElementAtIndex(1).intValue = 5;
        startQtyProp.GetArrayElementAtIndex(2).intValue = 5;

        // 수확물 매핑: FarmManager._crops 인덱스와 동일 순서
        var harvestProp = so.FindProperty("_harvestItems");
        harvestProp.arraySize = 3;
        harvestProp.GetArrayElementAtIndex(0).objectReferenceValue = itemAssets[3]; // 무
        harvestProp.GetArrayElementAtIndex(1).objectReferenceValue = itemAssets[4]; // 당근
        harvestProp.GetArrayElementAtIndex(2).objectReferenceValue = itemAssets[5]; // 감자

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[Phase1 Step5] Player에 Inventory 컴포넌트 설정 완료 (씨앗 3종 × 5개).");
    }

    // ─────────────────────────────────────────
    //  인벤토리 UI 생성
    // ─────────────────────────────────────────

    /// <summary>
    /// HUD Canvas에 인벤토리 UI 패널과 30개 슬롯을 생성한다.
    /// </summary>
    static void CreateInventoryUI()
    {
        // 기존 InventoryUI가 있으면 스킵
        if (Object.FindFirstObjectByType<InventoryUI>() != null)
        {
            Debug.Log("[Phase1 Step5] InventoryUI 이미 존재. 스킵.");
            return;
        }

        var hudCanvas = FindHUDCanvas();
        if (hudCanvas == null) return;

        int slotCount = 30;
        int cols = 6;
        float cellSize = 48f;
        float spacing = 4f;
        float padding = 10f;
        float titleHeight = 30f;

        float gridWidth = cols * cellSize + (cols - 1) * spacing + padding * 2;
        int rows = Mathf.CeilToInt((float)slotCount / cols);
        float gridHeight = rows * cellSize + (rows - 1) * spacing + padding * 2 + titleHeight;

        // ─── InventoryPanel (루트, 비활성) ───
        var panelObj = new GameObject("InventoryPanel");
        panelObj.transform.SetParent(hudCanvas.transform, false);

        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(gridWidth, gridHeight);

        var panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.92f);
        panelBg.raycastTarget = true;

        // ─── 타이틀 "가방" ───
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);

        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -4);
        titleRect.sizeDelta = new Vector2(0, titleHeight);

        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "가방";
        titleTmp.fontSize = 20;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.raycastTarget = false;

        // TMP 폰트 할당
        var tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KOREAN_TMP_FONT_PATH);
        if (tmpFont != null) titleTmp.font = tmpFont;

        // ─── SlotContainer (GridLayout) ───
        var containerObj = new GameObject("SlotContainer");
        containerObj.transform.SetParent(panelObj.transform, false);

        var containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 0f);
        containerRect.anchorMax = new Vector2(1f, 1f);
        containerRect.offsetMin = new Vector2(padding, padding);
        containerRect.offsetMax = new Vector2(-padding, -(titleHeight + padding));

        var grid = containerObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.spacing = new Vector2(spacing, spacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.childAlignment = TextAnchor.UpperLeft;

        // ─── 30개 슬롯 생성 ───
        var slotBgs = new Image[slotCount];
        var slotIcons = new Image[slotCount];
        var slotTexts = new TextMeshProUGUI[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            // 슬롯 배경
            var slotObj = new GameObject($"Slot_{i:D2}");
            slotObj.transform.SetParent(containerObj.transform, false);

            var slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(cellSize, cellSize);

            var slotBg = slotObj.AddComponent<Image>();
            slotBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            slotBg.raycastTarget = false;

            slotBgs[i] = slotBg;

            // 아이콘 이미지
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);

            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.15f);
            iconRect.anchorMax = new Vector2(0.9f, 0.95f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            var iconImg = iconObj.AddComponent<Image>();
            iconImg.color = Color.clear; // 초기: 투명
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;

            slotIcons[i] = iconImg;

            // 수량 텍스트
            var qtyObj = new GameObject("Quantity");
            qtyObj.transform.SetParent(slotObj.transform, false);

            var qtyRect = qtyObj.AddComponent<RectTransform>();
            qtyRect.anchorMin = new Vector2(1f, 0f);
            qtyRect.anchorMax = new Vector2(1f, 0f);
            qtyRect.pivot = new Vector2(1f, 0f);
            qtyRect.anchoredPosition = new Vector2(-2, 2);
            qtyRect.sizeDelta = new Vector2(30, 16);

            var qtyTmp = qtyObj.AddComponent<TextMeshProUGUI>();
            qtyTmp.text = "";
            qtyTmp.fontSize = 12;
            qtyTmp.color = Color.white;
            qtyTmp.alignment = TextAlignmentOptions.BottomRight;
            qtyTmp.raycastTarget = false;
            if (tmpFont != null) qtyTmp.font = tmpFont;

            slotTexts[i] = qtyTmp;
        }

        // ─── InventoryUI 컴포넌트를 Canvas에 부착 (항상 활성) ───
        // panelObj는 비활성이므로, 컴포넌트는 Canvas에 붙여야 OnEnable/InputAction이 동작
        var uiComp = hudCanvas.gameObject.AddComponent<InventoryUI>();
        var uiSO = new SerializedObject(uiComp);

        uiSO.FindProperty("_panel").objectReferenceValue = panelObj;
        uiSO.FindProperty("_slotContainer").objectReferenceValue = containerObj.transform;

        // 배열 할당
        var bgsProp = uiSO.FindProperty("_slotBackgrounds");
        bgsProp.arraySize = slotCount;
        for (int i = 0; i < slotCount; i++)
            bgsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotBgs[i];

        var iconsProp = uiSO.FindProperty("_slotIcons");
        iconsProp.arraySize = slotCount;
        for (int i = 0; i < slotCount; i++)
            iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotIcons[i];

        var textsProp = uiSO.FindProperty("_slotQuantityTexts");
        textsProp.arraySize = slotCount;
        for (int i = 0; i < slotCount; i++)
            textsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotTexts[i];

        uiSO.ApplyModifiedPropertiesWithoutUndo();

        // 패널 비활성 (InventoryUI.OnEnable은 Canvas에서 동작, 패널만 토글)
        panelObj.SetActive(false);

        Debug.Log($"[Phase1 Step5] InventoryUI 생성 완료 ({slotCount}슬롯, {cols}열 × {rows}행).");
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
