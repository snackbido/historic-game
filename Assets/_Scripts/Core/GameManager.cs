using UnityEngine;

namespace PrehistoricTribe
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private PlayerController player;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private FarmManager farmManager;
        [SerializeField] private TamingSystem tamingSystem;
        [SerializeField] private VillagerManager villagerManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SaveGame()
        {
            var data = new SaveData
            {
                playerX = player.GetPosition().x,
                playerY = player.GetPosition().y,
                resources = resourceManager.GetSaveData(),
                buildings = buildingPlacer.GetSaveData(),
                farmPlots = farmManager.GetSaveData(),
                animals = tamingSystem.GetSaveData(),
                villagers = villagerManager.GetSaveData()
            };
            SaveSystem.Save(data);
        }

        public void LoadGame()
        {
            SaveData data = SaveSystem.Load();
            if (data == null) return;

            player.SetPosition(new Vector2(data.playerX, data.playerY));
            resourceManager.LoadFromSaveData(data.resources);
            buildingPlacer.LoadFromSaveData(data.buildings);
            farmManager.LoadFromSaveData(data.farmPlots);
            tamingSystem.LoadFromSaveData(data.animals);
            villagerManager.LoadFromSaveData(data.villagers);
        }
    }
}
