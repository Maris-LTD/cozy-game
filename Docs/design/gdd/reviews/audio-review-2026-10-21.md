# Review — audio — 2026-10-21 — Verdict: APPROVED
Round 1 (lượt pipeline này; tài liệu đã qua Revision 5)
Document: Docs/design/gdd/audio.md (git hash-object b65efb272993a1710ee1147dad61246c7b2d96e5)
Registry: Docs/design/registry/entities.yaml (git hash-object 86c897c269e1a6a58b77974d2f52fb7e6151614c)
Scope signal: L
Specialists: none (lean review)
Blocking items: 0 | Recommended: 14
Prior verdict resolved: Yes (báo cáo trước APPROVED, không có mục chặn; Revision 5 đã xử lý khuyến nghị 2 mốc nhạc `Interactive`, 20 ánh xạ hũ theo `bucketIndex`, 22 AU-R20)
Summary: Tài liệu đủ chính xác để hiện thực MVP không phải đoán; công thức đúng ở biên, tên sự kiện và payload khớp `plinko-board.md` R12 và `bootstrap-and-scene-flow.md` (Paused {reason}, Resumed, FirstInteraction, Interactive, TabChanged, B6.10), không mâu thuẫn registry, không còn Open Question NEEDS-USER. Còn các khuyến nghị mang từ trước (tok spawn bị cướp, demand_rate cũ, jackpot DropNew, literal chưa là knob) và tài liệu dài 718 dòng, vượt mục tiêu 500.

## Design Review: Audio (Round 1)
Specialists consulted: none (lean review)
Re-review: Yes, prior verdict APPROVED (cùng ngày, lượt trước); tài liệu và registry đã đổi → review đầy đủ.

### Completeness: 6/6 required sections present (workflow standard)
Overview, Detailed Design (A1-A16, States, Interactions), Formulas (11 công thức; hệ có luật số nên bắt buộc), Edge Cases, Dependencies, Acceptance Criteria (43 AC, đủ tag và GIVEN/WHEN/THEN) đều có nội dung; Summary có. Advisory: không có Player Fantasy (có chủ đích, hệ Foundation); Tuning Knobs, Game Feel, UI Requirements, Visual/Audio, Cross-References, Open Questions đều có.

### Theo dõi báo cáo trước
Không có mục chặn trước đó. Đã xử lý: mốc nhạc VS = `Interactive` (A11.2, A16.1, Interactions, Dependencies, AC-26), ánh xạ hũ theo `bucketIndex`, AU-R20 (CC0 MVP, mua gói VS), D2 6+2 là quyết định user. Chưa xử lý: các khuyến nghị 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 21 của báo cáo trước (xem dưới, gộp lại).

### Dependency graph
- ✓ bootstrap-and-scene-flow.md exists: payload khớp. Lệch phía Bootstrap: hàng Audio của Interactions (dòng 191) vẫn ghi `BootCompleted` làm mốc nhạc và "mixer"; B6.5 "qua mixer của nó" (Sync).
- ✓ plinko-board.md exists: R12 (`CoinSpawned {origin, burstIndex}`, `PegHit {rowIndex, scored}`, `JackpotHit`, `CoinResolved`, `CoinStuck`, `BoardBusy`), R11.3 (xả hàng đợi bằng `Overflow`) khớp.
- ✓ object-pooling.md, economy-core.md, game-concept.md exist; ✓ pet-catalog.md exists (dùng `Gacha.RarityReveal.<phẩm cấp>`) nhưng audio.md chưa tham chiếu.
- ✗ feedback-and-juice.md, hud-and-tab-navigation.md, settings-and-accessibility.md, rewarded-ads.md, platform-services.md, gacha-machine.md NOT FOUND (Not Started; không chặn).

