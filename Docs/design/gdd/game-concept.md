# Game Concept: Cozy Gachapon Room (Tiệm Trứng Thư Giãn)

*Created: 2026-10-21*
*Status: Draft*
*Nguồn: `Docs/FirstGDD.md` (giữ nguyên, không sửa) + quyết định cấp concept từ `Docs/design/gdd/reviews/first-gdd-review-2026-10-21.md`*

> [CD-PILLARS] skipped (lean)
> [AD-CONCEPT-VISUAL] skipped (lean)
> [TD-FEASIBILITY] skipped (lean)
> [PR-SCOPE] skipped (lean)

## Elevator Pitch
> Một game idle/incremental cozy màn hình dọc: bạn thả xu qua bàn Plinko gỗ, dùng xu vặn máy trứng Gachapon để săn thú cưng, rồi xếp chúng lên kệ 3–5 ô để tạo cộng hưởng và kiếm xu ngày càng nhiều.

## Core Identity
| Aspect | Detail |
|---|---|
| **Genre** | Casual Incremental / Idle + Gacha + Loadout (xếp kệ); từ v1.1 thêm tương tác bất đồng bộ với NPC hàng xóm |
| **Platform** | WebGL (Poki / CrazyGames), Android, iOS — màn dọc 9:16 / 9:19.5, không cuộn dọc |
| **Target Audience** | Người chơi casual thích sưu tập và thư giãn (xem Target Player Profile) |
| **Player Count** | Single-player (hàng xóm v1.1 là NPC chạy cục bộ) |
| **Session Length** | 5–15 phút, nhiều phiên mỗi ngày (idle + offline) |
| **Monetization** | F2P, 100% Rewarded Ads tự nguyện, không interstitial ép buộc |
| **Estimated Scope** | Medium (4-6 tháng, solo) |
| **Comparable Titles** | Cookie Clicker, Neko Atsume, Coin Master (chỉ phần vòng lặp đột kích) |

## Core Fantasy
Làm chủ một tiệm trứng nhỏ ấm cúng: nghe tiếng xu gỗ lách cách, hồi hộp chờ quả trứng bung ra, và tự tay sắp kệ thú cưng thành một "cỗ máy" kiếm xu của riêng mình — không áp lực, không bị phạt, luôn có điều nhỏ đáng mong chờ.

## Unique Hook
Như Cookie Clicker, nhưng nguồn xu là một bàn Plinko vật lý có thể chạm thả, và sức mạnh không đến từ nâng chỉ số mà từ cách xếp vài thú cưng gacha vào số ô kệ giới hạn.

## Player Experience Analysis (MDA)
### Target Aesthetics
| Aesthetic | Priority (1-8 or N/A) | How we deliver it |
|---|---|---|
| Sensation | 1 | Xu nảy qua chốt gỗ, âm marimba, máy rung "Cạch... Cạch... BỤP!", ánh sáng phẩm cấp khi bóc trứng |
| Submission | 2 | Idle CPS, thu nhập offline, nhịp chậm thư giãn, không thất bại |
| Discovery | 3 | Gacha bốc thú mới; khám phá combo nhóm/kề nhau trên kệ |
| Challenge | 4 | Tối ưu đội hình kệ theo cách chơi hiện tại (nhẹ, không có trạng thái thua) |
| Expression | 5 | Cách bày kệ phản ánh lối chơi (chủ động vs idle) và sở thích |
| Fantasy | 6 | Làm chủ tiệm trứng; "Mở Tiệm Mới" khi prestige |
| Fellowship | 7 | v1.1: trò nghịch qua lại với NPC hàng xóm có tên, tính cách (không giả người thật) |
| Narrative | N/A | Không có cốt truyện; chỉ flavor text của thú và hàng xóm |

### Key Dynamics
- Người chơi đổi đội hình kệ khi chuyển giữa ngồi chạm thả xu và để máy tự chạy.
- Người chơi thử ghép thú cùng nhóm / đặt cạnh nhau để tìm combo, kể cả dùng thú Thường.
- "Thêm một lượt quay nữa" khi gần đủ xu, và quay lại game để nhận offline.
- Người chơi chủ động chọn thời điểm "Mở Tiệm Mới" khi tiến độ chậm lại.

