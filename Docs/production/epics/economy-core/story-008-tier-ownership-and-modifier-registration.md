# Story 008: Tier Ownership & Modifier Registration Rules

> **Epic**: Economy Core
> **Status**: Ready
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R4.3, R4.7, Edge Cases "đăng ký sai tầng", "đăng ký hai lần cùng `sourceId`", Tuning Knobs `tierOwnerPrefixes`)
**Requirement**: `TR-economy-002`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format
**ADR Decision Summary**: hệ đăng ký modifier tự gỡ; pipeline kiểm tầng để thú/kệ không bao giờ vào Boost, Prestige, Global (chốt chặn chống lạm phát, Pillar 3).
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: modifier khai tầng tường minh; dữ liệu cân bằng tự kiểm tra (validator).
- Forbidden: không bypass pipeline; không static singleton; không mutate asset trong validator.
- Guardrails: không cấp phát khi đăng ký thành công quá dung lượng cấp sẵn; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có API sau cutoff.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Economy/` (`TierOwnershipPolicy.cs`, `TierOwnerPrefixes.cs` dữ liệu serialize, mở rộng `ModifierBook.cs`, mở rộng `EconomyConfigValidator.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/TierOwnershipTests.cs`, `ModifierReplacementTests.cs`
- Data: `EconomyConfig.tierOwnerPrefixes` (Base/Synergy: `shelf.`; Boost: `boost.`, `ad.`; Prestige: `prestige.`; Global: `event.`)

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [ ] `AC-13 [EditMode]` **GIVEN** `C.tierOwnerPrefixes` mặc định, **WHEN** `shelf.x` đăng ký vào Boost, Prestige hoặc Global, hoặc `boost.x` đăng ký vào Synergy, **THEN** bị từ chối và `idle_cps` không đổi; `ad.x` vào Boost và `shelf.x` vào Synergy được nhận; validator báo lỗi nếu cấu hình cho `shelf.` vào Global.
- [ ] `AC-14 [EditMode]` **GIVEN** `idle_cps` = X, **WHEN** đăng ký hai lần cùng `sourceId` + đích với value 0.5 rồi `Unregister(sourceId)`, **THEN** sau lần đăng ký thứ hai chỉ tính một lần +0.5 (không +1.0), sau khi gỡ `idle_cps` = X; `Unregister` một id không tồn tại không đổi gì.

## Implementation Notes

- `TierOwnershipPolicy.IsAllowed(string sourceId, ModifierTier tier)` kiểm `sourceId` bắt đầu bằng một tiền tệ trong danh sách của tầng đó theo `EconomyConfig.tierOwnerPrefixes` (so sánh `StartsWith(prefix, StringComparison.Ordinal)`, không cấp phát). `ModifierBook.TryRegister` trả `false` và báo sink `ModifierTierRejected` khi sai tầng; modifier bị từ chối không đổi thu nhập (AC-13). Thú/Shelf (`shelf.`, `pet.`) không bao giờ được vào Boost, Prestige, Global (R4.7).
- Bảng ownership của GDD: tầng Base và Synergy: `shelf.` (Base còn nhận `Plinko qua CoinResolved` nội bộ, không qua `sourceId`); Boost: `boost.`, `ad.`; Prestige: `prestige.`; Global: `event.`. Mặc định này là dữ liệu của `EconomyConfig`, không literal trong logic.
- Validator (`EconomyConfigValidator`, cùng mẫu Story 001): báo lỗi nếu `tierOwnerPrefixes` cho `shelf.` hoặc `pet.` vào Boost, Prestige hoặc Global, hoặc cấu hình tầng trống tiền tố. Chỉ đọc, không sửa asset.
- Đăng ký trùng (Edge Case): cùng `sourceId` và cùng đích (cùng tầng, kênh, target) thì bản sau thay bản trước, không cộng đôi. `Unregister(sourceId)` gỡ mọi modifier của `sourceId`, id không tồn tại thì không làm gì và không báo lỗi. ADR-0005 (Risks) nói ném ngoại lệ khi trùng ID; GDD (thay thế) thắng và không ném ngoại lệ ở runtime, ghi lệch vào Completion Notes.
- Hết hạn modifier là việc của hệ đăng ký (đồng hồ ở Time & Timers / Boosts), Economy không có trạng thái "hết hạn". `isTemporary` chỉ phục vụ `OfflineRateSnapshot` (Story 009).
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng, SRP, không static singleton.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 007]: tính tầng, trần và `channel_income` (đã có khi story này bắt đầu).
- [Story 009]: tính `idle_cps` (AC-13 và AC-14 đọc `idle_cps` từ Story 009).
- Boosts, Rewarded Ads, Prestige, live-ops events: các hệ đăng ký thật nằm ngoài epic (VS/Alpha).

## QA Test Cases

- **AC-13**: quyền vào tầng
  - Given: `tierOwnerPrefixes` mặc định, một nguồn Idle có `idle_cps` = X
  - When: đăng ký `shelf.x` vào Boost, Prestige, Global; `boost.x` vào Synergy; rồi `ad.x` vào Boost và `shelf.x` vào Synergy; chạy validator với config cho `shelf.` vào Global
  - Then: bốn ca đầu bị từ chối, `idle_cps` = X; hai ca sau được nhận; validator báo lỗi
  - Edge cases: `sourceId` rỗng hoặc null bị từ chối; tiền tố khác hoa/thường không khớp
- **AC-14**: thay thế và gỡ
  - Given: `idle_cps` = X
  - When: đăng ký hai lần cùng `sourceId` + đích value 0.5; `Unregister(sourceId)`; `Unregister("khong.ton.tai")`
  - Then: sau lần hai chỉ +0.5 (không +1.0); sau khi gỡ = X; gỡ id lạ không đổi gì
  - Edge cases: cùng `sourceId` nhưng đích khác thì là hai modifier; `Unregister` gỡ cả hai

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/TierOwnershipTests.cs`, `ModifierReplacementTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 007, Story 009
- Unlocks: Story 012
