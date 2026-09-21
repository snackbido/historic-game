using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    [System.Serializable]
    public struct ResourceAmount
    {
        public ResourceType type;
        public int amount;
    }

    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        private readonly Dictionary<ResourceType, int> amounts = new Dictionary<ResourceType, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public int GetAmount(ResourceType type)
        {
            return amounts.TryGetValue(type, out int amount) ? amount : 0;
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            amounts[type] = GetAmount(type) + amount;
            EventBus.RaiseResourceChanged(type, amounts[type]);
        }

        public bool TrySpend(ResourceType type, int amount)
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

        public List<ResourceAmount> GetSaveData()
        {
            var data = new List<ResourceAmount>();
            foreach (var pair in amounts)
                data.Add(new ResourceAmount { type = pair.Key, amount = pair.Value });
            return data;
        }

        public void LoadFromSaveData(List<ResourceAmount> data)
        {
            amounts.Clear();
            if (data == null) return;

            foreach (var entry in data)
            {
                amounts[entry.type] = entry.amount;
                EventBus.RaiseResourceChanged(entry.type, entry.amount);
            }
        }
    }
}
