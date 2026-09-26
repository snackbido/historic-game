using TMPro;
using UnityEngine;

namespace PrehistoricTribe
{
    public class ResourceBarUI : MonoBehaviour
    {
        [SerializeField] private ResourceTypeData displayedResource;
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            EventBus.OnResourceChanged += HandleResourceChanged;
            if (ResourceManager.Instance != null)
                UpdateLabel(ResourceManager.Instance.GetAmount(displayedResource));
        }

        private void OnDisable()
        {
            EventBus.OnResourceChanged -= HandleResourceChanged;
        }

        private void HandleResourceChanged(ResourceTypeData type, int newAmount)
        {
            if (type == displayedResource)
                UpdateLabel(newAmount);
        }

        private void UpdateLabel(int amount)
        {
            if (label != null && displayedResource != null)
                label.text = $"{displayedResource.displayName}: {amount}";
        }
    }
}
