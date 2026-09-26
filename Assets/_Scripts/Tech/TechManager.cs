using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    public class TechManager : MonoBehaviour
    {
        public static TechManager Instance { get; private set; }

        [SerializeField] private ResourceTypeData knowledgeResource;
        [SerializeField] private float knowledgeGenerationInterval = 5f;

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

            knowledgeTimer += Time.deltaTime;
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
            if (!CanUnlock(tech)) return false;
            if (!ResourceManager.Instance.SpendAll(tech.cost)) return false;

            unlockedTechIds.Add(tech.id);
            foreach (var id in tech.unlockedBuildingIds) unlockedBuildingIds.Add(id);
            foreach (var id in tech.unlockedCropIds) unlockedCropIds.Add(id);

            EventBus.RaiseTechUnlocked(tech);
            return true;
        }

        public bool IsBuildingUnlocked(BuildingData data) =>
            data != null && (data.unlockedByDefault || unlockedBuildingIds.Contains(data.id));

        public bool IsCropUnlocked(CropData data) =>
            data != null && (data.unlockedByDefault || unlockedCropIds.Contains(data.id));
    }
}
