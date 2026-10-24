# Story 008: Viewport Calculator & Aspect Ratio Letterboxing

> **Epic**: Bootstrap & Scene Flow
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-24
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/bootstrap-and-scene-flow.md`
**Requirement**: `TR-boot-004`

**ADR Governing Implementation**: ADR-0002: Bootstrap Composition Root & Phased Lifecycle, ADR-0006: World Space Plinko Physics
**ADR Decision Summary**: Tính toán khung hiển thị cố định tỉ lệ dọc từ 9:16 đến 9:21 với viền letterbox/pillarbox trang trí, bảo vệ bố cục giao diện không bị méo lệch trên đa nền tảng.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Khung hiển thị tỉ lệ dọc cố định 9:16 - 9:21, cấu hình PlayerSettings chỉ định hướng Portrait.
- Forbidden: Không kéo méo tỉ lệ khung hình (giữ aspect ratio không đổi), không cho phép xoay ngang trên thiết bị di động.
- Guardrails: Cập nhật `FrameChanged` tức thời và đồng bộ với cả camera World Space lẫn Canvas UI.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Bootstrap/` (`CozyGachapon.Foundation`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/ViewportCalculatorTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: fields `minAspect` / `maxAspect` on `SceneFlowConfig` (ScriptableObject class); the `.asset` file is created with the composition root, as for `BootConfig` (tests use `ScriptableObject.CreateInstance`)
- Settings: `ProjectSettings/ProjectSettings.asset` (AC-41)

## Acceptance Criteria

*From Docs/design/gdd/bootstrap-and-scene-flow.md, scoped to this story:*

- [x] `AC-30 [EditMode]`: Khi tính toán khung hiển thị `frame_rect` trên các độ phân giải màn hình chuẩn: 1920×1080 (màn ngang desktop Poki) sinh khung dọc 607.5×1080 đặt tại x = 656.25 với `frame_scale` = 0.5625; 1080×2400 sinh khung full width với chiều cao thêm `extra_height_ref` = 480; 1080×2700 (dài hơn 9:21) sinh khung giới hạn ở tỉ lệ 9:21 kèm viền đen/trang trí trên dưới (khung 1080×≈2519.8, y ≈ 90, `extra_height_ref` ≈ 600; sai số 0.5 px / 0.5 đơn vị tham chiếu vì `minAspect` = 0.4286 không đúng bằng 9/21); kích thước màn hình 0×0 giữ nguyên khung trước mà không phát `FrameChanged`.
- [x] `AC-32 [EditMode]`: Khi kích thước màn hình thay đổi liên tục nhiều lần trong cùng một khung hình, sự kiện `FrameChanged` phát đúng 1 lần duy nhất trong khung hình đó với thông số kích thước cuối cùng trước bước render.
- [x] `AC-41 [EditMode]`: Cài đặt `PlayerSettings` của dự án phải đảm bảo `runInBackground` = `true` và `defaultInterfaceOrientation` = `UIOrientation.Portrait` (không cho phép chế độ xoay ngang tự động).

## Implementation Notes

- Xây dựng thuật toán tính toán khung hiển thị chuẩn xác (viewport rect) theo công thức `frame_rect` của GDD, các tỉ lệ tham chiếu 9:16 (1080×1920) tới 9:21 (1080×2520).
- Thêm `[SerializeField] private float _minAspect = 0.4286f` và `_maxAspect = 0.5625f` vào `SceneFlowConfig`, kèm kiểm tra `minAspect < maxAspect ≤ referenceWidth / referenceHeight`.
- Tạo sự kiện `FrameChangedEvent` trong `Contracts/Events` với các trường float `FrameX`, `FrameY`, `FrameWidth`, `FrameHeight`, `FrameScale`, `ExtraHeightRef` (Contracts không tham chiếu UnityEngine nên không dùng `Rect`, giống Story 007).
- Seam cho AC-32: `SetScreenSize(w, h)` có thể gọi nhiều lần trong một khung; `FlushFrame()` (gọi một lần mỗi khung trước render) tính lại và phát tối đa 1 `FrameChanged` với giá trị cuối.
- Hiện thực `IFrameRectProvider.Contains(x, y)` (có từ Story 007) theo khung hiện tại để `WorldPressRouter` bỏ chạm rơi vào viền.
- Sửa `ProjectSettings/ProjectSettings.asset` qua `PlayerSettings`: `runInBackground = true`, `defaultInterfaceOrientation = Portrait`, tắt `allowedAutorotateToLandscapeLeft/Right` và `allowedAutorotateToPortraitUpsideDown`; sau đó kiểm tra `git diff` để loại thay đổi nhiễu.
- Viết test kiểm tra trực tiếp thuộc tính `PlayerSettings` trong assembly EditMode.
- Hiệu năng: mỗi khung chỉ so sánh kích thước màn hình với giá trị đã lưu, 0 B GC mỗi khung; chỉ tính lại khi kích thước đổi.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 007]: Lọc chạm màn hình theo tọa độ camera.
- [Presentation/HUD]: Cấu hình CanvasScaler và viền trang trí hình ảnh.
- [Presentation/HUD]: Vùng an toàn (tai thỏ) — `safeInsetsPx` trong `FrameChanged` và tính lại khi `Screen.safeArea` đổi (GDD B8.2) để cho story HUD.
- [Composition root]: Tạo `SceneFlowConfig.asset`, adapter MonoBehaviour đọc `Screen.width/height` mỗi khung và nối vào scene.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/ViewportCalculatorTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] `Tests/EditMode/Bootstrap/ViewportCalculatorTests.cs`, `ViewportCalculatorEdgeTests.cs`, `ProjectSettingsTests.cs` (105/105 passed 2026-10-24)

## Dependencies

- Depends on: Story 001
- Unlocks: Story 009

## Completion Notes
**Completed**: 2026-10-24
**Criteria**: 3/3 passing (no deferred items)
**Harness**: unity-harness all PASS (rules 7 files, compile, CozyGachapon.Tests.EditMode 105/105); smoke not run (nothing wired into a scene)
**Run result**: N/A - pure C# logic and a PlayerSettings change; no scene, prefab or adapter wired, nothing on screen changes
**Deviations**: Story readiness gaps fixed in this file before implementation (user approved recommendations 2026-10-24); safeInsetsPx deferred to the HUD story; FrameChangedEvent uses float fields; frame math split into internal FrameRectCalculator; ViewportCalculator throws on an invalid aspect range at construction and skips recompute if the config becomes invalid later; ProjectSettings changed for AC-41 (runInBackground, Portrait, landscape and upside-down autorotate off)
**Test Evidence**: ViewportCalculatorTests, ViewportCalculatorEdgeTests, ProjectSettingsTests - 105/105 passed in CozyGachapon.Tests.EditMode
**Visual sign-off**: n/a (Logic)
**Code Review**: unity-reviewer 3 minor (2 fixed: FlushFrame re-validates config, FrameRectCalculator internal; SetAspectRangeForTest kept public to match BootConfig); qa-tester edge gaps added as 4 tests; game-code-review APPROVED WITH SUGGESTIONS; LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none (AC-32 per-frame FlushFrame call order is wiring, checked when the composition root lands: TD-004)
**Tech debt**: TD-004
