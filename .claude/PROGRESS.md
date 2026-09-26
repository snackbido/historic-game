# PROGRESS.md — Tiến trình dự án

> Cập nhật file này sau mỗi buổi làm việc: đánh dấu việc đã xong, ghi chú vấn đề gặp phải, quyết định đã chốt.

## Trạng thái hiện tại
- **Giai đoạn**: Milestone 1 đã xác nhận chạy đúng (kể cả Save/Load F5/F9); Milestone 2-4 đã xác nhận đúng logic qua test PlayMode tự động (8/8 pass) + xác nhận thêm qua Unity MCP (Play mode thật không lỗi Console). Còn thiếu xác nhận input/UI trực quan bằng người thật (Milestone 5 chưa bắt đầu)
- **Cập nhật lần cuối**: 2026-09-26

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
- [ ] HealthComponent dùng chung cho Player/NPC/Building
- [ ] Enemy AI cơ bản (patrol/chase/attack)
- [ ] Vũ khí cơ bản (giáo/đá ném)
- [ ] Cơ chế chỉ huy nhóm: chọn NPC + ra lệnh (thiết kế chi tiết còn thiếu, xem quyết định 2026-09-26 ở Decision Log)

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

## Vấn đề đang tồn đọng (Known issues / Open questions)
- [ ] Chưa quyết định: chế độ combat (trực tiếp hay chỉ huy nhóm)?
- [ ] Chưa có tên chính thức cho dự án
- [ ] Milestone 2-4: đã xác nhận logic (test tự động) + Console sạch khi Play thật, nhưng chưa có ai tự tay bấm phím/chuột thật để xác nhận input (`PlayerInteraction`, `BuildingPlacer`) và chưa xác nhận trực quan layout UI (công cụ chụp ảnh hiện tại không thấy được Canvas ScreenSpaceOverlay)
- [ ] Chưa làm save/load cho trạng thái FarmPlot (đang trồng gì, giai đoạn nào) và trạng thái vật nuôi (đã thuần hóa chưa, độ đói) — nợ kỹ thuật từ M2/M3
- [x] Save/load (F5/F9) của Milestone 1 đã test trong Play mode thật (2026-09-26) — đúng
- [ ] Camera dùng Skybox clear flags gây nền trời không hợp — đổi Solid Color khi polish (M7)
- [ ] `com.unity.ai.assistant` đã cài local, chưa quyết định có commit vào repo chung không
