# Systems Index: Cozy Gachapon Room (Tiệm Trứng Thư Giãn)

> **Status**: Draft
> **Created**: 2026-10-21
> **Last Updated**: 2026-10-21 (Sync batch 2026-10-21, lần 5: Inventory, Gacha Machine, Shelf & Synergy, HUD & Tab Navigation, Feedback & Juice → Approved; Depends On khớp mục Dependencies của GDD; registry thêm giá trị Gacha/Shelf/Feedback)
> **Source Concept**: Docs/design/gdd/game-concept.md

> [TD-SYSTEM-BOUNDARY] skipped (lean)
> [PR-SCOPE] skipped (lean)
> [CD-SYSTEMS] skipped (lean)

## Overview
Game cần 24 hệ thống quanh một vòng lặp duy nhất: Plinko sinh xu → Gacha tiêu xu ra thú → Kệ biến thú thành thu nhập → Tiệm mở rộng kệ. Mọi nguồn thu và mọi bùa lợi đi qua một Income Pipeline chung (Economy Core), nên các hệ gameplay chỉ đăng ký modifier vào pipeline thay vì gọi lẫn nhau. Raid/Kidnap của FirstGDD đã đổi thành **Neighbor Pranks** và dời sang v1.1 (Quyết định 2, 3 của concept).

**MVP** (11 hệ) — trả lời giả thuyết "Plinko + vặn trứng + kệ 3 ô đủ vui để chơi tiếp 15 phút":
- **Economy Core**: ví Xu, số lớn dạng double + định dạng K/M/B, Income Pipeline với hai kênh (CPS idle / Plinko chủ động), thứ tự cộng/nhân modifier, trần, xu khởi đầu, họ công thức giá theo giá quay (đường neo; giá không phụ thuộc CPS hay đội hình kệ). Gỡ các BLOCKING về công thức thu nhập và giá cố định (Pillar 3).
- **Pet Catalog**: danh mục thú dạng ScriptableObject — nhóm, kênh thu nhập, phẩm cấp, chỉ số gốc, hiệu ứng. Nơi cân lại Mèo Hoàng Gia và Kẹo Bảy Màu (Quyết định 5, Pillar 3).
- **Bootstrap & Scene Flow** *(suy ra)*: một scene, trình tự khởi động theo pha (`IBootStep`), vòng đời tạm dừng/tiếp tục, trạng thái tab + khóa tab, input router + cổng chặn, khung 9:16–9:21, hàng đợi màn chào. HUD chỉ vẽ nav. GDD ngắn, chi tiết chốt bằng ADR.
- **Object Pooling** *(suy ra)*: pool xu Plinko và số bay để giữ 60 FPS trên WebGL. GDD ngắn / ADR.
- **Audio** *(suy ra)*: voice tự quản (16), kho SoundCue, bus, nhạc nền (VS), tắt tiếng theo lý do — khung cho âm thanh ASMR (Pillar 1). GDD ngắn.
- **Plinko Board**: chạm thả + tự thả, 5 hũ nhân, điểm thả được phép, trần nảy, trần tốc độ thả và số xu đồng thời; công bố sự kiện `CoinSpawned` / `PegHit` / `JackpotHit` / `CoinResolved` / `CoinStuck` / `BoardBusy`, API `RequestBurst` / `RegisterAutoDropRate` / `RegisterBouncinessFactor` / `Unregister`, cờ `acceptManualTaps` và interface `IBaseCoinValueSource` (Pillar 1).
- **Inventory / Kho**: sở hữu thú, số bản trùng; là kho mà Gacha ghi vào và Kệ đọc ra.
- **Gacha Machine**: giá quay, drop pool v1.0 (chỉ thú), tỷ lệ trong từng phẩm cấp và công khai, luật trùng, nhịp mở trứng có nút tua (Pillar 2).
- **Shelf & Synergy**: 3–5 ô, buff theo nhóm, ô kề nhau và hai kênh thu nhập; không có đội hình tối ưu cho cả hai kênh (Pillar 3).
- **HUD & Tab Navigation**: header Xu / CPS / Boost, bottom nav 3 tab. Bố cục từng màn hình đã có trên Penpot nên không tách GDD UI riêng cho từng tab.
- **Feedback & Juice**: số bay, tween, ánh sáng phẩm cấp, rung, bảng map sự kiện → SFX (Pillar 1, 2).

