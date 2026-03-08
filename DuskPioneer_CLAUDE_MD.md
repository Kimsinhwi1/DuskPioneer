# DuskPioneer — Claude Code 작업 설계문서
> 이 문서는 Claude Code가 DuskPioneer 프로젝트 작업 시 참고하는 마스터 문서입니다.
> Unity 프로젝트 루트에 CLAUDE.md로 배치하세요.
> 최종 업데이트: 2026-03-08

---

## 1. 프로젝트 개요

| 항목 | 값 |
|------|-----|
| 프로젝트명 | 황혼의 개척자 (Dusk Pioneer) |
| 장르 | 2D 픽셀아트 액션 RPG + 농장 시뮬레이션 |
| 엔진 | Unity 6.3 LTS (6000.3.10f1) |
| 플랫폼 | PC (Steam) |
| 시점 | 2D 탑다운 뷰 |
| 해상도 | 16x16 또는 32x32 타일 기반 |
| 타겟 해상도 | 1920x1080 (16:9) |
| 개발자 | 솔로 (Sin Hwi) |
| 개발 방식 | 바이브 코딩 (Claude Code + Coplay MCP) |
| Unity 경험 | 초보 (학습 병행 개발) |

---

## 2. Unity 프로젝트 폴더 구조

```
DuskPioneer/
├── Assets/
│   ├── Animations/          # 애니메이션 컨트롤러 + 클립
│   │   ├── Player/
│   │   ├── Monsters/
│   │   └── NPC/
│   ├── Art/                 # 모든 비주얼 에셋
│   │   ├── Sprites/         # 스프라이트시트
│   │   │   ├── Player/
│   │   │   ├── Monsters/
│   │   │   ├── NPC/
│   │   │   ├── Items/
│   │   │   └── Effects/
│   │   ├── Tiles/           # 타일맵용 타일셋
│   │   │   ├── Farm/        # 농장 타일 (흙, 잔디, 물, 작물)
│   │   │   ├── Village/     # 마을 타일 (건물, 길, 장식)
│   │   │   └── Dungeon/     # 던전 타일 (바닥, 벽, 함정)
│   │   └── UI/              # UI 스프라이트
│   ├── Audio/
│   │   ├── BGM/             # 배경 음악
│   │   └── SFX/             # 효과음
│   ├── Prefabs/
│   │   ├── Player/
│   │   ├── Monsters/
│   │   ├── Items/
│   │   ├── Buildings/
│   │   └── UI/
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   ├── Farm.unity       # 메인 농장 씬
│   │   ├── Village.unity    # 마을 씬
│   │   └── Dungeon.unity    # 던전 씬
│   ├── Scripts/
│   │   ├── Core/            # GameManager, TimeManager, SaveManager, InputManager
│   │   ├── Player/          # 이동, 전투, 스탯, 인벤토리, 스킬
│   │   ├── Farm/            # 작물, 도구, 건물, 동물, 계절
│   │   ├── Dungeon/         # 방 생성, 몬스터 AI, 보스, 드롭
│   │   ├── NPC/             # 대화, 호감도, 퀘스트, 상점
│   │   ├── Spirit/          # 정령 계약, 공명, 각성
│   │   ├── Crafting/        # 레시피, 제작대, 재료 조합
│   │   ├── UI/              # HUD, 메뉴, 인벤토리UI, 대화창
│   │   └── Data/            # ScriptableObject 정의, 세이브/로드
│   ├── ScriptableObjects/
│   │   ├── Items/           # 아이템 데이터
│   │   ├── Monsters/        # 몬스터 데이터
│   │   ├── Spirits/         # 정령 데이터
│   │   ├── Crops/           # 작물 데이터
│   │   ├── Recipes/         # 크래프팅 레시피
│   │   └── Quests/          # 퀘스트 데이터
│   ├── Tilemaps/            # 타일맵 팔레트
│   └── Materials/           # 머티리얼 (셰이더)
├── Packages/                # Unity 패키지
├── ProjectSettings/
├── CLAUDE.md                # ← 이 문서
└── .gitignore
```

