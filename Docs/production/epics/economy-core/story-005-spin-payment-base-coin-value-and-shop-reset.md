# Story 005: Spin Payment, Base Coin Value Source & Shop Reset

> **Epic**: Economy Core
> **Status**: Ready
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R8.3–R8.4, R9, R10, R12.2–R12.3, R15 `SpinPriceChanged`/`ShopEconomyReset`, Formulas `base_coin_value`)
**Requirement**: `TR-economy-003`, `TR-economy-004`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format
**ADR Decision Summary**: Plinko lấy giá trị xu gốc qua `IBaseCoinValueSource` do Economy cài đặt; giá quay tăng mũ theo số lượt đã trả; không hệ nào trừ số dư ngoài ví.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: `baseCoinValue` qua `IBaseCoinValueSource`; số `double`; giá trị kéo, không đẩy sang Plinko.
- Forbidden: Economy không ghi vào Plinko; không bypass ví; không static singleton.
- Guardrails: getter `BaseCoinValue` 0 B GC; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có API sau cutoff.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Contracts/Plinko/` (`IBaseCoinValueSource.cs`, đặt ở thư mục Plinko vì Plinko sở hữu interface, xem Open Questions của EPIC); `Assets/CozyGachapon/Scripts/Contracts/Economy/` (`ISpinPriceService.cs`, `SpinPriceChangedEvent.cs`, `ShopEconomyResetEvent.cs`); `Assets/CozyGachapon/Scripts/Core/Economy/` (`SpinPriceState.cs`, `ShopResetter.cs`, `BaseCoinValueSource.cs`, mở rộng `EconomyCore.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/SpinPaymentTests.cs`, `BaseCoinValueSourceTests.cs`, `ShopResetTests.cs`, `FakePlinkoReader.cs`
- Data: `EconomyConfig` (Story 001)

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [ ] `AC-30 [EditMode]` **GIVEN** Xu = 150, n = 0, **WHEN** gọi `TryPaySpin` hai lần trong cùng khung có ngữ cảnh hành động, **THEN** lần 1 `true`, Xu = 50, n = 1, `SpinPriceChanged { 1, 110, 1.0297 }`; lần 2 `false` (giá 110 > 50), Xu = 50, n = 1, một `SpendRejected`.
- [ ] `AC-33 [EditMode]` **GIVEN** α = `C.coinValueExponent`, **WHEN** tính `base_coin_value`, **THEN** n = 0 → 1, n = 1 → 1.0297, n = 100 → 18.679 (α 0.6), 11.467 (α 0.5), 30.43 (α 0.7) (sai số 1e-3 tương đối). **GIVEN** registry dịch vụ giả và Plinko giả chỉ đọc `IBaseCoinValueSource` (không có kênh ghi nào từ Economy Core), **WHEN** (a) sau `Run()` với game mới; (b) sau một `TryPaySpin` thành công; (c) sau một `TryPaySpin` thất bại; (d) sau `Run()` với dữ liệu nạp có `spinsThisShop` = 100 (VS), **THEN** `BaseCoinValue` đọc được lần lượt = 1; = `base_coin_value(1)` (1.0297); không đổi so với trước lệnh; = `base_coin_value(100)` (18.679), và Plinko giả không bao giờ dùng `PlinkoConfig.baseCoinValue` trong cả bốn ca. (Hành vi dự phòng khi không có nguồn nào là AC-45 của Plinko.)
- [ ] `AC-36 [EditMode]` **GIVEN** Xu = 5,000, Tem = 7, n = 40, một modifier `prestige.x` +1.0, **WHEN** `ResetForNewShop` có ngữ cảnh hành động, **THEN** Xu = `C.startingCoins`, Tem = 7, n = 0, giá quay = `C.spinBasePrice`, Plinko giả đọc `IBaseCoinValueSource.BaseCoinValue` = 1 ngay sau lời gọi (trước n = 40 nó đọc `base_coin_value(40)`), modifier `prestige.x` vẫn tính, đúng một `WalletChanged` (`currency.coin`, previous 5,000, current `C.startingCoins`, reason `ShopReset`), đúng một `SpinPriceChanged { 0, C.spinBasePrice, 1 }` và đúng một `ShopEconomyReset { [currency.coin], 0 }`, theo thứ tự đó.

## Implementation Notes

- `TryPaySpin(in PlayerActionContext context)` gọi `TrySpend(currency.coin, spin_price(n), GachaSpin, context)` của Story 002; thành công thì tăng n lên 1 trong cùng thao tác, tính lại giá và `baseCoinValue` đã cache, phát `SpinPriceChanged { spinsThisShop, spinPrice, baseCoinValue }`, trả `true`; thất bại thì n giữ nguyên và `SpendRejected` do `TrySpend` phát (R8.3). `GetSpinPrice()` và `CanAfford` cho Gacha đọc. Lượt quay miễn phí không có API làm tăng n (R8.4); Economy không có hàm "tăng n" nào khác ngoài `TryPaySpin` thành công và `ResetForNewShop`.
- Phát `WalletChanged` (trừ tiền) trước `SpinPriceChanged` trong `TryPaySpin`; GDD không quy định thứ tự giữa hai sự kiện này ngoài `ResetForNewShop`, ghi lựa chọn vào Completion Notes.
- `BaseCoinValueSource : IBaseCoinValueSource` trả giá trị cache `base_coin_value(n)` hiện tại, tính lại ngay trong thao tác làm n đổi (`TryPaySpin` thành công, `ResetForNewShop`, nạp trong `Initialize`). Getter không cấp phát, luôn hữu hạn và ≥ 1. Economy không ghi vào Plinko: không có input, setter hay lời gọi nào từ Economy sang Plinko (R10.2). Plinko giả trong test chỉ giữ tham chiếu `IBaseCoinValueSource` và đọc.
- `IBaseCoinValueSource` theo GDD là property `double BaseCoinValue { get; }` (Plinko sở hữu interface). ADR-0005 phác `GetCurrentBaseCoinValue()`; GDD (hợp đồng đã đối chiếu với Plinko Revision 3) thắng, ghi lệch vào Completion Notes. Story 018 đăng ký nguồn này vào registry dịch vụ; ở đây test dùng tham chiếu trực tiếp.
- `ResetForNewShop(in PlayerActionContext context)` (R12.2–R12.3): chỉ khi ngữ cảnh hợp lệ (cùng phần AC-6 còn lại: không ngữ cảnh thì không làm gì, Xu giữ nguyên, test thêm một ca cho phần này). Mọi tiền tệ có `resetsOnNewShop = true` về giá trị khởi đầu, n = 0, giá và `baseCoinValue` tính lại; Tem Tiệm và modifier tầng Prestige giữ nguyên. Phát theo thứ tự cùng khung sau khi trạng thái nhất quán: `WalletChanged(ShopReset)` cho mỗi tiền tệ reset có số dư đổi (không phát nếu bằng nhau), `SpinPriceChanged { 0, spinBasePrice, 1 }` (luôn phát kể cả n đã = 0), rồi `ShopEconomyReset { resetCurrencyIds[], spinsThisShop = 0 }`. Mảng `resetCurrencyIds` cấp sẵn tối đa số tiền tệ khi `Initialize`, không cấp phát lúc reset.
- Test AC-36 dùng cổng đăng ký modifier của Story 007 để đăng ký `prestige.x` (nên làm Story 007 trước Story 005; số thứ tự không phải thứ tự build).
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng (tách `SpinPriceState`, `ShopResetter`, `BaseCoinValueSource`), SRP, không static singleton.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 007]: cài đặt modifier và tầng Prestige (story này chỉ dùng chúng trong test).
- [Story 011]: `CoinResolved` và đọc `BaseCoinValue` lúc giải quyết xu (phía Plinko thật thuộc epic Plinko Board).
- [Story 017]: chứng minh bằng reflection chỉ `TrySpend`, `TryPaySpin`, `ResetForNewShop` nhận ngữ cảnh hành động.
- [Story 018]: đăng ký `IBaseCoinValueSource` vào registry dịch vụ trong `Run()`.
- AC-34 (Play mode với Plinko thật): xem Deferred acceptance criteria trong EPIC.

## QA Test Cases

- **AC-30**: trả tiền quay hai lần cùng khung
  - Given: Xu = 150, n = 0, ngữ cảnh hợp lệ, recorder sự kiện
  - When: `TryPaySpin` hai lần liên tiếp
  - Then: lần 1 `true`, Xu = 50, n = 1, `SpinPriceChanged { 1, 110, 1.0297 }`; lần 2 `false`, Xu = 50, n = 1, đúng một `SpendRejected`
  - Edge cases: không ngữ cảnh → `false`, n giữ nguyên; số dư đúng bằng giá thì thành công
- **AC-33**: giá trị xu gốc và nguồn kéo
  - Given: α = 0.6, 0.5, 0.7; Plinko giả giữ `IBaseCoinValueSource` của `EconomyCore`
  - When: tính `base_coin_value` ở n = 0, 1, 100; chạy các ca (a) game mới, (b) `TryPaySpin` thành công, (c) `TryPaySpin` thất bại, (d) `Initialize` với n = 100
  - Then: 1, 1.0297, 18.679 / 11.467 / 30.43 (sai số 1e-3); `BaseCoinValue` đọc lần lượt 1, 1.0297, không đổi, 18.679; Plinko giả không chạm `PlinkoConfig.baseCoinValue`
  - Edge cases: getter luôn ≥ 1 và hữu hạn; 0 allocation khi đọc (`GcAllocProbe`)
- **AC-36**: reset tiệm mới
  - Given: Xu = 5,000, Tem = 7 (tier Alpha), n = 40, modifier `prestige.x` +1.0, recorder ghi thứ tự sự kiện
  - When: `ResetForNewShop` có ngữ cảnh
  - Then: Xu = `startingCoins`, Tem = 7, n = 0, giá = 100, `BaseCoinValue` = 1, `prestige.x` vẫn tính; sự kiện đúng thứ tự `WalletChanged(ShopReset)`, `SpinPriceChanged { 0, 100, 1 }`, `ShopEconomyReset { [currency.coin], 0 }`
  - Edge cases: Xu đã bằng `startingCoins` thì không phát `WalletChanged` nhưng vẫn phát hai sự kiện kia; không ngữ cảnh thì không có sự kiện nào và Xu giữ nguyên

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/SpinPaymentTests.cs`, `BaseCoinValueSourceTests.cs`, `ShopResetTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 002, Story 003, Story 004, Story 007
- Unlocks: Story 011, Story 016, Story 017, Story 018