**Vertical Slice** (7 hệ) — một vòng chơi hoàn chỉnh, có lý do quay lại:
- **Save/Load**: lưu JSON cục bộ; mọi hệ có trạng thái đăng ký dữ liệu vào đây. Xuất/nhập Base64 cho WebGL thuộc Alpha.
- **Time & Timers** *(suy ra)*: đồng hồ chống delta âm, timer dùng chung cho boost, Flash Deal, offline, cooldown ads, (v1.1) hàng xóm.
- **Star Progression (Sao)**: thú trùng lên Sao, số bản/cấp, trần, hệ số sản lượng — nguồn tăng trưởng chính trong một tiệm (Quyết định 4).
- **Boosts** *(tách từ Shop)*: Trà Sốt Dẻo, Mưa Đồng Xu — modifier tạm thời và lệnh thả xu hàng loạt; dùng chung cho Shop và Ads.
- **Shop (Tiệm Tạp Hóa)**: mở ô kệ, bán boost, Flash Deal mỗi 30 phút (Alpha), quà Bác Bảo Vệ (Alpha, qua Ads); giá theo họ công thức của Economy Core.
- **Offline Earnings**: thu nhập khi vắng mặt, trần giờ, hệ số; nút x2 qua Ads có từ Alpha.
- **Tutorial / Onboarding** *(suy ra)*: kịch bản 10 phút đầu của concept, mỗi bước dạy một nút.

**Alpha (v1.0)** (4 hệ):
- **Platform Services** *(suy ra)*: adapter SDK theo nền tảng (Poki / CrazyGames trên web, LevelPlay hoặc AdMob trên mobile), vòng đời gameplay start/stop, lưu trữ.
- **Rewarded Ads**: danh sách placement, cap và cooldown theo nền tảng, 100% tự nguyện (Anti-Pillar: không interstitial ép buộc).
- **Prestige "Mở Tiệm Mới"**: điều kiện, phần reset/giữ lại, tiền tệ Tem Tiệm và những gì nó mua (Quyết định 4).
- **Settings & Accessibility** *(suy ra)*: âm lượng, giảm chuyển động, ngôn ngữ, xuất/nhập dữ liệu.

