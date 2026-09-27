# ARCHITECTURE.md — Cấu trúc dự án & hệ thống

## 1. Cấu trúc thư mục Unity (Assets/)

```
Assets/
  _Scenes/
    MainMenu.unity
    Gameplay.unity
    (thêm scene test riêng khi cần, ví dụ TestBuilding.unity)

  _Scripts/
    Core/                → GameManager, SaveSystem, InputManager, EventBus
    Player/               → PlayerController, PlayerInteraction
    Building/             → BuildingPlacer, BuildingData, BuildingInstance
    Resources/            → ResourceManager, ResourceNode
    Farming/              → FarmPlot, FarmManager
    Animals/              → AnimalController, TamingSystem
    Tech/                 → TechManager
    Combat/               → HealthComponent, CombatController, EnemyAI
    Disaster/             → DisasterManager, DisasterEvent (base class)
    UI/                   → HUDController, BuildMenuUI, ResourceBarUI, CropSelectionUI, TechTreeUI
    Data/                 → chứa tất cả ScriptableObject definitions (BuildingData, ResourceTypeData, CropData, AnimalData, TechNode...)
    Utils/                → helper functions, extension methods

  Prefabs/
    Player/
    Buildings/
    Resources/
    Animals/
    Enemies/
    UI/

  Sprites/
    Characters/
    Buildings/
    Environment/
    UI/

  Animations/
  Audio/
    SFX/
    Music/

  ScriptableObjects/       → asset instances tạo ra từ các SO class trong Data/
    Resources/
    Buildings/
    Crops/
    Animals/
    Tech/
```

**Quy tắc đặt tên**: PascalCase cho tên file script và class (`PlayerController.cs`), camelCase cho biến/field private, PascalCase cho property public. Tên scene, prefab rõ nghĩa, không viết tắt khó hiểu.

## 2. Nguyên tắc kiến trúc tổng thể

### 2.1 Data-driven bằng ScriptableObject
Mọi "loại" dữ liệu lặp lại (loại cây, loại công trình, loại vật nuôi, loại tài nguyên, tech node) đều định nghĩa bằng `ScriptableObject`, KHÔNG hard-code trong script logic. Ví dụ:

```
Data/CropData.cs (class kế thừa ScriptableObject)
  → tạo asset "Wheat.asset", "Potato.asset" trong ScriptableObjects/Crops/
```

Lý do: thêm loại cây/nhà mới không cần sửa code, chỉ tạo asset mới — tương tự việc tách config/data ra khỏi logic trong web dev (giống file JSON config).

### 2.2 Tách biệt Data / Logic / Presentation
- **Data**: ScriptableObject, chỉ chứa số liệu (HP, thời gian lớn, chi phí xây...).
- **Logic**: MonoBehaviour hoặc plain C# class xử lý hành vi (di chuyển, tăng trưởng, va chạm).
- **Presentation**: Animator, SpriteRenderer, UI — chỉ hiển thị, không chứa logic nghiệp vụ.

