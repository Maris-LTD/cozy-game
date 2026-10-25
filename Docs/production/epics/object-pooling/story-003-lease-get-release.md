# Story 003: Lease Get / Release Guards

> **Epic**: Object Pooling
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/object-pooling.md` (R5 Mượn, R6 Trả, States and Transitions "Trạng thái của một đối tượng", R9 Chẩn đoán)
**Requirement**: `TR-pooling-003`

**ADR Governing Implementation**: ADR-0003: Zero-Allocation Object Pooling (kèm ADR-0001)
**ADR Decision Summary**: `PoolLease` là struct chứa instance, pool id và số thứ tự tăng dần; trả phiếu đã trả hoặc hết hiệu lực bị bỏ qua và ghi nhận, không exception.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: mượn/trả bằng `PoolLease` có kiểm tra số thứ tự; hệ mượn phải trả bằng `Release(lease)` khi hết vòng đời.
- Forbidden: không `Instantiate`/`Destroy` ngoài `Prewarm`; không static singleton; không tham chiếu tầng cao hơn.
- Guardrails: 0 B GC mỗi khung khi Get/Release.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: bật/tắt đối tượng theo cách đã chọn ở Story 002; kiểm tra "Destroy từ ngoài" bằng so sánh `Object` với null (Story 005).

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Pooling/`: `PoolInstanceTable.cs`, `PoolLeaseValidator.cs`, mở rộng `ObjectPoolService.cs` (hoặc tách `PoolGetHandler.cs`, `PoolReleaseHandler.cs` để giữ ≤ 250 dòng); `Assets/CozyGachapon/Scripts/Contracts/`: `PoolLease.cs`, `IPoolHooks.cs`, `PoolReturnReason.cs`
- Test: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolLeaseTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: không có knob mới

## Acceptance Criteria

*From Docs/design/gdd/object-pooling.md, scoped to this story:*

- [ ] `AC-11 [EditMode]` **GIVEN** dịch vụ Ready, **WHEN** Get một đối tượng, **THEN** đối tượng đang hiển thị/mô phỏng, hook lấy ra được gọi đúng 1 lần trước khi Get trả về, `inUse` +1 và `peakInUse` ≥ `inUse`.
- [ ] `AC-12 [EditMode]` **GIVEN** một đối tượng đang mượn, **WHEN** Release bằng phiếu của nó, **THEN** đối tượng không hiển thị, không mô phỏng, hook trả về được gọi đúng 1 lần với lý do `Released`, `inUse` −1; dữ liệu do hệ khách gắn trên đối tượng không bị Pooling đổi.
- [ ] `AC-13 [EditMode]` **GIVEN** một phiếu đã Release, **WHEN** Release lại cùng phiếu, **THEN** `doubleRelease` +1, một lỗi Editor, `inUse` không đổi, và hai lần Get kế tiếp trả về hai đối tượng khác nhau.
- [ ] `AC-14 [EditMode]` **GIVEN** đối tượng nằm trong pool khác, một GameObject không thuộc pool nào, và một phiếu rỗng, **WHEN** Release từng cái vào pool đang test, **THEN** cả ba bị bỏ qua, `foreignRelease` +3, không exception.
- [ ] `AC-15 [EditMode]` **GIVEN** dịch vụ ở Accepting hoặc Prewarming, **WHEN** Get, **THEN** trả rỗng, một lỗi Editor, `created` không đổi.

## Implementation Notes

