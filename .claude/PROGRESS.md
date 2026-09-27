# PROGRESS.md — Tiến trình dự án

> Cập nhật file này sau mỗi buổi làm việc: đánh dấu việc đã xong, ghi chú vấn đề gặp phải, quyết định đã chốt.

## Trạng thái hiện tại
- **Giai đoạn**: Milestone 1-4 đã xác nhận chạy đúng qua cả test tự động lẫn user tự playtest thật — nợ kỹ thuật M1-M4 đã trả xong. **Milestone 4.5 (Dân làng) Phase 1 đã code xong**: nhu cầu đói/ngủ/ấm, job Gathering/Guarding, kéo-chọn-vùng + ra lệnh, UI panel, save/load — 11/11 test PlayMode pass qua Unity batch mode (2026-09-27). Job "Building" cố tình chưa làm (cần bàn thêm thiết kế). Còn thiếu: user tự tay test input chuột thật (kéo chọn/ra lệnh) — chưa làm được qua công cụ tự động. **Đã chuyển góc nhìn top-down → isometric (2026-09-27)**: Grid dùng `CellLayout.Isometric`, di chuyển Player theo 2 trục chéo, camera không xoay — xác nhận sống qua Unity MCP (grid math + round-trip đúng, building đặt đúng vị trí hình thoi), xem ARCHITECTURE.md §5 để biết chi tiết cách làm.
- **Cập nhật lần cuối**: 2026-09-27
- **Lưu ý môi trường quan trọng**: Unity Editor GUI đã bị đóng giữa phiên 2026-09-27 (không rõ do ai/vì sao) khiến Unity MCP mất kết nối hoàn toàn. Đã chuyển sang xác nhận qua `Unity.exe -batchmode -nographics -executeMethod ...`/`-runTests` chạy trực tiếp từ terminal (không cần mở GUI) — vẫn hoạt động tốt, dùng cách này nếu MCP không kết nối được ở phiên sau. Toàn bộ thay đổi Milestone 4.5 **chưa commit** (đợi user xác nhận).

## Kế hoạch triển khai tiếp theo (ưu tiên, cập nhật 2026-09-27)

**Ưu tiên 1 — Trả nợ kỹ thuật còn lại của M1-M4** — ✅ **Hoàn thành (2026-09-27)**
1. ~~Xác nhận nhánh thông báo "thu hoạch sản phẩm"~~ — Xong, xác nhận sống qua Unity MCP.
2. ~~Save/Load cho `FarmPlot` và `AnimalController`/`TamingSystem`~~ — Xong.
3. ~~Test tay input/UI trực quan~~ — **Xong, user đã tự playtest thật** (di chuyển, tương tác phím, Tech Tree, Build Menu). Qua đó phát hiện thêm 2 việc và đã xử lý luôn: (a) `BuildingPlacer` im lặng khi đặt công trình thất bại → đã thêm thông báo; (b) tài nguyên biến mất vĩnh viễn sau khi hết → đã đổi thành tự mọc lại (60-120s) + thêm thông báo thu hoạch cho mọi loại tài nguyên (node lẫn nông sản) + thêm 2 cây gỗ nữa (tổng 3 cây).
4. ~~Đổi Camera `clearFlags` từ Skybox → Solid Color~~ — Xong.

**Ưu tiên 2 — Milestone 4.5 (Dân làng)** — Phase 1 ✅ **đã code + test xong (2026-09-27)**, còn lại:
- [x] Nhu cầu đói/ngủ/ấm, job Gathering/Guarding, `SelectionManager`/`CommandSystem` (kéo chọn + ra lệnh, dùng chung được cho Combat sau), `VillagerPanelUI`, save/load — 11/11 test PlayMode pass.
- [ ] **User tự tay test input chuột thật** (kéo chọn vùng, click phải ra lệnh, xem `SelectionRing` hiện đúng, xem panel danh sách cập nhật đúng) — chưa xác nhận được qua batch mode/MCP.
- [ ] Job "Building" (villager tự đi xây) — **cố tình chưa làm**, cần bàn thiết kế cụ thể trước (khái niệm công trường/thời gian thi công chưa tồn tại). Sau khi bàn xong mới code tiếp phần này, hoặc bỏ qua luôn nếu user thấy không cần thiết cho MVP.