### Kiểm tra công thức (thay biên)
db_to_linear (0, -6, -12, -80, -100) đúng; voice_gain (0.398, 0.100, 0.251, 0.025, 0.631) đúng; slider_to_db (1, 0.75, 0.5, 0, NaN, 1.5) đúng; note_index (0, 1, 0.727, -0.5, 1.7, NaN) đúng; sample_pitch n = 0..9 và burst 0..4 đúng, biên [0.770, 1.298]; cue_admitted thứ tự khớp A3; steal_victim trạng thái A và B đúng; voice_budget 12 ≤ 12 và storm_peak_gain 1.756 ≤ 2.0 đúng số học (đúng biên); cue_throughput 2a/2b đúng biên (0.12 = 0.12); fade_factor T = 0 không chia 0.

### Unity 6000.3
Không dùng AudioMixer (một phần trên Web), pitch luôn > 0, các cơ chế phụ thuộc nền tảng đã đẩy sang ADR âm thanh. Ghi cho ADR: `AudioSource.priority` của Unity (0 = cao nhất) ngược `SoundCue.priority`; unlock Web theo pointerup/touchend chứ không phải pointerdown của `FirstInteraction`; thời gian phát = `clip.length / pitch` bị lượng tử theo bộ đệm Web Audio.

### Required before implementation
Không có.

### Recommended revisions
1. [main review] Dài 718 dòng, vượt mục tiêu 500: gộp Revision 1-5 ở header thành 1-2 dòng, rút các hàng Open Questions đã chốt và Edge Cases trùng A9; header Revision đang sắp xếp 5 trước 4.
2. [main review] Dependencies "Hai chiều" và hàng Bootstrap ghi Bootstrap liệt kê `BootCompleted` "khớp", trong khi A11.2 dùng `Interactive`: sửa câu thành `Interactive`, báo Sync sửa hàng Audio của Bootstrap (dòng 191) và Status/Depends On của systems-index hàng 5 (còn `BootCompleted`).
3. [main review] "Tok" spawn vẫn bị cướp (`PegHitScored` 60 cướp `CoinSpawn` 50) hoặc `NoVoice` lúc jackpot; câu "không bao giờ cắt tok đang kêu" sai. Nâng `CoinSpawn` lên 65 hoặc đổi câu thành "không bị `InstanceCap`", thêm ràng buộc ngân sách cue thưởng và vế AC-13.
4. [main review] `demand_rate` còn dẫn "spam tay 6.67 x 14" và "registry Plinko" cho số 14 (thuộc `plinko_scored_peg_hits_per_coin_assumed` của Economy Core, chỉ đếm chạm được tính); chưa theo `coin_rate_ceiling` mềm có auto-drop; AC-13 thiếu `CoinSpawnAuto`.
5. [main review] `JackpotHit` `DropNew` 2 bản thấp hơn nhịp jackpot thực (xu Bùng Xu cũng phát, 1.5-3.7/s) nên `InstanceCap` xảy ra thật; thêm knob độ dài clip jackpot và ràng buộc validator hoặc đổi `StealOldest`; AC-13 kiểm theo nhịp thực.
6. [main review] Cách xác định bản hết phát chưa có: A4/A13 ghi bản hết khi `t >= startTime + clip.length / pitch` theo đồng hồ A13; kiểm lại biên `spawnClipMarginSeconds` 0.03 ở 30 FPS.
7. [main review] `minInterval` lượng tử theo khung: `admitted_rate = min(demand_rate, fps / ceil(minInterval x fps))` (20 nốt/s ở 60 FPS, không phải 25); sửa ví dụ, Edge Case, Game Feel; chốt nguồn đồng hồ.
8. [main review] `stormPriorityCeiling` safe range 40-70 phá chính kiểm tra (70 kéo cue `Ui.*` vào, 50 bỏ `PegHitScored`): dùng cờ `SoundCue.isStormCue` hoặc thu safe range về 60-69.
9. [main review] `fade_factor` dùng chung cho 4 loại fade và đảo chiều giữa chừng (AC-4) không có trong công thức `fade_in`; định nghĩa `f(t) = clamp(f_start + dir x (t - t0)/T, 0, 1)` theo từng fade, nêu fade toàn cục hay theo voice.
10. [main review] Nhạc VS chỉ thử bắt đầu một lần: nếu lúc đó `Suspended` (Ad) hoặc thanh trượt Nhạc = 0 thì có thể không bao giờ phát; nhạc nên là trạng thái "nên phát" tự giữ; thêm ca vào AC-26. Cue loop (`RainLoop`, `ShopLoop`) chưa có `StopCue`.
11. [main review] `pitchJitterRatio` 0.03 trên cue `Scale` = khoảng 51 cent (nửa nửa cung), trái A6.5 "ngũ cung hòa âm": dùng knob cent (khoảng 10) hoặc ratio <= 0.006 cho cue `Scale`; báo Plinko nếu đổi tên field.
12. [main review] Số literal chưa là knob/fixture/constant: "attack < 10 ms", AC-13 "10%", "6.67/s", "1 jackpot mỗi 2 s"; AC-15 "10 000", AC-16 "1000", AC-20 "5 s", AC-21 "60 s", AC-25 "10 lần / >= 9", AC-33 "600 khung", AC-34 "60 s", AC-37 "1080x1920", AC-39 "30 s / 2 jackpot", sai số 1e-4. Thêm knob `AudioBalanceTargets` hoặc ghi "dữ liệu fixture"/"constant (lý do)".
13. [main review] Safe range lệch: `busDefaultDb` Formulas -30..0 vs Tuning Knobs -12..0; `scaleNoteCount` 5-15 nhưng mẫu 4/12/19 chỉ phủ 10-11 nốt; `maxSampleShiftSemitones` 2-6 trượt với bảng hiện tại (shift +-4) nên safe range thực 4-6.
14. [main review] Câu cũ, tham chiếu thiếu: Dependencies còn "Chưa có trong systems index" cho HUD (hàng 10), Rewarded Ads/Platform Services (hàng 19, 20); Cross-References thiếu `pet-catalog.md` (hậu tố phẩm cấp, lớp ngân Huyền Thoại); `MuteChanged` chưa có payload; `tap_cooldown` của validator nên đọc `PlinkoConfig.tapCooldown` qua assembly Editor; AC-20/25/34 phụ thuộc Feedback/jslib/marker Profiler chưa nêu; AC-9 vế 2 cần điều kiện `minInterval`; mở khóa Web theo pointerup/touchend cần ghi cho ADR và AC-25.

