using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PrehistoricTribe
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private PlayerController player;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private BuildingPlacer buildingPlacer;
        [Tooltip("Tạo lại cây đã bị chặt sau lúc lưu khi tải game")]
        [SerializeField] private GameObject treePrefab;

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
                saveVersion = SaveData.CurrentVersion,
                playerX = player.GetPosition().x,
                playerZ = player.GetPosition().z,
                resources = resourceManager.GetSaveData(),
                buildings = buildingPlacer.GetSaveData(),
                unlockedTechIds = TechManager.Instance != null ? TechManager.Instance.GetSaveData() : new List<string>(),
                farmPlots = InteractableRegistry.All<FarmPlot>().ConvertAll(p => p.GetSaveData()),
                riceMortars = InteractableRegistry.All<RiceMortar>().ConvertAll(m => m.GetSaveData()),
                canals = Canal.All.Select(c => c.GetSaveData()).ToList(),
                resourceNodes = InteractableRegistry.All<ResourceNode>().ConvertAll(n => n.GetSaveData()),
                disaster = DisasterManager.Instance != null ? DisasterManager.Instance.GetSaveData() : new DisasterSaveData(),
                animals = InteractableRegistry.All<AnimalController>().ConvertAll(a => a.GetSaveData()),
                npcs = NpcManager.Instance != null ? NpcManager.Instance.GetSaveData() : new List<NpcSaveData>(),
                predators = PredatorManager.Instance != null ? PredatorManager.Instance.GetSaveData() : new List<PredatorSaveData>(),
                timeOfDay = DayNightCycle.Instance != null ? DayNightCycle.Instance.TimeOfDay : 0f,
                day = DayNightCycle.Instance != null ? DayNightCycle.Instance.Day : 1
            };
            SaveSystem.Save(data);
            EventBus.RaiseNotification("Đã lưu game");
        }

        public void LoadGame()
        {
            SaveData data = SaveSystem.Load();
            if (data == null)
            {
                EventBus.RaiseNotification("Chưa có bản lưu nào");
                return;
            }

            // Tech trước: công trình/cây trồng khôi phục phía sau cần biết đã mở khóa chưa.
            bool hasWorldState = data.saveVersion >= 2;
            if (hasWorldState && TechManager.Instance != null)
                TechManager.Instance.LoadFromSaveData(data.unlockedTechIds);

            player.SetPosition(new Vector3(data.playerX, 0f, data.playerZ));
            resourceManager.LoadFromSaveData(data.resources);
            buildingPlacer.LoadFromSaveData(data.buildings);

            if (hasWorldState)
            {
                LoadFarmPlots(data.farmPlots);
                LoadAnimals(data.animals);
                foreach (var mortar in InteractableRegistry.All<RiceMortar>())
                    mortar.LoadFromSaveData(data.riceMortars?.Find(s => s.objectName == mortar.name));
                foreach (var canal in Canal.All)
                    canal.SetDigProgress(data.canals?.Find(s => s.objectName == canal.name)?.digWork ?? 0);
            }

            // Save cũ hơn (chưa có dân làng) giữ nguyên NPC đang có trong scene.
            if (data.saveVersion >= 3 && NpcManager.Instance != null)
                NpcManager.Instance.LoadFromSaveData(data.npcs);

            // Save cũ hơn (chưa có thú dữ) giữ nguyên thú dữ đang có trong scene.
            if (data.saveVersion >= 4 && PredatorManager.Instance != null)
                PredatorManager.Instance.LoadFromSaveData(data.predators);

            // Save cũ hơn (chưa lưu cây) giữ nguyên cây đang có trong scene.
            if (data.saveVersion >= 6) LoadResourceNodes(data.resourceNodes);

            if (data.saveVersion >= 5 && DayNightCycle.Instance != null)
                DayNightCycle.Instance.SetTime(data.timeOfDay, data.day);

            if (DisasterManager.Instance != null)
                DisasterManager.Instance.LoadFromSaveData(data.disaster);

            EventBus.RaiseGameLoaded();
            EventBus.RaiseNotification("Đã tải game");
        }

        /// <summary>
        /// Cây / ao là object có sẵn trong scene → khớp theo tên: không có trong bản lưu = đã bị chặt hết → xoá;
        /// có thì đặt lại số còn lại. Cây trong bản lưu mà ván này đã chặt mất (chặt sau lúc lưu) → tạo lại từ prefab.
        /// </summary>
        private void LoadResourceNodes(List<ResourceNodeSaveData> saved)
        {
            var remaining = new List<ResourceNodeSaveData>(saved ?? new List<ResourceNodeSaveData>());
            Transform parent = null;
            foreach (var node in new List<ResourceNode>(InteractableRegistry.All<ResourceNode>()))
            {
                if (parent == null) parent = node.transform.parent;
                ResourceNodeSaveData entry = remaining.Find(s => s.objectName == node.name);
                if (entry == null)
                {
                    node.gameObject.SetActive(false); // gỡ khỏi danh sách tương tác ngay, không chờ cuối khung hình
                    Destroy(node.gameObject);
                    continue;
                }
                remaining.Remove(entry);
                node.LoadFromSaveData(entry);
            }

            foreach (var entry in remaining)
            {
                if (treePrefab == null || entry.resourceId != "wood") continue;
                var tree = Instantiate(treePrefab, new Vector3(entry.x, entry.y, entry.z), Quaternion.Euler(0f, entry.rotationY, 0f), parent);
                tree.name = entry.objectName;
                var node = tree.GetComponent<ResourceNode>();
                if (node != null) node.LoadFromSaveData(entry);
            }
        }

        private static void LoadFarmPlots(List<FarmPlotSaveData> saved)
        {
            // Ô đất là object cố định trong scene → khớp theo tên.
            foreach (var plot in InteractableRegistry.All<FarmPlot>())
            {
                FarmPlotSaveData entry = saved?.Find(s => s.plotName == plot.name);
                CropData crop = entry != null && FarmManager.Instance != null ? FarmManager.Instance.FindCrop(entry.cropId) : null;
                plot.LoadFromSaveData(entry, crop);
            }
        }

        private static void LoadAnimals(List<AnimalSaveData> saved)
        {
            // Vật nuôi sinh sản ra thêm lúc chơi → xóa hết rồi tạo lại đúng danh sách đã lưu.
            foreach (var animal in InteractableRegistry.All<AnimalController>())
            {
                animal.gameObject.SetActive(false); // gỡ khỏi registry ngay, Destroy chỉ chạy cuối frame
                Destroy(animal.gameObject);
            }

            if (saved == null || TamingSystem.Instance == null) return;
            foreach (var entry in saved)
            {
                AnimalData animalData = TamingSystem.Instance.FindAnimal(entry.animalId);
                if (animalData == null || animalData.prefab == null) continue;

                var position = new Vector3(entry.x, 0f, entry.z);
                var rotation = Quaternion.Euler(0f, entry.rotationY, 0f);
                GameObject go = Instantiate(animalData.prefab, position, rotation);
                if (!string.IsNullOrEmpty(entry.objectName)) go.name = entry.objectName;
                go.GetComponent<AnimalController>()?.LoadFromSaveData(entry, animalData);
            }
        }
    }
}
