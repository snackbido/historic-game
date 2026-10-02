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
- [x] Enemy AI cơ bản (patrol/chase/attack) — `Combat/PredatorAI.cs` + `PredatorData` (sói), M5.5 2026-09-30
- [x] Vũ khí cơ bản (giáo/đá ném) — `Combat/PlayerCombat.cs` (F đâm giáo, R ném đá), M5.5 2026-09-30
- [x] Chốt thiết kế chỉ huy (2026-09-30): **mọi NPC đều nghe lệnh**; **kéo khung chuột** để chọn NPC, mỗi NPC có **nghề nghiệp riêng** (trinh sát, thợ săn, nông dân, dân làng…) quyết định lệnh nó thực hiện được; UI hiển thị nhóm đang chọn **theo nhóm và theo nghề** của từng NPC
- [x] Chốt chi tiết (2026-09-30):
  - Nhóm lẫn nhiều nghề nhận lệnh mà có con không làm được → con làm được thì làm, con còn lại **đi theo tới chỗ đó**
  - Dân số: bắt đầu **3–4 NPC có cả nam lẫn nữ**; một cặp nam + nữ trưởng thành **sinh em bé**, em bé **lớn dần theo thời gian** (em bé → trẻ em → người lớn), chỉ người lớn mới làm việc/nhận lệnh. Chi tiết tự đề xuất (chỉnh được): cặp đôi cố định; sinh con khi còn chỗ ở (mỗi lều +2 chỗ) và đủ thức ăn (tốn thức ăn); thời gian lớn lên cấu hình trong data
  - Người mới trưởng thành là "Dân làng", người chơi **đổi nghề qua UI**
  - Khi không có lệnh, NPC **tự làm việc theo nghề** ở gần (vd nông dân tự thu hoạch ô đã chín)
- [x] M5.2 Chọn + ra lệnh di chuyển (2026-09-30): `Npc/SelectionManager.cs` — kéo khung/click chọn (Shift thêm/bỏ), chuột phải đi tới (dàn lưới, không chồng nhau), Ctrl+1..9 lưu nhóm / 1..9 gọi nhóm, vòng chọn dưới chân; NPC đã nhận lệnh đứng giữ vị trí (lưu trong save)
- [x] M5.3 Ra lệnh theo nghề (2026-09-30): chuột phải lên cây → chặt (dân làng/nông dân/thợ săn); ô đất → làm ruộng liên tục (nông dân); thú hoang → săn (thợ săn) hoặc thuần hóa (nông dân); thú thuần → chăm (nông dân); ai không làm được thì đi theo
- [x] M5.4 UI nhóm/nghề + đổi nghề (2026-09-30): bảng "Đang chọn" (tên + giới tính, ô theo nghề bấm để lọc, nút đổi nghề cho cả nhóm), thanh máu trên đầu NPC/thú
- [x] M5.6 Dân số (2026-09-30): cặp đôi nam–nữ cố định, sinh con khi còn chỗ ở (4 ban đầu + 2/lều) và có 5 thức ăn, em bé (60s) → trẻ em (90s) → người lớn thành Dân làng; nhãn "Dân số: x/y"
- [x] M5.6 Tự làm việc khi rảnh (2026-09-30): theo `ProfessionData.autoWork` — dân làng chặt cây, nông dân làm ruộng/chăm thú, thợ săn canh gác, trinh sát đi dạo

## Milestone 5b — Công trình nâng cấp + kinh tế lương thực (thêm 2026-09-30, trước Thiên tai)
Quyết định user: công trình nâng cấp **5 cấp** (click công trình → bảng thông tin + nút Nâng cấp); **lều là nhà riêng của từng cặp đôi**, chỉ sinh con tại lều; lương thực tách **nhiều loại** (Thịt, Lúa gạo, Quả mọng, Sữa, Cá, Rau…); kho có **giới hạn lưu trữ**, **thịt để ngoài kho bị hỏng**, **dân làng ăn hằng ngày**.
- [x] E1 Hệ thống nâng cấp 5 cấp cho Lều + Kho (chi phí, model đổi theo cấp, bảng thông tin khi click công trình, lưu/tải cấp) — 2026-09-30
- [x] E2 Lều là nhà: gán cặp đôi vào lều, cặp đôi về lều để sinh con, cấp lều → thêm chỗ cho con + sinh nhanh hơn — 2026-10-01
- [x] E3 Nhiều loại lương thực + nguồn mới (lúa, rau = cây trồng mới; cá = đánh cá; sữa = vật nuôi mới; thịt = săn/vật nuôi); chi phí "thức ăn" nhận loại nào cũng được — 2026-10-01
- [x] E4 Kho: giới hạn lưu trữ theo loại (không kho ~20/loại, kho + cấp kho tăng), thịt ngoài kho hỏng dần — 2026-10-01
- [x] E5 Dân làng ăn hằng ngày: thiếu ăn → đói, yếu, không sinh con — 2026-10-01 (**Milestone 5b hoàn tất**)

## Milestone 5c — Ngày & đêm (thêm 2026-10-01)
Quyết định user: một ngày **20 phút** thời gian thật (~14 phút ngày, ~6 phút đêm); **chỉ thể hiện bằng ánh sáng, không có chữ/đồng hồ**; ban đêm: **dân làng về lều ngủ** (người chưa có nhà ngủ quanh đống lửa; ra lệnh vẫn đánh thức được; cặp đôi sinh con về đêm tại lều), **sói rời hang lùng sục rộng hơn**, **cây trồng ngừng lớn**, **đống lửa trại chiếu sáng** (sói không dám lại gần lửa).
- [x] N1 Chu kỳ ngày/đêm: mặt trời quay, đổi màu/độ sáng, trời tối; cây ngừng lớn ban đêm; đống lửa trại tự cháy khi tối; lưu/tải giờ trong ngày — 2026-10-01
- [x] N2 Ngủ: tối về lều (ẩn vào trong) / ngủ quanh đống lửa, sáng thức dậy, ngủ thì hồi máu, ra lệnh đánh thức; sinh con về đêm tại lều
- [x] N3 Sói ban đêm: lùng sục rộng, mò vào trại, né đống lửa; sáng về hang; người ngủ trong lều an toàn

## Milestone 5d — Canh tác thực tế (thêm 2026-10-01)
Quyết định user: quy trình **vừa phải** (lúa ~6 bước: làm đất → dẫn nước → gieo mạ → cấy → chăm → gặt + giã); **người chơi tự đặt ruộng** qua menu xây dựng; nước **đủ 3 giai đoạn** (gánh nước → mương → guồng nước, mở dần theo tech); **nông dân tự làm bước kế tiếp**, người chơi vẫn ra lệnh được.
- [x] F1 Xây ruộng: "Ruộng cạn" + "Ruộng nước" trong menu xây; ruộng mới là đất hoang → nông dân khai hoang/đắp bờ; ruộng nước phải gần nguồn nước; lúa chỉ trồng ruộng nước, rau/quả mọng ruộng cạn. 2 ô vườn có sẵn giữ lại (đã khai hoang)
- [x] F2 Làm đất + nước: cày/xới trước mỗi vụ; ruộng có mức nước (bốc hơi dần, ruộng nước cần ngập mới cấy/lớn, ruộng cạn thiếu nước thì ngừng lớn rồi héo); việc mới "gánh nước" từ ao; công trình Giếng
- [x] F3 Gieo mạ & cấy: ô ươm mạ (gieo thóc giống → mạ), nhổ mạ, cấy vào ruộng nước đã cày + ngập; giữ thóc giống sau gặt
- [x] F4 Chăm sóc: cỏ dại mọc giảm năng suất → làm cỏ; bón phân (phân từ vật nuôi) tăng năng suất
- [x] F5 Sau gặt: lúa gặt về là bó lúa → phơi/tuốt/giã ở công trình Cối giã → gạo ăn được
- [x] F6 Mương dẫn nước (tech Thủy lợi): đào mương từ ao, ruộng cạnh mương tự có nước
- [x] F7 Guồng nước (tech): đặt bên ao/đầu mương, bơm nước mạnh → ruộng nối mương luôn đầy nước

## Milestone 6 — Thiên tai (chốt với user 2026-10-01: cả 4 loại, tần suất vừa phải, thiệt hại sửa được)
- [x] D1 Khung: `DisasterManager` (ngày đầu an toàn, sau đó 2–3 ngày/lần, báo trước nửa ngày) + `DisasterEvent` base + băng cảnh báo + công trình hư hại/sập → dân làng sửa (tốn gỗ) + lưu/tải
- [x] D2 Hạn hán: ruộng bốc hơi nhanh, ao cạn dần (ít cá, mương/guồng yếu) — chống bằng giếng, mương, guồng
- [x] D3 Lũ lụt: mưa lớn, ao tràn — ruộng/công trình gần ao ngập (cây chết úng, nhà hư) — chống bằng Đê (công trình mới)
- [x] D4 Cháy rừng: sét/lửa lan qua cây và lều — dân gánh nước dập lửa
- [x] D5 Bầy sói đột kích: đêm cả bầy tràn vào trại theo đợt — chống bằng hàng rào, lính gác, đuốc

