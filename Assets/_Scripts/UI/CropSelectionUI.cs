using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrehistoricTribe
{
    public class CropSelectionUI : MonoBehaviour
    {
        [SerializeField] private List<CropData> availableCrops = new List<CropData>();
        [SerializeField] private Button buttonPrefab;
        [SerializeField] private Transform buttonContainer;

        private void Start() => Rebuild();

        private void OnEnable() => EventBus.OnTechUnlocked += HandleTechUnlocked;

        private void OnDisable() => EventBus.OnTechUnlocked -= HandleTechUnlocked;

        private void HandleTechUnlocked(TechNode tech) => Rebuild();

        private void Rebuild()
        {
            if (buttonPrefab == null || buttonContainer == null) return;

            foreach (Transform child in buttonContainer)
                Destroy(child.gameObject);

            foreach (var crop in availableCrops)
            {
                if (crop == null) continue;
                bool unlocked = crop.unlockedByDefault ||
                                 (TechManager.Instance != null && TechManager.Instance.IsCropUnlocked(crop));
                if (!unlocked) continue;

                Button button = Instantiate(buttonPrefab, buttonContainer);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = crop.displayName;

                button.onClick.AddListener(() => FarmManager.Instance.SelectCrop(crop));
            }
        }
    }
}
