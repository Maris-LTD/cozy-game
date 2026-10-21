# Cozy Gachapon Room (Tiệm Trứng Thư Giãn) - Master Architecture

## Document Status
- Version: 1.0.0
- Last Updated: 2026-10-21
- Unity: 6000.3.16f1 (URP 2D, Input System: new)
- Platforms: WebGL, Android, iOS | Budgets: 60 fps, <=100 draw calls, <=1024 MB
- GDDs Covered: economy-core, pet-catalog, bootstrap-and-scene-flow, object-pooling, audio, plinko-board, inventory, gacha-machine, shelf-and-synergy, hud-and-tab-navigation, feedback-and-juice
- ADRs Referenced: ADR-0001 to ADR-0007 (Planned)
- Sign-offs:
  - TD-ARCHITECTURE: skipped (lean)
  - LP-FEASIBILITY: skipped (lean)

## Unity Knowledge Gap Summary
- **Unity 6000.3.16f1 (URP 2D)**:
  - **Physics2D**: Box2D chạy trên WebGL và Mobile cần trần số lượng vật thể động (active coins <= 48). Cơ chế `Simulate()` theo `FixedUpdate` được cô lập trong World Space Camera.
  - **Audio WebGL**: Trình duyệt yêu cầu thao tác người dùng đầu tiên (pointer up / touch) để mở khóa Web Audio context. Hệ thống Audio lắng nghe `FirstInteraction` để kích hoạt voice pool.
  - **UI / uGUI 2.0**: HUD 3 tab và header dùng Canvas Scaler 1080x1920 reference resolution, tách riêng Canvas tĩnh và Canvas động (số bay/hiệu ứng) để tránh Canvas Rebuild toàn phần.
  - **GC Allocations**: Tuyệt đối không dùng LINQ, string concatenation trong Update loop; tất cả số bay và xu Plinko sử dụng Object Pooling cấp sẵn trước khi vào màn chơi.

## Layer Map

```
┌──────────────────────────────────────────────────────────────────┐
│                          PRESENTATION                            │
│  - HUD & Tab Navigation (uGUI, Header, 3 Tabs, Modal Host)      │
│  - Feedback & Juice (DOTween, Floating Text View, FX View)       │
│  - Audio Service Implementation (AudioSource 16-Voice Pool)      │
└─────────────────────────────────┬────────────────────────────────┘
                                  │
┌─────────────────────────────────▼────────────────────────────────┐
│                            FEATURE                               │
│  - Gacha Machine (Drop pool resolution, reveal sequence)         │
│  - Inventory (Pet storage, duplicate tracking, star levels)      │
│  - Shelf & Synergy (Slot loadout, synergy calculation)           │
└─────────────────────────────────┬────────────────────────────────┘
                                  │
┌─────────────────────────────────▼────────────────────────────────┐
│                             CORE                                 │
│  - Economy Core (Wallet, Layered Income Pipeline, Cost Curves)   │
│  - Pet Catalog (Pet definitions, rarity matrices, role data)     │
│  - Plinko Board (2D Physics simulation, drop limiters, buckets)  │
└─────────────────────────────────┬────────────────────────────────┘
                                  │
┌─────────────────────────────────▼────────────────────────────────┐
│                          FOUNDATION                              │
│  - Bootstrap & Scene Flow (IBootStep phased lifecycle, router)   │
│  - Object Pooling (Zero-allocation fixed & recyclable pools)     │
│  - Audio Engine Contracts (SoundCue repository, bus routing)     │
└─────────────────────────────────┬────────────────────────────────┘
                                  │
┌─────────────────────────────────▼────────────────────────────────┐
│                          CONTRACTS                               │
│  - Common Interfaces, DTOs, Enums, Struct Event Channels         │
│  - Solves upward dependencies cleanly (IBaseCoinValueSource)     │
└──────────────────────────────────────────────────────────────────┘
```

## Module Ownership

### 1. Asmdef Definition Tables

| Module (asmdef) | Layer | Owns | Exposes | Consumes | Unity APIs / Packages |
|---|---|---|---|---|---|
| `CozyGachapon.Contracts` | Contracts | Shared Enums, DTOs, Event Structs | `IWallet`, `IIncomePipeline`, `IBaseCoinValueSource`, `IPoolService`, `IAudioService`, `IBootStep` | None | Pure C# |
| `CozyGachapon.Foundation` | Foundation | Boot sequence state, Object Pools, Voice Pool instances | `BootstrapController`, `ObjectPoolService`, `AudioService` | `Contracts` | `UnityEngine.CoreModule`, `UnityEngine.AudioModule` |
| `CozyGachapon.Core` | Core | Currency balance, modifiers, active coins, physics bounds | `Wallet`, `IncomePipeline`, `PetDatabase`, `PlinkoSimulation` | `Contracts` | `UnityEngine.Physics2DModule`, `UnityEngine.CoreModule` |
| `CozyGachapon.Features` | Feature | Owned pets, slot bindings, gacha state machine | `InventoryService`, `ShelfService`, `GachaMachineService` | `Contracts` | `UnityEngine.CoreModule` |
| `CozyGachapon.Presentation`| Presentation | View controllers, UI canvas binders, Tween animations | `HudView`, `TabView`, `FeedbackJuiceManager`, `RevealView` | `Contracts` | `UnityEngine.UI`, `com.unity.ugui`, `DOTween` |

