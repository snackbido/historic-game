using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Nguồn tài nguyên khai thác được (cây, ao cá…). Loại "sinh sôi" (ao cá) không biến mất khi cạn
    /// mà hồi lại dần theo thời gian.
    /// </summary>
    public class ResourceNode : MonoBehaviour
    {
        [SerializeField] private ResourceTypeData resourceType;
        [SerializeField] private int amountRemaining = 10;
        [SerializeField] private int yieldPerHit = 1;

        [Tooltip("Tên hành động hiện trên gợi ý/thông báo, vd \"Chặt cây\", \"Đánh cá\"")]
        [SerializeField] private string actionName = "Chặt cây";
        [Tooltip("Khoảng cách (m) từ tâm đối tượng mà dân làng đứng làm việc được (ao to thì xa hơn)")]
        [SerializeField] private float workRange = 1.1f;

        [Header("Sinh sôi lại (0 = không, cạn thì biến mất)")]
        [SerializeField] private float regenInterval;
        [SerializeField] private int maxAmount = 10;

        private float regenTimer;

        public ResourceTypeData ResourceType => resourceType;
        public int AmountRemaining => amountRemaining;
        public string ActionName => actionName;
        public float WorkRange => workRange;
        public bool Regenerates => regenInterval > 0f;
        public bool IsDepleted => amountRemaining <= 0;
        /// <summary>Tạm ngừng sinh sôi lại (vd ao cạn khi hạn hán).</summary>
        public bool RegenPaused { get; set; }

        private void OnEnable() => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Unregister(this);

        private void Update()
        {
            if (!Regenerates || RegenPaused || amountRemaining >= maxAmount) return;

            regenTimer += GamePace.Scaled(Time.deltaTime);
            if (regenTimer < regenInterval) return;
            regenTimer = 0f;
            amountRemaining++;
        }

        public void Harvest()
        {
            int amount = Mathf.Min(yieldPerHit, amountRemaining);
            if (amount <= 0) return;

            amountRemaining -= amount;
            ResourceManager.Instance.AddResource(resourceType, amount);

            bool isWood = resourceType != null && resourceType.id == "wood";
            Vector3 at = transform.position + Vector3.up * (Regenerates ? 0.1f : 0.6f);
            VfxManager.Play(Regenerates ? VfxKind.Splash : isWood ? VfxKind.WoodChips : VfxKind.Dust, at);

            if (amountRemaining <= 0 && !Regenerates)
            {
                if (isWood) VfxManager.Play(VfxKind.Leaves, transform.position + Vector3.up * 1.6f); // cây đổ
                Destroy(gameObject);
            }
        }

        // ─── Lưu / tải (M7) ─────────────────────────────────────────────────
        public ResourceNodeSaveData GetSaveData() => new ResourceNodeSaveData
        {
            objectName = name,
            resourceId = resourceType != null ? resourceType.id : null,
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z,
            rotationY = transform.eulerAngles.y,
            amount = amountRemaining
        };

        /// <summary>Đặt lại số còn lại theo bản lưu (cây chặt dở, ao vơi cá).</summary>
        public void LoadFromSaveData(ResourceNodeSaveData data)
        {
            if (data == null) return;
            amountRemaining = Mathf.Max(0, data.amount);
            regenTimer = 0f;
        }
    }

    [System.Serializable]
    public class ResourceNodeSaveData
    {
        public string objectName;
        public string resourceId;
        public float x, y, z;
        public float rotationY;
        public int amount;
    }
}
