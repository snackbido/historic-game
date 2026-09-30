# PROGRESS.md — Tiến trình dự án

> Cập nhật file này sau mỗi buổi làm việc: đánh dấu việc đã xong, ghi chú vấn đề gặp phải, quyết định đã chốt.

## Trạng thái hiện tại
- **Giai đoạn**: Bản Unity đã chuyển sang **2.5D** (2026-09-30, nhánh `feat/unity-2.5d`): logic M1-4 giữ nguyên, 10/10 test PlayMode pass trên scene 3D mới. Còn thiếu xác nhận input/UI trực quan bằng người thật (Milestone 5 chưa bắt đầu). Có thêm bản web Three.js ở `web/` (nhánh `feat/web-threejs`)
- **Cập nhật lần cuối**: 2026-09-30

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
- **Đã xác nhận đúng logic qua test tự động (2026-09-26)**: `Assets/Tests/PlayMode/Milestone2FarmingTests.cs` chạy trong Unity Editor thật (batch mode, `-runTests -testPlatform PlayMode`) trên scene `Gameplay.unity` — xác nhận vòng đời Seed→Sprouting→Mature→Harvest cộng đúng tài nguyên và reset ô đất, cây héo đúng giờ nếu không thu hoạch kịp và dọn được, trồng thất bại khi chưa chọn giống. Chưa xác nhận phần input/UI trực quan (bấm phím tương tác thật, nhìn sprite đổi giai đoạn) — cần test tay.
- Chưa làm save/load cho trạng thái `FarmPlot` (đất đã trồng gì, đang ở giai đoạn nào) — nằm ngoài phạm vi 5 việc gốc của Milestone 2, để dành xem xét sau.

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
- **Đã xác nhận đúng logic qua test tự động (2026-09-26)**: `Assets/Tests/PlayMode/Milestone3AnimalTests.cs` — cho ăn đủ `feedingsToTame` lần thì thuần hóa thành công, sau đó đợi đúng `productionInterval` thì có sản phẩm để thu và cộng đúng tài nguyên, cho ăn thất bại khi không đủ tài nguyên. Chưa test sinh sản (Reproduce) và chưa xác nhận phần input thật (bấm phím cho ăn/thuần hóa ngoài Editor).
- Chưa làm save/load cho trạng thái vật nuôi (đã thuần hóa chưa, độ đói...) — ngoài phạm vi gốc của Milestone 3, để dành xem xét sau (cùng nhóm với việc save FarmPlot còn nợ ở Milestone 2).
- **Hiện tượng gây nhầm lẫn khi test tay (2026-09-26), không phải bug**: User báo "lần cho ăn đầu tiên thì tài nguyên tăng lên 2 thay vì giảm 3, từ lần 2 trở đi mới giảm 3 đúng". Điều tra trực tiếp qua Unity MCP (`RunCommand` đọc state live) phát hiện: con heo đã **sinh sản** ra `WildBoar(Clone)` nằm sát con gốc — con non sinh ra qua `InitializeAsTamed()` nên **đã Tamed sẵn**, không phải hoang dã. User tưởng đang "cho ăn lần đầu" một con hoang dã nhưng thực ra đang tương tác với con clone đã thuần *đúng lúc nó có sản phẩm sẵn sàng thu* → chạy nhánh `CollectProduct()` (+2 Food) thay vì `TryFeed()` (-3 Food) trong `TamingSystem.TryInteract()`. Logic code đúng như thiết kế, đây là lỗ hổng UX: không có phản hồi (log/UI) cho biết vừa xảy ra hành động gì (cho ăn/thuần hóa/thu hoạch), và tamed vs wild chỉ khác nhau qua tint màu nhẹ (`TamedColor`) nên dễ nhầm giữa 2 con giống hệt nhau đứng sát nhau.
  - **Đề xuất cải thiện (chưa làm)**: thêm log/thông báo ngắn khi tương tác với vật nuôi (VD "Đã cho ăn", "Đã thuần hóa!", "Thu hoạch: +2 Food") để người chơi luôn biết vừa xảy ra hành động gì.

## Milestone 4 — Tech Tree / Progression
- [x] Định nghĩa TechNode (ScriptableObject), điều kiện mở khóa — `Data/TechNode.cs` (chi phí + `prerequisites` + danh sách building/crop id mở khóa)
- [x] TechManager quản lý trạng thái đã mở khóa — `Tech/TechManager.cs`, tự sinh tài nguyên "tri thức" theo thời gian (`knowledgeGenerationInterval`)
- [x] UI cây công nghệ — `UI/TechTreeUI.cs`
- [x] Kết nối tech mở khóa → building/farming mới — `BuildingData`/`CropData` có cờ `unlockedByDefault`; `TechManager.IsBuildingUnlocked/IsCropUnlocked` kiểm tra; `BuildMenuUI`/`CropSelectionUI` chỉ hiện lựa chọn đã mở khóa và tự rebuild khi có tech mới mở

