# **TÀI LIỆU THIẾT KẾ TRÒ CHƠI (GAME DESIGN DOCUMENT)**

> **Status**: Needs Revision

## **1\. TỔNG QUAN DỰ ÁN (EXECUTIVE SUMMARY)**

* **Tên dự án (Working Title):** Cozy Gachapon Room (Tiệm Trứng Thư Giãn).  
* **Thể loại:** Casual Incremental / Idle kết hợp Gacha, Deckbuilding Loadout và Asynchronous Social (PvP nhẹ nhàng).  
* **Nền tảng mục tiêu:** Web (WebGL / CrazyGames / Poki) & Mobile (Android / iOS).  
* **Game Engine:** Unity (2D Physics & UGUI / UI Toolkit).  
* **Định dạng hiển thị:** Màn hình dọc (Mobile-First Portrait), bố cục khóa cứng tỷ lệ \$100\\%\$ viewport không cuộn dọc (Zero Vertical Scroll).  
* **Phong cách nghệ thuật (Art Style):** Cozy / Minimalist (Zero Art) — bảng màu kem bơ, gỗ sồi ấm áp kết hợp biểu tượng vector phẳng và bộ Emojis có bản quyền mở.  
* **Mô hình kinh doanh:** Free-to-Play tích hợp \$100\\%\$ Quảng cáo nhận thưởng tự nguyện (Rewarded Ads).

## **2\. TRỤ CỘT THIẾT KẾ (DESIGN PILLARS)**

1. **Cảm giác động lực học thư giãn (Kinetic ASMR):** Sự thỏa mãn thị giác và thính giác khi nhìn dòng đồng xu vàng nảy tưng bừng qua các hàng chốt đinh gỗ và rơi vào các ô điểm thưởng.  
2. **Kích thích Dopamine có kiểm soát (Gacha Thrill):** Cảm giác hồi hộp khi vặn cần gạt, mở bung quả trứng Capsule để săn tìm các thẻ thú cưng quý hiếm và công cụ độc lạ.  
3. **Tính toán chiến thuật có ý nghĩa (Meaningful Constraints):** Loại bỏ việc nâng cấp chỉ số tĩnh vô vị; buộc người chơi phải cân nhắc sắp xếp đội hình trong số lượng ô kệ giới hạn để tối ưu hóa cộng hưởng (Synergy).  
4. **Tương tác xã hội tinh nghịch, không ức chế (Cozy Pranks):** Cơ chế "bắt cóc đòi tiền chuộc" bất đồng bộ tạo sự cạnh tranh và thôi thúc quay lại game (retention) mà không làm mất vĩnh viễn tài sản của người chơi.

## **3\. BỐ CỤC GIAO DIỆN & TRẢI NGHIỆM (UI/UX SPECIFICATION)**

Toàn bộ trò chơi hiển thị gọn gàng trong 1 khung hình dọc chuẩn (tỷ lệ $9:16$ hoặc $9:19.5$), chia làm 3 vùng cố định:

Plaintext  
┌──────────────────────────────────────────────┐  
│  TOP HEADER: \[ 🪙 Số Xu \]  \[ \+CPS \] \[ 🔥 Boost \]│  
├──────────────────────────────────────────────┤  
│                                              │  
│               VÙNG NỘI DUNG                  │  
│  (Chuyển đổi giữa 3 Tab không cuộn trang)    │  
│                                              │  
│  \- TAB 1: Bàn Plinko (Canvas) & Máy Gacha    │  
│  \- TAB 2: Kệ 3-5 Slot & Kho Đồ (Inventory)   │  
│  \- TAB 3: Tiệm Tạp Hóa (Shop & Boosts)       │  
│                                              │  
├──────────────────────────────────────────────┤  
│  BOTTOM NAV: \[ 🎰 Bàn Chơi \] \[ 🧸 Kệ \] \[ 🛒 Tiệm \] │  
└──────────────────────────────────────────────┘

