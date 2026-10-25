# Story 005: Diagnostics, Zero-GC & Robustness

> **Epic**: Object Pooling
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/object-pooling.md` (R8 Vòng đời phiên, R9 Chẩn đoán, R10 Không cấp phát, State "Lost", Edge Cases liên quan đối tượng bị Destroy từ ngoài và đồng hồ)
**Requirement**: `TR-pooling-004`

**ADR Governing Implementation**: ADR-0003: Zero-Allocation Object Pooling (kèm ADR-0001)
**ADR Decision Summary**: Sau Ready cấm `Instantiate`/`Destroy`; mọi đường Get/Release/tái chế không cấp phát; bộ đếm chẩn đoán đọc không cấp phát để test và đo perf dùng thay cho log.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: lease có kiểm tra số thứ tự; bộ đếm chẩn đoán thay cho đọc log.
- Forbidden: không `Instantiate`/`Destroy` ngoài `Prewarm`; không static singleton; không tham chiếu tầng cao hơn.
- Guardrails: 0 B GC mỗi khung khi borrow/release.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: `GC.GetAllocatedBytesForCurrentThread()` trả 0 trong Unity Mono nên KHÔNG dùng cho AC-24 (xem Implementation Notes). Đối tượng bị Destroy từ ngoài phát hiện bằng toán tử `== null` của `UnityEngine.Object` (không dùng `?.`/`??` trên Object).

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Pooling/`: `PoolCounters.cs`, `PoolLostObjectGuard.cs`, cập nhật `ObjectPoolService.cs`/`PoolGetHandler.cs`; `PoolStats` (Contracts) đủ mười trường R9
- Test: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolRobustnessTests.cs`, `PoolZeroAllocTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: `PoolingTestTargets` (`gcTestCycles`) — ScriptableObject test, không nằm trong build chạy (xem GDD Tuning Knobs)

## Acceptance Criteria

*From Docs/design/gdd/object-pooling.md, scoped to this story:*

- [x] `AC-21 [EditMode]` **GIVEN** pool đã từng đạt `peakInUse` = `capacity`, **WHEN** trả hết rồi chạy thêm `T.gcTestCycles` chu kỳ Get/Release, **THEN** `created` = `capacity` suốt quá trình (không thu nhỏ, không tạo mới, không hủy).
- [x] `AC-22 [EditMode]` **GIVEN** một đối tượng Free bị Destroy từ bên ngoài, **WHEN** Get đủ `capacity` lần, **THEN** đối tượng bị hủy không bao giờ được trả ra, `lost` = 1, một lỗi Editor, Get thứ `capacity` áp chính sách cạn của pool, `created` không đổi.
- [x] `AC-23 [EditMode]` **GIVEN** đồng hồ test đứng yên (giả lập Paused) hoặc chạy nhanh 1 giờ, **WHEN** thực hiện cùng chuỗi Get/Release/tái chế, **THEN** kết quả (đối tượng trả ra, victim, bộ đếm) giống hệt nhau — Pooling không phụ thuộc thời gian.
- [x] `AC-24 [EditMode]` **GIVEN** dịch vụ Ready, một pool Fixed và một pool RecycleOldest, cảnh báo cạn đầu tiên của pool Fixed đã được ghi, **WHEN** chạy `T.gcTestCycles` chu kỳ gồm Get, Release, Get qua tái chế, Get rỗng khi Fixed cạn và đọc mọi bộ đếm, đo bằng `GC.GetAllocatedBytesForCurrentThread()` trước và sau, **THEN** chênh lệch = 0 B. (Các đường lỗi luôn ghi log Editor — trả kép, trả lạ — nằm ngoài phép đo vì log Editor tự cấp phát; trong build chúng chỉ tăng bộ đếm.)

## Implementation Notes