**Ghi chú milestone này**:
- Refactor lớn kèm theo: `BuildingPlacer` chuyển từ 1 field `buildingToPlace` cố định sang registry `List<BuildingData>` + `SelectBuilding()` + singleton `Instance` (giống `FarmManager`/`TamingSystem`), để hỗ trợ nhiều loại công trình và tra cứu theo id khi `LoadFromSaveData` — xử lý luôn nợ kỹ thuật đã ghi từ Milestone 1.
- Tạo mới `UI/BuildMenuUI.cs` (trước đây chưa có, vì M1 chỉ có 1 loại công trình gán tay) — cùng cấu trúc với `CropSelectionUI`: liệt kê lựa chọn đã mở khóa, lắng nghe `EventBus.OnTechUnlocked` để tự cập nhật danh sách.
- Combat chưa tồn tại (Milestone 5 chưa làm) nên `TechNode` chỉ có danh sách unlock cho building/crop, chưa có unlock cho combat — sẽ thêm trường tương ứng khi làm Milestone 5.
- **Đã xác nhận đúng logic qua test tự động (2026-09-26)**: `Assets/Tests/PlayMode/Milestone4TechTests.cs` — tri thức tự sinh theo thời gian, đủ tri thức thì mở khóa được `TechNode_Farming`, mở khóa đúng công trình (Storage) + cây trồng (Berry) liên kết, `BuildMenuUI`/`CropSelectionUI` tự rebuild và hiện thêm nút khi có tech mới mở, mở khóa thất bại khi chưa đủ tri thức. Chưa test nhấn nút thật trên UI (click chuột qua `EventSystem`) và chưa test prerequisites nhiều tầng (hiện chỉ có 1 `TechNode` không phụ thuộc tech khác).

## Milestone 5 — Combat
- [x] Quyết định chế độ chiến đấu (hướng chung, 2026-09-26): **lai** — vừa điều khiển trực tiếp nhân vật chính, vừa chỉ huy được nhóm NPC. Chưa chốt chi tiết: NPC nào chỉ huy được (dân làng đã có công việc? hay cần "lính" riêng?), cơ chế ra lệnh (click chọn + click ra lệnh kiểu RTS, hay đơn giản hơn như "theo tôi"/"tấn công mục tiêu này"), UI hiển thị nhóm đang chỉ huy — cần bàn kỹ trước khi code
- [x] HealthComponent dùng chung cho Player/NPC/Building — `Combat/HealthComponent.cs` (M5.1, 2026-09-30; thú sẽ gắn khi làm săn bắt/chiến đấu)
- [x] M5.1 NPC nền tảng: `ProfessionData` (4 nghề), `NpcController` (tên/giới tính/tuổi/nghề, đi bằng NavMesh, tự đi dạo quanh "nhà"), `NpcManager` (tra nghề, lưu/tải dân làng), 4 dân làng ban đầu (Ka ♂ dân làng, Mây ♀ nông dân, Đá ♂ thợ săn, Suối ♀ trinh sát)
- [ ] Enemy AI cơ bản (patrol/chase/attack)
- [ ] Vũ khí cơ bản (giáo/đá ném)
- [x] Chốt thiết kế chỉ huy (2026-09-30): **mọi NPC đều nghe lệnh**; **kéo khung chuột** để chọn NPC, mỗi NPC có **nghề nghiệp riêng** (trinh sát, thợ săn, nông dân, dân làng…) quyết định lệnh nó thực hiện được; UI hiển thị nhóm đang chọn **theo nhóm và theo nghề** của từng NPC
- [x] Chốt chi tiết (2026-09-30):
  - Nhóm lẫn nhiều nghề nhận lệnh mà có con không làm được → con làm được thì làm, con còn lại **đi theo tới chỗ đó**
  - Dân số: bắt đầu **3–4 NPC có cả nam lẫn nữ**; một cặp nam + nữ trưởng thành **sinh em bé**, em bé **lớn dần theo thời gian** (em bé → trẻ em → người lớn), chỉ người lớn mới làm việc/nhận lệnh. Chi tiết tự đề xuất (chỉnh được): cặp đôi cố định; sinh con khi còn chỗ ở (mỗi lều +2 chỗ) và đủ thức ăn (tốn thức ăn); thời gian lớn lên cấu hình trong data
  - Người mới trưởng thành là "Dân làng", người chơi **đổi nghề qua UI**
  - Khi không có lệnh, NPC **tự làm việc theo nghề** ở gần (vd nông dân tự thu hoạch ô đã chín)