---

## 3. 필요한 에셋 목록 + 다운로드 위치

### 3.1 Phase 0 — 당장 필요한 에셋 (프로토타입용)

**목표: "캐릭터가 농장 맵을 걸어다니는 30초"를 만들기 위한 최소 에셋**

#### 캐릭터 스프라이트
| 에셋명 | 내용 | 가격 | 다운로드 |
|--------|------|------|----------|
| Ninja Adventure Asset Pack | 캐릭터 + 타일셋 + 아이템 + 이펙트 올인원. CC0 라이센스(상업 이용 가능). 탑다운 16x16 | 무료 (기부형) | https://pixel-boy.itch.io/ninja-adventure-asset-pack |
| Pixel Art Top Down - Basic (Cainos) | 32x32 기본 캐릭터 + 타일셋. 심플하고 깔끔 | 무료 | https://cainos.itch.io/pixel-art-top-down-basic |
| Mystic Woods (Game Endeavor) | 16x16 캐릭터 + 슬라임 적 + 숲 타일셋. 귀여운 스타일 | 무료 | https://game-endeavor.itch.io/mystic-woods |

> **추천: Ninja Adventure Asset Pack을 메인으로 사용**
> 이유: 캐릭터, 타일셋, 아이템, 이펙트, 사운드까지 올인원. 탑다운 RPG에 최적화. CC0 라이센스라 상업용 OK. 향후 자체 에셋으로 교체할 때까지 충분.

#### 농장 타일셋 (보충)
| 에셋명 | 내용 | 가격 | 다운로드 |
|--------|------|------|----------|
| Sprout Lands (Cup Nooble) | 16x16 농장 특화 타일셋 + 작물 + 도구 | 무료 | https://cupnooble.itch.io/sprout-lands-asset-pack |
| Sunnyside World (danieldiggle) | 농장+마을+던전 타일셋 세트 | 무료 | https://danieldiggle.itch.io/sunnyside-world |

#### 던전 타일셋
| 에셋명 | 내용 | 가격 | 다운로드 |
|--------|------|------|----------|
| 2D Pixel Dungeon Asset Pack (Pixel_Poem) | 16x16 던전 타일 + 적 + 아이템 | 무료 | https://pixel-poem.itch.io/dungeon-assetpuck |
| Anokolisa Topdown Tileset | 500 스프라이트 + 3 히어로 + 8 적 + 50 무기 | 무료 | https://anokolisa.itch.io/dungeon-crawler-pixel-art-asset-pack |

#### UI 에셋
| 에셋명 | 내용 | 가격 | 다운로드 |
|--------|------|------|----------|
| Ninja Adventure (포함) | 기본 UI 아이콘 포함 | 무료 | (위와 동일) |
| Pixel Art UI (Kenney) | 범용 UI 키트 | 무료 | https://kenney.nl/assets/ui-pack-pixel-adventure |

#### 사운드/음악
| 에셋명 | 내용 | 가격 | 다운로드 |
|--------|------|------|----------|
| Ninja Adventure (포함) | BGM + SFX 포함 | 무료 | (위와 동일) |
| FREE Music Loop Bundle (Tallbeard Studios) | 150+ BGM 루프 | 무료 | https://tallbeard.itch.io/music-loop-bundle |
| Leviathan Music (OpenGameArt) | 판타지 RPG BGM | 무료 (CC) | https://opengameart.org |

### 3.2 에셋 다운로드 후 배치 규칙