**Full Vision (v1.1)** (2 hệ):
- **Analytics Hooks** *(suy ra)*: sự kiện funnel và kinh tế để cân bằng sau ra mắt.
- **Neighbor Pranks**: NPC hàng xóm, đồ nghịch/phòng thủ, tạm giữ thú/xu, ba đường gỡ (Pillar 4). Ranh giới v1.0 phải cứng.

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | Economy Core | Economy | MVP | Approved | Docs/design/gdd/economy-core.md | Plinko Board (hợp đồng: `CoinResolved`, `IBaseCoinValueSource`), Bootstrap & Scene Flow (`IBootStep` pha `GameplayInit`, `Paused`/`Resumed`, `Interactive` + `IsSimulationRunning`), Save/Load (soft, từ VS) |
| 2 | Pet Catalog | Economy | MVP | Approved | Docs/design/gdd/pet-catalog.md | Bootstrap & Scene Flow (bước `pet.catalog`, pha `DataLoad`); Economy Core, Plinko Board (soft, chỉ lúc làm nội dung: validator đọc knob); Settings & Accessibility (soft, Alpha: ngôn ngữ) |
| 3 | Bootstrap & Scene Flow (inferred) | Core | MVP | Approved | Docs/design/gdd/bootstrap-and-scene-flow.md | — |
| 4 | Object Pooling (inferred) | Core | MVP | Approved | Docs/design/gdd/object-pooling.md | Bootstrap & Scene Flow (hợp đồng pha: `ServiceInit`, `Prewarm`) |
| 5 | Audio (inferred) | Audio | MVP | Approved | Docs/design/gdd/audio.md | Bootstrap & Scene Flow (soft: bước `audio.service` pha `ServiceInit`, `Paused`/`Resumed`, `FirstInteraction`, `Interactive`: mốc nhạc VS) |
| 6 | Plinko Board | Gameplay | MVP | Approved | Docs/design/gdd/plinko-board.md | Object Pooling, Bootstrap & Scene Flow |
| 7 | Inventory | Economy | MVP | Approved | Docs/design/gdd/inventory.md | Pet Catalog, Bootstrap & Scene Flow (bước `inventory.core`), Economy Core (soft, chỉ thứ tự khởi tạo), Save/Load (soft, VS) |
| 8 | Gacha Machine | Gameplay | MVP | Approved | Docs/design/gdd/gacha-machine.md | Economy Core, Pet Catalog, Inventory, Bootstrap & Scene Flow, HUD & Tab Navigation (Hard, popup `ModalHost`) |
| 9 | Shelf & Synergy | Gameplay | MVP | Approved | Docs/design/gdd/shelf-and-synergy.md | Economy Core, Pet Catalog, Inventory, Plinko Board, Bootstrap & Scene Flow, HUD & Tab Navigation (soft), Star Progression (soft, VS) |
| 10 | HUD & Tab Navigation | UI | MVP | Approved | Docs/design/gdd/hud-and-tab-navigation.md | Economy Core, Bootstrap & Scene Flow, Audio (soft: nút loa), Pet Catalog (soft: dữ liệu thẻ thú) |
| 11 | Feedback & Juice | UI | MVP | Approved | Docs/design/gdd/feedback-and-juice.md | Plinko Board, Gacha Machine (hợp đồng bóc trứng; chiều phụ thuộc đang chốt), Shelf & Synergy (soft), HUD & Tab Navigation (soft), Audio, Object Pooling, Economy Core (`IncomeCredited`, `SpendRejected`), Bootstrap & Scene Flow (`TabChanged`, `Paused`/`Resumed`), Pet Catalog (soft: dữ liệu thẻ thú) |
| 12 | Save/Load | Persistence | Vertical Slice | Not Started | — | Bootstrap & Scene Flow (*provisional*) |
| 13 | Time & Timers (inferred) | Core | Vertical Slice | Not Started | — | Bootstrap & Scene Flow (soft, *provisional*) |
| 14 | Star Progression | Progression | Vertical Slice | Not Started | — | Economy Core, Pet Catalog, Inventory, Gacha Machine |
| 15 | Boosts (inferred) | Economy | Vertical Slice | Not Started | — | Economy Core, Plinko Board, Time & Timers |
| 16 | Shop | Economy | Vertical Slice | Not Started | — | Economy Core, Inventory, Shelf & Synergy, Boosts, Time & Timers |
| 17 | Offline Earnings | Economy | Vertical Slice | Not Started | — | Economy Core, Shelf & Synergy, Save/Load, Time & Timers, Bootstrap & Scene Flow (*provisional*) |
| 18 | Tutorial & Onboarding (inferred) | Meta | Vertical Slice | Not Started | — | Plinko Board, Gacha Machine, Shelf & Synergy, Shop, Bootstrap & Scene Flow (*provisional*) |
| 19 | Platform Services (inferred) | Meta | Alpha | Not Started | — | Bootstrap & Scene Flow (*provisional*), Audio (soft: `SetMuteReason(Ad)`) |
| 20 | Rewarded Ads | Meta | Alpha | Not Started | — | Platform Services, Economy Core, Time & Timers, Audio (soft: `SetMuteReason(Ad)`), Plinko Board (soft: `RequestBurst`) |
| 21 | Prestige | Progression | Alpha | Not Started | — | Economy Core, Inventory, Star Progression, Shelf & Synergy, Shop, Save/Load |
| 22 | Settings & Accessibility (inferred) | UI | Alpha | Not Started | — | Audio, Save/Load, Feedback & Juice |
| 23 | Analytics Hooks (inferred) | Meta | Full Vision | Not Started | — | Platform Services |
| 24 | Neighbor Pranks | Gameplay | Full Vision | Not Started | — | Economy Core, Pet Catalog, Inventory, Shelf & Synergy, Time & Timers, Save/Load, Rewarded Ads |

## Categories