### Specialist disagreements
Không có (lean review).

### Nice-to-have
- Validator so `steal_age` (0.20) và 2b (0.12) đang đúng biên: dùng dung sai float.
- `SetMuteReason`, `SetUserMuted` khi Uninitialized: ghi lý do được lưu và áp sau `Run()`.
- `AudioConfig.stealFadeSeconds` (mặc định 0) thay cho "thêm knob nếu nghe click".
- AC-22 thêm ca thực tế "Ad tắt + `Paused` cùng khung"; AC-39/40 ghi đóng theo biên bản playtest.

### Cần Sync (không sửa ở đây)
- `bootstrap-and-scene-flow.md`: hàng Audio của Interactions (dòng 191) `BootCompleted` thành `Interactive`, bỏ "mixer"; B6.5 bỏ "qua mixer của nó".
- `systems-index.md`: hàng 5 Depends On `BootCompleted` thành `Interactive`; Status Approved sau lượt này, header GDD đặt cùng từ.
- `entities.yaml`: không có mục Audio sở hữu cần thêm; thay đổi user đã duyệt (nhãn provisional của pet scope, `plinkoBounceUpliftAtCapAssumed` thuộc economy-core.md) không liên quan audio.md và không mâu thuẫn.

### Scope signal
Rough scope signal: L (producer should verify before sprint planning)
- 11 công thức, Foundation dùng bởi 5+ hệ, cần ADR âm thanh mới.

### Verdict: APPROVED
