---
name: unity-scene-setup
description: Unity 씬 구성 시 MCP 대신 Editor 스크립트를 사용하는 규칙. Assets/Editor/에 에디터 스크립트를 만들어서 DuskPioneer 메뉴로 실행하는 패턴. 씬에 오브젝트 배치, UI Canvas 구성, SO 에셋 생성, 스프라이트 Import 변경 등 모든 씬 설정 작업에 이 스킬을 따른다.
---

# Unity 씬 구성 — Editor 스크립트 패턴

## 핵심 원칙

> **MCP(Model Context Protocol)를 통한 Unity 씬 조작은 절대 사용하지 않는다.**
> 모든 씬 구성은 `Assets/Editor/` 폴더에 에디터 스크립트를 만들어서
> `DuskPioneer > ...` 메뉴 항목으로 실행한다.

### 왜 Editor 스크립트인가?

1. **재현 가능** — 메뉴 클릭 한 번으로 동일 결과
2. **버전 관리** — 스크립트 파일이 Git에 포함됨
3. **의존성 명시** — Phase 0 → Phase 1 순서가 코드에 드러남
4. **디버깅 용이** — Console 로그로 전 과정 추적

## 에디터 스크립트 기본 템플릿

```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// [Phase/Step 설명]. 에디터 메뉴에서 실행.
/// </summary>
public static class PhaseXSetup
{
    // ── 에셋 경로 상수 ──
    private const string NINJA_BASE = "Assets/Art/AssetPacks/NinjaAdventure/Ninja Adventure - Asset Pack";
    private const string SO_DIR = "Assets/ScriptableObjects/SubDir";

    [MenuItem("DuskPioneer/Phase X Step Y - 기능 이름")]
    static void RunSetup()
    {
        EditorUtility.DisplayProgressBar("Phase X", "1/N: 작업 설명...", 0.2f);
        DoStep1();

        EditorUtility.DisplayProgressBar("Phase X", "2/N: 작업 설명...", 0.6f);
        DoStep2();

        EditorUtility.ClearProgressBar();

        // 씬 저장
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[PhaseX] 셋업 완료!");
    }

    // ── 각 단계 메서드 ──

    static void DoStep1() { /* ... */ }
    static void DoStep2() { /* ... */ }

    // ── 유틸리티 ──

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
```

## 스프라이트 Import 설정 변경

```csharp
static void FixSpriteImport(string path, int ppu = 16)
{
    var ti = AssetImporter.GetAtPath(path) as TextureImporter;
    if (ti == null) { Debug.LogWarning($"파일 없음: {path}"); return; }

    ti.textureType = TextureImporterType.Sprite;
    ti.spritePixelsPerUnit = ppu;
    ti.filterMode = FilterMode.Point;           // 픽셀아트 필수
    ti.textureCompression = TextureImporterCompression.Uncompressed;
    ti.SaveAndReimport();
}
```

## 스프라이트 그리드 슬라이싱

```csharp
/// <summary>
/// 스프라이트를 cols x rows 그리드로 슬라이싱한다.
/// 이름 형식: {파일명}_{번호:D2} (예: Walk_00, Walk_01...)
/// </summary>
static void SliceSpriteGrid(string path, int cols, int rows, int cellSize = 16)
{
    var ti = AssetImporter.GetAtPath(path) as TextureImporter;
    if (ti == null) return;

    ti.textureType = TextureImporterType.Sprite;
    ti.spriteImportMode = SpriteImportMode.Multiple;
    ti.spritePixelsPerUnit = cellSize;
    ti.filterMode = FilterMode.Point;
    ti.textureCompression = TextureImporterCompression.Uncompressed;

    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    string baseName = Path.GetFileNameWithoutExtension(path);
    float cellW = tex.width / (float)cols;
    float cellH = tex.height / (float)rows;

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
}
```

## 열 기반 스프라이트 추출 (Ninja Adventure 전용)

Ninja Adventure 스프라이트시트는 **열(column) 기반**이다.
행(row)이 아니라 열이 같은 방향을 의미한다.

```
4x4 그리드에서:
  열0=Down, 열1=Up, 열2=Left, 열3=Right

인덱스 매핑 (row-major 슬라이싱 후):
  Down  = [0, 4, 8, 12]   (각 행의 0번째)
  Up    = [1, 5, 9, 13]   (각 행의 1번째)
  Left  = [2, 6, 10, 14]  (각 행의 2번째)
  Right = [3, 7, 11, 15]  (각 행의 3번째)
```