| Category | Typical systems |
|---|---|
| Core | player controller, input, physics, camera, scene flow, state machine |
| Gameplay | combat, AI, movement abilities, interaction, puzzles |
| Progression | XP/levels, unlocks, achievements |
| Economy | currency, loot, crafting, shops, item database |
| Persistence | save/load, settings, cloud sync |
| UI | HUD, menus, inventory screen, popups, notifications |
| Audio | music, SFX, ambience |
| Narrative | dialogue, quests, cutscenes |
| Meta | analytics, tutorial, accessibility, ads/IAP |

## Priority Tiers

| Tier | Definition | Target milestone | Design urgency |
|---|---|---|---|
| MVP | Needed for the core loop; without it "is this fun?" cannot be tested | First playable | First |
| Vertical Slice | One complete, polished area | Demo | Second |
| Alpha | All features present in rough form | Alpha | Third |
| Full Vision | Polish, edge cases, nice-to-haves | Beta / release | As needed |

| Tier | Systems | Count |
|---|---|---|
| MVP | Economy Core, Pet Catalog, Bootstrap & Scene Flow, Object Pooling, Audio, Plinko Board, Inventory, Gacha Machine, Shelf & Synergy, HUD & Tab Navigation, Feedback & Juice | 11 |
| Vertical Slice | Save/Load, Time & Timers, Star Progression, Boosts, Shop, Offline Earnings, Tutorial & Onboarding | 7 |
| Alpha | Platform Services, Rewarded Ads, Prestige, Settings & Accessibility | 4 |
| Full Vision | Analytics Hooks, Neighbor Pranks | 2 |

## Dependency Map

### Foundation Layer
Tầng Foundation không còn "không phụ thuộc": hầu hết hệ ở đây phụ thuộc Bootstrap & Scene Flow qua hợp đồng pha (cùng tầng). Ba mũi tên đi lên chưa gỡ: Economy Core → Plinko Board (Core, Hard, chỉ hợp đồng), Pet Catalog → Plinko Board (Core, soft, chỉ lúc làm nội dung: validator đọc `PlinkoConfig`) và Pet Catalog → Settings & Accessibility (Presentation, soft, Alpha) — xem Circular Dependencies.
1. Economy Core — framework mà mọi nguồn thu, mọi bùa lợi và mọi giá đều cắm vào (Income Pipeline + họ công thức giá). Depends on: Plinko Board (Hard, chỉ hợp đồng `CoinResolved` và `IBaseCoinValueSource` do Plinko sở hữu; Economy Core cài đặt `IBaseCoinValueSource`, Plinko kéo `baseCoinValue` và không tham chiếu Economy Core), Bootstrap & Scene Flow (Hard: bước `economy.core` pha `GameplayInit`, `Paused`/`Resumed`, `Interactive` + `IsSimulationRunning` — chỉ cộng Idle khi vòng đời `Running`), Save/Load (soft, từ VS). Mũi tên Foundation → Core: `game-systems` chọn chuyển Economy Core sang tầng Core (sau Plinko) hoặc tách hợp đồng Plinko thành module Foundation.
2. Pet Catalog — dữ liệu tĩnh của thú; Inventory, Gacha, Shelf, Sao, HUD, Feedback đều đọc. Depends on: Bootstrap & Scene Flow (Hard: bước `pet.catalog` pha `DataLoad`); Economy Core, Plinko Board (soft, chỉ lúc làm nội dung: validator đọc knob và hợp đồng sự kiện, không phụ thuộc lúc chạy); Settings & Accessibility (soft, Alpha: ngôn ngữ).
3. Bootstrap & Scene Flow — trình tự khởi động theo pha, vòng đời, tab + khóa tab, input router + cổng chặn. Không phụ thuộc hệ nào.
4. Object Pooling — dùng bởi Plinko và Feedback & Juice. Depends on: Bootstrap & Scene Flow (Hard: bước `pooling.service` pha `ServiceInit`, `pooling.prewarm` pha `Prewarm`).
5. Audio — khung phát âm thanh; nội dung SFX map ở Feedback & Juice. Depends on: Bootstrap & Scene Flow (soft: bước `audio.service` pha `ServiceInit` không cốt lõi, `Paused`/`Resumed`, `FirstInteraction`, `Interactive`: mốc nhạc VS, B5.3).
6. Save/Load — framework lưu trữ; mọi hệ có trạng thái đăng ký dữ liệu vào đây (từ Vertical Slice). Depends on: Bootstrap & Scene Flow (pha `SaveLoad`, `Paused`, `Quitting`; *provisional*).
7. Time & Timers — nguồn thời gian và timer dùng chung. Depends on: Bootstrap & Scene Flow (soft, `Paused`/`Resumed {awaySeconds}`; *provisional*).
8. Platform Services — lớp trừu tượng SDK theo nền tảng. Depends on: Bootstrap & Scene Flow (`BootCompleted`, `Interactive`, `FirstInteraction`, `Paused`, `Resumed`, `BootFailed`, `IsAdOpen`; out: `SetAdOpen(true/false)` quanh mỗi rewarded ad, B6.10; *provisional*), Audio (soft, `SetMuteReason(Ad)`).

