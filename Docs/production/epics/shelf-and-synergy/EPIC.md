# Epic: Shelf & Synergy

> **Layer**: Feature
> **GDD**: Docs/design/gdd/shelf-and-synergy.md
> **Code area**: Assets/CozyGachapon/Scripts/Features/Shelf/ (assembly `CozyGachapon.Features` — asmdef chưa tồn tại; view Tab 2 thuộc `CozyGachapon.Presentation`, cũng chưa tồn tại)
> **Status**: Ready
> **Stories**: Not yet created — run `game-tasks shelf-and-synergy`

## Overview

Người chơi kéo thú từ dải Kho lên một trong 3–5 ô kệ, đổi chỗ hoặc gỡ xuống, miễn phí và không hồi chiêu. Mỗi lần kệ đổi, Shelf dựng lại toàn bộ "bản kế hoạch đăng ký" (nguồn, modifier, đóng góp cho bàn) từ trạng thái kệ và dữ liệu Pet Catalog, rồi đẩy sang Economy Core (Income Pipeline) và Plinko Board (`RegisterAutoDropRate` / `RegisterBouncinessFactor`). Shelf sở hữu số ô và giá mở ô, luật mỗi loài một ô, bố cục một hàng và tính kề, bảng combo nhóm, và vẽ cả Tab 2 "Kệ & Kho" (kệ + dải Kho đọc Inventory). Pillar 3 được kiểm bằng test EditMode duyệt mọi đội hình: phải tìm ra hai đội hình tốt nhất khác nhau cho hai kênh thu nhập. Bước boot `shelf.core` chạy sau `plinko.board`.

## Governing ADRs

| ADR | Decision summary | Engine risk |
|-----|------------------|-------------|
| ADR-0005: Layered Income Pipeline | Đăng ký/hủy modifier động vào pipeline, hai kênh Idle/Plinko, cộng trước nhân sau có trần | LOW |
| ADR-0006: World Space Plinko Physics | API modifier bàn (`Register*`/`Unregister`) và trần vật lý do Plinko sở hữu | LOW |
| ADR-0001: Layered Assembly Architecture | `ShelfService` ở Features, đọc Inventory qua Contracts | LOW |
| ADR-0002: Bootstrap Composition Root | Bước `shelf.core`, `RunsAfter` `plinko.board` | LOW |

## Requirements

| ID | Requirement | ADR coverage |
|----|-------------|--------------|
| TR-shelf-001 | 3 to 5 shelf slot loadout state reading from Inventory | ADR-0001 ⚠️ partial |
| TR-shelf-002 | Synergy evaluation for group matching and adjacent slot bonuses | ADR-0005 ⚠️ partial |
| TR-shelf-003 | Dual-channel income modifier calculation for Idle CPS and Plinko boosts | ADR-0005 ✅ |
| TR-shelf-004 | Dynamic modifier registration and deregistration into Income Pipeline | ADR-0005 ✅ |

Thông tin: TR-shelf-001 và TR-shelf-002 mới được phủ một phần; không chặn ở mức standard.

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed and closed with `game-done`.
- All acceptance criteria (AC-1…AC-41) from `Docs/design/gdd/shelf-and-synergy.md` are verified.
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`. At qaLevel minimal tests are advisory.
- Visual/Feel and UI stories have screenshots kept in `Docs/production/qa/evidence/shelf-and-synergy/` (mỗi màn hình được can thiệp; Visual/Feel cần có sự xác nhận của người dùng trong `game-done`).
- `unity-harness all` passes.
- Test duyệt đội hình (Pillar 3) chứng minh không có đội hình trội cả hai kênh; AC-37 (5 ô, v1.0) chưa được tính tay trong GDD, cần mô phỏng với Economy Core.
- Phụ thuộc cần Economy Core cung cấp `PreviewIdleCps(plan)` và `GetSourceIdleCps(sourceKey)`; trước khi có, UI ẩn số Δ và nhãn CPS ô (nêu trong Open Questions). Phải theo dõi khi lập story của Economy Core.
- GDD lưu ý: header `Status` là `In Review` trong khi systems-index ghi Approved; `slotPriceCoefs` [4, 10] và `maxCrossChannelShare` 0.5 là tạm. Shop (VS) mới mở ô nên MVP chỉ cần API mở ô.

## Next Step

Run `game-tasks shelf-and-synergy` to break this epic into implementable stories (balanced granularity: kỳ vọng 5-10 stories; nếu vượt 10 cân nhắc tách phần view Tab 2). PR-EPIC bỏ qua (lean).