- Namespace `CozyGachapon.Pooling` (hoặc con) cho mọi kiểu Pooling (GDD AC-32). `PoolLease`, `IPoolHooks`, `PoolReturnReason` ở `CozyGachapon.Contracts`.
- `PoolLease` (Contracts): readonly struct theo ADR-0003 (`InstanceId`, `PoolId`, `Sequence`, `Instance`, `IsValid`). GDD đặt `acquire_seq` kiểu `long`, ADR ghi `uint`: dùng `long` theo GDD (hằng số kiểu có chủ đích) và ghi lệch vào Completion Notes.
- Hook (Contracts): `IPoolHooks` hoặc delegate đăng ký theo pool — `OnAcquire(GameObject)` gọi đúng một lần trước khi Get trả về; `OnReturn(GameObject, PoolReturnReason)` với `Released` (và `Recycled` ở Story 004). Hook tiêm lúc đăng ký/cấu hình pool; không cấp phát khi gọi (delegate/interface đã tạo sẵn).
- Get: chỉ hợp lệ khi Ready; ngược lại trả `PoolLease` rỗng, báo một lỗi qua `IPoolDiagnosticsSink`, không đổi `created`. Chọn đối tượng Free từ hàng đợi Free (mảng/stack cấp trước ở prewarm), bật đối tượng, tăng `acquire_seq` của pool, gọi hook lấy ra, tăng `inUse`, cập nhật `peakInUse`.
- Release: kiểm theo thứ tự — phiếu rỗng hoặc đối tượng không thuộc pool đang test → `foreignRelease`; phiếu đã trả (đối tượng Free) → `doubleRelease` + một lỗi sink; còn lại → tắt đối tượng, gọi hook trả về `Released`, `inUse` −1, đưa về Free. Phiếu stale (`staleRelease`) do Story 004 hoàn thiện nhưng bảng tra instance nên có sẵn `Sequence` hiện tại của từng đối tượng.
- Pooling không đổi dữ liệu riêng của hệ khách trên đối tượng (AC-12): chỉ bật/tắt hiển thị-mô phỏng.
- Phân biệt `foreignRelease` (đối tượng pool khác / GameObject lạ / phiếu rỗng) bằng bảng tra `InstanceId` → pool, không dùng `GetComponent` hay `Find`.
- "Lỗi Editor" đi qua `IPoolDiagnosticsSink`, KHÔNG dùng `Debug.Log*`; test đọc sink giả thay cho `LogAssert`. Không LINQ, không comment, file ≤ 250 dòng.
- Đọc bộ đếm không cấp phát (struct `PoolStats` qua `TryGetStats`).

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 004]: Hết đối tượng rảnh (Fixed / RecycleOldest), phiếu stale sau tái chế (AC-20).
- [Story 005]: Đối tượng bị Destroy từ ngoài (`lost`, AC-22), bất biến không cấp phát (AC-24).
- AC-26 (Plinko dùng `plinko.coin` thật) — cần Plinko Board.

## QA Test Cases

- **AC-11**: Get thành công
  - Given: dịch vụ Ready, một pool, hook lấy ra giả đếm lời gọi và ghi thứ tự
  - When: `Get(id)`
  - Then: đối tượng đang bật/mô phỏng; hook lấy ra = 1 lần và đã chạy trước khi Get trả về; `inUse` +1; `peakInUse` ≥ `inUse`
  - Edge cases: Get nhiều lần liên tiếp → `peakInUse` không giảm khi `inUse` giảm; `Sequence` tăng dần
- **AC-12**: Release hợp lệ
  - Given: một đối tượng đang mượn, hệ khách gắn một giá trị dữ liệu lên nó
  - When: `Release(lease)`
  - Then: đối tượng ẩn và không mô phỏng; hook trả về = 1 lần với lý do `Released`; `inUse` −1; giá trị dữ liệu của hệ khách nguyên vẹn
  - Edge cases: sau Release, Get lại có thể trả chính đối tượng đó với `Sequence` mới
- **AC-13**: trả kép
  - Given: một phiếu đã Release
  - When: Release lại cùng phiếu, rồi Get hai lần
  - Then: `doubleRelease` +1; sink nhận 1 lỗi; `inUse` không đổi; hai Get trả hai đối tượng khác nhau; hook trả về không bị gọi lần hai
  - Edge cases: không exception
- **AC-14**: trả lạ
  - Given: đối tượng của pool khác, một GameObject tự tạo ngoài pool, một `PoolLease` mặc định
  - When: Release từng cái vào pool đang test
  - Then: cả ba bị bỏ qua; `foreignRelease` +3; `inUse` không đổi; không exception
  - Edge cases: phiếu của pool khác (cùng cấu trúc, khác `PoolId`) cũng tính `foreignRelease`
- **AC-15**: Get sai pha
  - Given: dịch vụ ở Accepting, rồi ở Prewarming
  - When: `Get(id)`
  - Then: trả `PoolLease` rỗng (`IsValid` = false); sink nhận 1 lỗi mỗi lần; `created` không đổi
  - Edge cases: `Get` với `id` chưa đăng ký khi Ready trả rỗng, không exception

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolLeaseTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 002
- Unlocks: Story 004
