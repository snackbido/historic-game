using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrehistoricTribe
{
    public class VillagerPanelUI : MonoBehaviour
    {
        [SerializeField] private Button entryPrefab;
        [SerializeField] private Transform entryContainer;

        private void OnEnable() => EventBus.OnSelectionChanged += Rebuild;

        private void OnDisable() => EventBus.OnSelectionChanged -= Rebuild;

        private void Start() => Rebuild();

        private void Rebuild()
        {
            if (entryPrefab == null || entryContainer == null) return;

            foreach (Transform child in entryContainer)
                Destroy(child.gameObject);

            if (SelectionManager.Instance == null) return;

            foreach (var villager in SelectionManager.Instance.Selected)
            {
                Button entry = Instantiate(entryPrefab, entryContainer);
                TMP_Text label = entry.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = $"{villager.Data.displayName} (Đói {villager.Hunger:0} Ngủ {villager.Sleep:0} Ấm {villager.Warmth:0})";
            }
        }
    }
}
