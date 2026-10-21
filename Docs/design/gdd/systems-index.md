# Systems Index: Cozy Gachapon Room (Tiệm Trứng Thư Giãn)

> **Status**: Draft
> **Created**: 2026-10-21
> **Last Updated**: 2026-10-21
> **Source Concept**: Docs/design/gdd/game-concept.md

> [TD-SYSTEM-BOUNDARY] skipped (lean)
> [PR-SCOPE] skipped (lean)
> [CD-SYSTEMS] skipped (lean)

## Overview
Game cần 24 hệ thống quanh một vòng lặp duy nhất: Plinko sinh xu → Gacha tiêu xu ra thú → Kệ biến thú thành thu nhập → Tiệm mở rộng kệ. Mọi nguồn thu và mọi bùa lợi đi qua một Income Pipeline chung (Economy Core), nên các hệ gameplay chỉ đăng ký modifier vào pipeline thay vì gọi lẫn nhau. Raid/Kidnap của FirstGDD đã đổi thành **Neighbor Pranks** và dời sang v1.1 (Quyết định 2, 3 của concept).

**MVP** (11 hệ) — trả lời giả thuyết "Plinko + vặn trứng + kệ 3 ô đủ vui để chơi tiếp 15 phút":
- **Economy Core**: ví Xu, số lớn dạng double + định dạng K/M/B, Income Pipeline với hai kênh (CPS idle / Plinko chủ động), thứ tự cộng/nhân modifier, trần, xu khởi đầu, họ công thức giá theo CPS hoặc giá quay. Gỡ các BLOCKING về công thức thu nhập và giá cố định (Pillar 3).
- **Pet Catalog**: danh mục thú dạng ScriptableObject — nhóm, kênh thu nhập, phẩm cấp, chỉ số gốc, hiệu ứng. Nơi cân lại Mèo Hoàng Gia và Kẹo Bảy Màu (Quyết định 5, Pillar 3).
- **Bootstrap & Scene Flow** *(suy ra)*: một scene, trình tự khởi động, chuyển 3 tab. GDD ngắn, chi tiết chốt bằng ADR.
- **Object Pooling** *(suy ra)*: pool xu Plinko và số bay để giữ 60 FPS trên WebGL. GDD ngắn / ADR.
- **Audio** *(suy ra)*: mixer, pool SFX, nhạc nền — khung cho âm thanh ASMR (Pillar 1). GDD ngắn.
- **Plinko Board**: chạm thả + tự thả, 5 hũ nhân, điểm thả được phép, trần nảy, trần tốc độ thả và số xu đồng thời; công bố sự kiện `PegHit` / `CoinLanded(bucket)` (Pillar 1).
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
| 1 | Economy Core | Economy | MVP | Not Started | — | — |
| 2 | Pet Catalog | Economy | MVP | Not Started | — | — |
| 3 | Bootstrap & Scene Flow (inferred) | Core | MVP | Not Started | — | — |
| 4 | Object Pooling (inferred) | Core | MVP | Not Started | — | — |
| 5 | Audio (inferred) | Audio | MVP | Not Started | — | — |
| 6 | Plinko Board | Gameplay | MVP | Not Started | — | Economy Core, Object Pooling, Audio |
| 7 | Inventory | Economy | MVP | Not Started | — | Pet Catalog |
| 8 | Gacha Machine | Gameplay | MVP | Not Started | — | Economy Core, Pet Catalog, Inventory |
| 9 | Shelf & Synergy | Gameplay | MVP | Not Started | — | Economy Core, Pet Catalog, Inventory, Plinko Board |
| 10 | HUD & Tab Navigation | UI | MVP | Not Started | — | Economy Core, Shelf & Synergy, Bootstrap & Scene Flow |
| 11 | Feedback & Juice | UI | MVP | Not Started | — | Plinko Board, Gacha Machine, Shelf & Synergy, Audio, Object Pooling |
| 12 | Save/Load | Persistence | Vertical Slice | Not Started | — | — |
| 13 | Time & Timers (inferred) | Core | Vertical Slice | Not Started | — | — |
| 14 | Star Progression | Progression | Vertical Slice | Not Started | — | Economy Core, Pet Catalog, Inventory, Gacha Machine |
| 15 | Boosts (inferred) | Economy | Vertical Slice | Not Started | — | Economy Core, Plinko Board, Time & Timers |
| 16 | Shop | Economy | Vertical Slice | Not Started | — | Economy Core, Inventory, Shelf & Synergy, Boosts, Time & Timers |
| 17 | Offline Earnings | Economy | Vertical Slice | Not Started | — | Economy Core, Shelf & Synergy, Save/Load, Time & Timers |
| 18 | Tutorial & Onboarding (inferred) | Meta | Vertical Slice | Not Started | — | Plinko Board, Gacha Machine, Shelf & Synergy, Shop |
| 19 | Platform Services (inferred) | Meta | Alpha | Not Started | — | — |
| 20 | Rewarded Ads | Meta | Alpha | Not Started | — | Platform Services, Economy Core, Time & Timers |
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