### Core Mechanics
1. Bàn Plinko vật lý (chạm thả + tự thả) với 5 hũ nhân.
2. Máy Gachapon: sink chính, bốc thú theo phẩm cấp; trùng thì lên Sao.
3. Kệ 3–5 ô: chỉ thú trên kệ mới tạo CPS/buff; synergy theo nhóm, vị trí kề nhau và 2 kênh thu nhập.
4. Tiệm Tạp Hóa: mở ô kệ, boost tạm thời, Flash Deal.
5. Prestige "Mở Tiệm Mới": reset xu/giá quay/ô kệ, giữ thú + sao, nhận tiền tệ vĩnh viễn.

## Player Motivation Profile
| Need | How this game satisfies it | Strength (Core / Supporting / Minimal) |
|---|---|---|
| Autonomy | Tự chọn đội hình kệ, chơi chủ động hay idle, thời điểm prestige, có xem ads hay không | Core |
| Competence | Thấy CPS tăng sau mỗi lần xếp kệ tốt; hiểu combo nhóm; prestige nhanh dần | Supporting |
| Relatedness | Gắn bó với thú cưng sưu tầm; v1.1 có NPC hàng xóm có tính cách | Minimal (v1.0) / Supporting (v1.1) |

### Player Type Appeal
- [x] Achievers — how: hoàn thành bộ sưu tập, nâng Sao, mốc prestige.
- [x] Explorers — how: khám phá combo nhóm/kề nhau, thú hiếm mới.
- [ ] Socializers — how: chỉ gián tiếp qua NPC hàng xóm v1.1; không có chat hay người chơi thật.
- [ ] Competitors — how: không có bảng xếp hạng hay PvP thật trong phạm vi hiện tại.

### Flow State Design
- **Onboarding curve**: 10 phút đầu: chạm thả xu → đủ xu quay lượt đầu → nhận thú → bày lên kệ thấy CPS chạy → mở ô kệ thứ 4. Mỗi bước dạy đúng một nút.
- **Difficulty scaling**: Không có độ khó; "thử thách" là giá quay tăng dần và đội hình kệ phải khéo hơn để theo kịp; prestige đặt lại đường cong.
- **Feedback clarity**: Số bay (floating text) trên mỗi xu, CPS trên header, so sánh CPS trước/sau khi đổi kệ.
- **Recovery from failure**: Không có thất bại. Hết xu → chờ CPS / offline / ads quà Bác Bảo Vệ. v1.1: mọi thiệt hại do hàng xóm đều tạm thời và gỡ được.

## Core Loop
Giữ nguyên vòng lặp FirstGDD §4:

```
Thả xu Plinko (chủ động / tự động) ──► Tích lũy Xu
        ▲                                   │
Sắp xếp Kệ tối ưu Synergy           Vặn trứng Gachapon
        ▲                                   │
Tiệm Tạp Hóa: mở thêm ô Kệ  ◄── Nhận thú cưng vào Kho (trùng → lên Sao)
```

### Moment-to-Moment (30 seconds)
Chạm thả xu, nhìn xu nảy qua chốt gỗ rơi vào hũ, xu bay lên ví. Phải tự thỏa mãn nhờ vật lý, âm thanh và số bay.

### Short-Term (5-15 minutes)
Gom đủ xu → vặn trứng → bóc vỏ → bày thú mới lên kệ hoặc lên Sao → thấy CPS tăng → mua boost/ô kệ. "Thêm một lượt" nằm ở thanh xu sắp đủ giá quay.

### Session-Level (30-120 minutes)
Phiên thật thường ngắn: mở game nhận offline (x2 qua ads), vài vòng quay, tối ưu kệ, đóng game. Điểm dừng tự nhiên: vừa mở xong một trứng hiếm hoặc vừa mua ô kệ. Lý do quay lại: thu nhập offline (trần giờ do game-design chốt), Flash Deal mỗi 30 phút.