```
다운로드한 에셋 → Assets/Art/ 아래 적절한 폴더에 배치

예시:
ninja-adventure-asset-pack/
  ├── Actor/ → Assets/Art/Sprites/Player/, Assets/Art/Sprites/NPC/
  ├── Backgrounds/ → Assets/Art/Tiles/Farm/, Assets/Art/Tiles/Dungeon/
  ├── Items/ → Assets/Art/Sprites/Items/
  ├── FX/ → Assets/Art/Sprites/Effects/
  └── Music+Sounds/ → Assets/Audio/BGM/, Assets/Audio/SFX/
```

> **중요: 스프라이트 Import 설정**
> - Pixels Per Unit: 16 (16x16 에셋) 또는 32 (32x32 에셋) — 에셋에 맞춰 통일
> - Filter Mode: Point (no filter) — 픽셀아트 필수! Bilinear하면 흐려짐
> - Compression: None — 픽셀아트는 압축하면 깨짐
> - Sprite Mode: Multiple (스프라이트시트의 경우) → Sprite Editor에서 슬라이싱

---

## 4. Unity 패키지 설치 목록

### 4.1 Phase 0에서 필요한 패키지

Unity Package Manager (Window → Package Manager)에서 설치:

| 패키지 | 용도 | 설치 방법 |
|--------|------|-----------|
| 2D Tilemap Editor | 타일맵 생성/편집 | Built-in (기본 포함) |
| 2D Tilemap Extras | Rule Tile 등 고급 타일 | Package Manager → Unity Registry |
| Cinemachine | 카메라 따라가기 | Package Manager → Unity Registry |
| Input System | 키보드/게임패드 입력 | Package Manager → Unity Registry |
| TextMeshPro | UI 텍스트 렌더링 | Built-in (기본 포함) |

### 4.2 Phase 2 이후 추가 예정

| 패키지 | 용도 | 시점 |
|--------|------|------|
| 2D Animation | 스프라이트 본 애니메이션 | Phase 2 (전투) |
| 2D Pixel Perfect | 픽셀아트 렌더링 정확성 | Phase 1 |
| Yarn Spinner | NPC 대화 시스템 | Phase 3 (NPC) |
| Steamworks.NET | Steam 연동 | Phase 5 (출시) |

---

## 5. 핵심 기술 설정

### 5.1 프로젝트 설정 (Edit → Project Settings)

```
[Player]
- Resolution: 1920x1080
- Fullscreen Mode: Fullscreen Window

[Quality]
- Anti Aliasing: Disabled (픽셀아트에 안티앨리어싱 쓰면 흐려짐)
- Texture Quality: Full Res

[Editor]
- Enter Play Mode Settings: ✅ 활성화
  - Reload Domain: ❌ 비활성화 (MCP 연결 유지 + 플레이 모드 진입 빠르게)
  - Reload Scene: ✅ 활성화

[Physics 2D]
- Gravity Y: 0 (탑다운이라 중력 불필요)

[Tags & Layers]
- Sorting Layers: Background, Ground, Objects, Characters, Effects, UI
- Tags: Player, Monster, NPC, Item, Crop, Building, Interactable
- Layers: Default, Player, Monster, Ground, Obstacle, Interactable
```

### 5.2 카메라 설정

```
Main Camera:
- Projection: Orthographic
- Size: 8 (16x16 에셋 기준, 조정 가능)
- Background: 단색 (어두운 초록 또는 검정)

Cinemachine:
- Virtual Camera → Follow: Player
- Body: Framing Transposer
- Dead Zone: 0.1 (약간의 여유)
- Damping: 0.5 (부드러운 따라가기)
```

### 5.3 입력 설정 (Input System)

```
PlayerInputActions.inputactions:

[Movement]
- Move: WASD / Arrow Keys / Left Stick → Vector2
  
[Combat]
- Attack: Left Mouse / Z → Button
- ChargeAttack: Left Mouse Hold → Button
- Dodge: Space / Right Trigger → Button
- Parry: Right Mouse / X → Button
- Skill1: Q → Button
- Skill2: E → Button
- ResonanceBurst: R → Button

[Interaction]  
- Interact: F / A Button → Button
- Inventory: I / Tab → Button
- Menu: Escape / Start → Button

[QuickSlot]
- Slot1~5: 1~5 Number Keys → Button
```

