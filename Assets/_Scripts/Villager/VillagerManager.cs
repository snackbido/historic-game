using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    [System.Serializable]
    public struct VillagerSaveEntry
    {
        public string villagerId;
        public float posX;
        public float posY;
        public float hunger;
        public float sleep;
        public float warmth;
    }

    public class VillagerManager : MonoBehaviour
    {
        public static VillagerManager Instance { get; private set; }

        [SerializeField] private List<VillagerData> knownVillagerTypes = new List<VillagerData>();

        private readonly Dictionary<string, VillagerData> villagersById = new Dictionary<string, VillagerData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (var villager in knownVillagerTypes)
                if (villager != null) villagersById[villager.id] = villager;
        }

        public List<VillagerSaveEntry> GetSaveData()
        {
            var data = new List<VillagerSaveEntry>();
            foreach (var villager in Object.FindObjectsByType<VillagerController>(FindObjectsInactive.Exclude))
            {
                if (villager.Data == null) continue;

                data.Add(new VillagerSaveEntry
                {
                    villagerId = villager.Data.id,
                    posX = villager.transform.position.x,
                    posY = villager.transform.position.y,
                    hunger = villager.Hunger,
                    sleep = villager.Sleep,
                    warmth = villager.Warmth
                });
            }
            return data;
        }

        public void LoadFromSaveData(List<VillagerSaveEntry> data)
        {
            foreach (var villager in Object.FindObjectsByType<VillagerController>(FindObjectsInactive.Exclude))
                if (villager != null) Destroy(villager.gameObject);

            if (data == null) return;

            foreach (var entry in data)
            {
                if (!villagersById.TryGetValue(entry.villagerId, out var villagerData) || villagerData.prefab == null) continue;

                var instance = Instantiate(villagerData.prefab, new Vector3(entry.posX, entry.posY, 0f), Quaternion.identity);
                instance.GetComponent<VillagerController>()?.LoadState(villagerData, entry.hunger, entry.sleep, entry.warmth);
            }
        }
    }
}