**Ưu tiên 3 — Milestone 5 (Combat)**, sau khi Dân làng xong: `HealthComponent` dùng chung Player/NPC/Building → vũ khí cơ bản (giáo/đá ném) → Enemy AI cơ bản (patrol/chase/attack) → mở rộng `CommandSystem` đã có ở M4.5 để ra lệnh tấn công/theo tôi → UI danh sách nhóm chỉ huy.

**Ưu tiên 4 — Milestone 6 (Thiên tai)**, sau khi Combat ổn định: `DisasterManager`, base class `DisasterEvent`, 1-2 loại thiên tai đầu tiên (cháy rừng/lũ lụt), cảnh báo trước, hiệu ứng lên Building/Resource/Animal/Farming.

**Ưu tiên 5 — Milestone 7 (Polish)**: âm thanh, VFX, cân bằng số liệu, main menu/settings.

**Việc phụ, làm khi thuận tiện (không chặn tiến độ)**: đặt tên chính thức cho dự án.

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

## Milestone 4.5 — Dân làng (Villager NPC)
> Chèn mới (2026-09-27), chặn Milestone 5 vì Combat đã chốt "chỉ huy dân làng" nhưng dân làng chưa tồn tại. Theo đúng SPEC.md §4: "NPC dân làng: có nhu cầu cơ bản (đói, ngủ, ấm), có thể gán công việc (thu thập, xây dựng, canh gác)".
- [x] `Data/VillagerData.cs` (ScriptableObject) — tốc độ di chuyển, tốc độ giảm đói/ngủ/ấm, ngưỡng cảnh báo, tài nguyên ăn
- [x] `Villager/VillagerController.cs` — 3 chỉ số nhu cầu (Hunger/Sleep/Warmth) giảm dần theo thời gian (mirror pattern `AnimalController`); đói thấp tự tiêu Food từ `ResourceManager` (ăn xong hồi về 100); ngủ/ấm thấp cảnh báo 1 lần qua `EventBus.RaiseNotification` (chưa có hiệu ứng gameplay phục hồi — đúng mức đơn giản hóa đã ghi trước, để mở rộng sau)
- [x] `Villager/VillagerJob` (enum: **None/Gathering/Guarding** — bỏ **Building** khỏi enum, xem ghi chú bên dưới) + logic thực thi trên `VillagerController` (Gathering: di chuyển tới `ResourceNode` được giao, Harvest lặp lại mỗi ~1s khi đủ gần, tự dừng nếu node cạn; Guarding: di chuyển tới vị trí chỉ định rồi đứng yên)
- [x] `Villager/SelectionManager.cs` — kéo chọn vùng bằng chuột (drag box-select) hoặc click chọn 1 villager gần nhất, hiển thị `SelectionRing` quanh villager đã chọn, bắn `EventBus.OnSelectionChanged` (dùng chung sau cho Combat)
- [x] `Villager/CommandSystem.cs` — chuột phải ra lệnh theo mục tiêu (lên `ResourceNode` = gán Gathering kèm thông báo, lên vị trí trống = Guarding tại đó kèm thông báo) (dùng chung sau cho Combat)
- [x] `Villager/VillagerManager.cs` — registry `VillagerData` theo id + `GetSaveData()/LoadFromSaveData()` (destroy-toàn-bộ-rồi-respawn, mirror đúng `TamingSystem`)
- [x] `UI/VillagerPanelUI.cs` — danh sách các villager đang được chọn (tên + Đói/Ngủ/Ấm), tự rebuild qua `EventBus.OnSelectionChanged`
- [x] Spawn 2 villager mẫu (`Villager_1`, `Villager_2`) qua `GameplaySceneBuilder.cs`; prefab + `VillagerData_Basic.asset` sinh qua `GameContentBuilder.cs` (mirror `BuildWildBoarContent`)
- [x] Save/Load cho trạng thái villager (vị trí, đói/ngủ/ấm) — gắn vào `SaveData.villagers` + `GameManager.SaveGame()/LoadGame()`, đúng pattern Farm/Animal

