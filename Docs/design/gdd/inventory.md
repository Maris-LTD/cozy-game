# Inventory (Kho)

> **Status**: In Review
> **Author**: game-designer (workflow gdd-pipeline, đợt 2) + quyết định Inv D1–D6 của người dùng
> **Last Updated**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Implements Pillar**: Pillar 2 Gacha Thrill (phần thưởng bóc trứng ở lại, thành bộ sưu tập) · Anti-Pillar "NOT mất mát vĩnh viễn" (không bán, không trần, không xóa thú)

## Summary

Inventory là sổ sở hữu thú của người chơi: với mỗi **loài** (ID `pet.<slug>` của Pet Catalog) nó nhớ **đã có chưa** và **số bản thô** (`copies`, kiểu `int`). Thú trùng chỉ làm `copies` tăng (Inv D1); Inventory không biết Sao là gì, vì Sao = f(`copies` − 1) do Star Progression suy ra (Inv D2). Gacha Machine ghi vào qua `AddPet`; Shelf & Synergy, Star Progression, Shop, Prestige, Neighbor Pranks và màn Kho chỉ đọc. Không có sức chứa kho, không có trần số bản ngoài giới hạn `int` (Inv D6, D4), không có `RemovePet`. ID lạ khi nạp (thú chưa phát hành ở build này) được giữ ẩn, không xóa, và hiện lại khi Catalog có (Inv D5). Vị trí thú trên kệ và màn Kho thuộc Shelf (Inv D3, Shelf D2). MVP chạy bộ nhớ thuần, không lưu; từ Vertical Slice Inventory đăng ký mảng `{id, copies}` với Save/Load.

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `Pet Catalog (hard), Bootstrap & Scene Flow (hard, bước inventory.core pha GameplayInit), Economy Core (chỉ thứ tự khởi tạo), Save/Load (VS)`; dữ liệu cho Gacha Machine, Shelf & Synergy, Star Progression, Shop, Prestige, Neighbor Pranks

## Overview

Inventory là một **mô hình dữ liệu nhỏ có đúng một cổng ghi**. Nó trả lời ba câu hỏi cho phần còn lại của game: "người chơi có loài này chưa", "có bao nhiêu bản", "những loài nào đã sở hữu theo thứ tự ổn định". Nó cũng phát một sự kiện duy nhất, `PetAcquired`, để Gacha biết thú vừa nhận là mới hay trùng (hoạt cảnh khác nhau) và để HUD / Feedback cập nhật.

Ranh giới sở hữu (khớp registry và quyết định đợt 2):

| Thuộc Inventory | Không thuộc Inventory (chủ sở hữu) |
|---|---|
| Đã sở hữu loài X chưa, `copies` của X, danh sách loài đã sở hữu và thứ tự hiển thị, cách giữ ID lạ, `AddPet`, sự kiện `PetAcquired`, dữ liệu gửi Save/Load | Dữ liệu thú, ID, phẩm cấp (Pet Catalog) · tỷ lệ, pity, luật trùng (Gacha) · công thức Sao, trần Sao (Star Progression, Inv D2) · vị trí kệ, luật một loài tối đa một ô (Shelf D2, Inv D3), vẽ màn Kho (Shelf) · định dạng file save (Save/Load) · nội dung khởi động/vòng đời (Bootstrap) |

Hai điểm khác khuyến nghị gốc, đã chốt bởi người dùng: **không trần số bản** (Inv D4: bản vượt trần sẽ mất, trái Anti-Pillar) và **giữ ID lạ ẩn thay vì xóa** (Inv D5).

## Detailed Design

### Core Rules

Mọi tên trong `backtick` là knob ở `InventoryConfig` hoặc hằng engine (xem Tuning Knobs). Không có con số nào nằm trong logic.

**R1. Mô hình sở hữu (Inv D1)**
1. Trạng thái là ánh xạ `id → copies` (`int` ≥ 1). Loài **đã sở hữu** ⇔ có mục với `copies` ≥ 1. Không có mục `copies` = 0: chưa sở hữu thì không có mục.
2. Một loài là một mục duy nhất, dù nhận bao nhiêu lần; Inventory không lưu từng "cá thể", không lưu thời điểm nhận, không lưu thứ tự nhận.
3. Inventory chỉ khóa theo `id` (Catalog R1.2). Nó không sao chép dữ liệu thú; tên, phẩm cấp, nhóm, vai, sprite luôn đọc từ Catalog.
4. Inventory **không** lưu Sao, vị trí kệ, cờ "đã xem", hay bất kỳ thuộc tính nào khác của thú (Inv D2, D3). Thêm trường mới vào mục là thay đổi hợp đồng, phải qua GDD này.

**R2. Cổng ghi duy nhất `AddPet(id, amount = 1)`**
1. Kiểm theo thứ tự: (a) Inventory ở `Ready`, (b) `amount` ≥ 1, (c) `Catalog.TryGetPet(id)` tìm thấy, (d) hàng đợi R5.2 chưa đầy. Sai điều nào → trả `Rejected` với lý do `NotReady` / `InvalidAmount` / `UnknownId` / `QueueFull`; không đổi trạng thái, không phát sự kiện, không ném lỗi.
2. Đúng cả ba: `copies_after` theo công thức `add_copies`; `isNew` = (`copies_before` = 0). Mục được tạo nếu chưa có.
3. Trả `Added { isNew, copies }` và xếp `PetAcquired { id, isNew, copies }` vào hàng đợi một lần, **sau** khi trạng thái đã cập nhật (R5.2).
4. `amount` mặc định 1: một lượt Gacha cho một bản (Gacha D3: thú trùng chỉ cộng số bản). `amount` > 1 dành cho thưởng gộp sau này; luật khi nào gọi thuộc hệ gọi.
5. Thú `deprecated` (Catalog R9) vẫn nhận được qua `AddPet` nếu Catalog tra được ID; việc có đưa vào Gacha pool hay không là việc của Catalog và Gacha.
6. **Không có** `RemovePet`, giảm `copies`, bán, đổi hay tiêu thú ở v1.0 (Prestige giữ thú; Shop không bán thú). Thêm đường giảm `copies` là thay đổi hợp đồng.

**R3. Không trần, không sức chứa (Inv D4, D6)**
1. Không có số loài tối đa (chỉ bị giới hạn bởi số loài trong Catalog) và không có số bản tối đa theo thiết kế.
2. Giới hạn duy nhất là giới hạn kỹ thuật của `int`: cộng vượt thì **kẹp** về `INT_MAX`, không tràn số, không báo lỗi, không mất loài. Đây là hằng engine, không phải knob (xem Tuning Knobs).
3. Kẹp ở `INT_MAX` coi như người chơi đã "đầy" bản; hệ đọc (Star) tự xử lý giá trị lớn của chúng.

