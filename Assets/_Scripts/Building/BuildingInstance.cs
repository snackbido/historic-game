using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    public class BuildingInstance : MonoBehaviour
    {
        private static readonly List<BuildingInstance> all = new List<BuildingInstance>();
        /// <summary>Mọi công trình đang có trên bản đồ (vd tính sức chứa dân số theo số lều).</summary>
        public static IReadOnlyList<BuildingInstance> All => all;

        [Tooltip("Model từng cấp, phần tử 0 = cấp 1 — chỉ bật model của cấp hiện tại")]
        [SerializeField] private List<GameObject> levelModels = new List<GameObject>();
        [Tooltip("Vòng sáng dưới công trình khi đang được chọn")]
        [SerializeField] private GameObject selectionRing;
        [Tooltip("Đống đổ nát (gỗ gãy, đá) hiện khi công trình sập")]
        [SerializeField] private GameObject rubble;

        public BuildingData Data { get; private set; }
        public Vector3Int GridPosition { get; private set; }
        public int Level { get; private set; } = 1;
        public bool IsMaxLevel => Data == null || Level >= Data.MaxLevel;
        public BuildingLevel CurrentLevel => Data?.GetLevel(Level);
        public BuildingLevel NextLevel => IsMaxLevel ? null : Data.GetLevel(Level + 1);
        public int Housing => CurrentLevel?.housing ?? 0;
        /// <summary>Kho đã sập thì không giữ được gì.</summary>
        public int StorageCapacity => IsCollapsed ? 0 : CurrentLevel?.storageCapacity ?? 0;
        public string LevelName => CurrentLevel != null && !string.IsNullOrEmpty(CurrentLevel.displayName)
            ? CurrentLevel.displayName
            : Data != null ? Data.displayName : name;

        public void Initialize(BuildingData data, Vector3Int gridPosition, int level = 1)
        {
            Data = data;
            GridPosition = gridPosition;
            SetLevel(level);
            SetSelected(false);
        }

        private void Awake()
        {
            Health = GetComponent<HealthComponent>();
            if (Health != null) Health.OnChanged += HandleHealthChanged;
        }

        private bool started;

        private void OnEnable()
        {
            all.Add(this);
            if (started) UpdateDamageState();
        }

        // Start chứ không phải OnEnable: lúc OnEnable đầu tiên HealthComponent có thể chưa Awake (máu = 0 → tưởng là sập).
        private void Start()
        {
            started = true;
            UpdateDamageState();
        }

        private void OnDisable()
        {
            all.Remove(this);
            InteractableRegistry.Unregister(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => all.Clear();

        /// <summary>Lý do không nâng cấp được (null = nâng cấp được).</summary>
        public string UpgradeBlocker()
        {
            if (IsMaxLevel) return "Đã đạt cấp tối đa";
            BuildingLevel next = NextLevel;
            if (next.requiredTech != null && TechManager.Instance != null && !TechManager.Instance.IsUnlocked(next.requiredTech))
                return $"Cần nghiên cứu {next.requiredTech.displayName}";
            if (!ResourceManager.Instance.CanAfford(next.upgradeCost)) return "Chưa đủ tài nguyên";
            return null;
        }

        public bool TryUpgrade()
        {
            if (UpgradeBlocker() != null) return false;
            if (!ResourceManager.Instance.SpendAll(NextLevel.upgradeCost)) return false;

            SetLevel(Level + 1);
            EventBus.RaiseNotification($"Đã nâng cấp thành {LevelName} (cấp {Level}/{Data.MaxLevel})");
            EventBus.RaiseBuildingUpgraded(this);
            return true;
        }

        public void SetLevel(int level)
        {
            Level = Data != null ? Mathf.Clamp(level, 1, Data.MaxLevel) : 1;
            for (int i = 0; i < levelModels.Count; i++)
                if (levelModels[i] != null) levelModels[i].SetActive(i == Level - 1);
        }

        public void SetSelected(bool selected)
        {
            if (selectionRing != null) selectionRing.SetActive(selected);
        }

        // ─── Hư hại & sửa chữa (Milestone 6 — thiên tai) ───────────────────────
        /// <summary>Mỗi lượt sửa hồi chừng này phần máu tối đa, tốn 1 vật liệu xây (gỗ).</summary>
        public const float RepairFractionPerWork = 0.25f;

        public HealthComponent Health { get; private set; }
        public bool IsDamaged => Health != null && Health.Current < Health.Max - 0.01f;
        /// <summary>Sập: hết máu — mất chức năng (kho không chứa, lều không ở, cối/giếng/guồng ngừng) tới khi sửa.</summary>
        public bool IsCollapsed => Health != null && Health.IsDead;
        public float HealthFraction => Health != null ? Health.Current / Health.Max : 1f;
        /// <summary>Máu đã mất (lưu game: 0 = nguyên vẹn, save cũ cũng đọc ra 0).</summary>
        public float MissingHealth => Health != null ? Health.Max - Health.Current : 0f;

        /// <summary>Vật liệu sửa: loại tài nguyên đầu tiên trong chi phí xây (gỗ).</summary>
        public ResourceAmount RepairCost => new ResourceAmount
        {
            type = Data != null && Data.costs.Count > 0 ? Data.costs[0].type : null,
            amount = 1
        };

        private bool wasCollapsed;

        /// <summary>Thiên tai gây hư hại; sập thì báo cho người chơi.</summary>
        public void Damage(float amount, string cause)
        {
            if (Health == null || IsCollapsed) return;
            Health.TakeDamage(amount);
            if (IsCollapsed) EventBus.RaiseNotification($"{LevelName} bị sập vì {cause}! Cần dân làng sửa lại");
        }

        /// <summary>Lý do chưa sửa được (null = sửa được).</summary>
        public string RepairBlocker()
        {
            if (!IsDamaged) return "Công trình còn nguyên vẹn";
            ResourceAmount cost = RepairCost;
            if (cost.type != null && ResourceManager.Instance != null && ResourceManager.Instance.GetAmount(cost.type) < cost.amount)
                return $"Thiếu {cost.type.displayName} để sửa {LevelName}";
            return null;
        }

        /// <summary>Sửa một lượt. Trả về false nếu không sửa được (đã lành hoặc thiếu vật liệu).</summary>
        public bool DoRepairWork()
        {
            if (RepairBlocker() != null) return false;
            ResourceAmount cost = RepairCost;
            if (cost.type != null && !ResourceManager.Instance.TrySpend(cost.type, cost.amount)) return false;
            Health.SetCurrent(Health.Current + Health.Max * RepairFractionPerWork);
            VfxManager.Play(VfxKind.Dust, transform.position + Vector3.up * 0.5f);
            if (!IsDamaged) EventBus.RaiseNotification($"Đã sửa xong {LevelName}");
            return true;
        }

        /// <summary>Đặt máu khi tải game (máu đã mất).</summary>
        public void SetMissingHealth(float missing)
        {
            if (Health != null) Health.SetCurrent(Health.Max - Mathf.Max(0f, missing));
        }

        private void HandleHealthChanged(HealthComponent _) => UpdateDamageState();

        private void UpdateDamageState()
        {
            // Công trình hư hại thì bấm E / chuột phải vào để sửa.
            if (IsDamaged && isActiveAndEnabled) InteractableRegistry.Register(this);
            else InteractableRegistry.Unregister(this);

            bool collapsed = IsCollapsed;
            if (collapsed == wasCollapsed) return;
            wasCollapsed = collapsed;

            // Sập: model đổ nghiêng, lún xuống; các chức năng gắn trên công trình ngừng.
            foreach (var model in levelModels)
            {
                if (model == null) continue;
                model.transform.localRotation = collapsed ? Quaternion.Euler(24f, 0f, 14f) : Quaternion.identity;
                model.transform.localScale = collapsed ? new Vector3(1.1f, 0.4f, 1.1f) : Vector3.one;
            }
            if (rubble != null) rubble.SetActive(collapsed);
            if (collapsed) VfxManager.Play(VfxKind.BigDust, transform.position);
            foreach (var behaviour in GetComponents<Behaviour>())
                if (behaviour is RiceMortar || behaviour is WaterWheel || behaviour is WaterSource || behaviour is Campfire ||
                    behaviour is UnityEngine.AI.NavMeshObstacle) // sập thì đi qua được (hàng rào bị phá), đuốc đổ thì tắt
                    behaviour.enabled = !collapsed;
            if (!collapsed) EventBus.RaiseNotification($"{LevelName} hoạt động trở lại");
        }
    }
}
