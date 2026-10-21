# Review — Feedback & Juice — 2026-10-21 — Verdict: APPROVED (Round 1)
Document: Docs/design/gdd/feedback-and-juice.md (git hash-object 4122b44802828f5ff83e91366bf911a48b3e6ef3)
Registry: Docs/design/registry/entities.yaml (git hash-object 86c897c269e1a6a58b77974d2f52fb7e6151614c)
Scope signal: L
Specialists: none (lean review)
Blocking items: 0 | Recommended: 14
Prior verdict resolved: First review (Round 1 theo workflow; một báo cáo cùng tên của vòng cũ đã bị ghi đè, các mục khuyến nghị cũ về AC-7, SpendRejected tier, haptic API, knob thiếu vẫn còn nguyên trong tài liệu)
Summary: Đủ 8 mục, công thức đúng khi thay biên (reveal 3.2 s, floor(7.5) = 7, pitch/pan), hợp đồng RequestReveal / RevealSettleStarted / RevealFinished khớp gacha-machine.md, sự kiện Plinko/Economy/Shelf khớp payload. Không có mục chặn; còn các lỗ nhỏ về trạng thái Suspended của RequestReveal, knob thiếu, AC chưa đo được và API haptic của Unity.

## Completeness: 8/8 (5 required at standard + Summary, Formulas, Tuning Knobs present and non-empty)

## Dependency graph
- OK plinko-board, economy-core, audio, object-pooling, bootstrap-and-scene-flow, pet-catalog, hud-and-tab-navigation, gacha-machine, shelf-and-synergy exist
- Settings & Accessibility, Tutorial: chưa có GDD (Alpha/VS, soft, chấp nhận)
- Hai chiều: Gacha liệt kê Feedback (OK) nhưng dòng 199 ghi "Feedback nghe Gacha", trái với Feedback "không nghe sự kiện Gacha nào"; Plinko/Pooling/Audio/HUD còn tham chiếu PlinkoFeedbackConfig hoặc "provisional" (đã nằm trong ghi chú Sync).

Findings:
- [RECOMMENDED] States / R11: `RequestReveal` khi hệ ở Suspended hoặc Uninitialized chưa quy định ("bỏ sự kiện mới" có thể bị hiểu là bỏ lời gọi, Gacha không nhận RevealFinished và giữ khóa tab); thêm: nhận lời gọi, vào Charging nhưng đứng yên tới Resumed (Uninitialized: Finished(skipped=true) ngay) + AC.
- [RECOMMENDED] R13 / effect_admit: khi active = B mà không còn bậc 3 hay số bay để cắt, "active ≤ B luôn đúng" không bảo đảm; nêu nhánh (bỏ hiệu ứng bậc 1 mới, không bao giờ bỏ reveal) và thêm vào AC-16.
- [RECOMMENDED] R4 hàng SpendRejected, ComboTierChanged, ShelfSlotUnlocked ghi tier 2 trong khi R10/AC-27 nói SpendRejected không tốn ngân sách và R13 chỉ gán bậc 2 cho reveal; ghi "—" cho SpendRejected, và định nghĩa bậc cho hai cue Shelf (chỉ cue, không hiệu ứng hình).
- [RECOMMENDED] R14 haptic: `hapticTapMs` (0–50 ms) không thực hiện được bằng API Unity 6000.3 (Handheld.Vibrate không có thời lượng; iOS chỉ Light/Medium/Heavy; WebGL không có). Đổi sang enum cường độ hoặc ghi cần plugin native + ADR; thêm vào ADR của Open Questions.
- [RECOMMENDED] R14 / haptic_allow: tap Manual rung 10 ms có thể chặn jackpot/reveal bởi `hapticMinInterval`; thêm ưu tiên (jackpot/reveal vượt tap) và chỉ rung jackpot khi ActiveTab = Board hoặc không đè lên reveal.
- [RECOMMENDED] Tuning Knobs: thiếu knob cho thời lượng nháy khấc thả (Manual), pop nhỏ Auto, thời lượng/độ mờ nhãn "BONUS!", độ mờ khấc BoardBusy, biên độ rung trứng ở Charge, scale co của fizzle, thời lượng punch ô thú (đang mượn bucketPunchDuration); nội dung "số nhỏ" của fizzle chưa nêu (CoinStuck không mang số tiền). Thêm hàng knob hoặc đánh dấu hằng số kèm lý do.
- [RECOMMENDED] Tuning Knobs: `feedbackDrawCallBudget` chỉ xuất hiện ở AC-29, không có luật runtime nào dùng; ghi rõ là ngưỡng kiểm thử hay nối vào effect_admit.
- [RECOMMENDED] floating_text_pose / R6: gộp thêm khoản đặt lại tuổi sống nên y(t) nhảy về 0 giữa chừng (số tụt xuống); giữ y hiện tại, chỉ đặt lại alpha/thời gian sống còn lại, hoặc nêu đây là chủ ý.
- [RECOMMENDED] R11 tua: không nêu hạt/ánh sáng/haptic của Burst khi tua từ Charge (chỉ cue RarityReveal). Quy định: ánh sáng phẩm cấp hiện ngay ở Settle với hạt = particleCount × k rút gọn, haptic Burst vẫn phát một lần.
- [RECOMMENDED] busy_throttle: "giờ thực không dừng khi Paused" mâu thuẫn "dùng giờ tween của Feedback" (dừng khi Paused); chọn một và nói rõ (khuyến nghị giờ tween, Suspended dù sao đã bỏ sự kiện).
- [RECOMMENDED] Acceptance Criteria: AC-1 chỉ miễn trừ Ui.Rejected trong khi Gacha.*, Shelf.ComboTierUp, Shelf.SlotUnlock cũng provisional; AC-7 ngoặc "kể cả khi ActiveTab ≠ Board với phần cue" mâu thuẫn với "đúng hai số bay" (tách tab Board: số bay + cue; tab khác: chỉ cue); AC-26 "giống hệt" cần ngưỡng đo (ví dụ chênh pixel ≤ 1% sau cùng seed); AC-6 NaN chưa có trong Formulas/Edge Cases.
- [RECOMMENDED] Acceptance Criteria thiếu: ComboTierChanged/ShelfSlotUnlocked → đúng cue (R4), RequestReveal rarity None → dòng common, trạng thái Suspended bỏ sự kiện, floating_text_pose (y/alpha tại t = 0.72), RevealSettleStarted phát đúng một lần khi không tua.
- [RECOMMENDED] Open Questions: dòng "reserve 4/10" lỗi thời (knob hiện 10, không có 4); sắp lại AC-32 vào đúng thứ tự.
- [RECOMMENDED] Sync (không sửa ở đây): entities.yaml — floating_text_merge_window và busy_feedback_min_interval đổi source sang feedback-and-juice.md (đã duyệt) kèm referenced_by thêm plinko-board.md; floating_text_max_on_screen / fizzle_max_on_screen cũng phải đổi source vì knob nay thuộc FeedbackConfig; gacha-machine.md dòng 199 sửa "Feedback nghe Gacha" thành một chiều; object-pooling.md bỏ nhãn provisional hai pool.
