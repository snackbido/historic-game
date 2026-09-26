# PROGRESS.md — Tiến trình dự án

> Cập nhật file này sau mỗi buổi làm việc: đánh dấu việc đã xong, ghi chú vấn đề gặp phải, quyết định đã chốt.

## Trạng thái hiện tại
- **Giai đoạn**: Milestone 1-4 đã xác nhận chạy được trong Unity Editor thật (Milestone 5 chưa bắt đầu)
- **Cập nhật lần cuối**: 2026-09-25

## Milestone 0 — Setup môi trường
- [x] Cài Unity Hub
- [x] Cài Unity Editor (6000.6.2f1, tại `D:\jdk\cshape\unity\editor`) — khớp `ProjectSettings/ProjectVersion.txt`
- [ ] Cài VS Code + extension C# Dev Kit + extension Unity
- [ ] Set VS Code làm External Script Editor trong Unity Preferences
- [ ] Tạo project Unity mới (template 2D Core)
- [x] Tạo cấu trúc thư mục Assets/ theo ARCHITECTURE.md
- [x] Khởi tạo Git repo cho project, tạo `.gitignore` chuẩn cho Unity

## Milestone 1 — MVP Prototype (mục tiêu: 1-2 tuần)
- [x] Nhân vật di chuyển bằng WASD/Arrow keys (Rigidbody2D) — `PlayerController.cs`
- [x] Camera theo dõi nhân vật — `CameraFollow.cs`
- [x] 1 loại tài nguyên (gỗ) hiện trên map, nhân vật thu thập được khi tương tác — `ResourceNode.cs` + `PlayerInteraction.cs`
- [x] ResourceManager lưu số lượng tài nguyên, bắn event khi thay đổi — `ResourceManager.cs` + `EventBus.cs`
- [x] UI hiển thị số gỗ hiện có (ResourceBarUI) — `ResourceBarUI.cs`
- [x] Đặt được 1 loại công trình (lều) bằng click chuột, trừ tài nguyên khi đặt — `BuildingPlacer.cs` + `BuildingData.cs` + `BuildingInstance.cs`
- [x] Save/Load đơn giản (vị trí nhân vật + số tài nguyên) — `SaveSystem.cs` + `GameManager.cs` (mở rộng thêm để save cả building đã đặt)

**Ghi chú milestone này**:
- Script đã viết xong (2026-09-21). **Đã xác nhận chạy đúng trong Unity Editor thật (2026-09-25)**: di chuyển WASD + thu thập gỗ + đặt công trình (lều) đều hoạt động trong scene `Gameplay.unity`.
- Cố tình **không tạo `InputManager` riêng** ở Milestone 1: chỉ có 2 loại input (di chuyển, tương tác/đặt công trình), thêm tầng trừu tượng ngay bây giờ là over-engineering. Đọc input trực tiếp trong `PlayerController`/`PlayerInteraction`/`BuildingPlacer`. Sẽ tách ra khi input phức tạp hơn (combat, tech tree UI...).
- `ResourceType` giữ là enum (không phải ScriptableObject) vì MVP chỉ có 1 loại tài nguyên (gỗ) — đúng theo lựa chọn "enum/SO" đã ghi trong ARCHITECTURE.md. Đã chuyển sang ScriptableObject ở Milestone 2 (xem ghi chú M2).
- `BuildingPlacer.LoadFromSaveData` hiện chỉ khớp lại đúng 1 loại building đang gán trong Inspector (`buildingToPlace`) — đã refactor sang registry nhiều `BuildingData` ở Milestone 4 (xem ghi chú M4).
- Scene `Gameplay.unity` được dựng **bằng code** (không kéo-thả tay) qua `Assets/Editor/GameplaySceneBuilder.cs` + `Assets/Editor/GameContentBuilder.cs`, chạy được cả trong Editor (menu `Tools/Prehistoric/...`) lẫn batch mode (`-executeMethod ...BuildAll`) — xem chi tiết trong nhật ký phiên 2026-09-25 bên dưới.
- **Save/Load (F5 lưu / F9 tải qua `SaveLoadHotkeys.cs`) đã code xong nhưng chưa được xác nhận chạy đúng trong Play mode** — cần test ở phiên làm việc tiếp theo.

