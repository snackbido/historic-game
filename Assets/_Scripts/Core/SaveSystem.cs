using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PrehistoricTribe
{
    [System.Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 3;

        // Không gán giá trị mặc định: save cũ (chưa có field này) đọc ra 0 → biết là bản cũ,
        // tránh hiểu nhầm danh sách vật nuôi rỗng là "không còn con nào" rồi xóa hết.
        public int saveVersion;

        // Vị trí trên mặt đất (x, z). Save cũ của bản 2D dùng playerY nên sẽ nạp về z = 0.
        public float playerX;
        public float playerZ;
        public List<ResourceSaveEntry> resources = new List<ResourceSaveEntry>();
        public List<PlacedBuildingData> buildings = new List<PlacedBuildingData>();

        // Từ saveVersion 2
        public List<string> unlockedTechIds = new List<string>();
        public List<FarmPlotSaveData> farmPlots = new List<FarmPlotSaveData>();
        public List<AnimalSaveData> animals = new List<AnimalSaveData>();

        // Từ saveVersion 3
        public List<NpcSaveData> npcs = new List<NpcSaveData>();
    }

    public static class SaveSystem
    {
        private const string DefaultSaveFileName = "savegame.json";

        /// <summary>Test đặt tên file riêng để không ghi đè save thật của người chơi.</summary>
        public static string FileNameOverride { get; set; }

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, FileNameOverride ?? DefaultSaveFileName);

        public static void Delete()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }

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