### Long-Term Progression
- **Sao**: thú trùng lên Sao, tăng sản lượng gốc vĩnh viễn — nguồn tăng trưởng chính trong một tiệm.
- **Prestige "Mở Tiệm Mới"** (v1.0): khi giá quay vượt thu nhập (hoặc sau khi đủ thú), người chơi chuyển tiệm: reset xu, giá quay, ô kệ; giữ thú + Sao; nhận tiền tệ vĩnh viễn (tạm gọi "Tem Tiệm") để mua hệ số nhân / tiện ích lâu dài.
- **Thêm nội dung** (live-ops, sau v1.1): series thú / máy trứng mới.
- Game "xong" về mặt mục tiêu khi đủ bộ sưu tập ở Sao tối đa; prestige vẫn cho mục tiêu tối ưu hóa tiếp.

### Retention Hooks
- **Curiosity**: thú hiếm chưa có, combo nhóm chưa thử, Flash Deal mỗi 30 phút.
- **Investment**: Sao tích lũy, Tem Tiệm vĩnh viễn, thu nhập offline đang chờ.
- **Social**: v1.1: hàng xóm NPC nghịch thú của bạn, bạn đi đón thú về hoặc nghịch lại.
- **Mastery**: tìm đội hình kệ tốt nhất cho từng cách chơi; prestige mỗi lần nhanh hơn.

## Game Pillars
Bốn pillar giữ nguyên tên và ý từ FirstGDD §2; Pillar 3 và 4 được bổ sung theo quyết định concept.

### Pillar 1: Kinetic ASMR (Cảm giác động lực học thư giãn)
Sự thỏa mãn thị giác và thính giác khi nhìn dòng xu nảy qua các hàng chốt gỗ và rơi vào hũ thưởng.
*Design test*: Nếu tranh luận giữa thêm tính năng mới hay đánh bóng cảm giác xu rơi / âm thanh, pillar này chọn đánh bóng cảm giác.

### Pillar 2: Gacha Thrill (Kích thích dopamine có kiểm soát)
Hồi hộp khi vặn cần, bóc quả trứng để săn thú hiếm — nhưng chỉ bằng xu trong game, không tiền thật.
*Design test*: Nếu tranh luận giữa bỏ qua hoạt cảnh mở trứng cho nhanh hay giữ nhịp hồi hộp, pillar này chọn giữ nhịp (có nút tua cho người đã quen), và không bao giờ chọn bán lượt quay bằng tiền thật.

### Pillar 3: Meaningful Constraints (Tính toán chiến thuật có ý nghĩa)
Không nâng cấp chỉ số tĩnh; người chơi cân nhắc đội hình trong số ô kệ giới hạn để tối ưu cộng hưởng. Lựa chọn là thật vì:
- Thú có **nhóm** (ví dụ Động Vật, Đồ Ngọt, Hoàng Gia — danh sách cuối do game-design chốt); buff phụ thuộc **ô bên cạnh** và **số thú cùng nhóm** trên kệ.
- Thú chia **hai kênh thu nhập**: CPS idle và Plinko chủ động. Đội hình tốt nhất phụ thuộc vào cách chơi lúc đó và boost đang bật.
- Thú Thường mở combo nhóm mà thú hiếm không thay thế được; độ hiếm nâng trần, không quyết định thắng tuyệt đối.

*Design test*: Nếu một thú hay một đội hình tốt nhất trong mọi tình huống, pillar này yêu cầu sửa nó. "5 con hiếm nhất" không bao giờ được là đáp án đúng cho cả hai kênh cùng lúc.

