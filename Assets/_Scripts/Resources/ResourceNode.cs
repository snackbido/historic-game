using UnityEngine;

namespace PrehistoricTribe
{
    public class ResourceNode : MonoBehaviour
    {
        [SerializeField] private ResourceTypeData resourceType;
        [SerializeField] private int amountRemaining = 10;
        [SerializeField] private int yieldPerHit = 1;

        public ResourceTypeData ResourceType => resourceType;
        public int AmountRemaining => amountRemaining;

        private void OnEnable() => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Unregister(this);

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
