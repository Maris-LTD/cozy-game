# Story 008: Viewport Calculator & Aspect Ratio Letterboxing

> **Epic**: Bootstrap & Scene Flow
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: [set by game-dev when implementation begins]
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
- Data: `SceneFlowConfig.asset`

## Acceptance Criteria

*From Docs/design/gdd/bootstrap-and-scene-flow.md, scoped to this story:*

- [ ] `AC-30 [EditMode]`: Khi tính toán khung hiển thị `frame_rect` trên các độ phân giải màn hình chuẩn: 1920×1080 (màn ngang desktop Poki) sinh khung dọc 607.5×1080 đặt tại x = 656.25 với `frame_scale` = 0.5625; 1080×2400 sinh khung full width với chiều cao thêm `extra_height_ref` = 480; 1080×2700 (dài hơn 9:21) sinh khung giới hạn ở tỉ lệ 9:21 kèm viền đen/trang trí trên dưới; kích thước màn hình 0×0 giữ nguyên khung trước mà không phát `FrameChanged`.
- [ ] `AC-32 [EditMode]`: Khi kích thước màn hình thay đổi liên tục nhiều lần trong cùng một khung hình, sự kiện `FrameChanged` phát đúng 1 lần duy nhất trong khung hình đó với thông số kích thước cuối cùng trước bước render.
- [ ] `AC-41 [EditMode]`: Cài đặt `PlayerSettings` của dự án phải đảm bảo `runInBackground` = `true` và `defaultInterfaceOrientation` = `UIOrientation.Portrait` (không cho phép chế độ xoay ngang tự động).

## Implementation Notes

- Xây dựng thuật toán tính toán `Rect` khung hiển thị chuẩn xác (viewport rect) theo các tỉ lệ tham chiếu 9:16 (1080×1920) tới 9:21 (1080×2520).
- Tạo sự kiện cấu trúc `FrameChangedEvent` mang theo `Rect frameRectPx`, `float scale`, `float extraHeight`.
- Viết test kiểm tra trực tiếp thuộc tính `PlayerSettings` trong assembly EditMode.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 007]: Lọc chạm màn hình theo tọa độ camera.
- [Presentation/HUD]: Cấu hình CanvasScaler và viền trang trí hình ảnh.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/ViewportCalculatorTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 001
- Unlocks: Story 009
