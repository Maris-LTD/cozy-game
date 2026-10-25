# Epic: Economy Core

> **Layer**: Foundation
> **GDD**: Docs/design/gdd/economy-core.md
> **Code area**: Assets/CozyGachapon/Scripts/Core/Economy/ (assembly `CozyGachapon.Core` theo architecture.md — asmdef chưa tồn tại, story đầu tiên phải tạo)
> **Status**: Ready
> **Stories**: Not yet created — run `game-tasks economy-core`

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

## Next Step

Run `game-tasks economy-core` to break this epic into implementable stories (balanced granularity: kỳ vọng 5-10 stories). PR-EPIC bỏ qua (lean).
