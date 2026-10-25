# Story 001: Pool Registration & Capacity

> **Epic**: Object Pooling
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/object-pooling.md` (R2 Định nghĩa pool, R3 Đăng ký, Formulas `pool_capacity` và `registration_valid`, Tuning Knobs `PoolingConfig.pools`, `maxCapacityPerPool`, `maxTotalPooledObjects`)
**Requirement**: `TR-pooling-001`

**ADR Governing Implementation**: ADR-0003: Zero-Allocation Object Pooling (kèm ADR-0001: Layered Assembly Definitions)
**ADR Decision Summary**: `ObjectPoolService` ở `CozyGachapon.Foundation`; hệ khách đăng ký pool ở pha `GameplayInit`; dung lượng do hệ khách khai báo cộng headroom; interface, enum, struct ở `CozyGachapon.Contracts`.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: interface/enum/struct đặt trong `CozyGachapon.Contracts`; logic ở `CozyGachapon.Foundation`; hệ khách đăng ký pool ở `GameplayInit`.
- Forbidden: không tham chiếu tầng cao hơn; không static singleton / `FindObjectOfType` / `GameObject.Find`; không `Instantiate`/`Destroy` ngoài pha `Prewarm`.
- Guardrails: 0 B GC mỗi khung ở trạng thái ổn định (bộ đếm đọc không cấp phát).

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không dùng API Unity ngoài `GameObject` làm kiểu prefab; phần đăng ký không tạo đối tượng.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Pooling/` (`CozyGachapon.Foundation`): `ObjectPoolService.cs`, `PoolRegistry.cs`, `PoolCapacity.cs`, `PoolRegistrationValidator.cs`, `PoolingConfigValidator.cs`; `Assets/CozyGachapon/Scripts/Contracts/` (`CozyGachapon.Contracts`): `PoolExhaustionPolicy.cs`, `IPoolService.cs`, `PoolRegistrationResult.cs`, `PoolStats.cs`, `IPoolDiagnosticsSink.cs`
- Test: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolRegistrationTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: `PoolingConfig` ScriptableObject `Assets/CozyGachapon/Data/Pooling/PoolingConfig.asset` (`pools` gồm `PoolDefinition` {`id`, `prefab`, `policy`, `headroom`}, `maxCapacityPerPool`, `maxTotalPooledObjects`, `prewarmPerFrame`, `prewarmMaxSeconds`); `[SerializeField] private`, property chỉ đọc

## Acceptance Criteria

*From Docs/design/gdd/object-pooling.md, scoped to this story:*

- [ ] `AC-1 [EditMode]` **GIVEN** dịch vụ Accepting, `P.pools[plinko.coin]` = Fixed, headroom `H`, **WHEN** đăng ký `plinko.coin` với `client_declared_max` = `PlinkoConfig.maxActiveCoins` và `forbid_recycle` = true, **THEN** đăng ký thành công và bộ đếm `capacity` = `PlinkoConfig.maxActiveCoins` + `H` (24).
- [ ] `AC-2 [EditMode]` **GIVEN** cặp (declared, headroom) = (1, 0), (24, 4), (40, 8), (10, 2), **WHEN** đăng ký từng pool, **THEN** `capacity` lần lượt = 1, 28, 48, 12.
- [ ] `AC-3 [EditMode]` **GIVEN** dịch vụ Accepting, **WHEN** đăng ký lần lượt các ca sai: `id` không có trong config; prefab rỗng; `client_declared_max` = 0 và = −1; `pool_capacity` = `P.maxCapacityPerPool` + 1; pool làm tổng vượt `P.maxTotalPooledObjects`; `id` đã đăng ký; policy RecycleOldest với `forbid_recycle` = true, **THEN** mỗi ca trả thất bại, ghi đúng một lỗi Editor, không tạo đối tượng nào (bộ đếm `created` của mọi pool không đổi) và các pool đã chấp nhận trước đó giữ nguyên `capacity`.
- [ ] `AC-4 [EditMode]` **GIVEN** dịch vụ đã ở Prewarming và một lần khác ở Ready, **WHEN** đăng ký một `id` hợp lệ, **THEN** cả hai lần bị từ chối với lỗi Editor và không tạo đối tượng.
- [ ] `AC-5 [EditMode]` **GIVEN** `PoolDefinition` có prefab chứa AudioSource, **WHEN** chạy validator, **THEN** có đúng một cảnh báo nêu `id` đó; đăng ký vẫn thành công.