**Ghi chú milestone này**:
- **Bỏ job "Building" khỏi scope lần này** (không làm nửa vời): cần khái niệm "công trường/thời gian thi công" chưa tồn tại (Player hiện đặt building tức thời qua `BuildingPlacer`), và cách đơn giản hóa hợp lý cần bàn thêm với user trước khi code — để lại làm follow-up riêng, không nhét enum/method rỗng vào code cho có.
- `SelectionManager`/`CommandSystem` viết đúng như kế hoạch: dùng chung được cho cả gán việc dân làng lẫn chỉ huy chiến đấu ở Milestone 5 sau này (chỉ cần mở rộng `CommandSystem` để nhận diện thêm mục tiêu enemy/animal → AssignAttack thay vì AssignGathering).
- **Đã xác nhận qua Unity Editor batch mode** (GUI đã bị đóng giữa phiên, không còn mở qua Unity MCP được nữa — chuyển sang `Unity.exe -batchmode -nographics -executeMethod ...` để build + test, không cần mở GUI): `Tools/Prehistoric/Build All` chạy sạch, không lỗi compile; scene `Gameplay.unity` dựng lại có đủ `VillagerManager`/`SelectionManager`/`CommandSystem`/`VillagerPanel`/2 villager. Viết thêm `Assets/Tests/PlayMode/Milestone4_5VillagerTests.cs` (3 test: đói tự ăn đúng, gán Gathering đi tới cây và thu hoạch đúng, `SetSelected` đổi state đúng) — **11/11 test PlayMode pass** (8 test cũ M2-M4 + 3 test mới), không có regression.
- Chưa xác nhận bằng tay thật (kéo-chọn-vùng bằng chuột, click phải ra lệnh, nhìn `SelectionRing` hiện đúng) — công cụ hiện tại (batch mode/test tự động) không giả lập được input chuột thật, giống hạn chế đã ghi nhận từ M1-M4. Cần user tự mở Editor test khi có dịp.

