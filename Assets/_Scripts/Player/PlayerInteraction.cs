using UnityEngine;

namespace PrehistoricTribe
{
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private float interactRadius = 1.2f;
        [SerializeField] private LayerMask resourceLayer = ~0;

        private void Update()
        {
            if (Input.GetKeyDown(interactKey))
                TryHarvestNearest();
        }

        private void TryHarvestNearest()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, resourceLayer);
            ResourceNode nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                var node = hit.GetComponent<ResourceNode>();
                if (node == null) continue;

                float distance = Vector2.Distance(transform.position, hit.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = node;
                    nearestDistance = distance;
                }
            }

            nearest?.Harvest();
        }
    }
}
