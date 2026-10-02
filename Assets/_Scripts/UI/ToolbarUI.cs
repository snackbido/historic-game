using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrehistoricTribe
{
    /// <summary>
    /// Thanh công cụ ở cạnh dưới (thiết kế lại UI theo .claude/RE-DESIGNUI.md): 3 tab Xây dựng / Trồng trọt / Nghiên cứu
    /// (phím B / G / T — F và R đã dùng cho đâm giáo, ném đá), mỗi lần chỉ mở một bảng; mỗi công cụ là một ô icon 4 trạng thái
    /// (<see cref="UnlockState"/>), rê chuột ra tooltip điều kiện; chấm đỏ trên tab khi có thứ nghiên cứu được, và báo một lần
    /// khi một công nghệ vừa đủ điều kiện. Bảng ẩn khi đang đặt công trình. Giao diện tự dựng bằng code khi chạy.
    /// </summary>
    public class ToolbarUI : MonoBehaviour
    {
        [SerializeField] private List<BuildingData> buildings = new List<BuildingData>();
        [SerializeField] private List<CropData> crops = new List<CropData>();
        [SerializeField] private List<TechNode> techs = new List<TechNode>();
        [SerializeField] private Sprite lockSprite;
        [SerializeField] private Sprite badgeSprite;

        private const float Cell = 96f;
        private static readonly string[] TabNames = { "Xây dựng", "Trồng trọt", "Nghiên cứu" };
        private static readonly KeyCode[] TabKeys = { KeyCode.B, KeyCode.G, KeyCode.T };

        private class Tool
        {
            public ToolCategory category;
            public Object data;
            public string name;
            public RectTransform root;
            public Image background, icon, glow, lockBadge;
            public TMP_Text label;
            public UnlockState state = (UnlockState)(-1);
        }

        private readonly List<Tool> tools = new List<Tool>();
        private readonly HashSet<TechNode> announced = new HashSet<TechNode>();
        private RectTransform panel;
        private readonly RectTransform[] pages = new RectTransform[3];
        private readonly Image[] tabBackgrounds = new Image[3];
        private readonly GameObject[] badges = new GameObject[3];
        private bool initialized;

        public static ToolbarUI Instance { get; private set; }
        public ToolCategory? Current { get; private set; }
        /// <summary>Bảng đang hiện (khung hình trước) — Esc lúc này chỉ đóng bảng, không mở menu tạm dừng.</summary>
        public static bool PanelWasOpen { get; private set; }
        public bool PanelVisible => panel != null && panel.gameObject.activeSelf;

        private void Awake()
        {
            Instance = this;
            BuildUI();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            PanelWasOpen = false;
        }

        private void OnEnable()
        {
            EventBus.OnResourceChanged += HandleResourceChanged;
            EventBus.OnTechUnlocked += HandleTechUnlocked;
            EventBus.OnGameLoaded += Refresh;
        }

        private void OnDisable()
        {
            EventBus.OnResourceChanged -= HandleResourceChanged;
            EventBus.OnTechUnlocked -= HandleTechUnlocked;
            EventBus.OnGameLoaded -= Refresh;
        }

        private void Start() => Refresh();

        private void HandleResourceChanged(ResourceTypeData _, int __) => Refresh();
        private void HandleTechUnlocked(TechNode _) => Refresh();

        // ─── Mở / đóng ──────────────────────────────────────────────────────
        public void Toggle(ToolCategory category)
        {
            if (Current == category) Close();
            else Open(category);
        }

        public void Open(ToolCategory category)
        {
            Current = category;
            for (int i = 0; i < pages.Length; i++) pages[i].gameObject.SetActive(i == (int)category);
            UpdatePanelVisibility();
        }

        public void Close()
        {
            Current = null;
            UpdatePanelVisibility();
        }

        private void UpdatePanelVisibility()
        {
            bool placing = BuildingPlacer.Instance != null && BuildingPlacer.Instance.IsPlacing;
            bool show = Current.HasValue && !placing && !GameMenuUI.IsOpen;
            if (panel.gameObject.activeSelf != show) panel.gameObject.SetActive(show);
            for (int i = 0; i < tabBackgrounds.Length; i++)
                tabBackgrounds[i].color = Current.HasValue && (int)Current.Value == i ? new Color(0.45f, 0.31f, 0.17f) : UiKit.SlotColor;
        }

        private void Update()
        {
            if (!GameMenuUI.IsOpen)
            {
                for (int i = 0; i < TabKeys.Length; i++)
                    if (Input.GetKeyDown(TabKeys[i])) Toggle((ToolCategory)i);
                // Esc: đang đặt công trình thì để BuildingPlacer hủy đặt (bảng hiện lại sau đó); không thì đóng bảng.
                bool placing = BuildingPlacer.Instance != null && BuildingPlacer.Instance.IsPlacing;
                if (Input.GetKeyDown(KeyCode.Escape) && Current.HasValue && !placing) Close();
            }
            UpdatePanelVisibility();

            // Viền vàng nhấp nháy nhẹ cho ô "Sẵn sàng".
            float pulse = 0.45f + 0.35f * Mathf.Sin(Time.unscaledTime * 4f);
            foreach (var tool in tools)
                if (tool.state == UnlockState.Ready) tool.glow.color = new Color(1f, 0.82f, 0.29f, pulse);
        }

        private void LateUpdate() => PanelWasOpen = Current.HasValue;

        // ─── Trạng thái ─────────────────────────────────────────────────────
        public UnlockState StateOf(Object data)
        {
            switch (data)
            {
                case BuildingData b: return UnlockRules.Of(b);
                case CropData c: return UnlockRules.Of(c);
                case TechNode t: return UnlockRules.Of(t);
                default: return UnlockState.Hidden;
            }
        }

        /// <summary>Ô của công cụ này đang hiện trên bảng (không ẩn)?</summary>
        public bool IsShown(Object data) => FindTool(data) is Tool tool && tool.root.gameObject.activeSelf;

        public bool HasBadge(ToolCategory category) => badges[(int)category].activeSelf;

        public string TooltipFor(Object data)
        {
            switch (data)
            {
                case BuildingData b: return UnlockRules.Tooltip(b);
                case CropData c: return UnlockRules.Tooltip(c);
                case TechNode t: return UnlockRules.Tooltip(t);
                default: return null;
            }
        }

        /// <summary>Tính lại trạng thái mọi công cụ khi tài nguyên / công nghệ đổi (không chạy mỗi khung hình).</summary>
        public void Refresh()
        {
            if (TechManager.Instance == null) return;
            var readyNow = new List<TechNode>();
            foreach (var tool in tools)
            {
                UnlockState state = StateOf(tool.data);
                if (state != tool.state)
                {
                    if (initialized && state == UnlockState.Ready && tool.data is TechNode tech && announced.Add(tech)) readyNow.Add(tech);
                    if (!initialized && state == UnlockState.Ready && tool.data is TechNode readyAtStart) announced.Add(readyAtStart);
                    tool.state = state;
                    ApplyState(tool);
                }
                ApplyAffordability(tool);
            }
            initialized = true;

            for (int i = 0; i < badges.Length; i++)
                badges[i].SetActive(tools.Exists(t => (int)t.category == i && t.state == UnlockState.Ready));

            foreach (var tech in readyNow)
            {
                string unlocks = TechManager.Instance.UnlocksText(tech);
                ToastUI.Show(unlocks != null ? $"Có thể nghiên cứu: {tech.displayName} (mở {unlocks})" : $"Có thể nghiên cứu: {tech.displayName}",
                    () => Open(ToolCategory.Research));
                SfxManager.Play(SfxKind.Chime, null, 0.5f);
            }
        }

        private void ApplyState(Tool tool)
        {
            tool.root.gameObject.SetActive(tool.state != UnlockState.Hidden);
            bool locked = tool.state == UnlockState.Locked;
            tool.lockBadge.gameObject.SetActive(locked);
            tool.glow.gameObject.SetActive(tool.state == UnlockState.Ready);
            tool.label.color = locked ? UiKit.DimTextColor : UiKit.TextColor;
            tool.label.text = tool.data is TechNode && tool.state == UnlockState.Unlocked ? $"{tool.name} √" : tool.name;
        }

        /// <summary>Đã mở nhưng thiếu tài nguyên để xây/gieo → icon mờ; giống đang chọn → nền sáng.</summary>
        private void ApplyAffordability(Tool tool)
        {
            float alpha = 1f;
            if (tool.state == UnlockState.Locked) alpha = 0.35f;
            else if (tool.state == UnlockState.Unlocked && ResourceManager.Instance != null)
            {
                List<ResourceAmount> cost = tool.data is BuildingData b ? b.costs : tool.data is CropData c ? c.plantCost : null;
                if (cost != null && !ResourceManager.Instance.CanAfford(cost)) alpha = 0.45f;
            }
            tool.icon.color = new Color(1f, 1f, 1f, alpha);

            bool selected = tool.data is CropData crop && FarmManager.Instance != null && FarmManager.Instance.IsSelected(crop);
            tool.background.color = selected ? new Color(0.5f, 0.38f, 0.14f, 0.98f) : UiKit.SlotColor;
        }

        // ─── Bấm ────────────────────────────────────────────────────────────
        public void Click(Object data)
        {
            Tool tool = FindTool(data);
            if (tool == null || tool.state == UnlockState.Hidden) return;
            TechManager tm = TechManager.Instance;
            switch (data)
            {
                case TechNode tech:
                    tm.TryUnlock(tech); // chưa đủ điều kiện thì TechManager tự báo lý do
                    break;
                case BuildingData building when tool.state == UnlockState.Unlocked:
                    BuildingPlacer.Instance.SelectBuilding(building);
                    break;
                case CropData crop when tool.state == UnlockState.Unlocked:
                    FarmManager.Instance.SelectCrop(crop);
                    EventBus.RaiseNotification($"Đã chọn giống {crop.displayName} — đến ruộng {(crop.fieldType == FieldType.Paddy ? "nước" : "cạn")} bấm E để gieo");
                    Refresh();
                    break;
                case BuildingData building:
                    ShowUnlockPath(tm.UnlockHint(building), tool.state);
                    break;
                case CropData crop:
                    ShowUnlockPath(tm.UnlockHint(crop), tool.state);
                    break;
            }
        }

        /// <summary>Mục còn khóa: báo cách mở; công nghệ mở ra nó đã sẵn sàng thì chuyển sang tab Nghiên cứu luôn.</summary>
        private void ShowUnlockPath(string hint, UnlockState state)
        {
            EventBus.RaiseNotification(hint);
            if (state == UnlockState.Ready) Open(ToolCategory.Research);
        }

        private Tool FindTool(Object data) => tools.Find(t => t.data == data);

        // ─── Dựng giao diện ─────────────────────────────────────────────────
        private void BuildUI()
        {
            var rect = (RectTransform)transform;
            UiKit.Anchor(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(600f, 44f));

            // Tab.
            var tabs = UiKit.Rect("Tabs", transform);
            UiKit.Stretch(tabs);
            var tabLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 8f;
            tabLayout.childAlignment = TextAnchor.MiddleCenter;
            tabLayout.childControlWidth = tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = tabLayout.childForceExpandHeight = true;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;
                var tab = UiKit.Rect($"Tab_{(ToolCategory)i}", tabs);
                tabBackgrounds[i] = UiKit.Fill(tab, UiKit.SlotColor);
                var button = tab.gameObject.AddComponent<Button>();
                button.targetGraphic = tabBackgrounds[i];
                button.onClick.AddListener(() => Toggle((ToolCategory)index));
                var text = UiKit.Rect("Text", tab);
                UiKit.Stretch(text);
                UiKit.Text(text, $"{TabNames[i]} <color=#BFA77A><size=75%>[{TabKeys[i]}]</size></color>", 21f, UiKit.TextColor);

                var badge = UiKit.Rect("Badge", tab);
                UiKit.Anchor(badge, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-6f, -6f), new Vector2(22f, 22f));
                var badgeImage = UiKit.Fill(badge, badgeSprite != null ? Color.white : new Color(0.85f, 0.2f, 0.15f), badgeSprite, raycast: false);
                badgeImage.preserveAspect = true;
                badges[i] = badge.gameObject;
                badge.gameObject.SetActive(false);
            }

            // Bảng công cụ (một trang mỗi tab), nằm ngay trên dải tab.
            panel = UiKit.Rect("ToolPanel", transform);
            UiKit.Anchor(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(0f, Cell + 16f));
            UiKit.Fill(panel, UiKit.PanelColor);
            var panelLayout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            panelLayout.padding = new RectOffset(8, 8, 8, 8);
            panelLayout.childControlWidth = panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = panelLayout.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < pages.Length; i++)
            {
                var page = UiKit.Rect($"Page_{(ToolCategory)i}", panel);
                var grid = page.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(Cell, Cell);
                grid.spacing = new Vector2(8f, 8f);
                grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                grid.constraintCount = 1;
                page.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                pages[i] = page;
                page.gameObject.SetActive(false);
            }

            foreach (var building in buildings)
                if (building != null) AddTool(ToolCategory.Build, building, building.displayName, building.icon);
            foreach (var crop in crops)
                if (crop != null) AddTool(ToolCategory.Farm, crop, crop.displayName, crop.icon);
            foreach (var tech in techs)
                if (tech != null) AddTool(ToolCategory.Research, tech, tech.displayName, tech.icon);

            panel.gameObject.SetActive(false);
        }

        private void AddTool(ToolCategory category, Object data, string name, Sprite iconSprite)
        {
            var tool = new Tool { category = category, data = data, name = name };
            tool.root = UiKit.Rect($"Tool_{name}", pages[(int)category]);

            // Viền vàng (Sẵn sàng) nằm sau nền, tràn ra 3px.
            var glow = UiKit.Rect("ReadyGlow", tool.root);
            UiKit.Stretch(glow, -3f);
            tool.glow = UiKit.Fill(glow, UiKit.AccentColor, raycast: false);

            var background = UiKit.Rect("Background", tool.root);
            UiKit.Stretch(background);
            tool.background = UiKit.Fill(background, UiKit.SlotColor);
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = tool.background;
            button.onClick.AddListener(() => Click(data));
            background.gameObject.AddComponent<TooltipTrigger>().Content = () => TooltipFor(data);

            var icon = UiKit.Rect("Icon", background);
            UiKit.Anchor(icon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(64f, 64f));
            tool.icon = UiKit.Fill(icon, Color.white, iconSprite, raycast: false);

            var label = UiKit.Rect("Label", background);
            label.anchorMin = Vector2.zero;
            label.anchorMax = new Vector2(1f, 0f);
            label.pivot = new Vector2(0.5f, 0f);
            label.offsetMin = new Vector2(3f, 3f);
            label.offsetMax = new Vector2(-3f, 25f);
            tool.label = UiKit.Text(label, name, 18f, UiKit.TextColor);
            tool.label.enableAutoSizing = true;
            tool.label.fontSizeMin = 13f;
            tool.label.fontSizeMax = 18f;
            tool.label.textWrappingMode = TextWrappingModes.NoWrap;
            tool.label.overflowMode = TextOverflowModes.Ellipsis;

            var lockBadge = UiKit.Rect("Lock", background);
            UiKit.Anchor(lockBadge, Vector2.one, Vector2.one, new Vector2(-2f, -2f), new Vector2(30f, 30f));
            tool.lockBadge = UiKit.Fill(lockBadge, Color.white, lockSprite, raycast: false);

            tools.Add(tool);
        }
    }
}
