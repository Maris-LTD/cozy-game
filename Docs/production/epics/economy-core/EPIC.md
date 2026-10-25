# Epic: Economy Core

> **Layer**: Foundation
> **GDD**: Docs/design/gdd/economy-core.md
> **Code area**: Assets/CozyGachapon/Scripts/Core/Economy/ (assembly `CozyGachapon.Core` theo architecture.md — asmdef chưa tồn tại, story đầu tiên phải tạo)
> **Status**: In Progress
> **Stories**: 18 — see table

## Overview

Economy Core là lớp kinh tế mà mọi hệ khác cắm vào: ví đa tiền tệ (MVP chỉ có `currency.coin`), Income Pipeline biến mọi nguồn thu thành xu qua hai kênh Idle (CPS từ thú trên kệ) và Plinko (mỗi xu giải quyết, kể cả tự thả và Bùng Xu), cùng đường giá neo: giá quay Gacha tăng theo số lần quay trong tiệm, mọi giá khác là hệ số nhân của giá quay, còn `baseCoinValue` của Plinko tăng chậm hơn theo cùng đường đó. Epic hiện thực ví, pipeline với thứ tự cộng/nhân và trần, họ công thức giá, định dạng số lớn K/M/B/T, bước boot `economy.core` (pha `GameplayInit`) và cài đặt `IBaseCoinValueSource`. Epic chỉ định nghĩa hợp đồng (kênh, tầng, trần); giá trị buff từng thú thuộc Pet Catalog / Shelf & Synergy.

Epic có thể làm trước Plinko Board vì chỉ cần hợp đồng `CoinResolved` và `IBaseCoinValueSource` trong Contracts; test dùng Plinko giả và Bootstrap giả.

## Governing ADRs

| ADR | Decision summary | Engine risk |
|-----|------------------|-------------|
| ADR-0005: Layered Income Pipeline | Ví double không âm, pipeline phân tầng cộng trước nhân sau có trần, đường giá 100 * 1.05^n, formatter K/M/B/T không cấp phát | LOW |
| ADR-0001: Layered Assembly Architecture | `IWallet`, `IIncomePipeline`, `IBaseCoinValueSource` nằm ở Contracts, cài đặt ở assembly Core | LOW |
| ADR-0002: Bootstrap Composition Root | Bước `economy.core` pha `GameplayInit`, `Paused`/`Resumed`, `Interactive` + `IsSimulationRunning` | LOW |

## Requirements

