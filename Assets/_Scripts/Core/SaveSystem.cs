using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PrehistoricTribe
{
    [System.Serializable]
    public class SaveData
    {
        public float playerX;
        public float playerY;
        public List<ResourceSaveEntry> resources = new List<ResourceSaveEntry>();
        public List<PlacedBuildingData> buildings = new List<PlacedBuildingData>();
        public List<FarmPlotSaveEntry> farmPlots = new List<FarmPlotSaveEntry>();
        public List<AnimalSaveEntry> animals = new List<AnimalSaveEntry>();
        public List<VillagerSaveEntry> villagers = new List<VillagerSaveEntry>();
    }

    public static class SaveSystem
    {
        private const string SaveFileName = "savegame.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }

        public static SaveData Load()
        {
            if (!File.Exists(SavePath)) return null;
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<SaveData>(json);
        }

        public static bool HasSaveFile() => File.Exists(SavePath);
    }
}
