# Review — Inventory — 2026-10-21 — Verdict: APPROVED
Document: Docs/design/gdd/inventory.md (git hash-object 6bcefb3aef2a644e852d82be4575ea0fa0191e6a)
Registry: Docs/design/registry/entities.yaml (git hash-object 86c897c269e1a6a58b77974d2f52fb7e6151614c)
Scope signal: M
Specialists: none (lean review)
Blocking items: 0 | Recommended: 11
Prior verdict resolved: Round 1 của lượt rà này (độc lập, đọc lại từ đầu); báo cáo cũ cùng ngày (APPROVED, 0 chặn) đã được thay thế
Summary: GDD đủ các mục bắt buộc ở workflow standard, công thức cộng/kẹp/tiến độ đúng ở cực trị, tên `AddPet` / `PetAcquired` khớp Gacha và Shelf, không mâu thuẫn entities.yaml (`gacha_copies_per_spin` = 1 khớp) hay pillar. Không có mục chặn. Còn các khuyến nghị: định nghĩa độ sâu hàng đợi chưa khớp AC-29, phụ thuộc HUD / Feedback chưa hai chiều, vài chữ lỗi thời và literal ngoài bảng knob.

Completeness: 4/4 required (Overview, Detailed Rules, Edge Cases, Dependencies) + Acceptance Criteria có; Formulas có; Tuning Knobs, Player Fantasy: advisory, Game Feel ghi "Không áp dụng". Tổng ~313 dòng (< 500).
Dependency graph: pet-catalog.md, bootstrap-and-scene-flow.md, economy-core.md, gacha-machine.md, shelf-and-synergy.md, hud-and-tab-navigation.md, feedback-and-juice.md tồn tại; star-progression, shop, prestige, neighbor-pranks, save-load chưa có GDD (đã đánh dấu, hệ VS/sau).

Findings:
- [RECOMMENDED] R5.2 / Tuning Knobs `maxQueuedEvents` / AC-29: "hàng đợi dài tối đa N" không khớp AC-29. Với FIFO lấy ra rồi mới phát, chuỗi người nghe lồng nhau chỉ làm hàng đợi dài 1, nên ngưỡng không bao giờ chạm và vòng lặp vô hạn. AC-29 lại kỳ vọng đúng 8 sự kiện rồi `Rejected(QueueFull)`. Fix: định nghĩa knob là "số sự kiện tối đa trong một lượt phát hết hàng đợi (tính cả sự kiện đang phát)".
- [RECOMMENDED] Dependencies (HUD / Feedback) / Visual-Audio: HUD và Feedback & Juice GDD không nhắc `PetAcquired`, `CollectionProgress` hay Inventory (grep không thấy); cue bóc trứng của Feedback đi theo `RequestReveal` từ Gacha, không nghe `PetAcquired`. Phụ thuộc "UI đọc Inventory" chưa hai chiều. Fix: Inventory chỉ ghi Shelf là người đọc UI, hoặc báo Sync thêm dòng Inventory vào HUD và Feedback; sửa Cross-References (Feedback R4, R11) cho đúng.
- [RECOMMENDED] Dependencies Shelf (shelf-and-synergy.md dòng 275) chỉ liệt kê `IsOwned`, `GetOwnedSpecies`, `PetAcquired`, `CollectionProgress`; Inventory cấp thêm `GetCopies`, `GetUnownedReleased`, `OwnedSpeciesCount`, `ReleasedSpeciesCount` cho màn Kho. Fix: báo Sync bổ sung vào Shelf.
- [RECOMMENDED] Dependencies (hàng Gacha, Star, Shop...) và Open Questions (Gacha owner, Sync): còn chữ lỗi thời ("GDD Gacha viết cùng đợt", "khi viết GDD Gacha", việc Sync đã làm; systems-index đã ghi Approved trong khi header GDD vẫn In Review). Fix: đổi thành "Gacha G4/G9 đã khớp", xóa việc Sync đã xong; Sync đặt lại Status cho khớp.
- [RECOMMENDED] pet-catalog.md (dòng 389, 396, 526): vẫn ghi "GDD Inventory chưa có" và "chưa có trong systems index". Fix: báo Sync / Catalog cập nhật; Inventory không cần đổi.
- [RECOMMENDED] R4.3 / Formulas `collection_progress`: tên API Catalog "danh sách thú đã phát hành" mơ hồ về việc có gồm `deprecated` không (Catalog State Released yêu cầu `deprecated` = false, Deprecated là trạng thái riêng). Fix: ghi một lần Inventory tự lọc `deprecated` và yêu cầu Catalog phơi `IsDeprecated` / danh sách kèm cờ; chốt tên API ở ADR.
- [RECOMMENDED] AC-26 [Perf] / Formulas `copies_badge_text`: "< 1 ms", "1000 lần", hậu tố "x" / "+", khoảng 9–999 là literal ngoài bảng knob. Fix: ghi "constant — benchmark" hoặc knob `perfQueryBudgetMs`; ghi chuỗi tiền / hậu tố là hằng trình bày; nêu số loài 7 (MVP) / 13 (V1_0) và máy mục tiêu.
- [RECOMMENDED] R8 vs Edge Cases / AC-17: R8 chỉ nói bỏ mục ID lạ hoặc `copies` < 1; việc gộp cộng ID trùng (AC-17: `pet.b` = 3) chỉ nằm ở Edge Cases. Fix: thêm một câu "ID trùng được gộp cộng" vào R8.
- [RECOMMENDED] R2.1 / R2.2: liệt kê bốn điều kiện (a)–(d) nhưng R2.2 viết "Đúng cả ba". Fix: "Đúng cả bốn".
- [RECOMMENDED] Tuning Knobs `showUnownedSlots`, `copiesDisplayMax`: là trình bày màn Kho (Shelf vẽ) nhưng nằm ở `InventoryConfig`; `showUnownedSlots` không tác động API nào của Inventory. Fix: ghi rõ Inventory chỉ lưu knob cho UI đọc, hoặc chuyển chủ sang `ShelfConfig` ở lần sửa Shelf kế.
- [RECOMMENDED] Acceptance Criteria: AC-29 đặt trước AC-25 (sai thứ tự) và gộp hai kiểm thử không liên quan (hàng đợi đầy; `startingPets` với save rỗng). Fix: tách thành hai AC, đánh số lại, mỗi cái một GIVEN/WHEN/THEN.
- [RECOMMENDED] Unity / ADR: R5 và AC-26 yêu cầu GC = 0 cho `AddPet` trùng; ADR phải chốt `PetAcquired` là delegate với struct payload (không boxing, không closure) và hàng đợi tái dùng bộ đệm. Chưa kiểm khả thi Unity 6000.3; không dùng API Unity nào đã deprecated.
