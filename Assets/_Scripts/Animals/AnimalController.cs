using UnityEngine;

namespace PrehistoricTribe
{
    public enum AnimalState
    {
        Wild,
        Tamed
    }

    public class AnimalController : MonoBehaviour
    {
        [SerializeField] private AnimalData data;

        private SpriteRenderer spriteRenderer;
        private float hungerTimer;
        private float reproductionTimer;
        private float productionTimer;

        private static readonly Color TamedColor = new Color(0.6f, 0.9f, 0.6f);
        private static readonly Color ProductReadyColor = new Color(1f, 0.85f, 0.2f);

        public AnimalData Data => data;
        public AnimalState State { get; private set; } = AnimalState.Wild;
        public int TamingProgress { get; private set; }
        public float Hunger { get; private set; } = 100f;
        public bool ProductReady { get; private set; }

        private bool IsWellFed => Hunger >= data.hungerThresholdForNeeds;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (State != AnimalState.Tamed) return;

            UpdateHunger();
            if (!IsWellFed) return;

            UpdateReproduction();
            UpdateProduction();
        }

        public void Feed()
        {
            if (State == AnimalState.Wild)
            {
                TamingProgress++;
                if (TamingProgress >= data.feedingsToTame)
                    Tame();
            }
            else
            {
                Hunger = 100f;
            }
        }

        public bool CollectProduct()
        {
            if (!ProductReady) return false;

            foreach (var product in data.products)
                ResourceManager.Instance.AddResource(product.type, product.amount);

            ProductReady = false;
            productionTimer = 0f;
            UpdateVisual();
            return true;
        }

        public void InitializeAsTamed(AnimalData source)
        {
            data = source;
            TamingProgress = data.feedingsToTame;
            Tame();
        }

        public void LoadState(AnimalData source, AnimalState loadedState, int tamingProgress, float hunger, bool productReady)
        {
            data = source;
            State = loadedState;
            TamingProgress = tamingProgress;
            Hunger = hunger;
            ProductReady = productReady;
            hungerTimer = 0f;
            reproductionTimer = 0f;
            productionTimer = 0f;
            UpdateVisual();
        }

        private void Tame()
        {
            State = AnimalState.Tamed;
            Hunger = 100f;
            hungerTimer = 0f;
            reproductionTimer = 0f;
            productionTimer = 0f;
            ProductReady = false;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (spriteRenderer == null || State != AnimalState.Tamed) return;
            spriteRenderer.color = ProductReady ? ProductReadyColor : TamedColor;
        }

        private void UpdateHunger()
        {
            hungerTimer += Time.deltaTime;
            if (hungerTimer < data.hungerDecayInterval) return;

            hungerTimer = 0f;
            Hunger = Mathf.Max(0f, Hunger - 1f);
        }

        private void UpdateReproduction()
        {
            reproductionTimer += Time.deltaTime;
            if (reproductionTimer < data.reproductionInterval) return;

            reproductionTimer = 0f;
            Reproduce();
        }

        private void UpdateProduction()
        {
            if (ProductReady) return;

            productionTimer += Time.deltaTime;
            if (productionTimer >= data.productionInterval)
            {
                ProductReady = true;
                UpdateVisual();
            }
        }

        private void Reproduce()
        {
            if (data.prefab == null) return;

            Vector3 spawnPosition = transform.position + (Vector3)Random.insideUnitCircle;
            GameObject offspring = Instantiate(data.prefab, spawnPosition, Quaternion.identity);
            offspring.GetComponent<AnimalController>()?.InitializeAsTamed(data);
        }
    }
}