---

## 6. Phase별 Claude Code 작업 목록

### Phase 0: 기반 구축 (2주)

**목표: 캐릭터가 농장 맵을 걸어다니는 화면**

```
작업 순서:
1. 폴더 구조 생성 (위 구조대로)
2. 에셋 임포트 + 스프라이트 설정 (Pixels Per Unit, Filter Mode)
3. 농장 타일맵 생성 (잔디, 흙, 물 기본 타일)
4. 플레이어 캐릭터 배치 + 4방향 이동 스크립트 (PlayerController.cs)
5. 4방향 걷기 애니메이션 연결 (Animator Controller)
6. Cinemachine 카메라 따라가기 설정
7. 충돌 처리 (물, 나무 등 못 지나가는 곳)
8. 기본 HUD (시간 표시, 체력바 — placeholder)
```

**핵심 스크립트 — PlayerController.cs 요구사항:**
```
- New Input System 사용
- Rigidbody2D.MovePosition으로 이동 (물리 기반)
- 이동 속도: public float 변수로 Inspector에서 조정 가능
- 8방향 이동 허용하되, 애니메이션은 4방향 (상하좌우)
- 마지막 이동 방향 기억 (idle 애니메이션용)
- 이동 시 먼지 파티클 (Phase 1에서 추가 가능, 일단 빈 자리)
```

### Phase 1: 농장 기초 (1~2개월)

```
작업 순서:
1. 시간 시스템 (TimeManager.cs) — 낮/밤 전환, 시간 흐름
2. 도구 시스템 — 삽, 물뿌리개, 괭이, 도끼 (ToolManager.cs)
3. 타일 상호작용 — 흙 파기, 물 주기 (FarmTile.cs)
4. 작물 시스템 — 씨앗 심기 → 성장 → 수확 (Crop.cs)
5. 인벤토리 시스템 (Inventory.cs + InventoryUI.cs)
6. 상점 시스템 — 판매/구매 (ShopManager.cs)
7. 세이브/로드 (SaveManager.cs — JSON 직렬화)
8. 낮/밤 비주얼 전환 (조명 변화)
```

### Phase 2: 전투 기초 (1~2개월)

```
작업 순서:
1. 전투 상태 전환 (무기 들기/농기구 들기)
2. 공격 시스템 — 콤보(3타), 차지 공격, 패링
3. 구르기/회피 (무적 프레임)
4. 몬스터 AI — 순찰, 추적, 공격 패턴 (MonsterAI.cs)
5. 히트박스/허트박스 시스템
6. 체력 시스템 (HP, 피격, 사망)
7. 던전 1~5층 수동 설계 (BSP는 Phase 3)
8. 드롭 시스템 (아이템/재료)
9. 첫 번째 보스 (거대 슬라임)
```

### Phase 3: 시스템 확장 (2~3개월)

```
작업 순서:
1. 정령 계약 시스템 (Spirit.cs, SpiritManager.cs)
2. 정령 슬롯 (주/부/대지) + 공명 효과
3. 크래프팅 시스템 (요리, 대장간, 연금술)
4. NPC 시스템 (대화, 호감도, 퀘스트) — Yarn Spinner 사용
5. BSP 랜덤 던전 생성 (DungeonGenerator.cs)
6. 계절 시스템 (4계절 + 시각 변화)
7. 정령 영향 재배 (대지 정령에 따라 수확물 변화)
8. 던전 11~30층 + 보스 2종 추가
```

---

## 7. 코딩 규칙

### 7.1 네이밍 컨벤션

