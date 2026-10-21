# Review — plinko-board — 2026-10-21 — Verdict: APPROVED
Document: Docs/design/gdd/plinko-board.md (git hash-object 4d46a3aa8eefe2e0d25a48b3372aad5929c50f7b)
Registry: Docs/design/registry/entities.yaml (git hash-object 86c897c269e1a6a58b77974d2f52fb7e6151614c)
Scope signal: L
Specialists: none (lean review)
Blocking items: 0 | Recommended: 16
Prior verdict resolved: Yes — mục chặn duy nhất của vòng 1 (`dropZoneLineAlpha` hai chủ) đã xử lý.
Round: 2 (verification, sau vòng sửa; ghi đè báo cáo vòng 1)
Summary: Mục chặn vòng 1 đã giải quyết: `dropZoneLineAlpha` chỉ còn ở `FeedbackConfig` (Tuning Knobs, Visual/Audio), khớp feedback-and-juice.md (bảng knob, Tutorial override). Cross-References và Dependencies đã ghi đúng shelf-and-synergy.md và feedback-and-juice.md là đã có; chỉ boosts, rewarded-ads, tutorial còn "chưa có" (đúng thực tế). Không phát hiện mâu thuẫn mới ở các đoạn đã sửa; các khuyến nghị còn lại là khoảng trống triển khai và cỡ tài liệu (747 dòng so với mục tiêu 500), không chặn.

## Design Review: Plinko Board
Specialists consulted: none (lean review)
Re-review: Yes, prior verdict NEEDS REVISION on 2026-10-21 (round 1)

### Completeness: 5/5 required sections present
Advisory at standard: thiếu heading Player Fantasy.

### Dependency graph
- ✓ economy-core.md, object-pooling.md, bootstrap-and-scene-flow.md, audio.md, pet-catalog.md, shelf-and-synergy.md, feedback-and-juice.md tồn tại, interface khớp.
- ✓ `dropZoneLineAlpha` một chủ (`FeedbackConfig`).
- Chưa có (đúng): boosts.md, rewarded-ads.md, tutorial-and-onboarding.md.

### Blocking resolution
1. `dropZoneLineAlpha` hai chủ — RESOLVED (Revision 8; plinko-board.md dòng 546, 569; feedback-and-juice.md dòng 327).

### Required before implementation
- none

### Recommended revisions (carried over, chưa xử lý)
1. [main] R11.4.1 `min(dt, maxSimStep)`: thêm quy tắc chia bước cố định `fixed_delta_time`, trần số bước/khung, ngân sách Physics2D WebGL (AC-49, AC-36).
2. [main] R10 / R12.5: định nghĩa bộ hẹn giờ auto-drop khi tổng tốc độ đổi hoặc đăng ký lại cùng giá trị (giữ phần thời gian đã tích lũy).
3. [main] R4 / tap_admitted: thiếu biến `t`; cooldown lượng tử theo bước mô phỏng; nêu `WorldPress` xử lý theo khung hay theo bước.
4. [main] R1 / Tuning Knobs: thiếu knob hình học và vật lý (độ cao vạch thả, khoảng tầng cuối tới miệng hũ, vách, ma sát, nảy tường, damping); khe tầng cuối với đỉnh vách ngoài `min_passage_gap`.
5. [main] `coin_rate_ceiling` / AC-13…13d: `T.maxRawPayoutPerSecondPerBaseCoin*` tuyệt đối, trượt khi `tapCooldown` = 0.10; biểu diễn theo `1 / tapCooldown` hoặc thu hẹp safe range.
6. [main] `coin_payout` / R11.3 / Edge Cases: safe range `instantPayoutMultiplier` 1.0–2.0 vượt `evMeanMin` 1.85, không có validator; câu "nhận đủ phần thưởng" sai với xu Rain.
7. [main] R9.2: không làm sạch `BaseCoinValue` runtime không hữu hạn hoặc < 1 (AC-45); thêm kẹp hoặc nêu Economy Core bảo đảm.
8. [main] R12.1 / States: `coinId` cho xu Overflow trực tiếp (không qua hàng đợi) chưa có chỗ cấp; lối ra Queued "tràn `burst_queue_max`" trái R4.2.
9. [main] AC thiếu: R4.5 (pool rỗng), validator `manualReservedSlots` < `maxActiveCoins`, cảnh báo hệ số hũ 0, lỗi dự phòng `baseCoinValue` < 1.
10. [qa] AC-15, AC-19, AC-29, AC-39: THEN gộp nhiều GIVEN; "tốc độ tối đa" (AC-19) và màu x1/x2/x5 (AC-39) không có số đo; tách AC.
11. [qa] AC-37: soi Frame Debugger thủ công; thay bằng `UnityStats.drawCalls` hoặc ProfilerRecorder.
12. [qa] AC-35, AC-38, AC-39…41: gắn nhãn tích hợp và trỏ AC của feedback-and-juice.md để tránh test trùng.
13. [unity] Nhóm "Sim" EditMode dựa `SceneManager.CreateScene` + `LocalPhysicsMode.Physics2D` chưa xác minh; chưa có PlayMode asmdef cho AC-21, AC-28, AC-34, AC-35; R5.2 nên ghi là kiểm trộn độ nảy Box2D (`bounceThreshold`); đặt hạn chốt ADR trước story đầu.
14. [main] `tapCooldown`: ràng buộc `cue_throughput` của Audio vỡ khi < 0.15 trong safe range; ghi vào "Interacts with" hoặc thu hẹp.
15. [main] drop_x: `coinRadius` 0.12–0.20 khác 0.12–0.19 (min_passage_gap); output range neo ±5 s, viết theo `pegs_per_row`. States / R11.4.1: thống nhất nguồn sự thật "Chờ mở cửa" (sự kiện `Interactive` so với đọc `IsSimulationRunning`).
16. [main] Độ dài 747 dòng (mục tiêu ≤ 500): tám đoạn revision đầu file nên rút gọn, dồn vào decision-log; `PlinkoFeedbackConfig` còn 3 chỗ nhắc lại; hàng Assumed đã đóng chuyển decision-log; thiếu Player Fantasy (advisory).