## Milestone 7 — Polish & mở rộng (bắt đầu 2026-10-02; chia bước P1–P4, tự chọn mặc định vì user chưa có asset)
- [x] P1 Hiệu ứng hình ảnh: particle dựng bằng code (`VfxManager`) cho chặt/đánh cá/cuốc/gặt/xây/sửa/sập/đánh nhau/dập lửa + mưa khi lũ, chớp khi sét
- [x] P2a Nhạc nền đơn giản sinh bằng code (ngày/đêm) — user sẽ tự thay bằng nhạc riêng
- [x] P2b Hiệu ứng âm thanh sinh bằng code (14 tiếng), thay được bằng file riêng
- [x] P3 Menu chính + tạm dừng (Esc) + cài đặt (âm lượng nhạc/tiếng động, pixel, toàn màn hình)
- [ ] P4 Cân bằng (đầu game 0 lương thực, chuỗi lúa dài mà ít gạo, thiên tai 2–3 ngày/lần có dày quá?) + lưu cây đã chặt — cần user chơi thử

## Nhật ký quyết định quan trọng (Decision Log)
> Ghi lại các quyết định kỹ thuật/thiết kế lớn để không quên lý do tại sao chọn hướng này.

| Ngày | Quyết định | Lý do |
|------|-----------|-------|
| 2026-10-01 | Thêm Milestone 5d Canh tác thực tế (F1–F7) trước Thiên tai: ruộng do người chơi xây, quy trình nhiều bước, nước 3 giai đoạn (gánh → mương → guồng nước thay cho "bơm" cho hợp thời tiền sử) | Chốt với user; nông nghiệp là trục chính của game, thiên tai (lũ, hạn) sau này sẽ tác động lên hệ thống nước/ruộng này |
| 2026-09-21 | Chọn Unity + C#, IDE VS Code | Nhiều tài nguyên học, asset store phong phú cho thể loại survival/building; VS Code quen thuộc với nền web dev |
| 2026-09-21 | Bắt đầu bằng 2D top-down | Dễ quản lý hơn 3D khi làm một mình, chưa có kinh nghiệm Unity |
| 2026-09-21 | Đặt công trình theo grid-based (không free-placement) | Dễ quản lý va chạm/chồng lấn, dễ tính toán, phù hợp người mới học Unity |
| 2026-09-21 | Bỏ qua `InputManager` riêng ở Milestone 1 | Chỉ có 2 loại input ở MVP, thêm tầng trừu tượng ngay là over-engineering; sẽ tách khi cần |
| 2026-09-22 | Chuyển `ResourceType` từ enum sang ScriptableObject ở Milestone 2 | Farming sẽ thêm nhiều loại tài nguyên/cây trồng; đúng nguyên tắc data-driven trong ARCHITECTURE.md, tránh sửa code mỗi khi thêm loại tài nguyên mới |
| 2026-09-22 | Thuần hóa thú hoang ở Milestone 3: cho ăn (tốn tài nguyên) thay vì tương tác phím đơn thuần | Gần sát mô tả SPEC.md hơn, tái dùng pattern chi phí `ResourceAmount` đã có ở `BuildingData`, tránh thuần hóa quá dễ dàng |
| 2026-09-22 | Tài nguyên "tri thức" (Milestone 4) tự sinh theo thời gian (tốc độ cố định), không gắn vào hành động gameplay | Đơn giản nhất, không phụ thuộc EventBus của các hệ thống khác; dễ cân bằng lại tốc độ sau này |
| 2026-09-22 | Refactor `BuildingPlacer` từ 1 field `buildingToPlace` sang registry nhiều `BuildingData` + `SelectBuilding()` | Cần thiết để Tech Tree mở khóa được nhiều loại công trình khác nhau; đã ghi nợ từ Milestone 1 |
| 2026-09-26 | Thứ tự triển khai tiếp theo: trả nợ kỹ thuật (feedback UX vật nuôi, save/load FarmPlot + Animal, test tay input) **trước** khi bắt đầu Milestone 5 | Đảm bảo M1-4 vững chắc, không cộng dồn nợ kỹ thuật trước khi mở rộng sang hệ thống mới |
| 2026-10-01 | Thêm Milestone 5c Ngày & đêm: ngày 20 phút, chỉ ánh sáng (không đồng hồ); ban đêm dân làng ngủ (lều/đống lửa), sinh con về đêm, sói đi săn rộng, cây ngừng lớn, đống lửa chiếu sáng + đuổi sói | Chốt với user; tạo nhịp sinh hoạt và rủi ro ban đêm, gắn với lều (E2) và sói (M5.5) |
| 2026-09-30 | Thêm Milestone 5b: công trình nâng cấp 5 cấp; lều = nhà của cặp đôi (chỉ sinh con tại lều); lương thực nhiều loại (thịt, lúa gạo, quả mọng, sữa, cá, rau…); kho giới hạn lưu trữ, thịt ngoài kho hỏng, dân làng ăn hằng ngày | User muốn công trình có chức năng riêng và nâng cấp được; làm kho/lương thực có ý nghĩa thật trong kinh tế. Chia 5 bước E1–E5 vì khối lượng lớn |
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

## Nhật ký phiên làm việc 2026-09-30 (phần 8 — M5.5 thú dữ + chiến đấu)
- Mới `Data/PredatorData.cs` + asset `PredatorData_Wolf` (Sói: 50 máu, cắn 8/1,2s, đuổi 3,8 m/s — nhanh hơn dân làng, chậm hơn trinh sát; phát hiện 4,5m; không đuổi xa hang quá 12m; rơi 4 thức ăn). Prefab `Assets/Prefabs/Enemies/Wolf.prefab` (xám, tai nhọn, mắt vàng phát sáng, thanh máu).
- Mới `Combat/PredatorAI.cs`: Patrol (đi tuần 2m quanh hang) → Chase → Attack; chọn mục tiêu (NPC + player) 0,3s/lần; bị đánh thì quay sang kẻ tấn công; mục tiêu ra khỏi phạm vi 12m quanh hang thì bỏ cuộc về hang. `Combat/PredatorManager.cs` lưu/tải thú dữ (`saveVersion` 4, gồm máu).
- Hang sói ở (-10, 9,5) — cách trại ~14m, đi tuần 2m + phát hiện 4,5m → **sói không tự tràn vào trại**, người chơi chủ động đi săn. Tấn công trại theo đợt để dành cho M6 (thiên tai/đột kích).
- `HealthComponent`: `TakeDamage(amount, source)` + sự kiện `OnDamaged`; hồi máu `regenPerSecond` sau `regenDelay` (6s) không bị đánh — dân làng và người chơi hồi 1 máu/giây.
- NPC: bị thú dữ cắn → nghề biết đánh (Fight/Hunt: dân làng, thợ săn, trinh sát) tự đánh trả (`AttackJob`), nông dân bỏ chạy 6m; chết → thông báo "X đã chết!" và biến mất (rời nhóm đang chọn). Chuột phải lên sói: người biết đánh xông vào, **người không biết đánh đứng yên** (ngoại lệ có chủ đích của quy tắc "đi theo" — không đưa họ vào chỗ nguy hiểm).
- `PlayerCombat`: F đâm giáo (15 sát thương, tầm 1,8m, hồi 0,8s), R ném đá (8, tầm 8m, hồi 1,5s, có viên đá bay), tự nhắm sói gần nhất; bị hạ → về trại đầy máu. Dòng gợi ý dưới màn hình nhắc phím khi có sói trong tầm ném.
- `TestResultReporter.RunPlayModeTestGroup(tên lớp)` — chạy riêng một lớp test (nhẹ bộ nhớ hơn). Gọi thẳng TestRunnerApi qua MCP bị chặn ("user interactions not supported") → luôn gọi qua `TestResultReporter`.
- **Đã xác nhận**: **41/41 test PlayMode pass** (9 test mới `Milestone5CombatTests.cs`); ảnh render xác nhận model sói + hang.
- **Sự cố bộ nhớ lặp lại** (2 crash trong phần này): Unity ~4,6GB + Edge ~4,1GB + VS Code ~1,8GB trên máy 8GB, pagefile vẫn trên ổ C → chỉ chạy test ổn khi đóng Edge. Vẫn khuyến nghị chuyển pagefile sang ổ D.
- **Chưa xác nhận bằng tay**: cảm giác chiến đấu (phím F/R, sói đuổi), cân bằng số liệu.

