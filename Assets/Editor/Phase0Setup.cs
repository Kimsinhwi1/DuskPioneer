using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using System.Linq;

/// <summary>
/// Phase 0 전체 셋업: 스프라이트 임포트 → 애니메이션 생성 → Farm 씬 구성.
/// 메뉴: DuskPioneer > Phase 0 - Full Setup
/// </summary>
public static class Phase0Setup
{
    const string NINJA_BASE = "Assets/Art/AssetPacks/NinjaAdventure/Ninja Adventure - Asset Pack";
    const string PRINCESS_PATH = NINJA_BASE + "/Actor/Characters/Princess";
    const string TILESET_PATH = NINJA_BASE + "/Backgrounds/Tilesets";
    const int TILE_SIZE = 16;

    // ─────────────────────────────────────────
    //  메인 메뉴
    // ─────────────────────────────────────────

    [MenuItem("DuskPioneer/Phase 0 - Full Setup")]
    static void RunFullSetup()
    {
        EditorUtility.DisplayProgressBar("Phase 0", "Step 1/4: 스프라이트 Import 설정 수정...", 0.1f);
        FixAllSpriteImports();

        EditorUtility.DisplayProgressBar("Phase 0", "Step 2/4: 타일 에셋 생성...", 0.35f);
        CreateTileAssets();

        EditorUtility.DisplayProgressBar("Phase 0", "Step 3/4: Farm 씬 생성...", 0.55f);
        SetupFarmScene();

        EditorUtility.ClearProgressBar();

        Debug.Log("[Phase0] Setup 완료! Farm 씬이 저장되었습니다. Play 버튼으로 테스트하세요.");
    }

    // ─────────────────────────────────────────
    //  Step 1: 스프라이트 Import 설정
    // ─────────────────────────────────────────

