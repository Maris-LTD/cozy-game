# Story 009: Startup Curtain View & Retry Panel

> **Epic**: Bootstrap & Scene Flow
> **Status**: Ready
> **Layer**: Foundation
> **Type**: UI
> **Estimate**: 1-2 days
> **Last Updated**: [set by game-dev when implementation begins]
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
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Bootstrap/` (`CozyGachapon.Foundation`)
- Scene / prefab: `Assets/CozyGachapon/Prefabs/Bootstrap/StartupCurtain.prefab`, `MainGame.unity`
- Data: `BootConfig.asset`

## Acceptance Criteria

*From Docs/design/gdd/bootstrap-and-scene-flow.md, scoped to this story:*

- [ ] `AC-15 [Screenshot]`: Khi scene ở trạng thái `Covering`, ảnh chụp màn hình kiểm tra ở cả 1080×1920 và 1920×1080 hiển thị rèm kem phủ kín vùng `frame_rect`, bảng gỗ "Tiệm mở cửa" nằm chính giữa, không có thanh tiến độ hay số %, chữ đạt độ tương phản tối thiểu ≥ 4.5:1.
- [ ] `AC-16 [Screenshot]`: Khi xảy ra lỗi khởi động `BootFailed` do bước cốt lõi lỗi, ảnh chụp màn hình hiển thị bảng gỗ thông báo lỗi thân thiện, nút "Thử lại" màu matcha, và dòng chữ nhỏ hiển thị mã định danh bước lỗi `stepId` (không bị màn hình trắng xóa).

## Implementation Notes

- Xây dựng prefab giao diện uGUI gồm Canvas rèm che, bảng hiệu gỗ "Tiệm mở cửa", và panel lỗi `RetryPanel`.
- Tích hợp logic xử lý sự kiện bấm nút "Thử lại": gọi `SceneManager.LoadScene("MainGame")` để nạp lại scene từ đầu sạch sẽ.
- Kết nối với `StartupCurtainController` (Story 003) để kích hoạt hiệu ứng mờ dần/kéo mở rèm khi nhận tín hiệu sẵn sàng.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 003]: Logic máy trạng thái rèm và đếm thời gian khởi động.
- [Story 008]: Thuật toán tính toán kích thước viewport rect.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: UI
**Required evidence**: A screenshot of each screen touched, `Docs/production/qa/evidence/story-009-startup-curtain-view/NN-[screen].png`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 003, Story 008
- Unlocks: None
