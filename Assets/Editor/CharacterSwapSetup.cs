using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// PixelLab 생성 캐릭터 스프라이트를 임포트하고 PlayerController에 할당하는 에디터 스크립트.
/// 8방향 Idle + 8방향 Walk(6프레임) 지원.
/// </summary>
public static class CharacterSwapSetup
{
    private const string SPRITE_DIR = "Assets/Art/Sprites/Player";
    private const int PPU = 32; // 64px 캐릭터가 2유닛(2타일) 크기

    // 8방향 이름 (PlayerController 순서: S, SW, W, NW, N, NE, E, SE)
    private static readonly string[] DIR_NAMES =
        { "south", "south-west", "west", "north-west", "north", "north-east", "east", "south-east" };

    private const int WALK_FRAMES = 6;

    [MenuItem("DuskPioneer/캐릭터 변경 - PixelLab 스프라이트 적용")]
    static void Run()
    {
        EditorUtility.DisplayProgressBar("캐릭터 변경", "스프라이트 임포트 설정 중...", 0.1f);
        FixImportSettings();

        EditorUtility.DisplayProgressBar("캐릭터 변경", "PlayerController에 스프라이트 할당 중...", 0.5f);
        AssignSpritesToPlayer();

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("캐릭터 변경 완료",
            "PixelLab 캐릭터 스프라이트가 적용되었습니다.\n" +
            "8방향 Idle + 8방향 Walk(6프레임)\n" +
            "PPU=32 (캐릭터 ≈ 2타일 크기)", "확인");
    }

    /// <summary>
    /// 모든 PNG 파일의 임포트 설정을 픽셀아트에 맞게 수정.
    /// </summary>
    static void FixImportSettings()
    {
        // Rotations (idle) 폴더
        string rotDir = SPRITE_DIR + "/rotations";
        foreach (var dirName in DIR_NAMES)
        {
            string path = $"{rotDir}/{dirName}.png";
            FixSingleSprite(path);
        }

        // Walk 애니메이션 폴더
        for (int d = 0; d < DIR_NAMES.Length; d++)
        {
            for (int f = 0; f < WALK_FRAMES; f++)
            {
                string path = $"{SPRITE_DIR}/animations/walk/{DIR_NAMES[d]}/frame_{f:D3}.png";
                FixSingleSprite(path);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("[CharSwap] 모든 스프라이트 임포트 설정 완료 (PPU=32, Point, No Compression)");
    }

    static void FixSingleSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[CharSwap] 파일 없음: {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PPU;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    /// <summary>
    /// Scene에서 PlayerController를 찾아 새 스프라이트를 할당.
    /// </summary>
    static void AssignSpritesToPlayer()
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("[CharSwap] Scene에서 PlayerController를 찾을 수 없습니다!");
            return;
        }

        // SerializedObject로 HideInInspector 필드에 접근
        var so = new SerializedObject(player);

        // ── Idle 스프라이트 (8개) ──
        var idleProp = so.FindProperty("idleSprites");
        idleProp.arraySize = PlayerController.DIR_COUNT;
        for (int d = 0; d < DIR_NAMES.Length; d++)
        {
            string path = $"{SPRITE_DIR}/rotations/{DIR_NAMES[d]}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[CharSwap] Idle 스프라이트 로드 실패: {path}");
            idleProp.GetArrayElementAtIndex(d).objectReferenceValue = sprite;
        }

        // ── Walk 스프라이트 (8방향 × 6프레임 = 48개, 1차원 배열) ──
        var walkProp = so.FindProperty("walkSprites");
        int totalFrames = DIR_NAMES.Length * WALK_FRAMES;
        walkProp.arraySize = totalFrames;
        for (int d = 0; d < DIR_NAMES.Length; d++)
        {
            for (int f = 0; f < WALK_FRAMES; f++)
            {
                string path = $"{SPRITE_DIR}/animations/walk/{DIR_NAMES[d]}/frame_{f:D3}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                    Debug.LogWarning($"[CharSwap] Walk 스프라이트 로드 실패: {path}");
                int idx = d * WALK_FRAMES + f;
                walkProp.GetArrayElementAtIndex(idx).objectReferenceValue = sprite;
            }
        }

        // ── Walk 프레임 수 ──
        so.FindProperty("walkFramesPerDir").intValue = WALK_FRAMES;

        so.ApplyModifiedProperties();

        // SpriteRenderer 초기 스프라이트도 south idle로 설정
        var sr = player.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            string southPath = $"{SPRITE_DIR}/rotations/south.png";
            var southSprite = AssetDatabase.LoadAssetAtPath<Sprite>(southPath);
            if (southSprite != null)
                sr.sprite = southSprite;
        }

        // ToolController는 8방향 전환 후 attack 스프라이트를 사용하지 않음

        // Scene을 dirty로 마크해서 저장 가능하게
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[CharSwap] PlayerController에 스프라이트 할당 완료: Idle {DIR_NAMES.Length}개, Walk {totalFrames}개");
    }

    static void ClearArrayProp(SerializedObject so, string propName)
    {
        var prop = so.FindProperty(propName);
        if (prop != null)
            prop.arraySize = 0;
    }
}
