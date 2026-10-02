# Hướng dẫn thiết kế lại UI game

Oct 2, 2026

## Tổng quan

Mục tiêu: chỉ hiện những nút người chơi cần lúc đó, và báo ngay khi có công cụ mới mở được. Tài liệu viết cho Unity (uGUI + TextMeshPro).

Vấn đề của UI hiện tại:

- Hơn 20 nút hiện cùng lúc ở bên phải, phần lớn đang khóa, che gần nửa màn hình.
- Chữ dài như "Ruộng mạ (khóa)" bị xuống dòng và đè lên nút bên cạnh.
- Nút xây dựng, cây trồng và nghiên cứu nằm lẫn với nhau.
- HUD bên trái hiện cả tài nguyên bằng 0 hoặc chưa mở khóa.

Kết quả sau khi làm xong: một thanh tab ở cạnh dưới (Xây dựng, Trồng trọt, Nghiên cứu), mỗi công cụ là một icon có 4 trạng thái, tooltip ghi rõ điều kiện, và chấm báo trên tab khi có thứ mới mở được.

## Bước 1: Thiết lập Canvas và layout chống đè nút

Làm bước này trước, vì nó sửa luôn lỗi nút đè nhau ở mọi độ phân giải.

1. Chọn Canvas → Canvas Scaler: UI Scale Mode = Scale With Screen Size, Reference Resolution = 1920 × 1080, Match = 0.5.
2. Xóa các nút đặt tay hiện có. Mọi danh sách nút từ giờ nằm trong một container có Layout Group.
3. Container danh sách công cụ: thêm Grid Layout Group (Cell Size 96 × 96, Spacing 8 × 8, Constraint = Fixed Row Count = 1) và Content Size Fitter (Horizontal Fit = Preferred Size).
4. Mỗi nút dùng icon thay vì chữ dài. Nếu cần nhãn, đặt TextMeshPro nhỏ dưới icon với Auto Size (min 14, max 20) và Overflow = Ellipsis.
5. Neo (anchor) các khối UI vào góc/cạnh màn hình: HUD neo trên cùng, thanh tab neo giữa dưới.

Cấu trúc Hierarchy đề xuất:

```
Canvas
├── TopHUD            (Horizontal Layout Group)
├── Toast             (thông báo trượt ra)
├── Tooltip           (ẩn mặc định, Raycast Target = off)
└── BottomBar
    ├── ToolPanel     (Grid Layout Group, ẩn mặc định)
    └── Tabs          (Horizontal Layout Group)
        ├── Tab_Build
        ├── Tab_Farm
        └── Tab_Research
```

## Bước 2: Gom nút vào thanh tab

Chia toàn bộ nút hiện có thành 3 nhóm; chỉ một nhóm mở tại một thời điểm.

| Tab | Phím tắt | Công cụ |
| --- | --- | --- |
| Xây dựng | B | Lều, Ruộng cạn, Hàng rào, Đuốc, Kho, Giếng, Đê, Ruộng mạ, Ruộng nước, Mương, Cối giã, Guồng nước |
| Trồng trọt | F | Cây mọng, Rau, Lúa, Mạ |
| Nghiên cứu | R | Nông nghiệp, Trồng lúa, Thủy lợi, Guồng nước |

Hành vi của thanh tab:

- Bấm một tab → ToolPanel hiện danh sách của tab đó; bấm lại cùng tab hoặc Esc → đóng.
- Bấm tab khác → đổi nội dung, không mở thêm panel thứ hai.
- Khi người chơi đang đặt công trình, ẩn panel để không che vị trí đặt.

Script điều khiển tab (gắn vào BottomBar):

```csharp
public class ToolbarController : MonoBehaviour {
    public GameObject toolPanel;
    public ToolButtonList list;      // vẽ các nút trong panel
    ToolCategory? current;

    public void OnTabClicked(int cat) { Toggle((ToolCategory)cat); }

    void Update() {
        if (Input.GetKeyDown(KeyCode.B)) Toggle(ToolCategory.Build);
        if (Input.GetKeyDown(KeyCode.F)) Toggle(ToolCategory.Farm);
        if (Input.GetKeyDown(KeyCode.R)) Toggle(ToolCategory.Research);
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    void Toggle(ToolCategory cat) {
        if (current == cat) { Close(); return; }
        current = cat;
        toolPanel.SetActive(true);
        list.Show(cat);
    }

    public void Close() { current = null; toolPanel.SetActive(false); }
}
```

Nghiên cứu có thể tách thành màn hình cây công nghệ riêng sau này; ban đầu dùng chung panel là đủ.

## Bước 3: Dữ liệu công cụ và 4 trạng thái mở khóa

Mỗi công cụ là một ScriptableObject chứa điều kiện; nút tự đổi giao diện theo trạng thái tính từ dữ liệu đó.