- [x] M5.2 Chọn + ra lệnh di chuyển (2026-09-30): `Npc/SelectionManager.cs` — kéo khung/click chọn (Shift thêm/bỏ), chuột phải đi tới (dàn lưới, không chồng nhau), Ctrl+1..9 lưu nhóm / 1..9 gọi nhóm, vòng chọn dưới chân; NPC đã nhận lệnh đứng giữ vị trí (lưu trong save)
- [x] M5.3 Ra lệnh theo nghề (2026-09-30): chuột phải lên cây → chặt (dân làng/nông dân/thợ săn); ô đất → làm ruộng liên tục (nông dân); thú hoang → săn (thợ săn) hoặc thuần hóa (nông dân); thú thuần → chăm (nông dân); ai không làm được thì đi theo
- [x] M5.4 UI nhóm/nghề + đổi nghề (2026-09-30): bảng "Đang chọn" (tên + giới tính, ô theo nghề bấm để lọc, nút đổi nghề cho cả nhóm), thanh máu trên đầu NPC/thú
- [ ] Dân số: giới tính, cặp đôi, sinh con, lớn lên theo thời gian, sức chứa theo số lều

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
| 2026-09-26 | Thứ tự triển khai tiếp theo: trả nợ kỹ thuật (feedback UX vật nuôi, save/load FarmPlot + Animal, test tay input) **trước** khi bắt đầu Milestone 5 | Đảm bảo M1-4 vững chắc, không cộng dồn nợ kỹ thuật trước khi mở rộng sang hệ thống mới |
| 2026-09-30 | M5 chỉ huy NPC: (1) tất cả NPC đều nghe lệnh (không có "lính" riêng); (2) kéo khung chuột để chọn nhiều NPC, mỗi NPC có nghề (trinh sát, thợ săn, nông dân, dân làng…) quyết định việc nó làm khi nhận lệnh; (3) UI hiển thị các NPC đang chọn theo nhóm và theo nghề | Chốt với user; nghề nghiệp gắn chỉ huy với hệ thống kinh tế (nông dân trồng trọt, thợ săn săn bắt) thay vì tách riêng quân đội |
| 2026-09-30 | Chuyển bản Unity từ 2D top-down sang **2.5D**: model 3D low-poly + camera phối cảnh nghiêng cố định, gameplay vẫn trên mặt phẳng đất (XZ), vẫn đặt công trình theo grid | Không cần họa sĩ (dùng model miễn phí đồng phong cách), khớp game tham khảo (Banished/Frostpunk), nhẹ cho máy 8GB/mobile; bản web Three.js đã chứng minh hướng này. Loại 3D đầy đủ (camera tự do, địa hình) vì nhân khối lượng việc |
| 2026-09-26 | Milestone 5 (Combat): chế độ **lai** — vừa điều khiển trực tiếp nhân vật chính, vừa chỉ huy được nhóm NPC | User muốn cả hai, khác với 2 lựa chọn thuần trong SPEC.md § 3.5; cần thiết kế chi tiết thêm trước khi code (NPC nào chỉ huy được, cơ chế ra lệnh ra sao) — **chưa chốt chi tiết, chỉ mới chốt hướng chung** |

## Nhật ký phiên làm việc 2026-09-22 (đang bắt đầu test trong Editor thật)
- Cài xong Unity Editor 6000.6.2f1, mở project lần đầu → gặp lỗi `CS0246: Button could not be found` ở `TechTreeUI.cs`/`BuildMenuUI.cs`/`CropSelectionUI.cs`. Nguyên nhân: `Packages/manifest.json` thiếu package `com.unity.ugui` (namespace `UnityEngine.UI` tồn tại qua `com.unity.modules.ui` nhưng rỗng, không có `Button`/`Text`/`Image`). **Đã fix**: thêm `com.unity.ugui` vào manifest, Unity tự resolve về bản `2.6.0`. Build lại (`Assembly-CSharp.dll` compile sạch) — hết lỗi.
- Tạo scene `Assets/Scenes/testUI.unity` để test riêng 3 màn UI của Milestone 4 (`TechTreeUI`/`BuildMenuUI`/`CropSelectionUI`).
- Tạo sẵn bộ asset mẫu trong `Assets/_Data/` để kéo-thả thay vì tạo tay từng cái: `ResourceType_Wood/Knowledge/Food.asset`, `BuildingData_Hut.asset` (mở sẵn), `BuildingData_Storage.asset` (khóa), `CropData_Berry.asset` (khóa), `TechNode_Farming.asset` (tốn 5 Knowledge, mở `storage` + `berry`).
- **Gotcha phát hiện được khi test**: nếu để `TechTreeUI`/`BuildMenuUI`/`CropSelectionUI` dùng chung 1 `Transform` làm container (VD gắn cả 3 lên thẳng `Canvas`), nút của script chạy trước sẽ bị script chạy sau **xóa nhầm** — vì `BuildMenuUI.Rebuild()`/`CropSelectionUI.Rebuild()` đều `Destroy()` toàn bộ children của container trước khi thêm nút mới, không phân biệt nút đó của ai. Cách sửa: mỗi script phải có container riêng (3 child Transform riêng dưới Canvas, VD `TechPanel`/`BuildPanel`/`CropPanel`). Ghi nhớ áp dụng tương tự khi dựng HUD thật ở Gameplay scene sau này nếu có nhiều UI list cùng dùng chung 1 khu vực.
- Đã soạn (nhưng **chưa thực hiện**) hướng dẫn dựng scene placeholder `Assets/Scenes/Gameplay.unity`: Player (Square sprite + Rigidbody2D gravity=0 + PlayerController + PlayerInteraction), 1 `Tree` (ResourceNode + BoxCollider2D, dùng `ResourceType_Wood`), `ResourceManager` trong scene, `CameraFollow` gắn Main Camera target=Player. Mục đích: xác nhận vòng lặp di chuyển + thu thập tài nguyên chạy đúng trước khi đầu tư sprite/tileset thật. **Tạm dừng tại đây, chưa build/test.**

