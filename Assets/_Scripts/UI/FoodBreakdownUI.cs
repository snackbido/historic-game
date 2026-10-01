using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Dòng chi tiết dưới "Thức ăn: N": "Sức chứa: 50/loại · Thịt 12 (hỏng dần) · Lúa gạo 0 · …".
    /// Đánh dấu loại dễ hỏng đang để ngoài kho (E4).
    /// </summary>
    public class FoodBreakdownUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            EventBus.OnResourceChanged += HandleChanged;
            EventBus.OnBuildingPlaced += HandleBuilding;
            EventBus.OnBuildingUpgraded += HandleBuilding;
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.OnResourceChanged -= HandleChanged;
            EventBus.OnBuildingPlaced -= HandleBuilding;
            EventBus.OnBuildingUpgraded -= HandleBuilding;
        }

        private void Start() => Refresh();

        private void HandleChanged(ResourceTypeData type, int _)
        {
            if (type != null && type.IsFood) Refresh();
        }

        // Kho mới/nâng cấp → sức chứa đổi.
        private void HandleBuilding(BuildingInstance _) => Refresh();

        private void Refresh()
        {
            if (label != null && ResourceManager.Instance != null) label.text = Describe(ResourceManager.Instance);
        }

        public static string Describe(ResourceManager resources)
        {
            var parts = new List<string> { $"Sức chứa: {resources.FoodCapacity}/loại" };
            foreach (var food in resources.FoodTypes)
            {
                string entry = $"{food.displayName} {resources.GetAmount(food)}";
                if (resources.SpoilingAmount(food) > 0) entry += " (hỏng dần)";
                parts.Add(entry);
            }
            return string.Join(" · ", parts);
        }
    }
}
