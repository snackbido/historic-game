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

        private void OnEnable() => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Unregister(this);

        private void Update()
        {
            if (!Regenerates || amountRemaining >= maxAmount) return;

            regenTimer += Time.deltaTime;
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

            if (amountRemaining <= 0 && !Regenerates)
                Destroy(gameObject);
        }
    }
}
