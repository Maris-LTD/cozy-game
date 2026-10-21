# Review — gacha-machine — 2026-10-21 — Verdict: APPROVED
Document: Docs/design/gdd/gacha-machine.md (git hash-object 2d06bdcb584086030d4b281cf2a57a577ec3bcf3)
Registry: Docs/design/registry/entities.yaml (git hash-object 86c897c269e1a6a58b77974d2f52fb7e6151614c)
Round: Round 1
Scope signal: L
Specialists: none (lean review)
Blocking items: 0 | Recommended: 10
Prior verdict resolved: First review (vòng này tính là Round 1; báo cáo cũ cùng tên bị ghi đè)
Summary: GDD đủ các mục bắt buộc ở workflow standard, công thức đúng khi thay biên (E_N = 26.06 / 19.97 / 31.18 cho N = 50 / 30 / 90; chu kỳ bảo hiểm ≤ N lượt; tỷ lệ chia lại khi pool rỗng), tên sự kiện và payload khớp Feedback, HUD, Inventory, Economy. Không có mâu thuẫn với entities.yaml (4 mục `gacha_*` đã có, `gacha_rarity_rates` chủ là Gacha). Còn các chỗ nên làm rõ về số trần chưa là knob, AC khó đo và hợp đồng Sync, không chặn triển khai. GDD dài 360 dòng, dưới trần 500.

## Design Review: Gacha Machine (Máy Trứng Gachapon)
Specialists consulted: none (lean review)
Re-review: No, first review (Round 1)

### Completeness: 5/5 required sections present (standard)
Overview, Detailed Design, Formulas, Edge Cases, Dependencies, Acceptance Criteria đều có và không rỗng. Có `## Summary`, Tuning Knobs, Cross-References, Visual/Audio, Game Feel, UI Requirements. Advisory at standard: thiếu `## Player Fantasy` (chỉ có dòng Implements Pillar).

### Dependency graph
- ✓ economy-core.md, pet-catalog.md, inventory.md, bootstrap-and-scene-flow.md, hud-and-tab-navigation.md, feedback-and-juice.md tồn tại và liệt kê Gacha
- ✓ shelf-and-synergy.md tồn tại (nhưng Gacha không liệt kê Shelf)
- ✗ audio.md: Dependencies ghi "Gián tiếp qua Feedback"; không đọc trong vòng này

### Required before implementation
- none

### Recommended revisions
1. [Dependencies] Shelf & Synergy không có dòng trong Dependencies/Interactions của Gacha dù luật lượt đầu (vai Idle) và Open Question "thú Hỗ trợ 0 CPS" phụ thuộc Shelf, và Shelf tự đặt thú mới từ `PetAcquired`. Thêm dòng soft hai chiều.
2. [Tuning Knobs] Số trần chưa là knob: fade 0.15 s, 17/100/33 ms (Game Feel, AC-29), dung sai tổng tỷ lệ 0.001, dung sai AC-2/AC-3 (1e-4), AC-10 "1 000 seed ± 0.05", cận lệch làm tròn 0.05. Đưa vào `GachaTestTargets`/`GachaConfig` hoặc đánh dấu "constant" kèm lý do.
3. [Formulas pet_pick] Chưa ghi luật chọn "phần tử đầu có v < tổng dồn" (AC-5 dựa vào: u₂ = 0.5 → `pet.lucky_cat`), dự phòng phần tử cuối có w > 0 khi sai số float, và trường hợp Σw = 0 của tập con lượt đầu (mọi thú Thường vai Idle có w = 0).
4. [G3 / Dependencies Economy] `GetSpinPrice()` mô tả là chuỗi định dạng nhưng `CanAfford` cần số; dùng `spinPrice` của `SpinPriceChanged` cho số và chuỗi `display_format` cho hiển thị. `WalletChanged` (UI Requirements) chưa có trong dòng interface Economy.
5. [G2 / States Waiting] Hiện giá từ `Waiting` dựa vào thứ tự chạy `economy.core` trước `gacha.machine` mà `RunsAfter` rỗng không bảo đảm; đọc giá lần đầu ở `Interactive` hoặc khai báo phụ thuộc hợp lệ với Bootstrap AC-2.
6. [G8.4 / UI Requirements] Chưa ghi `ModalRequest` không đặt `primaryActionId`/`secondaryActionId` (nút "Tua"/"Nhận" nằm trong `GachaRevealView`), cue của "Nhận", và kích thước nút cụ thể theo `navMinTouchDp`.
7. [Acceptance Criteria] AC-28 (reflection "phương thức tạo ra thú") không đo được nếu không có danh sách tên cho phép; viết lại: "tập phương thức public có kiểu trả về `SpinResult` chính xác là {`TrySpin`, `TryGrantFreeSpin`}". AC-32 dùng "Android tầm trung" và "24 xu": thay bằng `perf_reference_device` và knob Plinko `maxActiveCoins`. AC-33 đặt sau AC-27 (sai thứ tự số).
8. [Formulas / Naming] Payload `spinsSincePity` khác tên bộ đếm `spinsSinceLegendary`; `dropWeight` vs `PetDefinition.weight` dễ nhầm. Thống nhất tên payload.
9. [Detailed Design G4] RNG: lượt bị ép (G6/G7) không rút u nhưng luôn rút u₂, nên chuỗi RNG theo seed khác nhau giữa lượt ép và lượt thường; ghi rõ (hoặc luôn rút cả hai) để test seed cố định tái lập được.
10. [Open Questions / Sync] Danh sách Sync cuối chưa nêu: `entities.yaml` thêm `referenced_by` cho `gacha_pity_threshold` và `gacha_first_spin_rarity_role` (hiện rỗng, Shelf/Catalog dùng), Feedback `RevealFinished.handle` rỗng khi phát đồng bộ ở `Failed` và không phát `RevealSettleStarted` ở `Failed` (Gacha đã xử lý nhờ `awaitingHandle`), Audio gỡ câu hỏi mở "tua có cắt tiếng không".

### Specialist disagreements
none (lean review)

### Nice-to-have
- Thêm `## Player Fantasy` ngắn (2-3 câu) để các skill sau đọc.
- Open Question `pityThreshold = 50` cần mô phỏng cùng Economy/Shelf trước 2026-11-04 (đã có owner).

### Senior verdict [creative-director]
Không áp dụng (lean review).

### Scope signal
Rough scope signal: L (producer should verify before sprint planning)

### Verdict: APPROVED