## Milestone 2 — Farming
- [x] Refactor `ResourceType` enum → `ResourceTypeData` (ScriptableObject) — `Data/ResourceTypeData.cs`, cập nhật `ResourceManager`/`ResourceNode`/`EventBus`/`ResourceBarUI`/`SaveSystem`
- [x] Định nghĩa CropData (ScriptableObject) — `Data/CropData.cs`
- [x] FarmPlot: đất có thể trồng, trạng thái (trống/đang lớn/sẵn sàng thu hoạch/héo) — `Farming/FarmPlot.cs`
- [x] Vòng đời cây trồng theo thời gian (state machine enum+switch: Seed → Sprouting → Mature → Withered nếu không thu hoạch kịp) — trong `FarmPlot.cs`
- [x] Thu hoạch → cộng vào ResourceManager — `FarmPlot.Harvest()` gọi thẳng `ResourceManager.Instance.AddResource`, cùng pattern với `ResourceNode.Harvest()`
- [x] UI: chọn loại cây để trồng — `FarmManager.cs` (giữ SelectedCrop) + `UI/CropSelectionUI.cs` (danh sách nút chọn giống)
- [x] Mở rộng `PlayerInteraction` để tương tác được cả `ResourceNode` lẫn `FarmPlot` bằng cùng 1 phím tương tác

**Ghi chú milestone này**:
- Chưa tạo `CropController` riêng như phác thảo ban đầu trong ARCHITECTURE.md — logic vòng đời cây gộp thẳng vào `FarmPlot` vì đơn giản, tránh tách lớp không cần thiết (mirror cách `ResourceNode` tự xử lý harvest).
- **Chưa test trong Unity Editor** (máy dev chưa cài Unity) — các bước thủ công cần làm khi cài xong: tạo asset `Wood.asset` (ResourceTypeData), gán vào `ResourceNode`/`ResourceBarUI`/`ResourceManager.knownResourceTypes`; tạo asset crop (VD `Wheat.asset`), scene `FarmPlot` prefab (SpriteRenderer + Collider2D), gán `CropSelectionUI.availableCrops` + `buttonPrefab`.
- Chưa làm save/load cho trạng thái `FarmPlot` (đất đã trồng gì, đang ở giai đoạn nào) — nằm ngoài phạm vi 5 việc gốc của Milestone 2, để dành xem xét sau khi test được trong Editor.

## Milestone 3 — Chăn nuôi
- [x] Định nghĩa AnimalData (ScriptableObject) — `Data/AnimalData.cs`
- [x] Vật nuôi có nhu cầu: đói, sinh sản — `Animals/AnimalController.cs` (chỉ tính đói/sinh sản khi đã thuần hóa; sinh sản dừng lại nếu đói dưới ngưỡng)
- [x] Cơ chế thuần hóa động vật hoang dã — cho ăn tốn tài nguyên qua `TamingSystem.cs`, tích lũy `TamingProgress` đến khi đạt `feedingsToTame`
- [x] Thu sản phẩm từ vật nuôi (thịt/da/sữa) — `AnimalController.CollectProduct()` cộng thẳng vào `ResourceManager`, sẵn sàng theo chu kỳ `productionInterval`
- [x] Mở rộng `PlayerInteraction` để tương tác được với `AnimalController` (cho ăn/thuần hóa/thu sản phẩm) bằng cùng 1 phím tương tác

**Ghi chú milestone này**:
- Chưa tách `AnimalNeeds` riêng như phác thảo ban đầu trong ARCHITECTURE.md — nhu cầu đói/sinh sản/sản xuất gộp vào `AnimalController` cho gọn, cùng tinh thần đã bỏ `CropController` ở Milestone 2.
- Sinh sản: vật nuôi tamed tự `Instantiate` một bản sao gần đó khi đủ điều kiện (không dùng EventBus/manager riêng vì chỉ 1 hành vi đơn giản, tương tự cách `ResourceNode` tự gọi thẳng `ResourceManager`).
- Một phím tương tác giờ xử lý cả 3 loại: `ResourceNode` (harvest), `FarmPlot` (trồng/thu hoạch/dọn héo), `AnimalController` (cho ăn/thuần hóa/thu sản phẩm) — logic chọn nearest interactable không đổi từ Milestone 2.
- **Chưa test trong Unity Editor** — việc thủ công cần làm khi cài xong Unity: tạo asset AnimalData (VD `WildBoar.asset`), gán `prefab` (dùng chính prefab con vật để spawn khi sinh sản), cấu hình `feedCost`/`products` tham chiếu đến `ResourceTypeData` đã tạo.
- Chưa làm save/load cho trạng thái vật nuôi (đã thuần hóa chưa, độ đói...) — ngoài phạm vi gốc của Milestone 3, để dành xem xét sau khi test được trong Editor (cùng nhóm với việc save FarmPlot còn nợ ở Milestone 2).

