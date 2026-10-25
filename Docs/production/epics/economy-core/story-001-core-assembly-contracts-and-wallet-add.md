# Story 001: Core Assembly, Economy Contracts & Wallet Add

> **Epic**: Economy Core
> **Status**: Complete
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R1 Ví đa tiền tệ, R2 Cộng vào ví, Formulas `wallet_add`, Tuning Knobs "Ví và tiền tệ", Edge Cases "Ví và chi tiêu")
**Requirement**: `TR-economy-001`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format (kèm ADR-0001)
**ADR Decision Summary**: `Wallet` giữ số dư `double`, không âm, nằm trong `CozyGachapon.Core`; interface và kiểu sự kiện nằm ở `CozyGachapon.Contracts`; Core chỉ tham chiếu Contracts và Foundation.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: số dư là `double`; interface, struct sự kiện, enum đặt ở `CozyGachapon.Contracts`; logic miền là C# thuần test được trong EditMode; tham số cân bằng nằm trong ScriptableObject.
- Forbidden: không `int`/`float` cho tiền; không để hệ khác sửa số dư ngoài API ví; không static singleton; Contracts không chứa logic; không tham chiếu tầng cao hơn.
- Guardrails: 0 B GC mỗi khung ở đường nóng; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không dùng API sau cutoff; ScriptableObject và asmdef chuẩn.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/CozyGachapon.Core.asmdef` (references `CozyGachapon.Contracts`, `CozyGachapon.Foundation`; root namespace `CozyGachapon.Core`); `Assets/CozyGachapon/Scripts/Core/Economy/` (`WalletService.cs`, `CurrencyLedger.cs`, `EconomyConfig.cs`, `CurrencyDefinition.cs`, `EconomyConfigValidator.cs`, `EconomyBalanceTargets.cs`); `Assets/CozyGachapon/Scripts/Contracts/Economy/` (`IWallet.cs`, `WalletChangeReason.cs`, `WalletChangedEvent.cs`, `EconomyIds.cs`, `IEconomyDiagnosticsSink.cs`, `EconomyDiagnosticCode.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/WalletAddTests.cs`, `EconomyEventRecorder.cs`, `CountingEconomyDiagnosticsSink.cs`; thêm `CozyGachapon.Core` vào `references` của `CozyGachapon.Tests.EditMode.asmdef`
- Data: `Assets/CozyGachapon/Data/Economy/EconomyConfig.asset`, `CurrencyCoin.asset` (`currency.coin`), `EconomyBalanceTargets.asset` (giá trị mặc định theo cột Current của GDD; công thức dùng nó ở Story 015 và 016)

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [x] `AC-2 [EditMode]` **GIVEN** Xu = 0, **WHEN** `Add(coin, 2.5)` hai lần, rồi từ 0 `Add(coin, 0.4)` ba lần, **THEN** số dư lần lượt là 5.0 và 1.2; chuỗi hiển thị "5" và "1"; mỗi lần `Add` thành công phát đúng một `WalletChanged` có `delta` = amount và đúng `reason`.
- [x] `AC-3 [EditMode]` **GIVEN** Xu = 10, **WHEN** `Add` với amount ∈ {−1, 0, NaN, +∞} hoặc với `currencyId` chưa đăng ký, **THEN** trả `false`, số dư vẫn 10, không có sự kiện nào.
- [x] `AC-4 [EditMode]` **GIVEN** Xu = `C.walletSafeCap` − 1, **WHEN** `Add(coin, C.walletSafeCap / 10)`, **THEN** Xu = `C.walletSafeCap`.

## Implementation Notes

- Tạo `CozyGachapon.Core.asmdef` đúng ADR-0001: references chỉ `CozyGachapon.Contracts` và `CozyGachapon.Foundation` (không Unity module thừa); thêm vào test asmdef. Asmdef `Runtime` thêm tham chiếu `Core` ở Story 018, không ở đây.
- `EconomyBalanceTargets` (ScriptableObject, chỉ test/mô phỏng, không runtime nào tham chiếu) chứa các knob mục tiêu của GDD (Story 016 kiểm validator). `CurrencyDefinition` (ScriptableObject): `id` chuỗi bất biến, tên hiển thị, biểu tượng, `releaseTier`, `resetsOnNewShop` (R1.1). `EconomyConfig` (ScriptableObject) khai đủ mọi knob của Tuning Knobs (ví, giá, pipeline, thời gian, hiển thị) để các story sau chỉ đọc; story này chỉ dùng `currencies`, `startingCoins`, `walletSafeCap`, `displaySigDigits`. Mọi giá trị đặt sẵn theo cột Current của GDD. Setter dùng cho test theo mẫu `SetXForTest` của `AudioConfig`.
- `EconomyConfigValidator` là lớp C# thuần trả danh sách issue (mẫu `AudioConfigValidator`), kiểm safe range của từng knob và `currency.coin` hiện diện. ADR-0009 mô tả `IValidatableAsset` nhưng interface đó chưa có trong Contracts; theo mẫu Audio, ghi lệch vào Completion Notes. Kiểm tiền tố `tierOwnerPrefixes` làm ở Story 008.
- `IWallet` (Contracts): `bool Add(string currencyId, double amount, WalletChangeReason reason)` và truy vấn `double GetBalance(string currencyId)`; `event Action<WalletChangedEvent> WalletChanged`. ADR-0005 phác `IWallet` một đồng (`Credit(double)`, `Coins`); GDD (đa tiền tệ, `reason`) thắng, ghi lệch vào Completion Notes. `TrySpend`/`CanAfford` thêm ở Story 002.
- `WalletChangedEvent { currencyId, previous, current, delta, reason }` là `readonly struct`. `WalletChangeReason` liệt kê đúng các lý do của bảng faucet/sink R14 (Offline, AdIdleCompensation, AdReward, Prestige, ShopReset, GachaSpin, ShopPurchase, PrestigeUpgrade, NeighborCandy) cộng hai lý do thu nhập pipeline (xem Open Questions của EPIC).
- Quy tắc `Add` (R2, `wallet_add`): từ chối amount ≤ 0, NaN, vô cực, `currencyId` chưa đăng ký (trả `false`, không sự kiện, báo sink `RejectedAdd`); `balance' = min(balance + amount, walletSafeCap)`; số dư giữ phần lẻ; chuỗi hiển thị lấy qua bộ định dạng của Story 006, nên AC-2 phần chuỗi dùng hàm cắt xuống số nguyên tối thiểu (`Math.Floor`) trong test và được kiểm lại bằng bộ định dạng thật ở Story 006.
- `Add` phát `WalletChanged` đúng một lần kể cả khi bị kẹp (delta là phần thực sự cộng). Không có đường nào đặt số dư trực tiếp: `CurrencyLedger` là nội bộ (`internal`), không property setter public.
- Chẩn đoán qua `IEconomyDiagnosticsSink` (Contracts), test dùng sink đếm; không `Debug.Log*`.
- Quy tắc dự án: không comment, không LINQ, file ≤ 250 dòng, SRP, không static singleton, tuning trong ScriptableObject, không cấp phát trên đường `Add`.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 002]: `TrySpend`, `CanAfford`, ngữ cảnh hành động người chơi, `SpendRejected`.
- [Story 004]: khởi tạo ví từ cấu hình (đăng ký tiền tệ theo `releaseTier`, `startingCoins`), lưu/nạp.
- [Story 006]: bộ định dạng số (AC-2 chỉ kiểm chuỗi bằng cắt xuống số nguyên ở đây).
- [Story 017]: kiểm bằng reflection mọi API public (AC-7a, AC-7b).