## Nhật ký phiên làm việc 2026-09-30 (phần 9 — M5.6 tự làm việc + dân số) — **Milestone 5 hoàn tất**
- **Tự làm việc khi rảnh**: `ProfessionData.autoWork` (cờ `NpcCapability`, data-driven): dân làng = Gather, nông dân = Farm + TendAnimals, thợ săn = Hunt (canh gác: sói đang đuổi/cắn ai trong 10m thì lao vào), trinh sát = None. `NpcJobFactory.FindAutoJob` tìm việc trong 6m quanh NPC, 2s/lần, chỉ khi người lớn, không bị chọn, không giữ vị trí; không tranh việc NPC khác đang làm. `FarmJob(continuous: false)` khi tự làm: 1 thao tác (gieo/thu/dọn) rồi tìm việc khác → nông dân chia đều nhiều ô. Việc tự làm/tự vệ xong thì đi dạo tiếp (không giữ vị trí như lệnh người chơi).
- **Dân số** (`NpcManager`): ghép cặp nam–nữ người lớn chưa có cặp (Ka–Mây, Đá–Suối), cặp cố định (một người chết thì người kia ghép cặp mới). 20s/lần: nếu dân số < sức chứa (4 + `BuildingData.housing` của mọi công trình — lều = 2) và đủ 5 thức ăn → một cặp (nghỉ 90s giữa hai lần) sinh em bé, tên lấy từ danh sách không trùng. Em bé 60s → trẻ em 90s → người lớn = Dân làng (thông báo "đã trưởng thành — có thể giao nghề"). Trẻ con nhỏ hơn, chậm hơn, không cầm dụng cụ, không chọn/ra lệnh được, không tự làm việc. Lưu/tải: tuổi, thời gian lớn, cặp đôi (nối lại theo tên). `BuildingInstance.All` để đếm công trình. Mới `UI/PopulationUI.cs` ("Dân số: 5/6 (1 trẻ em)").
- Test: `PlayModeTestBase` tắt `NpcController.AutoWorkEnabled` + `NpcManager.BirthsEnabled` mặc định (test cũ không bị NPC tự làm việc chen ngang), test M5.6 tự bật lại.
- **Đã xác nhận**: **49/49 test PlayMode pass** (8 test mới `Milestone5PopulationTests.cs`: dân làng tự chặt cây không giữ vị trí; nông dân tự gieo + thu hoạch; người đang được chọn không bị kéo đi làm; thợ săn canh gác lao vào sói đang tấn công; ghép cặp nam–nữ; sinh con cần chỗ ở + thức ăn, mỗi lều +2, hết chỗ thì dừng; em bé lớn thành dân làng chọn được; lưu/tải giữ em bé + cặp đôi).
- **Chưa xác nhận bằng tay**: nhịp độ (có thể dân làng chặt hết 8 cây khá nhanh vì tự làm việc — cần user chơi thử để cân bằng), hiển thị trẻ con.

## Nhật ký phiên làm việc 2026-09-30 (phần 10 — giãn nhịp độ)
- User chơi thử: "nhịp độ khá nhanh" → giãn các nhịp chính ~2 lần: chặt cây 1 gỗ/3s (trước 1,5s); cây mọng 12/24/40s (trước 5/10/20); tri thức 1 điểm/6s (trước 3s); heo thuần ra sản phẩm 30s, sinh sản 90s (trước 15/45); xét sinh con 40s, mỗi cặp nghỉ 180s (trước 20/90); em bé 120s → trẻ em 180s (trước 60/90). Test M4 chờ tri thức 45s thay vì 20s. **49/49 test pass**.
- Hướng cân bằng tiếp nếu vẫn thấy nhanh/chậm: sửa trực tiếp số trong asset/Inspector (data-driven), không cần sửa code; cân nhắc thêm cây mọc lại để dân làng tự chặt không làm trụi bản đồ.

## Nhật ký phiên làm việc 2026-09-30 (phần 11 — M5b/E1 nâng cấp công trình)
- `BuildingData.levels` (lớp mới `BuildingLevel`: tên, chi phí nâng lên, chỗ ở, sức chứa lương thực, công nghệ cần — hiện chưa gắn tech nào) + `functionDescription`; bỏ field `housing` cũ. Lều: Lều da (2 chỗ) → Lều da lớn (3, 15 gỗ) → Nhà lá (4, 25 gỗ + 5 tri thức) → Nhà sàn (5, 40 + 10) → Nhà dài (6, 60 + 20). Kho: Kho chứa (30/loại) → Kho lớn (60, 25 gỗ) → Lẫm lúa (100, 40 + 5) → Hầm chứa (150, 60 + 10) → Kho lương (220, 90 + 20).
- `BuildingInstance`: `Level`, `TryUpgrade()`/`UpgradeBlocker()`, model con `Level1..5` bật theo cấp, `SelectionRing`, danh sách `All`; sức chứa dân số tính theo cấp lều. `PlacedBuildingData.level` (save cũ = cấp 1). `BuildingPlacer.PlaceBuilding` thành public.
- Chọn công trình: `SelectionManager.SelectAt` không trúng người → raycast vật lý (BoxCollider của công trình) → `SelectBuilding`; chọn người thì bỏ chọn công trình và ngược lại; click chỗ trống bỏ chọn cả hai. Mới `UI/BuildingInfoPanelUI.cs` (tên + cấp, chức năng, lợi ích/chi phí cấp kế, lý do chưa nâng được, nút Nâng cấp — tự bật/tắt theo tài nguyên), cùng chỗ với bảng "Đang chọn". `EventBus.OnBuildingSelected/OnBuildingUpgraded`.
- Bẫy lặp lại: sau khi bake NavMesh (tạo asset), **mọi** tham chiếu component-prefab nạp trước đó mất hiệu lực (lần này là `Button.prefab`) → nạp lại ngay trước khi dùng.
- **Đã xác nhận**: **53/53 test PlayMode pass** (4 test mới `Milestone5bBuildingTests.cs`); ảnh render 5 cấp lều + 5 cấp kho phân biệt rõ.

## Nhật ký phiên làm việc 2026-10-01 (M5b/E2 lều là nhà)
- Bỏ "sức chứa dân số" chung (4 + chỗ ở). Giờ **mỗi lều = nhà của một cặp đôi**: `NpcManager.AssignHomes()` gán lều chưa có chủ cho cặp chưa có nhà (chạy khi xây lều — `EventBus.OnBuildingPlaced` — và mỗi lượt xét sinh con); người tái hôn mang người kia về lều cũ của mình. `BuildingLevel.housing` đổi nghĩa: **số con nhỏ tối đa** gia đình nuôi được (lều cấp 1 = 2 … cấp 5 = 6).
- Sinh con chỉ tại lều: `TryStartBirth()` chọn cặp đủ điều kiện (`BirthBlocker`: có cặp, có lều, lều còn chỗ cho con, hết thời gian nghỉ, đủ thức ăn) và **đang rảnh** (không được chọn, không giữ vị trí theo lệnh, không đánh nhau) → cả hai nhận `HomeVisitJob` về lều; ở cùng nhau 4s thì `CompleteBirth` trừ 5 thức ăn và em bé ra đời trước cửa lều, thuộc về lều đó (`NpcController.Home`). Bỏ cuộc nếu chờ quá 45s. Thời gian nghỉ giữa hai lần sinh giảm 10% mỗi cấp lều (180s → 108s ở cấp 5).
- Trẻ con chơi quanh lều nhà mình; trưởng thành thì `Home = null` (ra ở riêng, lập gia đình mới cần lều mới). Lưu/tải nhà theo ô grid của lều (`NpcSaveData.hasHome/homeCellX/homeCellY`, nối lại sau khi tải công trình).
- UI: nhãn dân số "Dân số: 5 (1 trẻ em) · Nhà: 1/2 cặp"; bảng công trình lều hiện "Chỗ cho con: N" + "Gia đình: Ka & Mây — 1/2 con" (hoặc "Lều trống").
- **Sự cố**: Unity crash lúc chạy test nhóm NPC qua MCP (bộ nhớ ~0,5GB) và **ghi hỏng `LiberationSans SDF - Fallback.asset`** (toàn ký tự rỗng → test lỗi "File is either empty or corrupted"). Đã khôi phục bản trong git (chỉ là bộ đệm ký tự font, Unity tự thêm lại) → file này hết bị "modified" luôn.
- **Đã xác nhận**: **59/59 test PlayMode pass** (chạy batch mode khi Editor đã tắt, 4,9GB trống); 6 test mới `Milestone5bHomeTests.cs` (xây lều → một cặp có nhà; cặp đôi về lều, em bé ra đời ở cửa lều; lều đủ con chặn sinh tới khi nâng cấp; lều cấp 5 nghỉ ngắn hơn; con trưởng thành ra ở riêng; lưu/tải giữ nhà).