```csharp
// 클래스: PascalCase
public class PlayerController : MonoBehaviour

// public 변수: camelCase (Inspector 노출)
public float moveSpeed = 5f;

// private 변수: _camelCase
private Rigidbody2D _rb;
private Vector2 _moveInput;

// 메서드: PascalCase
public void TakeDamage(int amount)

// 상수: UPPER_SNAKE
public const int MAX_INVENTORY_SIZE = 30;

// ScriptableObject: SO_ 접두사
[CreateAssetMenu(fileName = "SO_NewItem")]
public class SO_Item : ScriptableObject
```

### 7.2 아키텍처 원칙

```
- 싱글톤: GameManager, TimeManager, AudioManager만 사용. 남용 금지
- ScriptableObject: 모든 게임 데이터는 SO로 관리 (하드코딩 금지)
- 이벤트 기반: UnityEvent 또는 C# event로 시스템 간 통신
- 컴포넌트 분리: 하나의 스크립트가 하나의 역할만
- SerializeField: Inspector 노출이 필요한 private 변수에 사용
```

### 7.3 주석 규칙

```csharp
/// <summary>
/// 한국어로 작성. 메서드 목적을 한 줄로 설명.
/// </summary>
/// <param name="damage">받는 데미지량</param>
public void TakeDamage(int damage)
{
    // 인라인 주석도 한국어
    _currentHp -= damage;
}
```

---

## 8. MCP 사용 가이드

### 8.1 Coplay MCP로 할 수 있는 것

```
- GameObject 생성/삭제/수정
- Component 추가/설정
- Material 생성/적용
- Scene hierarchy 탐색
- Console 로그 확인
- 스크립트 컴파일 트리거
- Menu item 실행
```

### 8.2 MCP로 할 수 없는 것 (직접 해야 하는 것)

```
- 에셋 다운로드/임포트 (수동으로 에셋 스토어 or itch.io에서 받기)
- Sprite Editor에서 슬라이싱 (Unity GUI 필요)
- Tilemap 페인팅 (Unity GUI 필요)
- Play Mode 테스트 (Unity에서 직접 재생)
- 빌드 설정 및 실행
```

### 8.3 효율적인 Claude Code 워크플로우

```
1. 이 문서를 먼저 읽게 함: "CLAUDE.md를 읽고 프로젝트 상황을 파악해줘"
2. 작업 단위를 작게: "PlayerController.cs를 만들어줘" (O) / "게임 전체 만들어줘" (X)
3. 한 번에 하나의 스크립트: 작성 → Unity에서 확인 → 다음 스크립트
4. 에러 발생 시: Unity Console 에러 메시지를 Claude Code에 복사
5. 테스트 자주: 스크립트 3~4개 만들 때마다 Unity Play Mode로 테스트
```

---

## 9. 게임 디자인 핵심 참조

> 상세 기획은 별도 GDD 문서 참조. 여기에는 코딩 시 필요한 핵심만 요약.

### 9.1 정령 계보 구조

```
태초 (여명 + 황혼)
├── 여명 (Dawn) ☀️ — 정화, 축복, 안전
│   ├── 잿불 (Ember) 🔥 — 파괴, 제련, 근접 전투
│   └── 뿌리 (Root) 🌿 — 생명, 성장, 소환/치유
└── 황혼 (Dusk) 🌑 — 침식, 소멸, 리스크+리턴
    ├── 서리 (Frost) ❄️ — 시간, 보존, 원거리 마법
    └── 심연 (Void) 🌀 — 공간, 확률, 그림자
```

### 9.2 핵심 수치 참조

```
[플레이어]
- 기본 HP: 100
- 기본 이동속도: 5
- 레벨업 시 스탯 포인트: 5 (STR/DEX/INT/VIT/LUK에 자유 분배)
- 스태미나: 100 (도구 사용, 구르기에 소모)

[시간]
- 1일 = 현실 약 15분 (조정 가능)
- 06:00~18:00 = 낮, 18:00~06:00 = 밤
- 1계절 = 28일, 4계절 = 1년

[던전]
- 총 50층 + 무한 던전 (51층~)
- 5층마다 중간 세이브 포인트
- 보스: 10/20/30/40/50층

[작물]
- 일반: 3~5일 성장
- 고급: 7~10일
- 마법: 14~21일
- 전설: 계절 1회 수확

[정령]
- 슬롯: 주 정령(1) + 부 정령(20층 해금) + 대지 정령(30층 해금)
- 공명: 서로 다른 계보 조합 시 발동 (총 19가지)
```