### Pillar 4: Cozy Pranks (Tương tác tinh nghịch, không ức chế) — từ v1.1
Trò nghịch bất đồng bộ với **NPC hàng xóm** tạo lý do quay lại mà không bao giờ làm người chơi mất vĩnh viễn tài sản.
- **Không trừ xu đã nằm trong ví.** Phần thưởng của kẻ đi nghịch là xu sinh mới, không lấy từ nạn nhân (không tổng bằng không).
- Nạn nhân chỉ bị **tạm giữ** (thú bị dụ đi chơi, một phần xu bị giữ) và luôn gỡ lại được bằng 3 đường: trả tiền kẹo, xem ads Đội Cứu Hộ, hoặc chờ thú tự về.
- Đồ phòng thủ (Cún) đuổi được kẻ nghịch thì kẻ nghịch chỉ mất món đồ đã dùng, không mất xu.
- Hàng xóm luôn được ghi rõ là **NPC**: không username giả, không trạng thái online giả, không gọi là "người chơi khác".

*Design test*: Nếu một cơ chế khiến người chơi mất thứ gì đó không lấy lại được, pillar này loại cơ chế đó hoặc biến thiệt hại thành tạm thời.

### Anti-Pillars
- **NOT nâng cấp chỉ số tĩnh vô hạn** (mua "+1 CPS" lặp lại): phá Pillar 3, biến lựa chọn thành bấm mua.
- **NOT mất mát vĩnh viễn hay trừng phạt**: phá Pillar 4 và tông cozy.
- **NOT interstitial ép buộc hay bán gacha bằng tiền thật**: phá Pillar 2 ("có kiểm soát") và lòng tin của khán giả web/trẻ em.
- **NOT giả người chơi thật** (bot đội lốt người, chat giả): thiếu trung thực, rủi ro chính sách cổng game.
- **NOT hình ảnh bạo lực / tội phạm** (lồng sắt, bắt cóc, SWAT có vũ khí): phá tông cozy (xem Quyết định 3).

## Visual Identity Anchor
- **Direction**: Tiệm Gỗ Kem Bơ (Cozy Minimalist / Zero Art).
- **Visual rule**: Mọi thứ trông như đồ chơi gỗ bày trên kệ một tiệm tạp hóa ấm áp — phẳng, tròn, mềm, không cạnh sắc, không màu gắt.
- **Supporting principles**:
  1. *Zero Art*: vector phẳng + emoji giấy phép mở trong khung gỗ. Test: một asset mới có thay được bằng emoji + khung/nền gỗ không? Được thì không vẽ riêng.
  2. *Chuyển động thay cho chi tiết*: cảm xúc đến từ nảy, rung, bung, ánh sáng (DOTween, particle), không từ hình vẽ phức tạp. Test: khoảnh khắc này đã có juice (tween + âm thanh) chưa, trước khi thêm chi tiết hình?
  3. *Màu là thông tin*: màu nhấn chỉ dùng cho tương tác và phần thưởng. Test: nếu bỏ màu nhấn khỏi một phần tử mà người chơi vẫn hiểu nó bấm được, thì phần tử đó không cần màu nhấn.
- **Color philosophy** (giữ nguyên FirstGDD §3):
  - Nền chính giấy mộc / kem bơ `#FAF7F2` — sự yên tĩnh, không gian thở.
  - Khung viền & kệ gỗ sồi `#D9A066`, `#A77443` — cấu trúc, sự ấm áp, "đồ thật trong tiệm".
  - Điểm nhấn tương tác: xanh matcha `#7EA172` (xác nhận, tích cực), hồng san hô `#E07A5F` (chú ý, sự kiện), vàng mật ong `#F59E0B` (xu, phần thưởng, jackpot).
  - Chữ nâu cà phê đậm `#3D312A` — tương phản dịu mắt.
  - Màu phẩm cấp gacha: chưa định nghĩa, đặt trong art bible, phải hài hòa với palette trên.

### Bố cục giao diện (giữ nguyên FirstGDD §3)
Một khung dọc 9:16 / 9:19.5, ba vùng cố định, không cuộn dọc:

```
┌──────────────────────────────────────────────┐
│  TOP HEADER: [ 🪙 Số Xu ]  [ +CPS ] [ 🔥 Boost ]│
├──────────────────────────────────────────────┤
│               VÙNG NỘI DUNG                  │
│  (Chuyển đổi giữa 3 Tab không cuộn trang)    │
│  - TAB 1: Bàn Plinko & Máy Gacha             │
│  - TAB 2: Kệ 3-5 Slot & Kho Đồ               │
│  - TAB 3: Tiệm Tạp Hóa (Shop & Boosts)       │
├──────────────────────────────────────────────┤
│  BOTTOM NAV: [ 🎰 Bàn Chơi ] [ 🧸 Kệ ] [ 🛒 Tiệm ] │
└──────────────────────────────────────────────┘
```

Prestige "Mở Tiệm Mới" và (v1.1) lối vào Hàng Xóm phải đặt vừa trong ba tab này, không thêm tab thứ tư khi chưa có quyết định UI riêng.

## Inspiration and References
| Reference | What we take | What we do differently | Why it matters |
|---|---|---|---|
| Cookie Clicker | Vòng incremental, CPS, prestige | Nguồn xu là bàn Plinko vật lý; sức mạnh từ xếp kệ, không mua chỉ số | Chứng minh prestige giữ người chơi dài hạn |
| Neko Atsume | Tông cozy, sưu tầm thú, quay lại kiểm tra | Có vòng chủ động (Plinko) và tối ưu đội hình | Khán giả cozy thích sưu tập không áp lực |
| Coin Master | Vòng đột kích tạo lý do quay lại | Không tổng bằng không, NPC rõ ràng, không mất vĩnh viễn | Giữ hook xã hội, bỏ phần gây ức chế |
| Plinko / pachinko | Cảm giác xu nảy, hũ nhân | Hũ và điểm thả được cân lại (game-design) | Nền của Pillar 1 |

**Non-game inspirations**: máy gachapon capsule Nhật Bản, tiệm tạp hóa đồ gỗ, video ASMR đồ chơi gỗ / marimba.

## Target Player Profile
| Attribute | Detail |
|---|---|
| Age range | 10+ (khán giả web Poki/CrazyGames) đến người lớn chơi casual trên mobile |
| Gaming experience | Casual |
| Time availability | Nhiều phiên ngắn 5–15 phút trong ngày |
| Platform preference | Trình duyệt (desktop/mobile web), điện thoại |
| Current games they play | Idle/incremental trên Poki, game sưu tầm thú cozy, merge/idle mobile |
| What they look for | Thư giãn, tiến độ nhìn thấy được, niềm vui sưu tập, không bị phạt |
| What would turn them away | Quảng cáo ép buộc, tường trả tiền, mất đồ, tông bạo lực, lối chơi đòi tập trung cao |

Primary: Achievers / người sưu tầm (Quantic Foundry: Completion, Power). Secondary: Explorers thích tìm combo (Strategy). Không dành cho: người tìm PvP cạnh tranh thật, chiều sâu chiến thuật nặng, cốt truyện.

## Technical Considerations
| Consideration | Assessment |
|---|---|
| **Engine** | Unity 6000.3.16f1 |
| **Render pipeline** | URP (2D) |
| **Performance budget** | 60 FPS, ≤100 draw calls, ≤1024 MB |
| **Key technical challenges** | Plinko vật lý nhiều xu đồng thời trên WebGL/mobile (pooling, trần số xu, trần năng lượng nảy); Plinko world-space tách khỏi Canvas UI; số lớn dạng double + định dạng K/M/B; thời gian offline (chặn delta âm); lưu dữ liệu WebGL (xuất/nhập Base64); quảng cáo theo nền tảng (SDK Poki / CrazyGames trên web, LevelPlay hoặc AdMob trên mobile) |
| **Art style** | 2D phẳng (vector + emoji) |
| **Art pipeline complexity** | Low (emoji giấy phép mở + khung vector, tween) |
| **Audio needs** | Moderate — SFX ASMR là trọng tâm (marimba, xu, máy vặn, chuông phẩm cấp) |
| **Networking** | None (v1.0 và v1.1; hàng xóm là NPC cục bộ) |
| **Content volume** | v1.0: bộ thú khởi điểm (7 thú FirstGDD, số cuối do game-design chốt), 1 máy trứng, ~3 boost, prestige; v1.1: 4–6 món đồ hàng xóm + nhóm NPC |
| **Procedural systems** | Mô phỏng NPC hàng xóm (v1.1): tần suất, mục tiêu, kết quả — chạy cục bộ |