## Nhật ký phiên làm việc 2026-10-01 (phần 2 — M5b/E3 nhiều loại lương thực)
- `ResourceTypeData`: thêm `category` (Material/Food/Knowledge), `isFoodPool` + `poolDefault`, `perishable`. 6 loại lương thực mới (asset `ResourceType_Meat/Rice/Berries/Milk/Fish/Vegetables`): Thịt*, Lúa gạo, Quả mọng, Sữa*, Cá*, Rau (* = dễ hỏng). Asset cũ `ResourceType_Food` (id "food") thành **loại gộp "Thức ăn"** = tổng mọi loại → mọi chi phí/test đang dùng nó vẫn chạy; trả chi phí gộp thì dùng đồ dễ hỏng trước, rồi loại đang nhiều nhất; cộng vào loại gộp = cộng vào Quả mọng. Loại gộp không lưu trong save; save cũ có "food" → chuyển thành Quả mọng.
- Nguồn: cây mọng → Quả mọng; **cây Lúa** mới (20/40/60s, thu 6 Lúa gạo, mở bằng công nghệ mới **"Trồng lúa"** — 10 tri thức, cần Nông nghiệp); **cây Rau** mới (8/16/30s, thu 2 Rau, mở cùng Nông nghiệp); heo rừng → Thịt (sản phẩm 2, săn 6); sói → Thịt 4; **Dê núi** mới (cho ăn 3 lần để thuần, ra 2 Sữa / 45s, săn được 4 Thịt) đặt ở (-5, -6,5); **Ao cá** mới ở (4,5; 6) — `ResourceNode` không biến mất khi cạn, cá hồi 1 con / 20s (tối đa 8), dân làng đứng trên bờ đánh cá (`workRange` 1,7m), người chơi bấm E.
- `ResourceNode`: `actionName` ("Chặt cây"/"Đánh cá"), `workRange`, `regenInterval`/`maxAmount` (sinh sôi lại — dùng lại được cho cây mọc lại sau này). `GatherJob` dừng khi nguồn cạn; tự làm việc bỏ qua nguồn đã cạn.
- UI: dòng chi tiết dưới "Thức ăn: N" (`FoodBreakdownUI`: "Thịt 3 · Lúa gạo 0 · Quả mọng 6 · Sữa 0 · Cá 2 · Rau 0"); bảng hạt giống 3 cây; bảng công nghệ 2 mục. Đổi tên dữ liệu sang tiếng Việt có dấu: Gỗ, Tri thức, Heo rừng, Cây mọng, Nông nghiệp, Lều, Kho.
- **Đã xác nhận**: **67/67 test PlayMode pass** (8 test mới `Milestone5bFoodTests.cs`; sửa `SaveLoadTests` vì giờ bản đồ có 2 con vật); ảnh render lúa chín, rau, dê núi, ao cá.

## Nhật ký phiên làm việc 2026-10-01 (phần 3 — M5b/E4 kho chứa)
- `ResourceManager`: mỗi loại lương thực có sức chứa `FoodCapacity` = `baseFoodCapacity` (20, đống tạm ngoài trời) + `StoredFoodCapacity` (tổng `StorageCapacity` của mọi kho theo cấp: 30/60/100/150/220). `AddResource` lương thực vượt sức chứa → phần dư bị phí + thông báo "Kho đầy — phí N …" (mỗi loại tối đa 1 thông báo / 5s). Gỗ/tri thức không giới hạn.
- Hư hỏng: đồ `perishable` (thịt, cá, sữa) — phần vượt quá `StoredFoodCapacity` là "để ngoài kho"; cứ `spoilInterval` 15s mất `spoilFraction` 25% phần đó (ít nhất 1) + thông báo. Phần trong kho không hỏng; lúa gạo, quả mọng, rau không bao giờ hỏng.
- `FoodBreakdownUI`: "Sức chứa: 50/loại · Thịt 12 (hỏng dần) · …", làm mới khi xây/nâng cấp công trình.
- Test: `PlayModeTestBase` tắt `ResourceManager.SpoilageEnabled` mặc định (test cũ không bị lệch số), test E4 tự bật. **72/72 test PlayMode pass** (5 test mới `Milestone5bStorageTests.cs`: không kho giữ tối đa 20/loại, phần dư phí; kho cộng sức chứa theo cấp; thịt ngoài kho hỏng; thịt trong kho + lúa không hỏng; chỉ phần vượt sức chứa kho hỏng, hỏng tới đúng mức kho bảo quản thì dừng).

## Nhật ký phiên làm việc 2026-10-01 (phần 4 — M5b/E5 ăn uống) — **Milestone 5b hoàn tất**
- `NpcController.Fullness` (độ no 0–100, lưu trong save; save cũ = no đủ), `IsStarving` (= 0) → đi chậm ×0,6. `NpcManager.UpdateHunger(dt)`: người lớn mất 0,67 độ no/giây (trẻ con ×0,5); dưới 60 thì ăn 1 bữa (`mealCost` = 1 Thức ăn gộp → loại nào cũng được, đồ dễ hỏng trước) +40 → mỗi người lớn ~1 bữa/phút. Đói: mất 1 máu / 3s (có thể chết), không sinh con (`BirthBlocker` "Đang đói"), thông báo "Làng đang thiếu ăn! N người đói" mỗi 20s. UI: nhãn dân số "… · ĐANG ĐÓI: N", bảng "Đang chọn" đánh dấu "(đói)".
- Test: `PlayModeTestBase` tắt `NpcManager.HungerEnabled` mặc định; test E5 gọi thẳng `UpdateHunger(giây)` để kết quả chắc chắn. **79/79 test PlayMode pass** (7 test mới `Milestone5bHungerTests.cs`).
- **Rủi ro cân bằng chưa xử lý**: game bắt đầu với 0 thức ăn → sau ~2,5 phút cả làng đói, ~4,5 phút sau bắt đầu chết nếu người chơi chưa kiếm được thức ăn (đánh cá ở ao, săn heo, mở Nông nghiệp để trồng). Cân nhắc cho kho ban đầu một ít lương thực (vd 20 quả mọng) — cần chơi thử để quyết.

## Nhật ký phiên làm việc 2026-10-01 (phần 5 — M5c/N1 chu kỳ ngày/đêm)
- Mới `Core/DayNightCycle.cs`: `TimeOfDay` 0..1 (0 = bình minh), ngày 1200s, đêm từ 0,7 (30% = 6 phút), hoàng hôn/bình minh 4% chu kỳ (`Daylight` 1→0 mượt). Đổi hướng/màu/độ sáng mặt trời (ban ngày lên cao rồi hạ, hoàng hôn cam, đêm ánh trăng xanh 0,18), ánh sáng môi trường, màu trời + sương mù. Không có chữ/đồng hồ (theo quyết định). `SetTime(t, day)`; `EventBus.OnNightStarted/OnDayStarted`. Lưu/tải `timeOfDay` + `day` (`saveVersion` 5).
- Mới `Building/Campfire.cs` + đống lửa giữa trại (0, -2): vòng đá, củi, ngọn lửa phát sáng, đèn điểm cam bập bùng — tự cháy khi `Daylight` < 0,6; `SafeRadius` 6m + `Campfire.IsProtected(pos)` chuẩn bị cho N3 (sói né lửa).
- `FarmPlot`: ban đêm không lớn (cũng không héo).
- Test: `PlayModeTestBase` dừng chu kỳ (`DayNightCycle.Running = false`) và đặt giữa trưa (0,3). 5 test mới `Milestone5cDayNightTests.cs`. Ảnh render ban đêm (trại tối xanh, chỉ ánh lửa cam) + chạng vạng (bóng dài, lửa vừa nhóm).
- Sửa test chập chờn `AfterOrder_DeselectedVillager…`: chờ theo điều kiện thay vì mốc cố định (đống lửa mới làm đường đi dài/ngắn khác nhau). **84/84 test PlayMode pass** (batch mode khi Editor đã tắt — ổn định hơn nhiều so với chạy trong Editor đang mở; chạy trong Editor lại crash vì còn 0,2GB bộ nhớ).

## Nhật ký phiên làm việc 2026-10-01 (phần 6 — M5c/N2 ngủ)
- `SleepJob` (thay `HomeVisitJob`): tối đến, người rảnh (không bị chọn, không giữ lệnh) về lều của mình và "vào trong" (ẩn mọi Renderer); người chưa có lều nằm ngủ quanh đống lửa (xoay `Visual` nằm xuống). Ngủ hồi 1,5 máu/s. Trời sáng → dậy. Đang nằm cạnh lửa mà được chia lều → chuyển về lều. Trẻ con cũng ngủ.
- Ban đêm việc tự làm (chặt cây, làm ruộng…) dừng lại; lệnh của người chơi và tự vệ (`AttackJob`) vẫn tiếp tục. Ra lệnh / bị tấn công (bỏ chạy) thì thức dậy.
- Sinh con chỉ về đêm: `NpcManager.TryNightBirth()` — cặp đủ điều kiện mà cả hai đang ngủ trong lều chung → em bé ra đời (bỏ `TryStartBirth` + hẹn về lều ban ngày).
- Người ngủ trong lều: không chọn được bằng chuột (click/kéo khung), sói không nhắm tới (`PredatorAI.IsValidTarget`).
- Test: 6 test mới `Milestone5cSleepTests.cs`; viết lại test sinh con trong `Milestone5bHomeTests`/`Milestone5PopulationTests` theo ban đêm; test đói đặt đầy máu trước khi đo 30s (máu tối đa tùy nghề). **90/90 test PlayMode pass** (batch mode).