## Nhật ký phiên làm việc 2026-09-26 (test tự động Milestone 2-4)
- Không có công cụ điều khiển GUI Unity Editor trực tiếp, nên thay vì test tay, đã viết bộ **PlayMode test tự động** (`Assets/Tests/PlayMode/`) chạy thật trong Unity Editor ở chế độ batch/headless (`Unity.exe -batchmode -nographics -runTests -testPlatform PlayMode`), load scene `Gameplay.unity` thật (đã được `GameplaySceneBuilder`/`GameContentBuilder` dựng sẵn với đủ FarmManager/TamingSystem/TechManager/2 FarmPlot/WildBoar/UI panel), tăng `Time.timeScale` để không phải chờ thật các khung thời gian (`timeToMature`, `productionInterval`...).
- Kết quả: **8/8 test pass** — xác nhận đúng logic Farming (M2), Chăn nuôi (M3), Tech Tree (M4) như ghi ở từng milestone phía trên.
- Cần tách `Assets/_Scripts/` ra `PrehistoricTribe.asmdef` riêng để test assembly có thể tham chiếu được (assembly ngầm định `Assembly-CSharp` không thể là dependency của asmdef khác).
- **Gotcha phát hiện được**: asmdef của test PlayMode nếu đặt `includePlatforms: ["Editor"]` hoặc tham chiếu `UnityEditor.TestRunner`, Unity sẽ **âm thầm xếp nhầm nó vào EditMode** (test-runner tìm ra 0 test khi chạy `-testPlatform PlayMode`, dù compile không lỗi). Cách đúng: để `includePlatforms: []` (không giới hạn platform) và dùng `#if UNITY_EDITOR` để bọc các API `UnityEditor`/`AssetDatabase` cần dùng trong test.
- **Giới hạn của cách test này**: chỉ xác nhận đúng logic gameplay (state machine, cộng/trừ tài nguyên, mở khóa, rebuild UI) chạy trong Play Mode thật — **không** xác nhận được: bấm phím tương tác thật qua `Input`/`PlayerInteraction`, đặt công trình bằng chuột qua `BuildingPlacer`, sprite hiển thị đúng trên màn hình, layout UI nhìn có ổn không. Những phần này vẫn cần người dùng tự mở Editor, bấm Play, và quan sát bằng mắt.

## Nhật ký phiên làm việc 2026-09-26 (phần 2 — Unity MCP, sự cố pagefile, test Save/Load)
- Cài package `com.unity.ai.assistant` (Unity AI/MCP bridge) cho phép điều khiển Editor trực tiếp từ ngoài: Play/Stop, chụp ảnh camera, đọc Console, chạy C# tùy ý qua `RunCommand`. Rất hữu ích để tự động hóa việc test thay vì phải nhờ người dùng thao tác tay.
- **Sự cố gặp phải**: sau khi cài `com.unity.ai.assistant`, Package Manager tự thêm kèm `com.unity.ai.inference` (engine ML/Sentis, không phải dependency thật của `ai.assistant`) khiến project phải IL-postprocess tới **1043 assembly** khi compile — quá nặng cho máy chỉ có 8GB RAM + ổ C gần đầy (2.3GB trống). Compile luôn fail ở bước ILPP với lỗi Windows `GetLastError 1455: The paging file is too small`, khiến Unity chặn Play mode dù code không có lỗi C#.
- **Đã fix**: dọn ổ C (Recycle Bin + temp + installer cũ trong Downloads, +1.27GB) không đủ; gốc rễ thật sự là gỡ `com.unity.ai.inference` (xác nhận qua `package.json` là không phải dependency bắt buộc của `ai.assistant`) — sau khi gỡ, compile chạy sạch (`EditorUtility.scriptCompilationFailed = False`).
- **Đã xác nhận thêm qua Unity MCP (Play mode thật, không phải batch mode)**: Console sạch, không lỗi/exception khi chạy scene `Gameplay.unity`. Chụp ảnh camera xác nhận vị trí sprite Player/Tree/FarmPlot/WildBoar đúng như code dựng scene — nhưng công cụ chụp ảnh qua Camera không hiển thị được UI Canvas (ScreenSpaceOverlay), nên vẫn chưa xác nhận trực quan được layout UI.
- **Lưu ý polish nhỏ phát hiện được**: Camera dùng `clearFlags` mặc định (Skybox) nên nền có gradient bầu trời/đường chân trời phía sau sprite 2D — nhìn không hợp với game top-down 2D, nên đổi sang Solid Color khi làm polish (Milestone 7).
- **Save/Load Milestone 1 đã xác nhận chạy đúng trong Play mode thật**: gọi `GameManager.Instance.SaveGame()`/`LoadGame()` trực tiếp qua RunCommand — lưu vị trí player + số gỗ, đổi state, tải lại, khôi phục đúng chính xác cả vị trí lẫn tài nguyên.
- Package `com.unity.ai.assistant` hiện có trong `Packages/manifest.json` nhưng **chưa commit** — cần hỏi ý kiến trước khi đưa gói dev-tool này vào repo chung (có thể muốn giữ máy ai nấy cài, không ép vào git).

## Nhật ký phiên làm việc 2026-09-26 (phần 3 — feedback tương tác vật nuôi)
- Việc #1 của kế hoạch trả nợ kỹ thuật: thêm phản hồi (notification) khi tương tác với vật nuôi, để tránh lặp lại nhầm lẫn "heo rừng sinh sản" đã gặp trước đó.
- Thêm `EventBus.OnNotification`/`RaiseNotification(string)`, `TamingSystem.cs` phát thông báo tại 4 nhánh: không đủ tài nguyên, đã cho ăn (kèm tiến độ thuần hóa X/Y), đã thuần hóa thành công, thu hoạch sản phẩm (kèm số lượng). Thêm `UI/NotificationUI.cs` (label TMP đơn giản, tự ẩn sau `displayDuration`) + gắn vào `Gameplay.unity` qua `GameplaySceneBuilder.CreateNotificationLabel()`.
- **Đã xác nhận trực tiếp qua Unity MCP (Play mode thật)** 4/5 thông báo: "Không đủ tài nguyên...", "Đã cho ăn (X/Y)", "Đã thuần hóa...!", "Đã cho ăn" (refill khi đã thuần) — tất cả hiển thị đúng nội dung trên `NotificationLabel`.
- Thông báo còn lại (thu hoạch sản phẩm) **chưa xác nhận trực tiếp**: phát hiện cửa sổ Unity Editor bị throttle framerate rất thấp khi không có focus (`Time.time` gần như đứng yên dù đã đợi thật), nên không chờ được `productionInterval` trôi qua trong phiên Play mode sống. Thử dùng `TestRunnerApi` chạy PlayMode test ngay trong phiên Editor đang mở (để né việc phải mở process Unity thứ 2, vốn bị khóa vì Editor GUI đang mở project) nhưng callback bị mất qua domain reload khi vào Play mode, không lấy được kết quả.
- Đã bổ sung assertion cho cả 5 thông báo vào `Assets/Tests/PlayMode/Milestone3AnimalTests.cs` (dùng `StringAssert.Contains` trên `EventBus.OnNotification`) — **chưa chạy lại được bằng batch mode** vì Editor GUI đang mở khóa project (Unity không cho 2 instance mở cùng 1 project). Cần chạy `Assets/Tests/PlayMode` lại (đóng Editor GUI trước, hoặc dùng Test Runner window trong Editor) ở phiên sau để xác nhận nốt nhánh thu hoạch.

