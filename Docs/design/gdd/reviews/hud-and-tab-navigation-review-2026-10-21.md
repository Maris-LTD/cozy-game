# Review — hud-and-tab-navigation — 2026-10-21 — Verdict: APPROVED
Document: Docs/design/gdd/hud-and-tab-navigation.md (git hash-object 05b986fde4573f2bce0836fd4532d4f56fb7d735)
Registry: Docs/design/registry/entities.yaml (git hash-object 86c897c269e1a6a58b77974d2f52fb7e6151614c)
Scope signal: L
Specialists: none (lean review)
Blocking items: 0 | Recommended: 14
Prior verdict resolved: Yes (Round 1 NEEDS REVISION, 1 blocking, đã giải quyết)
Summary: Round 2 (kiểm chứng). Mục chặn duy nhất của round 1 đã được sửa đúng: H9.2 nêu modal Closing không tính vào `modalStackMax`, H9.1 chốt `ModalClosed`/`onClosed` chạy sau khi nhả token và Back handler, Edge Cases nêu `Open` từ `onClosed`, và AC-41 kiểm đúng kịch bản PendingPresentation của Gacha. Không phát sinh mâu thuẫn chặn mới. 14 khuyến nghị của round 1 vẫn mở (fix round chỉ chạm phần modal), trong đó cơ chế nuốt Back/token trong Closing vẫn tự mâu thuẫn.
Findings:
- [RECOMMENDED] H9.6 / AC-26 / AC-40 / Game Feel: token và Back handler nhả ngay lúc Closing bắt đầu, nhưng H9.6 và AC-40 đòi "Back bị nuốt, không rơi xuống Bootstrap" và chạm nav bị chặn; với ngăn xếp Bootstrap rỗng Back sẽ thành `RequestTab(Board)`, và world (Plinko) chạm được xuyên scrim đang mờ dần. Chốt cơ chế: HUD giữ một handler/token nuốt riêng tới hết `modalCloseSeconds` (không tính vào độ sâu), sửa AC-26 tương ứng.
- [RECOMMENDED] Dependencies / Cross-References: thiếu hàng Inventory; inventory.md (Interactions, dòng 113) còn nói HUD đọc `PetAcquired` cho "huy hiệu", mâu thuẫn H2.6/HUD D4. Thêm hàng "HUD không đọc `PetAcquired` ở MVP"; báo Sync sửa ghi chú Inventory.
- [RECOMMENDED] Interactions / Dependencies (Feedback): "neo chip Xu" chưa có tên giao diện, kiểu trả về hay AC (feedback-and-juice.md ghi provisional). Đặt tên (ví dụ `CoinChipAnchor`, ổn định theo `FrameChanged`) và thêm AC.
- [RECOMMENDED] H10 / Dependencies: chưa nêu id bước `GameplayInit` của HUD và `RunsAfter` (sau `economy.core` và các bước đăng ký gốc tab); thêm id và AC thứ tự.
- [RECOMMENDED] H4.4: `SpendRejected` chưa lọc theo `currencyId` như `WalletChanged`; tiền tệ khác nháy nhầm chip Xu. Thêm lọc và AC.
- [RECOMMENDED] Visual/Audio dòng `SpendRejected`: ghi "Thuộc Economy/Audio" nhưng Feedback R10 là chủ duy nhất của `Ui.Rejected`; sửa thành Feedback & Juice.
- [RECOMMENDED] Formulas coin_count_up Output range: câu "lớn hơn chỉ khi target < countUpMinGap / countUpSnapRatio thì ngắn hơn" tự mâu thuẫn; "≈ 1.04 s" (Game Feel) chỉ đúng ở mặc định (≈ 3.7 s ở tau 0.4, ratio 0.0001). Viết lại, nêu giá trị min/max theo knob.
- [RECOMMENDED] coin_count_up + Gacha G3: `displayed` tụt sau `target` xấp xỉ `idleCps × countUpTau` trong khi nút vặn sáng theo `CanAfford` trên ví thật; người chơi thấy số nhỏ hơn giá mà nút đã sáng. Ghi rõ là chấp nhận được hoặc chốt giới hạn tụt; thêm AC với CPS lớn.
- [RECOMMENDED] AC-16 / AC-34: ở `countUpTau` min 0.05 và dt kẹp 0.25 thì k = 0.993, AC-16 sai ở cực trị knob; AC-34 chỉ kiểm hữu hạn. Thêm kiểm `displayed < target` sau khung kẹp ở tau mặc định và nêu giới hạn cực trị.
- [RECOMMENDED] hud_layout: `dpi_eff` và `safe_*_px` không có luật kẹp khi `Screen.dpi` ngoài 120–640 hoặc inset > 0.15 × `frame_h_px`; ở dpi 640 và `navMinTouchDp` 64 thì `min_touch_px` = 640 > `nav_button_w` (344), trái AC-2/Output range. Thêm `clamp` cho `dpi_eff`, đổi khẳng định thành điều kiện ở dpi hợp lệ.
- [RECOMMENDED] States nút nav: thiếu chuyển Active → Pressed (nhấn lên nút Active rồi nhả = `NoChange`); thêm entry condition cho Pressed từ Active.
- [RECOMMENDED] H9.1 vòng đời `contentView`: "tháo khi đóng" chưa nói thời điểm so với hoạt cảnh đóng; vì `onClosed` chạy ở khung bắt đầu đóng, hệ gọi có thể hủy/tái dùng view khi scrim còn mờ dần. Chốt: tháo sau `modalCloseSeconds` (hệ gọi chỉ được hủy/tái dùng sau đó) và chỉnh AC-38.
- [RECOMMENDED] Acceptance Criteria: thứ tự số vẫn sai (AC-35..AC-38 giữa AC-27 và AC-28; AC-39, AC-41, AC-40 trước AC-29), AC-37 gộp hai điều kiện; chưa có AC cho H8 (đăng ký gốc "Sắp mở"), `TabChanged` từ nguồn khác tô nút, `Open` trong Booting, áp `safeInsetsPx` vào vị trí, H9.7 `PetCardView`; AC-11 [Screenshot] cần ảnh chuẩn. Đánh số lại, tách AC-37, bổ sung.
- [RECOMMENDED] Dependencies / Cross-References Pet Catalog: ghi đúng tên trường dùng cho `PetCardView` (tên, phẩm cấp, `sprite`, màu theo phẩm cấp) và báo Sync thêm HUD vào bảng Dependencies của pet-catalog.md.
- [RECOMMENDED] Open Questions "Cần Sync cập nhật": cập nhật danh sách theo hiện trạng (systems-index hàng 10 đã khớp; còn bootstrap-and-scene-flow.md dòng 387 và 486 "Provisional / chưa có", inventory.md ghi chú "huy hiệu", pet-catalog.md thêm HUD, feedback-and-juice.md bỏ "provisional" ở hàng HUD; Status GDD "In Review" cần Sync đồng bộ sang Approved).

## Design Review: HUD & Tab Navigation (Round 2, verification)
Specialists consulted: none (lean review)
Re-review: Yes, prior verdict NEEDS REVISION on 2026-10-21 (round 1)

### Prior blocking findings
| Finding | Status | Evidence |
|---|---|---|
| Modal Closing có tính vào `modalStackMax` không; `Open` từ `ModalClosed` có thể `StackFull` | Resolved | H9.2 "modal Closing không tính"; H9.1 `ModalClosed` sau khi nhả token/handler; States Opening entry; Edge Cases; AC-41 |

### Completeness: 5/5 required sections present (workflow standard)
Không đổi so với round 1. Dài 380 dòng (< 500).

### Dependency graph
Không đổi: các GDD phụ thuộc tồn tại; tutorial-and-onboarding.md, shop.md chưa có (provisional, không chặn).

### New contradictions in touched sections
Không có mục chặn mới. Phần chạm (H9.1, H9.2, H9.6, States modal, Edge Cases, AC-41) không có số trần trụi; AC-41 đo được ([EditMode], GIVEN/WHEN/THEN). Mâu thuẫn còn lại ở H9.6 / AC-40 đã nằm trong khuyến nghị đầu.

### Scope signal
L.

### Verdict: APPROVED