## Nhật ký phiên làm việc 2026-10-01 (phần 7 — M5c/N3 sói ban đêm) → Milestone 5c xong
- `PredatorData`: thêm `nightPatrolRadius` 14, `nightAggroRange` 7, `nightLeashRange` 26 (ngày vẫn 2 / 4,5 / 12). `PredatorAI.PatrolRadius/AggroRange/LeashRange` đổi theo ngày/đêm; ban đêm nghỉ giữa các chặng ngắn hơn (1–3s).
- Né lửa: điểm đi tuần nằm trong vùng sáng của đống lửa đang cháy bị bỏ qua; người đứng trong vùng sáng không bị nhắm/đuổi (`Campfire.IsProtected`); sói lọt vào vùng sáng thì lùi ra ngoài `SafeRadius + 2`.
- Trời sáng: sói đang lùng sục quay thẳng về hang. Người ngủ trong lều đã an toàn từ N2.
- Test: 4 test mới `Milestone5cWolfNightTests.cs`. **94/94 test PlayMode pass** (batch mode).
- Lưu ý cân bằng: đống lửa (0,-2) bán kính 6 che luôn lều đầu tiên ở (1.5,-5.5) → làng nhỏ ban đêm gần như an toàn; nguy hiểm chủ yếu với người được ra lệnh làm việc xa trại về đêm. Cần chơi thử để xem có nên thu nhỏ vùng an toàn không.

## Nhật ký phiên làm việc 2026-10-01 (phần 8 — M5d/F1 xây ruộng)
- Công trình mới "Ruộng cạn" (`dry_field`, 2 gỗ, mở sẵn) + "Ruộng nước" (`paddy_field`, 4 gỗ, mở cùng tech Trồng lúa, phải cách mép ao ≤ 3m). Prefab 1×1 ô: `BuildingInstance` + `FarmPlot`, collider trigger (đi xuyên được, click xem thông tin), hình đất hoang (cỏ, đá) ↔ đất đã làm (ruộng cạn: luống; ruộng nước: bùn + bờ đắp 4 phía).
- `FarmPlotState.Wild` (thêm cuối enum để save cũ không lệch): ruộng mới là đất hoang → `DoClearWork()` mỗi lượt công (ruộng cạn 8, ruộng nước 12 lượt). Nông dân tự khai hoang khi rảnh (làm đến xong), ra lệnh được; người chơi bấm E cũng khai hoang. 2 ô vườn có sẵn trong scene giữ nguyên (đã khai hoang).
- `CropData.fieldType` (lúa = ruộng nước, rau/quả mọng = ruộng cạn); `FarmPlot.Accepts`. `FarmManager` nhớ hạt giống chọn gần nhất **cho từng loại ruộng** (`CropFor(plot)`): chọn Lúa rồi Quả mọng → ruộng nước cấy lúa, ruộng cạn trồng quả mọng.
- Mới `Farming/WaterSource.cs` (ao cá có bán kính mặt nước 0,95). `BuildingData.requiresWaterWithin`; `BuildingPlacer.PlacementBlocker()` nêu lý do không đặt được (đã có công trình / chưa mở khóa / đè lên mặt nước / xa nguồn nước / thiếu tài nguyên) và hiện thông báo khi click sai chỗ. Công trình đặt ra được đặt tên theo ô (`{id}_{x}_{y}`) để lưu/tải ô ruộng khớp đúng; lưu tiến độ khai hoang (`clearWorkDone`).
- Test: 6 test mới `Milestone5dFieldTests.cs`; test lúa cũ chuyển sang ruộng nước cạnh ao. **100/100 test PlayMode pass**. Ảnh render xác nhận hình đất hoang/ruộng cạn/ruộng nước có bờ cạnh ao.

## Nhật ký phiên làm việc 2026-10-01 (phần 9 — M5d/F2 làm đất + nước)
- `FarmPlotState.Unplowed` (thêm cuối enum): khai hoang xong, thu hoạch xong, dọn cây héo xong → đất chưa cày; `DoPlowWork()` (ruộng cạn 3, ruộng nước 4 lượt) → `Empty` mới gieo được. 2 ô vườn có sẵn bắt đầu đã cày + đầy nước.
- Mức nước `Water` 0..1 mỗi ô: bốc hơi 1/150 mỗi giây (ban đêm 30%). Ruộng nước phải ngập ≥ 0,6 (`FloodedLevel`) mới cấy lúa; `CropData.minWater` (lúa 0,3, còn lại ~0) — dưới mức này cây ngừng lớn, khô liên tục 60s thì chết khô. `PlantBlocker()` nêu lý do không gieo được; `IsThirsty` = cây cần thêm nước / ruộng nước chưa ngập.
- `FarmJob` biết gánh nước: tới nguồn nước gần ruộng nhất (`WaterSource.Nearest`) múc → mang về đổ (ruộng cạn +0,4, ruộng nước +0,25 mỗi chuyến), có gàu nước hiện trên tay. Thứ tự việc: khai hoang → cày → thu hoạch/dọn héo → gánh nước nếu thiếu → gieo. Nông dân tự làm cả cày và gánh nước khi rảnh. `NpcJob.Claims()` để việc gánh nước vẫn "giữ" ruộng, người khác không tranh.
- Công trình mới **Giếng** (`well`, 8 gỗ, mở cùng tech Nông nghiệp): nguồn nước để gánh, nhưng `feedsPaddies = false` → không cho xây ruộng nước cạnh giếng (cần ao/mương).
- Hình: đất chưa cày (phẳng, nhạt), đã cày (luống / bùn có rãnh), mặt nước đục dâng trong bờ ruộng nước, vệt đất ướt trên ruộng cạn; hàm dùng chung `GameContentBuilder.SetupFieldPlot` cho cả ruộng xây và ô vườn. Gợi ý [E] hiện % cày và mức nước, cảnh báo THIẾU NƯỚC.
- Test: 7 test mới `Milestone5dWaterTests.cs`; cập nhật test cũ (sau thu hoạch là `Unplowed`, chờ cày lại trước khi gieo). **107/107 test PlayMode pass**. Ảnh render xác nhận các trạng thái đất, nước, giếng, người cầm gàu.

## Nhật ký phiên làm việc 2026-10-01 (phần 10 — M5d/F3 gieo mạ & cấy)
- Tài nguyên mới (vật tư, không ăn được): **Thóc giống** (`rice_seed`), **Mạ** (`rice_seedling`); nhãn hiển thị dưới dòng dân số.
- `FieldType.Seedbed` + công trình **Ruộng mạ** (`seedbed`, 2 gỗ, mở cùng tech Trồng lúa; khai hoang 4, cày 2 lượt; như ruộng nước: phải ngập mới gieo, đặt được ở đâu cũng được — gánh nước tới). Cây **Mạ** (`CropData_RiceSeedling`): gieo tốn 1 thóc giống, 10+20s, nhổ được 3 bó mạ; để quá 60s mạ già hỏng. `FarmPlot.IsWetField` gom luật nước cho ruộng nước + ruộng mạ.
- `CropData.plantCost` (giống tốn khi gieo/cấy) + `harvestVerb` ("Nhổ" mạ, "Gặt" lúa). Lúa: cấy tốn 1 bó mạ; gặt được 6 lúa gạo + 2 thóc giống (giữ giống cho vụ sau). `PlantBlocker` báo thiếu giống ("Thiếu Mạ để cấy — gieo mạ ở ruộng mạ rồi nhổ mạ"); nông dân tự làm chỉ gieo khi kho có giống.
- `TechNode.grantOnUnlock`: nghiên cứu Trồng lúa tặng 4 thóc giống để bắt đầu. Ruộng nước/ruộng mạ chỉ có 1 loại cây → `FarmManager.CropFor` tự chọn (không cần bấm hạt giống); ruộng cạn vẫn theo lựa chọn.
- Hình: thóc ngâm rải trên ruộng mạ → mạ mọc dày như thảm; lúa vừa cấy là từng khóm mạ nhỏ theo hàng.
- Test: 6 test mới `Milestone5dSeedlingTests.cs` (gồm chuỗi đầy đủ: nông dân làm ruộng mạ → nhổ mạ → cấy sang ruộng nước); test cũ có cấy lúa được cấp mạ; nới sai số mức nước trong test lưu/tải (frame tải dài khi test chạy tăng tốc). **113/113 test PlayMode pass**.

