# Review — economy-core — 2026-10-21 — Verdict: APPROVED
Round 2 (verification sau fix round 1b)
Document: Docs/design/gdd/economy-core.md (git hash-object b122ebf9e3b4cb51b522ff8cda00f3a50b2a18ee)
Registry: Docs/design/registry/entities.yaml (git hash-object 86c897c269e1a6a58b77974d2f52fb7e6151614c)
Scope signal: XL
Specialists: none (lean review)
Blocking items: 0 | Recommended: 13
Prior verdict resolved: Yes — mục chặn duy nhất của vòng 1 đã xử lý; các mục Recommended còn mở được giữ lại bên dưới.
Summary: EC-1 (reset tiệm phải phát `SpinPriceChanged` và `WalletChanged`) đã giải quyết đầy đủ ở R12.2, R14, R15 và AC-36, khớp `gacha-machine.md` AC-24 và G3. Các phần được sửa không sinh mâu thuẫn mới: thứ tự sự kiện, payload `{ 0, spinBasePrice, 1 }` và hàng sink R14 nhất quán, `ShopReset` thuộc bảng R14 nên AC-39 vẫn đúng. Còn lại chỉ là các mục Recommended của vòng 1 (chưa sửa), tài liệu 803 dòng (mục tiêu 500).

## Mục chặn của vòng 1
| Mục | Kết quả |
|---|---|
| [BLOCKING] R12.2 / R15 / AC-36: reset không phát `SpinPriceChanged` / `WalletChanged` | **Đã giải quyết** — R12.2 phát theo thứ tự `WalletChanged(ShopReset)` (chỉ khi số dư đổi), `SpinPriceChanged { 0, spinBasePrice, 1 }` (luôn phát), `ShopEconomyReset { resetCurrencyIds[], spinsThisShop = 0 }`; R14 có hàng sink `ShopReset`; R15 định nghĩa payload; AC-36 kiểm số lượng và thứ tự. |

Phạm vi quét mới (R12.2, R14, R15, AC-36, header): không có mâu thuẫn, số trần hay tiêu chí không đo được mới.

Dependency graph: shelf-and-synergy.md, gacha-machine.md, inventory.md, hud-and-tab-navigation.md, feedback-and-juice.md, plinko-board.md, bootstrap-and-scene-flow.md, pet-catalog.md đều tồn tại; save-load, boosts, shop, offline-earnings, rewarded-ads, prestige, neighbor-pranks, star-progression, analytics chưa có GDD (hệ VS/Alpha, không chặn MVP).

