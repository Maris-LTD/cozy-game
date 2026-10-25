# Story 011: Bootstrap Composition Root

> **Epic**: Bootstrap & Scene Flow
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: 2 days
> **Last Updated**: [set by game-dev when implementation begins]
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/bootstrap-and-scene-flow.md` (B1 trình tự khởi động, B4 lỗi + "Thử lại", B5 rèm, B6 vòng đời, B10 hàng đợi màn chào, mục Interactions)
**Requirement**: `TR-boot-001` (AC-10, AC-35; đóng TD-004, TD-005)

**ADR Governing Implementation**: ADR-0002: Bootstrap Composition Root & Phased Lifecycle
**ADR Decision Summary**: Một composition root duy nhất dựng mọi dịch vụ Bootstrap bằng `new`, tiêm phụ thuộc qua constructor, chạy các pha `IBootStep` tuần tự rồi mở rèm và bật router; không DI container, không singleton, không `Find`. Bước cốt lõi lỗi phải hiện bảng "Thử lại".
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: composition root duy nhất điều phối khởi động; mọi dịch vụ cần khởi tạo cài `IBootStep`; interface/event ở `CozyGachapon.Contracts`.
- Forbidden: `FindObjectOfType`, `GameObject.Find`, static singleton; `Instantiate`/`Destroy` lúc chạy ngoài pha `Prewarm`; tham chiếu tầng cao hơn.
- Guardrails: khung đầu tới tương tác ≤ 2.0 s (`BootConfig.maxBootToInteractiveSeconds`); 0 B GC mỗi khung ở Running do root, router và cổng chặn.

**Engine**: Unity 6000.3.16f1 | **Risk**: MEDIUM
**Engine Notes**: Thứ tự `Awake`/`Update` giữa root và adapter Input (`PointerPressInputAdapter`, `BackKeyInputAdapter`) phải cố định — dùng `[DefaultExecutionOrder]` âm cho root hoặc để root gọi tick các adapter. Chưa có adapter Unity cho vòng đời: story này thêm `AppLifecycleAdapter` (MonoBehaviour mỏng, Runtime) chuyển `OnApplicationPause(bool)` → trang ẩn/hiện, `OnApplicationFocus(bool)` → chỉ mất/được focus (không Paused, GDD B6), `OnApplicationQuit` → Quitting, vào `AppLifecycleWatchdog`. Visibility WebGL qua `document.visibilitychange` (jslib) ngoài phạm vi — ghi tech debt nếu `OnApplicationPause` không phát trên WebGL. `SceneManager.LoadScene` của "Thử lại" đã có ở Story 009.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Bootstrap/BootstrapController.cs` (MonoBehaviour mỏng, `CozyGachapon.Runtime`) + một lớp thuần C# `BootstrapGraph` (hoặc tên tương đương) dựng và nối các dịch vụ để test EditMode được; tách file nếu vượt 250 dòng.
- Test: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/BootstrapGraphTests.cs`
- Data: `Assets/CozyGachapon/Data/Bootstrap/BootConfig.asset`, `SceneFlowConfig.asset` (đã có)
- Scene / prefab: `Assets/CozyGachapon/Scenes/MainGame.unity` (thay `BootstrapCurtainDriver` bằng `BootstrapController`), `StartupCurtain.prefab`

## Acceptance Criteria

*From Docs/design/gdd/bootstrap-and-scene-flow.md, scoped to this story:*

- [ ] `AC-35 [EditMode]`: **GIVEN** đồ thị Bootstrap dựng bằng stub Platform Services ghi lại mọi lời gọi và nguồn tín hiệu nền tảng giả, **WHEN** chạy khởi động tới Running, 3 chu kỳ `Paused`/`Resumed`, 10 lần đổi tab và một lần BootFailed (đồ thị thứ hai với bước cốt lõi giả lỗi), **THEN** Bootstrap chỉ phát các sự kiện `BootCompleted`, `Interactive`, `BootFailed`, `FirstInteraction`, `Paused`, `Resumed`, `TabChanged`, `FrameChanged`, `WelcomeQueueCompleted`, `Quitting`, và số yêu cầu quảng cáo do Bootstrap gây ra = 0.
- [ ] `AC-WIRE [EditMode]`: **GIVEN** đồ thị Bootstrap với `BootConfig`/`SceneFlowConfig` mặc định và không có bước nào (MVP hiện tại), **WHEN** tick theo đồng hồ giả, **THEN** `BootCompleted` phát ở khung đầu, rèm Covering → Revealing lúc `curtainMinDisplaySeconds` → Open sau thêm `curtainRevealDuration`, `Interactive` phát đúng 1 lần, `WelcomeQueueCompleted` phát trong cùng khung, cổng chặn còn 0 token, `ActiveTab` = `SF.defaultTab`; với bước cốt lõi `pet.catalog` giả lỗi thì rèm vào ErrorShown và view nhận `stepId` = `pet.catalog` từ chính `BootFailedEvent` (không qua lời gọi riêng — đóng TD-005).
- [ ] `AC-10 [Play mode]`: **GIVEN** scene `MainGame` với một bước cốt lõi giả lỗi ở lần chạy đầu và thành công ở lần sau (bật qua một cờ chỉ có trong Editor/Development build), **WHEN** bấm "Thử lại", **THEN** scene được nạp lại đúng 1 lần, khởi động chạy lại từ `ServiceInit` và phát `BootCompleted`. Bằng chứng: playtest note + ảnh chụp trước/sau trong `Docs/production/qa/evidence/story-011-composition-root/`.
- [ ] `AC-RUN [Screenshot]`: **GIVEN** scene `MainGame` không có cờ lỗi, **WHEN** vào Play mode và chờ 2 s, **THEN** ảnh chụp 1080×1920 không còn rèm (rèm đã Open, Canvas rèm tắt), console 0 lỗi; `unity-harness smoke` với `expectTypes` = [`CozyGachapon.BootstrapController`] pass.

## Implementation Notes

- `BootstrapController` là composition root duy nhất (ADR-0002). Nó chỉ: giữ tham chiếu serialized (`BootConfig`, `SceneFlowConfig`, `StartupCurtainView`, các adapter Input/Platform đã có), dựng `BootstrapGraph` trong `Awake`, và mỗi khung gọi `graph.Tick(Time.unscaledDeltaTime)` trong `Update`. Mọi logic nối nằm trong lớp thuần C# để EditMode test dựng được.
- `BootstrapGraph` dựng bằng `new` và nối: `InputBlockGate`, `InputBarrier`, `BootStepSequencer`, `BootWatchdog`, `StartupCurtainController` (+ `StartupCurtainView` qua interface hoặc delegate để test không cần MonoBehaviour), `AppLifecycleWatchdog`, `BootStatusTracker`, `ViewportCalculator` (mỗi khung `SetScreenSize` + `FlushFrame`, truyền làm `IFrameRectProvider` cho `WorldPressRouter` — TD-004), `TabRouter`, `WorldPressRouter`, `BackButtonRouter`, `FirstInteractionDetector`, `WelcomeQueue` (Story 010, `Begin()` khi rèm Open). Nối sự kiện: `OnBootCompleted` → curtain; `OnBootFailed` → curtain + view nhận `BootFailedEvent` trực tiếp (TD-005); curtain `OnInteractive` → welcome queue.
- `BootStepSequencer` phát `stepId` rỗng khi thiếu config (`BootStepSequencer.cs:79`): đổi thành `"boot.config"` để bảng lỗi luôn có mã (TD-005).
- Thiếu `BootConfig`/`SceneFlowConfig`: hiện bảng "Thử lại" bằng giá trị dựng sẵn của prefab (GDD B1.2), như `BootstrapCurtainDriver` đang làm.
- Bước giả lỗi cho AC-10: một `IBootStep` cốt lõi (`debug.failOnce`, pha `DataLoad`) chỉ đăng ký khi bật cờ serialized `_failFirstBootForTest` và chỉ trong `UNITY_EDITOR || DEVELOPMENT_BUILD`; lần chạy đầu lỗi, đánh dấu qua `SessionState`/static bool chỉ trong Editor để lần nạp lại thành công. Cờ mặc định tắt trong scene.
- Xoá `BootstrapCurtainDriver` và `CurtainPreviewMode` sau khi root thay thế (giữ chế độ preview nếu cần chụp AC-15/16: chuyển thành cờ Editor-only trên root). Xoá file có sẵn được phép vì story này nói rõ.
- `.unity-harness/config.json`: `smoke.expectTypes` = [`CozyGachapon.BootstrapController`] (đổi config nhỏ, đã được user cho phép trong vòng lặp tự động).
- Không `Debug.Log*`. Dòng log `boot_to_interactive` (GDD B5.6) chưa làm ở story này — xung đột luật không Debug.Log, chờ user quyết.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 010]: Logic `WelcomeQueue`.
- Dòng log `boot_to_interactive` (B5.6, AC-36 [Perf]) — chờ quyết định của user.
- AC-20, AC-23, AC-24, AC-26, AC-28 (phần renderer/camera), AC-39: cần Plinko Board và HUD — làm trong các epic đó.
- Bước khởi động thật của Pooling, Audio, Economy, Pet Catalog — mỗi epic tự đăng ký `IBootStep`.

## QA Test Cases

- **AC-35**: chỉ phát sự kiện cho phép
  - Given: đồ thị dựng với stub ghi lại; bộ đếm theo tên sự kiện qua mọi event public của các dịch vụ Bootstrap
  - When: khởi động tới Running, 3 chu kỳ ẩn/hiện, 10 `RequestTab`, đồ thị thứ hai với bước cốt lõi lỗi
  - Then: tập tên sự kiện quan sát được ⊆ danh sách cho phép; stub ghi 0 yêu cầu quảng cáo
  - Edge cases: `Quitting` hai lần → 1 sự kiện
- **AC-WIRE**: nối đầy đủ
  - Given: config mặc định (0.4, 0.35, 2.0), đồng hồ giả
  - When: tick từng khung 1/60 s tới 1.0 s
  - Then: `BootCompleted` khung 1; Revealing lúc ≥ 0.40 s; Open và `Interactive` lúc ≈ 0.75 s; `WelcomeQueueCompleted` cùng khung; token = 0; `ActiveTab` = `Board`
  - Edge cases: bước `pet.catalog` lỗi → ErrorShown, `stepId` = `pet.catalog`, không `Interactive`
- **AC-10** (Play mode):
  - Setup: bật `_failFirstBootForTest` trên `BootstrapController` trong MainGame (không lưu scene), vào Play mode
  - Verify: bảng lỗi hiện với mã `debug.failOnce`; bấm "Thử lại"; scene nạp lại; rèm mở
  - Pass condition: đúng 1 lần nạp lại, lần hai tới Running, ghi playtest note
- **AC-RUN** (Screenshot):
  - Setup: MainGame mặc định, Play mode, chờ 2 s
  - Verify: không còn rèm kem, 0 lỗi console
  - Pass condition: ảnh `02-running-1080x1920.png` cho thấy nền camera (chưa có nội dung game), smoke pass

## Test Evidence

**Story Type**: Integration
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/BootstrapGraphTests.cs` passing; playtest note + screenshots `Docs/production/qa/evidence/story-011-composition-root/`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 001–010
- Unlocks: Object Pooling, Audio, Economy Core, Pet Catalog epics (đăng ký `IBootStep` vào root)
