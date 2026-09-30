using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrehistoricTribe
{
    public class BuildMenuUI : MonoBehaviour
    {
        [SerializeField] private List<BuildingData> availableBuildings = new List<BuildingData>();
        [SerializeField] private Button buttonPrefab;
        [SerializeField] private Transform buttonContainer;

        private void Start() => Rebuild();

        private void OnEnable()
        {
            EventBus.OnTechUnlocked += HandleTechUnlocked;
            EventBus.OnGameLoaded += Rebuild;
        }

        private void OnDisable()
        {
            EventBus.OnTechUnlocked -= HandleTechUnlocked;
            EventBus.OnGameLoaded -= Rebuild;
        }

        private void HandleTechUnlocked(TechNode tech) => Rebuild();

        private void Rebuild()
        {
            if (buttonPrefab == null || buttonContainer == null) return;

            foreach (Transform child in buttonContainer)
                Destroy(child.gameObject);

            foreach (var building in availableBuildings)
            {
                if (building == null) continue;
                bool unlocked = building.unlockedByDefault ||
                                 (TechManager.Instance != null && TechManager.Instance.IsBuildingUnlocked(building));
                if (!unlocked) continue;

                Button button = Instantiate(buttonPrefab, buttonContainer);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = building.displayName;

                button.onClick.AddListener(() => BuildingPlacer.Instance.SelectBuilding(building));
            }
        }
    }
}