## QA Test Cases

- **AC-2**: cộng phần lẻ
  - Given: ví có `currency.coin` = 0
  - When: `Add(coin, 2.5, reason)` hai lần; ví mới `Add(coin, 0.4)` ba lần
  - Then: 5.0 và 1.2 (sai số `valueRelTolerance`); chuỗi cắt xuống "5" và "1"; mỗi lần đúng một `WalletChanged` với `delta` = amount, `reason` đúng
  - Edge cases: 0.4 × 3 không được hiện "2" do làm tròn lên; `previous` + `delta` = `current`
- **AC-3**: từ chối đầu vào xấu
  - Given: Xu = 10, listener đếm sự kiện
  - When: `Add` với −1, 0, NaN, +∞, −∞ và với `currency.unknown`
  - Then: `false`, Xu = 10, 0 sự kiện
  - Edge cases: `currencyId` null hoặc rỗng; sink nhận mã `RejectedAdd`
- **AC-4**: kẹp tại trần
  - Given: Xu = `walletSafeCap` − 1
  - When: `Add(coin, walletSafeCap / 10)`
  - Then: Xu = `walletSafeCap`; `WalletChanged.delta` = phần thực cộng (1)
  - Edge cases: đang ở đúng trần rồi `Add` tiếp (delta 0, quyết định: vẫn trả `true` và không phát sự kiện nếu delta = 0, ghi vào Completion Notes)

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/WalletAddTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] `WalletAddTests.cs`, `WalletAddZeroAllocTests.cs`, `EconomyConfigValidatorTests.cs`, `EconomyDefaultAssetsTests.cs` (EditMode 690/690)