**R4. Truy vấn (chỉ đọc, không cấp phát GC)**
1. `IsOwned(id)` → `bool`; `GetCopies(id)` → `int` (0 nếu chưa có). Hai hàm trả "chưa có" / 0 cho ID lạ, ID orphan (R7), chuỗi rỗng, `null`; không ném lỗi.
2. `GetOwnedSpecies(List<string> buffer, group = null, rarity = null)` và `GetUnownedReleased(List<string> buffer)` (loài phát hành, không `deprecated`, chưa sở hữu, theo thứ tự R6; cho bóng mờ): ghi vào bộ đệm do **người gọi** cấp (xóa trước khi ghi) các `id` đã sở hữu, **không** gồm orphan, theo thứ tự R6, có lọc tùy chọn theo nhóm và phẩm cấp của Catalog. Bộ đệm `null` → trả 0, không ném lỗi.
3. `OwnedSpeciesCount`, `TotalCopies` (kiểu `long`, tổng của loài hiển thị), `ReleasedSpeciesCount` (số thú đã phát hành **không** `deprecated`, đọc từ Catalog), `CollectionProgress` (công thức `collection_progress`). `OwnedSpeciesCount` / `GetOwnedSpecies` vẫn gồm cả thú `deprecated` đã sở hữu; chỉ tiến độ loại chúng.
4. Mọi truy vấn trước `Ready` trả rỗng / 0 / `false`.

