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
                TryInteractNearest();
        }

        private void TryInteractNearest()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, resourceLayer);
            Component nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                Component interactable = hit.GetComponent<ResourceNode>();
                if (interactable == null) interactable = hit.GetComponent<FarmPlot>();
                if (interactable == null) interactable = hit.GetComponent<AnimalController>();
                if (interactable == null) continue;

                float distance = Vector2.Distance(transform.position, hit.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = interactable;
                    nearestDistance = distance;
                }
            }

            switch (nearest)
            {
                case ResourceNode node:
                    node.Harvest();
                    break;
                case FarmPlot plot:
                    FarmManager.Instance.TryInteract(plot);
                    break;
                case AnimalController animal:
                    TamingSystem.Instance.TryInteract(animal);
                    break;
            }
        }
    }
}