| Trạng thái | Khi nào | Hiển thị |
| --- | --- | --- |
| Ẩn | Chưa có điều kiện tiên quyết nào được mở | Không hiện nút |
| Khóa | Đã thấy được nhưng còn thiếu điều kiện | Icon xám, ổ khóa nhỏ ở góc, không ghi chữ "(khóa)" |
| Sẵn sàng | Đủ mọi điều kiện, chưa mở | Icon sáng, viền vàng nhấp nháy nhẹ |
| Đã mở | Đã mở khóa | Icon màu thường; mờ đi nếu thiếu tài nguyên để xây |

Dữ liệu công cụ (Create → Game → Tool):

```csharp
public enum ToolCategory { Build, Farm, Research }
public enum UnlockState { Hidden, Locked, Ready, Unlocked }

[System.Serializable]
public class Cost { public ResourceType type; public int amount; }

[CreateAssetMenu(menuName = "Game/Tool")]
public class ToolData : ScriptableObject {
    public string displayName;
    public Sprite icon;
    public ToolCategory category;
    public ToolData[] prerequisites;   // vd: Lúa cần Nông nghiệp
    public Cost[] unlockCost;          // vd: 5 Tri thức
    [System.NonSerialized] public bool unlocked;
}
```

Hàm tính trạng thái:

```csharp
public static UnlockState Evaluate(ToolData t, Inventory inv) {
    if (t.unlocked) return UnlockState.Unlocked;
    bool allPre = t.prerequisites.All(p => p.unlocked);
    bool anyPre = t.prerequisites.Length == 0 || t.prerequisites.Any(p => p.unlocked);
    if (!anyPre) return UnlockState.Hidden;
    if (allPre && t.unlockCost.All(c => inv.Get(c.type) >= c.amount))
        return UnlockState.Ready;
    return UnlockState.Locked;
}
```

Quan trọng: không gọi Evaluate trong Update mỗi frame. Inventory phát sự kiện `OnChanged` khi tài nguyên hoặc tri thức thay đổi; UnlockManager nghe sự kiện đó, tính lại trạng thái mọi công cụ và chỉ cập nhật nút nào đổi trạng thái.

```csharp
public class UnlockManager : MonoBehaviour {
    public ToolData[] allTools;
    public Inventory inventory;
    public event System.Action<ToolData, UnlockState> OnStateChanged;
    readonly Dictionary<ToolData, UnlockState> states = new();

    void OnEnable() { inventory.OnChanged += Refresh; Refresh(); }
    void OnDisable() { inventory.OnChanged -= Refresh; }

    public void Refresh() {
        foreach (var t in allTools) {
            var s = Evaluate(t, inventory);
            if (!states.TryGetValue(t, out var old) || old != s) {
                states[t] = s;
                OnStateChanged?.Invoke(t, s);
            }
        }
    }

    public UnlockState Get(ToolData t) => states[t];

    public bool TryUnlock(ToolData t) {
        if (Get(t) != UnlockState.Ready) return false;
        foreach (var c in t.unlockCost) inventory.Spend(c.type, c.amount);
        t.unlocked = true;
        Refresh();
        return true;
    }
}
```

Nút công cụ đổi giao diện theo trạng thái:

```csharp
public class ToolButton : MonoBehaviour {
    public Image icon; public GameObject lockBadge, readyGlow;
    public void Apply(UnlockState s) {
        gameObject.SetActive(s != UnlockState.Hidden);
        icon.color = s == UnlockState.Locked ? new Color(1,1,1,0.35f) : Color.white;
        lockBadge.SetActive(s == UnlockState.Locked);
        readyGlow.SetActive(s == UnlockState.Ready);
    }
}
```

## Bước 4: Tooltip điều kiện

Rê chuột (hoặc giữ ngón trên mobile) vào nút → tooltip liệt kê từng điều kiện với dấu đạt/chưa đạt. Nhờ vậy nút không cần chứa chữ dài.

Ví dụ hiển thị:

```
Lúa
✓ Nghiên cứu: Nông nghiệp
✗ Tri thức 5  (đang có 3)
```

1. Tạo object Tooltip: Image nền + TextMeshPro, Content Size Fitter (Preferred Size), tắt Raycast Target để tooltip không chặn chuột.
2. Gắn script vào mỗi ToolButton để bắt sự kiện rê chuột:

```csharp
public class ToolTooltipTrigger : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler {
    public ToolData tool;
    public void OnPointerEnter(PointerEventData e) =>
        Tooltip.Show(BuildText(tool, Inventory.Instance), transform.position);
    public void OnPointerExit(PointerEventData e) => Tooltip.Hide();

    static string BuildText(ToolData t, Inventory inv) {
        var sb = new System.Text.StringBuilder($"<b>{t.displayName}</b>\n");
        foreach (var p in t.prerequisites)
            sb.AppendLine(Line(p.unlocked, $"Nghiên cứu: {p.displayName}"));
        foreach (var c in t.unlockCost) {
            int have = inv.Get(c.type);
            sb.AppendLine(Line(have >= c.amount, $"{c.type} {c.amount}  (đang có {have})"));
        }
        return sb.ToString();
    }
    static string Line(bool ok, string s) =>
        ok ? $"<color=#6BD66B>✓ {s}</color>" : $"<color=#FF6B6B>✗ {s}</color>";
}
```

