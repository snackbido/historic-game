using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>Vòng sáng nhấp nháy dưới đối tượng tương tác được gần nhất (ẩn khi đang đặt công trình).</summary>
    public class InteractionHighlight : MonoBehaviour
    {
        [SerializeField] private PlayerInteraction interaction;
        [SerializeField] private GameObject ring;
        [SerializeField] private float pulseSpeed = 5f;
        [SerializeField] private float pulseAmount = 0.06f;
        [SerializeField] private float heightOffset = 0.03f;

        private Vector3 baseScale;

        private void Awake()
        {
            if (ring != null) baseScale = ring.transform.localScale;
        }

        private void LateUpdate()
        {
            if (ring == null) return;

            MonoBehaviour target = interaction != null ? interaction.Nearest : null;
            bool placing = BuildingPlacer.Instance != null && BuildingPlacer.Instance.IsPlacing;
            bool show = target != null && !placing;

            if (ring.activeSelf != show) ring.SetActive(show);
            if (!show) return;

            Vector3 position = target.transform.position;
            position.y = heightOffset;
            ring.transform.position = position;
            ring.transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
        }
    }
}
