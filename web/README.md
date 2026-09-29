# Prehistoric Tribe — bản Web (Three.js)

Bản port lên trình duyệt của prototype Unity (Milestone 1–4), dựa trên `.claude/SPEC.md`,
`.claude/ARCHITECTURE.md` và số liệu trong `Assets/_Data/`.

## Chạy

```bash
cd web
npm install
npm run dev      # mở http://localhost:5173
npm run build    # xuất bản tĩnh ra web/dist (deploy lên bất kỳ static host nào)
```

## Điều khiển

| Phím | Hành động |
|---|---|
| WASD / mũi tên | Di chuyển |
| E / Space | Tương tác đối tượng gần nhất (chặt cây, gieo/thu hoạch, cho ăn/thuần hóa/thu sản phẩm) |
| Chuột trái | Đặt công trình đã chọn ở bảng "Xây dựng" |
| Chuột phải / Esc | Hủy đặt công trình |
| Con lăn | Zoom camera |
| F5 / F9 | Lưu / tải (localStorage) |

Gõ `game` trong DevTools console để xem/chỉnh trạng thái (vd `game.resources.add('wood', 50)`).

## Cấu trúc (khớp ARCHITECTURE.md)

```
src/
  data/gameData.js      Data   — tương đương ScriptableObject (tài nguyên, công trình, cây, vật nuôi, tech, bố cục map)
  core/                 EventBus, World (danh sách entity), Input, SaveSystem, GameManager, physics
  resources/ building/ farming/ animals/ tech/ player/
                        Logic  — thuần JS, không import Three.js
  render/               Presentation — Three.js: Renderer (camera/ánh sáng/map), views.js (mesh cho từng loại entity)
  ui/HUD.js             Presentation — DOM overlay (thanh tài nguyên, menu xây, hạt giống, tech, thông báo)
```

Logic chạy trên mặt phẳng 2D `(x, y)` giống bản Unity; tầng render đổi sang 3D `(x, 0, -y)`.
Muốn đổi đồ họa (vd load model `.glb`) chỉ cần sửa `render/views.js`.

## Khác biệt so với bản Unity

- Save/load lưu thêm trạng thái ô đất, vật nuôi, cây còn lại và tech đã mở (bản Unity mới lưu player + tài nguyên + công trình).
- Heo rừng đi lang thang quanh chỗ sinh ra; có trần `maxPopulation` để không sinh sản vô hạn.
- Phản hồi rõ hơn: vòng sáng dưới đối tượng gần nhất, dòng gợi ý hành động, chữ bay "+1 Gỗ", vòng cổ cho con đã thuần,
  biểu tượng vàng khi có sản phẩm, thông báo khi vật nuôi sinh con.
- Công trình/hạt giống chưa mở khóa vẫn hiện (mờ, có gợi ý tech cần nghiên cứu) thay vì ẩn hẳn.