## Milestone 4 — Tech Tree / Progression
- [x] Định nghĩa TechNode (ScriptableObject), điều kiện mở khóa — `Data/TechNode.cs` (chi phí + `prerequisites` + danh sách building/crop id mở khóa)
- [x] TechManager quản lý trạng thái đã mở khóa — `Tech/TechManager.cs`, tự sinh tài nguyên "tri thức" theo thời gian (`knowledgeGenerationInterval`)
- [x] UI cây công nghệ — `UI/TechTreeUI.cs`
- [x] Kết nối tech mở khóa → building/farming mới — `BuildingData`/`CropData` có cờ `unlockedByDefault`; `TechManager.IsBuildingUnlocked/IsCropUnlocked` kiểm tra; `BuildMenuUI`/`CropSelectionUI` chỉ hiện lựa chọn đã mở khóa và tự rebuild khi có tech mới mở

**Ghi chú milestone này**:
- Refactor lớn kèm theo: `BuildingPlacer` chuyển từ 1 field `buildingToPlace` cố định sang registry `List<BuildingData>` + `SelectBuilding()` + singleton `Instance` (giống `FarmManager`/`TamingSystem`), để hỗ trợ nhiều loại công trình và tra cứu theo id khi `LoadFromSaveData` — xử lý luôn nợ kỹ thuật đã ghi từ Milestone 1.
- Tạo mới `UI/BuildMenuUI.cs` (trước đây chưa có, vì M1 chỉ có 1 loại công trình gán tay) — cùng cấu trúc với `CropSelectionUI`: liệt kê lựa chọn đã mở khóa, lắng nghe `EventBus.OnTechUnlocked` để tự cập nhật danh sách.
- Combat chưa tồn tại (Milestone 5 chưa làm) nên `TechNode` chỉ có danh sách unlock cho building/crop, chưa có unlock cho combat — sẽ thêm trường tương ứng khi làm Milestone 5.
- **Chưa test trong Unity Editor** — việc thủ công khi cài xong Unity: tạo asset `ResourceTypeData` "Tri thức" gán vào `TechManager.knowledgeResource`; tạo các asset `TechNode`; đánh dấu `unlockedByDefault = true` cho Tent.asset/Wheat.asset (công trình/cây khởi điểm không cần tech); gán danh sách building/crop cho `BuildMenuUI`/`CropSelectionUI`/`TechTreeUI` trong Inspector.

## Milestone 5 — Combat
- [ ] Quyết định chế độ chiến đấu: điều khiển trực tiếp hay chỉ huy nhóm (RTS nhẹ)
- [ ] HealthComponent dùng chung cho Player/NPC/Building
- [ ] Enemy AI cơ bản (patrol/chase/attack)
- [ ] Vũ khí cơ bản (giáo/đá ném)

## Milestone 6 — Thiên tai
- [ ] DisasterManager: hệ thống sự kiện ngẫu nhiên
- [ ] Base class DisasterEvent, tạo 1-2 loại thiên tai đầu tiên (VD: cháy rừng, lũ lụt)
- [ ] Cảnh báo trước khi thiên tai xảy ra
- [ ] Hiệu ứng ảnh hưởng lên Building/Resource/Animal/Farming

## Milestone 7 — Polish & mở rộng
- [ ] Âm thanh (SFX + nhạc nền)
- [ ] Hiệu ứng hình ảnh (particle cho thu hoạch, xây dựng, thiên tai)
- [ ] Cân bằng game (balance số liệu tài nguyên, thời gian, độ khó)
- [ ] Menu chính, màn hình cài đặt

## Nhật ký quyết định quan trọng (Decision Log)
> Ghi lại các quyết định kỹ thuật/thiết kế lớn để không quên lý do tại sao chọn hướng này.

| Ngày | Quyết định | Lý do |
|------|-----------|-------|
| 2026-09-21 | Chọn Unity + C#, IDE VS Code | Nhiều tài nguyên học, asset store phong phú cho thể loại survival/building; VS Code quen thuộc với nền web dev |
| 2026-09-21 | Bắt đầu bằng 2D top-down | Dễ quản lý hơn 3D khi làm một mình, chưa có kinh nghiệm Unity |
| 2026-09-21 | Đặt công trình theo grid-based (không free-placement) | Dễ quản lý va chạm/chồng lấn, dễ tính toán, phù hợp người mới học Unity |
| 2026-09-21 | Bỏ qua `InputManager` riêng ở Milestone 1 | Chỉ có 2 loại input ở MVP, thêm tầng trừu tượng ngay là over-engineering; sẽ tách khi cần |
| 2026-09-22 | Chuyển `ResourceType` từ enum sang ScriptableObject ở Milestone 2 | Farming sẽ thêm nhiều loại tài nguyên/cây trồng; đúng nguyên tắc data-driven trong ARCHITECTURE.md, tránh sửa code mỗi khi thêm loại tài nguyên mới |
| 2026-09-22 | Thuần hóa thú hoang ở Milestone 3: cho ăn (tốn tài nguyên) thay vì tương tác phím đơn thuần | Gần sát mô tả SPEC.md hơn, tái dùng pattern chi phí `ResourceAmount` đã có ở `BuildingData`, tránh thuần hóa quá dễ dàng |
| 2026-09-22 | Tài nguyên "tri thức" (Milestone 4) tự sinh theo thời gian (tốc độ cố định), không gắn vào hành động gameplay | Đơn giản nhất, không phụ thuộc EventBus của các hệ thống khác; dễ cân bằng lại tốc độ sau này |
| 2026-09-22 | Refactor `BuildingPlacer` từ 1 field `buildingToPlace` sang registry nhiều `BuildingData` + `SelectBuilding()` | Cần thiết để Tech Tree mở khóa được nhiều loại công trình khác nhau; đã ghi nợ từ Milestone 1 |