**R5. Sự kiện `PetAcquired { id, isNew, copies }`**
1. Đúng một sự kiện cho mỗi `AddPet` thành công; `copies` là giá trị **sau** khi cộng (đã kẹp) tại lúc `AddPet` đó, `isNew` đúng theo R2.2. Không phát khi `Rejected`, khi nạp save, hay khi áp dụng thú khởi đầu.
2. **Hàng đợi phát (FIFO)**: `AddPet` cập nhật trạng thái ngay, xếp sự kiện vào hàng đợi. Nếu không đang phát, `AddPet` phát hết hàng đợi tuần tự rồi mới trả về; nếu đang phát (gọi từ người nghe), nó chỉ xếp hàng và trả `Added` ngay. Mỗi sự kiện đến đủ mọi người nghe, theo cùng thứ tự đăng ký, trước khi sự kiện kế tiếp bắt đầu; thứ tự sự kiện = thứ tự `AddPet`. Truy vấn của người nghe thấy trạng thái mới nhất (có thể mới hơn payload); payload là ảnh chụp tại lúc `AddPet`. Người nghe được phép gọi `AddPet` lồng nhau; hàng đợi dài tối đa `InventoryConfig.maxQueuedEvents`, `AddPet` lồng làm vượt hạn → `Rejected(QueueFull)`, không đổi trạng thái.
3. Người nghe dự kiến: Gacha (chọn hoạt cảnh mới / trùng), HUD / Feedback (cập nhật huy hiệu, số), Star Progression (VS), Save/Load (VS, đánh dấu cần ghi). Cơ chế phát (C# event / bus) → ADR dùng chung với Economy và Plinko.

**R6. Thứ tự hiển thị (minorChoices "Thứ tự hiển thị")**
Khóa sắp: (1) phẩm cấp, theo `InventoryConfig.rarityOrderDescending` (đúng = Huyền Thoại trước); (2) nhóm theo thứ tự enum của Catalog (`animal`, `sweet`, `royal`); (3) `id` so sánh ordinal tăng dần. Thứ tự xác định, không phụ thuộc thứ tự nhận, không lưu. Danh sách đã sắp được giữ sẵn và chỉ dựng lại khi có loài **mới** (`isNew` = đúng) hoặc khi nạp; thú trùng không dựng lại.

**R7. ID lạ / orphan (Inv D5)**
1. **Orphan** = mục có `id` mà `Catalog.TryGetPet` không tìm thấy lúc nạp (thú `releaseTier` > `buildTier`, đổi build, ID hỏng). Phân loại cố định lúc nạp vì Catalog tĩnh.
2. Orphan được **giữ nguyên** `copies`, không xóa, không sửa, và được trả lại cho Save/Load khi ghi (VS) để không mất khi người chơi quay lại build có Catalog đủ.
3. Orphan **ẩn**: không có trong `GetOwnedSpecies`, `OwnedSpeciesCount`, `TotalCopies`, `CollectionProgress`; `IsOwned` = `false`, `GetCopies` = 0; không có sự kiện.
4. Khi build sau có `id` trong Catalog (nâng `buildTier`), mục tự là loài hiển thị với đúng `copies` đã giữ; không cần di chuyển dữ liệu.
5. `AddPet` với ID mà Catalog không tìm thấy luôn `Rejected(UnknownId)`, nên không thể tạo hay chạm vào orphan.

**R8. Thú khởi đầu (minorChoices "Số tuning")**
`InventoryConfig.startingPets` là danh sách `{id, copies}` áp dụng khi **bắt đầu game mới**. MVP mặc định rỗng (người chơi nhận thú đầu tiên từ Gacha); Onboarding có thể đặt 1 thú. Mục có ID Catalog không tìm thấy hoặc `copies` < 1 bị bỏ qua (validator Editor báo lỗi, R9.1). Áp dụng im lặng, không phát `PetAcquired`. Từ VS: chỉ áp dụng khi Save/Load **không có khóa** Inventory (game mới; mảng rỗng là dữ liệu hợp lệ của người chơi 0 thú, không áp dụng lại), không bao giờ ghi đè hay cộng thêm vào save đã có.

**R9. Validator (chỉ Editor / trước build)**
1. Lỗi: `startingPets` có ID không tồn tại trong Catalog hoặc `copies` < 1 hoặc ID trùng; `copiesDisplayMax` hoặc `maxQueuedEvents` ngoài khoảng.
2. Cảnh báo: `startingPets` chứa thú `deprecated`; `startingPets` > 1 mục lúc ra mắt.

**R10. Lưu (VS; MVP không lưu)**
1. MVP: bộ nhớ thuần; đóng game là mất, Inventory không đăng ký với Save/Load và bước `Run()` luôn bắt đầu từ `startingPets`.
2. VS: Inventory cung cấp cho Save/Load mảng cặp `{id, copies}` gồm **cả loài hiển thị lẫn orphan**, sắp theo `id` ordinal để file ổn định, và nhận lại mảng đó khi nạp. Save/Load sở hữu định dạng file và thời điểm ghi; Inventory chỉ báo "đã đổi" bằng `PetAcquired`.
3. Nạp (VS): mục `id` rỗng / `null` hoặc `copies` < 1 bị bỏ; hai mục trùng `id` được cộng (kẹp `INT_MAX`); phần còn lại xử lý theo R7.

**R11. Khởi động (IBootStep, `bootstrap-and-scene-flow.md` B2/B3)**
1. Inventory đăng ký một bước: `Id = inventory.core`, `Phase = GameplayInit`, `RunsAfter = [economy.core]`, `IsCritical = true`. Không đưa `pet.catalog` vào `RunsAfter` vì Catalog đã `Ready` ở `DataLoad` (pha trước, B3).
2. `RunsAfter` trỏ tới `economy.core` chỉ để giữ thứ tự khởi tạo mà Bootstrap B3 và Economy R16.1 đã ghi; Inventory không gọi Economy Core và không đọc ví.
3. `Run()` (một khung): (a) Catalog chưa `Ready` → `IsFailed` (BootFailed, `stepId = inventory.core`); (b) dựng bảng rỗng; (c) VS: nhận mảng từ Save/Load nếu có, phân loại orphan (R7); (d) nếu chưa có dữ liệu thì áp dụng `startingPets` (R8); (e) dựng danh sách đã sắp (R6); (f) vào `Ready`, báo `IsDone`.
4. Gacha và Shelf chỉ gọi Inventory sau `Interactive` / khi tab mở, nên Gacha (không có `RunsAfter`, B3) không được gọi `AddPet` trong `Run()` của nó; Shelf `RunsAfter` `inventory.core` (B3).

### States and Transitions

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Uninitialized | Từ lúc scene nạp tới khi bước `inventory.core` được gọi `Run()` | `Run()` được Bootstrap gọi | Truy vấn trả rỗng / 0; `AddPet` → `Rejected(NotReady)` |
| Ready | `Run()` hoàn tất không lỗi | Không thoát trong phiên | Nhận `AddPet`, trả truy vấn, phát `PetAcquired`; không bị ảnh hưởng bởi `Paused` / `Resumed`, đổi tab, quảng cáo, Shop reset, Prestige |
| Failed | Catalog chưa `Ready` lúc `Run()` (sai pha) | Không thoát (BootFailed) | Bước báo `IsFailed`; mọi `AddPet` → `Rejected(NotReady)` |

Không có trạng thái Saving / Dirty riêng: việc ghi là của Save/Load (R10.2).

### Interactions with Other Systems

| Hệ | Inventory nhận | Inventory cấp ra | Chủ interface |
|---|---|---|---|
| Pet Catalog | `TryGetPet(id)` (kiểm ID lúc `AddPet` và lúc nạp), danh sách thú đã phát hành, phẩm cấp, nhóm, `deprecated` | — | Catalog (dữ liệu, ID); Inventory (sở hữu, cách giữ ID lạ) |
| Bootstrap & Scene Flow | Lượt `Run()` của bước `inventory.core` ở `GameplayInit` | `IsDone` / `IsFailed` | Bootstrap (hợp đồng pha); Inventory (nội dung bước) |
| Economy Core | — (chỉ thứ tự: `inventory.core` `RunsAfter` `economy.core`) | — | Bootstrap |
| Gacha Machine | `AddPet(id, amount)` | Kết quả `Added { isNew, copies }` / `Rejected`, sự kiện `PetAcquired`, `IsOwned` (hoạt cảnh) | Inventory (hàm ghi, sự kiện); Gacha (khi nào gọi, luật trùng) |
| Shelf & Synergy (gồm màn Kho) | — | `IsOwned`, `GetCopies`, `GetOwnedSpecies`, `GetUnownedReleased`, `CollectionProgress`, `OwnedSpeciesCount`, `ReleasedSpeciesCount`; sự kiện `PetAcquired` | Inventory (sở hữu, hợp đồng thẻ); Shelf (vị trí, luật một loài/kệ Shelf D2, vẽ màn Kho) |
| Star Progression (VS) | — | `GetCopies(id)`, `PetAcquired` | Star sở hữu f(`copies` − 1), trần Sao (Inv D2) |
| Shop, Prestige, Neighbor Pranks | — | Đọc `IsOwned`, `GetCopies`, `GetOwnedSpecies` | Mỗi hệ sở hữu luật của mình; không hệ nào được ghi `copies` ngoài Gacha (và hệ thưởng được GDD sau cho phép gọi `AddPet`) |
| Save/Load (VS) | Mảng `{id, copies}` lúc nạp | Mảng `{id, copies}` (gồm orphan) | Save/Load (định dạng, thời điểm); Inventory (nội dung) |
| HUD & Tab Navigation, Feedback & Juice | — | `PetAcquired`, `CollectionProgress`, `OwnedSpeciesCount` (huy hiệu, cue) | Inventory (dữ liệu); HUD / Feedback (trình bày, cue) |

→ Thành ADR: cơ chế sự kiện và đăng ký bước `IBootStep` (dùng chung với các hệ khác); hình dạng API (interface thuần C#, không singleton); nguồn bộ đệm không cấp phát cho `GetOwnedSpecies`. Không có engine reference cho domain scripting: chưa kiểm khả thi theo Unity 6000.3.16f1; chưa có ADR nào.

## Formulas

Hệ này chỉ có số học nguyên và tỷ lệ; mọi biến lấy từ knob, trạng thái hoặc hằng engine.

### add_copies

`copies_after = min(copies_before + amount, INT_MAX)` (cộng bằng `long` rồi kẹp, không tràn `int`)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| copies_before | int | 0 – INT_MAX | calculated (trạng thái R1) | số bản trước khi cộng; 0 = chưa sở hữu |
| amount | int | 1 – INT_MAX | tham số `AddPet` (mặc định 1, R2.4) | số bản nhận thêm; < 1 bị `Rejected` (R2.1) |
| INT_MAX | int | 2 147 483 647 | constant | giới hạn `int` của .NET, không phải quyết định thiết kế (Inv D4) |

**Output range:** 1 đến INT_MAX; không bao giờ giảm, không bao giờ 0 sau khi cộng thành công. Ở cực trị `copies_before` = INT_MAX → kết quả INT_MAX, vẫn `Added`, vẫn phát `PetAcquired` với `isNew` = sai.
**Example:** (a) `copies_before` = 0, `amount` = 1 → 1, `isNew` = đúng. (b) 4 + 1 = 5, `isNew` = sai. (c) 2 147 483 646 + 5 → kẹp 2 147 483 647.

### collection_progress

`collection_progress = ReleasedOwned / ReleasedSpeciesCount` (= 0 khi `ReleasedSpeciesCount` = 0)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| ReleasedOwned | int | 0 – ReleasedSpeciesCount | calculated | số loài đã sở hữu, hiển thị, đã phát hành và **không** `deprecated` (orphan không tính, R7.3) |
| ReleasedSpeciesCount | int | 0 – số thú của Catalog ở `buildTier` | Catalog (`GetReleasedPets` + cờ `deprecated`; Assumed tên API) | số loài phát hành ở build này, **không** gồm `deprecated` |

Tử và mẫu cùng một tập nên tiến độ luôn ≤ 1.0 và đạt 1.0 được. Thú `deprecated` đã sở hữu vẫn có thẻ nhưng ngoài tiến độ.

**Output range:** 0.0 đến 1.0; 1.0 khi sở hữu hết mọi loài phát hành không `deprecated`. Mẫu số 0 trả 0, không chia cho 0.
**Example:** Catalog MVP 7 thú, đã có 3 → 3 / 7 ≈ 0.4286 (UI hiển thị "3/7", không hiển thị phần trăm).

### total_copies

`total_copies = Σ copies(id)` trên mọi loài hiển thị (kiểu `long`)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| copies(id) | int | 1 – INT_MAX | calculated | số bản của một loài hiển thị |
| N | int | 0 – ReleasedSpeciesCount | calculated | số loài hiển thị |

**Output range:** 0 đến N × INT_MAX (vừa trong `long`). Không bao gồm orphan.
**Example:** 3 loài với 1, 4, 2 bản → 7.

### copies_badge_text

`copies_badge_text = "x" + copies` nếu `copies` ≤ `InventoryConfig.copiesDisplayMax`, ngược lại `"x" + copiesDisplayMax + "+"`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| copies | int | 1 – INT_MAX | calculated | số bản thật (không bị cắt trong dữ liệu) |
| copiesDisplayMax | int | 9 – 999 | SO field `InventoryConfig.copiesDisplayMax` | số bản lớn nhất hiển thị nguyên |

**Output range:** chuỗi 2–5 ký tự ("x1" … "x999+"); chỉ là hiển thị, `GetCopies` luôn trả số thật.
**Example:** `copiesDisplayMax` = 99: 7 → "x7"; 99 → "x99"; 100 → "x99+"; INT_MAX → "x99+".

## Edge Cases

- **If `AddPet` nhận ID Catalog không tìm thấy** (ID gõ sai, thú `releaseTier` > `buildTier`, chuỗi rỗng, `null`): `Rejected(UnknownId)`, không đổi trạng thái, không sự kiện, không lỗi (R2.1).
- **If `AddPet` nhận `amount` ≤ 0**: `Rejected(InvalidAmount)`; `copies` không bao giờ giảm qua `AddPet`.
- **If `AddPet` được gọi trước `Ready` hoặc ở `Failed`**: `Rejected(NotReady)`; không có hàng đợi, hệ gọi tự quyết định (Gacha chỉ gọi sau `Interactive`).
- **If cộng làm `copies` vượt `INT_MAX`**: kẹp về `INT_MAX`, `Added`, `PetAcquired` phát với `copies` = `INT_MAX` (R3.2). Không mất loài.
- **If nạp save có mục orphan** (VS): mục được giữ nguyên, ẩn khỏi mọi truy vấn, và ghi lại nguyên vẹn lần lưu sau; `GetCopies(id)` vẫn trả 0 ở build này (R7).
- **If build sau có ID đã từng orphan trong Catalog**: mục hiện lại với `copies` cũ ngay lần nạp đó, không phát `PetAcquired`.
- **If save có hai mục cùng `id`, mục `copies` ≤ 0, hoặc `id` rỗng / `null`**: gộp cộng (kẹp) cho trùng; bỏ mục `copies` ≤ 0 hoặc `id` rỗng (R10.3). Không ném lỗi.
- **If `startingPets` có ID lạ, `copies` < 1 hoặc ID trùng**: runtime bỏ mục sai và nhận các mục đúng; ID trùng được gộp cộng; validator báo lỗi trước build (R8, R9).
- **If `startingPets` không rỗng và (VS) save đã có dữ liệu Inventory**: bỏ qua hoàn toàn `startingPets` (R8). Người chơi cũ không nhận thêm thú.
- **If `GetOwnedSpecies` nhận bộ đệm `null`**: trả 0, không lỗi; bộ đệm đã có phần tử được xóa trước khi ghi.
- **If người nghe `PetAcquired` gọi `AddPet` lồng nhau**: hợp lệ; trạng thái đổi ngay, sự kiện lồng xếp sau sự kiện đang phát và đến mọi người nghe sau khi sự kiện hiện tại đã đến hết (R5.2); không đệ quy.
- **If thú đã sở hữu bị Catalog đánh `deprecated`** (build sau): vẫn hiển thị thẻ, vẫn đếm trong `OwnedSpeciesCount` và `TotalCopies`; **không** tính trong tử / mẫu `collection_progress`; không bị xóa hay ẩn. Thú `deprecated` chưa sở hữu không có bóng mờ.
- **If Catalog không có thú phát hành** (`ReleasedSpeciesCount` = 0): `collection_progress` = 0, danh sách rỗng, không chia cho 0.
- **If Shop Reset / Prestige / `Paused` / quảng cáo xảy ra**: Inventory không đổi (R1.4); không có sự kiện ngoài `PetAcquired`.
- **If bước `inventory.core` chạy khi Catalog chưa `Ready`** (sai pha): `IsFailed` → BootFailed (R11.3); không cố nạp một nửa.
- **If `RunsAfter` `economy.core` trỏ tới bước không đăng ký**: Bootstrap B3.4 coi là BootFailed; Inventory không tự bỏ cạnh này (cấu hình MVP luôn có `economy.core`).

## Dependencies

| System | Direction | Hard or soft | Interface (data in / out, owner) |
|---|---|---|---|
| Pet Catalog | Inventory phụ thuộc Catalog | Hard | In: `TryGetPet(id)` (không tìm thấy = "không tìm thấy", không ném lỗi, Catalog R12.2), danh sách thú đã phát hành, phẩm cấp, nhóm, `deprecated`. Out: không. Owner: Catalog (dữ liệu, ID); Inventory (sở hữu, `copies`, ID lạ). Khớp Catalog Dependencies "Inventory". |
| Bootstrap & Scene Flow | Inventory phụ thuộc Bootstrap (hợp đồng pha) | Hard | In: `Run()` ở `GameplayInit`, hạn `BootConfig.stepTimeoutSeconds`. Out: bước `inventory.core` (`IsCritical = true`, `RunsAfter = [economy.core]`) báo `IsDone` / `IsFailed`. Owner: Bootstrap (hợp đồng, thứ tự); Inventory (nội dung). Khớp Bootstrap B3 và AC-2 (bỏ nhãn *provisional* ở bước Sync). |
| Economy Core | Inventory khai `RunsAfter` bước của Economy | Soft (chỉ thứ tự khởi tạo) | In/Out: không có dữ liệu. Owner: Bootstrap. Economy R16.1 đã ghi Inventory xếp sau bằng `RunsAfter`. |
| Gacha Machine | Gacha phụ thuộc Inventory | Hard (với Gacha) | Out: `AddPet(id, amount)` → `Added { isNew, copies }` / `Rejected`, `PetAcquired`, `IsOwned`. In: gọi `AddPet`. Owner: Inventory (hàm ghi, sự kiện); Gacha (tỷ lệ, pity, khi nào gọi). *GDD Gacha viết cùng đợt: phải dùng đúng tên và payload này.* |
| Shelf & Synergy | Shelf phụ thuộc Inventory | Hard (với Shelf) | Out: `IsOwned`, `GetCopies`, `GetOwnedSpecies`, `PetAcquired`. Owner: Inventory (sở hữu); Shelf (vị trí kệ, luật một loài/kệ Shelf D2, vẽ màn Kho). Out thêm `GetUnownedReleased`, `CollectionProgress`, `OwnedSpeciesCount`, `ReleasedSpeciesCount`. *Shelf `RunsAfter` `inventory.core`.* |
| Star Progression (VS) | Star phụ thuộc Inventory | Hard (với Star) | Out: `GetCopies(id)`, `PetAcquired`. Owner: Star (f(`copies` − 1), trần Sao, Inv D2). *GDD chưa có.* |
| Shop | Shop phụ thuộc Inventory | Soft (VS) | Out: đọc `IsOwned` / `GetCopies` nếu Shop cần điều kiện thú. Owner: Shop. *GDD chưa có.* |
| Prestige | Prestige phụ thuộc Inventory | Soft (Alpha) | Out: đọc để xác nhận thú được giữ; Prestige không xóa hay giảm `copies`. Owner: Prestige. *GDD chưa có.* |
| Neighbor Pranks | Neighbor phụ thuộc Inventory | Soft (v1.1) | Out: đọc `GetOwnedSpecies` (chọn thú "dụ đi chơi"); không ghi. Owner: Neighbor. *GDD chưa có.* |
| Save/Load | Inventory phụ thuộc Save/Load | Soft (VS; MVP không có) | In: mảng `{id, copies}` lúc nạp. Out: mảng `{id, copies}` gồm orphan, sắp theo `id`. Owner: Save/Load (định dạng, thời điểm); Inventory (nội dung). *GDD chưa có.* |
| HUD & Tab Navigation / Feedback & Juice | UI đọc Inventory | Soft | Out: `PetAcquired`, `CollectionProgress`, `OwnedSpeciesCount`. Owner: Inventory (dữ liệu); UI (trình bày). *Đã có trong systems index; HUD / Feedback không vẽ màn Kho (Shelf vẽ).* |

**Hai chiều**: Pet Catalog (Dependencies "Inventory", Interactions) đã liệt kê Inventory là hệ phụ thuộc Catalog với `TryGetPet` và danh sách phát hành; Bootstrap (B3, Dependencies) đã liệt kê `inventory.core` ở `GameplayInit` `RunsAfter` Economy; Economy Core R16.1 ghi Inventory xếp sau Economy. Systems index liệt kê Pet Catalog và Bootstrap trong Depends On của Inventory; Gacha, Shelf, Star, Shop, Prestige, Neighbor liệt kê Inventory. Khớp.

## Tuning Knobs

Một asset: `InventoryConfig` (ScriptableObject, runtime + validator). Không có số nào trong code. `INT_MAX` là hằng engine (xem dưới bảng).

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Thú khởi đầu | `InventoryConfig.startingPets` | mảng {id, copies} | rỗng (MVP) | 0–N theo Onboarding (validator cảnh báo > 1 mục lúc ra mắt); `copies` ≥ 1; ID thuộc Catalog `buildTier` | Người chơi nhận thú không qua Gacha, lệch kinh tế mở máy | — | Onboarding, Gacha pity, Catalog `buildTier` |
| Hiện ô thú chưa có | `InventoryConfig.showUnownedSlots` | bool | đúng | đúng / sai | — (đúng: hiện bóng mờ động lực sưu tập) | sai: màn Kho chỉ thấy thú đã có, mất mục tiêu sưu tập | Catalog (danh sách phát hành), UI |
| Thứ tự phẩm cấp | `InventoryConfig.rarityOrderDescending` | bool | đúng (Huyền Thoại trước) | đúng / sai | — | — | R6, UI |
| Hàng đợi sự kiện tối đa | `InventoryConfig.maxQueuedEvents` | int | 64 | 8–1024 | Người nghe lặp vô hạn chạy lâu mới bị chặn | Gacha / thưởng gộp hợp lệ bị `Rejected(QueueFull)` | R5.2 |
| Số bản hiển thị tối đa | `InventoryConfig.copiesDisplayMax` | int | 99 | 9–999 | Huy hiệu rộng, tràn thẻ nhỏ | Huy hiệu "9+" che mất khác biệt bản | `copies_badge_text`, UI |

**Hằng, không phải knob.** `INT_MAX` (2 147 483 647): giới hạn kiểu `int` của .NET dùng để kẹp, không phải quyết định cân bằng (Inv D4). Số bản mỗi lượt quay (1) thuộc Gacha, không phải Inventory (`amount` mặc định 1 là giá trị tham số, R2.4). Không có knob "trần số bản" hay "sức chứa" (Inv D4, D6); knob `copiesCap` của bản brief bị bỏ theo Inv D4.

## Visual/Audio Requirements

Inventory không có hiệu ứng riêng; các sự kiện dưới đây do hệ khác phát hình / tiếng (cue do Feedback & Juice phát, không phải Gacha), Inventory chỉ cung cấp tín hiệu.

| Event | Visual feedback | Audio feedback | Priority |
|---|---|---|---|
| `PetAcquired` với `isNew` = đúng | Gacha: hoạt cảnh mở trứng "thú mới"; màn Kho (Shelf): thẻ từ bóng mờ chuyển thành thẻ màu theo phẩm cấp | Cue bóc trứng do Feedback & Juice map qua `RevealProfile` và phát (Audio) | Cao |
| `PetAcquired` với `isNew` = sai | Gacha: thẻ báo "dùng để lên Sao (sắp có)" (Gacha D3); màn Kho: huy hiệu `copies_badge_text` nảy nhẹ | Không cue riêng của Inventory (Feedback chọn) | Trung bình |
| Màn Kho trống (0 loài) | Thông điệp trống thân thiện (UI Requirements) | Không | Thấp |

## Game Feel

Không áp dụng — Inventory là mô hình dữ liệu không có đầu vào trực tiếp của người chơi và không có hoạt ảnh; cảm giác bóc trứng thuộc Gacha, cảm giác thẻ trong màn Kho thuộc UI và Feedback & Juice.

## UI Requirements

Inventory không sở hữu màn hình. Màn Kho là một phần của Tab 2 "Kệ & Kho" và do **Shelf & Synergy** vẽ (Shelf D2; bố cục do GDD Shelf chốt). Thông tin dưới đây là hợp đồng dữ liệu để mọi nơi hiển thị giống nhau.

| Information | Display location | Update frequency | Condition |
|---|---|---|---|
| Thẻ loài: sprite, tên, phẩm cấp, nhóm, vai, mô tả hiệu ứng (đọc từ Catalog) | Màn Kho, theo thứ tự R6 | Khi mở màn và khi `PetAcquired` | Loài đã sở hữu |
| Huy hiệu số bản `copies_badge_text` | Góc thẻ | Khi `PetAcquired` | `copies` ≥ 1 |
| Bóng mờ / khóa của loài chưa có (`GetUnownedReleased`) | Màn Kho, cuối nhóm phẩm cấp tương ứng | Khi mở màn | `showUnownedSlots` = đúng; loài đã phát hành, không `deprecated`, chưa sở hữu |
| Tiến độ bộ sưu tập "owned/released" (ví dụ "3/7") | Đầu màn Kho | Khi `PetAcquired` | `ReleasedSpeciesCount` > 0 |
| Thông điệp Kho trống | Giữa màn Kho | Khi mở màn | `OwnedSpeciesCount` = 0 |
| Orphan | Không hiển thị ở đâu | — | Luôn ẩn (R7.3) |

## Cross-References

| This document references | Target GDD | Element referenced | Nature |
|---|---|---|---|
| `TryGetPet(id)`, ID `pet.<slug>`, danh sách phát hành, `deprecated`, `releaseTier` | `pet-catalog.md` | R1.2, R9, R12.2, Interactions "Inventory" | Phụ thuộc dữ liệu |
| Bước `inventory.core`, `IBootStep`, `RunsAfter`, `IsCritical`, BootFailed | `bootstrap-and-scene-flow.md` | B2, B3 (GameplayInit), B3.4, AC-2 | Phụ thuộc hợp đồng pha |
| Thứ tự khởi tạo sau `economy.core` | `economy-core.md` | R16.1 | Ràng buộc thứ tự |
| Cue bóc trứng, `RevealProfile` | `feedback-and-juice.md` | R4, R11 | Tín hiệu qua Feedback |
| Thú trùng chỉ cộng số bản, thẻ "dùng để lên Sao" | `gacha-machine.md` | G9, D3 | Hợp đồng ghi |
| Sao = f(`copies` − 1) | Star Progression (chưa có GDD) | Inv D2 | Hợp đồng đọc |
| Vị trí kệ, nhiều ô cùng loài | `shelf-and-synergy.md` | D2, màn Kho | Ranh giới sở hữu, chủ màn Kho |
| Mảng `{id, copies}` | Save/Load (chưa có GDD) | Định dạng, thời điểm ghi | Hợp đồng lưu (VS) |
| Pillar 2, Anti-Pillar "NOT mất mát vĩnh viễn" | `game-concept.md` | Game Pillars, Anti-Pillars | Neo thiết kế |

## Acceptance Criteria

Ký hiệu: `INV.` = `InventoryConfig`. Test EditMode dùng Catalog giả (stub: 3 thú `pet.a` Thường nhóm `animal`, `pet.b` Hiếm nhóm `sweet`, `pet.c` Huyền Thoại nhóm `royal`; `pet.d` có mặt trong "save" nhưng không có trong Catalog giả) và Bootstrap giả. Assembly test: `CozyGachapon.Tests.EditMode`.

- `AC-1 [EditMode]` **GIVEN** Inventory `Ready` và rỗng, **WHEN** `AddPet("pet.a")`, **THEN** trả `Added { isNew = true, copies = 1 }`, `IsOwned("pet.a")` = đúng, `GetCopies("pet.a")` = 1, `PetAcquired` phát đúng 1 lần với `{ pet.a, true, 1 }`.
- `AC-2 [EditMode]` **GIVEN** `pet.a` có 4 bản, **WHEN** `AddPet("pet.a")`, **THEN** `Added { isNew = false, copies = 5 }`, `OwnedSpeciesCount` không đổi, `PetAcquired` phát 1 lần với `{ pet.a, false, 5 }`.
- `AC-3 [EditMode]` **GIVEN** `pet.a` có 2 bản, **WHEN** `AddPet("pet.a", 3)`, **THEN** `copies` = 5 và `PetAcquired` phát đúng 1 lần (không 3 lần).
- `AC-4 [EditMode]` **GIVEN** `Ready`, **WHEN** `AddPet` với `"pet.zzz"`, `""`, `null`, **THEN** mỗi lần trả `Rejected(UnknownId)`, 0 sự kiện, `OwnedSpeciesCount` = 0, không exception.
- `AC-5 [EditMode]` **GIVEN** `Ready`, **WHEN** `AddPet("pet.a", 0)` và `AddPet("pet.a", -2)`, **THEN** cả hai `Rejected(InvalidAmount)`, 0 sự kiện, `GetCopies("pet.a")` = 0.
- `AC-6 [EditMode]` **GIVEN** Inventory `Uninitialized` (chưa `Run()`) và một Inventory `Failed`, **WHEN** `AddPet("pet.a")`, `IsOwned("pet.a")`, `GetCopies("pet.a")`, **THEN** `Rejected(NotReady)`, `false`, 0; trạng thái không đổi; 0 sự kiện.
- `AC-7 [EditMode]` **GIVEN** `pet.a` có `INT_MAX` − 1 bản, **WHEN** `AddPet("pet.a", 5)`, **THEN** `copies` = `INT_MAX` (2 147 483 647), `Added { isNew = false }`, `PetAcquired` phát 1 lần với `copies` = `INT_MAX`; **WHEN** `AddPet("pet.a")` thêm lần nữa, **THEN** `copies` vẫn `INT_MAX`, không tràn âm, không exception.
- `AC-8 [EditMode]` **GIVEN** `copies_before` = 0 và `amount` = 1, **WHEN** tính `add_copies` và `is_new`, **THEN** `copies_after` = 1 và `isNew` = đúng; **GIVEN** `copies_before` = `INT_MAX` và `amount` = `INT_MAX`, **THEN** `copies_after` = `INT_MAX`.
- `AC-9 [EditMode]` **GIVEN** Inventory có `pet.a`(2), `pet.b`(1), `pet.c`(1) và `INV.rarityOrderDescending` = đúng, **WHEN** `GetOwnedSpecies(buffer)`, **THEN** buffer = [`pet.c`, `pet.b`, `pet.a`]; **WHEN** `rarityOrderDescending` = sai, **THEN** [`pet.a`, `pet.b`, `pet.c`]; thứ tự giống nhau dù ba thú được nhận theo thứ tự nào.
- `AC-10 [EditMode]` **GIVEN** hai thú cùng phẩm cấp và nhóm có `id` `pet.x2`, `pet.x1`, **WHEN** `GetOwnedSpecies`, **THEN** `pet.x1` đứng trước `pet.x2` (ordinal tăng).
- `AC-11 [EditMode]` **GIVEN** Inventory có `pet.a`, `pet.b`, `pet.c`, **WHEN** `GetOwnedSpecies(buffer, group = sweet)` và `GetOwnedSpecies(buffer, rarity = legendary)`, **THEN** lần lượt [`pet.b`] và [`pet.c`]; bộ đệm có sẵn 5 phần tử bị xóa trước khi ghi; bộ đệm `null` → trả 0, không exception.
- `AC-12 [EditMode]` **GIVEN** save giả chứa `{pet.a, 3}`, `{pet.d, 7}` (`pet.d` không có trong Catalog giả), **WHEN** `Run()` nạp, **THEN** `IsOwned("pet.d")` = sai, `GetCopies("pet.d")` = 0, `OwnedSpeciesCount` = 1, `TotalCopies` = 3, `GetOwnedSpecies` không chứa `pet.d`, 0 sự kiện.
- `AC-13 [EditMode]` **GIVEN** trường hợp AC-12, **WHEN** xuất mảng cho Save/Load, **THEN** mảng chứa đúng 2 cặp `{pet.a, 3}` và `{pet.d, 7}` sắp theo `id`; **WHEN** dựng Inventory mới với Catalog giả có thêm `pet.d` rồi nạp mảng đó, **THEN** `GetCopies("pet.d")` = 7 và `OwnedSpeciesCount` = 2.
- `AC-14 [EditMode]` **GIVEN** orphan `pet.d`(7) đã nạp, **WHEN** `AddPet("pet.d")`, **THEN** `Rejected(UnknownId)` và mảng xuất vẫn có `{pet.d, 7}`.
- `AC-15 [EditMode]` **GIVEN** save giả chứa hai mục `{pet.a, 3}` và `{pet.a, 2}`, `{pet.b, 0}`, `{"" , 4}`, `{null, 1}`, **WHEN** nạp, **THEN** `GetCopies("pet.a")` = 5, `IsOwned("pet.b")` = sai, `OwnedSpeciesCount` = 1, không exception, mảng xuất không chứa các mục bị bỏ.
- `AC-16 [EditMode]` **GIVEN** `INV.startingPets` = [{`pet.a`, 1}] và không có dữ liệu save, **WHEN** `Run()`, **THEN** `GetCopies("pet.a")` = 1, 0 sự kiện `PetAcquired`; **GIVEN** cùng cấu hình nhưng save giả có `{pet.b, 2}`, **THEN** `IsOwned("pet.a")` = sai và `GetCopies("pet.b")` = 2.
- `AC-17 [EditMode]` **GIVEN** `INV.startingPets` = [{`pet.zzz`, 1}, {`pet.a`, 0}, {`pet.b`, 1}, {`pet.b`, 2}], **WHEN** chạy validator, **THEN** đúng 3 lỗi (ID lạ, `copies` < 1, ID trùng); **WHEN** `Run()` bỏ qua validator, **THEN** `OwnedSpeciesCount` = 1 và `GetCopies("pet.b")` = 3.
- `AC-18 [EditMode]` **GIVEN** `INV.startingPets` chứa thú `deprecated` hợp lệ, **WHEN** chạy validator, **THEN** 0 lỗi và đúng 1 cảnh báo.
- `AC-19 [EditMode]` **GIVEN** Catalog giả có 7 thú phát hành không `deprecated` và Inventory sở hữu 3, **WHEN** đọc `CollectionProgress`, **THEN** = 3/7 (±1e-6) và `ReleasedSpeciesCount` = 7; **GIVEN** Catalog giả 0 thú phát hành, **THEN** `CollectionProgress` = 0, không exception; **GIVEN** thêm 1 thú `deprecated` đã sở hữu và 1 chưa sở hữu, **THEN** `CollectionProgress` vẫn 3/7, `OwnedSpeciesCount` = 4, `GetUnownedReleased` không chứa thú `deprecated`; **GIVEN** sở hữu đủ 7 thú không `deprecated`, **THEN** `CollectionProgress` = 1.0.
- `AC-20 [EditMode]` **GIVEN** Inventory có `pet.a`(1), `pet.b`(4), `pet.c`(2) và orphan `pet.d`(100), **WHEN** đọc `TotalCopies`, **THEN** = 7 (kiểu `long`, không tính orphan).
- `AC-21 [EditMode]` **GIVEN** `INV.copiesDisplayMax` = 99, **WHEN** tạo `copies_badge_text` cho 7, 99, 100 và `INT_MAX`, **THEN** "x7", "x99", "x99+", "x99+"; `GetCopies` vẫn trả số thật; **WHEN** `copiesDisplayMax` = 8 hoặc 1000, **THEN** validator báo đúng 1 lỗi cho mỗi giá trị.
- `AC-22 [EditMode]` **GIVEN** người nghe L1 (đăng ký trước) gọi `AddPet("pet.b")` khi nhận `pet.a`, và người nghe L2 (đăng ký sau) chỉ ghi log, **WHEN** `AddPet("pet.a")`, **THEN** cả L1 và L2 nhận đúng 2 sự kiện theo thứ tự `{pet.a, true, 1}` rồi `{pet.b, true, 1}` (L2 thấy `pet.a` trước `pet.b`); `AddPet` lồng trả `Added` ngay; `GetCopies("pet.b")` = 1 ngay khi L1 xử lý `pet.a`; `AddPet` ngoài cùng chỉ trả sau khi hàng đợi rỗng; không đệ quy vô hạn, không exception.
- `AC-23 [EditMode]` **GIVEN** `IBootStep` giả của Catalog (Ready) và Economy, **WHEN** Bootstrap giả chạy `GameplayInit`, **THEN** `inventory.core` có `Phase` = `GameplayInit`, `RunsAfter` = [`economy.core`], `IsCritical` = đúng, chạy sau `economy.core`, `IsDone` trong cùng khung của `Run()`; **GIVEN** Catalog giả chưa Ready, **THEN** `IsFailed` = đúng và mọi `AddPet` → `Rejected(NotReady)`.
- `AC-24 [EditMode]` **GIVEN** Inventory có `pet.a`(2), **WHEN** phát `Paused` / `Resumed`, `TabChanged`, `ShopEconomyReset` giả, **THEN** `GetCopies("pet.a")` = 2 và 0 sự kiện `PetAcquired`.
- `AC-29 [EditMode]` **GIVEN** `INV.maxQueuedEvents` = 8 và người nghe luôn gọi `AddPet` lồng, **WHEN** `AddPet`, **THEN** đúng 8 sự kiện được phát, lần lồng thứ 9 trả `Rejected(QueueFull)` không đổi trạng thái, không exception, hàng đợi rỗng sau đó; **GIVEN** Save/Load có khóa Inventory là mảng rỗng và `startingPets` = [{`pet.a`, 1}], **THEN** `IsOwned("pet.a")` = sai.
- `AC-25 [EditMode]` **GIVEN** build MVP không đăng ký Save/Load, **WHEN** khởi động hai lần liên tiếp sau khi `AddPet("pet.a")` ở lần đầu, **THEN** lần hai bắt đầu từ `startingPets` (không có `pet.a` nếu `startingPets` rỗng).
- `AC-26 [Perf]` **GIVEN** Inventory có 13 loài, **WHEN** gọi `IsOwned`, `GetCopies`, `GetOwnedSpecies(buffer)` (bộ đệm dùng lại) 1000 lần, **THEN** cấp phát GC = 0 byte (Profiler `GC.Alloc` bằng 0) và tổng thời gian CPU < 1 ms trên máy mục tiêu; **WHEN** `AddPet` trùng 1000 lần, **THEN** GC = 0 byte (chỉ `AddPet` loài mới được phép cấp phát).
- `AC-27 [Play mode]` (chạy khi Gacha hoàn tất; trước đó dùng nút debug gọi `AddPet`) **GIVEN** scene `MainGame` build MVP khởi động xong (`BootCompleted`), **WHEN** gọi `AddPet` từ Gacha thật cho một thú Catalog MVP, **THEN** màn Kho (hoặc log kiểm thử) cho thấy loài đó với `copies` = 1 và tiến độ tăng thêm 1/7; quay trùng cùng loài tăng `copies` lên 2 mà không thêm ô mới.
- `AC-28 [Screenshot]` (chạy khi Shelf hoàn tất màn Kho) **GIVEN** màn Kho với 3 thú sở hữu (một có 100 bản, `copiesDisplayMax` = 99) và `showUnownedSlots` = đúng, **WHEN** chụp màn hình Kho, **THEN** thấy 3 thẻ màu theo phẩm cấp theo thứ tự R6, huy hiệu "x99+" trên thẻ 100 bản, 4 bóng mờ cho loài chưa có, tiêu đề tiến độ "3/7"; **GIVEN** 0 thú, **THEN** thấy thông điệp Kho trống và không thấy thẻ nào.

## Open Questions

| Question | Owner | Target date |
|---|---|---|
| Đã chốt (người dùng): Shelf vẽ cả Tab 2 gồm màn Kho; mỗi loài tối đa một ô (Shelf D2). Inventory chỉ cấp dữ liệu và hợp đồng thẻ. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: `maxQueuedEvents` = 64 và vượt hạn → `Rejected(QueueFull)` (chặn vòng lặp người nghe mà không làm rơi sự kiện đã nhận). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: `rarityOrderDescending` = đúng (Huyền Thoại trước) vì thú hiếm là điểm nhấn sưu tập; chỉnh bằng knob, không đổi dữ liệu. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: `AddPet(id, amount = 1)` có tham số `amount` thay vì chỉ `AddPet(id)` để thưởng gộp sau này không cần đổi hợp đồng; MVP chỉ Gacha gọi với 1. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: không có cờ "đã xem" / huy hiệu "Mới" lưu trong Inventory (R1.4); nếu UI cần, đó là trạng thái UI phiên, không lưu. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: knob `copiesCap` của brief bị bỏ vì Inv D4 (chỉ kẹp ở `INT_MAX`); `INT_MAX` ghi là hằng engine, không vào entities.yaml. | game-designer | 2026-10-21 (đã áp dụng) |
| Star Progression: GDD phải suy Sao chỉ từ `GetCopies` (Inv D2) và tự xử lý `copies` rất lớn (tới `INT_MAX`); Inventory không đổi nếu công thức Sao đổi. | Star owner | khi viết GDD Star |
| Gacha: GDD phải dùng đúng `AddPet(id, amount)` → `Added { isNew, copies }` / `Rejected`, sự kiện `PetAcquired { id, isNew, copies }`, không gọi `AddPet` trong `Run()` của bước Gacha (B3: Gacha không `RunsAfter` `inventory.core`), và xử lý `Rejected`. | Gacha owner | khi viết GDD Gacha |
| Save/Load (VS): định dạng mảng `{id, copies}` cùng orphan, và quy ước "không có khóa Inventory" (khác mảng rỗng) để áp dụng `startingPets` (R8). | Save/Load owner | VS |
| ADR: cơ chế sự kiện `PetAcquired` và đăng ký `IBootStep`, hình dạng API (interface, không singleton), bộ đệm không cấp phát (R4). Chưa kiểm khả thi theo Unity 6000.3.16f1 cho domain scripting. | technical-director → `game-adr` | 2026-11-04 |
| Việc cho bước Sync: thêm hàng Inventory vào systems-index (Core, MVP, Depends On: Pet Catalog, Bootstrap, Economy Core thứ tự); đổi *provisional* của `inventory.core` trong `bootstrap-and-scene-flow.md` thành xác nhận; thêm tên hợp đồng `AddPet` / `PetAcquired` / `inventory.core` vào registry nếu registry theo dõi sự kiện và bước khởi động. | Sync | 2026-10-21 |
