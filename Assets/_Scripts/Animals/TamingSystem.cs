using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    [System.Serializable]
    public struct AnimalSaveEntry
    {
        public string animalId;
        public float posX;
        public float posY;
        public int state;
        public int tamingProgress;
        public float hunger;
        public bool productReady;
    }

    public class TamingSystem : MonoBehaviour
    {
        public static TamingSystem Instance { get; private set; }

        [SerializeField] private List<AnimalData> knownAnimalTypes = new List<AnimalData>();

        private readonly Dictionary<string, AnimalData> animalsById = new Dictionary<string, AnimalData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (var animal in knownAnimalTypes)
                if (animal != null) animalsById[animal.id] = animal;
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

        public List<AnimalSaveEntry> GetSaveData()
        {
            var data = new List<AnimalSaveEntry>();
            foreach (var animal in Object.FindObjectsByType<AnimalController>(FindObjectsInactive.Exclude))
            {
                if (animal.Data == null) continue;

                data.Add(new AnimalSaveEntry
                {
                    animalId = animal.Data.id,
                    posX = animal.transform.position.x,
                    posY = animal.transform.position.y,
                    state = (int)animal.State,
                    tamingProgress = animal.TamingProgress,
                    hunger = animal.Hunger,
                    productReady = animal.ProductReady
                });
            }
            return data;
        }

        public void LoadFromSaveData(List<AnimalSaveEntry> data)
        {
            foreach (var animal in Object.FindObjectsByType<AnimalController>(FindObjectsInactive.Exclude))
                if (animal != null) Destroy(animal.gameObject);

            if (data == null) return;

            foreach (var entry in data)
            {
                if (!animalsById.TryGetValue(entry.animalId, out var animalData) || animalData.prefab == null) continue;

                var instance = Instantiate(animalData.prefab, new Vector3(entry.posX, entry.posY, 0f), Quaternion.identity);
                instance.GetComponent<AnimalController>()?.LoadState(
                    animalData, (AnimalState)entry.state, entry.tamingProgress, entry.hunger, entry.productReady);
            }
        }
    }
}
