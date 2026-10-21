# Review — first-gdd — 2026-10-21 — Verdict: NEEDS REVISION
Document: Docs/FirstGDD.md (git hash-object b47480696ee2cfbadebcbec763fa02a151abacc0)
Registry: Docs/design/registry/entities.yaml (absent)
Scope signal: XL
Specialists: none (solo review, rigor minimal mặc định)
Blocking items: 9 | Recommended: 7
Prior verdict resolved: First review
Summary: Concept và core loop vững, pillars rõ, nhưng tài liệu chưa đủ để code Plinko, kinh tế và hệ đột kích mà không phải đoán. Ba vấn đề nặng nhất: hũ x5 ở giữa là hũ dễ trúng nhất, bounciness x2 vượt 1.0 gây vô hạn xu, và đường tăng trưởng hàm mũ không có nguồn thu nhập dài hạn tương ứng.
Findings:
- [BLOCKING] §5.1 Plinko: hũ JACKPOT x5 ở giữa có xác suất cao nhất (phân phối nhị thức, ~37%, EV ~x3) và người chơi tự chọn điểm thả → luôn chạm giữa.
- [BLOCKING] §5.3 Kẹo Bảy Màu: x2 bounciness (0.6–0.75 → 1.2–1.5 > 1.0) cộng năng lượng; kết hợp Thỏ +1 xu/lần nảy → vô hạn xu; thiếu trần nảy/despawn.
- [BLOCKING] §7 Formulas: thiếu công thức thu nhập Plinko (giá trị xu gốc, thứ tự nhân/cộng các buff), Multiplier_Synergy chưa định nghĩa, thiếu xu khởi đầu.
- [BLOCKING] §5.1 Thả xu: không giới hạn tốc độ/số xu đồng thời → autoclicker phá kinh tế, không có ngân sách hiệu năng.
- [BLOCKING] §7.1 Progression: giá 100×1.05^n vs thu nhập bị chặn (5 ô, 7 pet) → tường quanh lượt 150–200; hệ thống sao thiếu công số, trần, số bản trùng/cấp; thiếu mục tiêu sau khi đủ pet.
- [BLOCKING] §5.2 Drop pool: Gương Dò La, Hộp Ngụy Trang, Vé SWAT, tag "Động Vật" chưa định nghĩa; thiếu tỷ lệ trong từng phẩm cấp và luật trùng gadget.
- [BLOCKING] §5.3/§6 Cún Bông, Ổ Khóa: có chiếm ô kệ hay không chưa rõ.
- [BLOCKING] §6 Raid/Kidnap: mâu thuẫn Pillar 4 (Cần Câu lấy 15–25% xu vĩnh viễn); thiếu phần thưởng người đột kích, tần suất bot đột kích (cả lúc offline), tác dụng Khiên, xử lý ô bị bắt, khái niệm "két".
- [BLOCKING] §5.5/§8 Giá cố định (tiền chuộc, Trà, Mưa Xu, quà Ads, ô kệ) mất ý nghĩa khi giá quay tăng hàm mũ; cần định giá theo CPS/giá quay.
- [RECOMMENDED] §2 Pillar 3: 7 pet/5 ô, pet hiếm mạnh tuyệt đối → đội hình tối ưu hiển nhiên; cần đánh đổi giữa các pet.
- [RECOMMENDED] §8 Ads: Poki/CrazyGames dùng SDK riêng, LevelPlay/AdMob không chạy WebGL → adapter theo nền tảng; cap/cooldown mỗi loại ads.
- [RECOMMENDED] §6 Bot: ghi rõ là NPC thay vì "đối thủ"; chủ đề bắt cóc/SWAT lệch tông cozy và khán giả Poki.
- [RECOMMENDED] Scope: cân nhắc dời Raid/Kidnap sang v1.1, MVP = Plinko + Gacha + Kệ + Tiệm + Offline + Ads.
- [RECOMMENDED] §9 Tech: Plinko world-space thay vì Rigidbody2D trong Canvas; chốt UGUI hoặc UI Toolkit; số xu dùng double + định dạng K/M/B; tránh singleton; synergy tính ở một chỗ.
- [RECOMMENDED] Edge cases: lưu kết quả gacha trước animation; chặn delta thời gian âm; hết xu, kệ đầy, Flash Deal trùng pet; kho đồ phân trang.
- [RECOMMENDED] Acceptance criteria: thêm tiêu chí đo được (tỷ lệ hũ theo vị trí thả, thời gian despawn, trần offline 4h × 0.7).