### 9.3 무기 종류

```
검 — 3타 콤보, 밸런스형
대검 — 2타+범위, 고데미지 느림
쌍검 — 5타 연타, 크리티컬 특화
활 — 원거리 조준, 약점 적중 2배
지팡이 — 정령 스킬 쿨감, 마법 특화
낫 — HP 흡수, 리스크+리턴
농기구 — 전투+농사 겸용, 도구별 고유 효과
```

---

## 10. Git 설정

### .gitignore (Unity 표준)

```
# Unity
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emoryCaptures/
[Rr]ecordings/
Asset[Ss]tore[Tt]ools/

# VS / Rider
.vs/
.idea/
*.csproj
*.sln
*.suo
*.user
*.pidb
*.booproj
*.svd
*.pdb
*.mdb

# OS
.DS_Store
Thumbs.db

# Builds
*.apk
*.aab
*.exe
*.app
```

### Git LFS (.gitattributes)

```
# 대용량 바이너리 파일은 Git LFS로 추적
*.png filter=lfs diff=lfs merge=lfs -text
*.jpg filter=lfs diff=lfs merge=lfs -text
*.wav filter=lfs diff=lfs merge=lfs -text
*.mp3 filter=lfs diff=lfs merge=lfs -text
*.ogg filter=lfs diff=lfs merge=lfs -text
*.psd filter=lfs diff=lfs merge=lfs -text
*.fbx filter=lfs diff=lfs merge=lfs -text
```

---

## 11. 레퍼런스 게임

코드 구조나 기능 구현 시 참고할 게임:

| 게임 | 참고 포인트 |
|------|-------------|
| Stardew Valley | 농장 시스템, 시간/계절, NPC 관계, 전체 게임 루프 |
| Moonlighter | 던전+상점 선순환, 낮밤 구조, 인디 개발 스코프 |
| Hades | 액션 전투 감각, 스킬 조합, 로그라이크 던전 |
| Rune Factory | 농사+전투 혼합, 몬스터 교류 |
| Vampire Survivors | 솔로 개발 성공 사례, 미니멀 스코프 |

---

## 12. 자주 겪을 문제 & 해결법

### Unity 초보가 자주 만나는 에러

```
1. "NullReferenceException"
   → GetComponent<>()가 null 반환. Inspector에서 할당 확인 or Awake()에서 캐싱

2. "Script doesn't have a valid namespace"
   → 파일명과 클래스명이 정확히 일치해야 함

3. 오브젝트가 안 보임
   → Sorting Layer 확인. 기본 Default면 다른 것에 가려질 수 있음
   → Z position이 0인지 확인 (2D에서 Z가 0이 아니면 카메라 밖)

4. 충돌이 안 됨
   → 둘 다 Collider2D 있는지 확인
   → 최소 하나에 Rigidbody2D 있는지 확인
   → isTrigger 설정 확인

5. 애니메이션이 안 바뀜
   → Animator Controller의 Transition 조건 확인
   → Has Exit Time 체크 해제 (즉시 전환하려면)
   → Parameter 이름 오타 확인

6. 타일맵이 깨져 보임
   → Sprite의 Filter Mode: Point (no filter) 확인
   → Compression: None 확인
   → Pixels Per Unit이 타일 크기와 일치하는지 확인
```

---

> **이 문서는 프로젝트가 진행될 때마다 업데이트합니다.**
> Phase가 끝날 때마다 해당 Phase의 완료 내용을 추가하고, 다음 Phase 작업을 구체화합니다.