UI framework (uGUI hay UI Toolkit) chốt bằng ADR ở bước `game-architecture`; prototype hiện giả định uGUI (Canvas Scaler 1080×1920). Web Build Support cho Unity 6000.3.16f1 chưa cài.

## Quyết định cấp concept (từ review 2026-10-21)
| # | Vấn đề | Quyết định | Hệ quả cho game-design |
|---|---|---|---|
| 1 | Pillar 4 mâu thuẫn (Cần Câu lấy vĩnh viễn 15–25% xu, Cún thu tiền phạt) | Thưởng sinh mới, thiệt hại chỉ tạm thời; không hệ nào trừ xu trong ví; Cún chỉ làm kẻ nghịch mất món đồ | Cần Câu đổi thành "giữ tạm" một phần xu, có đường gỡ; định nghĩa thưởng cho kẻ đi nghịch, "két" |
| 2 | Phạm vi MVP | Raid/Kidnap dời sang v1.1; v1.0 = Plinko + Gacha + Kệ + Kho/Sao + Tiệm + Offline + Prestige + Rewarded Ads | Đồ đột kích/phòng thủ rút khỏi pool v1.0; drop pool v1.0 chỉ có thú |
| 3 | Tông cozy vs bắt cóc/lồng sắt/SWAT | "Trò nghịch hàng xóm": dụ thú đi chơi bằng kẹo, giỏ mây kẹt nắp, tiền kẹo đón thú về, Đội Cứu Hộ / Bác Bảo Vệ; hàng xóm luôn ghi rõ NPC | Đổi tên toàn bộ đồ v1.1 (ví dụ Bao Tải → Giỏ Kẹo Dụ Dỗ, Vé SWAT → Vé Đội Cứu Hộ) |
| 4 | Tăng trưởng dài hạn, mục tiêu sau khi đủ thú | Sao là tăng trưởng chính + Prestige "Mở Tiệm Mới" trong v1.0; nội dung mới là live-ops sau v1.1 | Công thức Sao (bản trùng/cấp, trần), điều kiện + thưởng prestige, tiền tệ Tem Tiệm |
| 5 | Pillar 3: xếp kệ hiển nhiên | Synergy theo nhóm + ô kề nhau + hai kênh thu nhập (CPS idle / Plinko chủ động) | Gán nhóm & kênh cho từng thú; cân lại Mèo Hoàng Gia (x2 mọi thứ) và Kẹo Bảy Màu |

## Risks and Open Questions
### Design Risks
- Hũ x5 ở giữa dễ trúng nhất khi người chơi tự chọn điểm thả → EV Plinko lệch (review BLOCKING).
- Synergy vẫn có thể hội tụ về một đội hình tối ưu duy nhất nếu hai kênh không cân bằng.
- Prestige đến quá sớm (nhàm) hoặc quá muộn (tường) — cần mô phỏng kinh tế.
- Bộ thú nhỏ khiến bộ sưu tập hoàn thành nhanh; Sao phải đủ dài.

### Technical Risks
- Feasibility not assessed (TD-FEASIBILITY skipped, lean).
- Lực nảy > 1.0 (Kẹo x2 bounciness) cộng năng lượng → xu vô hạn khi kết hợp Thỏ; cần trần nảy / despawn.
- Hiệu năng vật lý trên WebGL và thiết bị yếu khi tự thả / Mưa Xu; autoclicker.
- SDK quảng cáo khác nhau theo nền tảng; LevelPlay/AdMob không chạy WebGL.
- Lưu dữ liệu WebGL dễ mất khi xóa dữ liệu trình duyệt.

