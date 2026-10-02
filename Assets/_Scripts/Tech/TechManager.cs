using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    public class TechManager : MonoBehaviour
    {
        public static TechManager Instance { get; private set; }

        [SerializeField] private ResourceTypeData knowledgeResource;
        [SerializeField] private float knowledgeGenerationInterval = 5f;

        [Tooltip("Mọi TechNode trong game — dùng để tra theo id khi tải game")]
        [SerializeField] private List<TechNode> allTechs = new List<TechNode>();

        [Tooltip("Công trình / cây trồng — để báo tên những gì vừa được mở khóa")]
        [SerializeField] private List<BuildingData> knownBuildings = new List<BuildingData>();
        [SerializeField] private List<CropData> knownCrops = new List<CropData>();

        private float knowledgeTimer;
        private readonly HashSet<string> unlockedTechIds = new HashSet<string>();
        private readonly HashSet<string> unlockedBuildingIds = new HashSet<string>();
        private readonly HashSet<string> unlockedCropIds = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (knowledgeResource == null) return;

            knowledgeTimer += GamePace.Scaled(Time.deltaTime);
            if (knowledgeTimer < knowledgeGenerationInterval) return;

            knowledgeTimer = 0f;
            ResourceManager.Instance.AddResource(knowledgeResource, 1);
        }

        public bool IsUnlocked(TechNode tech) => tech != null && unlockedTechIds.Contains(tech.id);

        public bool CanUnlock(TechNode tech)
        {
            if (tech == null || IsUnlocked(tech)) return false;

            foreach (var prerequisite in tech.prerequisites)
                if (!IsUnlocked(prerequisite)) return false;

            return ResourceManager.Instance.CanAfford(tech.cost);
        }

        public bool TryUnlock(TechNode tech)
        {
            if (!CanUnlock(tech))
            {
                // Báo người chơi cần làm gì (thiếu tri thức / chưa nghiên cứu công nghệ trước).
                string reason = LockReason(tech);
                if (reason != null) EventBus.RaiseNotification($"Chưa nghiên cứu được \"{tech.displayName}\": {reason}");
                return false;
            }
            if (!ResourceManager.Instance.SpendAll(tech.cost)) return false;

            ApplyUnlock(tech);
            foreach (var grant in tech.grantOnUnlock)
                if (grant.type != null) ResourceManager.Instance.AddResource(grant.type, grant.amount);
            string unlocks = UnlocksText(tech);
            EventBus.RaiseNotification(unlocks != null
                ? $"Đã nghiên cứu \"{tech.displayName}\" — mở khóa: {unlocks}"
                : $"Đã nghiên cứu \"{tech.displayName}\"");
            EventBus.RaiseTechUnlocked(tech);
            return true;
        }

        private void ApplyUnlock(TechNode tech)
        {
            unlockedTechIds.Add(tech.id);
            foreach (var id in tech.unlockedBuildingIds) unlockedBuildingIds.Add(id);
            foreach (var id in tech.unlockedCropIds) unlockedCropIds.Add(id);
        }

        public List<string> GetSaveData() => new List<string>(unlockedTechIds);

        /// <summary>Không bắn OnTechUnlocked — GameManager bắn OnGameLoaded sau khi tải xong.</summary>
        public void LoadFromSaveData(List<string> techIds)
        {
            unlockedTechIds.Clear();
            unlockedBuildingIds.Clear();
            unlockedCropIds.Clear();
            knowledgeTimer = 0f;

            if (techIds == null) return;
            foreach (var id in techIds)
            {
                TechNode tech = allTechs.Find(t => t != null && t.id == id);
                if (tech != null) ApplyUnlock(tech);
            }
        }

        public bool IsBuildingUnlocked(BuildingData data) =>
            data != null && (data.unlockedByDefault || unlockedBuildingIds.Contains(data.id));

        public bool IsCropUnlocked(CropData data) =>
            data != null && (data.unlockedByDefault || unlockedCropIds.Contains(data.id));

        // ─── Gợi ý mở khóa (M7/P4) ──────────────────────────────────────────
        public TechNode FindUnlocker(BuildingData data) =>
            data == null ? null : allTechs.Find(t => t != null && t.unlockedBuildingIds.Contains(data.id));

        public TechNode FindUnlocker(CropData data) =>
            data == null ? null : allTechs.Find(t => t != null && t.unlockedCropIds.Contains(data.id));

        /// <summary>Lý do chưa nghiên cứu được (null = nghiên cứu được ngay, hoặc đã xong).</summary>
        public string LockReason(TechNode tech)
        {
            if (tech == null || IsUnlocked(tech)) return null;
            var missing = new List<string>();
            foreach (var prerequisite in tech.prerequisites)
                if (prerequisite != null && !IsUnlocked(prerequisite)) missing.Add($"\"{prerequisite.displayName}\"");
            if (missing.Count > 0) return $"cần nghiên cứu trước {string.Join(", ", missing)}";
            if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(tech.cost))
                return $"cần {CostText(tech.cost, withStock: true)}";
            return null;
        }

        /// <summary>Tên những gì công nghệ này mở khóa ("Mương, Đê"), null nếu không mở gì.</summary>
        public string UnlocksText(TechNode tech)
        {
            if (tech == null) return null;
            var names = new List<string>();
            foreach (var building in knownBuildings)
                if (building != null && tech.unlockedBuildingIds.Contains(building.id)) names.Add(building.displayName);
            foreach (var crop in knownCrops)
                if (crop != null && tech.unlockedCropIds.Contains(crop.id)) names.Add(crop.displayName);
            return names.Count > 0 ? string.Join(", ", names) : null;
        }

        /// <summary>Câu báo cho người chơi biết cần làm gì để mở công trình này.</summary>
        public string UnlockHint(BuildingData data) => UnlockHint(data != null ? data.displayName : "?", FindUnlocker(data));

        public string UnlockHint(CropData data) => UnlockHint(data != null ? data.displayName : "?", FindUnlocker(data));

        private string UnlockHint(string name, TechNode tech)
        {
            if (tech == null) return $"{name} chưa mở được";
            string hint = $"{name} bị khóa — nghiên cứu \"{tech.displayName}\" ({CostText(tech.cost)}) ở bảng Công nghệ";
            string reason = LockReason(tech);
            return reason != null ? $"{hint}; {reason}" : hint;
        }

        /// <summary>"15 Tri thức" — kèm số đang có nếu <paramref name="withStock"/> ("15 Tri thức (đang có 3)").</summary>
        public static string CostText(List<ResourceAmount> cost, bool withStock = false)
        {
            var parts = new List<string>();
            foreach (var item in cost)
            {
                if (item.type == null) continue;
                string part = $"{item.amount} {item.type.displayName}";
                if (withStock && ResourceManager.Instance != null)
                    part += $" (đang có {ResourceManager.Instance.GetAmount(item.type)})";
                parts.Add(part);
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "miễn phí";
        }
    }
}
