using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    [System.Serializable]
    public struct ResourceAmount
    {
        public ResourceTypeData type;
        public int amount;
    }

    [System.Serializable]
    public struct ResourceSaveEntry
    {
        public string resourceId;
        public int amount;
    }

    /// <summary>
    /// Kho tài nguyên chung. Lương thực có nhiều loại (thịt, gạo, quả mọng, sữa, cá, rau…) và một loại "gộp"
    /// (Thức ăn) = tổng mọi loại: chi phí ghi bằng loại gộp thì trả bằng loại nào cũng được, ưu tiên đồ dễ hỏng.
    /// M5b/E4: mỗi loại lương thực có sức chứa (đống tạm + các kho); vượt thì phí; đồ dễ hỏng để ngoài kho hỏng dần.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        /// <summary>Tắt để test cũ không bị thịt/cá/sữa tự hỏng làm lệch số; test E4 tự bật lại.</summary>
        public static bool SpoilageEnabled { get; set; } = true;

        [SerializeField] private List<ResourceTypeData> knownResourceTypes = new List<ResourceTypeData>();

        [Header("Kho chứa lương thực (E4)")]
        [Tooltip("Mỗi loại lương thực giữ được bao nhiêu khi chưa có kho (đống tạm ngoài trời)")]
        [SerializeField] private int baseFoodCapacity = 20;
        [Tooltip("Cứ bao nhiêu giây thì đồ dễ hỏng để ngoài kho hỏng một phần")]
        [SerializeField] private float spoilInterval = 15f;
        [Tooltip("Mỗi lần hỏng mất phần này của lượng đang để ngoài kho (ít nhất 1)")]
        [SerializeField, Range(0f, 1f)] private float spoilFraction = 0.25f;

        private float spoilTimer;
        private readonly Dictionary<ResourceTypeData, float> lastWasteNotice = new Dictionary<ResourceTypeData, float>();
        private const float WasteNoticeCooldown = 5f;

        private readonly Dictionary<ResourceTypeData, int> amounts = new Dictionary<ResourceTypeData, int>();
        private readonly Dictionary<string, ResourceTypeData> typesById = new Dictionary<string, ResourceTypeData>();
        private readonly List<ResourceTypeData> foodTypes = new List<ResourceTypeData>();
        private readonly List<ResourceTypeData> foodPools = new List<ResourceTypeData>();

        /// <summary>Các loại lương thực thật (không gồm loại gộp), theo thứ tự khai báo.</summary>
        public IReadOnlyList<ResourceTypeData> FoodTypes => foodTypes;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (var type in knownResourceTypes)
            {
                if (type == null) continue;
                typesById[type.id] = type;
                if (type.isFoodPool) foodPools.Add(type);
                else if (type.IsFood) foodTypes.Add(type);
            }
        }

        public int GetAmount(ResourceTypeData type)
        {
            if (type == null) return 0;
            if (type.isFoodPool)
            {
                int total = 0;
                foreach (var food in foodTypes) total += GetAmount(food);
                return total;
            }
            return amounts.TryGetValue(type, out int amount) ? amount : 0;
        }

        // ─── Sức chứa & hư hỏng (E4) ─────────────────────────────────────────
        /// <summary>Tổng sức chứa của mọi kho (theo cấp) — phần lương thực nằm trong này được bảo quản.</summary>
        public int StoredFoodCapacity
        {
            get
            {
                int capacity = 0;
                foreach (var building in BuildingInstance.All) capacity += building.StorageCapacity;
                return capacity;
            }
        }

        /// <summary>Mỗi loại lương thực giữ được tối đa bao nhiêu (đống tạm + kho).</summary>
        public int FoodCapacity => baseFoodCapacity + StoredFoodCapacity;

        /// <summary>Lượng đồ dễ hỏng đang để ngoài kho (sẽ hỏng dần); 0 với đồ không dễ hỏng.</summary>
        public int SpoilingAmount(ResourceTypeData type) =>
            type != null && type.IsFood && type.perishable ? Mathf.Max(0, GetAmount(type) - StoredFoodCapacity) : 0;

        private void Update()
        {
            if (!SpoilageEnabled) return;
            spoilTimer += Time.deltaTime;
            if (spoilTimer < spoilInterval) return;
            spoilTimer = 0f;
            SpoilOutsideFood();
        }

        private void SpoilOutsideFood()
        {
            var lost = new List<string>();
            foreach (var food in foodTypes)
            {
                int outside = SpoilingAmount(food);
                if (outside <= 0) continue;
                int spoiled = Mathf.Max(1, Mathf.RoundToInt(outside * spoilFraction));
                Set(food, GetAmount(food) - spoiled);
                lost.Add($"{spoiled} {food.displayName}");
            }
            if (lost.Count > 0)
                EventBus.RaiseNotification($"Hỏng {string.Join(", ", lost)} để ngoài kho — xây/nâng cấp kho để bảo quản");
        }

        public void AddResource(ResourceTypeData type, int amount)
        {
            if (amount <= 0 || type == null) return;
            if (type.isFoodPool)
            {
                // Cộng vào loại gộp = cộng vào loại mặc định của nó (vd quả mọng).
                ResourceTypeData target = type.poolDefault != null ? type.poolDefault : (foodTypes.Count > 0 ? foodTypes[0] : null);
                if (target != null) AddResource(target, amount);
                return;
            }

            if (type.IsFood)
            {
                int space = Mathf.Max(0, FoodCapacity - GetAmount(type));
                int wasted = amount - Mathf.Min(amount, space);
                amount -= wasted;
                if (wasted > 0) NotifyWaste(type, wasted);
                if (amount <= 0) return;
            }
            Set(type, GetAmount(type) + amount);
        }

        private void NotifyWaste(ResourceTypeData type, int wasted)
        {
            // Không spam: mỗi loại tối đa một thông báo / vài giây.
            if (lastWasteNotice.TryGetValue(type, out float last) && Time.time - last < WasteNoticeCooldown) return;
            lastWasteNotice[type] = Time.time;
            EventBus.RaiseNotification($"Kho đầy — phí {wasted} {type.displayName} (chứa tối đa {FoodCapacity}/loại)");
        }

        public bool TrySpend(ResourceTypeData type, int amount)
        {
            if (GetAmount(type) < amount) return false;
            if (type.isFoodPool)
            {
                SpendFromPool(amount);
                return true;
            }
            Set(type, GetAmount(type) - amount);
            return true;
        }

        /// <summary>Trả lương thực bằng loại nào cũng được: đồ dễ hỏng trước, rồi loại đang có nhiều nhất.</summary>
        private void SpendFromPool(int amount)
        {
            var order = new List<ResourceTypeData>(foodTypes);
            order.Sort((a, b) =>
            {
                if (a.perishable != b.perishable) return a.perishable ? -1 : 1;
                return GetAmount(b).CompareTo(GetAmount(a));
            });

            foreach (var food in order)
            {
                if (amount <= 0) break;
                int take = Mathf.Min(amount, GetAmount(food));
                if (take <= 0) continue;
                Set(food, GetAmount(food) - take);
                amount -= take;
            }
        }

        public bool CanAfford(List<ResourceAmount> costs)
        {
            // Gộp chi phí theo loại trước khi so (vd 3 Thức ăn + 2 Thịt cùng rút từ thịt).
            int poolNeed = 0;
            var direct = new Dictionary<ResourceTypeData, int>();
            foreach (var cost in costs)
            {
                if (cost.type == null) continue;
                if (cost.type.isFoodPool) poolNeed += cost.amount;
                else
                {
                    direct.TryGetValue(cost.type, out int need);
                    direct[cost.type] = need + cost.amount;
                }
            }

            int directFood = 0;
            foreach (var pair in direct)
            {
                if (GetAmount(pair.Key) < pair.Value) return false;
                if (pair.Key.IsFood) directFood += pair.Value;
            }
            if (poolNeed > 0 && foodPools.Count > 0 && GetAmount(foodPools[0]) - directFood < poolNeed) return false;
            return true;
        }

        public bool SpendAll(List<ResourceAmount> costs)
        {
            if (!CanAfford(costs)) return false;
            // Loại cụ thể trước, loại gộp sau (để gộp không "ăn" mất phần loại cụ thể cần).
            foreach (var cost in costs)
                if (cost.type != null && !cost.type.isFoodPool) TrySpend(cost.type, cost.amount);
            foreach (var cost in costs)
                if (cost.type != null && cost.type.isFoodPool) TrySpend(cost.type, cost.amount);
            return true;
        }

        private void Set(ResourceTypeData type, int value)
        {
            amounts[type] = value;
            EventBus.RaiseResourceChanged(type, value);
            if (type.IsFood)
                foreach (var pool in foodPools) EventBus.RaiseResourceChanged(pool, GetAmount(pool));
        }

        public List<ResourceSaveEntry> GetSaveData()
        {
            var data = new List<ResourceSaveEntry>();
            foreach (var pair in amounts)
                if (!pair.Key.isFoodPool) data.Add(new ResourceSaveEntry { resourceId = pair.Key.id, amount = pair.Value });
            return data;
        }

        public void LoadFromSaveData(List<ResourceSaveEntry> data)
        {
            // Đặt mọi loại về 0 trước (kể cả loại không có trong save) để UI cập nhật đúng.
            foreach (var type in new List<ResourceTypeData>(amounts.Keys)) Set(type, 0);
            amounts.Clear();
            if (data == null) return;

            foreach (var entry in data)
            {
                if (!typesById.TryGetValue(entry.resourceId, out var type)) continue;
                // Save cũ (trước E3) lưu "food" là một loại thật → chuyển vào loại mặc định của loại gộp.
                if (type.isFoodPool) AddResource(type, entry.amount);
                else Set(type, GetAmount(type) + entry.amount);
            }
        }
    }
}