## Nhật ký phiên làm việc 2026-10-01 (phần 11 — M5d/F4 chăm sóc)
- Cỏ dại: `FarmPlot.Weeds` 0..1 mọc khi đang có cây (ban ngày, 1/60 mỗi giây → ~1 phút phủ kín); cỏ phủ kín làm mất tối đa 40% sản lượng. `DoWeedWork()` mỗi lượt nhổ 0,5 (2 lượt sạch). Cày lại đất là lấp hết cỏ. Hình: khóm cỏ vàng xanh mọc xen giữa hàng, cao dần.
- Phân bón: tài nguyên mới **Phân bón** (`manure`) — dê và heo thuần cho thêm 1 phân mỗi lần thu sản phẩm. `FarmManager.TryFertilize()` (tốn 1 phân, mỗi vụ 1 lần) → sản lượng +50%; hình cục phân sẫm màu trên mặt ruộng.
- Sản lượng khi thu = gốc × (bón phân 1,5) × (1 − 0,4 × cỏ), làm tròn lên từ .5, ít nhất 1 (`FarmPlot.HarvestAmount`) — áp cho mọi thứ thu được (lúa, thóc giống, mạ…).
- Nông dân: sau gánh nước → làm cỏ khi cỏ ≥ 40% (làm đến sạch) → bón phân nếu kho có; tự làm khi rảnh. Người chơi bấm E: có cỏ thì làm cỏ, sạch cỏ thì bón phân. Gợi ý hiện % cỏ / "đã bón phân".
- `FarmPlot.WeedsEnabled` tắt trong `PlayModeTestBase` (test cũ đếm sản lượng chính xác), test F4 tự bật. Lưu/tải cỏ + đã bón.
- Test: 7 test mới `Milestone5dCareTests.cs`. **120/120 test PlayMode pass**.

## Nhật ký phiên làm việc 2026-10-01 (phần 12 — M5d/F5 sau gặt)
- Gặt lúa giờ ra **Lúa bó** (`rice_sheaf`, vật tư, 4 bó/vụ trước hệ số cỏ/phân) thay vì gạo + thóc giống. Đổi tên: "Lúa gạo" → **Gạo**, "Thóc giống" → **Thóc** (thóc vừa để giã vừa làm giống).
- Công trình mới **Cối giã** (`rice_mortar`, 6 gỗ, mở cùng tech Trồng lúa) — `Farming/RiceMortar.cs`: giàn phơi 4 chỗ; mỗi lượt công làm 1 việc theo thứ tự: tuốt bó đã khô (+2 thóc) → treo bó mới lên giàn → giã thóc (2 lượt/mẻ, 2 thóc → 2 gạo). Bó lúa khô sau 30s nắng, ban đêm không khô. Luôn chừa 4 thóc làm giống (chỉ giã phần dư). Hình: cối gỗ + chày, giàn tre với bó lúa xanh (ướt) → vàng (khô), thóc trong lòng cối khi giã dở.
- Nông dân: `MortarJob` (ra lệnh được; tự làm khi rảnh sau việc ruộng). Người chơi bấm E làm 1 lượt; gợi ý hiện việc kế tiếp + giàn phơi "N/4 (khô M)". Lưu/tải giàn phơi + mẻ giã dở (`SaveData.riceMortars`, khớp theo tên công trình).
- HUD: thêm nhãn "Lúa bó"; bảng hạt giống dời xuống -290, công nghệ -500 (menu xây giờ 7 nút).
- Test: 7 test mới `Milestone5dMortarTests.cs`, sửa test lúa cũ (ra bó lúa). **127/127 test PlayMode pass**.
- Lưu ý cân bằng: 1 vụ lúa = 4 bó → 8 thóc → giã được ~4–8 gạo (tùy thóc giống còn) — ít hơn trước (6 gạo + 2 thóc); cần chơi thử.

## Nhật ký phiên làm việc 2026-10-01 (phần 13 — M5d/F6 mương)
- Công nghệ mới **Thủy lợi** (`tech_irrigation`, 15 tri thức, cần Trồng lúa) mở công trình **Mương** (`canal`, 1 gỗ, 1 ô, đi xuyên được).
- `Farming/Canal.cs`: mới đặt là cọc + dây đánh dấu → đào 4 lượt (nông dân được ra lệnh / tự làm khi rảnh, người chơi bấm E). Đào xong thì nhường phím E cho ruộng bên cạnh (bỏ khỏi `InteractableRegistry`).
- `CanalNetwork`: mương đã đào cách mép ao ≤ 1,2m là đầu nguồn; nước loang sang mương liền kề 4 hướng tới tối đa `GravityReach` = 8 ô (F7 guồng nước sẽ đẩy xa hơn). Đoạn chưa đào chặn dòng. Tính lại khi đặt công trình / đào xong / tải game (cờ dirty, chạy trong `Update` của mương).
- Mương có nước: bật `WaterSource` (bán kính 0,2) → xây được ruộng nước gần mương, gánh nước từ mương; ruộng nằm sát (4 hướng) tự được tưới `IrrigationPerSecond` 1/12 (đầy sau ~12s) và không còn `IsThirsty` → nông dân khỏi gánh.
- Hình: lòng mương đất sẫm + bờ đắp, nhánh nối sang mương/ruộng/ao bên cạnh, mặt nước xanh khi có nước. Gợi ý: "[E] Đào mương — N%". Lưu/tải tiến độ đào (`SaveData.canals`).
- Sửa lỗi teardown: mương cuối cùng bị hủy chỉ xóa cờ tưới, không tính lại (Grid đã bị hủy).
- Bảng hạt giống dời xuống -320, công nghệ -530 (menu xây 8 nút, công nghệ 3 mục).
- Test: 8 test mới `Milestone5dCanalTests.cs`. **135/135 test PlayMode pass**.
- Lưu ý: cây trang trí trong scene không chặn đặt công trình (mương/ruộng có thể đè lên gốc cây) — vấn đề có sẵn từ trước.

## Nhật ký phiên làm việc 2026-10-01 (phần 14 — M5d/F7 guồng nước) → **Milestone 5d hoàn tất**
- Công nghệ mới **Guồng nước** (`tech_water_wheel`, 20 tri thức, cần Thủy lợi) mở công trình **Guồng nước** (`water_wheel`, 10 gỗ). Phải dựng sát mép ao (≤ 1,2m) — `BuildingData.requiresOpenWater` + `WaterSource.IsNatural` (mương không tính: "phải dựng sát mép ao").
- `Farming/WaterWheel.cs`: guồng cạnh một đoạn mương đã đào thì quay, máng tự xoay về phía mương, có dòng nước đổ xuống; chưa nối mương thì đứng yên.
- `CanalNetwork`: 2 nguồn loang riêng — tự chảy từ mương sát ao (`GravityReach` 8 ô) và guồng bơm vào mương sát guồng (`PumpedReach` 20 ô). Mương có nước guồng (`Canal.IsPumped`) tưới ruộng sát bên nhanh gấp đôi (`Canal.IrrigationRate`). Gợi ý ruộng: "nước N% (mương)" / "(guồng nước)".
- Hình: bánh xe tre 8 cánh + ống múc, trục trên 2 cột, máng tre trên đỉnh.
- Bảng hạt giống dời xuống -350, công nghệ -560 (menu xây 9 nút, công nghệ 4 mục).
- Test: 6 test mới `Milestone5dWaterWheelTests.cs`. **141/141 test PlayMode pass**.

## Nhật ký phiên làm việc 2026-10-01 (phần 15 — M6/D1 khung thiên tai)
- User chốt M6: làm cả 4 loại (hạn hán, lũ lụt, cháy rừng, sói đột kích), tần suất vừa phải, thiệt hại "hư hại, sửa được".
- `Disaster/DisasterEvent.cs` (base MonoBehaviour: id, tên, điềm báo, mô tả, thời lượng theo ngày game, trọng số; `CanHappen`/`OnWarning`/`OnBegin`/`OnTick(dt, progress)`/`OnEnd`/`IsFinishedEarly`). `Disaster/DisasterManager.cs`: thiên tai đầu tiên sau 1,5 ngày, sau đó mỗi 2–3 ngày chọn ngẫu nhiên theo trọng số trong các `DisasterEvent` gắn trên cùng object; báo trước 0,5 ngày → diễn ra → kết thúc. Đếm bằng giây game (chạy cả khi ngày/đêm đứng yên). `RandomEnabled` tắt trong `PlayModeTestBase`. Lưu/tải giai đoạn + thời gian còn lại (`SaveData.disaster`; đang diễn ra thì OnEnd rồi OnBegin lại).
- `UI/DisasterBannerUI.cs`: băng chữ giữa mép trên (dưới dòng thông báo) — vàng "Điềm báo: … sắp tới (còn khoảng N giờ)", đỏ "TÊN: … (còn …)".
- Công trình hư hại: `BuildingInstance.Damage(amount, cause)`, `IsDamaged`, `IsCollapsed` (hết máu: model đổ nghiêng + đống gỗ gãy/đá, kho không chứa, lều không ở → cả nhà ra đống lửa ngủ, cối/giếng/guồng tắt). Sửa: mỗi lượt 1 gỗ hồi 25% máu (`DoRepairWork`); công trình hư hại đăng ký vào `InteractableRegistry` → người chơi bấm E, ra lệnh chuột phải (`RepairJob`, nghề có Build), dân làng tự sửa khi rảnh (autoWork thêm Build). Thanh máu trên công trình hư hại. Lưu máu đã mất (`PlacedBuildingData.damage`).
- Sửa lỗi: kiểm tra sập trong `Start` thay vì `OnEnable` (lúc đó HealthComponent chưa Awake, máu = 0 → tưởng sập, tắt cả cối/giếng/guồng).
- Test: 7 test mới `Milestone6FrameworkTests.cs` (thiên tai giả `TestDisaster`). **148/148 test PlayMode pass**.

