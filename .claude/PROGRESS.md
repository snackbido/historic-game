# PROGRESS.md — Tiến trình dự án

> Cập nhật file này sau mỗi buổi làm việc: đánh dấu việc đã xong, ghi chú vấn đề gặp phải, quyết định đã chốt.

## Trạng thái hiện tại
- **Giai đoạn**: Setup / Pre-production
- **Cập nhật lần cuối**: 2026-09-21

## Milestone 0 — Setup môi trường
- [ ] Cài Unity Hub + Unity Editor (bản LTS)
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
- Script đã viết xong (2026-09-21), nhưng **chưa test được trong Unity Editor** vì máy dev chưa cài Unity Hub/Editor/VS Code extension — xem checklist Milestone 0 còn lại.
- Cố tình **không tạo `InputManager` riêng** ở Milestone 1: chỉ có 2 loại input (di chuyển, tương tác/đặt công trình), thêm tầng trừu tượng ngay bây giờ là over-engineering. Đọc input trực tiếp trong `PlayerController`/`PlayerInteraction`/`BuildingPlacer`. Sẽ tách ra khi input phức tạp hơn (combat, tech tree UI...).
- `ResourceType` giữ là enum (không phải ScriptableObject) vì MVP chỉ có 1 loại tài nguyên (gỗ) — đúng theo lựa chọn "enum/SO" đã ghi trong ARCHITECTURE.md. Sẽ cân nhắc chuyển sang SO khi Milestone 2 (farming) cần nhiều loại tài nguyên hơn.
- `BuildingPlacer.LoadFromSaveData` hiện chỉ khớp lại đúng 1 loại building đang gán trong Inspector (`buildingToPlace`) — vì MVP chỉ có 1 loại công trình (lều). Khi có nhiều loại building hơn, cần đổi sang một registry/mảng `BuildingData[]` để tra theo `id`.
- Các bước còn lại phải làm tay trong Unity Editor (không thể làm từ code): tạo scene `Gameplay.unity`/`MainMenu.unity`, tạo prefab Tent/Tree, tạo asset `Tent.asset` (BuildingData), kéo-thả component + gán field Inspector, import TextMeshPro Essentials. Xem chi tiết trong plan `bright-foraging-honey.md`.

## Milestone 2 — Farming
- [ ] Định nghĩa CropData (ScriptableObject)
- [ ] FarmPlot: đất có thể trồng, trạng thái (trống/đã trồng/đang lớn/sẵn sàng thu hoạch)
- [ ] Vòng đời cây trồng theo thời gian (state machine đơn giản)
- [ ] Thu hoạch → cộng vào ResourceManager
- [ ] UI: chọn loại cây để trồng

## Milestone 3 — Chăn nuôi
- [ ] Định nghĩa AnimalData (ScriptableObject)
- [ ] Vật nuôi có nhu cầu: đói, sinh sản
- [ ] Cơ chế thuần hóa động vật hoang dã (nếu có trong scope)
- [ ] Thu sản phẩm từ vật nuôi (thịt/da/sữa)

## Milestone 4 — Tech Tree / Progression
- [ ] Định nghĩa TechNode (ScriptableObject), điều kiện mở khóa
- [ ] TechManager quản lý trạng thái đã mở khóa
- [ ] UI cây công nghệ
- [ ] Kết nối tech mở khóa → building/farming/combat mới

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

## Vấn đề đang tồn đọng (Known issues / Open questions)
- [ ] Chưa quyết định: chế độ combat (trực tiếp hay chỉ huy nhóm)?
- [ ] Chưa có tên chính thức cho dự án
- [ ] Chưa test Milestone 1 trong Unity Editor thật (máy dev chưa cài Unity) — xem "Ghi chú milestone này" ở trên
