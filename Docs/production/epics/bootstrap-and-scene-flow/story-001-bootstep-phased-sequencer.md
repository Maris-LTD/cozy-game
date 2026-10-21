# Story 001: IBootStep Phased Startup Sequencer

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
**ADR Decision Summary**: Khởi tạo Pure C# Composition Root không dùng DI container bên ngoài, điều phối tuần tự qua hợp đồng interface `IBootStep` theo các pha xác định.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Khởi tạo Pure C# Composition Root, phân pha khởi động `IBootStep` tuần tự.
- Forbidden: Tuyệt đối không tham chiếu tầng cao hơn (`Core`, `Features`, `Presentation`), không đặt logic vào `Contracts`, không dùng static singletons.
- Guardrails: Hoàn tất toàn bộ chuỗi khởi động và prewarm trong ngân sách 2.0 s.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Bootstrap/` (`CozyGachapon.Foundation`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/BootStepSequencerTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: None (logic sequencer thuần C#; ScriptableObject BootConfig được tạo ở Story 002/003)

## Acceptance Criteria

*From Docs/design/gdd/bootstrap-and-scene-flow.md, scoped to this story:*

- [ ] `AC-1 [EditMode]`: Khi đăng ký các bước giả theo thứ tự xáo trộn thuộc các pha `ServiceInit`, `DataLoad`, `SaveLoad`, `GameplayInit`, `Prewarm`, thứ tự gọi `Run()` phải theo đúng thứ tự pha, mỗi bước được gọi đúng 1 lần, và sự kiện `BootCompleted` phát đúng 1 lần sau khi bước `Prewarm` hoàn tất.
- [ ] `AC-2 [EditMode]`: Trong cùng một pha (ví dụ `GameplayInit`), thứ tự thực thi giữa các bước phải giải quyết quan hệ phụ thuộc tô-pô theo `RunsAfter`, trường hợp không có phụ thuộc thì sắp xếp theo thứ tự bảng chữ cái của `Id`.
- [ ] `AC-3 [EditMode]`: Khi một bước kéo dài nhiều khung hình mới báo `IsDone`, bước phụ thuộc tiếp theo không được gọi `Run()` trước khung hình bước trước hoàn tất.
- [ ] `AC-4 [EditMode]`: Pha không có bước nào (như `SaveLoad` trong MVP) phải kết thúc ngay trong cùng khung hình mà không gây trễ thêm khung nào.
- [ ] `AC-6 [EditMode]`: Khi hệ thống đã bắt đầu pha `ServiceInit`, mọi yêu cầu đăng ký bước mới phải bị từ chối; hai bước đăng ký trùng `Id` phải bị từ chối và kích hoạt `BootFailed`.

## Implementation Notes

- Xây dựng sequencer thuần C# nhận danh sách `IBootStep`.
- Hợp đồng `IBootStep` gồm các thuộc tính: `Id` (string), `Phase` (enum BootPhase), `RunsAfter` (IReadOnlyList<string>), `IsCritical` (bool), cùng các phương thức/thuộc tính `Run()`, `IsDone`, `IsFailed`.
- Sắp xếp tô-pô độc lập trong từng pha, phát hiện chu trình phụ thuộc và từ chối bước trùng lặp ID.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 002]: Xử lý timeout của từng bước và hiển thị bảng thử lại khi gặp lỗi khởi động.
- [Story 003]: Điều phối rèm che "Tiệm mở cửa" và phát sự kiện `Interactive`.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/BootStepSequencerTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: None
- Unlocks: Story 002, Story 003