### Core Layer
1. Plinko Board — depends on: Object Pooling (pool `plinko.coin`), Bootstrap & Scene Flow (input `WorldPress`, tab, vòng đời `Paused`/`Resumed`, `Interactive` + `IsSimulationRunning`, `FrameChanged`). Audio chỉ gián tiếp qua Feedback & Juice (Plinko không gọi Audio). Công bố sự kiện `CoinSpawned`, `PegHit`, `JackpotHit`, `CoinResolved`, `CoinStuck`, `BoardBusy`; API `RequestBurst` / `RegisterAutoDropRate` / `RegisterBouncinessFactor` / `Unregister`; cờ `acceptManualTaps`; tham số vật lý có trần. Plinko công bố `IBaseCoinValueSource`, Economy Core cài đặt (Plinko có dự phòng `PlinkoConfig.baseCoinValue`).
2. Inventory — depends on: Pet Catalog, Bootstrap & Scene Flow (bước `inventory.core`), Economy Core (soft, thứ tự khởi tạo), Save/Load (soft, VS).
3. Rewarded Ads — depends on: Platform Services, Economy Core, Time & Timers, Audio (soft, `SetMuteReason(Ad)`), Plinko Board (soft, `RequestBurst`).

### Feature Layer
1. Gacha Machine — depends on: Economy Core, Pet Catalog, Inventory, Bootstrap & Scene Flow, HUD & Tab Navigation (Hard, popup).
2. Shelf & Synergy — depends on: Economy Core, Pet Catalog, Inventory, Plinko Board (sự kiện, API modifier bàn), Bootstrap & Scene Flow, HUD & Tab Navigation (soft), Star Progression (soft, VS).
3. Star Progression — depends on: Economy Core, Pet Catalog, Inventory, Gacha Machine.
4. Boosts — depends on: Economy Core, Plinko Board, Time & Timers.
5. Shop — depends on: Economy Core, Inventory, Shelf & Synergy, Boosts, Time & Timers.
6. Offline Earnings — depends on: Economy Core, Shelf & Synergy, Save/Load, Time & Timers, Bootstrap & Scene Flow (*provisional*).
7. Tutorial & Onboarding — depends on: Plinko Board, Gacha Machine, Shelf & Synergy, Shop, Bootstrap & Scene Flow (*provisional*).
8. Prestige — depends on: Economy Core, Inventory, Star Progression, Shelf & Synergy, Shop, Save/Load.
9. Analytics Hooks — depends on: Platform Services (lắng nghe sự kiện của các hệ gameplay).
10. Neighbor Pranks — depends on: Economy Core, Pet Catalog, Inventory, Shelf & Synergy, Time & Timers, Save/Load, Rewarded Ads.

Rewarded Ads là placement trong Shop (quà Bác Bảo Vệ), Offline Earnings (x2), Boosts (Mưa Xu) từ Alpha; các hệ đó gọi Rewarded Ads qua một API placement, không đổi tầng của chúng.

### Presentation Layer
1. HUD & Tab Navigation — depends on: Economy Core, Bootstrap & Scene Flow, Audio (soft, nút loa), Pet Catalog (soft, dữ liệu thẻ thú).
2. Feedback & Juice — depends on: Plinko Board, Gacha Machine (chiều phụ thuộc đang chốt), Shelf & Synergy (soft), HUD & Tab Navigation (soft), Audio, Object Pooling, Economy Core (`IncomeCredited`, `SpendRejected`), Bootstrap & Scene Flow (`TabChanged`), Pet Catalog (soft, dữ liệu thẻ thú).
3. Settings & Accessibility — depends on: Audio, Save/Load, Feedback & Juice.