## Nhật ký phiên làm việc 2026-09-29 (bản Web bằng Three.js)
- Tạo thư mục `web/` (Vite + Three.js, JS thuần): port Milestone 1–4 lên trình duyệt với cùng số liệu data (`web/src/data/gameData.js` ≈ các asset trong `Assets/_Data/`), giữ kiến trúc Data / Logic / Presentation + EventBus. Logic chạy trên mặt phẳng 2D như bản Unity, tầng render là 3D low-poly (camera nghiêng, đổ bóng).
- Save/load (F5/F9, localStorage) lưu đủ cả FarmPlot, vật nuôi, cây còn lại, tech — tức bản web đã trả nợ kỹ thuật save FarmPlot/Animal mà bản Unity còn thiếu. Thêm các phản hồi UX: vòng sáng dưới đối tượng gần nhất, dòng gợi ý hành động, chữ bay "+1 Gỗ", vòng cổ cho con đã thuần, biểu tượng khi có sản phẩm, thông báo khi vật nuôi sinh con.
- **Đã xác nhận**: `npm run build` sạch; smoke test logic headless bằng Node 25/25 đạt (di chuyển, chặt cây, đặt lều + va chạm, tri thức + mở khóa, vòng đời cây/héo, thuần hóa/sản phẩm/sinh sản, save/load); chụp màn hình Chrome headless xác nhận scene + HUD hiển thị đúng. **Chưa xác nhận**: người thật chơi bằng bàn phím/chuột trong trình duyệt.

## Nhật ký phiên làm việc 2026-09-30 (chuyển bản Unity sang 2.5D)
- Chốt hướng 2.5D (xem Decision Log). Chuyển toàn bộ tầng vật lý/hiển thị sang 3D, logic gameplay (ResourceManager, TechManager, TamingSystem, FarmManager, EventBus, UI, ScriptableObject) không đổi:
  - `PlayerController`: `Rigidbody` (khóa xoay + khóa trục Y), đi trên mặt XZ, model con `visual` xoay theo hướng đi. `CameraFollow`: camera phối cảnh nghiêng 52°, bám theo, zoom bằng con lăn.
  - Mới: `Core/InteractableRegistry.cs` — `ResourceNode`/`FarmPlot`/`AnimalController` tự đăng ký; `PlayerInteraction` tìm đối tượng gần nhất bằng khoảng cách trên mặt đất thay vì `Physics2D.OverlapCircleAll` → tương tác không phụ thuộc vật lý 2D/3D (đúng tinh thần ARCHITECTURE § 5). Cây/ô đất/heo không cần collider nữa.
  - `BuildingPlacer`: raycast chuột xuống mặt phẳng đất, `Grid` dùng `cellSwizzle = XZY`, preview đổi xanh/đỏ theo việc đặt được hay không.
  - `CropData`: 4 field `Sprite` → 4 prefab model (`seedModel`…); `FarmPlot` sinh model dưới `cropAnchor`. `AnimalController`: tô màu qua material + `tamedIndicator` (vòng cổ) và `productIndicator` (biểu tượng vàng) — xử lý luôn lỗ hổng UX "con thuần/con hoang giống nhau" ở M3.
  - Save: `playerY` → `playerZ` (save cũ của bản 2D nạp về z = 0, chấp nhận vì chưa phát hành).
  - Editor: mới `EditorBuildUtils.cs` (primitive, material/mesh nón lưu thành asset ở `Assets/Materials/Generated`, `Assets/Models/Generated`). `GameContentBuilder`/`GameplaySceneBuilder` viết lại để dựng model low-poly (lều, kho, cây, heo, 4 giai đoạn cây mọng, rừng trang trí) và **hỏi trước khi ghi đè** prefab/scene đã có (trước đây ghi đè im lặng → mất phần chỉnh tay).
- **Đã xác nhận**: compile sạch; batch `BuildAll` dựng được content + scene; **10/10 test PlayMode pass** (8 test cũ + 2 test mới `Milestone1InteractionTests.cs` cho registry trên mặt XZ); render camera ra ảnh xác nhận scene 3D hiển thị đúng (nhân vật, cây, lều, kho, 2 giai đoạn cây, heo có vòng cổ + biểu tượng sản phẩm).
- **Chưa xác nhận**: người thật bấm WASD/E/chuột trong Play mode (cảm giác di chuyển, va chạm với công trình, đặt công trình bằng chuột, zoom).
- **Sự cố**: 2 lần Unity batch crash do thiếu bộ nhớ ảo (xem Known issues) — chạy lại được sau khi giải phóng bộ nhớ.

