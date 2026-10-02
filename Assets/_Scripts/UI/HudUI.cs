using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrehistoricTribe
{
    /// <summary>
    /// Dải tài nguyên trên cùng (RE-DESIGNUI bước 6): mỗi ô = icon + số. Gỗ, Thức ăn, Tri thức, Dân số luôn hiện;
    /// các loại khác (lúa bó, thóc, mạ, phân bón…) chỉ hiện khi có. Chi tiết (sức chứa, từng loại lương thực, nhà ở,
    /// người đói) nằm trong tooltip khi rê chuột. Số đổi thì nháy xanh (tăng) / đỏ (giảm).
    /// </summary>
    public class HudUI : MonoBehaviour
    {
        [System.Serializable]
        public struct Entry
        {
            public ResourceTypeData type;
            public bool alwaysShow;
            [TextArea] public string hint;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        [SerializeField] private Sprite peopleIcon;
        [SerializeField] private float populationRefreshInterval = 0.5f;

        private static readonly Color Up = new Color(0.45f, 0.9f, 0.45f);
        private static readonly Color Down = new Color(1f, 0.45f, 0.4f);
        private const float FlashSeconds = 0.6f;

        private class Slot
        {
            public Entry entry;
            public RectTransform root;
            public TMP_Text value;
            public int last = int.MinValue;
            public Color flash;
            public float flashUntil;
            public TooltipTrigger tooltip;
        }

        private readonly List<Slot> slots = new List<Slot>();
        private Slot people;
        private float populationTimer;

        public static HudUI Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            BuildUI();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            EventBus.OnResourceChanged += HandleResourceChanged;
            EventBus.OnBuildingPlaced += HandleBuilding;
            EventBus.OnBuildingUpgraded += HandleBuilding;
            EventBus.OnGameLoaded += RefreshResources;
        }

        private void OnDisable()
        {
            EventBus.OnResourceChanged -= HandleResourceChanged;
            EventBus.OnBuildingPlaced -= HandleBuilding;
            EventBus.OnBuildingUpgraded -= HandleBuilding;
            EventBus.OnGameLoaded -= RefreshResources;
        }

        private void Start()
        {
            RefreshResources();
            RefreshPopulation();
        }

        private void HandleResourceChanged(ResourceTypeData _, int __) => RefreshResources();
        private void HandleBuilding(BuildingInstance _) => RefreshResources(); // kho mới → sức chứa đổi (tooltip)

        /// <summary>Ô của loại tài nguyên này đang hiện?</summary>
        public bool IsShown(ResourceTypeData type) => slots.Find(s => s.entry.type == type) is Slot slot && slot.root.gameObject.activeSelf;

        public string ValueText(ResourceTypeData type) => slots.Find(s => s.entry.type == type)?.value.text;
        public string PeopleText => people?.value.text;
        public string TooltipFor(ResourceTypeData type) => slots.Find(s => s.entry.type == type) is Slot slot ? TooltipOf(slot.entry) : null;

        private void RefreshResources()
        {
            if (ResourceManager.Instance == null) return;
            foreach (var slot in slots)
            {
                int amount = ResourceManager.Instance.GetAmount(slot.entry.type);
                slot.root.gameObject.SetActive(slot.entry.alwaysShow || amount > 0);
                SetValue(slot, amount, amount.ToString());
                TooltipUI.Refresh(slot.tooltip, TooltipOf(slot.entry));
            }
        }

        private void RefreshPopulation()
        {
            NpcManager manager = NpcManager.Instance;
            if (manager == null || people == null) return;
            int starving = manager.StarvingCount;
            SetValue(people, manager.Population, starving > 0 ? $"{manager.Population} <color=#FF6B6B>({starving} đói)</color>" : manager.Population.ToString());
            TooltipUI.Refresh(people.tooltip, PopulationUI.Describe(manager));
        }

        private void SetValue(Slot slot, int amount, string text)
        {
            if (slot.last != int.MinValue && amount != slot.last)
            {
                slot.flash = amount > slot.last ? Up : Down;
                slot.flashUntil = Time.unscaledTime + FlashSeconds;
            }
            slot.last = amount;
            slot.value.text = text;
        }

        private void Update()
        {
            populationTimer -= Time.unscaledDeltaTime;
            if (populationTimer <= 0f)
            {
                populationTimer = populationRefreshInterval;
                RefreshPopulation();
            }

            foreach (var slot in slots) Fade(slot);
            if (people != null) Fade(people);
        }

        private static void Fade(Slot slot)
        {
            float left = slot.flashUntil - Time.unscaledTime;
            slot.value.color = left > 0f ? Color.Lerp(UiKit.TextColor, slot.flash, left / FlashSeconds) : UiKit.TextColor;
        }

        private static string TooltipOf(Entry entry)
        {
            if (entry.type == null) return null;
            string text = $"<b>{entry.type.displayName}</b>";
            if (entry.type.isFoodPool && ResourceManager.Instance != null)
                text += "\n" + FoodBreakdownUI.Describe(ResourceManager.Instance).Replace(" · ", "\n");
            if (!string.IsNullOrEmpty(entry.hint)) text += $"\n<size=85%>{entry.hint}</size>";
            return text;
        }

        // ─── Dựng giao diện ─────────────────────────────────────────────────
        private void BuildUI()
        {
            var rect = (RectTransform)transform;
            UiKit.Anchor(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(0f, 44f));
            var layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var entry in entries)
            {
                if (entry.type == null) continue;
                var slot = CreateSlot(entry.type.id, entry.type.icon);
                slot.entry = entry;
                Entry captured = entry;
                slot.tooltip.Content = () => TooltipOf(captured);
                slots.Add(slot);
            }

            people = CreateSlot("people", peopleIcon);
            people.tooltip.Content = () => NpcManager.Instance != null ? $"<b>Dân số</b>\n{PopulationUI.Describe(NpcManager.Instance).Replace(" · ", "\n")}" : null;
        }

        private Slot CreateSlot(string name, Sprite icon)
        {
            var root = UiKit.Rect($"Slot_{name}", transform);
            UiKit.Fill(root, UiKit.PanelColor);
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 12, 4, 4);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var iconRect = UiKit.Rect("Icon", root);
            UiKit.Fill(iconRect, Color.white, icon, raycast: false);
            var iconSize = iconRect.gameObject.AddComponent<LayoutElement>();
            iconSize.preferredWidth = iconSize.preferredHeight = 32f;

            var valueRect = UiKit.Rect("Value", root);
            var value = UiKit.Text(valueRect, "0", 22f, UiKit.TextColor, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            value.textWrappingMode = TextWrappingModes.NoWrap;
            valueRect.gameObject.AddComponent<LayoutElement>().minWidth = 28f;

            return new Slot { root = root, value = value, tooltip = root.gameObject.AddComponent<TooltipTrigger>() };
        }
    }
}