## Milestone 5 — Combat
- [x] Quyết định chế độ chiến đấu (hướng chung, 2026-09-26): **lai** — vừa điều khiển trực tiếp nhân vật chính, vừa chỉ huy được nhóm NPC.
- [x] Quyết định chi tiết (2026-09-27): NPC chỉ huy được = **cả dân làng rảnh lẫn "lính" chuyên** (lính là loại unit mới, mở rộng sau khi Villager MVP xong); cơ chế ra lệnh = **kéo chọn vùng (drag box-select) + click ra lệnh** kiểu RTS thật (không phải lệnh ngữ cảnh đơn giản); UI = **có panel danh sách/icon riêng** cho nhóm đang chỉ huy (không chỉ hiệu ứng trên map)
- [ ] HealthComponent dùng chung cho Player/NPC/Building
- [ ] Enemy AI cơ bản (patrol/chase/attack)
- [ ] Vũ khí cơ bản (giáo/đá ném)
- [ ] Đơn vị "lính" chuyên biệt (mở khóa qua tech/building mới) — bổ sung thêm vào bên cạnh dân làng làm đối tượng chỉ huy được
- [ ] Mở rộng `SelectionManager`/`CommandSystem` (đã xây ở Milestone 4.5 cho việc dân làng) để hỗ trợ thêm lệnh tấn công mục tiêu/theo tôi
- [ ] `UI/CommandedUnitsPanelUI.cs` — mở rộng hoặc dùng chung `VillagerPanelUI` để hiển thị cả lính

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
| 2026-09-27 | Milestone 5 (Combat) chi tiết: chỉ huy được **cả dân làng rảnh lẫn "lính" chuyên**; ra lệnh bằng **kéo chọn vùng (drag box-select) + click ra lệnh** kiểu RTS thật; **có UI panel danh sách/icon riêng** cho nhóm đang chỉ huy | User chọn qua 3 câu hỏi thiết kế trực tiếp; chọn "cả hai" cho đối tượng chỉ huy để MVP dùng ngay dân làng, mở rộng lính sau; chọn kéo-chọn-vùng thay vì lệnh ngữ cảnh đơn giản dù tốn công hơn — ưu tiên trải nghiệm RTS quen thuộc hơn tối giản input |
| 2026-09-27 | Chèn thêm **Milestone 4.5 — Dân làng (Villager NPC)** trước Milestone 5, làm **đầy đủ** theo SPEC.md §4 (nhu cầu đói/ngủ/ấm + gán công việc thu thập/xây dựng/canh gác), không làm bản rút gọn | Phát hiện khi lên kế hoạch Combat: game chưa có NPC dân làng nào dù SPEC.md đã mô tả từ đầu — không thể "chỉ huy dân làng" nếu dân làng chưa tồn tại; user được hỏi 3 lựa chọn phạm vi (tối giản/đổi hướng dùng lính/làm đầy đủ) và chọn làm đầy đủ ngay, chấp nhận kéo dài thời gian tới Combat |
| 2026-09-27 | `SelectionManager`/`CommandSystem` (kéo chọn + ra lệnh) xây 1 lần ở Milestone 4.5, dùng chung cho cả gán việc dân làng lẫn chỉ huy chiến đấu ở Milestone 5 | Cả 2 tính năng đều cần "chọn nhiều đối tượng + ra lệnh theo mục tiêu click", tách thành 2 hệ thống riêng sẽ trùng lặp code không cần thiết |
| 2026-09-27 | Đổi góc nhìn từ 2D top-down sang **2D isometric thật** (chiếu tọa độ chuẩn, không phải "isometric giả" chỉ xoay camera/đổi sprite) | User chủ động yêu cầu đổi góc nhìn; chọn phương án đúng chuẩn thay vì mẹo hình ảnh rẻ tiền dù tốn công hơn, vì ARCHITECTURE.md đã thiết kế sẵn để tách Presentation dễ đổi — xem ARCHITECTURE.md §5 cho chi tiết kỹ thuật (`Grid.CellLayout.Isometric` + `IsometricUtility` cho input, không xoay camera) |

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

