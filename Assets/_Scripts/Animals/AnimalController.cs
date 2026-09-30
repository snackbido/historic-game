using UnityEngine;

namespace PrehistoricTribe
{
    public enum AnimalState
    {
        Wild,
        Tamed
    }

    [System.Serializable]
    public class AnimalSaveData
    {
        public string animalId;
        public string objectName;
        public float x;
        public float z;
        public float rotationY;
        public AnimalState state;
        public int tamingProgress;
        public float hunger;
        public bool productReady;
        public float hungerTimer;
        public float reproductionTimer;
        public float productionTimer;
    }

    public class AnimalController : MonoBehaviour
    {
        [SerializeField] private AnimalData data;

        [Tooltip("Các phần thân được tô màu theo trạng thái (hoang/đã thuần). Để trống = mọi Renderer con")]
        [SerializeField] private Renderer[] bodyRenderers;

        [Tooltip("Hiện khi đã thuần hóa (vd vòng cổ) — phân biệt rõ con hoang/con thuần")]
        [SerializeField] private GameObject tamedIndicator;

        [Tooltip("Hiện khi có sản phẩm sẵn sàng để thu")]
        [SerializeField] private GameObject productIndicator;

        private Color[] wildColors;
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
            if (bodyRenderers == null || bodyRenderers.Length == 0)
                bodyRenderers = System.Array.FindAll(GetComponentsInChildren<Renderer>(true), r => !IsIndicator(r.transform));

            wildColors = new Color[bodyRenderers.Length];
            for (int i = 0; i < bodyRenderers.Length; i++)
                wildColors[i] = bodyRenderers[i].material.color;

            UpdateVisual();
        }

        private bool IsIndicator(Transform t) =>
            (tamedIndicator != null && t.IsChildOf(tamedIndicator.transform)) ||
            (productIndicator != null && t.IsChildOf(productIndicator.transform));

        private void OnEnable() => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Unregister(this);

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

        public AnimalSaveData GetSaveData() => new AnimalSaveData
        {
            animalId = data != null ? data.id : null,
            objectName = name,
            x = transform.position.x,
            z = transform.position.z,
            rotationY = transform.eulerAngles.y,
            state = State,
            tamingProgress = TamingProgress,
            hunger = Hunger,
            productReady = ProductReady,
            hungerTimer = hungerTimer,
            reproductionTimer = reproductionTimer,
            productionTimer = productionTimer
        };

        /// <summary>Khôi phục trạng thái (vị trí do nơi gọi đặt lúc Instantiate).</summary>
        public void LoadFromSaveData(AnimalSaveData saved, AnimalData source)
        {
            data = source;
            State = saved.state;
            TamingProgress = saved.tamingProgress;
            Hunger = saved.hunger;
            ProductReady = saved.productReady;
            hungerTimer = saved.hungerTimer;
            reproductionTimer = saved.reproductionTimer;
            productionTimer = saved.productionTimer;
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
            bool tamed = State == AnimalState.Tamed;
            if (tamedIndicator != null) tamedIndicator.SetActive(tamed);
            if (productIndicator != null) productIndicator.SetActive(tamed && ProductReady);

            if (wildColors == null) return;
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] == null) continue;
                Color color = wildColors[i];
                if (tamed)
                {
                    // Có biểu tượng sản phẩm riêng thì giữ màu thuần; không có thì đổi màu vàng như bản 2D.
                    bool showReadyByColor = ProductReady && productIndicator == null;
                    color = showReadyByColor ? ProductReadyColor : color * TamedColor;
                }
                bodyRenderers[i].material.color = color;
            }
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

            Vector2 offset = Random.insideUnitCircle;
            Vector3 spawnPosition = transform.position + new Vector3(offset.x, 0f, offset.y);
            GameObject offspring = Instantiate(data.prefab, spawnPosition, Quaternion.identity);
            offspring.GetComponent<AnimalController>()?.InitializeAsTamed(data);
        }
    }
}
