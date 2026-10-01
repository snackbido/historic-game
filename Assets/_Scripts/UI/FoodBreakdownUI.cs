using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>Dòng chi tiết dưới "Thức ăn: N": "Thịt 3 · Lúa gạo 0 · Quả mọng 6 · …".</summary>
    public class FoodBreakdownUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            EventBus.OnResourceChanged += HandleChanged;
            Refresh();
        }

        private void OnDisable() => EventBus.OnResourceChanged -= HandleChanged;

        private void Start() => Refresh();

        private void HandleChanged(ResourceTypeData type, int _)
        {
            if (type != null && type.IsFood) Refresh();
        }

        private void Refresh()
        {
            if (label != null && ResourceManager.Instance != null) label.text = Describe(ResourceManager.Instance);
        }

        public static string Describe(ResourceManager resources)
        {
            var parts = new List<string>();
            foreach (var food in resources.FoodTypes)
                parts.Add($"{food.displayName} {resources.GetAmount(food)}");
            return string.Join(" · ", parts);
        }
    }
}
