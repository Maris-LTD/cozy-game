# Story 007: Cảm biến hũ, trả thưởng & tích hợp Economy (Bucket Payout & Economy Integration)

> **Epic**: Plinko Board
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-27
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/plinko-board.md`
**Requirement**: `TR-plinko-003`, `TR-plinko-004`

**ADR Governing Implementation**: ADR-0006: World Space 2D Physics Plinko Board vs Canvas UI Separation, ADR-0005: Layered Income Pipeline
**ADR Decision Summary**: Vùng cảm biến miệng 5 hũ bắt đồng xu rơi vào và giải quyết trả thưởng tức thì; giá trị gốc baseCoinValue được đọc trực tiếp từ IBaseCoinValueSource tại thời điểm giải quyết; Plinko chỉ phát sự kiện CoinResolved và không tự ý ghi vào ví.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Hợp đồng `CoinResolvedEvent` chuyển tải đầy đủ thông tin cho Economy Core; `rawValue` tính bằng `double`.
- Forbidden: Subsystems tuyệt đối không tự ý cộng tiền vào ví (Never bypass Wallet); Plinko không gọi trực tiếp Wallet mà chỉ phát `CoinResolved`.
- Guardrails: Mỗi đồng xu chỉ được giải quyết đúng 1 lần duy nhất; không có `coinId` nào bị giải quyết trùng lặp.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Plinko/` (CozyGachapon.Core)
- Test: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoPayoutTests.cs` (CozyGachapon.Tests.EditMode)
- Data: `PlinkoConfig.cs`
- Contracts: `Assets/CozyGachapon/Scripts/Contracts/Plinko/`

## Acceptance Criteria

*From `Docs/design/gdd/plinko-board.md`, scoped to this story:*

- [ ] **AC-3 [EditMode]**: GIVEN xu Falling đi vào cảm biến hũ i, WHEN trigger xảy ra, THEN xu chuyển Resolved với `bucketIndex = i` ngay trong bước đó, không chờ xu nằm yên.
- [ ] **AC-25 [EditMode]**: GIVEN từng hũ i, một ca kẹt và một ca tràn hàng đợi, WHEN xu được giải quyết, THEN `rawValue = C.baseCoinValue * C.bucketMultipliers[i]` (hũ x5 → 5) / `* C.stuckPayoutMultiplier` / `* C.instantPayoutMultiplier` (1.9).
- [ ] **AC-26 [EditMode, Slow]**: GIVEN sim `T.simCoinCountLarge` xu có xu rơi đúng mép vách, WHEN đếm sự kiện, THEN số `CoinResolved` = số xu được nhận + số Overflow, không id nào bị giải quyết hai lần.
- [ ] **AC-32 [EditMode]**: GIVEN listener Economy giả, WHEN sim `T.simCoinCountSmall` xu, THEN mỗi xu phát `CoinResolved` đúng một lần với đủ trường `coinId`, `origin`, `bucketIndex`, `multiplier`, `rawValue`, `scoredPegHits`, `reason` (`reason ∈ {Bucket, Stuck, Overflow}`); `Σ rawValue` khớp tổng tính lại từ các trường; Plinko không ghi vào ví.
- [ ] **AC-45 [EditMode]**: GIVEN (a) không đăng ký `IBaseCoinValueSource`; (b) một nguồn giả trả 2.68 rồi đổi sang 18.68 khi xu đang rơi, WHEN một xu vào hũ x2, THEN (a) `rawValue = C.bucketMultipliers[1] * C.baseCoinValue`; (b) `rawValue = C.bucketMultipliers[1] * 18.68` (giá trị đọc lúc giải quyết, không phải lúc sinh xu); Plinko không có API ghi `baseCoinValue` và validator không cảnh báo với giá trị runtime 18.68 của nguồn.

## Implementation Notes

- Cài đặt `BucketSensorTrigger` gắn trên trigger collider tại miệng mỗi hũ trong số 5 hũ đáy `[x5][x2][x1][x2][x5]`.
- Cài đặt `PlinkoPayoutResolver`:
  - Tiếp nhận `IBaseCoinValueSource` qua dependency injection hoặc service registration. Nếu null, fallback sang `PlinkoConfig.baseCoinValue` (mặc định = 1.0 cho test/prototype).
  - Tính toán `rawValue = currentBaseCoinValue * multiplier`.
  - Tạo và phát struct `CoinResolvedEvent`.
  - Cập nhật trạng thái xu từ `Falling` sang `Resolved` và thu hồi xu về pool ngay lập tức.

## Out of Scope

- [Story 008]: Xử lý trúng Jackpot và kích hoạt loạt Bùng Xu.
- [Story 010]: Phát đầy đủ 6 sự kiện và hoàn trả đối tượng pool.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Integration
**Required evidence**: An EditMode test `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoPayoutTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 006 must be Complete
- Unlocks: Story 008

