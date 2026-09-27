using System.Collections;
using UnityEngine;

namespace PrehistoricTribe
{
    public class ResourceNode : MonoBehaviour
    {
        [SerializeField] private ResourceTypeData resourceType;
        [SerializeField] private int amountRemaining = 10;
        [SerializeField] private int yieldPerHit = 1;

        [Tooltip("Thời gian (giây) chờ để mọc lại sau khi bị thu hoạch hết, chọn ngẫu nhiên trong khoảng min-max")]
        [SerializeField] private float respawnTimeMin = 60f;
        [SerializeField] private float respawnTimeMax = 120f;

        private int maxAmount;
        private SpriteRenderer spriteRenderer;
        private Collider2D nodeCollider;

        public ResourceTypeData ResourceType => resourceType;

        private void Awake()
        {
            maxAmount = amountRemaining;
            spriteRenderer = GetComponent<SpriteRenderer>();
            nodeCollider = GetComponent<Collider2D>();
        }

        public void Harvest()
        {
            int amount = Mathf.Min(yieldPerHit, amountRemaining);
            if (amount <= 0) return;

            amountRemaining -= amount;
            ResourceManager.Instance.AddResource(resourceType, amount);
            EventBus.RaiseNotification($"+{amount} {resourceType.displayName}");

            if (amountRemaining <= 0)
                StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            SetDepleted(true);
            yield return new WaitForSeconds(Random.Range(respawnTimeMin, respawnTimeMax));
            amountRemaining = maxAmount;
            SetDepleted(false);
        }

        private void SetDepleted(bool depleted)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = !depleted;
            if (nodeCollider != null) nodeCollider.enabled = !depleted;
        }
    }
}