## Nhật ký phiên làm việc 2026-09-27 (trả nợ kỹ thuật M1-M4: save/load Farm+Animal, harvest notification, camera)
- **Save/Load `FarmPlot`**: thêm `FarmPlot.LoadState()` + `FarmManager.GetSaveData()/LoadFromSaveData()` (registry `knownCrops` mirroring pattern `typesById`/`buildingsById` đã có). Định danh plot theo `GameObject.name` (`FarmPlot_1`/`FarmPlot_2` — tên cố định do `GameplaySceneBuilder` đặt), không cần Instantiate/Destroy vì các ô đất là vật thể cố định trong scene (không sinh ra/mất đi khi chơi), khác với building.
- **Save/Load `AnimalController`/`TamingSystem`**: mirror đúng pattern destroy-toàn-bộ-rồi-respawn của `BuildingPlacer` (vì vật nuôi *có thể* sinh sản ra bản sao mới giữa 2 lần save, không thể định danh cố định theo tên như FarmPlot) — thêm registry `knownAnimalTypes` (id → `AnimalData`), `AnimalController.LoadState()`, `TamingSystem.GetSaveData()/LoadFromSaveData()`. Timer nội bộ (hungerTimer/reproductionTimer/productionTimer) **không** được lưu, reset về 0 khi load — đơn giản hóa có chủ đích, chấp nhận sai lệch nhỏ về thời điểm mốc tiếp theo, tương tự mức độ đơn giản hóa đã chọn ở các hệ thống khác.
- `GameManager`/`SaveData` gắn thêm 2 field mới (`farmPlots`, `animals`) + gọi `FarmManager`/`TamingSystem` trong `SaveGame()/LoadGame()`. `GameplaySceneBuilder.cs` cập nhật để wire `knownCrops`/`knownAnimalTypes`/`farmManager`/`tamingSystem` khi dựng scene — đã chạy `Tools/Prehistoric/Build All` qua Unity MCP để tái tạo `Gameplay.unity` với wiring mới (bắt buộc, sửa code builder không tự cập nhật file scene đã lưu).
- **Camera**: đổi `clearFlags` từ Skybox → Solid Color trong `GameplaySceneBuilder.cs` (không cần chờ M7 như dự kiến ban đầu).
- **Xác nhận sống qua Unity MCP** (Play mode thật, không phải test tự động): plant crop trên `FarmPlot_1` → save → trồng thêm `FarmPlot_2` + cho heo ăn thêm (mutate) → load → xác nhận `FarmPlot_1` giữ đúng crop/stage, `FarmPlot_2` về lại `Empty`, heo về lại đúng `Wild`/`TamingProgress=1` (không phải state đã mutate). Có 1 lần tưởng phát hiện bug (heo sau load vẫn hiện `Tamed`) — điều tra ra là do `GameObject.Find` gọi ngay trong cùng frame với `Destroy()` (destroy bị trì hoãn tới cuối frame), không phải bug thật; xác nhận lại bằng `FindObjectsByType<AnimalController>()` ở lệnh riêng (frame khác) thấy đúng 1 con, đúng state.
- **Xác nhận nốt thông báo "thu hoạch sản phẩm" của Milestone 3** (5/5, còn nợ từ 2026-09-26): gặp lại đúng hiện tượng đã ghi nhận trước đó — `Time.time` đứng yên dù đợi thật vì cửa sổ Unity Editor không có focus khi điều khiển qua MCP. **Cách khắc phục tìm được**: dùng PowerShell `(New-Object -ComObject WScript.Shell).AppActivate(<editorPid>)` để đưa cửa sổ Unity ra foreground trước khi đợi — sau đó `Time.time` chạy đúng tốc độ (`Time.timeScale` boost thêm để không phải đợi lâu). Ghi nhớ cách này cho các lần sau cần chờ real-time trong Play mode qua Unity MCP mà không muốn dựng lại batch-mode test.
- **Gotcha nhỏ phát hiện thêm**: `Object.FindObjectsByType<T>(FindObjectsSortMode.None)` bị deprecated (warning, không phải lỗi) ở Unity 6000.6.2f1 — đổi sang overload `FindObjectsByType<T>(FindObjectsInactive.Exclude)` trong `FarmManager`/`TamingSystem`.
- Chưa chạy lại bộ `Assets/Tests/PlayMode` bằng batch mode (Editor GUI vẫn đang mở, không mở được instance thứ 2) — nhưng đã xác nhận tương đương bằng thao tác sống qua Unity MCP như trên nên coi là đủ cho phiên này. Có thể bổ sung 1 test tự động mới cho `FarmManager.GetSaveData/LoadFromSaveData` và `TamingSystem.GetSaveData/LoadFromSaveData` ở phiên sau nếu muốn có regression test lâu dài (hiện chưa có, chỉ mới xác nhận qua RunCommand thủ công lần này).

