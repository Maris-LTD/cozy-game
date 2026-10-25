# Story 007: Income Modifier Tiers & Caps

> **Epic**: Economy Core
> **Status**: Ready
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R4.1–R4.6, R4.8, Formulas `multiplier_synergy`, `tier_multiplier`, `channel_income`, Edge Cases "Pipeline và modifier")
**Requirement**: `TR-economy-002`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format
**ADR Decision Summary**: modifier đăng ký vào tầng tường minh, các tầng cộng phẳng áp trước rồi nhân theo tầng có tên, mỗi tầng có trần riêng để chống lạm phát.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: modifier phải khai tầng tường minh; mỗi tầng có trần; số `double`; tham số trần trong ScriptableObject.
- Forbidden: không tính tiền ngoài pipeline; không static singleton; không literal cân bằng trong logic.
- Guardrails: tính CPS < 1 microsecond; 0 B GC trên đường tính; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có API sau cutoff.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Contracts/Economy/` (`IncomeChannel.cs`, `ModifierTier.cs`, `IncomeModifier.cs`, `IIncomePipeline.cs`); `Assets/CozyGachapon/Scripts/Core/Economy/` (`ModifierBook.cs`, `IncomeSourceTable.cs`, `TierMultiplierCalculator.cs`, `ChannelIncomeCalculator.cs`, `ValueSanitizer.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/ModifierTierTests.cs`, `ChannelIncomeTests.cs`
- Data: `EconomyConfig.synergyTierCap`, `boostTierCap`, `prestigeTierCap`, `globalTierCap` (Story 001)

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [ ] `AC-9 [EditMode]` **GIVEN** nguồn Idle A, B và nguồn `plinko.coin`, mỗi ca bắt đầu không có modifier, **WHEN** đăng ký (a) không gì; (b) hai Synergy kênh Idle +1.0; (c) Synergy kênh Idle tổng +4.2; (d) một Synergy nhắm A +0.5; (e) một Synergy kênh Idle +0.5; (f) một Synergy kênh Both +0.5, **THEN** (a) `M_Synergy(A)` = 1; (b) = 3; (c) = `C.synergyTierCap` (4.0); (d) `M_Synergy(A)` = 1.5, `M_Synergy(B)` = 1; (e) `M_Synergy(plinko.coin)` = 1; (f) `M_Synergy(A)` = `M_Synergy(plinko.coin)` = 1.5.
- [ ] `AC-10 [EditMode]` **GIVEN** hai modifier Boost +1.0 (một Both, một Plinko), **WHEN** tính, **THEN** `M_Boost(Plinko)` = min(3, `C.boostTierCap`) = 3 và `M_Boost(Idle)` = 2; tương tự tầng Prestige và Global bị kẹp tại `C.prestigeTierCap` và `C.globalTierCap` khi tổng vượt trần.
- [ ] `AC-11 [EditMode]` **GIVEN** một nguồn gốc 50, hai Synergy kênh +1.0, Boost +1.0, Global `event.test` +1.0, **WHEN** tính `channel_income`, **THEN** kết quả = 600.
- [ ] `AC-12 [EditMode]` **GIVEN** một cấu hình modifier hợp lệ cho kết quả X, **WHEN** thêm một modifier có value −0.5, NaN hoặc +∞ ở bất kỳ tầng nào, **THEN** kết quả vẫn bằng X.

## Implementation Notes

- `IncomeModifier` (`readonly struct`, Contracts) theo R4.3: `{ sourceId, channel (Idle | Plinko | Both), tier, value, target (Channel | sourceKey), targetKey, isTemporary }`. Enum `ModifierTier { Base, Synergy, Boost, Prestige, Global }` theo R4.4 (ADR-0005 phác bốn tầng `FlatBase/AdditivePercent/SynergyMultiplier/BoostMultiplier`; GDD có năm tầng và là bản sau, thắng, ghi lệch vào Completion Notes). `IIncomePipeline` (Contracts) khai `RegisterModifier`, `Unregister(sourceId)`, `RegisterIncomeSource(sourceKey, channel, baseRate)` ở mức tối thiểu cho story này; các story 008–012 mở rộng.
- Công thức giữ đúng GDD: trong một tầng nhân các `value` cộng (`M_tầng = min(1 + Σ sanitize(v), trần)`), giữa các tầng nhân; `income_s = (base_s + flat_s) × M_Synergy(s) × M_Boost(c) × M_Prestige(c) × M_Global(c)`. `M_Synergy(s)` cộng mọi modifier Synergy có target = s hoặc (target = Channel và channel(m) ∈ {channel(s), Both}), rồi kẹp tại `synergyTierCap`. `M_Boost/Prestige/Global(c)` cộng mọi modifier tầng đó có channel(m) ∈ {c, Both}.
- `sanitize(v)` = v nếu hữu hạn và ≥ 0, ngược lại 0 (R4.5, AC-12): value âm, NaN, vô cực là trung tính, các modifier khác vẫn tính. Không có modifier âm.
- `IncomeSourceTable` giữ nguồn `{ sourceKey, channel, baseRate, flatUnits }`; `plinko.coin` là nguồn Plinko dựng sẵn khi `Initialize`. Tầng Base (cộng phẳng) nhắm một nguồn và tính theo bội số: Idle theo `idleCpsUnit` (Story 009), Plinko theo `baseCoinValue` (Story 011); tại story này chỉ cần tổng `flat_units` cho một nguồn.
- Tính theo cờ "bẩn": kết quả `M_*` cache và tính lại khi đăng ký/gỡ; story này đặt cờ, Story 009 đo (AC-24). Không cấp phát khi tính; `ModifierBook` dùng mảng cấp sẵn (khởi tạo dung lượng mặc định, chỉ nới khi đăng ký, không nới trên đường tính).
- Giá trị trần và mọi số cân bằng đọc từ `EconomyConfig`; test dựng config bằng `SetXForTest`. Ví dụ GDD: Trà +1.0 (Both) và boost quảng cáo +1.0 (Plinko) → kênh Plinko min(1 + 2, 3) = 3, kênh Idle = 2.
- Quy tắc dự án: không comment, không LINQ, file ≤ 250 dòng (tách `ModifierBook`, `TierMultiplierCalculator`, `ChannelIncomeCalculator`, `ValueSanitizer`), SRP, không static singleton.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 008]: kiểm quyền vào tầng theo tiền tố `sourceId`, thay thế khi đăng ký trùng, `Unregister` một id không tồn tại.
- [Story 009]: `idle_cps`, đơn vị `idleCpsUnit`, cờ bẩn đo bằng bộ đếm, `OfflineRateSnapshot`.
- [Story 010]: `GetSourceIdleCps`, `PreviewIdleCps`.
- [Story 011]: `plinko_credit` và cộng phẳng theo `baseCoinValue`.

## QA Test Cases

- **AC-9**: `M_Synergy` theo nguồn và kênh
  - Given: nguồn Idle A, B và nguồn `plinko.coin`, config trần Synergy 4.0
  - When: đăng ký từng ca (a)–(f) trên ví dụ sạch
  - Then: (a) 1; (b) 3; (c) 4.0; (d) A = 1.5, B = 1; (e) `plinko.coin` = 1; (f) A = `plinko.coin` = 1.5
  - Edge cases: tổng đúng bằng trần; modifier kênh Plinko không ảnh hưởng nguồn Idle
- **AC-10**: trần từng tầng
  - Given: hai Boost +1.0 (Both, Plinko); các tổng vượt trần cho Prestige và Global
  - When: tính `M_Boost(Plinko)`, `M_Boost(Idle)`, `M_Prestige`, `M_Global`
  - Then: 3 và 2; Prestige và Global bằng `prestigeTierCap`, `globalTierCap`
  - Edge cases: ba Boost +1.0 cùng lúc → đúng `boostTierCap`
- **AC-11**: thứ tự cộng/nhân
  - Given: gốc 50, hai Synergy kênh +1.0, Boost +1.0, Global `event.test` +1.0
  - When: tính `channel_income`
  - Then: 600 (không phải 800 hay 250)
  - Edge cases: gốc 0 → 0
- **AC-12**: modifier xấu là trung tính
  - Given: cấu hình hợp lệ cho X ở mỗi tầng
  - When: thêm modifier −0.5, NaN, +∞ vào từng tầng
  - Then: kết quả vẫn X
  - Edge cases: modifier hợp lệ và xấu cùng tầng: chỉ phần hợp lệ tính

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/ModifierTierTests.cs`, `ChannelIncomeTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 001, Story 004
- Unlocks: Story 005, Story 009, Story 011
