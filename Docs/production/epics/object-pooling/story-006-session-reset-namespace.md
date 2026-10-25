# Story 006: Session Reset & Namespace Boundary

> **Epic**: Object Pooling
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/object-pooling.md` (R8.4, R8.5 Nạp lại scene kết thúc phiên Pooling, R3.3 đăng ký một lần mỗi phiên, States and Transitions "Accepting")
**Requirement**: `TR-pooling-004`, `TR-pooling-001`

**ADR Governing Implementation**: ADR-0003: Zero-Allocation Object Pooling (kèm ADR-0001, ADR-0002)
**ADR Decision Summary**: Không static singleton, phụ thuộc tiêm qua composition root; dịch vụ thuộc scene `MainGame` và được dựng mới ở mỗi `Run()` của `pooling.service`, kể cả sau "Thử lại".
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: dịch vụ khởi tạo qua `IBootStep`; interface/enum/struct ở `CozyGachapon.Contracts`; logic ở `CozyGachapon.Foundation`.
- Forbidden: không static singleton / `FindObjectOfType` / `GameObject.Find`; không tham chiếu tầng cao hơn; không `Instantiate`/`Destroy` ngoài `Prewarm`.
- Guardrails: không trạng thái Pooling nào sống qua nạp lại scene khi domain không reload.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: Enter Play Mode Options có thể tắt domain reload nên phiên mới không được phụ thuộc reload domain; mô phỏng nạp lại scene trong EditMode bằng hủy dịch vụ và container rồi chạy lại `Run()` của `pooling.service`.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Pooling/`: `PoolSession.cs` (gom dịch vụ + container + bộ đếm của một phiên, có `Dispose`/`Teardown`), cập nhật `PoolingServiceBootStep.cs` (tạo phiên mới mỗi `Run()`), `PoolContainer.cs`
- Test: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolSessionResetTests.cs`, `PoolNamespaceBoundaryTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: không có knob mới

## Acceptance Criteria

*From Docs/design/gdd/object-pooling.md, scoped to this story:*

- [x] `AC-31 [EditMode]` **GIVEN** ba ca của phiên cũ — dịch vụ ở Accepting với `plinko.coin` đã đăng ký; ở Prewarming sau một bước prewarm; ở Ready với `plinko.coin` có đối tượng đang mượn và `exhausted` > 0 — **WHEN** mô phỏng nạp lại scene (huỷ dịch vụ cùng mọi đối tượng pool, rồi chạy lại `Run()` của `pooling.service`) và đăng ký lại `plinko.coin` với cùng tham số, **THEN** ở cả ba ca: dịch vụ mới ở Accepting, đăng ký trả thành công, không có lỗi Editor "trùng `id`", trước prewarm `created` = 0 và mọi bộ đếm R9 khác (`inUse`, `peakInUse`, `exhausted`, `recycled`, `doubleRelease`, `staleRelease`, `foreignRelease`, `lost`) = 0; sau prewarm `created` = `capacity` và Get đầu tiên mang `acquire_seq` = 1; hook *trả về* của phiên cũ được gọi 0 lần.
- [x] `AC-32 [EditMode]` **GIVEN** tập kiểu Pooling theo R8.5 — mọi kiểu (kể cả kiểu lồng, kiểu generic) có `Type.Namespace` bằng `CozyGachapon.Pooling` hoặc bắt đầu bằng `CozyGachapon.Pooling.`, lấy từ assembly chứa kiểu dịch vụ Pooling (hiện `CozyGachapon.Runtime`; asmdef Pooling riêng nếu ADR D1 tách), bỏ qua kiểu mang `[CompilerGenerated]` (lớp cache lambda `<>c` với field `<>9`, `<>9__N_M`; lớp closure `<>c__DisplayClass…`) và field mang `[CompilerGenerated]`, **trừ** backing field của auto-property static (`<…>k__BackingField`) và của event static dạng field — hai loại này vẫn bị kiểm; tập có ≥ 1 kiểu, **WHEN** duyệt bằng reflection field của từng kiểu với `BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly`, **THEN** mọi field là `const` (`FieldInfo.IsLiteral`) hoặc `static readonly` (`FieldInfo.IsInitOnly`) có `FieldType` thuộc danh sách cho phép — kiểu số nguyên thủy, `bool`, `char`, `string`, enum, `Unity.Profiling.ProfilerMarker` — và số field còn lại = 0; test in tên kiểu + tên field của từng vi phạm. Không có trạng thái Pooling nào sống qua lần nạp lại scene khi domain không reload (R8.5).

