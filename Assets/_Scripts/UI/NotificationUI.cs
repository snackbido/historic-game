using TMPro;
using UnityEngine;

namespace PrehistoricTribe
{
    public class NotificationUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float displayDuration = 2.5f;

        private float hideTime;

        private void Start()
        {
            if (label != null) label.text = string.Empty;
        }

        private void OnEnable() => EventBus.OnNotification += HandleNotification;

        private void OnDisable() => EventBus.OnNotification -= HandleNotification;

        private void HandleNotification(string message)
        {
            if (label == null) return;
            label.text = message;
            hideTime = Time.time + displayDuration;
        }

        private void Update()
        {
            if (label == null || string.IsNullOrEmpty(label.text)) return;
            if (Time.time >= hideTime) label.text = string.Empty;
        }
    }
}
