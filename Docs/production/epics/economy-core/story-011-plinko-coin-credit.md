# Story 011: Plinko Coin Credit (`CoinResolved` → Wallet)

> **Epic**: Economy Core
> **Status**: Ready
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R5.1, R5.4, R15 `IncomeCredited`, Formulas `plinko_credit`, Game Feel "cùng khung", Edge Cases "hàng chục xu một khung")
**Requirement**: `TR-economy-002`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format
**ADR Decision Summary**: mỗi khoản thu Plinko đi qua pipeline phân tầng rồi mới `Credit` vào ví; Plinko Board là upstream qua `CoinResolved`, không nhận ghi từ Economy.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: mọi thu nhập vào ví qua pipeline; số `double`; `IncomeCredited` đúng một lần mỗi khoản.
- Forbidden: không tính lại `rawValue`; không bypass ví; không static singleton; Core không tham chiếu Plinko cài đặt.
- Guardrails: 0 B GC mỗi `CoinResolved`; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có API sau cutoff; Plinko giả trong test, không phụ thuộc Plinko Board thật.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Contracts/Plinko/` (`CoinResolvedEvent.cs`, `CoinOrigin.cs`, `CoinResolveReason.cs`; Plinko sở hữu, xem Open Questions của EPIC); `Assets/CozyGachapon/Scripts/Contracts/Economy/` (`IncomeCreditedEvent.cs`); `Assets/CozyGachapon/Scripts/Core/Economy/` (`PlinkoCreditHandler.cs`, `PlinkoCreditCalculator.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/PlinkoCreditTests.cs`, `FakePlinkoSource.cs`
- Data: `EconomyConfig.plinkoFlatPerCoinCap` (Story 001)

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [ ] `AC-15 [EditMode]` **GIVEN** modifier đã biết, **WHEN** Plinko giả phát `CoinResolved` cho mỗi tổ hợp origin {Manual, Auto, Rain, JackpotBurst} × reason {Bucket, Stuck, Overflow}, **THEN** mỗi sự kiện cộng đúng `plinko_credit`, phát đúng một `IncomeCredited` với `baseAmount` = rawValue, `coinId`, `bucketIndex` (kể cả none), origin và reason sao chép nguyên, và `creditedAmount` = delta của ví; **WHEN** `SubmitIncome(Plinko, shelf.slot1, 10)`, **THEN** `IncomeCredited` có `coinId` = none và `bucketIndex` = none.
- [ ] `AC-17 [EditMode]` **GIVEN** listener đếm sự kiện, **WHEN** `CoinResolved` được xử lý, **THEN** khi lệnh xử lý trả về, số dư đã tăng và `IncomeCredited` + `WalletChanged` đã phát trong cùng lời gọi (trễ 0 khung).
- [ ] `AC-18 [EditMode]` **GIVEN** n = 100, Synergy kênh Plinko +0.5, Trà +1.0, **WHEN** xu vào hũ x5 (rawValue = 5 × `base_coin_value(100)`), **THEN** credit = 280.2 (± 0.1); thêm cộng phẳng `plinko.coin` 0.4 → 302.6 (± 0.1); cộng phẳng 10 → dùng `C.plinkoFlatPerCoinCap` (2.0).

## Implementation Notes

- Hợp đồng `CoinResolved` theo GDD (R5.1): `{ coinId, origin, bucketIndex | none, multiplier, rawValue, scoredPegHits, reason }`, `origin ∈ {Manual, Auto, Rain, JackpotBurst}`, `reason ∈ {Bucket, Stuck, Overflow}`. Plinko Board sở hữu hợp đồng; vì epic Plinko chưa làm, định nghĩa tối thiểu đặt ở `Contracts/Plinko/` để Plinko epic nhận lại (không đổi tên trường). `bucketIndex | none` biểu diễn bằng `int` với −1 = none hoặc `bool HasBucket` (chọn một, ghi vào Completion Notes). `IncomeCredited` mang `{ channel, coinId, bucketIndex, origin, reason, sourceKey, baseAmount, creditedAmount }`; từ `SubmitIncome` thì `coinId` và `bucketIndex` là none (Story 012).
- `PlinkoCreditHandler.Handle(in CoinResolvedEvent)` xử lý **mọi** origin và **mọi** reason qua kênh Plinko với `rawValue` làm giá trị gốc; không tính lại `rawValue` (R5.1). Credit theo `plinko_credit`: `(rawValue + min(flat_units_coin, plinkoFlatPerCoinCap) × baseCoinValue) × M_Synergy(plinko.coin) × M_Boost(Plinko) × M_Prestige(Plinko) × M_Global(Plinko)`. `flat_units_coin` = Σ sanitize(v) tầng Base nhắm `plinko.coin`. `baseCoinValue` đọc từ giá trị cache hiện tại của Story 005 tại thời điểm giải quyết.
- Cộng vào ví ngay trong cùng lời gọi: số dư tăng, `WalletChanged` rồi `IncomeCredited` phát trước khi hàm trả về (R5.4, AC-17, Game Feel 0 khung). `IncomeCredited` phát đúng một lần mỗi khoản, `creditedAmount` bằng delta của ví (số bay "+n"). Trường hợp credit bằng 0 (hũ hệ số 0) vẫn phát `IncomeCredited` với 0, không phát `WalletChanged` vì `Add` từ chối amount 0 (GDD không quy định rõ, ghi vào Completion Notes); trường hợp ví đã ở `walletSafeCap` thì `creditedAmount` là delta thực.
- `rawValue` âm, NaN hoặc vô cực: bỏ qua khoản, không cộng ví, không `IncomeCredited` (kiểm ở Story 012 theo AC-19, nhưng logic nằm trong handler này). Cùng một xu phát hai lần: cộng cả hai (Plinko chặn trùng, Edge Case).
- Hàng chục xu trong một khung: mỗi xu cộng riêng và phát `IncomeCredited` riêng; tổng delta ví = tổng `creditedAmount`.
- Handler không phụ thuộc trạng thái Running (xử lý ở Ready và Paused, Story 013 kiểm AC-49); story này không gắn điều kiện trạng thái. `plinko_rate_avg` ghi nhận credit ở Story 015: handler phát tín hiệu nội bộ để Story 015 móc vào, không tự tính tốc độ.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng, SRP, không static singleton, không cấp phát (struct sự kiện, không boxing).

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 012]: `SubmitIncome`, đánh giá modifier tại thời điểm giải quyết (AC-16), từ chối khoản không hợp lệ (AC-19). Ca `SubmitIncome` của AC-15 chạy khi Story 012 xong; test story này chỉ chạy phần `CoinResolved`.
- [Story 013]: hành vi theo trạng thái (Ready/Paused, AC-49).
- [Story 015]: `PlinkoRateAvg`, `IncomeRatesChanged`, trần mềm (AC-47).
- Plinko Board (epic khác): sinh `CoinResolved` thật, đọc `IBaseCoinValueSource` lúc giải quyết, AC-34.

## QA Test Cases

- **AC-15**: mọi origin × reason
  - Given: modifier đã biết (ví dụ Synergy kênh Plinko +0.5, Trà +1.0), n = 100, Plinko giả
  - When: phát `CoinResolved` cho 4 origin × 3 reason (kể cả `bucketIndex` none); sau đó `SubmitIncome(Plinko, shelf.slot1, 10)` khi Story 012 sẵn sàng
  - Then: mỗi sự kiện cộng đúng `plinko_credit`, một `IncomeCredited` có `baseAmount` = rawValue, `coinId`, `bucketIndex`, origin, reason sao chép nguyên, `creditedAmount` = delta ví; với `SubmitIncome`: `coinId` và `bucketIndex` = none
  - Edge cases: `Overflow` có `bucketIndex` none; delta ví bằng `creditedAmount` kể cả khi bị kẹp tại trần
- **AC-17**: trễ 0 khung
  - Given: listener đếm `IncomeCredited` và `WalletChanged`, ví Xu = 0
  - When: gọi `Handle` một `CoinResolved`
  - Then: khi hàm trả về số dư đã tăng và cả hai sự kiện đã phát đúng một lần
  - Edge cases: 24 xu liên tiếp trong cùng lời gọi vòng lặp, tổng delta = Σ `creditedAmount`
- **AC-18**: công thức credit
  - Given: n = 100 (`baseCoinValue` 18.679), Synergy kênh Plinko +0.5, Trà +1.0
  - When: xu hũ x5 (rawValue 93.40); thêm cộng phẳng 0.4; thay bằng cộng phẳng 10
  - Then: 280.2 (± 0.1); 302.6 (± 0.1); cộng phẳng 10 kẹp bằng `plinkoFlatPerCoinCap` (2.0) → (93.40 + 2.0 × 18.679) × 3
  - Edge cases: cộng phẳng âm/NaN = 0; rawValue 0 → credit 0 vẫn có `IncomeCredited`

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/PlinkoCreditTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 004, Story 005, Story 007
- Unlocks: Story 012, Story 013, Story 014, Story 015