## Dependencies

- Depends on: None
- Unlocks: Story 002, Story 003, Story 004, Story 006

## Completion Notes

- Namespace: asmdef `CozyGachapon.Core` dùng `rootNamespace`/namespace `CozyGachapon.Economy` (giống Foundation dùng `CozyGachapon.Audio`), khác `CozyGachapon.Core` ghi trong story/ADR-0001; tránh namespace `Core` che các tên khác trong `CozyGachapon.*`.
- `IWallet` theo GDD (đa tiền tệ, `Add(currencyId, amount, reason)`, `GetBalance(currencyId)`, `event Action<WalletChangedEvent>`), lệch bản phác `Credit(double)`/`Coins` của ADR-0005 (TD-011).
- `IValidatableAsset` (ADR-0009) chưa có; `EconomyConfigValidator` theo mẫu validator thuần của Audio, tách `EconomyCurrencyValidator` và `EconomyKnobRangeValidator`.
- Safe range của 18 knob runtime nằm ở asset mới `EconomySafeRanges` (ngoài danh sách asset của story) để logic không có hằng literal; safe range của `EconomyBalanceTargets` để Story 016.
- `WalletChangeReason` thêm `IdleIncome`, `PlinkoIncome` cho thu nhập pipeline (Open Questions của EPIC); Story 004, 011, 012 dùng hai giá trị này.
- `Add` từ chối `reason` không thuộc faucet (sink và giá trị enum không định nghĩa), suy từ R2.4; trả `false`, báo `RejectedAdd`.
- `Add` khi đã ở đúng trần: trả `true`, số dư không đổi, không phát `WalletChanged` (delta 0), không báo sink.
- AC-4 kiểm hai lần: cap mặc định 1e300 (chỉ kiểm số dư, vì `1e300 − 1 == 1e300` trong double) và cap test 1000 (kiểm `delta` = 1).
- `RegisterCurrency` là `internal` (test qua `InternalsVisibleTo`); Story 004 thay bằng khởi tạo từ config.
- `ReleaseTier { Mvp, VerticalSlice, Alpha, PostLaunch }` đặt ở Contracts; `CurrencyShopStamp.asset` có tier `Alpha`.

## Completion Notes
**Completed**: 2026-10-25
**Criteria**: 3/3 passing (AC-2, AC-3, AC-4 + QA edge cases)
**Harness**: unity-harness all PASS (rules, compile, EditMode 690/690); smoke n/a (no scene change)
**Run result**: N/A - wallet logic, not wired into the scene until Story 018
**Deviations**: ADVISORY - new `CozyGachapon.Core` asmdef (namespace `CozyGachapon.Economy`, refs Contracts + Foundation) added to the test asmdef; `IWallet` follows the GDD (multi-currency `Add(currencyId, amount, reason)`, `GetBalance`, `WalletChanged`) over the ADR-0005 single-currency sketch (TD-011); `Add` rejects sink reasons; knob safe ranges live in a separate `EconomySafeRanges` SO; AC-4 tested with two caps because 1e300 - 1 == 1e300 in double; 5 data assets created through the Editor (`EconomyConfig`, `EconomySafeRanges`, `EconomyBalanceTargets`, `CurrencyCoin`, `CurrencyShopStamp`); no `IValidatableAsset` yet (plain validator like Audio)
**Test Evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/WalletAddTests.cs`, `WalletAddZeroAllocTests.cs`, `EconomyConfigValidatorTests.cs`, `EconomyDefaultAssetsTests.cs`, passing
**Visual sign-off**: n/a (Logic)
**Code Review**: unity-dev review, 1 minor (unused `CurrencyLedger.Count`) removed; LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none