### Foundation Layer (no dependencies)
1. Economy Core — framework mà mọi nguồn thu, mọi bùa lợi và mọi giá đều cắm vào (Income Pipeline + họ công thức giá).
2. Pet Catalog — dữ liệu tĩnh của thú; Inventory, Gacha, Shelf, Sao đều đọc.
3. Bootstrap & Scene Flow — trình tự khởi động và chuyển tab.
4. Object Pooling — dùng bởi Plinko và Feedback & Juice.
5. Audio — khung phát âm thanh; nội dung SFX map ở Feedback & Juice.
6. Save/Load — framework lưu trữ; mọi hệ có trạng thái đăng ký dữ liệu vào đây (từ Vertical Slice).
7. Time & Timers — nguồn thời gian và timer dùng chung.
8. Platform Services — lớp trừu tượng SDK theo nền tảng.

### Core Layer
1. Plinko Board — depends on: Economy Core, Object Pooling, Audio. Công bố hợp đồng sự kiện và tham số vật lý có trần.
2. Inventory — depends on: Pet Catalog.
3. Rewarded Ads — depends on: Platform Services, Economy Core, Time & Timers.

### Feature Layer
1. Gacha Machine — depends on: Economy Core, Pet Catalog, Inventory.
2. Shelf & Synergy — depends on: Economy Core, Pet Catalog, Inventory, Plinko Board (sự kiện).
3. Star Progression — depends on: Economy Core, Pet Catalog, Inventory, Gacha Machine.
4. Boosts — depends on: Economy Core, Plinko Board, Time & Timers.
5. Shop — depends on: Economy Core, Inventory, Shelf & Synergy, Boosts, Time & Timers.
6. Offline Earnings — depends on: Economy Core, Shelf & Synergy, Save/Load, Time & Timers.
7. Tutorial & Onboarding — depends on: Plinko Board, Gacha Machine, Shelf & Synergy, Shop.
8. Prestige — depends on: Economy Core, Inventory, Star Progression, Shelf & Synergy, Shop, Save/Load.
9. Analytics Hooks — depends on: Platform Services (lắng nghe sự kiện của các hệ gameplay).
10. Neighbor Pranks — depends on: Economy Core, Pet Catalog, Inventory, Shelf & Synergy, Time & Timers, Save/Load, Rewarded Ads.

Rewarded Ads là placement trong Shop (quà Bác Bảo Vệ), Offline Earnings (x2), Boosts (Mưa Xu) từ Alpha; các hệ đó gọi Rewarded Ads qua một API placement, không đổi tầng của chúng.

### Presentation Layer
1. HUD & Tab Navigation — depends on: Economy Core, Shelf & Synergy, Bootstrap & Scene Flow.
2. Feedback & Juice — depends on: Plinko Board, Gacha Machine, Shelf & Synergy, Audio, Object Pooling.
3. Settings & Accessibility — depends on: Audio, Save/Load, Feedback & Juice.

