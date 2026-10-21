# Story 002: Boot Failure Watchdog & Timeout Handler

> **Epic**: Bootstrap & Scene Flow
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: [set by game-dev when implementation begins]
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/bootstrap-and-scene-flow.md`
**Requirement**: `TR-boot-001`

**ADR Governing Implementation**: ADR-0002: Bootstrap Composition Root & Phased Lifecycle
**ADR Decision Summary**: Giám sát lỗi bước khởi động, timeout của bước, phân biệt bước cốt lõi (`IsCritical`) và không cốt lõi để đảm bảo game không bị treo vô tận.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Quản lý lỗi vòng đời khởi động, kích hoạt `BootFailed` an toàn.
- Forbidden: Tuyệt đối không tham chiếu tầng cao hơn (`Core`, `Features`, `Presentation`), không nuốt ngoại lệ của bước cốt lõi.
- Guardrails: Timeout bước `stepTimeoutSeconds` (10.0 s) chỉ đếm thời gian trang hiển thị.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Bootstrap/` (`CozyGachapon.Foundation`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/BootFailureHandlerTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: `BootConfig.asset`, `SceneFlowConfig.asset`

## Acceptance Criteria

*From Docs/design/gdd/bootstrap-and-scene-flow.md, scoped to this story:*

- [ ] `AC-5 [EditMode]`: Khi có cấu hình sai (`RunsAfter` trỏ `Id` không tồn tại, trỏ bước khác pha, hoặc vòng lặp phụ thuộc), validator phải báo lỗi và quá trình khởi động chuyển sang trạng thái `BootFailed` trong ≤ 1 khung hình mà không bị treo.
- [ ] `AC-8 [EditMode]`: Khi một bước không cốt lõi (`IsCritical = false`, ví dụ Audio) báo `IsFailed` hoặc ném ngoại lệ trong `Run()`, chuỗi khởi động vẫn tiếp tục chạy các bước sau và phát `BootCompleted`, ghi nhận bước lỗi vào nhật ký khởi động mà không vào `BootFailed`.
- [ ] `AC-9 [EditMode]`: Khi một bước cốt lõi (`IsCritical = true`, ví dụ Pet Catalog) báo `IsFailed`, chuỗi khởi động phát đúng 1 sự kiện `BootFailed {stepId, phase}`, dừng ngay các bước kế tiếp và kích hoạt trạng thái lỗi.
- [ ] `AC-11 [EditMode]`: Khi một bước cốt lõi không báo xong vượt quá `stepTimeoutSeconds` (10 s), chuỗi khởi động kích hoạt `BootFailed`; thời gian đếm timeout chỉ tính thời gian trang đang hiển thị (`step_elapsed_visible`). Nếu bước không cốt lõi bị timeout thì phát `BootCompleted` thay vì `BootFailed`.
- [ ] `AC-12 [EditMode]`: Nếu thiếu asset cấu hình bắt buộc `BootConfig` hoặc `SceneFlowConfig`, chuỗi khởi động lập tức vào `BootFailed` trước khi gọi bất kỳ `Run()` nào.

## Implementation Notes

- Xây dựng cơ chế Watchdog đếm thời gian hiển thị thực tế (bỏ qua khoảng thời gian khi tab bị ẩn).
- Xử lý bắt ngoại lệ try/catch quanh lời gọi `step.Run()` để phân loại bước lỗi dựa trên `IsCritical`.
- Quản lý trạng thái lỗi `BootFailedContext` lưu thông tin bước lỗi, pha và nguyên nhân để cung cấp cho UI bảng thử lại.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 001]: Trình tự thực thi tuần tự các pha.
- [Story 009]: Giao diện vẽ bảng lỗi và nút "Thử lại" trên uGUI.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/BootFailureHandlerTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 001
- Unlocks: Story 003, Story 009
