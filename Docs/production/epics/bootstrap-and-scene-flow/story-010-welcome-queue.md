# Story 010: Welcome Queue

> **Epic**: Bootstrap & Scene Flow
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/bootstrap-and-scene-flow.md` (B10 Hàng đợi màn chào, bảng trạng thái "Hàng đợi màn chào", Tuning Knobs `SceneFlowConfig.welcomeOrder`)
**Requirement**: `AC-34`

**ADR Governing Implementation**: ADR-0002: Bootstrap Composition Root & Phased Lifecycle
**ADR Decision Summary**: Bootstrap là composition root thuần C#; hệ khác chỉ biết interface của Bootstrap (ở `CozyGachapon.Contracts`), phụ thuộc được tiêm, không singleton, không `Find`.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: interface và event struct đặt trong `CozyGachapon.Contracts`; logic ở `CozyGachapon.Foundation`, thuần C#, test được ở EditMode.
- Forbidden: không tham chiếu tầng cao hơn; không static singleton / `FindObjectOfType` / `GameObject.Find`.
- Guardrails: 0 B GC mỗi khung khi hàng đợi rỗng hoặc đang chờ một bước.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none (thuần C#, không dùng API Unity)

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Bootstrap/WelcomeQueue.cs` (`CozyGachapon.Foundation`); `Assets/CozyGachapon/Scripts/Contracts/Boot/IWelcomeStep.cs`, `Assets/CozyGachapon/Scripts/Contracts/Events/WelcomeQueueCompletedEvent.cs` (`CozyGachapon.Contracts`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/WelcomeQueueTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: `SceneFlowConfig` thêm `[SerializeField] private List<string> _welcomeOrder` (mặc định rỗng) + property chỉ đọc `WelcomeOrder`

## Acceptance Criteria

*From Docs/design/gdd/bootstrap-and-scene-flow.md, scoped to this story:*

- [ ] `AC-34a [EditMode]`: **GIVEN** `SF.welcomeOrder` rỗng và không có bước nào đăng ký (MVP), **WHEN** `WelcomeQueue.Begin()` được gọi (thời điểm rèm kéo xong), **THEN** `WelcomeQueueCompleted` phát đúng 1 lần trong cùng lời gọi, không lấy token cổng chặn nào.
- [ ] `AC-34b [EditMode]`: **GIVEN** ba bước giả `a`, `b`, `c` với `welcomeOrder` = [`b`, `a`] và `c.ShouldShow()` = đúng, `a.ShouldShow()` = sai, **WHEN** `Begin()` rồi lần lượt gọi `onDone` của bước đang hiện, **THEN** thứ tự `Show` là `b` → `c` (bỏ `a`), mỗi lúc chỉ một bước hiện, trong lúc một bước hiện cổng chặn giữ đúng 1 token và nhả khi bước gọi `onDone`, `WelcomeQueueCompleted` phát đúng 1 lần sau `onDone` của `c`.
- [ ] `AC-34c [EditMode]`: **GIVEN** bước `c` không có trong `welcomeOrder` và bước `z` cũng không có, **WHEN** kiểm tra cấu hình (`Validate`), **THEN** trả về cảnh báo nêu `c` và `z`, và lúc chạy chúng xếp sau các bước có tên theo thứ tự chữ của `Id` (`c` trước `z`); gọi `onDone` hai lần hoặc gọi `Begin()` lần hai không làm phát thêm sự kiện hay nhả thêm token.

## Implementation Notes

- `IWelcomeStep` (Contracts): `string Id { get; }`, `bool ShouldShow()`, `void Show(Action onDone)`.
- `WelcomeQueueCompletedEvent` (Contracts): readonly struct, không trường (hoặc chỉ số bước đã hiện).
- `WelcomeQueue` (Foundation): constructor nhận `SceneFlowConfig` và `IInputBlockGate`; `bool Register(IWelcomeStep)` chỉ nhận trước `Begin()` (sau đó trả false); `Begin()` chạy hàng đợi; `IReadOnlyList<string> Validate()` trả danh sách `Id` không có trong `welcomeOrder` (không ghi log — luật dự án cấm `Debug.Log*`; nơi gọi tự hiển thị); trạng thái `Waiting` → `Running` → `Done` (GDD bảng trạng thái).
- Thứ tự: theo `welcomeOrder`; bước không có tên xếp sau, theo `string.CompareOrdinal` của `Id`. Sắp xếp một lần trong `Begin()`, không LINQ.
- Mỗi bước hiện giữ một token `AcquireInputBlock("welcome.<id>")`; nhả trước khi chuyển bước tiếp. `onDone` của một bước đã xong bị bỏ qua.
- Không cấp phát trong vòng lặp mỗi khung (hàng đợi không có `Tick`, chạy theo callback).

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 011]: Gọi `WelcomeQueue.Begin()` khi rèm Open trong composition root.
- Bước màn chào thật (Offline Earnings, Tutorial) — Vertical Slice.