## Nhật ký phiên làm việc 2026-10-01 (phần 16 — M6/D2 hạn hán)
- `Disaster/DroughtDisaster.cs` (1 ngày game, trọng số 1): ruộng bốc hơi ×3 (`FarmPlot.EvaporationMultiplier`); ao tự nhiên (WaterSource natural + feedsPaddies) co mặt nước dần còn 55% (20% đầu cạn dần, 20% cuối đầy lại — lộ bãi cát quanh ao), cá ngừng sinh sôi (`ResourceNode.RegenPaused`); mương tự chảy chỉ tới 3 ô và tưới ×0,5 (`CanalNetwork.LowWater`). Giếng và guồng nước không bị ảnh hưởng → cách chống hạn. Gỡ hết khi kết thúc / scene đóng (OnDisable); tải game giữa đợt hạn không làm ao co vĩnh viễn.
- Điềm báo "trời oi bức, nắng gắt không một gợn mây"; băng đỏ "HẠN HÁN: ruộng khô nhanh gấp 3, ao cạn dần — dùng giếng, guồng nước".
- `EditorBuildUtils.SetPrivateField` giờ dò cả field private của lớp cha (cấu hình `DisasterEvent` trong scene builder qua `ConfigureDisaster`).
- Test: 5 test mới `Milestone6DroughtTests.cs`; test lịch ngẫu nhiên chỉ kiểm tra "có thiên tai được chọn" (giờ có nhiều loại). **153/153 test PlayMode pass**.

## Nhật ký phiên làm việc 2026-10-01 (phần 17 — M6/D3 lũ lụt + đê)
- `Disaster/FloodDisaster.cs` (0,5 ngày game): mưa lớn tưới mọi ruộng (+1/20 mỗi giây); ao tràn — vùng ngập lan dần tới 3,5m từ mép ao (25% đầu dâng, 25% cuối rút), hình đĩa nước lũ quanh ao. Trong vùng ngập: ruộng đầy nước; ruộng cạn ngập liên tục 40s thì cây chết úng (`FarmPlot.KillCrop`), ruộng nước/ruộng mạ chịu được; công trình có máu mất 12% máu tối đa mỗi phút ("bị sập vì lũ cuốn").
- **Đê** (`levee`, 2 gỗ, mở cùng Thủy lợi, đi xuyên được) — `Disaster/Levee.cs`: nước tràn từ mép ao tới một điểm mà đường đi cắt qua đê (≤ 0,75m từ tâm đê) thì bị chặn. Thân đê tự xoay chắn ngang hướng ra ao. Lưu ý: đĩa nước lũ vẫn vẽ tràn qua đê (chỉ là hình), nhưng đối tượng sau đê không bị ngập.
- Điềm báo "mây đen kéo về, mưa rả rích không dứt". Menu xây 10 nút → bảng hạt giống -385, công nghệ -595 (giao diện bên phải bắt đầu chật — cần sắp xếp lại khi làm M7).
- Test: 7 test mới `Milestone6FloodTests.cs`. **160/160 test PlayMode pass**. Lưu ý: `Milestone5CombatTests.FarmerNearDen_FleesInsteadOfFighting` thỉnh thoảng lỗi (nông dân bị cắn mà đứng Idle thay vì chạy) — chập chờn có từ trước, chạy lại thì pass.

## Nhật ký phiên làm việc 2026-10-01 (phần 18 — M6/D4 cháy rừng)
- `Disaster/WildfireDisaster.cs` (tối đa 0,5 ngày, kết thúc sớm khi hết lửa; hết giờ thì "mưa" dập nốt): sét đánh cháy 2 cây trong bán kính 16m quanh đống lửa trại.
- `Disaster/Fire.cs` gắn lên đối tượng đang cháy: độ lửa 0..1 bùng dần (+1/15 mỗi giây); từ 0,5 trở lên cứ 6s có 30% bén sang mỗi đồ dễ cháy trong 2,2m. Cháy được: cây gỗ (40s thì cháy rụi → còn gốc cháy đen), công trình có máu trừ giếng (mất tối đa 1/45 máu/giây theo độ lửa, sập thì tắt), ruộng đang có cây mà **khô (nước < 50%)** (8s thì cây cháy chết) — ruộng tưới đủ không bắt lửa. Hình: 4–5 ngọn lửa hình nón cam có lõi vàng, khói từng cụm bay lên, đèn cam bập bùng.
- Dập lửa: `FirefightJob` — múc nước ở nguồn gần nhất (≤ 20m) dội −0,5 mỗi gàu (xách thùng nước), không có nước thì đập −0,15; tắt xong sang đám cháy gần đó. Ai cũng làm được (cả trinh sát); chuột phải vào chỗ cháy để ra lệnh; tự đi dập khi có cháy trong 15m — **cả ban đêm** (không đi ngủ, đang ngủ thì dậy). Người chơi bấm E đập lửa −0,2. Gợi ý "[E] Dập lửa — đang cháy N%".
- Đổi thiết kế so với dự tính: bỏ "hạn hán làm lửa lan nhanh" vì mỗi lúc chỉ có 1 thiên tai; thay bằng luật ruộng ướt không cháy.
- Test: 9 test mới `Milestone6WildfireTests.cs`. **169/169 test PlayMode pass**.

## Nhật ký phiên làm việc 2026-10-01 (phần 19 — M6/D5 sói đột kích) → **Milestone 6 hoàn tất**
- `Disaster/WolfRaidDisaster.cs` (tối đa 1 ngày): bắt đầu ban ngày thì chờ tối; đêm xuống cả bầy (3 con, cứ 3 ngày thêm 1, tối đa 6) xuất hiện cách làng 20m, kéo vào làng. Hết sói (bị hạ hoặc đã rút) thì kết thúc. Tải game giữa đợt không sinh thêm bầy thứ hai.
- `PredatorAI` chế độ đột kích (`BeginRaid`): hang = giữa làng, lùng sục 9m, phát hiện 9m, không bỏ cuộc; cắn cả vật nuôi đã thuần; vẫn sợ lửa (đống lửa trại, đuốc). Đường tới mục tiêu bị chặn (`PathPartial`) → cào phá đoạn rào gần nhất (2× sát thương cắn). Trời sáng → chạy về bìa rừng rồi biến mất (không rơi thịt). Lưu/tải giữ trạng thái đột kích/đang rút.
- **Đuốc** (`torch`, 1 gỗ, mở sẵn): dùng lại `Campfire` (vùng sáng 3,5m, `isSleepSpot` = false để không ai ngủ cạnh đuốc), tối tự cháy. **Hàng rào** (`fence`, 2 gỗ, mở sẵn, 120 máu): cọc nhọn chắn kín ô (NavMeshObstacle), hàng cọc tự xoay theo rào bên cạnh (`Disaster/Fence.cs`). Công trình sập thì tắt NavMeshObstacle + Campfire → rào bị phá thì đi qua được, đuốc đổ thì tắt.
- Lính gác: người biết đánh (dân làng, thợ săn, trinh sát) thấy sói đột kích trong 10m thì ra đánh — kể cả ban đêm (không đi ngủ / đang ngủ thì dậy).
- Menu xây 12 nút → xếp lưới 2 cột (`GridLayoutGroup`, ô 148×30); bảng hạt giống trở lại -250, công nghệ -420.
- Test: 7 test mới `Milestone6WolfRaidTests.cs`. **176/176 test PlayMode pass**.