| ID | Requirement | ADR coverage |
|----|-------------|--------------|
| TR-economy-001 | Multi-currency wallet tracking double-precision Coin amounts without negative balances | ADR-0005 ✅ |
| TR-economy-002 | Layered Income Pipeline with additive base and multiplicative tier caps | ADR-0005 ✅ |
| TR-economy-003 | Dynamic formula family for base coin value scaling with gacha cost | ADR-0005 ✅ |
| TR-economy-004 | Exponential gacha pull cost curve 100 * 1.05^n | ADR-0005 ✅ |
| TR-economy-005 | Compact string formatting for big numbers (K, M, B, T) backed by double values | ADR-0005 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed and closed with `game-done`.
- All acceptance criteria (AC-1…AC-51) from `Docs/design/gdd/economy-core.md` are verified.
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`. At qaLevel minimal tests are advisory.
- Visual/Feel and UI stories have screenshots kept in `Docs/production/qa/evidence/economy-core/` (Visual/Feel cần có sự xác nhận của người dùng trong `game-done`).
- `unity-harness all` passes.
- Asmdef `CozyGachapon.Core` được tạo đúng ADR-0001 (references chỉ `Contracts`/`Foundation`).
- GDD lưu ý: header `Status` của GDD là `In Review` trong khi systems-index ghi Approved; các giá trị số là tạm cho tới mô phỏng bảng tính chung với Pet Catalog và Shelf & Synergy (đã nêu trong GDD). Economy cần bổ sung `PreviewIdleCps(plan)` và `GetSourceIdleCps(sourceKey)` cho Shelf (xem Open Questions của Shelf & Synergy) — chưa có trong danh sách AC.
- Lệch tầng: systems-index xếp Economy Core ở Foundation nhưng architecture.md đặt vào assembly Core (tầng Core, phụ thuộc hợp đồng Plinko). Giữ hai nhãn như nguồn, không đổi ở epic này.

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Core Assembly, Economy Contracts & Wallet Add | Logic | Complete | ADR-0005, ADR-0001 |
| 002 | Spend, CanAfford & Player Action Context | Logic | Complete | ADR-0005 |
| 003 | Spin Price Curve, nice_ceil & Derived Price | Logic | Complete | ADR-0005 |
| 004 | Economy Core Initialization & State Snapshot | Logic | Ready | ADR-0005, ADR-0001 |
| 005 | Spin Payment, Base Coin Value Source & Shop Reset | Logic | Ready | ADR-0005 |
| 006 | Number Formatter (K, M, B, T, aa…) | Logic | Complete | ADR-0005 |
| 007 | Income Modifier Tiers & Caps | Logic | Ready | ADR-0005 |
| 008 | Tier Ownership & Modifier Registration Rules | Logic | Ready | ADR-0005 |
| 009 | Idle CPS Computation, Dirty Flag & Offline Snapshot | Logic | Ready | ADR-0005 |
| 010 | Per-Source Idle CPS & Preview Functions | Logic | Ready | ADR-0005 |
| 011 | Plinko Coin Credit (`CoinResolved` → Wallet) | Logic | Ready | ADR-0005 |
| 012 | SubmitIncome, Resolve-Time Modifiers & Invalid Input | Logic | Ready | ADR-0005 |
| 013 | Lifecycle State Machine & Idle Accrual | Logic | Ready | ADR-0002, ADR-0005 |
| 014 | Ad Idle Compensation & Wallet Event Integrity | Logic | Ready | ADR-0005, ADR-0002 |
| 015 | Published Income Rates & Plinko Soft Cap | Logic | Ready | ADR-0005 |
| 016 | Balance Model Targets (first spin, seconds per spin) | Logic | Ready | ADR-0005 |
| 017 | Pillar 4 API Guards (reflection, boundary inputs, spin count) | Logic | Ready | ADR-0005 |
| 018 | `economy.core` Boot Step, Composition Root Wiring & Zero-Alloc Frame | Integration | Ready | ADR-0002, ADR-0005, ADR-0001 |

Số thứ tự không phải thứ tự build. Thứ tự build theo phụ thuộc: 001 → 002, 003, 006 → 004 → 007 → 009 → 008 → 005 → 011 → 012 → 013 → 010, 014 → 015 → 016 → 017 → 018 (xem mục Dependencies của từng story). Số story (18) vượt khoảng 5-10 kỳ vọng ở trên vì Economy Core có 51 AC; mỗi story giữ 1-4 AC và ước tính 1-2 ngày.

## Deferred acceptance criteria

Các AC dưới đây không có story trong epic này vì cần đầu vào ngoài hoặc epic khác:

- AC-23 [Play mode]: cần scene đầy đủ với Tab 2 (Tab Navigation, Shelf) chạy thật (HUD & Tab Navigation, Shelf & Synergy); logic Idle liên tục không phụ thuộc tab đã được AC-21/AC-22 (Story 013) kiểm trong EditMode.
- AC-34 [Play mode]: cần Plinko Board thật phát `CoinResolved` và đọc `IBaseCoinValueSource` (epic Plinko Board); phía Economy đã được AC-33 (Story 005) và AC-15/AC-18 (Story 011) kiểm bằng Plinko giả.
- AC-42 [Play mode]: cần Plinko thật, `InputTestFixture` chạm bàn và nút vặn trứng bấm được (Plinko Board, Gacha Machine, HUD); mô hình thời gian lượt đầu đã được AC-40 (Story 016) kiểm.
- AC-44 [Perf]: cần bản WebGL trên thiết bị tham chiếu của ADR hiệu năng, 40 modifier và 5 nguồn Idle (Platform Services / ADR hiệu năng); 0 B GC của Economy đã được AC-43 (Story 018) kiểm trong EditMode, đo thời gian CPU p95 thì để bước đo thiết bị.
- AC-45 [Screenshot]: header "12.3K", "6.5/s" và giá "110" trên nút vặn trứng thuộc HUD & Tab Navigation và Gacha Machine; bộ định dạng đã được AC-37/AC-38 (Story 006) kiểm.

## Open questions (từ bước `game-tasks`)

Không có story nào bị `Blocked`; mỗi điểm dưới đây có giả định ghi trong story liên quan, cần chốt trước hoặc trong lúc làm story đó.

- **ADR-0005 lệch GDD**: ADR phác `IWallet` một đồng (`Credit`, `Coins`), bốn `ModifierLayer`, `IBaseCoinValueSource.GetCurrentBaseCoinValue()` và ném ngoại lệ khi trùng modifier ID. GDD (bản sau, đã đối chiếu Plinko Revision 3) dùng ví đa tiền tệ có `reason`, năm tầng, property `BaseCoinValue` và thay thế khi trùng. Các story theo GDD và ghi lệch vào Completion Notes; nên mở ADR-0005 sửa lại (Story 001, 005, 007, 008).
- **"Ngữ cảnh hành động người chơi" (Story 002, 017)**: GDD giao cho ADR nhưng ADR-0005 chưa chốt. Giả định `PlayerActionContext` (struct, `default` không hợp lệ) cấp bởi `IPlayerActionIssuer`. Cần ADR hoặc xác nhận.
- **Cơ chế sự kiện và registry dịch vụ (Story 005, 018)**: chưa có ADR; code hiện dùng C# event trên các lớp Foundation và chưa có registry. Story dùng C# `event Action<T>` với struct trong Contracts và `IServiceRegistry` tối thiểu (Story 018). Cần chốt trước khi Plinko Board lấy `IBaseCoinValueSource`.
- **`ISimulationState` chưa có ở Contracts (Story 013, 014, 018)**: `IsSimulationRunning` và `IsAdOpen` chỉ tồn tại trong `AppLifecycleWatchdog`/`StartupCurtainController` (Foundation). Story thêm interface ở Contracts và adapter ở Runtime.
- **Sở hữu hợp đồng Plinko (Story 005, 011)**: `CoinResolved`, `IBaseCoinValueSource`, `CoinOrigin`, `CoinResolveReason` thuộc Plinko Board nhưng epic đó chưa làm; Economy định nghĩa bản tối thiểu ở `Contracts/Plinko/`, Plinko epic phải nhận lại không đổi tên trường. Cách biểu diễn `bucketIndex | none` (−1 hay cờ) cần chốt.
- **`IValidatableAsset` chưa có (Story 001)**: ADR-0009 mô tả nhưng Contracts và `CozyGachapon.Editor` chưa có; Economy theo mẫu validator thuần của Audio.
- **Bản build đăng ký tiền tệ (Story 004)**: AC-1 nói `releaseTier` ≤ bản build nhưng GDD không nói nguồn của giá trị; giả định truyền `ReleaseTier.Mvp` từ composition root.
- **Tên `reason` cho thu nhập Idle và Plinko trong `WalletChanged` (Story 001, 014)**: R14 liệt kê nguồn nhưng không đặt tên enum; giả định hai giá trị `IdleIncome`, `PlinkoIncome`. `origin`/`reason` của `IncomeCredited` từ `SubmitIncome` cũng chưa định nghĩa (Story 012).
- **Chi tiết GDD chưa quy định**: `Add` đúng bằng trần có phát `WalletChanged` delta 0 hay không (Story 001); `TrySpend` thiếu ngữ cảnh có phát `SpendRejected` hay không (Story 002); thứ tự `WalletChanged` và `SpinPriceChanged` trong `TryPaySpin` (Story 005); credit bằng 0 có phát `WalletChanged` hay không (Story 011); khoản Plinko đến ở Ready/Paused tính vào cửa sổ `PlinkoRateAvg` thế nào khi `running_time` chưa tăng (Story 015). Mỗi story chọn một và ghi vào Completion Notes.
- **`IIdlePlan` (Story 010)**: cấu trúc `plan` do Shelf & Synergy định nghĩa (Assumed C5[13]); Economy tạo interface đọc ở Contracts để Shelf cài.
- **`PreviewIdleCps` và `GetSourceIdleCps` đã có trong GDD** (R7.5, AC-50, AC-51) nên được phủ ở Story 010; ghi chú "chưa có trong danh sách AC" ở Definition of Done phía trên đã lỗi thời.
- **`median_fall_time` (Story 016)**: thuộc `PlinkoBalanceTargets` của Plinko Board; test truyền 2.5 s làm dữ liệu test cho tới khi Plinko epic xong.
- **Tầng của epic**: giữ nhãn Foundation theo systems index, assembly `CozyGachapon.Core` theo ADR-0001 (đã nêu ở Definition of Done); story ghi cả hai.

## Next Step

Run `game-readiness` trên các story đầu (001, 003, 006), rồi `game-dev` cho Story 001. Thứ tự build theo phụ thuộc ở mục Stories. PR-EPIC bỏ qua (lean).