- Namespace `CozyGachapon.Pooling` (hoặc con) cho mọi kiểu Pooling (GDD AC-32).
- Bộ đếm R9 đủ mười trường trong `PoolStats`: `capacity`, `created`, `inUse`, `peakInUse`, `exhausted`, `recycled`, `doubleRelease`, `staleRelease`, `foreignRelease`, `lost`. `TryGetStats(string id, out PoolStats stats)` đọc không cấp phát; rà soát lại các story trước đã cập nhật đủ bộ đếm tương ứng.
- Không bao giờ thu nhỏ/tạo mới/hủy sau Ready (AC-21): chu kỳ Get/Release lặp lại `gcTestCycles` lần giữ `created` = `capacity`. Test dựng `peakInUse` = `capacity` trước rồi trả hết.
- Lost (AC-22): khi chọn đối tượng Free, nếu đối tượng `== null` (bị Destroy từ ngoài) thì bỏ khỏi pool, `lost` +1, một lỗi qua `IPoolDiagnosticsSink`, không tạo bù, dung lượng thực giảm 1; Get tiếp tục tìm đối tượng khác hoặc áp chính sách cạn khi hết. Đối tượng Lost không bao giờ được trả ra. Lưu ý: kiểm tra null Unity trong EditMode với đối tượng đã `DestroyImmediate` phải dùng `DestroyImmediate` trong test (Destroy thường trì hoãn).
- Không phụ thuộc thời gian (AC-23): dịch vụ không tham chiếu `Time`, `DateTime`, `Stopwatch`, timer. Test dùng đồng hồ giả/chuỗi thao tác giống hệt trong hai kịch bản và so sánh đối tượng trả ra, victim và bộ đếm; xác nhận bằng cách không có phụ thuộc thời gian trong Foundation/Pooling (có thể kiểm bằng reflection/grep trong test hoặc review).
- AC-24 đo không cấp phát: `GC.GetAllocatedBytesForCurrentThread()` trả 0 trong Unity Mono, nên dùng nguyên văn GDD sẽ cho kết quả "0 B" vô nghĩa. Test PHẢI dùng cách đo thực sự thấy cấp phát: `UnityEngine.Profiling.Recorder.Get("GC.Alloc")` bật trong khối đo (đọc `sampleBlockCount`/`elapsedNanoseconds`), hoặc `Profiler.GetMonoUsedSizeLong()` trước/sau với khởi động đệm (warm-up một chu kỳ trước khi đo), hoặc chứng minh cấu trúc (không `new`/boxing/closure/delegate tạo mới trên đường nóng, kiểm bằng review và test mã nguồn). Bắt buộc có đối chứng: một biến thể cố ý cấp phát (ví dụ tạo `new object[16]` hoặc nối chuỗi trong vòng đo) làm phép đo báo > 0, để chứng minh test có thể fail. Ghi phương pháp đo đã chọn và kết quả đối chứng vào Completion Notes.
- Chu kỳ đo của AC-24 gồm: Get, Release, Get qua tái chế (pool RecycleOldest), Get rỗng khi Fixed cạn (sau cảnh báo đầu đã ghi trước khi đo), đọc mọi bộ đếm. Các đường lỗi (trả kép, trả lạ) ở ngoài phép đo.
- `T.gcTestCycles` đọc từ `PoolingTestTargets` (test-only), không hardcode trong logic.
- Dự án: không comment, không `Debug.Log*` (lỗi/cảnh báo qua `IPoolDiagnosticsSink`; sink giả của test không được cấp phát khi đo — dùng bộ đếm số nguyên), không LINQ, file ≤ 250 dòng.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 006]: Hủy phiên khi nạp lại scene (AC-31), kiểm namespace và static (AC-32).
- AC-25 [Perf]: đo "GC Allocated In Frame" 600 khung trên WebGL với kịch bản Plinko — hoãn tới đợt perf sau, cần Plinko Board.
- AC-26, AC-29 (cần Plinko Board), AC-27, AC-34 (cần Feedback & Juice), AC-28 (cần prefab MVP thật).

## QA Test Cases

