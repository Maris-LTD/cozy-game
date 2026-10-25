# Story 006: Number Formatter (K, M, B, T, aa…)

> **Epic**: Economy Core
> **Status**: Ready
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R13 Định dạng số, Formulas `display_format`, Tuning Knobs `displaySigDigits`, `rateDecimals`)
**Requirement**: `TR-economy-005`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format
**ADR Decision Summary**: bộ định dạng số lớn dùng bộ đệm ký tự cấp sẵn và mảng hậu tố cache, không cấp phát chuỗi mỗi khung; chỉ làm mới khi giá trị đổi hoặc theo nhịp giới hạn.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: định dạng số dùng bộ đệm cấp sẵn và hậu tố cache; số `double`.
- Forbidden: không nối chuỗi (`+`, `string.Format`, interpolation) trên đường mỗi khung; không static singleton.
- Guardrails: 0 B GC mỗi khung; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: chỉ dùng `System.Math` và `char[]`; kiểm không cấp phát bằng `GcAllocProbe` (`Assets/CozyGachapon/Tests/EditMode/Pooling/GcAllocProbe.cs`).

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Contracts/Economy/` (`INumberFormatter.cs`); `Assets/CozyGachapon/Scripts/Core/Economy/` (`NumberFormatter.cs`, `NumberSuffixTable.cs`, `SignificantDigitsWriter.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/NumberFormatterTests.cs`
- Data: `EconomyConfig.displaySigDigits`, `rateDecimals` (Story 001)

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [ ] `AC-37 [EditMode]` **GIVEN** `C.displaySigDigits` = 3, **WHEN** định dạng số dư 0, 999.9, 1,000, 1,234.5, 12,345, 999,999.6, 1e6, 1e9, 1e12, 1e15, 1e18, 1e300, NaN, **THEN** = "0", "999", "1.00K", "1.23K", "12.3K", "999K", "1.00M", "1.00B", "1.00T", "1.00aa", "1.00ab", "1.00dr", "0".
- [ ] `AC-38 [EditMode]` **GIVEN** `C.rateDecimals` = 1, **WHEN** định dạng tốc độ 6.58, 0, 1,234.5, **THEN** = "6.5/s", "0.0/s", "1.23K/s".

## Implementation Notes

- `INumberFormatter` (Contracts): `string FormatAmount(double value)`, `string FormatRate(double value)` `int TryFormatAmount(double value, Span<char> destination)` và `int TryFormatRate(double value, Span<char> destination)` (trả số ký tự ghi, không cấp phát) cho HUD. HUD là người chọn cách dùng; story này cung cấp cả hai đường.
- Công thức `display_format`: `k = floor(log10 v / 3)`, `m = v / 1000^k`, hiển thị = `m` cắt (không làm tròn lên) còn `displaySigDigits` chữ số có nghĩa + hậu tố(k). Hậu tố: k = 1..4 → K, M, B, T; k ≥ 5 → hai chữ cái: chữ thứ nhất = `(k − 5) div 26`, chữ thứ hai = `(k − 5) mod 26` (a = 0). v < 1000 → `floor(v)` (số dư/giá) hoặc cắt `rateDecimals` chữ số thập phân (tốc độ). NaN hoặc âm → "0" (tốc độ: "0.0/s" theo `rateDecimals`). Giữ chữ số 0 cuối ("1.00K") để header không nhảy độ rộng.
- Cắt, không làm tròn: 999,999.6 → "999K" chứ không "1000K"; 999.9 → "999". Cẩn thận sai số dấu phẩy động quanh lũy thừa của 1000 (1e6, 1e9, 1e15 phải ra "1.00M", "1.00B", "1.00aa"): tính `k` bằng so sánh với bảng lũy thừa cấp sẵn hoặc hiệu chỉnh log10, không tin `Math.Log10` thuần. Kiểm tra cả 1e300 → "1.00dr" (k = 100).
- Không cấp phát: bảng hậu tố cấp sẵn một lần khi dựng (đủ để phủ tới `walletSafeCap` 1e305 → k = 101); bộ đệm `char[]` dùng chung do thể hiện formatter giữ; chuỗi trả về của `FormatAmount`/`FormatRate` cache theo giá trị đã cắt gần nhất (chỉ tạo `string` mới khi chuỗi hiển thị thay đổi). Đường `TryFormatAmount` ghi thẳng vào `Span<char>` và là đường được `GcAllocProbe` kiểm 0 allocation.
- `displaySigDigits` (3–4) và `rateDecimals` (0–2) đọc từ `EconomyConfig` khi dựng, không literal trong logic.
- Quy tắc dự án: không comment, không LINQ, file ≤ 250 dòng (tách bảng hậu tố và bộ ghi chữ số có nghĩa), SRP, không static singleton (instance do composition root giữ).

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 018]: gắn formatter vào bước boot và kiểm 0 B GC của formatter trong kịch bản khung đầy đủ (AC-43).
- HUD & Tab Navigation: cách hiển thị, tần suất làm mới, AC-45 (screenshot header) — xem Deferred acceptance criteria trong EPIC.

## QA Test Cases

- **AC-37**: số dư
  - Given: `displaySigDigits` = 3
  - When: `FormatAmount` cho 0, 999.9, 1,000, 1,234.5, 12,345, 999,999.6, 1e6, 1e9, 1e12, 1e15, 1e18, 1e300, NaN
  - Then: "0", "999", "1.00K", "1.23K", "12.3K", "999K", "1.00M", "1.00B", "1.00T", "1.00aa", "1.00ab", "1.00dr", "0"
  - Edge cases: giá trị âm → "0"; `displaySigDigits` = 4 cho 1,234.5 → "1.234K"; +∞ không ném ngoại lệ
- **AC-38**: tốc độ
  - Given: `rateDecimals` = 1
  - When: `FormatRate` cho 6.58, 0, 1,234.5
  - Then: "6.5/s", "0.0/s", "1.23K/s"
  - Edge cases: `rateDecimals` = 0 cho 2.5 → "2/s"; NaN → "0.0/s"

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/NumberFormatterTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 001
- Unlocks: Story 018