### 2.3 Giao tiếp giữa các hệ thống: Event-driven
Dùng một `EventBus`/`GameEvent` đơn giản (UnityEvent hoặc C# event/Action) để các hệ thống không phụ thuộc cứng vào nhau. Ví dụ: khi `ResourceManager` hết gỗ → bắn event `OnResourceDepleted` → UI tự lắng nghe và cập nhật, không cần `ResourceManager` biết về UI.

Tương tự pub/sub hoặc custom event trong JS — nếu đã quen Redux/event emitter thì áp dụng tư duy tương tự ở đây.

### 2.4 Singleton có kiểm soát cho các Manager
Các class quản lý toàn cục (`GameManager`, `ResourceManager`, `TechManager`, `DisasterManager`) dùng Singleton pattern đơn giản qua Awake(), đặt trong scene dạng object riêng hoặc dùng `DontDestroyOnLoad` nếu cần giữ qua nhiều scene. Tránh lạm dụng singleton cho mọi thứ — chỉ dùng cho các hệ thống thực sự toàn cục.

### 2.5 State Machine cho hành vi phức tạp
Dùng cho: vòng đời cây trồng, trạng thái NPC (idle/working/fleeing), AI kẻ địch (patrol/chase/attack), trạng thái thiên tai (warning/active/aftermath). Giai đoạn đầu có thể code state machine đơn giản bằng enum + switch; nếu phức tạp dần có thể chuyển sang pattern State đầy đủ.

## 3. Sơ đồ phụ thuộc hệ thống (cấp cao)

```
GameManager (điều phối chung)
   │
   ├── ResourceManager  ──> UI (ResourceBarUI)
   ├── BuildingSystem   ──> ResourceManager (trừ tài nguyên khi xây)
   ├── FarmingSystem    ──> ResourceManager (sinh ra tài nguyên khi thu hoạch)
   ├── AnimalSystem     ──> ResourceManager (sinh ra sản phẩm)
   ├── TechManager      ──> mở khóa Building/Farming/Combat mới
   ├── CombatSystem     ──> HealthComponent trên Player/Enemy/Building
   └── DisasterManager  ──> ảnh hưởng Building, ResourceManager, Animal, Farming
```

## 4. Save/Load
- Dùng JSON (JsonUtility hoặc Newtonsoft.Json) để serialize trạng thái game: vị trí công trình, tài nguyên hiện có, trạng thái cây trồng, tech đã mở khóa.
- Lưu file vào `Application.persistentDataPath`.
- Thiết kế mỗi hệ thống có hàm `GetSaveData()` / `LoadFromSaveData()` riêng, `GameManager` gọi tổng hợp — tránh 1 class khổng lồ ôm hết save logic.

## 5. Ghi chú kỹ thuật khi mở rộng
- **Đã chuyển sang isometric (2026-09-27)**, đúng như dự tính: chỉ đổi tầng Presentation, không đổi Logic/Data. Cách làm cụ thể:
  - `Grid` (dùng cho `BuildingPlacer`) đổi `cellLayout` sang `GridLayout.CellLayout.Isometric`, `cellSize = (1, 0.5, 1)` (tỉ lệ 2:1 chuẩn) — Unity tự lo việc `WorldToCell`/`GetCellCenterWorld` chiếu đúng ô hình thoi, không cần sửa code `BuildingPlacer`.
  - `PlayerController` chuyển input WASD từ 2 trục vuông góc sang 2 trục chéo isometric qua `Utils/IsometricUtility.cs` (`InputToIsometric`) — mọi logic khác (va chạm, tương tác, khoảng cách) vẫn hoạt động bình thường vì toàn bộ thế giới vẫn là 1 không gian Unity 2D thống nhất, không có 2 hệ tọa độ song song.
  - Camera **không xoay** — vẫn nhìn thẳng xuống trục -Z như top-down cũ; hiệu ứng isometric đến từ hình dạng lưới + hướng di chuyển, không phải góc camera. Thêm `transparencySortMode = CustomAxis` + `transparencySortAxis = Vector3.up` để sprite tự xếp lớp đúng theo trục Y (vật ở "phía sau" vẽ trước).
  - `VillagerController`/`AnimalController` không cần sửa gì: chúng di chuyển bằng vector hướng tới target (`target - currentPos`), tự động đúng hướng bất kể không gian có "hình dạng" gì.
  - **Giới hạn hiện tại**: sprite vẫn là hình vuông placeholder (chưa có art isometric thật) — khi nhiều vật đặt gần nhau trên lưới hình thoi (cellSize nhỏ hơn 1x1) sprite sẽ chồng lên nhau về mặt hình ảnh dù vị trí logic đúng. Cần thay bằng sprite/art vẽ theo góc isometric thật ở Milestone 7 (Polish).
- Nếu sau này muốn chuyển tiếp sang 3D: vẫn giữ nguyên tầng Logic + Data, chỉ thay tầng Presentation (sprite → model). Đây là lý do vì sao phải tách 3 tầng rõ ràng ngay từ đầu.
- Nếu cần AI phức tạp hơn cho combat/thiên tai: cân nhắc Unity NavMesh cho pathfinding khi lên 3D, hoặc A* Pathfinding Project (asset phổ biến) cho 2D.