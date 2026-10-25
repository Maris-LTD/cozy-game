# Story 002: Spend, CanAfford & Player Action Context

> **Epic**: Economy Core
> **Status**: Ready
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R3 Chi tiêu, Edge Cases "Ví và chi tiêu", R15 `SpendRejected`)
**Requirement**: `TR-economy-001`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format
**ADR Decision Summary**: số dư không âm; `TrySpend` và `CanAfford` trả `false` khi thiếu tiền; hệ khác không được trừ số dư ngoài API ví.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: số dư `double`; mọi thay đổi số dư đi qua API ví.
- Forbidden: không bypass ví; không static singleton; không `int`/`float` cho tiền.
- Guardrails: 0 B GC trên `TrySpend`/`CanAfford`; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có API sau cutoff.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Contracts/Economy/` (`PlayerActionContext.cs`, `IPlayerActionIssuer.cs`, `SpendReason.cs`, `SpendRejectedEvent.cs`, mở rộng `IWallet.cs`); `Assets/CozyGachapon/Scripts/Core/Economy/` (`WalletService.cs` thêm `TrySpend`/`CanAfford`, `PlayerActionIssuer.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/WalletSpendTests.cs`
- Data: dùng asset Story 001

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [ ] `AC-5 [EditMode]` **GIVEN** Xu = 100 và có ngữ cảnh hành động, **WHEN** `TrySpend(coin, 100)`, **THEN** `true`, Xu = 0, một `WalletChanged` với delta −100; **GIVEN** Xu = 99.7, **WHEN** `TrySpend(coin, 100)`, **THEN** `false`, Xu = 99.7, đúng một `SpendRejected`; trong cả hai ca `CanAfford(coin, 100)` trước lệnh trả cùng kết quả với `TrySpend`.
- [ ] `AC-6 [EditMode]` **GIVEN** Xu = 500, **WHEN** gọi `TrySpend(coin, 100)` hoặc `ResetForNewShop` **không** có ngữ cảnh hành động, hoặc `TrySpend` với amount ∈ {−1, 0, NaN}, **THEN** trả `false` / không làm gì, Xu vẫn 500.

## Implementation Notes

- `TrySpend(string currencyId, double amount, SpendReason reason, in PlayerActionContext context)` là cách duy nhất làm giảm số dư (R3.1). `CanAfford(string currencyId, double amount)` so sánh giá trị `double` chính xác, không dùng giá trị hiển thị (R3.3, Edge Case 99.7 so với 100). Nguyên tử: số dư ≥ amount thì trừ đúng amount, trả `true`, phát `WalletChanged` (delta âm, `reason` quy đổi từ `SpendReason`); ngược lại trả `false`, giữ nguyên số dư, phát một `SpendRejected { currencyId, amount, reason }`.
- amount không hợp lệ (≤ 0, NaN, vô cực) hoặc `currencyId` chưa đăng ký: `false`, không sự kiện (R3.4). Thiếu ngữ cảnh hành động hợp lệ: `false`, không đổi số dư; có phát `SpendRejected` hay không GDD không nói (R3.2 chỉ nói `false`, số dư giữ nguyên): theo R3.3 chỉ phát khi thiếu tiền, nên thiếu ngữ cảnh không phát sự kiện, ghi vào Completion Notes.
- `ResetForNewShop` thật nằm ở Story 005. Story này chỉ dựng `PlayerActionContext` và phần kiểm ngữ cảnh dùng chung (`PlayerActionGate.IsValid`); AC-6 phần `ResetForNewShop` được chạy ở Story 005 khi API tồn tại, và Story 017 kiểm lại bằng reflection. Test của Story 002 chỉ chạy phần `TrySpend`.
- "Ngữ cảnh hành động" là điểm GDD giao cho ADR nhưng ADR-0005 chưa chốt (Open Question của EPIC). Giả định để bắt đầu: `PlayerActionContext` là `readonly struct` trong Contracts có `IsValid`; `default` luôn không hợp lệ; cách hợp lệ duy nhất để có giá trị hợp lệ là gọi `IPlayerActionIssuer.Issue(PlayerActionKind)` (cài đặt `PlayerActionIssuer` ở Core, composition root chỉ đưa issuer cho tầng UI/Gacha/Shop). Test dùng issuer thật hoặc `PlayerActionContext` do issuer test cấp. Đổi sang cơ chế khác khi ADR chốt thì chỉ sửa lớp này.
- `SpendReason` ⊂ `WalletChangeReason` của Story 001 (GachaSpin, ShopPurchase, PrestigeUpgrade, NeighborCandy); không có enum "phạt" nào.
- Hai yêu cầu chi tiêu cùng khung xử lý tuần tự (Edge Case bấm đúp): không bao giờ trừ hai lần cho một lượt; không cần khóa vì đơn luồng.
- Quy tắc dự án: không comment, không `Debug.Log*` (dùng `IEconomyDiagnosticsSink`), không LINQ, file ≤ 250 dòng, không cấp phát trên đường chi tiêu.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 005]: `TryPaySpin` và `ResetForNewShop` (hai API còn lại cần ngữ cảnh hành động), AC-6 phần `ResetForNewShop`.
- [Story 017]: reflection chứng minh chỉ ba phương thức nhận ngữ cảnh hành động (AC-7a) và hardening giá trị biên (AC-7b).

## QA Test Cases

- **AC-5**: chi tiêu nguyên tử
  - Given: Xu = 100 hoặc 99.7, ngữ cảnh hợp lệ, recorder sự kiện
  - When: `CanAfford(coin, 100)` rồi `TrySpend(coin, 100, ...)`
  - Then: ca 100: `true`, Xu = 0, một `WalletChanged` delta −100; ca 99.7: `false`, Xu = 99.7, đúng một `SpendRejected`; `CanAfford` trùng kết quả `TrySpend`
  - Edge cases: số dư bằng đúng giá (100/100) thành công còn 0; hai `TrySpend` cùng khung không trừ đôi
- **AC-6**: không ngữ cảnh / amount xấu
  - Given: Xu = 500
  - When: `TrySpend(coin, 100)` với `default(PlayerActionContext)`; `TrySpend` với amount −1, 0, NaN có ngữ cảnh
  - Then: `false`, Xu vẫn 500; không `WalletChanged`
  - Edge cases: amount +∞; `currencyId` lạ; (phần `ResetForNewShop` không ngữ cảnh chạy ở Story 005)

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/WalletSpendTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 001
- Unlocks: Story 004, Story 005, Story 017
