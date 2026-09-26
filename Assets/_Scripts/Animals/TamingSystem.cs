using UnityEngine;

namespace PrehistoricTribe
{
    public class TamingSystem : MonoBehaviour
    {
        public static TamingSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public bool TryInteract(AnimalController animal)
        {
            if (animal == null || animal.Data == null) return false;

            switch (animal.State)
            {
                case AnimalState.Wild:
                    return TryFeed(animal);

                case AnimalState.Tamed:
                    return animal.ProductReady ? animal.CollectProduct() : TryFeed(animal);

                default:
                    return false;
            }
        }

        private bool TryFeed(AnimalController animal)
        {
            if (!ResourceManager.Instance.SpendAll(animal.Data.feedCost)) return false;
            animal.Feed();
            return true;
        }
    }
}