### Specialist disagreements
- none (lean review)

### Nice-to-have
- Cross-References: thêm báo cáo review này, bỏ dòng "Review Round 3" cũ.

### Senior verdict [creative-director]
Không chạy (lean review). Giữ đúng Pillar 1 và Pillar 3; không cần quyết định mới từ user.

### Scope signal
Rough scope signal: L (producer should verify before sprint planning)

### Sync cần làm (không sửa ở bước review này)
- `systems-index.md` và dòng `> **Status**:`: đặt `Approved` theo verdict này (GDD hiện ghi `In Review`).
- `entities.yaml`: không mâu thuẫn với giá trị Plinko; không cần thay đổi.

### Verdict: APPROVED

Findings:
- [RECOMMENDED] R11.4.1: quy tắc chia bước cố định và trần số bước mỗi khung
- [RECOMMENDED] R10/R12.5: bộ hẹn giờ auto-drop khi đăng ký lại
- [RECOMMENDED] R4/tap_admitted: biến `t`, lượng tử cooldown
- [RECOMMENDED] R1/Tuning Knobs: knob hình học và vật lý còn thiếu
- [RECOMMENDED] coin_rate_ceiling/AC-13: trần tuyệt đối trượt theo `tapCooldown`
- [RECOMMENDED] coin_payout/R11.3: safe range `instantPayoutMultiplier` vượt `evMeanMin`
- [RECOMMENDED] R9.2: làm sạch `BaseCoinValue` runtime
- [RECOMMENDED] R12.1/States: `coinId` Overflow trực tiếp; lối ra Queued trái R4.2
- [RECOMMENDED] Acceptance Criteria: thiếu AC cho R4.5, validator, hệ số hũ 0, dự phòng < 1
- [RECOMMENDED] AC-15/19/29/39: tách GIVEN, thêm giá trị đo
- [RECOMMENDED] AC-37: thay soi Frame Debugger bằng số đo tự động
- [RECOMMENDED] AC-35/38/39-41: gắn nhãn tích hợp, trỏ AC của feedback-and-juice.md
- [RECOMMENDED] ADR: xác minh Physics2D scene trong EditMode, PlayMode asmdef, combine độ nảy Box2D
- [RECOMMENDED] tapCooldown: ràng buộc `cue_throughput` của Audio
- [RECOMMENDED] drop_x / R11.4.1: sai lệch `coinRadius` và nguồn sự thật "Chờ mở cửa"
- [RECOMMENDED] Độ dài: 747 dòng, revision dài, tên cũ `PlinkoFeedbackConfig`, thiếu Player Fantasy