## Nhật ký phiên làm việc 2026-09-30 (phần 2 — trả nợ kỹ thuật trước M5)
- Chốt chi tiết thiết kế M5 với user (xem mục Milestone 5 + Decision Log): mọi NPC nghe lệnh, kéo khung chọn, nghề nghiệp, dân số nam/nữ sinh con lớn dần.
- **Save/Load mở rộng** (`SaveData.saveVersion = 2`): lưu thêm tech đã mở khóa, trạng thái từng `FarmPlot` (khớp theo tên object) và toàn bộ vật nuôi (xóa hết rồi tạo lại từ `AnimalData.prefab`, gồm cả con sinh sản ra). Save cũ (version 0/1) chỉ nạp player/tài nguyên/công trình, không đụng ô đất/vật nuôi. Thêm registry tra theo id: `FarmManager.knownCrops`, `TamingSystem.knownAnimals`, `TechManager.allTechs`. Mới `EventBus.OnGameLoaded` → `BuildMenuUI`/`CropSelectionUI`/`TechTreeUI` tự làm mới sau khi tải. F5/F9 giờ có thông báo "Đã lưu/Đã tải game".
- **Gợi ý tương tác**: `InteractionHighlight` (vòng sáng nhấp nháy dưới đối tượng gần nhất) + `InteractionPromptUI` (dòng chữ dưới màn hình, vd "[E] Cho Heo rung hoang ăn (-3 Thuc an) — thuần hóa 1/2"); ẩn khi đang đặt công trình (`BuildingPlacer.IsPlacing`).
- Test: `SaveSystem.FileNameOverride` để test không ghi đè save thật. Mới `SaveLoadTests.cs` (save → làm lệch trạng thái → load → khôi phục đúng tech/ô đất/heo đã thuần, không nhân đôi vật nuôi; nội dung gợi ý). **12/12 test PlayMode pass**. Ảnh render xác nhận vòng highlight hiển thị đúng; sửa thêm: vòng cổ/biểu tượng của heo tắt sẵn trong prefab.
- **Chưa xác nhận**: dòng gợi ý trên Canvas (công cụ chụp camera không thấy Canvas overlay) và cảm giác chơi thật — cần user bấm Play.

## Nhật ký phiên làm việc 2026-09-30 (phần 3 — M5.1 NPC nền tảng)
- Cài package `com.unity.ai.navigation` 2.0.14 (đi kèm Editor, không cần mạng). `GameplaySceneBuilder` bake `NavMeshSurface` trên `Environment` (mặt đất + rừng trang trí), lưu `Assets/_Scenes/Gameplay_NavMesh.asset`. Cây thu hoạch được và công trình dùng `NavMeshObstacle` (carve) vì chúng mất đi/xuất hiện lúc chơi.
- Mới: `Data/ProfessionData.cs` (+ enum cờ `NpcCapability`), 4 asset `Assets/_Data/Profession_*.asset` (Dân làng / Nông dân / Thợ săn / Trinh sát: khác tốc độ, máu, sát thương, tầm đánh, tầm nhìn, việc làm được); `Combat/HealthComponent.cs` (gắn cho Player, NPC, Hut, Storage); `Npc/NpcController.cs` (giới tính, `AgeStage` Baby/Child/Adult — scale nhỏ lại theo tuổi, nghề → màu áo qua `MaterialPropertyBlock` + dụng cụ cầm tay; `MoveTo()`; tự đi dạo khi rảnh); `Npc/NpcManager.cs` (lưu/tải dân làng — `saveVersion` 3); prefab `Assets/Prefabs/Npcs/Villager.prefab` (tóc nam/nữ, rìu/cuốc/giáo/băng lông vũ).
- Mới: `Assets/Editor/TestTools/TestResultReporter.cs` (asmdef riêng tham chiếu Test Runner) — ghi kết quả lần chạy test gần nhất ra `Logs/LastTestResults.xml/.txt`, đăng ký lại sau mỗi domain reload → **chạy được PlayMode test ngay trong Editor đang mở** (qua Unity MCP) mà vẫn đọc được kết quả, không phải đóng Editor để chạy batch. `EditorBuildUtils.AssumeYes` cho phép lệnh tự động bỏ qua hộp thoại xác nhận ghi đè.
- Bẫy gặp phải khi dựng scene bằng code: (1) `PrefabUtility.InstantiatePrefab(component)` trả về null — phải truyền GameObject; (2) tạo asset (NavMesh) giữa chừng làm tham chiếu prefab đã nạp trước đó mất hiệu lực — nạp lại ngay trước khi dùng; (3) bật/tắt object con của prefab instance trong Editor phải `RecordPrefabInstancePropertyModifications` cho từng object con, nếu không scene chỉ lưu giá trị gốc của prefab (dân làng hiện đủ 4 dụng cụ trong Scene view).
- **Đã xác nhận**: **16/16 test PlayMode pass** (4 test mới `Milestone5NpcTests.cs`: 4 dân làng có cả nam/nữ và mỗi người một nghề, đứng trên NavMesh, tốc độ/máu theo nghề; `MoveTo` đi tới đích; `HealthComponent` chết đúng 1 lần; lưu/tải giữ nghề + giới tính, không nhân đôi); ảnh render trong Play mode xác nhận màu áo/dụng cụ/tóc đúng theo nghề và giới tính; Console sạch.
- Chưa làm (các bước M5 sau): chọn NPC bằng kéo khung + ra lệnh (M5.2), việc theo nghề (M5.3), UI nhóm/nghề + đổi nghề (M5.4), thú dữ + chiến đấu (M5.5), tự làm việc khi rảnh + dân số sinh con (M5.6).