**Điểm nghẽn** (nhiều hệ phụ thuộc, đếm cả cạnh soft và *provisional* trong cột Depends On): Bootstrap & Scene Flow (15), Economy Core (13), Pet Catalog (7), Plinko Board (7), Inventory (6), Shelf & Synergy (6), Save/Load (6), Audio (5), Time & Timers (5).
**Lá** (không hệ nào phụ thuộc, rủi ro thấp): Tutorial & Onboarding, Offline Earnings, Prestige, Analytics Hooks, Neighbor Pranks.

## Recommended Design Order

| Order | System | Priority | Layer | Agent(s) | Est. Effort |
|---|---|---|---|---|---|
| 1 | Economy Core | MVP | Foundation | game-designer | L |
| 2 | Pet Catalog | MVP | Foundation | game-designer | M |
| 3 | Bootstrap & Scene Flow | MVP | Foundation | engine-programmer | S |
| 4 | Object Pooling | MVP | Foundation | engine-programmer | S |
| 5 | Audio | MVP | Foundation | game-designer, engine-programmer | S |
| 6 | Plinko Board | MVP | Core | game-designer, gameplay-programmer | L |
| 7 | Inventory | MVP | Core | game-designer | S |
| 8 | Gacha Machine | MVP | Feature | game-designer | M |
| 9 | Shelf & Synergy | MVP | Feature | game-designer | L |
| 10 | HUD & Tab Navigation | MVP | Presentation | unity-ui-specialist, art-director | S |
| 11 | Feedback & Juice | MVP | Presentation | art-director, game-designer | M |
| 12 | Save/Load | Vertical Slice | Foundation | engine-programmer | M |
| 13 | Time & Timers | Vertical Slice | Foundation | engine-programmer | S |
| 14 | Star Progression | Vertical Slice | Feature | game-designer | M |
| 15 | Boosts | Vertical Slice | Feature | game-designer | S |
| 16 | Shop | Vertical Slice | Feature | game-designer | M |
| 17 | Offline Earnings | Vertical Slice | Feature | game-designer | S |
| 18 | Tutorial & Onboarding | Vertical Slice | Feature | game-designer, unity-ui-specialist | S |
| 19 | Platform Services | Alpha | Foundation | technical-director | M |
| 20 | Rewarded Ads | Alpha | Core | game-designer, technical-director | M |
| 21 | Prestige | Alpha | Feature | game-designer | M |
| 22 | Settings & Accessibility | Alpha | Presentation | unity-ui-specialist | S |
| 23 | Analytics Hooks | Full Vision | Feature | technical-director | S |
| 24 | Neighbor Pranks | Full Vision | Feature | game-designer | L |

Effort: S = one session, M = 2-3 sessions, L = 4+ sessions (a session produces one complete GDD).

Economy Core được mở lại một lần sau khi Plinko Board và Shelf & Synergy có số liệu (mô phỏng bảng tính giá quay vs thu nhập), và lần nữa khi thiết kế Star Progression và Prestige.