## Nhật ký phiên làm việc 2026-09-27 (phần 2 — user playtest thật, fix thiếu feedback khi xây dựng)
- **User tự tay playtest** (không qua MCP): di chuyển, cho ăn/thuần hóa/thu hoạch vật nuôi (đã xác nhận thấy đủ cả thông báo thu hoạch sau khi đợi đúng `productionInterval`), mở Tech Tree đều ổn.
- **Báo lỗi**: chọn xây "Kho chứa" nhưng không xây được, không rõ vì sao (không có thông báo gì).
- **Điều tra qua Unity MCP** (đọc trực tiếp state đang chạy trong Play mode của user): `TechNode_Farming` đã unlock đúng, `BuildingData_Storage` đã unlock đúng (nên nút "Kho chứa" có hiện trong Build Menu) — nhưng `wood=10` trong khi Storage cần **20 Gỗ** (`BuildingData_Storage.asset costs`) → `CanAfford=False`. **Không phải bug**, chỉ là chưa đủ gỗ.
- **Lỗ hổng UX thật sự phát hiện được**: `BuildingPlacer.TryPlace()/PlaceBuilding()` xử lý đúng nhưng **im lặng hoàn toàn** khi thất bại (thiếu tài nguyên, ô đất đã có công trình, hoặc building chưa unlock) — không có phản hồi gì cho người chơi, giống đúng lớp lỗi UX đã gặp và sửa ở `TamingSystem` (Milestone 3, 2026-09-26). Người chơi bấm chuột mà "không có gì xảy ra" nên tưởng là bug.
- **Đã sửa**: thêm `EventBus.RaiseNotification(...)` vào `BuildingPlacer.cs` ở 4 nhánh — ô đất đã có công trình, công trình chưa mở khóa, không đủ tài nguyên (kèm tên công trình), và xây thành công (chỉ khi `spendResources=true`, tức không bắn thông báo khi đang `LoadFromSaveData`). Đã build lại sạch qua Unity MCP (`scriptCompilationFailed=False`), chưa test lại sống bằng tay/MCP vì đặt công trình cần input chuột thật kèm raycast lên `Grid` — để user tự xác nhận ở lần playtest tiếp theo (cho đủ 20 gỗ rồi thử xây lại).
- **Yêu cầu mới từ user**: tài nguyên (node như `Tree`) nên "vô hạn" — thu hoạch hết thì mọc lại sau khoảng 1-2 phút thay vì biến mất vĩnh viễn. **Đã implement**: `ResourceNode.cs` không còn `Destroy(gameObject)` khi hết tài nguyên — thay bằng ẩn `SpriteRenderer`/tắt `Collider2D` (để không hiện, không tương tác được) rồi chạy coroutine `WaitForSeconds(Random.Range(respawnTimeMin, respawnTimeMax))` (mặc định 60-120s, chỉnh được qua Inspector theo từng node), hết giờ thì nạp lại `amountRemaining` về mức ban đầu và hiện/enable lại. Giữ nguyên GameObject (không Instantiate lại) nên không cần thêm gì vào save/load.
- **Đã xác nhận sống qua Unity MCP**: harvest hết `Tree` (10 gỗ) → xác nhận sprite/collider tắt ngay → set `Time.timeScale=60` + focus cửa sổ Editor (dùng lại thủ thuật `AppActivate`) → đợi thật đến khi `Time.time` vượt mốc respawn → xác nhận sprite/collider bật lại đúng lúc → harvest lại thành công (+1 gỗ), xác nhận vòng lặp "hết → ẩn → chờ → mọc lại → thu hoạch được tiếp" hoạt động đúng như yêu cầu. Console sạch trong suốt quá trình.
- **Yêu cầu tiếp theo từ user**: thêm thông báo khi thu thập tài nguyên, áp dụng chung cho mọi loại tài nguyên (không chỉ gỗ). **Đã implement**: `ResourceNode.Harvest()` bắn `EventBus.RaiseNotification($"+{amount} {resourceType.displayName}")` — dùng `displayName` của chính `ResourceTypeData` gán cho node nên tự động đúng cho bất kỳ loại tài nguyên nào (gỗ, đá... sau này) mà không cần sửa code. Tiện thể phát hiện `FarmPlot.Harvest()` (thu hoạch nông sản) cũng thiếu thông báo y hệt — đã thêm luôn theo cùng pattern `TamingSystem.CollectProduct()` (liệt kê tất cả loại tài nguyên trong `harvestYield`, vd "Thu hoạch Cây mọng: +3 Thức ăn").
- **Đã xác nhận sống qua Unity MCP**: harvest `Tree` → notification `"+1 Gỗ"`; harvest `FarmPlot` (Berry, ép thẳng lên `ReadyToHarvest` qua `LoadState` để test nhanh không cần chờ thời gian lớn) → notification `"Thu hoạch Cây mọng: +3 Thức ăn"`. Console sạch.
- **Yêu cầu tiếp theo từ user**: thêm 1-2 cây gỗ nữa (chỉ có 1 cây `Tree` từ đầu). **Đã làm**: tách logic tạo cây thành helper `CreateTree(name, position, wood)` trong `GameplaySceneBuilder.cs` (mirror `CreateFarmPlot`), gọi 3 lần → `Tree_1` (2,1), `Tree_2` (4,2), `Tree_3` (-1, 2.5) — không chồng lấn với Player/FarmPlot/WildBoar. Build lại scene qua Unity MCP, xác nhận sống có đúng 3 `ResourceNode` trong scene, console sạch. Mỗi cây có respawn timer độc lập (60-120s) như đã implement ở mục trên.