Findings:
- [RECOMMENDED] Formulas `plinko_credit` / `plinko_rate_avg`: output range ghi "× 60" nhưng thiếu `synergyTierCap`, đúng là × 240 (4 × 3 × 10 × 2, như `channel_income`); `rawValue` "0–5 × bcv" chỉ đúng với hệ số hũ mặc định; `idle_accrual` output range ghi literal 0.25 thay vì `maxFrameDelta`.
- [RECOMMENDED] AC-18: ca cộng phẳng 10 (bị kẹp `plinkoFlatPerCoinCap`) thiếu kết quả số; kỳ vọng (93.395 + 2.0 × 18.679) × 3 = 392.26.
- [RECOMMENDED] R10.2 / R16.3 / Edge Cases / AC-8: `spinsThisShop` nạp từ save không bị kẹp; n < 0 cho `base_coin_value` < 1, n ≳ 14 500 làm `spin_growth^n` tràn +∞, trái "luôn hữu hạn ≥ 1". Thêm kẹp n ∈ [0, n_max] khi nạp và AC.
- [RECOMMENDED] Độ dài 803 dòng (mục tiêu 500): header có 6 ghi chú revision dài (cắt mỗi cái một dòng), bảng "Lựa chọn nhỏ đã giả định" 33 hàng lặp Detailed Design (gộp), các hàng Open Questions đã đóng (xóa).
- [RECOMMENDED] Cross-References (dòng 647–650) còn ghi `shelf-and-synergy.md`, `gacha-machine.md`, `hud-and-tab-navigation.md`, `feedback-and-juice.md` là "(chưa có)"; "Hai chiều" còn nói Pet Catalog "còn chuyển nốt" `bounceUpliftAtCapAssumed`; dòng Shelf Open Question "cần PreviewIdleCps" đã được R7.5 đáp ứng, đóng ở bước Sync.
- [RECOMMENDED] R6.4 / `ad_idle_compensation`: `ad_simulated_seconds` cộng khi `IsAdOpen` đúng nhưng truy vấn `IsAdOpen` (Bootstrap B6.10) không có trong R16.4, Interactions, Dependencies; chưa ghi dt là scaled hay unscaled và `running_time` / bộ đệm vòng có tiến ở Ready/Paused hay không.
- [RECOMMENDED] R4.3 / R6.1 / R5.4: API `Unregister` (Economy) với `UnregisterIncomeSource` (nêu ở R6.1) chưa thống nhất tên; chưa có luật khi `RegisterIncomeSource` trùng `sourceKey` (Shelf dựng lại kệ) hoặc `baseRate` đổi do lên Sao; khóa thay thế modifier chưa gồm tầng/kênh; `origin`/`reason` của `IncomeCredited` từ `SubmitIncome` chưa định nghĩa (Feedback R7 gộp theo `sourceKey`); `TrySpend(amount)` cho giá dẫn xuất nên có `TryBuyDerived(coef)` để không tin số tiền do hệ gọi đưa.
- [RECOMMENDED] AC-43 / AC-44: "0 B do Economy Core" không tách được khỏi counter GC toàn cục; "thiết bị tham chiếu của ADR hiệu năng" nên trỏ `BootTestTargets.perfReferenceDevice`; tải 40 modifier / 24 xu / 600 khung / 60 s / 5 nguồn chưa là knob; thiếu tải `SubmitIncome` của Thỏ (14 lần/xu).
- [RECOMMENDED] AC-23 / AC-34 / AC-42 / AC-45 [Play mode]/[Screenshot]: dự án mới có `CozyGachapon.Tests.EditMode`, chưa có assembly PlayMode, `InputTestFixture` phụ thuộc nút vặn/header; AC-23 "50 ± 10 × maxFrameDelta" nên viết dung sai tuyệt đối; AC-8 là VS nhưng chưa gắn tier.
- [RECOMMENDED] R13 / `display_format` / AC-37: cắt trên double có thể sai (1,150 → "1.14K" do sai số nhị phân), k lệch bậc sát lũy thừa 1000; "chuỗi ≤ 6 ký tự" sai khi `displaySigDigits` = 4.
- [RECOMMENDED] R1 / AC-1 / Tuning Knobs: "`releaseTier` ≤ bản build" chưa trỏ knob; schema `CurrencyDefinition` chưa nằm trong Tuning Knobs; `rate_bin` = 1 s ghi "constant" dù là lựa chọn thiết kế; "5–15 phút" ở R14 chưa là knob.
- [RECOMMENDED] R11.2 rủi ro `channel_parity` (ra khỏi dải từ n ≈ 59): cần chốt mốc prestige và điều kiện dừng (knob nào đổi nếu mô phỏng ngoài dải) trước khi Shelf & Synergy được duyệt.
- [RECOMMENDED] Completeness: thiếu `## Player Fantasy` (advisory ở standard; Summary đã có ý, chỉ cần tách mục ngắn). Unity 6000.3: round-trip `double` (1e300) qua `JsonUtility` chưa xác minh, ADR phải chốt mảng cặp id/giá trị và định dạng chuỗi số; `runInBackground: 0` ảnh hưởng giả định "mất focus vẫn cộng Idle".
- [RECOMMENDED] Sync (không sửa ở bước review): (1) `entities.yaml`: `floating_text_merge_window` và `busy_feedback_min_interval` đổi `source` sang `feedback-and-juice.md` (user đã duyệt), referenced_by thêm `plinko-board.md`; chưa có mục `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed` (owner economy-core.md, referenced_by pet-catalog.md, plinko-board.md); `referenced_by` của `shelf_slot_count_*`, `gacha_pity_threshold` còn rỗng. (2) `bootstrap-and-scene-flow.md` dòng 194 và 482 ghi trạng thái Economy "Uninitialized/Running/Paused", thiếu Ready. (3) `plinko-board.md` còn tên cũ `PetBalanceTargets.bounceUpliftAtCapAssumed` nếu chưa xóa. (4) Status trong `systems-index.md` phải khớp header GDD (hiện `In Review`; Sync đặt `Approved` nếu user chấp nhận verdict này).
