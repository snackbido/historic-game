using UnityEngine;

namespace PrehistoricTribe
{
    public class ResourceNode : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType = ResourceType.Wood;
        [SerializeField] private int amountRemaining = 10;
        [SerializeField] private int yieldPerHit = 1;

        public ResourceType ResourceType => resourceType;

        public void Harvest()
        {
            int amount = Mathf.Min(yieldPerHit, amountRemaining);
            if (amount <= 0) return;

            amountRemaining -= amount;
            ResourceManager.Instance.AddResource(resourceType, amount);

            if (amountRemaining <= 0)
                Destroy(gameObject);
        }
    }
}