## Nhật ký phiên làm việc 2026-09-22 (đang bắt đầu test trong Editor thật)
- Cài xong Unity Editor 6000.6.2f1, mở project lần đầu → gặp lỗi `CS0246: Button could not be found` ở `TechTreeUI.cs`/`BuildMenuUI.cs`/`CropSelectionUI.cs`. Nguyên nhân: `Packages/manifest.json` thiếu package `com.unity.ugui` (namespace `UnityEngine.UI` tồn tại qua `com.unity.modules.ui` nhưng rỗng, không có `Button`/`Text`/`Image`). **Đã fix**: thêm `com.unity.ugui` vào manifest, Unity tự resolve về bản `2.6.0`. Build lại (`Assembly-CSharp.dll` compile sạch) — hết lỗi.
- Tạo scene `Assets/Scenes/testUI.unity` để test riêng 3 màn UI của Milestone 4 (`TechTreeUI`/`BuildMenuUI`/`CropSelectionUI`).
- Tạo sẵn bộ asset mẫu trong `Assets/_Data/` để kéo-thả thay vì tạo tay từng cái: `ResourceType_Wood/Knowledge/Food.asset`, `BuildingData_Hut.asset` (mở sẵn), `BuildingData_Storage.asset` (khóa), `CropData_Berry.asset` (khóa), `TechNode_Farming.asset` (tốn 5 Knowledge, mở `storage` + `berry`).
- **Gotcha phát hiện được khi test**: nếu để `TechTreeUI`/`BuildMenuUI`/`CropSelectionUI` dùng chung 1 `Transform` làm container (VD gắn cả 3 lên thẳng `Canvas`), nút của script chạy trước sẽ bị script chạy sau **xóa nhầm** — vì `BuildMenuUI.Rebuild()`/`CropSelectionUI.Rebuild()` đều `Destroy()` toàn bộ children của container trước khi thêm nút mới, không phân biệt nút đó của ai. Cách sửa: mỗi script phải có container riêng (3 child Transform riêng dưới Canvas, VD `TechPanel`/`BuildPanel`/`CropPanel`). Ghi nhớ áp dụng tương tự khi dựng HUD thật ở Gameplay scene sau này nếu có nhiều UI list cùng dùng chung 1 khu vực.
- Đã soạn (nhưng **chưa thực hiện**) hướng dẫn dựng scene placeholder `Assets/Scenes/Gameplay.unity`: Player (Square sprite + Rigidbody2D gravity=0 + PlayerController + PlayerInteraction), 1 `Tree` (ResourceNode + BoxCollider2D, dùng `ResourceType_Wood`), `ResourceManager` trong scene, `CameraFollow` gắn Main Camera target=Player. Mục đích: xác nhận vòng lặp di chuyển + thu thập tài nguyên chạy đúng trước khi đầu tư sprite/tileset thật. **Tạm dừng tại đây, chưa build/test.**

## Vấn đề đang tồn đọng (Known issues / Open questions)
- [ ] Chưa quyết định: chế độ combat (trực tiếp hay chỉ huy nhóm)?
- [ ] Chưa có tên chính thức cho dự án
- [ ] Chưa test Milestone 1 (di chuyển + thu thập tài nguyên) trong Unity Editor thật — đã soạn sẵn hướng dẫn dựng scene `Gameplay.unity` placeholder, chưa thực hiện (xem nhật ký phiên 2026-09-22 ở trên)
- [ ] Milestone 4 (Tech Tree UI) đã wiring xong trong `testUI.unity` với data mẫu, chưa xác nhận chạy đúng trong Play mode (chờ user báo kết quả sau khi tách container riêng)