## Circular Dependencies
- `Plinko Board <-> Shelf & Synergy`: thú trên kệ đổi thưởng/vật lý Plinko (Mèo Chiêu Tài, Thỏ, Kẹo Bảy Màu), còn sự kiện Plinko kích hoạt hiệu ứng thú. Cách gỡ: Plinko công bố hợp đồng sự kiện (`CoinSpawned`, `PegHit`, `JackpotHit`, `CoinResolved`, `CoinStuck`, `BoardBusy`), API `RegisterAutoDropRate` / `RegisterBouncinessFactor` / `Unregister` và bộ tham số vật lý có trần do Plinko sở hữu; thưởng xu đi qua Income Pipeline. Shelf phụ thuộc Plinko, Plinko không biết Shelf.
- `Economy Core <-> các nguồn thu (Shelf, Boosts, Star Progression, Prestige)`: cách gỡ: Economy Core chỉ định nghĩa hợp đồng modifier (kênh, thứ tự cộng/nhân, trần); các hệ đăng ký modifier vào pipeline. Economy Core không phụ thuộc hệ nào ngoài Bootstrap & Scene Flow (hợp đồng pha, vòng đời), Save/Load (soft, từ VS) và hợp đồng Plinko (`CoinResolved`, `IBaseCoinValueSource`).
- `Economy Core <-> Plinko Board` (giá trị xu gốc): không còn vòng. Mô hình kéo: Plinko sở hữu `IBaseCoinValueSource`, Economy Core cài đặt; Plinko không tham chiếu Economy Core. Còn mở: Economy Core (Foundation) phụ thuộc Plinko (Core) — `game-systems` quyết định chuyển tầng hay tách module hợp đồng (`CoinResolved`, `IBaseCoinValueSource`) thành module Foundation.
- `Pet Catalog -> Plinko Board` (mũi tên đi lên, không phải vòng): Pet Catalog (Foundation) phụ thuộc mềm lúc làm nội dung vào Plinko Board (Core) — validator đọc `PlinkoConfig` (`jackpotBurstCount`, `autoDropBaseInterval`, `basePegBounciness`, `bouncinessCap`) và ngữ nghĩa hợp đồng sự kiện; không phụ thuộc lúc chạy. Cùng hướng xử lý với mũi tên Economy Core → Plinko Board.
- `Gacha Machine <-> Economy Core` (đường giá quay vs thu nhập): cách gỡ: họ công thức giá nằm ở Economy Core; Gacha chỉ tham chiếu. Thiết kế cùng nhau, mở lại Economy Core sau khi có số Plinko/Shelf.
- `Shop <-> Shelf & Synergy`: Shop mở ô, Shelf sở hữu số ô. Cách gỡ: Shelf cung cấp API mở ô; Shop phụ thuộc Shelf một chiều.

## High-Risk Systems

| System | Risk type (Technical / Design / Scope) | What could go wrong | Mitigation |
|---|---|---|---|
| Economy Core | Design | Giá quay vượt thu nhập tạo tường; prestige đến quá sớm/muộn; giá cố định mất ý nghĩa | Họ công thức giá theo giá quay (đường neo): mọi giá = hệ số × giá quay; mô phỏng bảng tính trước khi khóa số; mở lại sau Plinko/Shelf |
| Plinko Board | Technical + Design | Hũ x5 giữa dễ trúng (EV ~x3); lực nảy > 1 + Thỏ → xu vô hạn; autoclicker; nhiều xu làm tụt FPS WebGL | Điểm thả/hũ cân lại; trần nảy và despawn; trần tốc độ thả và số xu đồng thời; pooling; prototype đo FPS sớm |
| Shelf & Synergy | Design | Hội tụ về một đội hình tối ưu duy nhất, phá Pillar 3 | Hai kênh thu nhập cân bằng; combo nhóm cần thú Thường; kiểm tra bằng bảng so sánh đội hình theo cách chơi |
| Platform Services / Rewarded Ads | Technical | SDK khác nhau theo nền tảng; LevelPlay/AdMob không chạy WebGL; chính sách cổng game | Adapter theo nền tảng sau một interface; ADR chọn SDK; cap/cooldown theo nền tảng |
| Save/Load | Technical | Mất dữ liệu khi xóa dữ liệu trình duyệt; thay đổi schema làm hỏng save cũ | Xuất/nhập Base64; version schema + migration |
| Neighbor Pranks | Scope + Design | Phình NPC, đồ, timer; vi phạm Pillar 4 | Giữ ở Full Vision; GDD riêng với test "không mất vĩnh viễn"; có thể tách nhỏ khi thiết kế |

## Progress Tracker

| Metric | Count |
|---|---|
| Total systems | 24 |
| GDDs started | 11 |
| GDDs reviewed | 11 |
| GDDs approved | 11 |
| MVP systems designed | 11 / 11 |
| Vertical Slice systems designed | 0 / 7 |

## Next Steps
- [x] Approve this enumeration (2026-10-21)
- [x] Design MVP systems first with game-design (11/11 hệ MVP đã có GDD)
- [ ] Review each GDD with game-design-review (11 GDD MVP đã Approved; còn các hệ Vertical Slice trở đi chưa có GDD)
- [ ] Run game-gate technical-setup when the MVP systems are designed
