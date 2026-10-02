using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrehistoricTribe
{
    public class TechTreeUI : MonoBehaviour
    {
        [SerializeField] private List<TechNode> allTechs = new List<TechNode>();
        [SerializeField] private Button entryButtonPrefab;
        [SerializeField] private Transform entryContainer;
        [Tooltip("Nút rộng hơn nút thường để đủ chỗ ghi chi phí")]
        [SerializeField] private float entryWidth = 230f;

        private readonly Dictionary<TechNode, Button> buttons = new Dictionary<TechNode, Button>();

        private Color NormalTextColor
        {
            get
            {
                TMP_Text label = entryButtonPrefab != null ? entryButtonPrefab.GetComponentInChildren<TMP_Text>() : null;
                return label != null ? label.color : Color.black;
            }
        }

        private void Start()
        {
            if (entryButtonPrefab == null || entryContainer == null) return;

            foreach (var tech in allTechs)
            {
                if (tech == null) continue;

                Button button = Instantiate(entryButtonPrefab, entryContainer);
                ((RectTransform)button.transform).sizeDelta = new Vector2(entryWidth, ((RectTransform)button.transform).sizeDelta.y);
                buttons[tech] = button;
                button.onClick.AddListener(() => TechManager.Instance.TryUnlock(tech));
                RefreshEntry(tech);
            }
        }

        private void OnEnable()
        {
            EventBus.OnTechUnlocked += HandleTechUnlocked;
            EventBus.OnGameLoaded += RefreshAll;
        }

        private void OnDisable()
        {
            EventBus.OnTechUnlocked -= HandleTechUnlocked;
            EventBus.OnGameLoaded -= RefreshAll;
        }

        private void RefreshAll()
        {
            foreach (var tech in buttons.Keys)
                RefreshEntry(tech);
        }

        // Mở một công nghệ có thể gỡ khóa công nghệ khác (điều kiện tiên quyết) → làm mới tất cả.
        private void HandleTechUnlocked(TechNode tech) => RefreshAll();

        private void RefreshEntry(TechNode tech)
        {
            if (!buttons.TryGetValue(tech, out var button)) return;

            bool unlocked = TechManager.Instance != null && TechManager.Instance.IsUnlocked(tech);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                // Chưa mở: hiện luôn chi phí; còn thiếu công nghệ trước thì mờ + "(khóa)". Bấm vào mà chưa đủ
                // điều kiện thì TechManager báo lý do.
                bool prerequisitesMet = tech.prerequisites.TrueForAll(p => p == null || TechManager.Instance.IsUnlocked(p));
                label.text = unlocked ? $"{tech.displayName} (đã mở)"
                    : prerequisitesMet ? $"{tech.displayName}: {TechManager.CostText(tech.cost)}"
                    : tech.displayName + LockedEntry.LockedSuffix;
                label.color = unlocked || prerequisitesMet ? NormalTextColor : LockedEntry.LockedTextColor;
            }

            button.interactable = !unlocked;
        }
    }
}
