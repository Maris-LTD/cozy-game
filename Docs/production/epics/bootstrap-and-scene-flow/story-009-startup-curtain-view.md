# Story 009: Startup Curtain View & Retry Panel

> **Epic**: Bootstrap & Scene Flow
> **Status**: Complete
> **Layer**: Foundation
> **Type**: UI
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/bootstrap-and-scene-flow.md`
**Requirement**: `TR-boot-005`

**ADR Governing Implementation**: ADR-0002: Bootstrap Composition Root & Phased Lifecycle
**ADR Decision Summary**: Giao diện uGUI rèm kem, bảng gỗ "Tiệm mở cửa", bảng thông báo lỗi khởi động và nút bấm "Thử lại" tải lại scene.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Thiết kế giao diện uGUI tuân thủ tỉ lệ reference 1080x1920, chuẩn độ tương phản văn bản.
- Forbidden: Không hiển thị thanh tiến độ phần trăm % hay màn hình loading thô ráp (giữ chuẩn cozy game).
- Guardrails: Đảm bảo thời gian hiển thị và fade bảng lỗi không vượt quá cấu hình `BC.errorPanelFadeSeconds`.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: Unity 6 gộp TextMeshPro vào `com.unity.ugui` 2.0 (namespace `TMPro` giữ nguyên). Cần import TMP Essential Resources và dùng font asset có đủ glyph tiếng Việt (dynamic SDF); kiểm chữ có dấu trong ảnh chụp, không thấy ô vuông thiếu glyph.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Bootstrap/` (`CozyGachapon.Runtime`) — view, driver và nút "Thử lại" là MonoBehaviour dùng `UnityEngine.UI` / `SceneManager`, không đặt trong `CozyGachapon.Foundation` (chỉ tham chiếu Contracts).
- Scene / prefab: `Assets/CozyGachapon/Prefabs/Bootstrap/StartupCurtain.prefab` (story này tạo), `Assets/CozyGachapon/Scenes/MainGame.unity`
- Data: `Assets/CozyGachapon/Data/Bootstrap/BootConfig.asset`, `SceneFlowConfig.asset` (story này tạo; `errorPanelFadeSeconds` = 0.2 theo GDD Tuning Knobs, sửa mặc định 0.3 trong `BootConfig.cs` cho khớp)

## Acceptance Criteria

*From Docs/design/gdd/bootstrap-and-scene-flow.md, scoped to this story:*

- [ ] `AC-15 [Screenshot]`: Khi scene ở trạng thái `Covering`, ảnh chụp màn hình kiểm tra ở cả 1080×1920 và 1920×1080 hiển thị rèm kem phủ kín vùng `frame_rect`, bảng gỗ "Tiệm mở cửa" nằm chính giữa, không có thanh tiến độ hay số %, chữ đạt độ tương phản tối thiểu ≥ 4.5:1.
- [ ] `AC-16 [Screenshot]`: Khi xảy ra lỗi khởi động `BootFailed` do bước cốt lõi lỗi, ảnh chụp màn hình hiển thị bảng gỗ thông báo lỗi thân thiện, nút "Thử lại" màu matcha, và dòng chữ nhỏ hiển thị mã định danh bước lỗi `stepId` (không bị màn hình trắng xóa).

*Cách đưa scene vào trạng thái để chụp*: AC-15 dùng `BootstrapCurtainDriver._previewMode = HoldCovering`; AC-16 dùng `FailAtPetCatalog` (chụp ở 1080×1920, dòng chữ nhỏ phải là `pet.catalog`).

## Implementation Notes

- Prefab uGUI `StartupCurtain.prefab`: một Canvas Screen Space - Overlay (CanvasScaler reference 1080×1920, match theo chiều cao), con là `CurtainRoot` (RectTransform đặt theo `frame_rect`) chứa nền rèm, bảng gỗ "Tiệm mở cửa" ở giữa, và `RetryPanel` (ẩn mặc định).
- `StartupCurtainView` (Runtime): đọc `StartupCurtainController.State` / `RevealProgress` mỗi khung để vẽ Covering, Revealing (kéo `CurtainRoot` lên theo tiến độ), Open (tắt Canvas), ErrorShown (hiện `RetryPanel` mờ dần trong `BootConfig.ErrorPanelFadeSeconds`, ghi `stepId` từ `BootFailedEvent`). View không giữ trạng thái logic.
- View nghe `ViewportCalculator.OnFrameChanged` và đặt `CurtainRoot` theo `FrameX/FrameY/FrameWidth/FrameHeight` (đổi từ pixel màn hình sang toạ độ Canvas). Ngoài khung là màu nền camera.
- `BootstrapCurtainDriver` (Runtime, driver gọn — chưa phải composition root): trong `Awake` tạo `ViewportCalculator`, `InputBlockGate` và `StartupCurtainController` từ `BootConfig`/`SceneFlowConfig` serialized; mỗi khung gọi `SetScreenSize(Screen.width, Screen.height)` + `FlushFrame()` và `Tick(Time.unscaledDeltaTime)`. Có field `[SerializeField] private CurtainPreviewMode _previewMode` (`None`, `HoldCovering`, `FailAtPetCatalog`) để chụp ảnh: `HoldCovering` không bao giờ gọi `NotifyBootCompleted`; `FailAtPetCatalog` gọi `NotifyBootFailed(new BootFailedEvent("pet.catalog", BootPhase.DataLoad, "preview"))` ở khung đầu. `None` thì đứng ở Covering (chưa có sequencer thật).
- `RetryButton` (Runtime): `[SerializeField] private string _sceneName = "MainGame"` dựng sẵn trong prefab, không đọc `SceneFlowConfig` (GDD B-Edge: thiếu config cũng phải hiện được bảng). Lần bấm đầu tắt `interactable` rồi gọi `SceneManager.LoadScene(_sceneName)`; các lần bấm sau bị bỏ.
- Nút "Thử lại" có vùng chạm ≥ 120×120 đơn vị tham chiếu.

