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

        public BuildingData Data { get; private set; }
        public Vector3Int GridPosition { get; private set; }
        public int Level { get; private set; } = 1;
        public bool IsMaxLevel => Data == null || Level >= Data.MaxLevel;
        public BuildingLevel CurrentLevel => Data?.GetLevel(Level);
        public BuildingLevel NextLevel => IsMaxLevel ? null : Data.GetLevel(Level + 1);
        public int Housing => CurrentLevel?.housing ?? 0;
        public int StorageCapacity => CurrentLevel?.storageCapacity ?? 0;
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

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

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
    }
}
