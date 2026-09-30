using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Thanh máu nhỏ trên đầu (luôn quay về phía camera). Hiện khi bị thương,
    /// hoặc khi NPC đang được người chơi chọn.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [Tooltip("Tùy chọn: NPC sở hữu — thanh máu hiện khi NPC đang được chọn")]
        [SerializeField] private NpcController npc;
        [SerializeField] private GameObject barRoot;
        [Tooltip("Phần màu thể hiện lượng máu; gốc (pivot) ở mép trái để co giãn theo chiều ngang")]
        [SerializeField] private Transform fill;
        [SerializeField] private Renderer fillRenderer;
        [SerializeField] private Color healthyColor = new Color(0.45f, 0.85f, 0.3f);
        [SerializeField] private Color hurtColor = new Color(0.9f, 0.3f, 0.2f);

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock block;
        private Vector3 fullScale;

        private void Awake()
        {
            if (fill != null) fullScale = fill.localScale;
            block = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (health == null || barRoot == null) return;

            float ratio = health.Max > 0f ? health.Current / health.Max : 0f;
            bool show = !health.IsDead && (ratio < 0.999f || (npc != null && npc.IsSelected));
            if (barRoot.activeSelf != show) barRoot.SetActive(show);
            if (!show) return;

            Camera cam = Camera.main;
            if (cam != null) barRoot.transform.rotation = cam.transform.rotation;

            if (fill != null) fill.localScale = new Vector3(fullScale.x * ratio, fullScale.y, fullScale.z);
            if (fillRenderer != null)
            {
                fillRenderer.GetPropertyBlock(block);
                block.SetColor(ColorId, Color.Lerp(hurtColor, healthyColor, ratio));
                fillRenderer.SetPropertyBlock(block);
            }
        }
    }
}
