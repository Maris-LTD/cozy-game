# Architecture Review: Cozy Gachapon Room (Tiệm Trứng Thư Giãn)
Date: 2026-10-21
Unity: 6000.3.16f1 (URP 2D 17.3.0, Input System 1.19.0, uGUI 2.0.0)
Mode: full
GDDs reviewed: 11   ADRs reviewed: 7 (Accepted: 7)
Reviewed files:
- Docs/design/gdd/economy-core.md
- Docs/design/gdd/pet-catalog.md
- Docs/design/gdd/bootstrap-and-scene-flow.md
- Docs/design/gdd/object-pooling.md
- Docs/design/gdd/audio.md
- Docs/design/gdd/plinko-board.md
- Docs/design/gdd/inventory.md
- Docs/design/gdd/gacha-machine.md
- Docs/design/gdd/shelf-and-synergy.md
- Docs/design/gdd/hud-and-tab-navigation.md
- Docs/design/gdd/feedback-and-juice.md
- Docs/architecture/adr-0001-layered-assembly-architecture.md
- Docs/architecture/adr-0002-bootstrap-composition-root.md
- Docs/architecture/adr-0003-zero-allocation-object-pooling.md
- Docs/architecture/adr-0004-priority-voice-management.md
- Docs/architecture/adr-0005-layered-income-pipeline.md
- Docs/architecture/adr-0006-world-space-plinko-physics.md
- Docs/architecture/adr-0007-event-driven-feedback-pipeline.md

## Traceability summary
Total requirements: 44 | ✅ Covered: 32 (72.7%) | 🟡 Proposed: 0 | ⚠️ Partial: 10 (22.7%) | ❌ Gaps: 2 (4.5%) | ❓ Not assessed: 0

## Coverage gaps (no ADR)
- ❌ **TR-gacha-002**: gacha-machine.md → Gacha Machine → Drop pool resolver with public rarity weights producing starter pets
  - Suggested ADR: "ADR-0008: Weighted Random Drop Pool Resolution & Gacha Seed Determinism" (game-adr)
  - Domain: Gameplay / RNG
  - Engine risk: LOW
- ❌ **TR-pet-003**: pet-catalog.md → Pet Catalog → Editor validation for pet parameter bounds and group assignments
  - Suggested ADR: "ADR-0009: ScriptableObject Catalog Schema & Editor Data Validation" (game-adr)
  - Domain: Tooling / Data Integrity
  - Engine risk: LOW

## Partial Coverage (10 items)
- ⚠️ **TR-pet-001**: Data-driven ScriptableObject catalog (covered at asmdef/lifecycle level by ADR-0001, ADR-0002; data schema details in GDD).
- ⚠️ **TR-pet-002**: Multiplier calculation based on rarity factor (covered at pipeline tier level by ADR-0005; specific pet formulas in GDD).
- ⚠️ **TR-inventory-001**: In-memory store tracking owned pets, duplicates, and stars (covered by ADR-0001 Features layer & ADR-0002 lifecycle).
- ⚠️ **TR-inventory-002**: Mutation events for pet acquisition and duplicate increments (covered by ADR-0001 Contracts event channels).
- ⚠️ **TR-inventory-003**: Read-only query interfaces for shelf selection (covered by ADR-0001 Contracts interfaces).
- ⚠️ **TR-gacha-001**: Single-pull gacha machine with dynamic coin cost querying Economy Core (covered by ADR-0001 IGachaMachine & ADR-0005 cost curve).
- ⚠️ **TR-gacha-003**: Atomic integration dispatching new acquisitions to Inventory (covered by ADR-0001 layer contracts).
- ⚠️ **TR-shelf-001**: 3 to 5 shelf slot loadout state reading from Inventory (covered by ADR-0001 Features layer).
- ⚠️ **TR-shelf-002**: Synergy evaluation for group matching and adjacent bonuses (covered by ADR-0005 SynergyMultiplier layer).
- ⚠️ **TR-hud-001**: Top header displaying formatted coin balance, CPS rate, and active boost (covered by ADR-0005 NumberFormatter & ADR-0002).

## Cross-ADR conflicts
- **Conflict analysis**: 0 conflicts detected.
- **Data ownership**: Cleanly decoupled (Wallet owns balances; PoolService owns pooled GameObjects; AudioService owns voice instances; Plinko owns physics simulation state).
- **Interface contracts**: Aligned via `CozyGachapon.Contracts`.
- **Budgets**: Fits within 60 FPS, <= 100 draw calls, <= 1024 MB RAM, 0 B GC/frame runtime.