## QA Test Cases

- **AC-34a**: hàng đợi rỗng
  - Given: `SceneFlowConfig` mặc định (`welcomeOrder` rỗng), `InputBlockGate` mới, không đăng ký bước nào
  - When: `Begin()`
  - Then: đếm sự kiện = 1 ngay sau lời gọi; `gate.ActiveTokenCount` = 0 suốt quá trình
  - Edge cases: gọi `Begin()` lần hai → vẫn 1 sự kiện
- **AC-34b**: thứ tự và token
  - Given: bước giả ghi lại thứ tự `Show`, giữ `onDone` để test gọi tay
  - When: `Begin()`; gọi `onDone` của `b`; gọi `onDone` của `c`
  - Then: thứ tự [`b`, `c`]; token = 1 khi `b` hiện, 1 khi `c` hiện, 0 sau cùng; sự kiện = 1 chỉ sau `onDone` của `c`
  - Edge cases: `Register` sau `Begin()` trả false
- **AC-34c**: bước không có tên
  - Given: `welcomeOrder` = [`b`]; bước `z`, `c`, `b` đăng ký theo thứ tự đó
  - When: `Validate()`; `Begin()` và gọi `onDone` lần lượt
  - Then: `Validate()` chứa `c`, `z`; thứ tự hiện `b` → `c` → `z`
  - Edge cases: gọi `onDone` của `b` hai lần → không nhảy cóc qua `c`

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/WelcomeQueueTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] `WelcomeQueueTests.cs`, `WelcomeQueueEdgeTests.cs` (EditMode 133/133)

## Dependencies

- Depends on: Story 006 (InputBlockGate)
- Unlocks: Story 011

## Completion Notes
**Completed**: 2026-10-25
**Criteria**: 3/3 passing (AC-34a, AC-34b, AC-34c covered by EditMode tests in `WelcomeQueueTests.cs` / `WelcomeQueueEdgeTests.cs`)
**Harness**: unity-harness all PASS (rules, compile, EditMode 133/133); smoke n/a (no scene change)
**Run result**: N/A - pure C# logic, nothing on screen until Story 011 wires it
**Deviations**: ADVISORY - gate token owner is `step.Id` instead of `welcome.<id>` (avoids string concatenation); sorting extracted to `WelcomeStepOrder`; `WelcomeQueueCompletedEvent` carries the shown-step count
**Test Evidence**: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/WelcomeQueueTests.cs`, `WelcomeQueueEdgeTests.cs` (+ `WelcomeQueueFakes.cs`, `WelcomeQueueTestBase.cs`), passing
**Visual sign-off**: n/a (Logic)
**Code Review**: unity-dev review + 1 fix round, 0 remaining issues; 1 minor (exception in a step's Show/ShouldShow leaves the token held) logged as TD-007; LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none