3. Tooltip.Show đặt vị trí phía trên nút và kẹp trong màn hình để không bị tràn ra mép.
4. Font TextMeshPro phải có glyph ✓ ✗ và đủ dấu tiếng Việt; nếu thiếu, tạo Font Asset có bộ ký tự Vietnamese hoặc thay ✓ ✗ bằng icon sprite.

## Bước 5: Thông báo khi đủ điều kiện

Khi một công cụ chuyển sang Sẵn sàng, người chơi phải biết ngay dù panel đang đóng: một chấm đỏ trên tab và một thông báo ngắn.

- Chấm đỏ: mỗi Tab có một object con `Badge`, bật khi tab đó có ít nhất một công cụ Sẵn sàng.
- Thông báo (toast): trượt vào từ trên, hiện 2,5 giây với nội dung "Có thể mở khóa: Lúa"; bấm vào thì mở đúng tab đó.
- Chỉ báo một lần cho mỗi công cụ, kể cả khi tài nguyên lên xuống quanh ngưỡng.

```csharp
public class UnlockNotifier : MonoBehaviour {
    public UnlockManager manager;
    public ToolbarController toolbar;
    public GameObject[] tabBadges;   // theo thứ tự ToolCategory
    public Toast toast;
    readonly HashSet<ToolData> announced = new();

    void OnEnable() => manager.OnStateChanged += Handle;
    void OnDisable() => manager.OnStateChanged -= Handle;

    void Handle(ToolData t, UnlockState s) {
        RefreshBadges();
        if (s == UnlockState.Ready && announced.Add(t))
            toast.Show($"Có thể mở khóa: {t.displayName}",
                       () => toolbar.OnTabClicked((int)t.category));
    }

    void RefreshBadges() {
        for (int i = 0; i < tabBadges.Length; i++)
            tabBadges[i].SetActive(manager.allTools.Any(t =>
                (int)t.category == i && manager.Get(t) == UnlockState.Ready));
    }
}
```

Thêm âm thanh nhỏ khi toast hiện sẽ giúp người chơi chú ý mà không cần nhìn vào góc màn hình.

## Bước 6: Gọn HUD tài nguyên

Thay khối chữ bên trái bằng một dải icon + số ở trên cùng, và ẩn mọi tài nguyên chưa có.

- Mỗi tài nguyên là một prefab `ResourceSlot` (icon 32 px + số) trong TopHUD có Horizontal Layout Group.
- Ẩn slot khi tài nguyên chưa mở khóa hoặc đang bằng 0 (Sữa, Cá, Phân bón… khi mới vào game).
- Giữ luôn hiện: Gỗ, Thức ăn, Tri thức, Dân số/Nhà.
- Sức chứa từng loại ("Sức chứa: 20/loại") chuyển vào tooltip khi rê chuột vào slot, không ghi thường trực.
- Số thay đổi thì nháy màu nhẹ (xanh khi tăng, đỏ khi giảm) để người chơi thấy mà không phải đọc.

```csharp
public class ResourceSlot : MonoBehaviour {
    public ResourceType type; public TMP_Text label;
    public bool alwaysShow;
    public void Refresh(Inventory inv) {
        int v = inv.Get(type);
        gameObject.SetActive(alwaysShow || v > 0);
        label.text = v.ToString();
    }
}
```

Gọi Refresh cho mọi slot từ cùng sự kiện `Inventory.OnChanged` ở Bước 3.

## Danh sách kiểm tra

- [ ] Canvas Scaler đặt Scale With Screen Size 1920 × 1080; thử ở 1280 × 720 và 2560 × 1440 không đè nút
- [ ] Đã xóa 3 cột nút cũ, chỉ còn thanh tab ở cạnh dưới
- [ ] Tạo ToolData cho mọi công cụ, điền prerequisites và unlockCost
- [ ] Công cụ chưa có tiền đề không hiện; công cụ khóa có icon xám + ổ khóa
- [ ] Tooltip hiện đúng ✓ / ✗ và số đang có, không tràn mép màn hình
- [ ] Đủ điều kiện → chấm đỏ trên tab + toast, chỉ báo một lần
- [ ] Bấm toast mở đúng tab
- [ ] HUD ẩn tài nguyên bằng 0, sức chứa nằm trong tooltip
- [ ] Phím tắt B / F / R / Esc hoạt động; panel ẩn khi đang đặt công trình
- [ ] Font hiển thị đủ dấu tiếng Việt