## ADR dependency order (+ unresolved dependencies, cycles)
- **Dependency Graph**:
  - `ADR-0001` (Layered Assembly Architecture)
    - ➔ `ADR-0002` (Bootstrap Composition Root)
      - ➔ `ADR-0003` (Object Pooling Service)
        - ➔ `ADR-0006` (World Space Plinko Physics)
          - ➔ `ADR-0007` (Feedback & Juice Pipeline)
      - ➔ `ADR-0004` (Priority Voice Management)
        - ➔ `ADR-0007`
    - ➔ `ADR-0005` (Layered Income Pipeline)
- **Dependency cycles**: None (0 cycles; clean Directed Acyclic Graph).
- **Recommended implementation order**:
  1. Layer 1 (Substrate): ADR-0001 (Split asmdefs)
  2. Layer 2 (Foundation Roots): ADR-0002 (Bootstrap), ADR-0005 (Income Pipeline)
  3. Layer 3 (Foundation Services): ADR-0003 (Pooling), ADR-0004 (Audio)
  4. Layer 4 (Core Simulation): ADR-0006 (Plinko World Physics)
  5. Layer 5 (Presentation): ADR-0007 (Feedback & Juice)

## GDD revision flags
- Không có cờ yêu cầu sửa đổi GDD: Toàn bộ 11 GDD đều tương thích với hành vi và giới hạn của Unity 6000.3.16f1.

## Unity compatibility issues (+ Unity specialist findings)
> **Unity Specialist Review**: CONCERNS (Conditional Approval)
1. **Unity 6 API Deprecation**:
   - `Rigidbody2D.velocity` đã bị deprecate trong Unity 6. Bắt buộc sử dụng `Rigidbody2D.linearVelocity` cho toàn bộ mã nguồn xử lý xu Plinko.
   - Tránh dùng `FindObjectOfType` (đã bị cấm bởi ADR-0002 Composition Root).
   - Input System `com.unity.inputsystem` 1.19.0: Sử dụng `Pointer.current` / `Touchscreen.current` thay vì `Input.touches`.
2. **Missing Dependency (DOTween)**:
   - ADR-0007 quy định sử dụng `DOTween` cho animation tweening, nhưng DOTween hiện chưa được cài đặt trong `Packages/` hoặc `Assets/`. Cần import package hoặc bổ sung vào dependency trước khi triển khai Presentation layer.
3. **Zero-Alloc UI Formatter**:
   - Để đảm bảo 0 B GC/frame ở HUD, phải sử dụng `TMP_Text.SetText()` với char array/argument buffer hoặc cập nhật text theo event thay đổi (`OnCoinsChanged`), không thực hiện `string + string` trong `Update()`.
4. **Sub-Canvas Splitting**:
   - Cần tách riêng canvas hiển thị các số động (tiền, CPS) khỏi canvas tĩnh (khung nền, tab navigation) để tránh Canvas Rebuild toàn phần khi số tiền nhảy liên tục.

## Architecture document coverage
- `Docs/architecture/architecture.md` bao phủ toàn bộ 11 hệ thống MVP trong 5 tầng asmdef.
- Luồng khởi tạo `IBootStep` (6 pha) và luồng dữ liệu thời gian thực (Input ➔ Physics ➔ Event ➔ Wallet/Pooling ➔ Feedback) được mô tả đầy đủ.

## Verdict: CONCERNS
- **Lý do**:
  1. Có 2 khoảng trống yêu cầu (TR-gacha-002, TR-pet-003) và 10 mục Partial coverage ở tầng Feature/Tooling cần được lưu ý trong giai đoạn implementation.
  2. Thư viện `DOTween` trong ADR-0007 chưa có sẵn trong project.
  3. Cần chuyển đổi file `CozyGachapon.Runtime.asmdef` đơn lẻ hiện tại thành 5 asmdefs module hóa theo ADR-0001 trước khi bắt đầu code.
- Toàn bộ nền tảng Foundation và Core đều hợp lệ, không có xung đột hay chu trình phụ thuộc.

## Required ADRs (Optional/Future)
1. **ADR-0008: Weighted Random Drop Pool Resolution & Gacha Seed Determinism** (Feature - Gacha)
2. **ADR-0009: ScriptableObject Catalog Schema & Editor Data Validation** (Core/Tooling - Pet Catalog)