## Implementation Notes

- Namespace: mọi kiểu Pooling phải ở `CozyGachapon.Pooling` hoặc namespace con — GDD AC-32 yêu cầu tường minh. Story này rà lại toàn bộ kiểu của Story 001-005 để thỏa. Assembly chứa kiểu dịch vụ hiện là `CozyGachapon.Foundation` (theo ADR-0001), không phải `CozyGachapon.Runtime` như chữ trong AC-32; test lấy assembly bằng `typeof(ObjectPoolService).Assembly` và ghi chú lệch tên assembly này vào Completion Notes (GDD cần cập nhật tên asmdef).
- AC-32 duyệt reflection với `BindingFlags.Static | Public | NonPublic | DeclaredOnly` cho mọi kiểu trong assembly có namespace khớp, kể cả kiểu lồng và generic; bỏ kiểu và field `[CompilerGenerated]` trừ backing field của auto-property static và event static; field hợp lệ là `const` hoặc `static readonly` kiểu số, `bool`, `char`, `string`, enum, `Unity.Profiling.ProfilerMarker`. Test in `Type.FullName` + tên field cho mỗi vi phạm, và khẳng định tập có ≥ 1 kiểu. Hệ quả thiết kế: không có `static` mutable, không static event, không static cache trong Pooling; hook/delegate là field instance.
- Phiên Pooling = một `PoolSession` được `PoolingServiceBootStep.Run()` tạo mới mỗi lần chạy (sổ đăng ký rỗng, bộ đếm = 0, `acquire_seq` bắt đầu lại, cờ "đã cảnh báo cạn" xóa, mọi phiếu cũ bị bỏ). Teardown hủy container và mọi đối tượng pool; hook *trả về* KHÔNG được gọi khi teardown. Prewarm dở của phiên cũ bị bỏ, phiên mới prewarm lại từ đầu.
- Mô phỏng nạp lại scene trong EditMode: `Teardown()` phiên cũ (hủy container bằng `DestroyImmediate`) rồi tạo `PoolingServiceBootStep` mới và gọi `Run()`; không dựa vào reload domain. Đây là chỗ duy nhất được phép hủy đối tượng pool ngoài Prewarm (teardown theo scene).
- Chỉ kiểm điều kiện trên ba trạng thái phiên cũ (Accepting có `plinko.coin`; Prewarming sau một bước; Ready có đối tượng đang mượn và `exhausted` > 0) — Story 002/003/004 cung cấp trạng thái.
- Không thêm trạng thái Pooling ngoài scene (không static, không `DontDestroyOnLoad`).
- Dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 001-005]: Đăng ký, prewarm, Get/Release, chính sách cạn, bộ đếm, zero-GC.
- Hoãn sang epic khác hoặc đợt sau (liệt kê đầy đủ phần GDD chưa thuộc epic này):
  - AC-10 [Perf] và AC-25 [Perf]: đo WebGL trên thiết bị tham chiếu — hoãn tới đợt perf sau.
  - AC-26, AC-29 và AC-33: cần Plinko Board (xu thật, tap-to-spawn, kịch bản "Thử lại" Play mode trên `MainGame`).
  - AC-27 và AC-34: cần Feedback & Juice (`feedback.floatingText`, `feedback.coinFizzle`).
  - AC-28: cần prefab pool MVP thật từ Plinko Board và Feedback & Juice.

## QA Test Cases

- **AC-31**: phiên mới sạch sau nạp lại
  - Given: ba ca phiên cũ — (a) Accepting với `plinko.coin` đã đăng ký; (b) Prewarming sau đúng một bước prewarm; (c) Ready với `plinko.coin` có đối tượng đang mượn và `exhausted` > 0; hook trả về giả đếm lời gọi
  - When: teardown phiên cũ (hủy dịch vụ và đối tượng), chạy lại `Run()` của `pooling.service`, đăng ký lại `plinko.coin` cùng tham số, prewarm xong, Get một lần
  - Then: cả ba ca — dịch vụ ở Accepting; đăng ký thành công; sink không có lỗi "trùng `id`"; trước prewarm `created` = 0 và `inUse`, `peakInUse`, `exhausted`, `recycled`, `doubleRelease`, `staleRelease`, `foreignRelease`, `lost` = 0; sau prewarm `created` = `capacity`; Get đầu tiên có `acquire_seq` = 1; hook trả về của phiên cũ = 0 lần gọi
  - Edge cases: cờ "đã cảnh báo cạn" của phiên cũ không thừa kế (cạn lần đầu ở phiên mới lại báo một cảnh báo); phiếu mượn của phiên cũ Release vào phiên mới bị coi là lạ/stale, không exception
