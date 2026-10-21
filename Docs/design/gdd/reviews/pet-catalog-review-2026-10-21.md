# Review — pet-catalog — 2026-10-21 — Verdict: APPROVED
Document: Docs/design/gdd/pet-catalog.md (git hash-object 5cc471e9cf91bcc7ac167faec0037a91d35afc23)
Registry: Docs/design/registry/entities.yaml (git hash-object 86c897c269e1a6a58b77974d2f52fb7e6151614c)
Scope signal: L
Specialists: none (lean review)
Blocking items: 0 | Recommended: 11 (0 NEEDS-USER)
Prior verdict resolved: Yes (Round 1 of this run; the earlier report of the same date was APPROVED with 0 blocking, and Revisions 3-6 did not reopen anything blocking)
Summary: Round 1. Cả 6 mục bắt buộc ở tier standard đều có và có nội dung; công thức tính lại đúng với mọi ví dụ (Thỏ 0.1242, Mèo Hoàng Gia 14.49, channel_parity 0.959 / 0.222). Không mâu thuẫn registry (13 thú khớp) và các thay đổi user đã duyệt (gacha_rarity_rates thuộc Gacha, đã bỏ assumedRarityRates) đã áp dụng. Còn các điểm lỗi thời sau khi Shelf / Inventory / Gacha / HUD có GDD, thiếu ràng buộc `role` cho lượt đầu của Gacha, và tài liệu dài 613 dòng (> 500).

### Dependency graph
- ✓ bootstrap-and-scene-flow.md, plinko-board.md, economy-core.md, audio.md, game-concept.md exist
- ✓ shelf-and-synergy.md, inventory.md, gacha-machine.md, hud-and-tab-navigation.md, feedback-and-juice.md exist (Catalog vẫn ghi "chưa có" cho Shelf và Inventory)
- ✗ star-progression.md NOT FOUND (VS, đã ghi đúng là chưa có)

Bidirectional: Shelf, Inventory, Gacha, HUD, Feedback đều liệt kê Pet Catalog; Gacha/Shelf nhận lại AC-11..AC-15 của Catalog (Shelf AC-15..AC-20). Khớp.

Findings:
- [RECOMMENDED] Length: 613 dòng, vượt mục tiêu 500; Revision 2-6 ở header (dòng 11-15) lặp lại nội dung đã có trong thân và Open Questions. Rút mỗi revision còn một dòng và gộp các hàng Assumed của Open Questions.
- [RECOMMENDED] Dependencies / Interactions (Gacha): bảng Catalog cấp cho Gacha pool, ID phẩm cấp, sprite, màu nhưng không cấp `role`, trong khi Gacha G6 lọc lượt đầu theo `firstSpinRole` = Idle (registry `gacha_first_spin_rarity_role` common / Idle). Thêm `role` vào interface, và thêm luật validator: pool `common` của mọi bậc build phải có >= 1 thú Idle (MVP: Vịt Gỗ, Ếch Cốm đạt) cùng một AC.
- [RECOMMENDED] Dependencies / Cross-References / R6.3 / AC-14 / AC-15: còn ghi Shelf, Inventory "chưa có" và Open Questions dòng khóa nguồn "Shelf chốt". Shelf đã chốt: `shelf.slot<i>.idle` / `.plinko` cho nguồn thu, `shelf.slot<i>.fx` cho modifier hiệu ứng thú, `shelf.combo.<group>` cho combo, `shelf.slot<i>` cho API Plinko. Đổi AC-14/15 sang "modifier có `sourceId` = `shelf.slot<i>.fx`", bỏ chữ "đề xuất", đóng hàng Open Question.
- [RECOMMENDED] R2 / R11.1 / AC: `PetCatalog.scopeBothMinTier` = `None` vẫn không phải lỗi (R2 chỉ kể ba knob `buildTier`, `balanceRosterTier`, `groupRuleMinTier`); khi đó `releaseTier < None` không bao giờ đúng nên `scope = Both` mở cho mọi thú. Thêm knob thứ tư vào danh sách lỗi và một AC cho knob bậc = `None` (còn thiếu từ báo cáo trước); thêm bộ lọc R9.1 loại thú `releaseTier = None` ở runtime.
- [RECOMMENDED] UI Requirements vs HUD GDD: HUD (H9.7) sở hữu `PetCardView` và chỉ đọc tên, phẩm cấp, icon; Catalog tự định nghĩa thẻ có nhóm, vai, mô tả hiệu ứng, CPS và AC-40 chụp màn Kho. Ghi rõ chủ thẻ là HUD, Catalog chỉ cấp dữ liệu + mẫu mô tả + quy tắc định dạng; sửa hàng HUD ở Dependencies (bỏ "Chưa có trong systems index") và đề nghị HUD đọc thêm nhóm, vai, `effectDescriptionKeys`.
- [RECOMMENDED] `collection_spins_estimate`: mô hình bỏ qua bảo hiểm Huyền Thoại của Gacha (`pityThreshold` 50, registry) và luật lượt đầu; nói rõ đây là cận trên cho phẩm cấp Huyền Thoại và validator dùng roster `buildTier` hay `balanceRosterTier` (MVP 43.4 chỉ cách cận dưới 40 là 3.4).
- [RECOMMENDED] Formulas `support_power_estimate`: `referenceNeighborTargetUnits` 2.5 lớn hơn mọi đích hợp lệ của Cáo (Thỏ 2.475, Mèo Chiêu Tài 2.25, Thiên Nga 0.99), nên ngân sách Cáo 1.000 là do chọn knob chứ không đo; đổi sang trung bình có trọng số của các thú có nguồn `.plinko` trong roster, hoặc ghi vào Open Questions.
- [RECOMMENDED] Acceptance Criteria: còn literal chưa là knob hoặc hằng có lý do: "± 0.001" (AC-20, AC-37), "± 0.0001" (AC-34), "± 0.3" (AC-38), "10.9% / 20% / 1.83 lần" (Edge Cases), ngưỡng 1000 của quy tắc định dạng, "≥ 1 thú Idle / Plinko" (R5.5). Dùng `ET.valueRelTolerance` hoặc ghi "constant" kèm lý do.
- [RECOMMENDED] AC-29 / AC-40: AC-29 dùng "máy tham chiếu của dự án" thay vì `perf_reference_device` của Bootstrap (registry); AC-40 không nêu khóa màu `PetRarityConfig.rarities[legendary]`.
- [RECOMMENDED] R8 / Tuning Knobs: `inGachaPool` của 13 thú không được ghi trong bảng, dù AC-16 và AC-18 ngầm định true; thêm cột hoặc câu "mọi thú `inGachaPool` = true".
- [RECOMMENDED] Sync (không sửa ở bước này): registry `floating_text_merge_window` và `busy_feedback_min_interval` đổi source sang `feedback-and-juice.md` (FeedbackConfig, FB D1), referenced_by thêm `plinko-board.md`; `gacha_rarity_rates` đã đúng; bỏ nhãn provisional ở `pet.cozy_fox` / `pet.chubby_king_cat`; đăng ký `bounce_uplift_at_cap`; `gacha-machine.md` dòng 295 và `economy-core.md` / `plinko-board.md` (AC-13c, `raw_rate_ceiling`) còn tham chiếu `PetBalanceTargets.assumedRarityRates` / `bounceUpliftAtCapAssumed` đã xóa; systems index và header GDD đặt `Approved`.

### Verdict: APPROVED