## Implementation Notes

- Namespace: mọi kiểu Pooling ở `CozyGachapon.Pooling` (hoặc namespace con) — GDD AC-32 yêu cầu, Story 006 kiểm bằng reflection. Enum/struct/interface trong `CozyGachapon.Contracts` giữ namespace Contracts (không thuộc tập AC-32, vì AC-32 chỉ quét assembly chứa kiểu dịch vụ).
- `PoolExhaustionPolicy` { `Fixed`, `RecycleOldest` } (Contracts). `IPoolService` (Contracts) theo ADR-0003; vì GDD R2/R3 lấy `prefab` và `policy` từ `PoolingConfig` còn hệ khách chỉ khai `client_declared_max` và `forbid_recycle`, chữ ký đăng ký là `PoolRegistrationResult Register(string id, int clientDeclaredMax, bool forbidRecycle)`; ghi lệch so với `RegisterPool(poolId, prefab, capacity, policy)` của ADR vào Completion Notes (cần đồng bộ ADR khi đóng story).
- `PoolRegistrationResult` (Contracts): readonly struct {`bool Accepted`, `int Capacity`, mã lý do enum `PoolRegistrationError`}. Không ném exception.
- Lỗi/cảnh báo "Editor" của GDD: luật dự án cấm `Debug.Log*`, nên lỗi và cảnh báo đi qua `IPoolDiagnosticsSink` (Contracts: `void ReportError(PoolDiagnosticCode code, string poolId)`, `void ReportWarning(...)`) được tiêm vào dịch vụ. Test dùng sink giả đếm lời gọi thay cho `LogAssert`; sink thật (nếu cần ghi ra Console) là việc của composition root, không nằm trong story này.
- `pool_capacity = client_declared_max + headroom` (`PoolCapacity`, hàm thuần). `registration_valid` = phase Accepting ∧ id có trong config ∧ prefab khác null ∧ id chưa đăng ký ∧ `client_declared_max` ≥ 1 ∧ `pool_capacity` ≤ `maxCapacityPerPool` ∧ `registered_capacity_total + pool_capacity` ≤ `maxTotalPooledObjects` ∧ ¬(RecycleOldest ∧ `forbid_recycle`). Mọi giá trị trần đọc từ `PoolingConfig`, không literal trong logic.
- Một ca sai chỉ báo đúng một lỗi (kiểm theo thứ tự điều kiện, dừng ở điều kiện sai đầu tiên). Từ chối không tạo đối tượng, không đổi `registered_capacity_total`.
- Phạm vi story này: trạng thái dịch vụ chỉ cần `Accepting` / `Prewarming` / `Ready` làm enum và cổng kiểm tra pha; chuyển trạng thái do Story 002 điều khiển (test AC-4 đặt trạng thái qua hàm internal/test seam, không qua prewarm thật).
- Bộ đếm R9 `capacity` và `created` đọc qua `bool TryGetStats(string id, out PoolStats stats)` — không cấp phát. Các bộ đếm còn lại thêm dần ở các story sau, struct `PoolStats` có đủ trường từ đầu (giá trị 0).
- `PoolingConfigValidator` (thuần C#, không ghi log): trả danh sách cảnh báo/lỗi; AC-5 dùng `prefab.GetComponentInChildren<AudioSource>(true)` ở prefab nhẹ của test. Validator không chặn đăng ký.
- Dự án: không comment trong code, không LINQ, file ≤ 250 dòng, không static singleton; dịch vụ được tạo qua constructor với `PoolingConfig` và `IPoolDiagnosticsSink`.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 002]: Prewarm, chuyển trạng thái Accepting → Prewarming → Ready, đăng ký `IBootStep`.
- [Story 003]: Get / Release và phiếu mượn.
- [Story 004]: Chính sách khi cạn.
- AC-28 (đăng ký pool MVP thật) — cần prefab MVP của Plinko/Feedback, hoãn tới epic Plinko Board và Feedback & Juice.

