# Story 017: Pillar 4 API Guards (reflection, boundary inputs, spin count)

> **Epic**: Economy Core
> **Status**: Complete
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: 2026-10-27
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R3.1, R3.5, R8.4, Edge Cases "Ví và chi tiêu", Acceptance Criteria AC-7a, AC-7b, AC-35, Assumed "EC-B2")
**Requirement**: `TR-economy-001`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format
**ADR Decision Summary**: hệ khác không bao giờ trừ hoặc đặt số dư ngoài API ví; số dư không âm trong mọi điều kiện đầu vào.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: mọi thay đổi số dư qua API ví; số `double`.
- Forbidden: không bypass ví; không property số dư có setter public; không static singleton.
- Guardrails: test reflection chỉ chạy trong Editor/EditMode; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: dùng `System.Reflection` trong test EditMode; không có API sau cutoff.

**Unity hints**:
- Code: story này không thêm logic runtime mới; chỉ sửa API public của Story 001–016 nếu test lộ lỗ hổng (ví dụ property setter public, field public ghi được)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/ActionContextReflectionTests.cs`, `PublicApiBoundaryTests.cs`, `SpinCountIntegrityTests.cs`, `EconomyApiInvoker.cs` (helper sinh giá trị biên)
- Data: dùng asset Story 001

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [x] `AC-7a [EditMode]` **GIVEN** assembly runtime chứa Economy Core, **WHEN** liệt kê bằng reflection mọi phương thức public (kể cả accessor của property) của mọi kiểu public trong assembly đó thuộc ví / Economy Core, **THEN** tập tên phương thức có ít nhất một tham số thuộc kiểu ngữ cảnh hành động người chơi (tên kiểu do ADR chốt) đúng bằng {`TrySpend`, `TryPaySpin`, `ResetForNewShop`}; không có property số dư nào có setter public, không có field public không `readonly` nào chứa số dư hay bảng ví, và không phương thức public nào trả về tham chiếu ghi được (`ref`) tới số dư (Pillar 4).
- [x] `AC-7b [EditMode]` **GIVEN** Xu = 500 (Tem = 7 khi đã đăng ký), Economy Core ở Running, **WHEN** gọi bằng reflection mọi phương thức public còn lại (không thuộc tập của AC-7a), mỗi tham số lần lượt nhận từng giá trị biên — số thực {0, −1, NaN, +∞, −∞, `double.MaxValue`}, số nguyên {0, −1, `int.MaxValue`}, chuỗi/id {rỗng, null, `currency.unknown`, `currency.coin`, `sourceKey` lạ}, mọi giá trị của mỗi enum — trong khi các tham số khác giữ giá trị hợp lệ mặc định, rồi thêm một lần với mọi tham số cùng ở giá trị biên, **THEN** sau từng lời gọi Xu ≥ 500 và Tem ≥ 7, không có ngoại lệ nào thoát ra ngoài Economy Core, và không có `WalletChanged` nào có `delta` < 0.
- [x] `AC-35 [EditMode]` **GIVEN** n = 5, **WHEN** gọi mọi API public của Economy Core ngoại trừ `TryPaySpin` thành công và `ResetForNewShop`, **THEN** n vẫn là 5 (lượt quay miễn phí không tăng giá).

## Implementation Notes

- Tập kiểu quét: mọi kiểu public thuộc ví/Economy Core trong assembly `CozyGachapon.Core` và interface liên quan ở `CozyGachapon.Contracts` (`IWallet`, `ISpinPriceService`, `IIncomePipeline`). "Assembly runtime chứa Economy Core" là `CozyGachapon.Core` (ADR-0001). Loại các kiểu chỉ-dữ-liệu (struct sự kiện) khỏi phép kiểm "phương thức", nhưng tính chúng khi kiểm field/property số dư.
- Tên kiểu ngữ cảnh hành động do ADR chốt; Story 002 dùng `PlayerActionContext` (giả định, Open Questions của EPIC). Test lấy tên kiểu từ một hằng số tập trung trong test để đổi một chỗ khi ADR chốt. AC-7a đúng bằng tập `{TrySpend, TryPaySpin, ResetForNewShop}` (kể cả overload).
- AC-7b: helper `EconomyApiInvoker` sinh tổ hợp tham số: mỗi tham số lần lượt ở từng giá trị biên (các tham số còn lại hợp lệ mặc định), rồi một lần với mọi tham số cùng ở biên; bọc từng lời gọi bằng `try/catch` để kiểm "không ngoại lệ nào thoát ra". Phạm vi gọi: mọi phương thức public còn lại, gồm `Add`, `CanAfford`, `RegisterIncomeSource`, `RegisterModifier`, `Unregister`, `SubmitIncome`, `GetDerivedPrice`, `PreviewIdleCps` (với `IIdlePlan` biên), `HandlePaused`/`HandleResumed`/`HandleInteractive`/`HandleAdClosed`, `Tick`, truy vấn. Sau từng lời gọi kiểm Xu ≥ 500, Tem ≥ 7 và không có `WalletChanged` âm (recorder). Ngoại lệ nằm trong Economy Core được phép nếu bị bắt và báo sink; ngoại lệ lọt ra ngoài là thất bại. Nếu test lộ lỗ hổng, sửa API ở story gốc và ghi lại trong Completion Notes của story này.
- AC-35: dựng n = 5 (qua `Initialize` với dữ liệu nạp, Story 004), gọi mọi API public ngoại trừ `TryPaySpin` thành công và `ResetForNewShop` (kể cả `TryPaySpin` thất bại), kiểm n = 5 và giá quay không đổi.
- Không dùng LINQ trong code runtime; test được phép dùng LINQ cho tiện nhưng nên tránh để cùng quy ước (ghi lựa chọn vào Completion Notes).
- Quy tắc dự án cho code sửa: không comment, không `Debug.Log*`, file ≤ 250 dòng, SRP.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 002], [Story 005]: cài đặt `TrySpend`, `TryPaySpin`, `ResetForNewShop` (story này chỉ kiểm bằng reflection).
- [Story 018]: kiểm bước boot và đăng ký dịch vụ.
- Neighbor Pranks (v1.1): giữ tạm thu nhập mới (R4.9), ngoài epic.

## QA Test Cases

- **AC-7a**: chỉ ba API nhận ngữ cảnh hành động
  - Given: assembly `CozyGachapon.Core` và Contracts
  - When: reflection mọi phương thức public (kể cả accessor) của các kiểu ví/Economy
  - Then: tập tên có tham số kiểu ngữ cảnh hành động = {`TrySpend`, `TryPaySpin`, `ResetForNewShop`}; không setter public cho số dư; không field public ghi được chứa số dư; không `ref` return tới số dư
  - Edge cases: overload; property read-only trả `double` là hợp lệ; thêm một API mới nhận ngữ cảnh làm test đỏ
- **AC-7b**: giá trị biên
  - Given: Xu = 500, Tem = 7 (tier Alpha), Running
  - When: gọi từng API còn lại với từng giá trị biên (từng tham số, rồi tất cả cùng lúc)
  - Then: Xu ≥ 500, Tem ≥ 7, không ngoại lệ thoát ra, không `WalletChanged` delta < 0
  - Edge cases: `double.MaxValue` cho `Add` kẹp tại `walletSafeCap`; enum giá trị ngoài miền (ép kiểu)
- **AC-35**: n không đổi
  - Given: n = 5
  - When: gọi mọi API public trừ `TryPaySpin` thành công và `ResetForNewShop`
  - Then: n = 5, giá quay không đổi
  - Edge cases: `TryPaySpin` thất bại (thiếu xu) cũng không đổi n

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/ActionContextReflectionTests.cs`, `PublicApiBoundaryTests.cs`, `SpinCountIntegrityTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] Verified (`ActionContextReflectionTests.cs`, `PublicApiBoundaryTests.cs`, `SpinCountIntegrityTests.cs` — 1011/1011 EditMode tests PASS, `unity-harness rules` PASS, 2026-10-27)

## Dependencies

- Depends on: Story 005, Story 010, Story 012, Story 014, Story 015
- Unlocks: Story 018