    static void FixAllSpriteImports()
    {
        // Princess 캐릭터 스프라이트 (16x16 그리드 슬라이싱)
        SliceSpriteGrid(PRINCESS_PATH + "/SpriteSheet.png", 4, 7);
        SliceSpriteGrid(PRINCESS_PATH + "/SeparateAnim/Walk.png", 4, 4);
        SliceSpriteGrid(PRINCESS_PATH + "/SeparateAnim/Idle.png", 4, 1);
        SliceSpriteGrid(PRINCESS_PATH + "/SeparateAnim/Attack.png", 4, 4);

        // 타일셋 (자동 16x16 그리드)
        SliceTileset(TILESET_PATH + "/TilesetField.png");
        SliceTileset(TILESET_PATH + "/TilesetFloor.png");
        SliceTileset(TILESET_PATH + "/TilesetWater.png");
        SliceTileset(TILESET_PATH + "/TilesetNature.png");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Phase0] 스프라이트 Import 설정 완료.");
    }

    /// <summary>
    /// 스프라이트를 지정된 cols×rows 그리드로 슬라이싱 + PPU/Filter/Compression 수정
    /// </summary>
    static void SliceSpriteGrid(string path, int cols, int rows)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning($"[Phase0] 파일 없음: {path}"); return; }

        // 기본 설정
        ti.spritePixelsPerUnit = TILE_SIZE;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.spriteImportMode = SpriteImportMode.Multiple;

        // 텍스처 로드하여 실제 크기 확인
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int tileW = tex.width / cols;
        int tileH = tex.height / rows;

        // 그리드 슬라이싱 (좌상단부터 순서대로 번호 부여)
        string baseName = Path.GetFileNameWithoutExtension(path);
        var metas = new SpriteMetaData[cols * rows];
        int index = 0;

        for (int row = 0; row < rows; row++)
        {
            int y = tex.height - (row + 1) * tileH; // 위에서 아래로
            for (int col = 0; col < cols; col++)
            {
                metas[index] = new SpriteMetaData
                {
                    name = $"{baseName}_{index:D2}",
                    rect = new Rect(col * tileW, y, tileW, tileH),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center
                };
                index++;
            }
        }

        // 이전 슬라이싱의 잔여 ID 매핑 정리 (Walk_0, Idle_0 등 이전 이름 충돌 방지)
        var so = new SerializedObject(ti);
        var idTable = so.FindProperty("m_InternalIDToNameTable");
        if (idTable != null && idTable.isArray)
        {
            idTable.ClearArray();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        ti.spritesheet = metas;
        ti.SaveAndReimport();
    }

    /// <summary>
    /// 타일셋 텍스처를 자동으로 16x16 그리드로 슬라이싱
    /// </summary>
    static void SliceTileset(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning($"[Phase0] 파일 없음: {path}"); return; }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int cols = tex.width / TILE_SIZE;
        int rows = tex.height / TILE_SIZE;

        if (cols == 0 || rows == 0) { Debug.LogWarning($"[Phase0] 텍스처가 16x16보다 작음: {path}"); return; }

        SliceSpriteGrid(path, cols, rows);
    }

    // ─────────────────────────────────────────
    //  Step 2: 애니메이션 클립 + Animator Controller
    // ─────────────────────────────────────────

    // (Animator 제거됨 — PlayerController가 직접 스프라이트 제어)

    /// <summary>
    /// 스프라이트를 숫자 순서로 정렬하여 로드.
    /// 이전 슬라이싱의 잔여 스프라이트(Walk_0 등)를 제외하고 2자리 번호만 로드.
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

    // ─────────────────────────────────────────
    //  Step 3: 타일 에셋 생성
    // ─────────────────────────────────────────

    static void CreateTileAssets()
    {
        string tileDir = "Assets/Tilemaps/Tiles";
        Directory.CreateDirectory(Application.dataPath + "/Tilemaps/Tiles");

        // TilesetField에서 잔디/흙 타일 추출
        var fieldSprites = LoadSpritesOrdered(TILESET_PATH + "/TilesetField.png");
        if (fieldSprites.Length > 7)
        {
            // index 7 = 녹색 잔디 중앙 타일 (row2, col1)
            CreateTile(tileDir + "/Grass.asset", fieldSprites[7]);
            // index 1 = 주황/흙 중앙 타일 (row0, col1)
            CreateTile(tileDir + "/Dirt.asset", fieldSprites[1]);
        }
        else if (fieldSprites.Length > 0)
        {
            CreateTile(tileDir + "/Grass.asset", fieldSprites[0]);
            if (fieldSprites.Length > 1) CreateTile(tileDir + "/Dirt.asset", fieldSprites[1]);
        }

        // TilesetWater에서 물 타일 추출
        var waterSprites = LoadSpritesOrdered(TILESET_PATH + "/TilesetWater.png");
        if (waterSprites.Length > 5)
        {
            // index 5 = 푸른 물 중앙 타일
            CreateTile(tileDir + "/Water.asset", waterSprites[5]);
        }
        else if (waterSprites.Length > 0)
        {
            CreateTile(tileDir + "/Water.asset", waterSprites[0]);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Phase0] 타일 에셋 생성 완료.");
    }

    static void CreateTile(string path, Sprite sprite)
    {
        if (sprite == null) return;
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        AssetDatabase.CreateAsset(tile, path);
    }

    // ─────────────────────────────────────────
    //  Step 4: Farm 씬 구성
    // ─────────────────────────────────────────

    static void SetupFarmScene()
    {
        // 새 씬 생성
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
            UnityEditor.SceneManagement.NewSceneMode.Single);

        // 메인 카메라 설정
        var mainCam = Camera.main;
        mainCam.orthographic = true;
        mainCam.orthographicSize = 5;
        mainCam.backgroundColor = new Color(0.15f, 0.22f, 0.1f);

        // Grid + Tilemap 레이어 생성
        var gridObj = new GameObject("Grid");
        var grid = gridObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(1, 1, 0);

        // Ground 타일맵
        var groundObj = CreateTilemapLayer(gridObj.transform, "Ground", 0);
        var groundTilemap = groundObj.GetComponent<Tilemap>();

        // Water 타일맵 (충돌 포함)
        var waterObj = CreateTilemapLayer(gridObj.transform, "Water", 1);
        var waterTilemap = waterObj.GetComponent<Tilemap>();
        waterObj.AddComponent<TilemapCollider2D>();

        // 타일맵 페인팅
        PaintFarmLayout(groundTilemap, waterTilemap);

        // 플레이어 생성
        var player = CreatePlayerObject();

        // 카메라 따라가기 설정
        SetupCamera(player.transform);

        // 디버그 오버레이 (회전 문제 진단용)
        Camera.main.gameObject.AddComponent<RotationDebugOverlay>();

        // 씬 저장
        string scenePath = "Assets/Scenes/Farm.unity";
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[Phase0] Farm 씬 저장: " + scenePath);
    }

    static GameObject CreateTilemapLayer(Transform parent, string name, int sortOrder)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.AddComponent<Tilemap>();
        var renderer = obj.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortOrder;
        return obj;
    }

    /// <summary>
    /// 기본 농장 레이아웃 페인팅 (30×20 잔디 + 중앙 흙 + 왼쪽 물)
    /// </summary>
    static void PaintFarmLayout(Tilemap ground, Tilemap water)
    {
        var grassTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/Tiles/Grass.asset");
        var dirtTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/Tiles/Dirt.asset");
        var waterTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/Tiles/Water.asset");

        if (grassTile == null)
        {
            Debug.LogWarning("[Phase0] Grass 타일 없음. 타일맵 비어 있습니다.");
            return;
        }

        // 잔디 바닥 (30×20)
        for (int x = -15; x < 15; x++)
        {
            for (int y = -10; y < 10; y++)
            {
                ground.SetTile(new Vector3Int(x, y, 0), grassTile);
            }
        }

        // 흙 밭 (중앙 6×4 영역)
        if (dirtTile != null)
        {
            for (int x = -3; x < 3; x++)
            {
                for (int y = -2; y < 2; y++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), dirtTile);
                }
            }
        }

        // 물 (왼쪽 경계 2칸)
        if (waterTile != null)
        {
            for (int y = -10; y < 10; y++)
            {
                water.SetTile(new Vector3Int(-16, y, 0), waterTile);
                water.SetTile(new Vector3Int(-17, y, 0), waterTile);
            }
        }
    }

    /// <summary>
    /// Player GameObject 생성 (SpriteRenderer + Rigidbody2D + Collider + PlayerController)
    /// Animator 없이 PlayerController가 직접 스프라이트를 제어.
    /// </summary>
    static GameObject CreatePlayerObject()
    {
        // 스프라이트 로드
        var walkSprites = LoadSpritesOrdered(PRINCESS_PATH + "/SeparateAnim/Walk.png");
        var idleSprites = LoadSpritesOrdered(PRINCESS_PATH + "/SeparateAnim/Idle.png");

        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = Vector3.zero;

        // SpriteRenderer
        var sr = player.AddComponent<SpriteRenderer>();
        if (idleSprites.Length > 0) sr.sprite = idleSprites[0];
        sr.sortingOrder = 10;

        // Rigidbody2D (탑다운이라 중력 없음)
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // BoxCollider2D (하반신 충돌)
        var col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.5f);
        col.offset = new Vector2(0, -0.25f);

        // PlayerController — 스프라이트는 CharacterSwapSetup에서 할당
        var pc = player.AddComponent<PlayerController>();

        // 기본 Idle: Ninja Adventure 4방향 → 8방향 중 S/W/N/E만 할당 (임시)
        if (idleSprites.Length >= 4)
        {
            pc.idleSprites = new Sprite[PlayerController.DIR_COUNT];
            pc.idleSprites[PlayerController.DIR_S]  = idleSprites[0]; // Down
            pc.idleSprites[PlayerController.DIR_N]  = idleSprites[1]; // Up
            pc.idleSprites[PlayerController.DIR_W]  = idleSprites[2]; // Left
            pc.idleSprites[PlayerController.DIR_E]  = idleSprites[3]; // Right
            // 대각선은 가까운 방향 복사
            pc.idleSprites[PlayerController.DIR_SW] = idleSprites[2];
            pc.idleSprites[PlayerController.DIR_NW] = idleSprites[2];
            pc.idleSprites[PlayerController.DIR_NE] = idleSprites[3];
            pc.idleSprites[PlayerController.DIR_SE] = idleSprites[3];
        }

        // Walk: 4방향 4프레임 → 8방향 배열로 변환 (대각선은 가까운 방향 복사)
        if (walkSprites.Length >= 16)
        {
            Sprite[] down  = { walkSprites[0], walkSprites[4], walkSprites[8],  walkSprites[12] };
            Sprite[] up    = { walkSprites[1], walkSprites[5], walkSprites[9],  walkSprites[13] };
            Sprite[] left  = { walkSprites[2], walkSprites[6], walkSprites[10], walkSprites[14] };
            Sprite[] right = { walkSprites[3], walkSprites[7], walkSprites[11], walkSprites[15] };
            pc.walkFramesPerDir = 4;
            pc.walkSprites = new Sprite[PlayerController.DIR_COUNT * 4];
            System.Array.Copy(down,  0, pc.walkSprites, PlayerController.DIR_S  * 4, 4);
            System.Array.Copy(left,  0, pc.walkSprites, PlayerController.DIR_SW * 4, 4);
            System.Array.Copy(left,  0, pc.walkSprites, PlayerController.DIR_W  * 4, 4);
            System.Array.Copy(left,  0, pc.walkSprites, PlayerController.DIR_NW * 4, 4);
            System.Array.Copy(up,    0, pc.walkSprites, PlayerController.DIR_N  * 4, 4);
            System.Array.Copy(right, 0, pc.walkSprites, PlayerController.DIR_NE * 4, 4);
            System.Array.Copy(right, 0, pc.walkSprites, PlayerController.DIR_E  * 4, 4);
            System.Array.Copy(right, 0, pc.walkSprites, PlayerController.DIR_SE * 4, 4);
        }

        return player;
    }

    // ─────────────────────────────────────────
    //  카메라 설정 (Cinemachine 또는 CameraFollow)
    // ─────────────────────────────────────────

    static void SetupCamera(Transform playerTransform)
    {
        // 씬 내 Cinemachine 오브젝트가 있으면 제거 (카메라 회전 문제 방지)
        var cmType = System.Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
        if (cmType != null)
        {
            foreach (var cmObj in Object.FindObjectsByType(cmType, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate((cmObj as Component).gameObject);
            }
        }

        // CameraFollow 사용 (2D 탑다운에서 안정적)
        var cam = Camera.main;
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = playerTransform;
        follow.smoothSpeed = 5f;
        Debug.Log("[Phase0] CameraFollow 설정 완료.");
    }
}