## Nhật ký phiên làm việc 2026-09-30 (phần 4 — M5.2 chọn NPC + ra lệnh)
- Mới `Npc/SelectionManager.cs`: chuột trái kéo khung (vẽ bằng `OnGUI`) hoặc click (chọn NPC gần con trỏ nhất trong 40px trên màn hình, không cần collider) — Shift để thêm/bỏ; click chỗ trống bỏ chọn; chuột phải lên mặt đất → cả nhóm `MoveTo` theo lưới cách nhau 0.9m; Ctrl+1..9 / 1..9 lưu/gọi nhóm. Chỉ người lớn chọn được. NPC bị xóa tự rời khỏi nhóm/đang chọn.
- Nhường chuột cho đặt công trình: bỏ qua khi `BuildingPlacer.IsPlacing` hoặc `UsedMouseThisFrame` (chuột phải vừa hủy đặt công trình trong cùng frame → không ra lệnh nhầm).
- `NpcController`: `SetSelected` (vòng xanh `SelectionRing` trong prefab), `holdPosition` — sau lệnh của người chơi thì đứng giữ vị trí, không đi dạo (lưu trong `NpcSaveData`). Mới `EventBus.OnSelectionChanged` (chuẩn bị cho UI nhóm/nghề M5.4).
- **Đã xác nhận**: **21/21 test PlayMode pass** (5 test mới `Milestone5SelectionTests.cs`: kéo khung chọn đủ người lớn + hiện vòng; click/Shift-click/click trống; lệnh đi tới dàn đội hình không chồng nhau rồi giữ vị trí; lưu/gọi nhóm + NPC bị xóa rời nhóm; đội hình cân giữa); ảnh render Play mode xác nhận vòng chọn; Console sạch. Toàn bộ chạy trong Editor đang mở qua Unity MCP (`TestResultReporter`).
- **Chưa xác nhận**: kéo khung/click bằng chuột thật (cảm giác, khung vẽ `OnGUI`), phím Ctrl+số khi Game view có focus.

## Nhật ký phiên làm việc 2026-09-30 (phần 5 — M5.3 việc theo nghề)
- Mới `Npc/NpcJobs.cs`: lớp trừu tượng `NpcJob` (Target, WorkRange, Interval, `DoWork` trả false khi xong) + `NpcJobFactory.Create(npc, target)` chọn việc theo `NpcCapability` của nghề (null = đi theo). 4 việc: `GatherJob` (1 gỗ / 1,5s tới khi cây hết), `FarmJob` (vòng lặp gieo hạt đang chọn → chờ → thu hoạch → gieo lại, dọn cây héo; chưa chọn hạt thì báo và dừng), `HuntJob` (đánh trong tầm `attackRange` của nghề — thợ săn 5m, 14 sát thương), `TendAnimalJob` (qua `TamingSystem`: thú hoang cho ăn tới khi thuần, thú thuần làm 1 lần).
- `NpcController`: trạng thái `Working` — đi tới mục tiêu (cập nhật đường 0,5s/lần vì thú có thể di chuyển), vào tầm thì quay mặt về mục tiêu và làm theo nhịp; xong việc đứng giữ vị trí; `MoveTo` hủy việc đang làm. Việc đang làm **chưa được lưu** trong save (tải lại thì NPC đứng yên).
- `SelectionManager.IssueCommandAt`: chuột phải trúng đối tượng trong 0,8m → giao việc từng người, người không làm được đi theo đứng quanh đối tượng; thông báo tóm tắt ("3 người đi chặt cây · 1 người đi theo"). Mới `SetSelection()` (dùng cho lọc theo nghề ở M5.4).
- Săn bắt: `AnimalData.huntYield` (heo rừng: +6 thức ăn), heo có `HealthComponent` 60 máu; `AnimalController` chết thì rơi tài nguyên rồi biến mất. Máu thú chưa lưu trong save.
- **Đã xác nhận**: **27/27 test PlayMode pass** (6 test mới `Milestone5JobTests.cs`: nhóm lẫn nghề chặt cây/trinh sát đi theo + đủ 10 gỗ; nông dân gieo → thu hoạch → gieo lại; không chọn hạt thì dừng; thợ săn hạ heo +6 thức ăn; nông dân thuần hóa heo hoang; lệnh di chuyển hủy việc). Chạy thử trong Play mode qua MCP xác nhận phân việc đúng (Ka/Mây/Đá chặt cây, Suối đi theo).
- **Chưa xác nhận bằng mắt**: NPC đi tới và làm việc — Editor không focus thì gần như không chạy frame (đã ghi 2026-09-26), nên ảnh chụp không thấy NPC di chuyển; cần user chơi thử.