### 2. Assembly Reference Diagram

```
[ CozyGachapon.Presentation ]
       │              │
       │              ▼
       │      [ CozyGachapon.Features ]
       │              │
       ▼              ▼
[ CozyGachapon.Core ]  │
       │              │
       ▼              ▼
[ CozyGachapon.Foundation ]
       │
       ▼
[ CozyGachapon.Contracts ]  <── (All modules reference Contracts)

[ CozyGachapon.Tests.EditMode ] ──> References All Runtime Asmdefs
```

## Data Flow and Initialisation

### 1. Initialisation Order (`IBootStep` Phased Sequence)
Bootstrap thực hiện vai trò Pure C# Composition Root, khởi tạo và truyền phụ thuộc tuần tự qua các pha:
1. **CoreInit**: Khởi tạo cấu hình, phát hiện nền tảng và thiết lập thông số hiển thị (khung hình 9:16 - 9:21).
2. **ServiceInit**: Khởi tạo `AudioService` (đăng ký bus, tạo 16 AudioSource), `ObjectPoolService` (khởi tạo bảng pool rỗng ở trạng thái `Accepting`).
3. **DataLoad**: Nạp `PetCatalog` từ ScriptableObject, nạp dữ liệu tiến trình cục bộ (PlayerPrefs/JSON ban đầu).
4. **GameplayInit**: Khởi tạo các domain logic: `Wallet`, `IncomePipeline`, `PlinkoSimulation`, `InventoryService`, `ShelfService`, `GachaMachineService`. Đăng ký các pool đối tượng (`plinko.coin`, `feedback.floatingText`, `feedback.coinFizzle`).
5. **Prewarm**: `ObjectPoolService` tạo sẵn toàn bộ đối tượng định mức cho tất cả các pool đã đăng ký (dưới rèm đen "Tiệm mở cửa"). Đạt trạng thái `Ready`.
6. **Ready**: Kích hoạt giao diện HUD, mở rèm và bắt đầu vòng lặp game chính.

### 2. Runtime Frame Flow
- **Input / Tap Path**: Người dùng chạm màn hình → `InputRouter` kiểm tra cờ chặn input → Gửi tọa độ tới `PlinkoSimulation.DropCoin(position)` → `PlinkoSimulation` kiểm tra rate limit / active coin cap → Lấy coin từ `ObjectPoolService.Get("plinko.coin")` → Đặt vị trí và kích hoạt Rigidbody2D.
- **Physics & Resolution Path**: `FixedUpdate` mô phỏng nảy qua chốt → Xu chạm chốt phát sự kiện `PegHit` → Xu rơi vào hũ nhân `bucketIndex` → Phát sự kiện `CoinResolved(value)` → `Wallet.Credit(value)` cộng xu vào ví → `ObjectPoolService.Release(coin)` thu hồi xu về pool → `FeedbackJuiceManager` hiển thị số bay và âm thanh tương ứng.
- **Idle Income Path**: `Update` đếm tích lũy thời gian `deltaTime` → `IncomePipeline` tính toán `totalCPS` theo công thức phân tầng → Cộng tích lũy vào `Wallet` → Gửi sự kiện `BalanceChanged` → `HudView` cập nhật số xu và nhịp CPS.

### 3. Save / Load Path
- **Trạng thái lưu trữ**: Số dư Xu (`Wallet`), số lượt quay Gacha `n`, danh sách thú đã sở hữu kèm số bản trùng (`Inventory`), cấu hình ô Kệ hiện tại (`Shelf`).
- **Cơ chế lưu**: Đăng ký serialization vào `SaveLoadService` khi có thay đổi quan trọng hoặc khi app tạm dừng (`Paused`).

## API Boundaries (Key Contracts)

### 1. Wallet & Income Pipeline
```csharp
namespace CozyGachapon.Contracts
{
    public interface IWallet
    {
        double Coins { get; }
        event Action<double> CoinsChanged;
        void Credit(double amount);
        bool TrySpend(double amount);
        bool CanAfford(double amount);
    }

    public interface IIncomePipeline
    {
        double CalculateCurrentCps();
        void RegisterModifier(IncomeModifier modifier);
        void UnregisterModifier(string modifierId);
        event Action ModifiersChanged;
    }
}
```

### 2. Object Pooling Service
```csharp
namespace CozyGachapon.Contracts
{
    public interface IPoolService
    {
        void RegisterPool(string poolId, GameObject prefab, int capacity, PoolExhaustionPolicy policy);
        PoolLease Get(string poolId);
        void Release(PoolLease lease);
    }
}
```

