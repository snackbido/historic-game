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

    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [SerializeField] private List<ResourceTypeData> knownResourceTypes = new List<ResourceTypeData>();

        private readonly Dictionary<ResourceTypeData, int> amounts = new Dictionary<ResourceTypeData, int>();
        private readonly Dictionary<string, ResourceTypeData> typesById = new Dictionary<string, ResourceTypeData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (var type in knownResourceTypes)
                if (type != null) typesById[type.id] = type;
        }

        public int GetAmount(ResourceTypeData type)
        {
            return amounts.TryGetValue(type, out int amount) ? amount : 0;
        }

        public void AddResource(ResourceTypeData type, int amount)
        {
            if (amount <= 0) return;
            amounts[type] = GetAmount(type) + amount;
            EventBus.RaiseResourceChanged(type, amounts[type]);
        }

        public bool TrySpend(ResourceTypeData type, int amount)
        {
            if (GetAmount(type) < amount) return false;
            amounts[type] = GetAmount(type) - amount;
            EventBus.RaiseResourceChanged(type, amounts[type]);
            return true;
        }

        public bool CanAfford(List<ResourceAmount> costs)
        {
            foreach (var cost in costs)
                if (GetAmount(cost.type) < cost.amount) return false;
            return true;
        }

        public bool SpendAll(List<ResourceAmount> costs)
        {
            if (!CanAfford(costs)) return false;
            foreach (var cost in costs)
                TrySpend(cost.type, cost.amount);
            return true;
        }

        public List<ResourceSaveEntry> GetSaveData()
        {
            var data = new List<ResourceSaveEntry>();
            foreach (var pair in amounts)
                data.Add(new ResourceSaveEntry { resourceId = pair.Key.id, amount = pair.Value });
            return data;
        }

        public void LoadFromSaveData(List<ResourceSaveEntry> data)
        {
            amounts.Clear();
            if (data == null) return;

            foreach (var entry in data)
            {
                if (!typesById.TryGetValue(entry.resourceId, out var type)) continue;
                amounts[type] = entry.amount;
                EventBus.RaiseResourceChanged(type, entry.amount);
            }
        }
    }
}