### Market Risks
- Idle/incremental trên Poki rất đông; hook Plinko + xếp kệ phải thấy được ngay trong 30 giây.
- Từ "gacha" có thể bị xem nhạy cảm với khán giả nhỏ tuổi; giảm thiểu: không bán bằng tiền thật, tỷ lệ công khai.

### Scope Risks
- Solo dev: hệ hàng xóm v1.1 dễ phình (NPC, đồ, timer, ads); giữ ranh giới v1.0 cứng.
- Prestige vào v1.0 cộng thêm khối lượng so với FirstGDD.

### Open Questions
Các mục BLOCKING còn lại của review thuộc tầng hệ thống, để dành cho `game-design`:
- Plinko: vị trí/hệ số hũ, điểm thả được phép, giá trị xu gốc, trần tốc độ thả & số xu đồng thời → GDD Plinko + playtest prototype.
- Công thức thu nhập đầy đủ (thứ tự cộng/nhân buff, Multiplier_Synergy, xu khởi đầu) → GDD Economy.
- Đường giá quay vs thu nhập, Sao, prestige → GDD Economy + Progression, mô phỏng bảng tính.
- Drop pool v1.0: tỷ lệ trong từng phẩm cấp, luật trùng → GDD Gacha.
- Giá theo CPS / giá quay thay vì số cố định (tiền kẹo, boost, quà ads, ô kệ) → GDD Economy + Shop.
- v1.1: đồ phòng thủ có chiếm ô kệ không, tần suất NPC nghịch (kể cả offline), Khiên, ô bị tạm giữ → GDD Neighbor Pranks.
- Cap / cooldown mỗi loại rewarded ad theo nền tảng → GDD Ads.

## MVP Definition
**Core hypothesis**: Thả xu Plinko + vặn trứng + xếp kệ 3 ô đủ thỏa mãn để người chơi tự nguyện chơi tiếp 15 phút và muốn quay lại.

**Required for MVP**:
1. Bàn Plinko chạm thả với 5 hũ nhân, cảm giác + âm thanh cơ bản.
2. Máy Gachapon với hoạt cảnh mở trứng, bộ thú khởi điểm.
3. Kệ 3 ô với CPS và ít nhất một combo nhóm/kề nhau để thử Pillar 3.
4. HUD xu/CPS theo bố cục 3 vùng.

**Explicitly NOT in MVP**:
- Tiệm, boost, Flash Deal, mở ô kệ.
- Sao, prestige, offline, lưu dữ liệu nâng cao.
- Quảng cáo.
- Toàn bộ hệ hàng xóm và đồ đột kích/phòng thủ.

### Scope Tiers
| Tier | Content | Features | Timeline |
|---|---|---|---|
| MVP | 1 bàn Plinko, 1 máy trứng, bộ thú khởi điểm, art placeholder | Core loop only: Plinko → Gacha → Kệ 3 ô | ~1 tháng |
| Vertical Slice | Bàn + máy + kệ hoàn chỉnh, art & SFX chuẩn | Core + Kho/Sao + Tiệm (ô kệ, boost) + Offline + synergy đầy đủ | ~2 tháng (cộng dồn) |
| Alpha (v1.0) | Đủ thú v1.0, prestige | Mọi tính năng v1.0: + Prestige, Flash Deal, Rewarded Ads theo nền tảng, lưu/xuất dữ liệu, build WebGL + Android + iOS | ~3 tháng (cộng dồn) |
| Full Vision (v1.1) | + Nhóm NPC hàng xóm, 4–6 món đồ hàng xóm | + Pillar 4 Cozy Pranks, ads Đội Cứu Hộ, chuẩn bị live-ops series | 4-6 tháng (solo) |

## Next Steps
- [x] Configure the Unity project (`game-setup`) — đã xong 2026-10-21
- [ ] Prototype the core mechanic if it is unproven (Plinko + Gacha + Kệ 3 ô)
- [ ] `game-design-review` cho concept này, rồi `game-systems`
- [ ] Write per-system GDDs in the GDD folder (`game-design`)
- [ ] Create epics (`game-epics`), then stories (`game-tasks`)
- [ ] Plan the first sprint (`game-sprint-plan`)