## Nhật ký phiên làm việc 2026-09-27 (phần 3 — Milestone 4.5 Phase 1, đổi sang isometric)
- **Sự cố môi trường**: Unity Editor GUI bị đóng hoàn toàn giữa phiên (không có Unity.exe nào chạy), Unity MCP mất kết nối. **Giải pháp**: chạy thẳng `Unity.exe -batchmode -nographics -executeMethod PrehistoricTribe.EditorTools.GameplaySceneBuilder.BuildAll` và `-runTests -testPlatform PlayMode` từ terminal, không cần mở GUI — build content/scene + chạy test đều hoạt động bình thường qua cách này. Sau đó user tự mở lại Editor GUI, Unity MCP kết nối lại được bình thường.
- **Milestone 4.5 (Dân làng) — Phase 1**: implement toàn bộ breakdown đã lên kế hoạch — `VillagerData.cs`, `VillagerController.cs` (nhu cầu đói/ngủ/ấm, tự ăn khi đói thấp, cảnh báo 1 lần khi ngủ/ấm thấp), `VillagerJob` enum (chỉ **Gathering**/**Guarding**, cố tình bỏ **Building** vì cần bàn thiết kế công trường/thời gian thi công trước — không muốn code nửa vời), `SelectionManager.cs` (kéo-chọn-vùng/click chọn), `CommandSystem.cs` (chuột phải ra lệnh), `VillagerManager.cs` (registry + save/load kiểu destroy-respawn như `TamingSystem`), `VillagerPanelUI.cs`. `GameContentBuilder.cs` sinh thêm `Villager.prefab`/`VillagerData_Basic.asset` (mirror `BuildWildBoarContent`). Spawn 2 villager mẫu trong `GameplaySceneBuilder.cs`.
- **Xác nhận qua batch mode**: viết `Milestone4_5VillagerTests.cs` (3 test mới: đói tự ăn đúng, gán Gathering đi-tới-cây-và-harvest đúng, `SetSelected` đổi state đúng). Lần chạy đầu 1 test fail do lỗi *cách viết test* (chờ quá lâu ở `Time.timeScale` cao khiến độ đói giảm tiếp sau khi đã ăn) — không phải lỗi logic game, sửa lại cách chờ trong test (dừng ngay khung hình tài nguyên bị trừ thay vì chờ thêm 1 khoảng cố định) → **11/11 test PlayMode pass** (8 cũ + 3 mới), không regression.
- **Yêu cầu tiếp theo từ user**: đổi góc nhìn top-down → isometric, chọn hướng "isometric thật" (chiếu tọa độ chuẩn) thay vì mẹo hình ảnh rẻ tiền. **Đã implement** (chi tiết kỹ thuật xem ARCHITECTURE.md §5): `Utils/IsometricUtility.cs` (2 trục chéo isometric dùng cho input), `PlayerController.cs` đổi hướng di chuyển sang 2 trục này, `Grid` trong `GameplaySceneBuilder.cs` đổi `cellLayout = Isometric` + `cellSize = (1, 0.5, 1)`, camera thêm `transparencySortMode = CustomAxis`/`transparencySortAxis = Vector3.up` để tự xếp lớp theo Y. `VillagerController`/`AnimalController` không cần sửa vì chúng di chuyển theo vector hướng-tới-target, tự động đúng bất kể "hình dạng" không gian.
- **Đã xác nhận sống qua Unity MCP**: `Grid.cellLayout=Isometric cellSize=(1,0.5,1)`; kiểm chứng công thức chiếu đúng — `cell(1,0)-cell(0,0)` cho hướng trùng khớp chính xác với `IsometricUtility.InputToIsometric(1,0)` (hướng phím "D"); `WorldToCell(GetCellCenterWorld(cell))` round-trip đúng cho nhiều cell; đặt thử 4 hut vào ô liền kề qua `BuildingPlacer.LoadFromSaveData` (dùng lại API save/load công khai, không hack qua reflection) rồi chụp ảnh `Unity_SceneView_Capture2DScene` — xác nhận tâm các building lệch đúng theo hình thoi isometric (chỉ bị chồng hình do sprite placeholder hình vuông 1x1 lớn hơn khoảng cách giữa các ô — vấn đề mỹ thuật, để dành Milestone 7). Console sạch trong suốt quá trình, không cần chạy lại bộ test PlayMode vì không file nào bị test bao phủ thay đổi (`PlayerController.cs` chưa có test nào đụng tới).
- Chưa xác nhận bằng tay thật (bấm WASD xem hướng di chuyển chéo có đúng cảm giác isometric không, kéo-chọn-vùng dân làng, click phải ra lệnh) — cần user tự test khi mở Editor.

## Vấn đề đang tồn đọng (Known issues / Open questions)
- [ ] Chưa quyết định: chế độ combat (trực tiếp hay chỉ huy nhóm)?
- [ ] Chưa có tên chính thức cho dự án
- [x] Milestone 2-4: **user đã tự tay playtest thật (2026-09-27)** — di chuyển, tương tác phím (cho ăn/thuần hóa/thu hoạch), mở Tech Tree/Build Menu qua UI đều hoạt động đúng. Phát hiện thêm 1 lỗ hổng UX qua chính lần test này (xem log bên dưới, đã sửa)
- [x] Save/load cho trạng thái `FarmPlot` (crop + stage + timer) và `AnimalController`/`TamingSystem` (state/tamingProgress/hunger/productReady) — implement + xác nhận sống qua Unity MCP (2026-09-27), xem nhật ký phiên bên dưới
- [x] Save/load (F5/F9) của Milestone 1 đã test trong Play mode thật (2026-09-26) — đúng
- [x] Camera dùng Skybox clear flags gây nền trời không hợp — đổi sang Solid Color trong `GameplaySceneBuilder.cs` (2026-09-27), không cần chờ M7
- [x] `com.unity.ai.assistant` — đã quyết định commit vào repo chung (2026-09-26, commit `23cc8c8`) để mọi máy đều có cùng tooling MCP (Play/Stop, đọc Console, RunCommand); đã gỡ `com.unity.ai.inference` (dependency ML/Sentis không cần thiết gây lỗi compile) trước khi commit
- [x] Feedback tương tác vật nuôi (việc #1 trả nợ kỹ thuật) — đã code + xác nhận sống **5/5** thông báo (2026-09-27: xác nhận nốt nhánh "thu hoạch sản phẩm" qua Unity MCP Play mode thật, xem nhật ký phiên bên dưới)
- [ ] Milestone 4.5 (Dân làng) + đổi isometric: chưa xác nhận bằng tay thật (WASD di chuyển chéo có đúng cảm giác isometric không, kéo-chọn-vùng, click phải ra lệnh, panel danh sách villager) — chỉ mới xác nhận logic/math qua batch mode + Unity MCP
- [ ] Job "Building" của Villager (dân làng tự đi xây) chưa làm — cần bàn thiết kế cụ thể trước (khái niệm công trường/thời gian thi công chưa tồn tại), hoặc quyết định bỏ hẳn khỏi MVP
- [ ] Sprite placeholder hình vuông 1x1 sẽ chồng hình ảnh khi nhiều vật đặt gần nhau trên lưới isometric (cellSize 1x0.5 nhỏ hơn sprite) — cần art/sprite thật theo góc isometric ở Milestone 7, không phải lỗi logic
