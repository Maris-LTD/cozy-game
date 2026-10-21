# Story 004: Application Lifecycle & Visibility Watchdog

> **Epic**: Bootstrap & Scene Flow
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: [set by game-dev when implementation begins]
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/bootstrap-and-scene-flow.md`
**Requirement**: `TR-boot-002`

**ADR Governing Implementation**: ADR-0002: Bootstrap Composition Root & Phased Lifecycle
**ADR Decision Summary**: Điều phối vòng đời ứng dụng trên WebGL (Page Visibility) và thiết bị di động (`OnApplicationPause`), chỉ tạm dừng khi tab bị ẩn hoặc app xuống nền, không tạm dừng khi chỉ mất focus.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Quản lý vòng đời ứng dụng, kẹp thời gian vắng mặt UTC chống delta âm.
- Forbidden: Không can thiệp hoặc thay đổi `Time.timeScale` toàn cục.
- Guardrails: Cung cấp `awaySeconds` chính xác cho các hệ thống kinh tế/offline.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Bootstrap/` (`CozyGachapon.Foundation`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/LifecycleWatchdogTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: `SceneFlowConfig.asset`

## Acceptance Criteria

*From Docs/design/gdd/bootstrap-and-scene-flow.md, scoped to this story:*

- [ ] `AC-17 [EditMode]`: Khi ở trạng thái `Running`, việc cửa sổ mất focus (nhưng vẫn hiển thị trên màn hình) không phát sự kiện gì; khi trang web bị ẩn hoặc ứng dụng xuống nền, phát đúng 1 sự kiện `Paused {reason = Hidden}`; các tín hiệu ẩn tiếp theo bị bỏ qua; khi trang hiển thị lại, phát đúng 1 sự kiện `Resumed {awaySeconds}`.
- [ ] `AC-18 [EditMode]`: Thời gian vắng mặt `awaySeconds` được tính bằng chênh lệch thời gian UTC thực giữa `Resumed` và `Paused`; giá trị này được kẹp tự động ≥ 0 (nếu đồng hồ hệ thống bị lùi hoặc sai lệch NaN thì trả về 0 s).
- [ ] `AC-19 [EditMode]`: Nếu trang bị ẩn trong lúc đang chạy chuỗi khởi động (`Booting.Steps`), không phát sự kiện `Paused` trước `BootCompleted`; chỉ khi `BootCompleted` hoàn tất mới chuyển sang `Paused` với trạng thái trả về là `Booting.Curtain`; rèm vẫn giữ `Covering` cho đến khi trang hiển thị lại.
- [ ] `AC-21 [EditMode]`: Khi nền tảng thông báo đóng ứng dụng, phát đúng 1 sự kiện `Quitting`, sau đó không phát thêm bất kỳ sự kiện `Paused` hay `Resumed` nào khác.

## Implementation Notes

- Cài đặt interface trừu tượng hóa nguồn tín hiệu vòng đời để có thể kiểm thử giả lập hoàn toàn trong EditMode mà không phụ thuộc trình duyệt thật.
- Sử dụng `DateTimeOffset.UtcNow` để đo lường khoảng cách thời gian vắng mặt an toàn.
- Liên kết với máy trạng thái vòng đời: `Uninitialized` -> `Booting` -> `Running` <-> `Paused` -> `Quitting`.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 003]: Quản lý rèm che và sự kiện `Interactive`.
- [Story 005]: Xử lý cờ quảng cáo có thưởng chặn vòng đời tạm dừng.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/LifecycleWatchdogTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 003
- Unlocks: Story 005, Story 006