## Nhật ký phiên làm việc 2026-10-02 (thử nghiệm phong cách pixel art)
- User muốn cảnh và nhân vật dạng pixel, chưa có gói asset, không biết vẽ → thử "3D pixel art": giữ nguyên model 3D + gameplay, chỉ đổi cách vẽ.
- `Shaders/PixelArt.shader` + `Player/PixelArtCamera.cs` (trên Main Camera, `[ExecuteAlways]`): vẽ khung hình cao 240 điểm ảnh rồi phóng to lọc Point; viền tối mép vật thể (so độ sâu ô bên cạnh); ép về bảng màu 40 màu đất–rừng–nước (`GameplaySceneBuilder.PixelPalette`) có rải điểm Bayer 4×4 cho chuyển màu mượt. Giao diện Canvas không bị ảnh hưởng, chuột/chọn vẫn tính trên màn hình thật. **Phím P** bật/tắt.
- `Editor/PixelTextureBuilder.cs` sinh texture pixel lát liền mạch bằng code (`Assets/Textures/Pixel`): cỏ 64×64 (khóm cỏ, hoa dại), đất trại 32×32 (sỏi), cát 16×16, nước 32×32 (gợn sóng); lọc Point, không mipmap. Dùng cho mặt đất, sân trại, bờ cát + mặt ao (`EditorBuildUtils.TexturedMat`). `BuildAll` sinh texture trước.
- Chưa làm: nhân vật vẫn là khối 3D (bước sau nếu user thích: sprite pixel billboard — cần gói asset như Ninja Adventure (CC0) hoặc AI như PixelLab); camera vẫn phối cảnh (chưa orthographic/khóa điểm ảnh → có thể hơi rung khi di chuyển); chưa xem ban đêm.
- 176/176 test pass (lần đầu `GrownUpChild_LeavesTheFamilyHut` lỗi do chờ 10s game hụt giờ — chập chờn, chạy lại pass).
- **Hướng đồ họa do user tự thiết kế (tạm gác, ghi lại)**: user muốn tự làm cảnh + nhân vật, hỏi về Blender. Đã kiểm tra máy: iMac 2013 (i5-4570R, RAM 8GB, Intel Iris Pro 5200 driver 2017, ổ C còn ~4GB) → chạy được Blender cho low-poly: dùng **Blender 4.2 LTS bản portable cài ở ổ D**, khung nhìn Solid, không mở cùng lúc với Unity. Công cụ dựng nhanh đã giới thiệu: **BlenderMCP** (Claude điều khiển Blender trực tiếp, kèm Poly Haven/Sketchfab/Hyper3D Rodin), BlenderKit, Quaternius/Kenney ghép lại, Sapling Tree Gen / Rock Generator / A.N.T. Landscape / Geometry Nodes, Mixamo/Rigify; hoặc Claude viết script Python cho Blender. Còn treo: xuất bảng màu (.gpl/.png), cơ chế "có model của user thì dùng, không thì placeholder", camera orthographic chống rung.

## Nhật ký phiên làm việc 2026-10-02 (phần 2 — M7/P1 hiệu ứng hạt)
- `Core/VfxManager.cs`: mỗi loại hiệu ứng (`VfxKind`: mùn gỗ, lá, nước bắn, bụi, bụi lớn, gặt, lấp lánh, máu, hơi nước, sét) là một ParticleSystem dùng chung dựng bằng code; `VfxManager.Play(kind, pos)` phun một đợt (Emit) tại chỗ. Hạt vuông không texture (material `Assets/Materials/Particle.mat`, Legacy Particles/Alpha Blended) → qua hậu kỳ pixel thành điểm ảnh. Không có manager trong scene thì bỏ qua.
- Gắn vào: chặt cây (mùn gỗ, cây đổ thì lá), đánh cá/gánh nước tưới (nước bắn), khai hoang/làm cỏ (lá), cày/gieo/cối giã (bụi), gặt (hạt vàng–xanh), xây xong (bụi lớn qua `OnBuildingPlaced`), nâng cấp (lấp lánh), sửa (bụi), sập (bụi lớn), người/thú bị đánh (máu, giãn 0,15s; chết thì bụi lớn), công trình hư do thiên tai (bụi, giãn 1,5s), dập lửa (hơi nước), sét đánh (tia sáng + đèn chớp 2 nhịp). Lũ lụt: mưa (hạt kéo dài, phủ 36m theo camera) suốt lúc lũ.
- Lưu ý: hệ hạt đang dừng thì hạt Emit ra không cập nhật → `Play()` sau mỗi lần phun. Ảnh chụp chế độ edit chỉ thấy mưa (đợt hạt không mô phỏng ngoài Play) — test xác nhận hạt sống thật khi chạy.
- Test: 6 test mới `Milestone7VfxTests.cs`. **182/182 test PlayMode pass**.

## Nhật ký phiên làm việc 2026-10-02 (phần 3 — M7/P2a nhạc nền)
- User: "trước tiên làm nhạc đơn giản, sau đó tôi có thể thay bằng đoạn nhạc tùy ý".
- `Editor/MusicBuilder.cs` sinh 2 bài WAV (22050Hz mono, lặp liền mạch nhờ đuôi nốt quấn vòng về đầu) vào `Assets/Audio/Music/Generated/`: **Day** (92 BPM, 16 ô ≈ 42s: trống da, ống lắc tre, đàn gảy Karplus–Strong rải hợp âm, sáo trúc khuôn A–B–A từ ô 5) và **Night** (60 BPM, 8 ô = 32s: nền trầm, tiếng gảy thưa, nhịp tim nhẹ, dế kêu). Thang ngũ cung La thứ. `BuildAll` sinh nhạc trước khi dựng scene.
- **Thay nhạc riêng**: đặt `Day.*` / `Night.*` (.wav/.mp3/.ogg) vào `Assets/Audio/Music/` rồi Build All — `MusicBuilder.Resolve` ưu tiên file người dùng, file sinh ra nằm thư mục riêng nên không bao giờ đè. Hoặc kéo thả vào ô Day/Night Music của object `MusicManager` (bị mất khi dựng lại scene).
- `Core/MusicManager.cs`: 2 AudioSource lặp, đổi bài theo ngày/đêm (nhỏ dần/to dần 4s thật), **phím M** tắt/bật nhạc; `Volume`/`Muted` lưu PlayerPrefs (cho màn cài đặt P3).
- Test: 2 test mới `Milestone7MusicTests.cs`. **184/184 test PlayMode pass**.

## Nhật ký phiên làm việc 2026-10-02 (phần 4 — M7/P2b tiếng động)
- `Editor/SfxBuilder.cs` sinh 14 tiếng WAV vào `Assets/Audio/SFX/Generated/` (`SfxKind`): Chop (rìu bổ gỗ), Rustle (lá xào xạc), Splash, Dig (cuốc), Build (3 nhát búa), Crash (sập đổ), Harvest (xào xạc + 2 nốt "tưng"), Chime (3 tiếng chuông), Hit, Hiss (xèo hơi nước), Thunder (đanh rồi ầm ì 3s), Howl (2 con sói hú), Rain + Fire (lặp 4s/3s, quấn vòng liền mạch). **Thay tiếng riêng**: đặt file cùng tên (vd `Chop.wav`, `Howl.mp3`) vào `Assets/Audio/SFX/` rồi Build All.
- `Core/SfxManager.cs`: 10 nguồn phát xoay vòng, nửa 3D (gần to xa nhỏ theo camera), lệch cao độ ±8% cho khỏi máy móc, cùng một tiếng cách nhau ≥0,08s; `Volume` lưu PlayerPrefs. Mưa = nguồn lặp riêng; lửa = AudioSource lặp gắn vào vật đang cháy (gỡ khi tắt).
- Mỗi `VfxManager.Play` tự kèm tiếng mặc định theo loại hiệu ứng (đổi được bằng tham số): xây xong/sửa = búa, người/thú chết = Hit, sét = sấm (nghe khắp nơi). Sói đột kích kéo vào = tiếng hú; mở công nghệ = chuông.
- Test: 5 test mới `Milestone7SfxTests.cs`. **189/189 test PlayMode pass**.

## Nhật ký phiên làm việc 2026-10-02 (phần 5 — M7/P3 menu)
- `UI/GameMenuUI.cs` (object `GameMenu` phủ toàn Canvas, tự dựng giao diện bằng code khi chạy): **menu chính** lúc vào game (tên "BỘ LẠC TIỀN SỬ" — tạm, chưa có tên chính thức; Chơi mới / Chơi tiếp (bản lưu, mờ nếu chưa có) / Cài đặt / Thoát + bảng phím tắt), **tạm dừng** bằng Esc (Chơi tiếp / Lưu game / Cài đặt / Về menu chính = nạp lại scene / Thoát), **cài đặt** (2 thanh âm lượng nhạc + tiếng động, nút Đồ họa pixel BẬT/TẮT, Toàn màn hình). Esc trong cài đặt = quay lại; Esc lúc đang cầm công trình để đặt thì chỉ hủy đặt, không mở menu.
- Mở menu: `Time.timeScale` = 0 (đóng thì trả lại tốc độ cũ); `GameMenuUI.IsOpen` chặn phím/chuột của người chơi, chiến đấu, đặt công trình, chọn dân, F5/F9, P.
- `PixelArtCamera.SavedPreference` (PlayerPrefs) — chế độ pixel nhớ giữa các lần chơi (phím P cũng lưu).
- Scene builder đăng ký `Gameplay.unity` vào Build Settings (trước đây trống) — cần cho "Về menu chính" và để build game.
- Test: `PlayModeTestBase` tắt menu chính (`ShowMainMenuOnStart = false`); 4 test mới `Milestone7MenuTests.cs`. **193/193 test PlayMode pass**. Ảnh chụp menu (canvas tạm đổi sang ScreenSpaceCamera) xác nhận bố cục + chữ tiếng Việt.

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