**Điểm nghẽn** (nhiều hệ phụ thuộc): Economy Core (11), Shelf & Synergy (7), Inventory (6), Pet Catalog (5), Plinko Board (4).
**Lá** (không hệ nào phụ thuộc, rủi ro thấp): HUD & Tab Navigation, Tutorial & Onboarding, Offline Earnings, Prestige, Settings & Accessibility, Analytics Hooks, Neighbor Pranks.

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
- `Plinko Board <-> Shelf & Synergy`: thú trên kệ đổi thưởng/vật lý Plinko (Mèo Chiêu Tài, Thỏ, Kẹo Bảy Màu), còn sự kiện Plinko kích hoạt hiệu ứng thú. Cách gỡ: Plinko công bố hợp đồng sự kiện (`PegHit`, `CoinLanded(bucket, value)`) và bộ tham số vật lý có trần do Plinko sở hữu; thưởng xu đi qua Income Pipeline. Shelf phụ thuộc Plinko, Plinko không biết Shelf.
- `Economy Core <-> các nguồn thu (Shelf, Boosts, Star Progression, Prestige)`: cách gỡ: Economy Core chỉ định nghĩa hợp đồng modifier (kênh, thứ tự cộng/nhân, trần); các hệ đăng ký modifier vào pipeline. Economy Core không phụ thuộc hệ nào.
- `Gacha Machine <-> Economy Core` (đường giá quay vs thu nhập): cách gỡ: họ công thức giá nằm ở Economy Core; Gacha chỉ tham chiếu. Thiết kế cùng nhau, mở lại Economy Core sau khi có số Plinko/Shelf.
- `Shop <-> Shelf & Synergy`: Shop mở ô, Shelf sở hữu số ô. Cách gỡ: Shelf cung cấp API mở ô; Shop phụ thuộc Shelf một chiều.

## High-Risk Systems

| System | Risk type (Technical / Design / Scope) | What could go wrong | Mitigation |
|---|---|---|---|
| Economy Core | Design | Giá quay vượt thu nhập tạo tường; prestige đến quá sớm/muộn; giá cố định mất ý nghĩa | Họ công thức giá theo CPS/giá quay; mô phỏng bảng tính trước khi khóa số; mở lại sau Plinko/Shelf |
| Plinko Board | Technical + Design | Hũ x5 giữa dễ trúng (EV ~x3); lực nảy > 1 + Thỏ → xu vô hạn; autoclicker; nhiều xu làm tụt FPS WebGL | Điểm thả/hũ cân lại; trần nảy và despawn; trần tốc độ thả và số xu đồng thời; pooling; prototype đo FPS sớm |
| Shelf & Synergy | Design | Hội tụ về một đội hình tối ưu duy nhất, phá Pillar 3 | Hai kênh thu nhập cân bằng; combo nhóm cần thú Thường; kiểm tra bằng bảng so sánh đội hình theo cách chơi |
| Platform Services / Rewarded Ads | Technical | SDK khác nhau theo nền tảng; LevelPlay/AdMob không chạy WebGL; chính sách cổng game | Adapter theo nền tảng sau một interface; ADR chọn SDK; cap/cooldown theo nền tảng |
| Save/Load | Technical | Mất dữ liệu khi xóa dữ liệu trình duyệt; thay đổi schema làm hỏng save cũ | Xuất/nhập Base64; version schema + migration |
| Neighbor Pranks | Scope + Design | Phình NPC, đồ, timer; vi phạm Pillar 4 | Giữ ở Full Vision; GDD riêng với test "không mất vĩnh viễn"; có thể tách nhỏ khi thiết kế |

## Progress Tracker

| Metric | Count |
|---|---|
| Total systems | 24 |
| GDDs started | 0 |
| GDDs reviewed | 0 |
| GDDs approved | 0 |
| MVP systems designed | 0 / 11 |
| Vertical Slice systems designed | 0 / 7 |

## Next Steps
- [x] Approve this enumeration (2026-10-21)
- [ ] Design MVP systems first with game-design (bắt đầu: Economy Core)
- [ ] Review each GDD with game-design-review
- [ ] Run game-gate technical-setup when the MVP systems are designed