## QA Test Cases

- **AC-1**: đăng ký `plinko.coin`
  - Given: `PoolingConfig` test có `plinko.coin` Fixed, headroom `H`; dịch vụ Accepting
  - When: `Register("plinko.coin", maxActiveCoins, true)`
  - Then: `Accepted` = true; `TryGetStats` cho `capacity` = `maxActiveCoins` + `H`; `created` = 0
  - Edge cases: `H` = 0 và `H` > 0 đều cho `capacity` đúng công thức
- **AC-2**: bảng dung lượng
  - Given: bốn pool test với headroom 0, 4, 8, 2 (trong giới hạn `maxTotalPooledObjects` của config test hoặc mỗi ca một dịch vụ mới)
  - When: đăng ký với declared 1, 24, 40, 10
  - Then: `capacity` = 1, 28, 48, 12
  - Edge cases: ca (40, 8) = 48 vẫn ≤ `maxCapacityPerPool` theo PoolingConfig test
- **AC-3**: các ca đăng ký sai
  - Given: dịch vụ Accepting, một pool hợp lệ đã đăng ký trước, sink giả
  - When: lần lượt `id` lạ; prefab null; declared 0; declared −1; capacity = `maxCapacityPerPool` + 1; pool làm vượt `maxTotalPooledObjects`; `id` trùng; RecycleOldest + `forbid_recycle` = true
  - Then: mỗi ca `Accepted` = false, sink nhận đúng 1 lỗi, `created` của mọi pool không đổi, `capacity` của pool đã nhận trước đó không đổi
  - Edge cases: ca tổng vượt không làm đổi `registered_capacity_total`; pool đã nhận trước vẫn đăng ký lại bị coi là trùng `id`
- **AC-4**: đăng ký sai pha
  - Given: hai dịch vụ, một ở Prewarming, một ở Ready
  - When: đăng ký một `id` hợp lệ trên mỗi dịch vụ
  - Then: cả hai bị từ chối, sink nhận 1 lỗi mỗi dịch vụ, `created` = 0
  - Edge cases: cùng `id` đó đăng ký được khi dịch vụ ở Accepting (đối chứng)
- **AC-5**: prefab có AudioSource
  - Given: `PoolDefinition` có prefab chứa AudioSource
  - When: chạy `PoolingConfigValidator`, rồi đăng ký pool đó
  - Then: đúng một cảnh báo nêu `id`; `Accepted` = true
  - Edge cases: prefab không có AudioSource → 0 cảnh báo

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolRegistrationTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] `Tests/EditMode/Pooling/PoolRegistrationTests.cs`, `PoolRegistrationRejectionTests.cs`, `PoolingConfigValidatorTests.cs` (EditMode 169/169)

## Dependencies

- Depends on: Bootstrap & Scene Flow epic (Complete)
- Unlocks: Story 002

## Completion Notes
**Completed**: 2026-10-25
**Criteria**: 5/5 passing (AC-1..AC-5 by EditMode tests)
**Harness**: unity-harness all PASS (rules, compile, EditMode 169/169); smoke n/a (no scene change)
**Run result**: N/A - pure C# service, nothing on screen
**Deviations**: ADVISORY - registration signature `Register(id, clientDeclaredMax, forbidRecycle)` differs from ADR-0003 `RegisterPool(poolId, prefab, capacity, policy)` (prefab/policy/headroom come from PoolingConfig per GDD R2/R3; ADR text to be synced); Contracts pooling types under `Assets/CozyGachapon/Scripts/Contracts/Pooling/`
**Test Evidence**: EditMode tests above (+ `CountingPoolDiagnosticsSink.cs`, `PoolingTestFixture.cs`), passing
**Visual sign-off**: n/a (Logic)
**Code Review**: unity-dev review + 1 fix round; 1 minor (int overflow in capacity, negative headroom) fixed after the run with a wide-capacity check and test `Register_DeclaredIntMax_RejectedWithoutOverflow`; LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none
