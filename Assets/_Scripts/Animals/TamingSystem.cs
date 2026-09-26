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
                    return animal.ProductReady ? CollectProduct(animal) : TryFeed(animal);

                default:
                    return false;
            }
        }

        private bool TryFeed(AnimalController animal)
        {
            if (!ResourceManager.Instance.SpendAll(animal.Data.feedCost))
            {
                EventBus.RaiseNotification($"Không đủ tài nguyên để cho {animal.Data.displayName} ăn");
                return false;
            }

            bool wasWild = animal.State == AnimalState.Wild;
            animal.Feed();

            if (wasWild && animal.State == AnimalState.Tamed)
                EventBus.RaiseNotification($"Đã thuần hóa {animal.Data.displayName}!");
            else if (wasWild)
                EventBus.RaiseNotification($"Đã cho ăn ({animal.TamingProgress}/{animal.Data.feedingsToTame})");
            else
                EventBus.RaiseNotification($"Đã cho {animal.Data.displayName} ăn");

            return true;
        }

        private bool CollectProduct(AnimalController animal)
        {
            var products = animal.Data.products;
            if (!animal.CollectProduct()) return false;

            string summary = string.Join(", ", products.ConvertAll(p => $"+{p.amount} {p.type.displayName}"));
            EventBus.RaiseNotification($"Thu hoạch từ {animal.Data.displayName}: {summary}");
            return true;
        }
    }
}