```csharp
// 열 기반 방향별 스프라이트 추출
Sprite[] down  = { sprites[0], sprites[4], sprites[8],  sprites[12] };
Sprite[] up    = { sprites[1], sprites[5], sprites[9],  sprites[13] };
Sprite[] left  = { sprites[2], sprites[6], sprites[10], sprites[14] };
Sprite[] right = { sprites[3], sprites[7], sprites[11], sprites[15] };
```

## 슬라이싱된 스프라이트 안전하게 로드

```csharp
/// <summary>
/// 2자리 번호 패턴만 필터링하여 유령 스프라이트를 제외한다.
/// Unity는 재슬라이싱 후에도 이전 이름의 스프라이트를 반환할 수 있음.
/// </summary>
static Sprite[] LoadSpritesOrdered(string path)
{
    string baseName = Path.GetFileNameWithoutExtension(path);
    string pattern = $@"^{Regex.Escape(baseName)}_\d{{2}}$";

    return AssetDatabase.LoadAllAssetsAtPath(path)
        .OfType<Sprite>()
        .Where(s => Regex.IsMatch(s.name, pattern))
        .OrderBy(s =>
        {
            var m = Regex.Match(s.name, @"(\d+)$");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        })
        .ToArray();
}
```

## SerializedObject로 private 필드 할당

인스펙터에서 `[SerializeField] private`로 선언된 필드는
에디터 스크립트에서 `SerializedObject`를 통해 할당한다.

```csharp
var component = go.AddComponent<MyComponent>();
var so = new SerializedObject(component);
so.FindProperty("_myField").objectReferenceValue = someAsset;
so.FindProperty("_myFloat").floatValue = 3.14f;
so.FindProperty("_myBool").boolValue = true;

// 배열 할당
var arrayProp = so.FindProperty("_myArray");
arrayProp.arraySize = items.Length;
for (int i = 0; i < items.Length; i++)
    arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];

so.ApplyModifiedPropertiesWithoutUndo();
```

## Canvas / UI 생성 패턴

```csharp
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

// UI 요소 생성 (RectTransform 앵커 설정)
var panelObj = new GameObject("Panel");
panelObj.transform.SetParent(canvasObj.transform, false);
var rect = panelObj.AddComponent<RectTransform>();
rect.anchorMin = new Vector2(0.5f, 1f);    // 앵커
rect.anchorMax = new Vector2(0.5f, 1f);
rect.pivot = new Vector2(0.5f, 1f);        // 피벗
rect.anchoredPosition = new Vector2(0, -10); // 위치
rect.sizeDelta = new Vector2(400, 50);      // 크기

// Image 컴포넌트
var image = panelObj.AddComponent<Image>();
image.color = new Color(0, 0, 0, 0.4f);
image.raycastTarget = false;  // 입력 차단 방지

// TextMeshProUGUI
var textObj = new GameObject("Text");
textObj.transform.SetParent(panelObj.transform, false);
var tmp = textObj.AddComponent<TextMeshProUGUI>();
tmp.text = "텍스트";
tmp.fontSize = 24;
tmp.alignment = TextAlignmentOptions.Center;
tmp.raycastTarget = false;
```

## ScriptableObject 에셋 자동 생성

```csharp
static SO_MyData CreateSOAsset(string dir, string fileName)
{
    string path = $"{dir}/{fileName}.asset";

    // 중복 방지: 이미 존재하면 로드
    var existing = AssetDatabase.LoadAssetAtPath<SO_MyData>(path);
    if (existing != null) return existing;

    EnsureDirectory(dir);

    var so = ScriptableObject.CreateInstance<SO_MyData>();
    so.someField = "기본값";
    so.someValue = 10f;

    AssetDatabase.CreateAsset(so, path);
    AssetDatabase.SaveAssets();
    return so;
}
```

## 체크리스트: 새 에디터 스크립트 작성 시

- [ ] `Assets/Editor/` 폴더에 위치하는가?
- [ ] `using UnityEditor;` 포함하는가?
- [ ] `[MenuItem("DuskPioneer/...")]` 메뉴 항목이 있는가?
- [ ] 이미 존재하는 오브젝트/에셋 체크 (중복 생성 방지)?
- [ ] `EditorUtility.DisplayProgressBar` / `ClearProgressBar` 사용?
- [ ] 완료 후 `EditorSceneManager.SaveOpenScenes()` 호출?
- [ ] Debug.Log로 진행 상태 출력?
- [ ] `AssetDatabase.SaveAssets()` 호출로 에셋 저장?
