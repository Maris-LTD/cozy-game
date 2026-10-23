# Story 006: Central Input Barrier & Tab Router

> **Epic**: Bootstrap & Scene Flow
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-23
> **Manifest Version**: 2026-10-21
> **unity-reviewer review (LP-CODE-REVIEW)**: APPROVED 2026-10-23

## Context

**Design source**: `Docs/design/gdd/bootstrap-and-scene-flow.md`
**Requirement**: `TR-boot-003`

**ADR Governing Implementation**: ADR-0002: Bootstrap Composition Root & Phased Lifecycle
**ADR Decision Summary**: Router điều hướng tab trung tâm với cổng chặn input dạng token, hỗ trợ khóa tab có chủ sở hữu và đảm bảo 0 B GC/frame.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Quản lý token chặn input trung tâm an toàn, chuyển đổi tab không cấp phát rác.
- Forbidden: Không cho phép hệ thống khác trực tiếp bật/tắt camera hoặc Canvas mà không qua router.
- Guardrails: 0 B GC per frame trong toàn bộ các thao tác mượn/nhả token và chuyển tab.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Bootstrap/` (`CozyGachapon.Foundation`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/InputBarrierTabRouterTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: `SceneFlowConfig.asset`

## Acceptance Criteria

*From Docs/design/gdd/bootstrap-and-scene-flow.md, scoped to this story:*

- [x] `AC-25 [EditMode]`: Khi các hệ thống khác nhau lấy token chặn input, trạng thái cổng là `Blocked` chừng nào số lượng token > 0; cổng chỉ chuyển sang `Open` khi toàn bộ các token đã được nhả; việc nhả token lặp lại hoặc nhả token không hợp lệ không được ném ngoại lệ (chỉ ghi log cảnh báo Editor).
- [x] `AC-27 [EditMode]`: Khi gọi `RequestTab(targetTab)`: nếu chuyển sang chính tab đang active thì trả về `NoChange`; nếu tab đang bị khóa bởi 1 token mà gọi không kèm token đó thì trả về `Locked`; nếu gọi kèm đúng token khóa thì thực hiện chuyển tab và phát `TabChanged {previous, current}`; nếu gọi đổi tab trong lúc đang khởi động (`Booting`) thì bị từ chối `Locked`.
- [x] `AC-37 [Perf]`: Trong suốt quá trình vận hành liên tục 600 khung hình có thao tác mượn/nhả token và chuyển đổi tab giữa `Board`, `Shelf`, `Shop`, số byte GC cấp phát mới của router và cổng chặn phải đạt đúng 0 B GC mỗi khung hình.

## Implementation Notes

- Cài đặt `InputBarrier` sử dụng danh sách/bộ đếm token mượn bằng struct hoặc ID để tránh boxing.
- Quản lý 3 tab cố định: `Board`, `Shelf`, `Shop`.
- Triển khai cơ chế khóa tab `LockTab(targetTab, out token)` phục vụ các hệ thống như hướng dẫn (Tutorial) hoặc khi bóc trứng Gacha.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 007]: Lọc chạm màn hình thành sự kiện `WorldPress` và xử lý nút Back.
- [Presentation/HUD]: Giao diện thanh điều hướng bottom nav uGUI.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/InputBarrierTabRouterTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] `Assets/CozyGachapon/Tests/EditMode/Bootstrap/InputBarrierTabRouterTests.cs` (9 passing EditMode tests, total 46/46 tests PASS)

## Dependencies

- Depends on: Story 004
- Unlocks: Story 007

## Completion Notes

- **Date**: 2026-10-23
- **Changes**:
  - `InputGateStatus.cs`: enum `Open`, `Blocked`.
  - `IInputBlockGate.cs`: bổ sung property `InputGateStatus Status { get; }`.
  - `AppTab.cs`: enum `Board = 0`, `Shelf = 1`, `Shop = 2`.
  - `RequestTabResult.cs`: enum `Changed`, `NoChange`, `Locked`.
  - `TabChangedEvent.cs`: immutable readonly struct `Previous`, `Current`.
  - `ITabRouter.cs`: hợp đồng router tab (`ActiveTab`, `IsBooting`, `RequestTab`, `AcquireTabLock`, `LockTab`, `ReleaseTabLock`, `OnTabChanged`).
  - `InputBlockGate.cs` & `InputBarrier.cs`: quản lý token an toàn, trả về trạng thái `Blocked`/`Open`, nhả token lặp/không hợp lệ không ném ngoại lệ.
  - `TabRouter.cs`: quản lý 3 tab, cơ chế khóa đơn quyền/đa quyền, chặn đổi tab khi booting, chuyển tab phát `TabChangedEvent`, 0 B GC per frame.
  - `SceneFlowConfig.cs`: thêm `_defaultTab` kiểu `AppTab`.
  - `InputBarrierTabRouterTests.cs`: 9 tests EditMode bao phủ trọn vẹn AC-25, AC-27, và kiểm chứng AC-37 (0 B GC qua 600 frames).
- **Review**: Approved by `unity-reviewer` (LP-CODE-REVIEW).
- **Harness**: 46/46 tests passing, 0 rule violations.