**Đặc tả hình ảnh tạm (placeholder tới khi có board Penpot, GDD Q7 — art-director, 2026-11-04)**: chỉ dùng `Image` màu phẳng + Sprite mặc định UISprite bo tròn, không cần art.

| Phần | Màu | Chữ / màu chữ | Tương phản |
|---|---|---|---|
| Nền rèm | kem `#F6EBD9` | — | — |
| Bảng gỗ "Tiệm mở cửa" | gỗ `#7A5230` | "Tiệm mở cửa", kem `#F6EBD9`, cỡ 64 | ≈ 5.8:1 |
| Bảng lỗi | gỗ `#7A5230` | "Tiệm gặp chút trục trặc khi mở cửa", kem `#F6EBD9`, cỡ 44 | ≈ 5.8:1 |
| Nút "Thử lại" | matcha `#7EA172` | "Thử lại", nâu `#3B2A1E`, cỡ 48 (không dùng chữ trắng: chỉ ≈ 2.9:1) | ≈ 4.7:1 |
| Mã bước lỗi | trên bảng gỗ | `stepId`, kem nhạt `#E3D3BC`, cỡ 28 | ≈ 4.6:1 |

**Performance**: một Canvas overlay, ≤ 6 draw call khi rèm hiện; Canvas tắt hẳn khi Open nên 0 chi phí lúc chơi. `StartupCurtainView` không cấp phát trong `Update` (chuỗi `stepId` gán một lần khi vào ErrorShown).

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 003]: Logic máy trạng thái rèm và đếm thời gian khởi động.
- [Story 008]: Thuật toán tính toán kích thước viewport rect.
- Composition root đầy đủ (chạy `BootStepSequencer` thật, nối mọi router, phát `Interactive` cho các hệ) và bước giả `pet.catalog` chạy qua sequencer — story sau, đóng TD-004. `BootstrapCurtainDriver` ở đây chỉ đủ để chụp AC-15/16.
- Viền trang trí ngoài khung ở 16:9 (AC-31) — story HUD/viền; ở story này vùng ngoài khung là màu nền camera.
- Art cuối cho rèm/bảng gỗ (chờ board Penpot, GDD Q7).

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: UI
**Required evidence**: A screenshot of each screen touched, `Docs/production/qa/evidence/story-009-startup-curtain-view/NN-[screen].png`

**Evidence**: [x] `01-covering-1080x1920.png`, `02-covering-1920x1080.png` (AC-15), `03-bootfailed-1080x1920.png` (AC-16) — Play mode capture 2026-10-25

## Dependencies

- Depends on: Story 003, Story 008
- Unlocks: None

## Completion Notes
**Completed**: 2026-10-25
**Criteria**: 2/2 passing (none deferred)
**Harness**: unity-harness all PASS (rules, compile, EditMode 116/116); smoke PASS (MainGame, 0 console errors, 0 missing scripts)
**Run result**: OBSERVED - `Docs/production/qa/evidence/story-009-startup-curtain-view/01-covering-1080x1920.png`, `02-covering-1920x1080.png`, `03-bootfailed-1080x1920.png` (Play mode, preview modes HoldCovering / FailAtPetCatalog)
**Deviations**: ADVISORY - `BootstrapCurtainDriver` builds the controller outside ADR-0002's `BootstrapController` (temporary driver approved by this story, TD-004); stepId reaches the view through `StartupCurtainView.ShowBootFailure`, not from `BootFailedEvent` via the controller (TD-005); outside `frame_rect` at 16:9 is the camera background (AC-31 out of scope); extra polish: ease-out reveal, shop sign hidden while the error panel shows
**Test Evidence**: 3 screenshots above; EditMode `CurtainLayoutMathTests`, `RetryTriggerGuardTests` (passed, 116/116 total)
**Visual sign-off**: n/a (UI story; screenshots reviewed against AC-15/16)
**Code Review**: game-code-review APPROVED WITH SUGGESTIONS; suggestions 2-4 fixed (driver disables when Open, missing/invalid SceneFlowConfig shows fallback panel, RetryPanel interactable only at full alpha), 1/5/6 logged as TD-005, TD-006; LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none
