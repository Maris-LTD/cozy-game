# Story 001: Hình học bàn & kiểm định thông số (Board Geometry & Config Validation)

> **Epic**: Plinko Board
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-28
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/plinko-board.md`
**Requirement**: `TR-plinko-001`

**ADR Governing Implementation**: ADR-0006: World Space 2D Physics Plinko Board vs Canvas UI Separation
**ADR Decision Summary**: Bàn Plinko mô phỏng bằng Box2D 2D trong World Space camera riêng biệt với Canvas UI, lưới đinh so le và nửa đinh gắn tường đảm bảo thông số hình học chống kẹt nêm và chống xuyên vật lý.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: World Space 2D physics tách biệt Canvas UI; tự kiểm định cấu hình cân bằng qua `IValidatableAsset`.
- Forbidden: Không đặt logic hoặc đối tượng di chuyển vật lý dưới cây phân cấp Canvas.
- Guardrails: Cấu hình lưới đinh và tường phải tuân thủ khe hở tối thiểu `min_passage_gap` và điều kiện chống xuyên hầm `maxCoinSpeed * fixedDeltaTime < pegRadius + coinRadius`.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Plinko/` (CozyGachapon.Core)
- Test: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoGeometryTests.cs` (CozyGachapon.Tests.EditMode)
- Data: `PlinkoConfig.cs`, `PlinkoBalanceTargets.cs` (ScriptableObject)

## Acceptance Criteria

*From `Docs/design/gdd/plinko-board.md`, scoped to this story:*

- [x] **AC-1 [EditMode]**: GIVEN `C` mặc định, WHEN dựng bàn, THEN có `C.pegRows` tầng; tường tại `±C.pegsPerRow * C.pegSpacing / 2`; tầng chẵn có `C.pegsPerRow - 2` đinh tự do tại `x = (k - (C.pegsPerRow - 1) / 2) * C.pegSpacing`, `k = 1...C.pegsPerRow - 2`, cộng 2 nửa đinh bán kính `C.wallBumpRadius` có tâm trên mặt tường; tầng lẻ có `C.pegsPerRow - 1` đinh tại `x = (k - (C.pegsPerRow - 2) / 2) * C.pegSpacing`; khoảng cách tầng = `C.rowSpacingRatio * C.pegSpacing` (sai số ≤ `T.geometryTolerance`).
- [x] **AC-2 [EditMode]**: GIVEN `C.bucketMultipliers`, `C.bucketWidthWeights`, WHEN dựng hũ, THEN số hũ = số phần tử của `C.bucketMultipliers` (5) theo đúng thứ tự, tổng bề rộng = `board_inner_width` (sai số ≤ `T.geometryTolerance`), đúng 2 hũ ngoài cùng có `bucket_is_jackpot`.
- [x] **AC-19 [EditMode]**: GIVEN cấu hình hiện tại và cấu hình cố ý sai, WHEN chạy validator, THEN cấu hình hiện tại thỏa mãn `C.maxCoinSpeed * fixedDeltaTime < C.pegRadius + C.coinRadius` và `min_passage_gap` (đinh-đinh, đinh-tường tầng lẻ, đinh-nửa đinh tầng chẵn, khe chéo giữa hai tầng kề nhau) ≥ `2 * C.coinRadius + C.minGapMargin`; mỗi cấu hình sai (a: `C.coinRadius = 0.20`, b: bỏ nửa đinh để đinh ngoài cùng tầng chẵn cách tường `0.5 * C.pegSpacing`, c: `C.rowSpacingRatio = 0.75` khe chéo vi phạm) đều sinh lỗi validator tương ứng.

## Implementation Notes

- Cài đặt `PlinkoConfig` kế thừa ScriptableObject và triển khai `IValidatableAsset`.
- Xây dựng lớp sinh tọa độ bàn toán học `PlinkoBoardLayout` (pure C# logic) tính toán vị trí toàn bộ đinh, nửa đinh gắn tường, thành tường, vách ngăn và cảm biến 5 hũ để EditMode test có thể dựng và kiểm tra sai số mà không cần khởi động physics scene.
- Cài đặt logic validator kiểm tra các ràng buộc hình học: chống tunneling và khe hở thông suốt tối thiểu (đặc biệt là khe chéo giữa hai tầng).

## Out of Scope

- [Story 002]: Thiết lập PhysicsMaterial2D, layer collision matrix và Rigidbody2D của đồng xu.
- [Story 003]: Vùng thả và bộ kiểm soát nhận chạm.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoGeometryTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoGeometryTests.cs` (8 tests passing)

## Dependencies

- Depends on: None
- Unlocks: Story 002



## Completion Notes
**Completed**: 2026-10-28
**Criteria**: 3/3 passing (0 deferred)
**Harness**: unity-harness all PASS (1088/1088 EditMode tests); rules PASS; compile PASS
**Run result**: N/A - pure geometry math and config validation; no visual surface modified
**Deviations**: None
**Test Evidence**: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoGeometryTests.cs` (8/8 passed)
**Visual sign-off**: n/a (not Visual/Feel or UI)
**Code Review**: unity-reviewer: clean (0 findings); LP-CODE-REVIEW skipped (lean); qa-tester suggestions noted for future test seams
**Untested criteria**: None
