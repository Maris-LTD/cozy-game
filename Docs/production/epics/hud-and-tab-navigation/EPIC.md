# Epic: HUD & Tab Navigation

> **Layer**: Presentation
> **GDD**: Docs/design/gdd/hud-and-tab-navigation.md
> **Code area**: Assets/CozyGachapon/Scripts/Presentation/Hud/ (assembly `CozyGachapon.Presentation` — asmdef chưa tồn tại; hiện view Bootstrap nằm trong `CozyGachapon.Runtime`)
> **Status**: Ready
> **Stories**: Not yet created — run `game-tasks hud-and-tab-navigation`

## Overview

Màn hình chính luôn có ba vùng cố định, không cuộn dọc: header trên (Xu, CPS, chip Boost, nút loa), vùng nội dung giữa (ba tab) và bottom nav dưới. HUD sở hữu hai dải cố định và lớp phủ modal (`ModalHost`, LIFO, `modalStackMax`) cùng `PetCardView` dùng chung cho Gacha và Shelf; nội dung từng tab thuộc hệ của tab đó. Trạng thái tab (`ActiveTab`, `TabChanged`, `RequestTab`, khóa tab) thuộc Bootstrap; HUD chỉ gọi `RequestTab` và tô nút theo sự kiện. Bố cục đã có trên Penpot (không có GDD UI riêng cho từng tab). Canvas uGUI, Canvas Scaler 1080x1920.

## Governing ADRs

| ADR | Decision summary | Engine risk |
|-----|------------------|-------------|
| ADR-0002: Bootstrap Composition Root | Tab router, khóa tab, cổng chặn input, rèm khởi động | LOW |
| ADR-0005: Layered Income Pipeline | Formatter số lớn không cấp phát cho Xu/CPS, `IncomeCredited` | LOW |
| ADR-0006: World Space Plinko Physics | Đồng bộ viewport 9:16–9:21, Canvas chỉ đóng khung vùng bàn | LOW |
| ADR-0001: Layered Assembly Architecture | View ở assembly Presentation, chỉ tham chiếu Contracts | LOW |

## Requirements

| ID | Requirement | ADR coverage |
|----|-------------|--------------|
| TR-hud-001 | Top header displaying formatted coin balance, CPS rate, and active boost | ADR-0005, ADR-0002 ✅ |
| TR-hud-002 | Bottom navigation bar triggering tab transitions through Bootstrap router | ADR-0002 ✅ |
| TR-hud-003 | uGUI Canvas Scaler configured for 1080x1920 reference resolution | ADR-0002, ADR-0006 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed and closed with `game-done`.
- All acceptance criteria (AC-1…AC-41) from `Docs/design/gdd/hud-and-tab-navigation.md` are verified.
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`. At qaLevel minimal tests are advisory.
- Visual/Feel and UI stories have screenshots kept in `Docs/production/qa/evidence/hud-and-tab-navigation/` (mỗi màn hình được can thiệp; Visual/Feel cần có sự xác nhận của người dùng trong `game-done`).
- `unity-harness all` passes.
- Inherited from Bootstrap & Scene Flow: AC-26 (nhấn xuống/nhả trên nút nav "Kệ" tạo đúng một `TabChanged`), AC-28 (`ActiveTab` mặc định `Board` sau rèm, phối hợp với Plinko Board), AC-39 (nút UI sát mép letterbox không phát `WorldPress`, phối hợp với Plinko Board).
- Inherited from Audio: AC-37 (screenshot icon loa ở header). AC-38 (screenshot màn Settings) cần Settings & Accessibility là hệ Alpha chưa có epic; vì MVP không có epic Settings nên AC-38 được ghi ở đây như phần đã hoãn, chỉ xử lý nếu đưa màn Settings tối thiểu vào MVP, nếu không thì chuyển sang epic Settings & Accessibility (Alpha).
- GDD lưu ý: header `Status` là `In Review` trong khi systems-index ghi Approved; chip Boost provisional tới khi Boosts (VS) có GDD vì Economy chưa có sự kiện "boost active + thời gian còn lại".

## Next Step

Run `game-tasks hud-and-tab-navigation` to break this epic into implementable stories (balanced granularity: kỳ vọng 5-10 stories). Khuyến nghị làm trước Gacha Machine vì Gacha phụ thuộc cứng `ModalHost`. PR-EPIC bỏ qua (lean).
