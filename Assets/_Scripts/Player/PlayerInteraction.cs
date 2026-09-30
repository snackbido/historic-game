using UnityEngine;

namespace PrehistoricTribe
{
    public class PlayerInteraction : MonoBehaviour
    {
        // Bản 2D dùng OverlapCircle nên chạm được mép collider (~nửa ô) — cộng bù phần đó vào bán kính.
        private const float TargetExtent = 0.5f;

        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private float interactRadius = 1.2f;

        /// <summary>Đối tượng tương tác được gần nhất (null nếu không có) — dùng cho highlight/gợi ý UI.</summary>
        public MonoBehaviour Nearest { get; private set; }

        private void Update()
        {
            Nearest = InteractableRegistry.FindNearest(transform.position, interactRadius + TargetExtent);

            if (Input.GetKeyDown(interactKey))
                Interact(Nearest);
        }

        private static void Interact(MonoBehaviour target)
        {
            switch (target)
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
