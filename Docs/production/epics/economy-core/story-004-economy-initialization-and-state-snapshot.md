# Story 004: Economy Core Initialization & State Snapshot

> **Epic**: Economy Core
> **Status**: Ready
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R1.1–R1.4, R12.1, R16.3 phần `Run()`, Dependencies hàng Save/Load, Edge Cases "save chứa `currencyId` chưa đăng ký / số dư âm, NaN, vượt trần")
**Requirement**: `TR-economy-001`, `TR-economy-004`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format (kèm ADR-0001)
**ADR Decision Summary**: `Wallet` và bộ tính giá là C# thuần ở `CozyGachapon.Core`; số dư `double` không âm; giá quay theo `100 * 1.05^n`.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: số dư `double`; logic miền là C# thuần; tham số trong ScriptableObject; không phụ thuộc tầng cao hơn.
- Forbidden: không static singleton; không `int`/`float` cho tiền; Contracts không chứa logic.
- Guardrails: 0 B GC ở truy vấn; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không dùng `JsonUtility` ở story này; GDD ghi `JsonUtility` không serialize Dictionary nên trạng thái lưu là mảng cặp id/giá trị (Economy chỉ cung cấp cấu trúc dữ liệu, Save/Load sở hữu file).

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Contracts/Economy/` (`CurrencyBalanceEntry.cs`, `EconomyStateData.cs`, `ReleaseTier.cs`); `Assets/CozyGachapon/Scripts/Core/Economy/` (`EconomyCore.cs`, `EconomyStateRestorer.cs`, `BaseCoinValueCalculator.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/EconomyInitializationTests.cs`, `EconomyStateSnapshotTests.cs`
- Data: `EconomyConfig.currencies`, `startingCoins`, `walletSafeCap`, `CurrencyDefinition.releaseTier` (Story 001)

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [ ] `AC-1 [EditMode]` **GIVEN** `C` mặc định và game mới, **WHEN** khởi tạo, **THEN** chỉ các tiền tệ trong `C.currencies` có `releaseTier` ≤ bản build được đăng ký (MVP: đúng `currency.coin`), Xu = `C.startingCoins` (0), `spinsThisShop` = 0, `baseCoinValue` = 1, giá quay = `C.spinBasePrice` (100).
- [ ] `AC-8 [EditMode]` **GIVEN** Xu = 0.1, Tem = 7, một id lạ `currency.future` = 3, `spinsThisShop` = 37 (VS), **WHEN** lưu rồi nạp lại, **THEN** mọi giá trị bằng nhau tuyệt đối (kể cả 1e300 trong lần chạy thứ hai), `currency.future` vẫn còn trong dữ liệu lưu lần sau; dữ liệu nạp có Xu = −5 / NaN / 1e306 được kẹp thành 0 / 0 / `C.walletSafeCap`.

## Implementation Notes

- `EconomyCore` là lớp chủ (C# thuần, không MonoBehaviour) ghép `WalletService`, `SpinPriceCurve`, bộ đếm `spinsThisShop` (n) và `baseCoinValue` đã cache. `Initialize(EconomyConfig config, ReleaseTier buildTier, EconomyStateData loaded)` đăng ký tiền tệ theo `releaseTier` ≤ `buildTier`, đặt Xu = `startingCoins` khi game mới (`loaded` rỗng/null), các tiền tệ khác = 0, n = 0, giá quay và `baseCoinValue` tính sẵn. Trước `Initialize` mọi API trả `false`/giá trị an toàn (trạng thái Uninitialized đầy đủ ở Story 013/018).
- `BaseCoinValueCalculator` (thuần): `base_coin_value(n) = (RawPrice(n) / spinBasePrice)^coinValueExponent`, luôn hữu hạn và ≥ 1. Tạo ở đây vì AC-1 cần giá trị khởi đầu; giá trị theo n được kiểm bằng AC-33 ở Story 005.
- "Bản build được đăng ký" (`buildTier`) là tham số của `Initialize`: GDD chỉ nói `releaseTier` ≤ bản build, không nói giá trị lấy ở đâu (Open Question của EPIC). Giả định: composition root truyền `ReleaseTier.Mvp`; enum `ReleaseTier { Mvp, VerticalSlice, Alpha }`.
- Trạng thái lưu (R1, Dependencies "Save/Load"): `EconomyStateData` gồm mảng `CurrencyBalanceEntry { currencyId, amount }` và `spinsThisShop`; không dùng `Dictionary` trong dữ liệu lưu. `EconomyCore.ExportState()` trả bản chụp gồm cả cặp id chưa đăng ký đã nạp trước đó (giữ nguyên để không mất khi nâng cấp, Edge Case); `Initialize(..., loaded)` nạp lại. Economy không đọc/ghi file: Save/Load (VS) ngoài epic; test vòng lưu–nạp bằng bộ nhớ.
- Kẹp khi nạp (Edge Case): số dư âm → 0, NaN → 0, > `walletSafeCap` hoặc +∞ → `walletSafeCap`; `spinsThisShop` âm → 0. Giá trị hợp lệ giữ nguyên tuyệt đối (so sánh bằng, không dùng sai số), kể cả 1e300 và 0.1.
- Game mới: R12.1 (Xu = `startingCoins`, n = 0, `baseCoinValue` = 1, không modifier). Nạp save có `spinsThisShop` = 37 thì giá quay và `baseCoinValue` tính theo n = 37 ngay sau `Initialize` (cần cho AC-33(d), Story 005).
- Quy tắc dự án: không comment, không `Debug.Log*` (báo clamp qua sink: `LoadedBalanceClamped`), không LINQ, file ≤ 250 dòng (tách `EconomyStateRestorer`), SRP, không static singleton.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 005]: `TryPaySpin`, `ResetForNewShop`, `IBaseCoinValueSource`, sự kiện `SpinPriceChanged`.
- [Story 013]: các trạng thái Ready/Running/Paused và cộng Idle.
- [Story 018]: `IBootStep` `economy.core`, đọc `EconomyConfig` thiếu `currency.coin` → `IsFailed`.
- Save/Load (VS): định dạng file, version, thời điểm gọi lưu.

## QA Test Cases

- **AC-1**: khởi tạo game mới
  - Given: `EconomyConfig` mặc định, `buildTier` = Mvp, config có thêm `currency.shop_stamp` (tier Alpha)
  - When: `Initialize` không có dữ liệu nạp
  - Then: chỉ `currency.coin` được đăng ký; Xu = 0; n = 0; `baseCoinValue` = 1; giá quay = 100
  - Edge cases: `startingCoins` = 50 → Xu = 50; Tem không truy vấn được ở MVP (truy vấn trả không đăng ký)
- **AC-8**: lưu rồi nạp
  - Given: `buildTier` = Alpha, Xu = 0.1, Tem = 7, `currency.future` = 3 (nạp từ dữ liệu), n = 37
  - When: `ExportState`, `Initialize` lại từ dữ liệu đó (lặp với Xu = 1e300); sau đó chạy ở `buildTier` = Mvp và xuất lần nữa
  - Then: mọi giá trị bằng nhau tuyệt đối; `currency.future` còn trong dữ liệu xuất lần sau; dữ liệu nạp có Xu = −5 / NaN / 1e306 thành 0 / 0 / `walletSafeCap`
  - Edge cases: Tem = 7 giữ nguyên ở MVP dù không đăng ký; `spinsThisShop` âm → 0; mảng rỗng

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/EconomyInitializationTests.cs`, `EconomyStateSnapshotTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 001, Story 002, Story 003
- Unlocks: Story 005, Story 007, Story 009, Story 011, Story 013, Story 016