* **Màu sắc chủ đạo (Cozy Palette):**  
  * Nền chính: Màu giấy mộc / kem bơ (\#FAF7F2).  
  * Khung viền & Kệ gỗ: Sắc gỗ sồi ấm (\#D9A066, \#A77443).  
  * Điểm nhấn tương tác: Xanh matcha thư giãn (\#7EA172), Hồng san hô (\#E07A5F), Vàng mật ong (\#F59E0B).  
  * Màu chữ: Nâu cà phê đậm (\#3D312A) tạo độ tương phản dịu mắt.

## **4\. VÒNG LẶP CỐT LÕI (CORE GAMEPLAY LOOP)**

\$\$\\begin{matrix} \\text{Thả xu Plinko (Active / Tự động)} & \\longrightarrow & \\text{Tích lũy Xu vào Túi tiền} \\\\ \\uparrow & & \\downarrow \\\\ \\text{Sắp xếp Kệ tối ưu Synergy / Buff} & & \\text{Vặn trứng Gachapon (Chi phí: 100 xu)} \\\\ \\uparrow & & \\downarrow \\\\ \\text{Tiệm Tạp hóa: Mở thêm Slot Kệ} & \\longleftarrow & \\text{Nhận Thú cưng / Đồ trộm / Đồ bảo vệ vào Kho} \\end{matrix}\$\$

## **5\. CHI TIẾT CÁC CƠ CHẾ CỐT LÕI**

### **5.1. Bàn Thả Xu Plinko (The Kinetic Engine)**

* **Quy tắc vật lý (Unity 2D Physics):**  
  * Đồng xu rơi chịu tác động của Rigidbody2D và trọng lực (GravityScale thấp tạo cảm giác trôi nhẹ nhàng).  
  * Chốt đinh cắm (CircleCollider2D với PhysicsMaterial2D có độ nảy Bounciness: 0.6 \- 0.75).  
* **Hành vi tương tác:**  
  * *Chủ động:* Người chơi chạm vào màn hình để thả xu rơi tại tọa độ chạm.  
  * *Tự động:* Bàn cờ tự động thả xu theo nhịp từ bùa lợi hoặc mua hiệu ứng tạm thời.  
* **Hệ thống 5 Hũ Đáy (Bottom Buckets):**  
  * Bố cục: \[x1\] \- \[x2\] \- \[x5 JACKPOT\] \- \[x2\] \- \[x1\].  
  * Rơi vào hũ nào sẽ nhân giá trị đồng xu với hệ số của hũ đó trước khi cộng vào tài khoản ví.  
  * Hũ x5 JACKPOT ở chính giữa là hũ kích hoạt các sự kiện thưởng đặc biệt.

### **5.2. Cỗ Máy Vặn Trứng Gachapon (The Primary Sink)**

* **Chi phí mỗi lượt vặn:** Khởi điểm $100$ xu.  
* **Hiệu ứng Game Feel:** Khi bấm nút hoặc kéo cần, máy rung lắc (DOTween shake), phát âm thanh cơ học *"Cạch... Cạch... BỤP\!"* và đẩy ra một quả trứng Capsule.  
* **Màn hình bóc vỏ (Unboxing Modal):** Người chơi chạm vào quả trứng để tách đôi vỏ, ánh sáng màu phẩm cấp tỏa ra kèm âm thanh chuông hòa âm.  
* **Phân bổ tỷ lệ rơi (Gacha Drop Pool):**

| Phẩm cấp / Loại | Tỷ lệ | Nhóm nội dung mở ra |
| :---- | :---- | :---- |
| **Thường (Common)** | \$55\\%\$ | Thú cưng cơ bản (Vịt Gỗ, Ếch Cốm), Ổ Khóa Gỗ Mini 🔒, Gương Dò La 🪞 |
| **Hiếm (Rare)** | \$30\\%\$ | Thú cưng buff (Mèo Chiêu Tài, Thỏ Nhào Lộn), Cún Bông Canh Nhà 🐶, Cần Câu Trộm Xu 🎣 |
| **Sử Thi (Epic)** | \$12\\%\$ | Thú cưng cộng hưởng (Cáo Khăn Len, Kẹo Bảy Màu), Bao Tải Bắt Cóc 👝, Hộp Ngụy Trang 📦 |
| **Huyền Thoại (Legendary)** | \$3\\%\$ | Thú cưng tối thượng (Mèo Hoàng Gia Béo Múp), Vé Cảnh Sát Trưởng SWAT |

### **5.3. Kệ Xuất Chiến & Bùa Lợi Cộng Hưởng (Tactical Shelf Loadout)**

* **Quy tắc giới hạn Slot:**  
  * Khởi đầu game: Chỉ có **3 Slot trống trên Kệ**.  
  * Người chơi có thể mở rộng lên tối đa **5 Slot** thông qua việc chi trả một lượng xu lớn trong Cửa hàng.  
  * **Chỉ những vật phẩm được bày trực tiếp trên Kệ mới kích hoạt bùa lợi và sinh xu thụ động (CPS)**.  
* **Bảng thiết kế bùa lợi Đồ chơi mẫu:**

Plaintext  
\[ 🐥 Vịt Gỗ \] (Common)       : \+2 CPS cố định.  
\[ 🐸 Ếch Cốm \] (Common)      : \+3 CPS cố định.  
\[ 🐱 Mèo Chiêu Tài \] (Rare)   : \+5 CPS. Thưởng nóng \+50 xu mỗi khi bóng rơi trúng Hũ Jackpot x5\!  
\[ 🐰 Thỏ Nhào Lộn \] (Rare)    : \+6 CPS. Mỗi cú nảy vào chốt đinh Plinko cộng thêm \+1 xu.  
\[ 🦊 Cáo Khăn Len \] (Epic)    : \+12 CPS. Nhân đôi (x2) toàn bộ sản lượng CPS của mọi thú hệ "Động Vật" khác đang nằm trên kệ\!  
\[ 🍬 Kẹo Bảy Màu \] (Epic)     : \+15 CPS. Tăng gấp đôi lực nảy của toàn bộ bàn Plinko.  
\[ 👑 Mèo Hoàng Gia \] (Legend) : \+35 CPS. Nhân đôi (x2) toàn bộ thu nhập từ cả Plinko lẫn Kệ\!

### **5.4. Kho Đồ Hợp Nhất (Unified Inventory System)**

* Quản lý tập trung toàn bộ tài sản người chơi sở hữu trên một giao diện lưới ngăn nắp:  
  * **Tab Lọc / Thẻ:** Tất Cả | Đồ Chơi | Đồ Đột Kích | Đồ Phòng Thủ.  
  * **Cơ chế sao chép tăng sao:** Nếu quay trúng một con thú đã có sẵn, nó tự động tăng lên cấp Sao (⭐ $\rightarrow$ ⭐⭐) giúp tăng vĩnh viễn sản lượng gốc mà không tốn diện tích kho.  
  * **Thao tác nhanh:** Bấm vào vật phẩm hiển thị pop-up chi tiết bùa lợi kèm nút hành động: *Bày Lên Kệ*, *Cất Vào Kho*, hoặc *Sử Dụng Ngay*.

### **5.5. Tiệm Tạp Hóa (The Tactical Shop \- Thay thế Nâng cấp cũ)**

Cửa hàng đóng vai trò là "bể xả tiền" (Coin Sink) giúp kiểm soát lạm phát và cung cấp công cụ lướt sóng:

1. **Mở Rộng Slot Kệ (Mục tiêu lớn dài hạn):**  
   * Mở Slot thứ 4: $500$ xu.  
   * Mở Slot thứ 5: $2.000$ xu.  
2. **Bùa Tăng Tốc Tạm Thời (Consumable Boosts):**  
   * *Trà Sốt Dẻo (Fever Tea \- 150 xu):* Nhân đôi ($x2$) toàn bộ tiền kiếm được trong 30 giây.  
   * *Mưa Đồng Xu (Coin Rain \- 200 xu):* Thả lập tức 25 đồng xu liên tục vào bàn Plinko.  
3. **Gian Hàng Chợ Cũ (Flash Deals):**  
   * Mỗi 30 phút xuất hiện 1 thẻ thú cưng cụ thể cho phép mua đứt bằng xu (giá cao) thay vì phải quay may rủi.

## **6\. HỆ THỐNG XÃ HỘI BẤT ĐỒNG BỘ: BẮT CÓC ĐÒI TIỀN CHUỘC (ASYNCHRONOUS RAID & KIDNAP)**

Nhằm tối ưu hóa chi phí vận hành cho solo dev, **Giai đoạn 1 (v1.0) triển khai \$100\\%\$ bằng hệ thống Bot giả lập cục bộ (Local Simulated Bots)** mà không cần thuê máy chủ server.

Plaintext  
┌─────────────────────────────────────────────────────────────┐  
│             KỊCH BẢN ĐỘT KÍCH & BẮT CÓC                     │  
└─────────────────────────────────────────────────────────────┘

       \[ NGƯỜI CHƠI QUAY RA VẬT PHẨM ĐỘT KÍCH \]  
                          │  
                          ▼  
                 \[ BẤM NÚT: ĐỘT KÍCH \]  
      Game chọn ngẫu nhiên 1 Bot (hoặc đối thủ)  
                          │  
            ┌─────────────┴─────────────┐  
            ▼                           ▼  
\[ GẶP PHÒNG CÓ BẢO VỆ \]     \[ GẶP Ô KHÔNG BẢO VỆ \]  
\- Gặp Ổ Khóa 🔒: Bẻ khóa,   \- Dùng Bao Tải 👝 bắt cóc 1 thú\!  
  thú an toàn.              \- Nạn nhân bị tạm dừng CPS ô đó.  
\- Gặp Cún 🐶: Bị cắn đuổi,   \- Đặt giá tiền chuộc (ví dụ: 300 xu).  
  rơi lại tiền phạt.  
                          │  
                          ▼  
             \[ NẠN NHÂN MỞ GAME / NHẬN TIN \]  
            Hiển thị lồng sắt ⛓️ và 3 lựa chọn:  
 ┌────────────────────────┼────────────────────────┐  
 ▼                        ▼                        ▼  
\[ TRẢ TIỀN CHUỘC \]     \[ XEM VIDEO ADS \]     \[ ĐỢI TỰ ĐÀO TẨU \]  
Trả 300 xu, giải cứu   SWAT ập vào giải cứu  Hết 2 tiếng thú cưng  
ngay lập tức.          tức thì \+ Tặng Khiên  tự trốn về an toàn.  
                       bảo hộ 4 tiếng\!

### **Bộ Công Cụ Tương Tác Cụ Thể (Chỉ Quay Ra Từ Gacha):**

* **Công cụ đi trộm (Raid Gadgets):**  
  * 👝 *Bao Tải Bắt Cóc:* Đột kích bắt cóc 1 thú cưng đối thủ trong 2 tiếng.  
  * 🎣 *Cần Câu Trộm Xu:* Câu trộm trực tiếp \$15\\% \- 25\\%\$ số xu trong két của đối thủ.  
* **Công cụ phòng vệ (Defense Gadgets):**  
  * 🔒 *Ổ Khóa Gỗ Mini:* Gắn cố định lên 1 ô kệ; chịu đòn thay cho thú cưng 1 lần duy nhất.  
  * 🐶 *Cún Bông Canh Nhà:* Tự động đuổi trộm và nhặt tiền phạt rớt lại từ kẻ đột kích.

## **7\. MÔ HÌNH KINH TẾ & TÍNH TOÁN (ECONOMY & FORMULAS)**

### **7.1. Công thức chi phí & Tăng trưởng**

**Chi phí vặn Gacha cấp số nhân nhẹ:**

$$Chiphílượttiếptheo=Chiphígốc\times (1.05{)}^{sốlầnvặn}$$

*   
  *(Hàm mũ nhẹ giúp giữ giá trị cho tài nguyên nhưng không tạo bức tường quá dốc đầu game)*.  
  **Sản lượng thụ động (CPS \- Coins Per Second):**

  $$TổngCPS=\left({\sum\limits_{}^{}CPScủatừngthútrênkệ}\right)\times Multiplie{r}_{Synergy}\times Multiplie{r}_{Fever}$$  
* 

### **7.2. Tích lũy Ngoại Tuyến (Offline Earnings)**

Khi đóng game, hệ thống lưu lại Timestamp. Khi mở lại:

$$XuOffline=Thờigianvắngmặt(tốiđa4tiếng)\times TổngCPS\times 0.7$$

* 

* Pop-up chào mừng cung cấp 2 nút: Nhận Thường hoặc Xem Video Ads nhận x2.

## **8\. MÔ HÌNH KIẾM TIỀN (REWARDED ADS MONETIZATION)**

Tích hợp thông qua **Unity LevelPlay (IronSource)** hoặc **Google AdMob**, cam kết \$100\\%\$ không sử dụng quảng cáo ngắt quãng ép buộc (Zero Forced Interstitial):

1. **SWAT Giải cứu Thú cưng:** Bỏ qua tiền chuộc, bẻ khóa lồng tức thì và nhận Khiên bảo vệ 4 tiếng\[cite: 1, 3\].  
2. **Nhân đôi thưởng Offline:** Tăng gấp đôi sản lượng tích lũy khi mở lại game.  
3. **Cơn Mưa Đồng Xu:** Kích hoạt xả ngay 30 đồng xu rơi liên tục vào bàn Plinko.  
4. **Quà Bác Bảo Vệ (Shop):** Tặng ngay $150$ xu nạp máy vặn trứng khi người chơi cạn tiền.

## **9\. HƯỚNG DẪN TRIỂN KHAI TRÊN UNITY (TECHNICAL ARCHITECTURE)**

Plaintext  
Assets/  
├── \_Scripts/  
│   ├── Core/  
│   │   ├── GameManager.cs          (Quản lý State, Game Loop, Lưu dữ liệu)  
│   │   ├── EconomyManager.cs       (Tính toán Xu, CPS, Bùa lợi Synergy)  
│   │   └── AudioManager.cs         (Phát âm thanh mộc Marimba, Drop sound)  
│   ├── Plinko/  
│   │   ├── PlinkoSpawner.cs        (Xử lý thả xu chạm cảm ứng và tự động)  
│   │   ├── CoinPhysics.cs          (Vật lý va chạm 2D, trigger hũ đáy)  
│   │   └── BucketTrigger.cs        (Xử lý nhân thưởng x1, x2, x5)  
│   ├── Gachapon/  
│   │   ├── GachaMachine.cs         (Hiệu ứng vặn, trừ xu, sinh Capsule)  
│   │   └── UnboxModalUI.cs         (Hiệu ứng bóc tách trứng, hé lộ thẻ)  
│   ├── InventoryAndShelf/  
│   │   ├── InventoryManager.cs     (Quản lý danh sách vật phẩm sở hữu)  
│   │   ├── ShelfController.cs      (Quản lý 3-5 Slot xuất chiến, tính Synergy)  
│   │   └── ItemDataSO.cs           (ScriptableObject định nghĩa chỉ số Pet)  
│   ├── Social/  
│   │   ├── MockBotSystem.cs        (Tạo danh sách Bot ảo, mô phỏng bị cướp)  
│   │   └── KidnapController.cs     (Đồng hồ đếm ngược lồng sắt, xử lý chuộc)  
│   └── Ads/  
│       └── AdsManager.cs           (Tích hợp gọi Rewarded Video Ads SDK)  
├── ScriptableObjects/  
│   ├── Pets/                       (Dữ liệu các thẻ thú cưng)  
│   └── Gadgets/                    (Dữ liệu ổ khóa, bao tải, cún canh nhà)  
└── Scenes/  
    └── MainGame.unity              (Scene duy nhất chứa toàn bộ Canvas UI)

* **Lưu trữ dữ liệu (Save System):**  
  * Sử dụng mã hóa JSON lưu trong PlayerPrefs hoặc file cục bộ thông qua Application.persistentDataPath.  
  * Hỗ trợ hàm xuất chuỗi Base64 (ExportSaveData) để người chơi tự sao lưu chéo dữ liệu nếu chơi trên bản WebGL.  
* **Tối ưu hóa hiệu năng (Optimization):**  
  * Sử dụng **Object Pooling** cho các đồng xu Plinko và các hiệu ứng số bay (FloatingText) để tránh tình trạng phân mảnh bộ nhớ (Garbage Collection) khi thả xu dồn dập.  
  * Canvas sử dụng cơ chế chia nhỏ thành nhiều Sub-Canvas (Header, Content, BottomBar) để tránh việc Canvas bị vẽ lại toàn bộ (Re-batching) mỗi khi các con số xu nhảy liên tục.