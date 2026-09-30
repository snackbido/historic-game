using UnityEngine;

namespace PrehistoricTribe
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private PlayerController player;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private BuildingPlacer buildingPlacer;

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
                playerZ = player.GetPosition().z,
                resources = resourceManager.GetSaveData(),
                buildings = buildingPlacer.GetSaveData()
            };
            SaveSystem.Save(data);
        }

        public void LoadGame()
        {
            SaveData data = SaveSystem.Load();
            if (data == null) return;

            player.SetPosition(new Vector3(data.playerX, 0f, data.playerZ));
            resourceManager.LoadFromSaveData(data.resources);
            buildingPlacer.LoadFromSaveData(data.buildings);
        }
    }
}