### 3. Audio Service
```csharp
namespace CozyGachapon.Contracts
{
    public interface IAudioService
    {
        void PlayCue(string cueId, AudioPlayParams playParams = default);
        void SetMuteReason(MuteReason reason, bool isMuted);
        void UnlockAudioContext();
    }
}
```

### 4. Plinko & Gacha Contracts
```csharp
namespace CozyGachapon.Contracts
{
    public interface IPlinkoBoard
    {
        bool TryDropCoin(float normalizedX);
        event Action<PegHitInfo> PegHit;
        event Action<CoinResolvedInfo> CoinResolved;
        event Action<int> JackpotHit;
    }

    public interface IGachaMachine
    {
        double CurrentCost { get; }
        bool TryPull(out GachaResult result);
        event Action<GachaResult> PullCompleted;
    }
}
```

## ADR Audit & Required ADRs

Dự án hiện chưa có ADR nào được ghi nhận. Toàn bộ 7 quyết định cốt lõi cần được lập thành tài liệu ADR:

| ADR ID | Title | Layer | Criticality | GDDs Covered |
|---|---|---|---|---|
| ADR-0001 | Layered Assembly Architecture & Module Boundaries | Foundation | High (Must exist before code) | All |
| ADR-0002 | Bootstrap Composition Root & Phased Lifecycle via IBootStep | Foundation | High (Must exist before code) | bootstrap-and-scene-flow |
| ADR-0003 | Zero-Allocation Object Pooling Service | Foundation | High (Must exist before code) | object-pooling, plinko-board |
| ADR-0004 | Priority Voice Management & Audio Subsystem | Foundation | High (Must exist before code) | audio |
| ADR-0005 | Layered Income Pipeline & Double Currency Format | Core | High (Must exist before code) | economy-core, shelf-and-synergy |
| ADR-0006 | World Space 2D Physics Plinko Board vs Canvas UI Separation | Core | High (Must exist before code) | plinko-board, hud-and-tab-navigation |
| ADR-0007 | Event-Driven Feedback & Juice Translation Pipeline | Presentation | Medium | feedback-and-juice |

## Architecture Principles
1. **Plain C# Core, Thin MonoBehaviours**: Logic nghiệp vụ (Kinh tế, Gacha, Bảng tính, Bể chứa dữ liệu) nằm trong các class C# thuần. MonoBehaviours chỉ đóng vai trò View, Router và adapter vòng đời Unity.
2. **Data-Driven via ScriptableObjects**: Toàn bộ chỉ số gốc của Thú cưng, cấu hình âm thanh `SoundCue`, bảng trọng số Gacha và công số cân bằng nằm trong ScriptableObjects, không hardcode giá trị vào mã nguồn.
3. **Strict Downward Dependency**: Phụ thuộc tuân thủ ranh giới biên dịch: Presentation → Feature → Core → Foundation → Contracts. Không chấp nhận phụ thuộc ngược hoặc phụ thuộc vòng.
4. **Zero Runtime GC Allocation**: Các hệ thống sinh/hủy liên tục (xu, số bay, hiệu ứng) bắt buộc mượn và trả qua `ObjectPoolService`. Cấm LINQ và string concatenation trong Update/FixedUpdate.

## Open Questions
1. **WebGL LocalStorage vs IndexedDB**: Cần quyết định cơ chế lưu trữ bền vững cho WebGL để tránh mất dữ liệu khi trình duyệt xóa cache (sẽ giải quyết bằng ADR Persistence ở giai đoạn Alpha).
2. **Platform SDK Wrappers**: Kiến trúc adapter cho cổng game Poki/CrazyGames và AdMob/LevelPlay sẽ được thiết kế dạng interface cắm rút trong module Platform.

## Technical Requirements Baseline Summary
Hệ thống quản lý 44 yêu cầu kỹ thuật (`TR-ID`) được trích xuất từ 11 GDD MVP, lưu trữ tại `Docs/architecture/tr-registry.yaml`.
- `TR-economy-001` đến `TR-economy-005` (5 yêu cầu)
- `TR-pet-001` đến `TR-pet-003` (3 yêu cầu)
- `TR-boot-001` đến `TR-boot-005` (5 yêu cầu)
- `TR-pooling-001` đến `TR-pooling-004` (4 yêu cầu)
- `TR-audio-001` đến `TR-audio-004` (4 yêu cầu)
- `TR-plinko-001` đến `TR-plinko-005` (5 yêu cầu)
- `TR-inventory-001` đến `TR-inventory-003` (3 yêu cầu)
- `TR-gacha-001` đến `TR-gacha-004` (4 yêu cầu)
- `TR-shelf-001` đến `TR-shelf-004` (4 yêu cầu)
- `TR-hud-001` đến `TR-hud-003` (3 yêu cầu)
- `TR-feedback-001` đến `TR-feedback-004` (4 yêu cầu)