- **AC-32**: ranh giới namespace và field static
  - Given: assembly chứa kiểu dịch vụ Pooling; tập kiểu có namespace `CozyGachapon.Pooling` hoặc con (kể cả lồng, generic, bỏ `[CompilerGenerated]` trừ backing field static/event static)
  - When: duyệt reflection field static với `Public | NonPublic | DeclaredOnly`
  - Then: tập ≥ 1 kiểu; mọi field là `const` hoặc `static readonly` thuộc danh sách kiểu cho phép; số field còn lại = 0; tên kiểu + field của vi phạm được in
  - Edge cases: test có ca đối chứng dùng một kiểu giả có field `static` mutable để chứng minh bộ kiểm có thể fail; auto-property static và event static vẫn bị kiểm

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolSessionResetTests.cs`, `PoolNamespaceBoundaryTests.cs`, passing in `unity-harness tests`

**Evidence**: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolSessionResetTests.cs`, `PoolSessionResetEdgeTests.cs`, `PoolNamespaceBoundaryTests.cs` (checker: `PoolStaticFieldChecker.cs`, control fakes: `PoolStaticFieldFakes.cs`) — pending `unity-harness tests` run

## Completion Notes

- AC-32 lấy assembly bằng `typeof(ObjectPoolService).Assembly` = `CozyGachapon.Foundation` (chữ AC ghi `CozyGachapon.Runtime`); GDD cần cập nhật tên asmdef.
- Sửa vi phạm AC-32 có sẵn: bỏ `static readonly string[] NoDependencies` ở `PoolingServiceBootStep` và `PoolingPrewarmBootStep`, thay bằng `Array.Empty<string>()`.
- Thêm `PoolSession` (dịch vụ + container, `Teardown()` idempotent không gọi hook trả về), `PoolObjectDisposer` (điểm hủy duy nhất theo scene), `ObjectPoolService.Shutdown()`/`IsShutDown`, `PoolContainer.DestroyAll()`; `PoolingServiceBootStep.Run()` teardown phiên trước rồi tạo phiên mới.

## Dependencies

- Depends on: Story 005
- Unlocks: Plinko Board và Feedback & Juice epics (các AC tích hợp AC-26, AC-27, AC-28, AC-29, AC-33, AC-34), đợt perf (AC-10, AC-25)

## Completion Notes
**Completed**: 2026-10-25
**Criteria**: 2/2 passing (AC-31, AC-32 incl. control cases)
**Harness**: unity-harness all PASS (rules, compile, EditMode 272/272); smoke PASS (MainGame, 0 console errors)
**Run result**: OBSERVED - smoke boots MainGame through the new per-Run `PoolSession` with 0 console errors
**Deviations**: ADVISORY - Pooling types live in assembly `CozyGachapon.Foundation` (GDD AC-32 text says `CozyGachapon.Runtime`; GDD should be updated); test takes it via `typeof(ObjectPoolService).Assembly`; two existing violations fixed (`static readonly string[] NoDependencies` in both boot steps replaced by `Array.Empty<string>()`); `PoolSession` + `PoolObjectDisposer` own teardown (destroy without return hooks); prewarm step ignores a superseded session (review fix)
**Test Evidence**: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolSessionResetTests.cs`, `PoolSessionResetEdgeTests.cs`, `PoolNamespaceBoundaryTests.cs` (+ `PoolSessionTestBase.cs`, `PoolStaticFieldChecker.cs`, `PoolStaticFieldFakes.cs`), passing
**Visual sign-off**: n/a (Logic)
**Code Review**: unity-dev review, 1 minor (prewarm step cached a stale service after a second service Run) fixed with test `PrewarmStep_AfterServiceStepRunsAgain_DoesNotReportOldSessionAsDone`; LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none
