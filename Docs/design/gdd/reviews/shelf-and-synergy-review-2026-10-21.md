# Review — shelf-and-synergy — 2026-10-21 — Verdict: APPROVED
Document: Docs/design/gdd/shelf-and-synergy.md (git hash-object 60e83b57d1b94e6425b4568b47b12a117d54832b)
Registry: Docs/design/registry/entities.yaml (git hash-object 86c897c269e1a6a58b77974d2f52fb7e6151614c)
Round: 2 (verification)
Scope signal: L
Specialists: none (lean review)
Blocking items: 0 | Recommended: 11
Prior verdict resolved: Yes (NEEDS REVISION, round 1, cùng ngày)
Summary: Mục chặn duy nhất của vòng 1 đã được xử lý: AC-15, 16, 18, 19, 20 nay là GIVEN/WHEN/THEN tự đứng với kệ, sự kiện và kết quả cụ thể (số khớp Pet Catalog: Thỏ 0.017, Mèo Chiêu Tài 3.0, Kẹo 1.7, Hamster 0.27). Phần sửa chỉ chạm các AC này, không phát sinh mâu thuẫn hay số trần mới. Mười một mục khuyến nghị của vòng 1 chưa được sửa, vẫn mở, không chặn.

## Design Review: Shelf & Synergy
Specialists consulted: none (lean review)
Re-review: Yes, prior verdict NEEDS REVISION on 2026-10-21

### Completeness: 6/6 required sections present (workflow standard)
Advisory at standard: thiếu `## Player Fantasy`. Độ dài 433 dòng (≤ 500).

### Prior blocking findings
1. AC-15/16/18/19/20 không tự kiểm được — **Resolved**. Mỗi AC có GIVEN/WHEN/THEN, lời gọi và giá trị đo được (AC-15: 14/20 `PegHit` scored → 14 `SubmitIncome` 0.017 × b; AC-16: 3/4 origin, 4/4 khi `includeBurstCoins` đúng; AC-18: đúng 1 Register, 1 Unregister; AC-19/20: bản ghi Synergy theo đích).

### Dependency graph
- ✓ economy-core.md · ✓ pet-catalog.md · ✓ inventory.md · ✓ plinko-board.md · ✓ bootstrap-and-scene-flow.md · ✓ feedback-and-juice.md · ✓ hud-and-tab-navigation.md · ✓ gacha-machine.md (qua Inventory)
- Chưa có GDD, hợp lệ: Star Progression, Save/Load, Shop, Offline Earnings, Tutorial, Prestige, Neighbor Pranks

### Required before implementation
Không còn.

### Recommended revisions (còn mở từ vòng 1)
1. [Formulas/Knobs] Safe range `groupComboBonuses` lệch: Formulas "0.1–0.75 (cuối tới 1.0)" vs Knobs "0.1–1.0"; thêm validator độ dài bảng = `slotCountMax − groupComboMinCount + 1`.
2. [Knobs] Số trần chưa là knob: AC-21 (10 `JackpotHit`/s, "máy chuẩn WebGL"), AC-37 (≤ 30 s), Game Feel 17/50 ms, tween 1.1× / 80 / 120 / 150 ms. Thêm `ST.stressJackpotHitsPerSecond`, `ST.pillarTestMaxSeconds`; nêu máy tham chiếu từ studio config; tween ghi Sync cho FeedbackConfig.
3. [UI/States] "Chạm giữ ô → thẻ chi tiết" chưa có trạng thái thao tác, knob (`longPressSeconds`) và AC [Play mode].
4. [S6.2] "Đăng ký lại cả lô trong cùng lời gọi": Economy không có API lô; ghi là chuỗi `Unregister`/`Register…` cùng khung, tính lại một lần theo cờ bẩn; nêu `isTemporary = false` cho modifier `shelf.`.
5. [S6.3/AC] Đổi chỗ Hamster/Kẹo sang ô khác có thể ngắt nhịp auto-drop: quy định `Register` ô mới trước `Unregister` ô cũ cùng khung và thêm AC đổi chỗ (AC-25 chỉ phủ đặt thêm).
6. [S1.3/Formulas] Nêu tên API Economy cho `derived_price`; `slotPriceCoefs` cố định `double[2]` trong khi `slotCountStart` cho phép 4 → độ dài = `slotCountMax − slotCountStart` + validator.
7. [States] `Resumed` → Running phải đọc `IsSimulationRunning` (Bootstrap); `ResetForNewShop` khi Ready/Paused chưa định nghĩa.
8. [S2.5/AC-9] Dùng `isNew` của `PetAcquired { id, isNew, copies }` thay "copies 0 → 1".
9. [Dependencies/Sync] Mục "Hai chiều" thiếu: hàng Shelf trong Economy Dependencies, HUD hàng Shelf, Feedback cue/haptic cho `ShelfChanged`/`ComboTierChanged`/`ShelfSlotUnlocked`; registry `referenced_by` của `shelf_*` còn rỗng.
10. [AC] AC-2, AC-34 (mở ô, VS) và AC-29 (Prestige, Alpha) chưa ghi nhãn bậc như AC-10; AC-37 chưa ghi hai argmax kỳ vọng cho v1.0.
11. [Player Fantasy] Thiếu `## Player Fantasy` (advisory).

### Specialist disagreements
Không có.

### Nice-to-have
- Nhãn ("+120% Thỏ", "Hoàng Gia ×3 · +50%") là khóa bản địa hóa có mẫu định dạng.
- Pillar 3 chưa xét Boost đang bật; thêm một ca Boost giả ở VS.
- UGUI hay UI Toolkit cho kéo-thả: để ADR.

### Cần Sync (không sửa ở đây)
Economy Dependencies hàng Shelf (+ `PreviewIdleCps`, `GetSourceIdleCps`); Feedback cue/haptic/tween + `ComboTierChanged`; HUD hàng Shelf; registry `referenced_by` cho `shelf_*` và knob `ShelfConfig`; systems-index hàng Shelf theo verdict APPROVED.

### Scope signal
Rough scope signal: L (producer should verify before sprint planning)

### Verdict: APPROVED

Findings:
- [RECOMMENDED] Formulas/Tuning Knobs: safe range `groupComboBonuses` không thống nhất; thêm validator độ dài bảng.
- [RECOMMENDED] Tuning Knobs/AC-21/AC-37/Game Feel: số trần, "máy chuẩn", tween chưa là knob.
- [RECOMMENDED] UI/States: chạm giữ chưa có trạng thái, knob, AC.
- [RECOMMENDED] S6.2: không có API lô ở Economy; thiếu `isTemporary`.
- [RECOMMENDED] S6.3/AC: đổi chỗ thú auto-drop vẫn ngắt nhịp; thêm thứ tự và AC.
- [RECOMMENDED] S1.3/Formulas: tên API giá; độ dài `slotPriceCoefs`.
- [RECOMMENDED] States: `Resumed` và `ResetForNewShop` ở Ready/Paused.
- [RECOMMENDED] S2.5/AC-9: dùng `isNew` của `PetAcquired`.
- [RECOMMENDED] Dependencies: Sync hai chiều Economy/HUD/Feedback/registry `referenced_by`.
- [RECOMMENDED] AC: nhãn bậc VS/Alpha; argmax v1.0 cho AC-37.
- [RECOMMENDED] Thiếu `## Player Fantasy` (advisory).