## Nhật ký phiên làm việc 2026-09-30 (phần 6 — M5.4 UI nhóm/nghề)
- Mới `UI/SelectionPanelUI.cs` (góc dưới trái, trên dòng gợi ý): tiêu đề "Đang chọn: N người", danh sách tên + (nam)/(nữ), mỗi nghề một ô tô màu theo màu áo "Nông dân ×2" — bấm để chỉ giữ người nghề đó (`SelectionManager.SetSelection`), hàng nút "→ <nghề>" đổi nghề cả nhóm (`NpcController.SetProfession` → tốc độ/máu/dụng cụ/màu áo đổi theo). Ẩn khi không chọn ai — script nằm ở object cha, chỉ ẩn/hiện object con `Content` (nếu ẩn chính object gắn script thì mất luôn việc nghe sự kiện).
- Mới `UI/HealthBar.cs`: thanh máu 3D nổi trên đầu, luôn quay về camera, co từ mép trái theo % máu, xanh → đỏ; hiện khi bị thương hoặc NPC đang được chọn. Gắn vào prefab Villager và WildBoar.
- **Lần đầu xem được UI Canvas bằng ảnh**: trong Play mode tạm chuyển Canvas sang `ScreenSpaceCamera` rồi render camera (thay đổi mất khi thoát Play) — xác nhận bảng chọn, ô nghề, nút đổi nghề, tiếng Việt có dấu, thanh máu (Suối mất 35/70 máu → thanh vàng ngắn lại). Cách này dùng lại được để kiểm tra mọi UI sau này.
- Phát hiện khi xem UI: tên trong data cũ không dấu ("Go", "Thuc an", "Tri thuc", "Leu trai", "Kho chua", "Cay mong", "Nong nghiep", "Heo rung") dù font hiển thị được tiếng Việt có dấu → nên sửa cho đồng bộ (việc nhỏ, chưa làm).
- **Đã xác nhận**: **31/31 test PlayMode pass** (4 test mới `Milestone5UiTests.cs`: bảng ẩn/hiện + nhóm theo nghề; bấm ô nghề để lọc; đổi nghề cập nhật tốc độ/máu/bảng; thanh máu hiện khi chọn/bị thương, co theo máu); Console sạch.

## Nhật ký phiên làm việc 2026-09-30 (phần 7 — hết hạn giữ vị trí)
- Yêu cầu của user: NPC đã ra lệnh, khi **không còn được chọn** thì sau 20–30s quay lại hành vi ban đầu. `NpcController`: `holdPosition` giờ hết hạn sau `holdDuration` ngẫu nhiên trong [`minHoldTime` 20s, `maxHoldTime` 30s] — chỉ đếm khi đứng rảnh (Idle) và không được chọn; đang đi/đang làm việc/đang được chọn thì không đếm, lệnh mới hoặc xong việc thì đếm lại. Hết hạn → đi dạo quanh chỗ đang đứng (không quay về chỗ cũ).
- **Đã xác nhận**: **32/32 test PlayMode pass** (test mới: người bị bỏ chọn quay lại đi dạo sau 20–30s, người vẫn được chọn tiếp tục giữ vị trí).
- **Sự cố**: Editor mở liên tục ~2h (dựng scene + chạy test nhiều lần) phình lên ~4GB, bộ nhớ ảo còn ~0.5GB → lượt chạy test bị crash (`MimallocPrimErrorHandler`). Tắt/mở lại Editor rồi chạy lại thì được. Kinh nghiệm: mở lại Editor sau vài lượt dựng scene + test; gốc rễ vẫn là pagefile nằm trên ổ C gần đầy.

## Vấn đề đang tồn đọng (Known issues / Open questions)
- [x] Chế độ combat: lai (2026-09-26) + chi tiết chỉ huy NPC theo nghề (2026-09-30)
- [ ] Chưa có tên chính thức cho dự án
- [ ] Milestone 2-4: đã xác nhận logic (test tự động) + Console sạch khi Play thật, nhưng chưa có ai tự tay bấm phím/chuột thật để xác nhận input (`PlayerInteraction`, `BuildingPlacer`) và chưa xác nhận trực quan layout UI (công cụ chụp ảnh hiện tại không thấy được Canvas ScreenSpaceOverlay)
- [x] Save/load trạng thái FarmPlot + vật nuôi (+ tech đã mở khóa) — xong 2026-09-30 (`saveVersion` 2)
- [ ] Save/load chưa lưu cây (ResourceNode) đã bị chặt: tải game sẽ không hồi lại cây đã mất trong phiên, và cây đã chặt dở vẫn đầy gỗ
- [x] Save/load (F5/F9) của Milestone 1 đã test trong Play mode thật (2026-09-26) — đúng
- [x] Camera dùng Skybox clear flags gây nền trời không hợp — đã đổi Solid Color khi chuyển 2.5D (2026-09-30)
- [ ] Máy 8GB + pagefile nằm trên ổ C gần đầy: Unity batch mode crash "paging file is too small"/"Out of memory" khi bộ nhớ ảo trống < ~4GB (gặp lại 2026-09-30). Cách sửa tận gốc: chuyển pagefile sang ổ D (còn ~33GB). Tạm thời: đóng Edge/app nặng trước khi chạy Unity
- [ ] `Assets/Sprites/Generated/` (sprite khối màu của bản 2D) không còn được dùng — có thể xóa khi chắc chắn không quay lại 2D
- [ ] `com.unity.ai.assistant` đã cài local, chưa quyết định có commit vào repo chung không
- [x] Feedback tương tác vật nuôi (việc #1 trả nợ kỹ thuật, 2026-09-26) — đã code + xác nhận sống 4/5 thông báo, còn nhánh "thu hoạch sản phẩm" chỉ mới code-review + assertion tự động (chưa chạy lại batch để xác nhận, xem nhật ký phiên phần 3)
