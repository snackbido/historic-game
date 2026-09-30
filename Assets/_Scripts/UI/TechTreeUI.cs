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

        private readonly Dictionary<TechNode, Button> buttons = new Dictionary<TechNode, Button>();

        private void Start()
        {
            if (entryButtonPrefab == null || entryContainer == null) return;

            foreach (var tech in allTechs)
            {
                if (tech == null) continue;

                Button button = Instantiate(entryButtonPrefab, entryContainer);
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

        private void HandleTechUnlocked(TechNode tech)
        {
            if (buttons.ContainsKey(tech))
                RefreshEntry(tech);
        }

        private void RefreshEntry(TechNode tech)
        {
            if (!buttons.TryGetValue(tech, out var button)) return;

            bool unlocked = TechManager.Instance != null && TechManager.Instance.IsUnlocked(tech);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = unlocked ? $"{tech.displayName} (đã mở)" : tech.displayName;

            button.interactable = !unlocked;
        }
    }
}