- **AC-21**: không thu nhỏ
  - Given: pool đã đạt `peakInUse` = `capacity`, rồi trả hết
  - When: chạy `gcTestCycles` chu kỳ Get/Release
  - Then: `created` = `capacity` ở mọi thời điểm kiểm; không đối tượng nào bị tạo mới hay hủy
  - Edge cases: `gcTestCycles` đủ lớn để vòng qua toàn bộ đối tượng Free nhiều lần
- **AC-22**: đối tượng Lost
  - Given: một đối tượng Free bị `DestroyImmediate` từ ngoài
  - When: Get đủ `capacity` lần
  - Then: đối tượng hủy không bao giờ được trả ra; `lost` = 1; sink nhận đúng 1 lỗi; Get thứ `capacity` áp chính sách cạn (Fixed trả rỗng / RecycleOldest tái chế); `created` không đổi
  - Edge cases: hủy hai đối tượng → `lost` = 2; hủy đối tượng đang mượn không do Story này xử lý (không nằm trong AC)
- **AC-23**: không phụ thuộc thời gian
  - Given: hai lần chạy cùng một chuỗi Get/Release/tái chế, một với đồng hồ test đứng yên, một với đồng hồ chạy nhanh 1 giờ
  - When: ghi đối tượng trả ra, victim và toàn bộ bộ đếm
  - Then: hai chuỗi kết quả giống hệt nhau
  - Edge cases: chuỗi có tái chế nhiều vòng để seq lệch nhau nhiều bước
- **AC-24**: không cấp phát
  - Given: dịch vụ Ready, một pool Fixed và một pool RecycleOldest, cảnh báo cạn đầu của Fixed đã ghi, một chu kỳ warm-up đã chạy
  - When: chạy `gcTestCycles` chu kỳ Get, Release, Get qua tái chế, Get rỗng khi Fixed cạn, đọc mọi bộ đếm, đo bằng Recorder `GC.Alloc` hoặc phương pháp đã chọn (không dùng `GC.GetAllocatedBytesForCurrentThread`)
  - Then: chênh lệch cấp phát = 0 B
  - Edge cases: test đối chứng cố ý cấp phát trong cùng khung đo phải cho kết quả > 0 (chứng minh test có thể fail); đường lỗi trả kép/trả lạ ngoài phép đo

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolRobustnessTests.cs`, `PoolZeroAllocTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] `PoolRobustnessTests.cs`, `PoolTimeIndependenceTests.cs`, `PoolZeroAllocTests.cs` (EditMode 246/246)

## Dependencies

- Depends on: Story 004
- Unlocks: Story 006

## Completion Notes
**Completed**: 2026-10-25
**Criteria**: 4/4 passing (AC-21..AC-24 covered by EditMode tests)
**Harness**: unity-harness all PASS (rules, compile, EditMode 246/246); smoke PASS in workflow run (0 console errors)
**Run result**: N/A - pure pooling logic, no pool registered in MainGame yet
**Deviations**: ADVISORY - AC-24 measured with `UnityEngine.Profiling.Recorder("GC.Alloc")` sample count (`GcAllocProbe`) instead of `GC.GetAllocatedBytesForCurrentThread` (returns 0 in Unity Mono); controls `AC24_Control_DeliberateAllocationInsideMeasuredBlock_ReportsAllocations` and `AC24_Control_AllocatingHookOnPoolPath_ReportsAllocations` report > 0 and pass, zero-alloc runs with and without hooks report 0; AC-23 evidence is the reflection and source-scan tests (no time types in `CozyGachapon.Pooling`), the frozen/fast-forward trace test is a determinism check since the service takes no clock; `PoolingTestTargets` lives in the test assembly (not shipped)
**Test Evidence**: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolRobustnessTests.cs`, `PoolTimeIndependenceTests.cs`, `PoolZeroAllocTests.cs` (+ `GcAllocProbe.cs`, `AllocatingPoolHooks.cs`, `CountingPoolHooks.cs`, `PoolingTestTargets.cs`), passing
**Visual sign-off**: n/a (Logic)
**Code Review**: unity-dev review, 3 minor: zero-alloc case with non-allocating hooks added; probe validity confirmed by passing controls; trace-test naming noted above; LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none
