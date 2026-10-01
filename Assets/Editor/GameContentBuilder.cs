using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using static PrehistoricTribe.EditorTools.EditorBuildUtils;

namespace PrehistoricTribe.EditorTools
{
    /// <summary>Bảng màu low-poly dùng chung cho mọi model placeholder (material lưu ở Assets/Materials/Generated).</summary>
    internal static class Palette
    {
        public static Material Wood => Mat("Wood", Hex(0x6b4423));
        public static Material DarkWood => Mat("DarkWood", Hex(0x2e1d10));
        public static Material Plank => Mat("Plank", Hex(0x8b5e34));
        public static Material Hide => Mat("Hide", Hex(0xb08556));
        public static Material Thatch => Mat("Thatch", Hex(0xc9a35a));
        public static Material LeavesDark => Mat("LeavesDark", Hex(0x2f6b34));
        public static Material Leaves => Mat("Leaves", Hex(0x377a3a));
        public static Material LeavesLight => Mat("LeavesLight", Hex(0x418a42));
        public static Material Soil => Mat("Soil", Hex(0x5a3d22));
        public static Material SoilDark => Mat("SoilDark", Hex(0x47301a));
        public static Material Grass => Mat("Grass", Hex(0x5f8a3a));
        public static Material Skin => Mat("Skin", Hex(0xc98d5e));
        public static Material Fur => Mat("Fur", Hex(0x8a5a2b));
        public static Material Hair => Mat("Hair", Hex(0x3b2414));
        public static Material Stone => Mat("Stone", Hex(0x9a9a92));
        public static Material Boar => Mat("Boar", Hex(0x8a5a33));
        public static Material BoarDark => Mat("BoarDark", Hex(0x3a2616));
        public static Material Snout => Mat("Snout", Hex(0x9c6b5a));
        public static Material Ivory => Mat("Ivory", Hex(0xeee6d0));
        public static Material Rope => Mat("Rope", Hex(0xd9b36c));
        public static Material ProductGlow => Mat("ProductGlow", Hex(0xffd23f), emission: 0.6f);
        public static Material Preview => Mat("PlacementPreview", Hex(0x9ccf6a));

        public static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
    }

    /// <summary>
    /// Tạo prefab/asset placeholder 3D low-poly (công trình, cây, heo rừng, các giai đoạn cây trồng).
    /// Chạy qua menu Tools/Prehistoric/Build Missing Content trước khi chạy GameplaySceneBuilder.
    /// Muốn dùng model thật (Kenney/Quaternius…) thì thay trực tiếp trong prefab và đừng chạy lại menu này.
    /// </summary>
    public static class GameContentBuilder
    {
        private const string HutDataPath = "Assets/_Data/BuildingData_Hut.asset";
        private const string StorageDataPath = "Assets/_Data/BuildingData_Storage.asset";
        private const string WoodAssetPath = "Assets/_Data/ResourceType_Wood.asset";
        private const string FoodAssetPath = "Assets/_Data/ResourceType_Food.asset";
        private const string KnowledgeAssetPath = "Assets/_Data/ResourceType_Knowledge.asset";
        private const string BoarDataPath = "Assets/_Data/AnimalData_WildBoar.asset";
        private const string BerryDataPath = "Assets/_Data/CropData_Berry.asset";
        public const string RiceDataPath = "Assets/_Data/CropData_Rice.asset";
        public const string VegetableDataPath = "Assets/_Data/CropData_Vegetable.asset";
        public const string GoatDataPath = "Assets/_Data/AnimalData_Goat.asset";
        public const string GoatPrefabPath = "Assets/Prefabs/Animals/Goat.prefab";
        public const string PondPrefabPath = "Assets/Prefabs/Resources/FishingPond.prefab";
        public const string TechFarmingPath = "Assets/_Data/TechNode_Farming.asset";
        public const string TechRicePath = "Assets/_Data/TechNode_Rice.asset";

        public const string DryFieldDataPath = "Assets/_Data/BuildingData_DryField.asset";
        public const string PaddyFieldDataPath = "Assets/_Data/BuildingData_PaddyField.asset";
        private const string DryFieldPrefabPath = "Assets/Prefabs/Buildings/DryField.prefab";
        private const string PaddyFieldPrefabPath = "Assets/Prefabs/Buildings/PaddyField.prefab";
        public const string WellDataPath = "Assets/_Data/BuildingData_Well.asset";
        public const string SeedbedDataPath = "Assets/_Data/BuildingData_Seedbed.asset";
        private const string SeedbedPrefabPath = "Assets/Prefabs/Buildings/Seedbed.prefab";
        public const string SeedlingDataPath = "Assets/_Data/CropData_RiceSeedling.asset";
        private const string WellPrefabPath = "Assets/Prefabs/Buildings/Well.prefab";
        public const string MortarDataPath = "Assets/_Data/BuildingData_RiceMortar.asset";
        public const string CanalDataPath = "Assets/_Data/BuildingData_Canal.asset";
        private const string CanalPrefabPath = "Assets/Prefabs/Buildings/Canal.prefab";
        public const string TechIrrigationPath = "Assets/_Data/TechNode_Irrigation.asset";
        public const string WaterWheelDataPath = "Assets/_Data/BuildingData_WaterWheel.asset";
        private const string WaterWheelPrefabPath = "Assets/Prefabs/Buildings/WaterWheel.prefab";
        public const string TechWaterWheelPath = "Assets/_Data/TechNode_WaterWheel.asset";
        public const string LeveeDataPath = "Assets/_Data/BuildingData_Levee.asset";
        private const string LeveePrefabPath = "Assets/Prefabs/Buildings/Levee.prefab";
        private const string MortarPrefabPath = "Assets/Prefabs/Buildings/RiceMortar.prefab";

        private const string HutPrefabPath = "Assets/Prefabs/Buildings/Hut.prefab";
        private const string StoragePrefabPath = "Assets/Prefabs/Buildings/Storage.prefab";
        private const string BoarPrefabPath = "Assets/Prefabs/Animals/WildBoar.prefab";
        public const string TreePrefabPath = "Assets/Prefabs/Resources/Tree.prefab";
        private const string CropPrefabFolder = "Assets/Prefabs/Crops";
        public const string VillagerPrefabPath = "Assets/Prefabs/Npcs/Villager.prefab";
        public const string WolfPrefabPath = "Assets/Prefabs/Enemies/Wolf.prefab";
        public const string WolfDataPath = "Assets/_Data/PredatorData_Wolf.asset";

        // Thứ tự cố định: dân làng, nông dân, thợ săn, trinh sát.
        public static readonly string[] ProfessionIds = { "villager", "farmer", "hunter", "scout" };
        public static string ProfessionAssetPath(string id) => $"Assets/_Data/Profession_{id}.asset";

        [MenuItem("Tools/Prehistoric/Build Missing Content (Buildings + Animal)")]
        public static void Build()
        {
            if (!ConfirmOverwrite("Tạo lại prefab placeholder?",
                    "Prefab lều/kho/cây/heo rừng đã tồn tại. Tạo lại sẽ ghi đè mọi chỉnh sửa hoặc model thật bạn đã thay vào.",
                    HutPrefabPath, StoragePrefabPath, BoarPrefabPath, TreePrefabPath, VillagerPrefabPath, WolfPrefabPath,
                    GoatPrefabPath, PondPrefabPath, DryFieldPrefabPath, PaddyFieldPrefabPath, WellPrefabPath, SeedbedPrefabPath, MortarPrefabPath, CanalPrefabPath, WaterWheelPrefabPath, LeveePrefabPath))
                return;

            Foods foods = EnsureResourceTypes(out var wood, out var knowledge);
            if (foods == null) return;

            AssignBuildingPrefab(HutDataPath, SavePrefab(BuildHut(), HutPrefabPath),
                "Nhà ở của dân làng", HutLevels(wood, knowledge), "Lều");
            AssignBuildingPrefab(StorageDataPath, SavePrefab(BuildStorage(), StoragePrefabPath),
                "Cất giữ lương thực", StorageLevels(wood, knowledge), "Kho");
            SaveSingleLevelBuilding(DryFieldDataPath, "dry_field", "Ruộng cạn", SavePrefab(BuildField(FieldType.Dry), DryFieldPrefabPath),
                "Trồng rau, quả mọng. Mới xây là đất hoang — nông dân khai hoang xong mới gieo được",
                unlockedByDefault: true, waterWithin: 0f, Cost(wood, 2));
            SaveSingleLevelBuilding(PaddyFieldDataPath, "paddy_field", "Ruộng nước", SavePrefab(BuildField(FieldType.Paddy), PaddyFieldPrefabPath),
                "Cấy lúa. Phải xây gần ao — nông dân đắp bờ xong mới cấy được",
                unlockedByDefault: false, waterWithin: 3f, Cost(wood, 4));
            SaveSingleLevelBuilding(WellDataPath, "well", "Giếng", SavePrefab(BuildWell(), WellPrefabPath),
                "Nguồn nước gần ruộng: nông dân gánh nước tưới từ đây. Không đủ nước cho ruộng nước",
                unlockedByDefault: false, waterWithin: 0f, Cost(wood, 8));
            SavePrefab(BuildTree(wood), TreePrefabPath);
            SavePrefab(BuildPond(foods.fish), PondPrefabPath);
            // M5d/F4: phân chuồng — vật nuôi thuần cho ra cùng sản phẩm, dùng bón ruộng.
            var manure = SaveResourceType("Manure", "manure", "Phân bón", ResourceCategory.Material);
            BuildWildBoarContent(foods, manure);
            BuildGoatContent(foods, manure);
            // M5d/F3: thóc giống (gieo mạ) và mạ (cấy lúa) — vật tư nông nghiệp, không ăn được.
            var riceSeed = SaveResourceType("RiceSeed", "rice_seed", "Thóc", ResourceCategory.Material);
            var seedling = SaveResourceType("Seedling", "rice_seedling", "Mạ", ResourceCategory.Material);
            SaveSingleLevelBuilding(SeedbedDataPath, "seedbed", "Ruộng mạ", SavePrefab(BuildField(FieldType.Seedbed), SeedbedPrefabPath),
                "Ươm mạ: gieo thóc giống trên đất ngập nước, mạ lên thì nhổ đem cấy sang ruộng nước",
                unlockedByDefault: false, waterWithin: 0f, Cost(wood, 2));
            // M5d/F5: gặt lúa về là bó lúa → phơi, tuốt (thóc), giã (gạo) ở cối giã.
            var sheaf = SaveResourceType("RiceSheaf", "rice_sheaf", "Lúa bó", ResourceCategory.Material);
            SaveSingleLevelBuilding(MortarDataPath, "rice_mortar", "Cối giã", SavePrefab(BuildRiceMortar(sheaf, riceSeed, foods.rice), MortarPrefabPath),
                "Phơi bó lúa trên giàn cho khô, tuốt lấy thóc, giã thóc thành gạo. Luôn chừa thóc làm giống",
                unlockedByDefault: false, waterWithin: 0f, Cost(wood, 6));
            // M5d/F6: mương dẫn nước từ ao tới ruộng (công nghệ Thủy lợi).
            SaveSingleLevelBuilding(CanalDataPath, "canal", "Mương", SavePrefab(BuildCanal(), CanalPrefabPath),
                "Đào nối liền từ ao: nước tự chảy theo mương (tối đa 8 ô), ruộng sát mương tự được tưới",
                unlockedByDefault: false, waterWithin: 0f, Cost(wood, 1));
            // M5d/F7: guồng nước bên ao bơm nước vào mương — đi xa hơn, tưới nhanh hơn.
            SaveSingleLevelBuilding(WaterWheelDataPath, "water_wheel", "Guồng nước", SavePrefab(BuildWaterWheel(), WaterWheelPrefabPath),
                "Dựng sát mép ao, cạnh đầu mương: guồng quay đổ nước vào mương, nước đi xa tới 20 ô, ruộng được tưới nhanh gấp đôi",
                unlockedByDefault: false, waterWithin: CanalNetwork.FeedRange, Cost(wood, 10));
            var wheelData = AssetDatabase.LoadAssetAtPath<BuildingData>(WaterWheelDataPath);
            wheelData.requiresOpenWater = true;
            EditorUtility.SetDirty(wheelData);
            // M6/D3: đê đất chắn nước lũ tràn từ ao (mở cùng Thủy lợi).
            SaveSingleLevelBuilding(LeveeDataPath, "levee", "Đê", SavePrefab(BuildLevee(), LeveePrefabPath),
                "Ụ đất đắp giữa ao và ruộng/nhà: chặn nước lũ tràn qua. Xếp liền nhau thành bờ đê kín",
                unlockedByDefault: false, waterWithin: 0f, Cost(wood, 2));
            BuildCropModels(foods, riceSeed, seedling, sheaf);
            BuildTechs(knowledge, riceSeed);
            ProfessionData[] professions = BuildProfessions();
            SavePrefab(BuildVillager(professions), VillagerPrefabPath);
            BuildWolfContent(foods.meat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameContentBuilder] Da tao xong content: cong trinh, cay, ao ca, heo rung, de nui, 3 cay trong, 6 loai luong thuc, 2 cong nghe, dan lang, soi");
        }

        // ─── Tài nguyên & lương thực (M5b/E3) ────────────────────────────────
        /// <summary>Các loại lương thực + loại gộp "Thức ăn" (chi phí trả bằng loại nào cũng được).</summary>
        internal class Foods
        {
            public ResourceTypeData pool, meat, rice, berries, milk, fish, vegetables;
            public List<ResourceTypeData> All => new List<ResourceTypeData> { meat, rice, berries, milk, fish, vegetables };
        }

        public static string ResourcePath(string name) => $"Assets/_Data/ResourceType_{name}.asset";

        // Thứ tự = thứ tự hiển thị trên dòng chi tiết lương thực.
        public static readonly string[] FoodAssetNames = { "Meat", "Rice", "Berries", "Milk", "Fish", "Vegetables" };

        private static ResourceTypeData SaveResourceType(string assetName, string id, string displayName,
            ResourceCategory category, bool perishable = false)
        {
            string path = ResourcePath(assetName);
            var type = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(path);
            bool isNew = type == null;
            if (isNew) type = ScriptableObject.CreateInstance<ResourceTypeData>();
            type.id = id;
            type.displayName = displayName;
            type.category = category;
            type.perishable = perishable;
            if (isNew) AssetDatabase.CreateAsset(type, path);
            else EditorUtility.SetDirty(type);
            return type;
        }

        private static Foods EnsureResourceTypes(out ResourceTypeData wood, out ResourceTypeData knowledge)
        {
            wood = SaveResourceType("Wood", "wood", "Gỗ", ResourceCategory.Material);
            knowledge = SaveResourceType("Knowledge", "knowledge", "Tri thức", ResourceCategory.Knowledge);

            var foods = new Foods
            {
                meat = SaveResourceType("Meat", "meat", "Thịt", ResourceCategory.Food, perishable: true),
                rice = SaveResourceType("Rice", "rice", "Gạo", ResourceCategory.Food),
                berries = SaveResourceType("Berries", "berries", "Quả mọng", ResourceCategory.Food),
                milk = SaveResourceType("Milk", "milk", "Sữa", ResourceCategory.Food, perishable: true),
                fish = SaveResourceType("Fish", "fish", "Cá", ResourceCategory.Food, perishable: true),
                vegetables = SaveResourceType("Vegetables", "vegetables", "Rau", ResourceCategory.Food),
            };

            // "food" (asset cũ ResourceType_Food) thành loại gộp: mọi chi phí/test đang dùng nó vẫn chạy.
            foods.pool = SaveResourceType("Food", "food", "Thức ăn", ResourceCategory.Food);
            foods.pool.isFoodPool = true;
            foods.pool.poolDefault = foods.berries;
            EditorUtility.SetDirty(foods.pool);
            return foods;
        }

        private static void AssignBuildingPrefab(string dataAssetPath, GameObject prefab, string function,
            List<BuildingLevel> levels, string displayName)
        {
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(dataAssetPath);
            if (data == null)
            {
                Debug.LogError($"[GameContentBuilder] Khong tim thay {dataAssetPath}");
                return;
            }
            data.prefab = prefab;
            data.displayName = displayName;
            data.functionDescription = function;
            data.levels = levels;
            EditorUtility.SetDirty(data);
        }

        // ─── Cấp công trình (quyết định 2026-09-30: 5 cấp) ───────────────────
        private static BuildingLevel Level(string name, int housing, int storage, params ResourceAmount[] cost) =>
            new BuildingLevel { displayName = name, housing = housing, storageCapacity = storage, upgradeCost = new List<ResourceAmount>(cost) };

        private static ResourceAmount Cost(ResourceTypeData type, int amount) => new ResourceAmount { type = type, amount = amount };

        private static List<BuildingLevel> HutLevels(ResourceTypeData wood, ResourceTypeData knowledge) => new List<BuildingLevel>
        {
            Level("Lều da", 2, 0),
            Level("Lều da lớn", 3, 0, Cost(wood, 15)),
            Level("Nhà lá", 4, 0, Cost(wood, 25), Cost(knowledge, 5)),
            Level("Nhà sàn", 5, 0, Cost(wood, 40), Cost(knowledge, 10)),
            Level("Nhà dài", 6, 0, Cost(wood, 60), Cost(knowledge, 20)),
        };

        private static List<BuildingLevel> StorageLevels(ResourceTypeData wood, ResourceTypeData knowledge) => new List<BuildingLevel>
        {
            Level("Kho chứa", 0, 30),
            Level("Kho lớn", 0, 60, Cost(wood, 25)),
            Level("Lẫm lúa", 0, 100, Cost(wood, 40), Cost(knowledge, 5)),
            Level("Hầm chứa", 0, 150, Cost(wood, 60), Cost(knowledge, 10)),
            Level("Kho lương", 0, 220, Cost(wood, 90), Cost(knowledge, 20)),
        };

        // ─── Buildings ───────────────────────────────────────────────────────
        /// <summary>Máu + vật cản NavMesh (NPC đi vòng qua công trình đặt lúc chơi).</summary>
        private static void AddBuildingComponents(GameObject root, Vector3 center, Vector3 size, float maxHealth)
        {
            var col = root.AddComponent<BoxCollider>();
            col.center = center;
            col.size = size;
            root.AddComponent<BuildingInstance>();
            SetPrivateField(root.AddComponent<HealthComponent>(), "maxHealth", maxHealth);
            BuildHealthBar(root, center.y + size.y * 0.5f + 0.45f); // hiện khi công trình hư hại (M6)
            BuildRubble(root, size);
            AddObstacle(root, NavMeshObstacleShape.Box, center, size);
        }

        /// <summary>Đống đổ nát khi công trình sập (M6): khúc gỗ gãy nằm ngổn ngang + đá vụn, ẩn sẵn.</summary>
        private static void BuildRubble(GameObject root, Vector3 size)
        {
            var rubble = new GameObject("Rubble").transform;
            rubble.SetParent(root.transform, false);
            float rx = size.x * 0.5f, rz = size.z * 0.5f;
            var charred = Mat("CharredWood", Palette.Hex(0x3a2a1c));
            Part(rubble, "Log", PrimitiveType.Cylinder, new Vector3(-rx * 0.6f, 0.04f, rz * 0.4f), new Vector3(0.07f, rx * 0.7f, 0.07f), Palette.Wood, new Vector3(90f, 35f, 0f));
            Part(rubble, "Log", PrimitiveType.Cylinder, new Vector3(rx * 0.5f, 0.05f, -rz * 0.3f), new Vector3(0.08f, rx * 0.8f, 0.08f), charred, new Vector3(90f, -50f, 0f));
            Part(rubble, "Log", PrimitiveType.Cylinder, new Vector3(rx * 0.1f, 0.12f, rz * 0.6f), new Vector3(0.06f, rx * 0.6f, 0.06f), Palette.Wood, new Vector3(70f, 100f, 0f));
            Part(rubble, "Stone", PrimitiveType.Sphere, new Vector3(-rx * 0.4f, 0.04f, -rz * 0.6f), new Vector3(0.14f, 0.08f, 0.12f), Palette.Stone);
            Part(rubble, "Stone", PrimitiveType.Sphere, new Vector3(rx * 0.7f, 0.03f, rz * 0.5f), new Vector3(0.1f, 0.06f, 0.1f), Palette.Stone);
            rubble.gameObject.SetActive(false);
            SetPrivateField(root.GetComponent<BuildingInstance>(), "rubble", rubble.gameObject);
        }

        private static void AddObstacle(GameObject root, NavMeshObstacleShape shape, Vector3 center, Vector3 size)
        {
            var obstacle = root.AddComponent<NavMeshObstacle>();
            obstacle.shape = shape;
            obstacle.center = center;
            if (shape == NavMeshObstacleShape.Box)
            {
                obstacle.size = size;
            }
            else
            {
                obstacle.radius = size.x * 0.5f;
                obstacle.height = size.y;
            }
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
        }

        /// <summary>Gắn model từng cấp (Level1..5) + vòng chọn vào BuildingInstance.</summary>
        private static void AttachLevelModels(GameObject root, params System.Action<Transform>[] builders)
        {
            var models = new List<GameObject>();
            for (int i = 0; i < builders.Length; i++)
            {
                var model = new GameObject($"Level{i + 1}");
                model.transform.SetParent(root.transform, false);
                builders[i](model.transform);
                model.SetActive(i == 0);
                models.Add(model);
            }

            var ring = new GameObject("SelectionRing");
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            ring.transform.localScale = Vector3.one * 0.9f;
            ring.AddComponent<MeshFilter>().sharedMesh = RingMesh(0.62f, 0.72f, 40);
            var ringRenderer = ring.AddComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = Mat("SelectionRing", Palette.Hex(0xb8f07a), emission: 0.8f);
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.SetActive(false);

            var instance = root.GetComponent<BuildingInstance>();
            SetPrivateField(instance, "levelModels", models);
            SetPrivateField(instance, "selectionRing", ring);
        }

        // ─── Ruộng (Milestone 5d) ────────────────────────────────────────────
        private static void SaveSingleLevelBuilding(string path, string id, string displayName, GameObject prefab, string function,
            bool unlockedByDefault, float waterWithin, params ResourceAmount[] cost)
        {
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
            bool isNew = data == null;
            if (isNew) data = ScriptableObject.CreateInstance<BuildingData>();
            data.id = id;
            data.displayName = displayName;
            data.prefab = prefab;
            data.functionDescription = function;
            data.unlockedByDefault = unlockedByDefault;
            data.requiresWaterWithin = waterWithin;
            data.costs = new List<ResourceAmount>(cost);
            data.levels = new List<BuildingLevel> { Level(displayName, 0, 0) };
            if (isNew) AssetDatabase.CreateAsset(data, path);
            else EditorUtility.SetDirty(data);
        }

        /// <summary>
        /// Một ô ruộng 1×1: lúc mới xây là đất hoang (cỏ, đá); khai hoang xong thành đất đã làm
        /// (ruộng cạn: luống đất; ruộng nước: bùn + bờ đắp quanh). Đi xuyên qua được, click để xem thông tin.
        /// </summary>
        private static GameObject BuildField(FieldType type)
        {
            bool paddy = type != FieldType.Dry; // ruộng nước + ruộng mạ: đất ngập, có bờ
            var root = new GameObject(type == FieldType.Paddy ? "PaddyField" : type == FieldType.Seedbed ? "Seedbed" : "DryField");
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = true; // không chặn đường, chỉ để click chọn
            col.center = new Vector3(0f, 0.05f, 0f);
            col.size = new Vector3(0.96f, 0.1f, 0.96f);
            root.AddComponent<BuildingInstance>();
            AttachLevelModels(root, _ => { }); // chỉ có 1 cấp; lấy vòng chọn

            var plot = root.AddComponent<FarmPlot>();
            SetupFieldPlot(plot, type, startsWild: true);
            SetPrivateField(plot, "clearWorkNeeded", type == FieldType.Paddy ? 12 : type == FieldType.Seedbed ? 4 : 8); // đắp bờ lâu hơn
            SetPrivateField(plot, "plowWorkNeeded", type == FieldType.Seedbed ? 2 : paddy ? 4 : 3);
            return root;
        }

        /// <summary>
        /// Gắn hình ảnh + thông số cho một ô ruộng (dùng cho ruộng xây mới và ô vườn có sẵn trong scene):
        /// đất hoang (cỏ, đá) → đất chưa cày (phẳng, chai) → đất đã cày (luống / bùn có rãnh) + mặt nước dâng theo mức nước.
        /// </summary>
        public static void SetupFieldPlot(FarmPlot plot, FieldType type, bool startsWild, float startWater = 0f)
        {
            bool paddy = type != FieldType.Dry;
            var t = plot.transform;

            GameObject wild = null;
            if (startsWild)
            {
                wild = new GameObject("Wild");
                wild.transform.SetParent(t, false);
                var w = wild.transform;
                Part(w, "Ground", PrimitiveType.Cube, new Vector3(0f, 0.015f, 0f), new Vector3(0.96f, 0.03f, 0.96f),
                    paddy ? Mat("Marsh", Palette.Hex(0x55613a)) : Mat("WildGround", Palette.Hex(0x6b7a3c)));
                var tuft = Mat("WildGrass", Palette.Hex(0x7f9a3e));
                foreach (var p in new[] { new Vector3(-0.3f, 0f, 0.25f), new Vector3(0.28f, 0f, 0.3f), new Vector3(0.05f, 0f, -0.05f),
                             new Vector3(-0.2f, 0f, -0.32f), new Vector3(0.32f, 0f, -0.22f) })
                    for (int i = -1; i <= 1; i++)
                        Part(w, "Tuft", PrimitiveType.Cylinder, p + new Vector3(i * 0.03f, 0.08f, 0f), new Vector3(0.012f, 0.06f, 0.012f), tuft, new Vector3(0f, 0f, i * 18f));
                Part(w, "Rock", PrimitiveType.Sphere, new Vector3(-0.05f, 0.04f, 0.32f), new Vector3(0.14f, 0.08f, 0.11f), Palette.Stone);
                Part(w, "Rock", PrimitiveType.Sphere, new Vector3(0.22f, 0.03f, 0.02f), new Vector3(0.09f, 0.06f, 0.08f), Palette.Stone);
            }

            // Đất chưa cày: mặt phẳng, chai, màu nhạt.
            var unplowed = new GameObject("Unplowed").transform;
            unplowed.SetParent(t, false);
            if (paddy)
            {
                Part(unplowed, "HardMud", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.9f, 0.04f, 0.9f), Mat("HardMud", Palette.Hex(0x7a6244)));
                AddPaddyBanks(unplowed);
            }
            else
            {
                Part(unplowed, "HardSoil", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(0.96f, 0.06f, 0.96f), Mat("HardSoil", Palette.Hex(0x7d5f3e)));
            }

            // Đất đã cày: luống (ruộng cạn) / bùn sẫm có rãnh (ruộng nước).
            var prepared = new GameObject("Prepared").transform;
            prepared.SetParent(t, false);
            if (paddy)
            {
                Part(prepared, "Mud", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.9f, 0.04f, 0.9f), Mat("PaddyMud", Palette.Hex(0x4e3b26)));
                foreach (float z in new[] { -0.25f, 0f, 0.25f })
                    Part(prepared, "Ridge", PrimitiveType.Cube, new Vector3(0f, 0.045f, z), new Vector3(0.82f, 0.012f, 0.06f), Mat("MudRidge", Palette.Hex(0x5e4830)));
                AddPaddyBanks(prepared);
            }
            else
            {
                Part(prepared, "Soil", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0f), new Vector3(0.96f, 0.08f, 0.96f), Palette.Soil);
                foreach (float z in new[] { -0.3f, 0f, 0.3f })
                    Part(prepared, "Furrow", PrimitiveType.Cube, new Vector3(0f, 0.09f, z), new Vector3(0.86f, 0.03f, 0.1f), Palette.SoilDark);
            }

            // Nước: ruộng nước = mặt nước đục dâng trong bờ; ruộng cạn = vệt đất ướt sẫm màu.
            var water = paddy
                ? Part(t, "Water", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f), new Vector3(0.86f, 0.01f, 0.86f), Mat("PaddyWater", Palette.Hex(0x4f7d86), emission: 0.03f))
                : Part(t, "WetSoil", PrimitiveType.Cube, new Vector3(0f, 0.082f, 0f), new Vector3(0.94f, 0.004f, 0.94f), Mat("WetSoil", Palette.Hex(0x3a2614)));
            water.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Cỏ dại (F4): mọc xen giữa các hàng cây, cao dần theo mức cỏ (gốc tại mặt đất để scale theo chiều cao).
            var weeds = new GameObject("Weeds").transform;
            weeds.SetParent(t, false);
            weeds.localPosition = new Vector3(0f, 0.06f, 0f);
            var weedMat = Mat("Weed", Palette.Hex(0x9bb53a));
            foreach (var p in new[] { new Vector3(0f, 0f, -0.3f), new Vector3(0f, 0f, 0.02f), new Vector3(0f, 0f, 0.3f),
                         new Vector3(-0.42f, 0f, 0.15f), new Vector3(0.42f, 0f, -0.15f), new Vector3(0.4f, 0f, 0.36f) })
                for (int i = -1; i <= 1; i++)
                    Part(weeds, "Weed", PrimitiveType.Cylinder, p + new Vector3(i * 0.025f, 0.07f, (i & 1) * 0.02f), new Vector3(0.012f, 0.07f, 0.012f), weedMat, new Vector3(0f, 0f, i * 25f));
            weeds.gameObject.SetActive(false);

            // Phân đã bón (F4): vài cục phân chuồng sẫm màu rải trên mặt ruộng.
            var manure = new GameObject("Fertilizer").transform;
            manure.SetParent(t, false);
            var manureMat = Mat("ManureLump", Palette.Hex(0x3b2a18));
            foreach (var p in new[] { new Vector3(-0.12f, 0f, -0.15f), new Vector3(0.12f, 0f, 0.14f), new Vector3(-0.1f, 0f, 0.32f), new Vector3(0.14f, 0f, -0.36f) })
                Part(manure, "Lump", PrimitiveType.Sphere, p + new Vector3(0f, 0.095f, 0f), new Vector3(0.07f, 0.03f, 0.06f), manureMat);
            manure.gameObject.SetActive(false);

            // Trạng thái trong prefab/scene: ruộng mới = đất hoang; ô vườn có sẵn = đã cày.
            unplowed.gameObject.SetActive(false);
            prepared.gameObject.SetActive(!startsWild);
            water.SetActive(!startsWild && startWater > 0.05f);

            var anchor = new GameObject("CropAnchor").transform;
            anchor.SetParent(t, false);
            anchor.localPosition = new Vector3(0f, 0.1f, 0f);

            SetPrivateField(plot, "cropAnchor", anchor);
            SetPrivateField(plot, "fieldType", type);
            SetPrivateField(plot, "startsWild", startsWild);
            SetPrivateField(plot, "startWater", startWater);
            SetPrivateField(plot, "wildVisual", wild);
            SetPrivateField(plot, "unplowedVisual", unplowed.gameObject);
            SetPrivateField(plot, "preparedVisual", prepared.gameObject);
            SetPrivateField(plot, "waterVisual", water.transform);
            SetPrivateField(plot, "weedsVisual", weeds);
            SetPrivateField(plot, "fertilizerVisual", manure.gameObject);
        }

        /// <summary>Giếng (M5d/F2): thành đá tròn, khung gỗ, gàu treo — nguồn nước để gánh tưới (không nuôi được ruộng nước).</summary>
        private static GameObject BuildWell()
        {
            var root = new GameObject("Well");
            AddBuildingComponents(root, new Vector3(0f, 0.3f, 0f), new Vector3(0.7f, 0.6f, 0.7f), 150f);
            AttachLevelModels(root, t =>
            {
                Part(t, "Wall", PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.62f, 0.15f, 0.62f), Palette.Stone);
                Part(t, "Water", PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(0.46f, 0.01f, 0.46f), Mat("WellWater", Palette.Hex(0x2f5f86), emission: 0.15f))
                    .GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Part(t, "PostL", PrimitiveType.Cube, new Vector3(-0.3f, 0.45f, 0f), new Vector3(0.05f, 0.6f, 0.05f), Palette.Wood);
                Part(t, "PostR", PrimitiveType.Cube, new Vector3(0.3f, 0.45f, 0f), new Vector3(0.05f, 0.6f, 0.05f), Palette.Wood);
                Part(t, "Beam", PrimitiveType.Cylinder, new Vector3(0f, 0.72f, 0f), new Vector3(0.05f, 0.33f, 0.05f), Palette.Wood, new Vector3(0f, 0f, 90f));
                Part(t, "Rope", PrimitiveType.Cylinder, new Vector3(0f, 0.6f, 0f), new Vector3(0.012f, 0.12f, 0.012f), Palette.Rope);
                Part(t, "Pail", PrimitiveType.Cylinder, new Vector3(0f, 0.44f, 0f), new Vector3(0.12f, 0.06f, 0.12f), Palette.Plank);
            });
            var source = root.AddComponent<WaterSource>();
            SetPrivateField(source, "radius", 0.3f);
            SetPrivateField(source, "feedsPaddies", false);
            return root;
        }

        /// <summary>
        /// Cối giã (M5d/F5): cối gỗ khoét lòng + chày, phía sau là giàn tre phơi 4 bó lúa
        /// (bó còn ướt màu xanh vàng, khô thì vàng óng).
        /// </summary>
        private static GameObject BuildRiceMortar(ResourceTypeData sheaf, ResourceTypeData grain, ResourceTypeData rice)
        {
            var root = new GameObject("RiceMortar");
            AddBuildingComponents(root, new Vector3(0f, 0.3f, 0f), new Vector3(0.85f, 0.6f, 0.85f), 120f);
            var wetMat = Mat("SheafWet", Palette.Hex(0x7f9a3e));
            var dryMat = Mat("SheafDry", Palette.Hex(0xdcb85a), emission: 0.05f);
            var sheaves = new List<Renderer>();
            GameObject grainPile = null;
            AttachLevelModels(root, t =>
            {
                var m = new Vector3(-0.1f, 0f, -0.18f);
                Part(t, "Mortar", PrimitiveType.Cylinder, m + new Vector3(0f, 0.14f, 0f), new Vector3(0.3f, 0.14f, 0.3f), Palette.Wood);
                Part(t, "Hollow", PrimitiveType.Cylinder, m + new Vector3(0f, 0.282f, 0f), new Vector3(0.2f, 0.004f, 0.2f), Palette.DarkWood);
                grainPile = Part(t, "Grain", PrimitiveType.Sphere, m + new Vector3(0f, 0.28f, 0f), new Vector3(0.16f, 0.04f, 0.16f),
                    Mat("ThreshedGrain", Palette.Hex(0xe8d9a8))).gameObject;
                Part(t, "Pestle", PrimitiveType.Cylinder, m + new Vector3(0.14f, 0.36f, 0.02f), new Vector3(0.045f, 0.3f, 0.045f), Palette.Plank, new Vector3(0f, 0f, -22f));
                Part(t, "GrainBasket", PrimitiveType.Cylinder, new Vector3(0.28f, 0.035f, -0.28f), new Vector3(0.22f, 0.035f, 0.22f), Mat("Basket", Palette.Hex(0x9c7a45)));

                // Giàn phơi: 2 cọc + 1 sào ngang, bó lúa treo dọc sào.
                Part(t, "RackPostL", PrimitiveType.Cylinder, new Vector3(-0.4f, 0.3f, 0.3f), new Vector3(0.04f, 0.3f, 0.04f), Palette.Wood);
                Part(t, "RackPostR", PrimitiveType.Cylinder, new Vector3(0.4f, 0.3f, 0.3f), new Vector3(0.04f, 0.3f, 0.04f), Palette.Wood);
                Part(t, "RackPole", PrimitiveType.Cylinder, new Vector3(0f, 0.56f, 0.3f), new Vector3(0.03f, 0.42f, 0.03f), Palette.Plank, new Vector3(0f, 0f, 90f));
                for (int i = 0; i < 4; i++)
                {
                    var bundle = Part(t, $"Sheaf{i}", PrimitiveType.Capsule, new Vector3(-0.27f + i * 0.18f, 0.38f, 0.3f), new Vector3(0.1f, 0.15f, 0.1f), wetMat);
                    sheaves.Add(bundle.GetComponent<Renderer>());
                    bundle.gameObject.SetActive(false);
                }
                grainPile.SetActive(false);
            });
            var mortar = root.AddComponent<RiceMortar>();
            SetPrivateField(mortar, "sheaf", sheaf);
            SetPrivateField(mortar, "grain", grain);
            SetPrivateField(mortar, "rice", rice);
            SetPrivateField(mortar, "rackSheaves", sheaves);
            SetPrivateField(mortar, "wetSheafMaterial", wetMat);
            SetPrivateField(mortar, "drySheafMaterial", dryMat);
            SetPrivateField(mortar, "grainInMortar", grainPile);
            return root;
        }

        /// <summary>
        /// Một ô mương (M5d/F6): chưa đào là 4 cọc + dây căng; đào xong là lòng mương đất sẫm ở giữa + nhánh
        /// nối sang ô bên cạnh (mương khác, ruộng, ao); có nước chảy thì mặt nước xanh hiện trong lòng mương.
        /// </summary>
        private static GameObject BuildCanal()
        {
            var root = new GameObject("Canal");
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = true; // đi xuyên được, chỉ để click chọn
            col.center = new Vector3(0f, 0.05f, 0f);
            col.size = new Vector3(0.96f, 0.1f, 0.96f);
            root.AddComponent<BuildingInstance>();
            AttachLevelModels(root, _ => { });
            var t = root.transform;

            var marked = new GameObject("Marked").transform;
            marked.SetParent(t, false);
            foreach (var corner in new[] { new Vector2(-0.22f, -0.22f), new Vector2(0.22f, -0.22f), new Vector2(0.22f, 0.22f), new Vector2(-0.22f, 0.22f) })
                Part(marked, "Stake", PrimitiveType.Cylinder, new Vector3(corner.x, 0.08f, corner.y), new Vector3(0.025f, 0.08f, 0.025f), Palette.Wood);
            foreach (var (pos, size) in new[] {
                         (new Vector3(0f, 0.13f, 0.22f), new Vector3(0.44f, 0.006f, 0.006f)), (new Vector3(0f, 0.13f, -0.22f), new Vector3(0.44f, 0.006f, 0.006f)),
                         (new Vector3(0.22f, 0.13f, 0f), new Vector3(0.006f, 0.006f, 0.44f)), (new Vector3(-0.22f, 0.13f, 0f), new Vector3(0.006f, 0.006f, 0.44f)) })
                Part(marked, "String", PrimitiveType.Cube, pos, size, Palette.Rope);

            var bed = Mat("CanalBed", Palette.Hex(0x3d2a17));
            var bank = Mat("CanalBank", Palette.Hex(0x6b4a2a));
            var water = Mat("CanalWater", Palette.Hex(0x3f7f95), emission: 0.08f);
            const float width = 0.4f;

            GameObject Segment(string name, Vector3 center, Vector3 size, out GameObject waterSurface)
            {
                var seg = new GameObject(name).transform;
                seg.SetParent(t, false);
                Part(seg, "Bed", PrimitiveType.Cube, center + new Vector3(0f, 0.004f, 0f), new Vector3(size.x, 0.008f, size.z), bed);
                waterSurface = Part(t, $"{name}Water", PrimitiveType.Cube, center + new Vector3(0f, 0.018f, 0f), new Vector3(size.x, 0.006f, size.z), water);
                waterSurface.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                waterSurface.SetActive(false);
                seg.gameObject.SetActive(false);
                return seg.gameObject;
            }

            var waters = new GameObject[5];
            var center = Segment("Center", Vector3.zero, new Vector3(width, 0f, width), out waters[0]);
            // Bờ đất đắp 4 góc giữa các nhánh.
            foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) })
                Part(center.transform, "Bank", PrimitiveType.Cube, new Vector3(c.x * 0.24f, 0.025f, c.y * 0.24f), new Vector3(0.08f, 0.05f, 0.08f), bank);
            var arms = new GameObject[4];
            Vector3[] dirs = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
            string[] names = { "ArmN", "ArmE", "ArmS", "ArmW" };
            for (int i = 0; i < 4; i++)
            {
                Vector3 d = dirs[i];
                Vector3 size = new Vector3(Mathf.Abs(d.x) > 0f ? 0.3f : width, 0f, Mathf.Abs(d.z) > 0f ? 0.3f : width);
                arms[i] = Segment(names[i], d * 0.35f, size, out waters[i + 1]);
                Vector3 side = new Vector3(d.z, 0f, d.x); // vuông góc với nhánh
                Part(arms[i].transform, "Bank", PrimitiveType.Cube, d * 0.35f + side * 0.24f + new Vector3(0f, 0.025f, 0f),
                    new Vector3(Mathf.Abs(d.x) > 0f ? 0.3f : 0.08f, 0.05f, Mathf.Abs(d.z) > 0f ? 0.3f : 0.08f), bank);
                Part(arms[i].transform, "Bank", PrimitiveType.Cube, d * 0.35f - side * 0.24f + new Vector3(0f, 0.025f, 0f),
                    new Vector3(Mathf.Abs(d.x) > 0f ? 0.3f : 0.08f, 0.05f, Mathf.Abs(d.z) > 0f ? 0.3f : 0.08f), bank);
            }

            var source = root.AddComponent<WaterSource>();
            SetPrivateField(source, "radius", 0.2f);
            SetPrivateField(source, "feedsPaddies", true);
            SetPrivateField(source, "natural", false); // guồng nước không múc từ mương
            source.enabled = false; // chỉ là nguồn nước khi có nước chảy

            var canal = root.AddComponent<Canal>();
            SetPrivateField(canal, "markedVisual", marked.gameObject);
            SetPrivateField(canal, "centerTrench", center);
            SetPrivateField(canal, "armTrenches", arms);
            SetPrivateField(canal, "waterSurfaces", waters);
            SetPrivateField(canal, "waterSource", source);
            return root;
        }

        /// <summary>
        /// Guồng nước (M5d/F7): bánh xe tre có 8 cánh múc gắn trên trục giữa 2 cột chống, máng tre trên đỉnh
        /// đổ nước xuống; bánh xe quay + dòng nước rơi khi đang bơm vào mương.
        /// </summary>
        private static GameObject BuildWaterWheel()
        {
            var root = new GameObject("WaterWheel");
            AddBuildingComponents(root, new Vector3(0f, 0.45f, 0f), new Vector3(0.7f, 0.9f, 0.85f), 150f);
            var bamboo = Mat("Bamboo", Palette.Hex(0xa8a04a));
            var water = Mat("CanalWater", Palette.Hex(0x3f7f95), emission: 0.08f);
            Transform wheel = null;
            GameObject pouring = null;
            AttachLevelModels(root, t =>
            {
                const float hubHeight = 0.45f, radius = 0.36f;
                Part(t, "PostL", PrimitiveType.Cube, new Vector3(-0.22f, hubHeight * 0.5f, 0f), new Vector3(0.06f, hubHeight + 0.05f, 0.06f), Palette.Wood);
                Part(t, "PostR", PrimitiveType.Cube, new Vector3(0.22f, hubHeight * 0.5f, 0f), new Vector3(0.06f, hubHeight + 0.05f, 0.06f), Palette.Wood);
                Part(t, "Axle", PrimitiveType.Cylinder, new Vector3(0f, hubHeight, 0f), new Vector3(0.04f, 0.27f, 0.04f), Palette.DarkWood, new Vector3(0f, 0f, 90f));

                wheel = new GameObject("Wheel").transform;
                wheel.SetParent(t, false);
                wheel.localPosition = new Vector3(0f, hubHeight, 0f);
                Part(wheel, "Hub", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.12f, 0.08f, 0.12f), Palette.Wood, new Vector3(0f, 0f, 90f));
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f;
                    var rot = Quaternion.Euler(angle, 0f, 0f);
                    Part(wheel, "Spoke", PrimitiveType.Cube, rot * new Vector3(0f, radius * 0.5f, 0f), new Vector3(0.025f, radius, 0.025f), bamboo, new Vector3(angle, 0f, 0f));
                    Part(wheel, "Paddle", PrimitiveType.Cube, rot * new Vector3(0f, radius, 0f), new Vector3(0.2f, 0.025f, 0.12f), Palette.Plank, new Vector3(angle, 0f, 0f));
                    // Ống tre múc nước gắn mép bánh xe.
                    Part(wheel, "Tube", PrimitiveType.Cylinder, rot * new Vector3(0.12f, radius * 0.95f, 0.05f), new Vector3(0.04f, 0.05f, 0.04f), bamboo, new Vector3(angle + 60f, 0f, 0f));
                }

                // Máng tre trên đỉnh, chĩa ra một bên (phía mương), có cột đỡ.
                Part(t, "Trough", PrimitiveType.Cube, new Vector3(0.2f, hubHeight + radius + 0.04f, 0f), new Vector3(0.42f, 0.03f, 0.08f), bamboo, new Vector3(0f, 0f, -8f));
                Part(t, "TroughPost", PrimitiveType.Cube, new Vector3(0.38f, (hubHeight + radius) * 0.5f, 0f), new Vector3(0.04f, hubHeight + radius, 0.04f), Palette.Wood);
                pouring = Part(t, "Pouring", PrimitiveType.Cube, new Vector3(0.43f, (hubHeight + radius) * 0.5f, 0f), new Vector3(0.05f, hubHeight + radius - 0.04f, 0.05f), water);
                pouring.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            });
            var ww = root.AddComponent<WaterWheel>();
            SetPrivateField(ww, "wheel", wheel);
            SetPrivateField(ww, "pouringWater", pouring);
            SetPrivateField(ww, "model", root.transform.Find("Level1"));
            return root;
        }

        /// <summary>Đê (M6/D3): ụ đất dài phủ cỏ, cọc tre kè hai bên. Đi qua được (trigger), không chặn đường.</summary>
        private static GameObject BuildLevee()
        {
            var root = new GameObject("Levee");
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0.12f, 0f);
            col.size = new Vector3(0.96f, 0.24f, 0.96f);
            root.AddComponent<BuildingInstance>();
            AttachLevelModels(root, t =>
            {
                var earth = Mat("LeveeEarth", Palette.Hex(0x7a5a36));
                var turf = Mat("PaddyBank", Palette.Hex(0x6f8a3c));
                Part(t, "Mound", PrimitiveType.Cube, new Vector3(0f, 0.09f, 0f), new Vector3(0.98f, 0.18f, 0.6f), earth);
                Part(t, "Top", PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.98f, 0.05f, 0.42f), turf);
                Part(t, "SlopeN", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.33f), new Vector3(0.98f, 0.1f, 0.2f), earth, new Vector3(-25f, 0f, 0f));
                Part(t, "SlopeS", PrimitiveType.Cube, new Vector3(0f, 0.06f, -0.33f), new Vector3(0.98f, 0.1f, 0.2f), earth, new Vector3(25f, 0f, 0f));
                for (int i = 0; i < 5; i++)
                {
                    float x = -0.4f + i * 0.2f;
                    Part(t, "Stake", PrimitiveType.Cylinder, new Vector3(x, 0.14f, 0.44f), new Vector3(0.03f, 0.14f, 0.03f), Palette.Wood);
                    Part(t, "Stake", PrimitiveType.Cylinder, new Vector3(x, 0.14f, -0.44f), new Vector3(0.03f, 0.14f, 0.03f), Palette.Wood);
                }
            });
            SetPrivateField(root.AddComponent<Levee>(), "model", root.transform.Find("Level1"));
            return root;
        }

        private static void AddPaddyBanks(Transform parent)
        {
            var bank = Mat("PaddyBank", Palette.Hex(0x6f8a3c)); // bờ đất có cỏ
            Part(parent, "Bank", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.46f), new Vector3(0.98f, 0.12f, 0.07f), bank);
            Part(parent, "Bank", PrimitiveType.Cube, new Vector3(0f, 0.06f, -0.46f), new Vector3(0.98f, 0.12f, 0.07f), bank);
            Part(parent, "Bank", PrimitiveType.Cube, new Vector3(0.46f, 0.06f, 0f), new Vector3(0.07f, 0.12f, 0.98f), bank);
            Part(parent, "Bank", PrimitiveType.Cube, new Vector3(-0.46f, 0.06f, 0f), new Vector3(0.07f, 0.12f, 0.98f), bank);
        }

        private static GameObject BuildHut()
        {
            var root = new GameObject("Hut");
            AddBuildingComponents(root, new Vector3(0f, 0.55f, 0f), new Vector3(0.9f, 1.1f, 0.9f), 200f);
            AttachLevelModels(root, HutTeepee(1f), HutTeepee(1.12f, extraPelt: true), HutThatched, HutStilt, HutLonghouse);
            return root;
        }

        // Cấp 1–2: lều da hình nón (cấp 2 to hơn, thêm tấm da phơi).
        private static System.Action<Transform> HutTeepee(float scale, bool extraPelt = false) => t =>
        {
            t.localScale = Vector3.one * scale;
            Cone(t, "Cover", 9, Vector3.zero, new Vector3(0.82f, 1.05f, 0.82f), Palette.Hide);
            Part(t, "Band", PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(0.52f, 0.04f, 0.52f), Palette.Plank);
            Part(t, "Door", PrimitiveType.Cube, new Vector3(0f, 0.17f, -0.33f), new Vector3(0.2f, 0.34f, 0.05f), Palette.DarkWood, new Vector3(23f, 0f, 0f));
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 20f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Part(t, $"Pole{i}", PrimitiveType.Cylinder, new Vector3(0f, 1.1f, 0f) + dir * 0.05f,
                    new Vector3(0.035f, 0.22f, 0.035f), Palette.Wood, new Vector3(20f, a, 0f));
            }
            if (extraPelt)
            {
                Part(t, "Band2", PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(0.66f, 0.04f, 0.66f), Palette.Fur);
                Part(t, "Pelt", PrimitiveType.Cube, new Vector3(0.36f, 0.3f, 0.1f), new Vector3(0.03f, 0.3f, 0.24f), Palette.Fur, new Vector3(0f, 0f, -15f));
            }
        };

        // Cấp 3: nhà tròn tường đất, mái lá.
        private static void HutThatched(Transform t)
        {
            Part(t, "Wall", PrimitiveType.Cylinder, new Vector3(0f, 0.24f, 0f), new Vector3(0.78f, 0.24f, 0.78f), Mat("MudWall", Palette.Hex(0x9a7652)));
            Cone(t, "Roof", 9, new Vector3(0f, 0.46f, 0f), new Vector3(1.02f, 0.6f, 1.02f), Palette.Thatch);
            Part(t, "Door", PrimitiveType.Cube, new Vector3(0f, 0.17f, -0.38f), new Vector3(0.22f, 0.34f, 0.04f), Palette.DarkWood);
        }

        // Cấp 4: nhà sàn gỗ — sàn trên cột, tường ván, mái lá 4 mái, thang.
        private static void HutStilt(Transform t)
        {
            foreach (var p in new[] { new Vector3(-0.35f, 0f, -0.35f), new Vector3(0.35f, 0f, -0.35f), new Vector3(-0.35f, 0f, 0.35f), new Vector3(0.35f, 0f, 0.35f) })
                Part(t, "Stilt", PrimitiveType.Cylinder, p + new Vector3(0f, 0.15f, 0f), new Vector3(0.07f, 0.15f, 0.07f), Palette.Wood);
            Part(t, "Floor", PrimitiveType.Cube, new Vector3(0f, 0.31f, 0f), new Vector3(0.9f, 0.05f, 0.9f), Palette.Plank);
            Part(t, "Wall", PrimitiveType.Cube, new Vector3(0f, 0.53f, 0f), new Vector3(0.74f, 0.4f, 0.74f), Palette.Plank);
            Cone(t, "Roof", 4, new Vector3(0f, 0.73f, 0f), new Vector3(1.25f, 0.45f, 1.25f), Palette.Thatch, new Vector3(0f, 45f, 0f));
            Part(t, "Door", PrimitiveType.Cube, new Vector3(0f, 0.48f, -0.38f), new Vector3(0.2f, 0.3f, 0.03f), Palette.DarkWood);
            Part(t, "Ladder", PrimitiveType.Cube, new Vector3(0f, 0.15f, -0.5f), new Vector3(0.16f, 0.34f, 0.03f), Palette.Wood, new Vector3(-25f, 0f, 0f));
        }

        // Cấp 5: nhà dài — thân dài, mái hai dốc, khói bếp.
        private static void HutLonghouse(Transform t)
        {
            Part(t, "Wall", PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(0.62f, 0.5f, 0.92f), Palette.Plank);
            Part(t, "RoofL", PrimitiveType.Cube, new Vector3(-0.2f, 0.62f, 0f), new Vector3(0.48f, 0.05f, 0.98f), Palette.Thatch, new Vector3(0f, 0f, 38f));
            Part(t, "RoofR", PrimitiveType.Cube, new Vector3(0.2f, 0.62f, 0f), new Vector3(0.48f, 0.05f, 0.98f), Palette.Thatch, new Vector3(0f, 0f, -38f));
            Part(t, "Ridge", PrimitiveType.Cylinder, new Vector3(0f, 0.77f, 0f), new Vector3(0.06f, 0.5f, 0.06f), Palette.Wood, new Vector3(90f, 0f, 0f));
            Part(t, "Door", PrimitiveType.Cube, new Vector3(0f, 0.18f, -0.47f), new Vector3(0.22f, 0.36f, 0.03f), Palette.DarkWood);
            Part(t, "Chimney", PrimitiveType.Cylinder, new Vector3(0.12f, 0.78f, 0.25f), new Vector3(0.1f, 0.12f, 0.1f), Palette.Stone);
        }

        private static GameObject BuildStorage()
        {
            var root = new GameObject("Storage");
            AddBuildingComponents(root, new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 1f, 0.9f), 300f);
            AttachLevelModels(root, StorageShed(1f, 1), StorageShed(1.08f, 2), StorageGranary, StorageCellar, StorageBigGranary);
            return root;
        }

        // Cấp 1–2: kho gỗ mái lá + đống củi (cấp 2 to hơn, thêm đống củi).
        private static System.Action<Transform> StorageShed(float scale, int logPiles) => t =>
        {
            t.localScale = Vector3.one * scale;
            Part(t, "Base", PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(0.78f, 0.5f, 0.78f), Palette.Plank);
            Cone(t, "Roof", 4, new Vector3(0f, 0.5f, 0f), new Vector3(1.34f, 0.45f, 1.34f), Palette.Thatch, new Vector3(0f, 45f, 0f));
            Part(t, "Door", PrimitiveType.Cube, new Vector3(0f, 0.17f, -0.4f), new Vector3(0.26f, 0.34f, 0.03f), Palette.DarkWood);
            for (int pile = 0; pile < logPiles; pile++)
            {
                float x = pile == 0 ? 0.48f : -0.48f;
                foreach (var p in new[] { new Vector3(x, 0.05f, -0.07f), new Vector3(x, 0.05f, 0.07f), new Vector3(x, 0.14f, 0f) })
                    Part(t, "Log", PrimitiveType.Cylinder, p, new Vector3(0.09f, 0.22f, 0.09f), Palette.Wood, new Vector3(90f, 0f, 0f));
            }
        };

        // Cấp 3: lẫm lúa trên cột (tránh chuột, ẩm).
        private static void StorageGranary(Transform t)
        {
            foreach (var p in new[] { new Vector3(-0.25f, 0f, -0.25f), new Vector3(0.25f, 0f, -0.25f), new Vector3(-0.25f, 0f, 0.25f), new Vector3(0.25f, 0f, 0.25f) })
                Part(t, "Stilt", PrimitiveType.Cylinder, p + new Vector3(0f, 0.15f, 0f), new Vector3(0.07f, 0.15f, 0.07f), Palette.Wood);
            Part(t, "Body", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(0.72f, 0.25f, 0.72f), Mat("MudWall", Palette.Hex(0x9a7652)));
            Cone(t, "Roof", 9, new Vector3(0f, 0.8f, 0f), new Vector3(0.95f, 0.45f, 0.95f), Palette.Thatch);
            Part(t, "Ladder", PrimitiveType.Cube, new Vector3(0f, 0.25f, -0.42f), new Vector3(0.14f, 0.5f, 0.03f), Palette.Wood, new Vector3(-20f, 0f, 0f));
        }

        // Cấp 4: hầm chứa đắp đất, cửa đá.
        private static void StorageCellar(Transform t)
        {
            Part(t, "Mound", PrimitiveType.Sphere, new Vector3(0f, 0.12f, 0.05f), new Vector3(0.95f, 0.5f, 0.9f), Mat("EarthMound", Palette.Hex(0x6d7a45)));
            Part(t, "Frame", PrimitiveType.Cube, new Vector3(0f, 0.2f, -0.36f), new Vector3(0.38f, 0.34f, 0.1f), Palette.Stone);
            Part(t, "Door", PrimitiveType.Cube, new Vector3(0f, 0.18f, -0.42f), new Vector3(0.24f, 0.28f, 0.03f), Palette.DarkWood);
            Part(t, "Vent", PrimitiveType.Cylinder, new Vector3(0.18f, 0.42f, 0.15f), new Vector3(0.08f, 0.08f, 0.08f), Palette.Stone);
        }

        // Cấp 5: kho lương lớn — nhà kho + lẫm phụ + bao/giỏ lương thực.
        private static void StorageBigGranary(Transform t)
        {
            Part(t, "Base", PrimitiveType.Cube, new Vector3(-0.1f, 0.25f, 0.05f), new Vector3(0.66f, 0.5f, 0.78f), Palette.Plank);
            Cone(t, "Roof", 4, new Vector3(-0.1f, 0.5f, 0.05f), new Vector3(1.15f, 0.45f, 1.25f), Palette.Thatch, new Vector3(0f, 45f, 0f));
            Part(t, "Silo", PrimitiveType.Cylinder, new Vector3(0.33f, 0.3f, -0.25f), new Vector3(0.28f, 0.3f, 0.28f), Mat("MudWall", Palette.Hex(0x9a7652)));
            Cone(t, "SiloRoof", 7, new Vector3(0.33f, 0.6f, -0.25f), new Vector3(0.38f, 0.25f, 0.38f), Palette.Thatch);
            Part(t, "Door", PrimitiveType.Cube, new Vector3(-0.1f, 0.17f, -0.35f), new Vector3(0.24f, 0.34f, 0.03f), Palette.DarkWood);
            foreach (var p in new[] { new Vector3(-0.42f, 0.08f, -0.4f), new Vector3(-0.3f, 0.08f, -0.45f) })
                Part(t, "Sack", PrimitiveType.Sphere, p, new Vector3(0.14f, 0.16f, 0.14f), Mat("Sack", Palette.Hex(0xc8b27a)));
        }

        // ─── Tree (ResourceNode) ─────────────────────────────────────────────
        private static GameObject BuildTree(ResourceTypeData wood)
        {
            var root = new GameObject("Tree");
            var node = root.AddComponent<ResourceNode>();
            SetPrivateField(node, "resourceType", wood);
            SetPrivateField(node, "amountRemaining", 10);
            SetPrivateField(node, "yieldPerHit", 1);
            AddObstacle(root, NavMeshObstacleShape.Capsule, new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 1f, 0.5f));

            BuildTreeVisual(root.transform);
            return root;
        }

        /// <summary>Dùng chung cho cây thu hoạch được và cây trang trí quanh bản đồ.</summary>
        public static void BuildTreeVisual(Transform parent)
        {
            Part(parent, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(0.23f, 0.35f, 0.23f), Palette.Wood);
            Cone(parent, "Leaves0", 7, new Vector3(0f, 0.5f, 0f), new Vector3(1.16f, 0.8f, 1.16f), Palette.LeavesDark);
            Cone(parent, "Leaves1", 7, new Vector3(0f, 0.93f, 0f), new Vector3(0.9f, 0.7f, 0.9f), Palette.Leaves, new Vector3(0f, 25f, 0f));
            Cone(parent, "Leaves2", 7, new Vector3(0f, 1.33f, 0f), new Vector3(0.6f, 0.55f, 0.6f), Palette.LeavesLight, new Vector3(0f, 50f, 0f));
        }

        // ─── Animals ─────────────────────────────────────────────────────────
        /// <summary>Phần chung của mọi vật nuôi: vòng cổ (đã thuần), biểu tượng sản phẩm, thanh máu, lưu prefab.</summary>
        private static GameObject FinishAnimal(GameObject root, AnimalController controller, Renderer[] bodyRenderers,
            Vector3 collarPosition, float collarSize, float iconHeight, string prefabPath)
        {
            var t = root.transform;
            var collar = Part(t, "TamedCollar", PrimitiveType.Cylinder, collarPosition, new Vector3(collarSize, 0.03f, collarSize), Palette.Rope, new Vector3(90f, 0f, 0f));
            var productIcon = Part(t, "ProductIcon", PrimitiveType.Sphere, new Vector3(0f, iconHeight, 0f), Vector3.one * 0.2f, Palette.ProductGlow);
            collar.SetActive(false); // thú mới là thú hoang — AnimalController bật lên khi thuần hóa
            productIcon.SetActive(false);

            SetPrivateField(controller, "bodyRenderers", bodyRenderers);
            SetPrivateField(controller, "tamedIndicator", collar);
            SetPrivateField(controller, "productIndicator", productIcon);
            BuildHealthBar(root, iconHeight + 0.25f);
            return SavePrefab(root, prefabPath);
        }

        private static AnimalData SaveAnimalData(string path)
        {
            var data = AssetDatabase.LoadAssetAtPath<AnimalData>(path);
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<AnimalData>();
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static List<ResourceAmount> Amounts(ResourceTypeData type, int amount) =>
            new List<ResourceAmount> { new ResourceAmount { type = type, amount = amount } };

        /// <summary>Dê núi (E3): thuần hóa để vắt sữa.</summary>
        private static void BuildGoatContent(Foods foods, ResourceTypeData manure)
        {
            var root = new GameObject("Goat");
            var controller = root.AddComponent<AnimalController>();
            SetPrivateField(root.AddComponent<HealthComponent>(), "maxHealth", 40f);
            var t = root.transform;

            var coat = Mat("GoatCoat", Palette.Hex(0xe4ddd0));
            var horn = Mat("GoatHorn", Palette.Hex(0x5b5146));
            var body = Part(t, "Body", PrimitiveType.Sphere, new Vector3(0f, 0.42f, 0f), new Vector3(0.36f, 0.36f, 0.62f), coat);
            var neck = Part(t, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0.25f), new Vector3(0.13f, 0.12f, 0.13f), coat, new Vector3(35f, 0f, 0f));
            var head = Part(t, "Head", PrimitiveType.Cube, new Vector3(0f, 0.66f, 0.36f), new Vector3(0.16f, 0.17f, 0.24f), coat);
            Part(t, "Beard", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0.45f), new Vector3(0.05f, 0.1f, 0.04f), Mat("GoatBeard", Palette.Hex(0xbfb6a6)));
            Part(t, "HornL", PrimitiveType.Cylinder, new Vector3(-0.05f, 0.79f, 0.3f), new Vector3(0.03f, 0.08f, 0.03f), horn, new Vector3(-40f, 0f, 0f));
            Part(t, "HornR", PrimitiveType.Cylinder, new Vector3(0.05f, 0.79f, 0.3f), new Vector3(0.03f, 0.08f, 0.03f), horn, new Vector3(-40f, 0f, 0f));
            var legs = new[] { new Vector3(-0.1f, 0.13f, 0.17f), new Vector3(0.1f, 0.13f, 0.17f), new Vector3(-0.1f, 0.13f, -0.18f), new Vector3(0.1f, 0.13f, -0.18f) };
            for (int i = 0; i < legs.Length; i++)
                Part(t, $"Leg{i}", PrimitiveType.Cylinder, legs[i], new Vector3(0.05f, 0.13f, 0.05f), horn);

            GameObject prefab = FinishAnimal(root, controller,
                new[] { body.GetComponent<Renderer>(), neck.GetComponent<Renderer>(), head.GetComponent<Renderer>() },
                new Vector3(0f, 0.55f, 0.22f), 0.3f, 1.05f, GoatPrefabPath);

            var data = SaveAnimalData(GoatDataPath);
            data.id = "goat";
            data.displayName = "Dê núi";
            data.prefab = prefab;
            data.hungerDecayInterval = 20f;
            data.hungerThresholdForNeeds = 50f;
            data.feedCost = Amounts(foods.pool, 2); // ăn loại thức ăn nào cũng được
            data.feedingsToTame = 3;
            data.reproductionInterval = 120f;
            data.products = Amounts(foods.milk, 2);
            data.products.Add(new ResourceAmount { type = manure, amount = 1 });
            data.productionInterval = 45f;
            data.huntYield = Amounts(foods.meat, 4);
            EditorUtility.SetDirty(data);
        }

        private static void BuildWildBoarContent(Foods foods, ResourceTypeData manure)
        {
            var root = new GameObject("WildBoar");
            var controller = root.AddComponent<AnimalController>();
            SetPrivateField(root.AddComponent<HealthComponent>(), "maxHealth", 60f); // thợ săn (14 dmg) hạ trong 5 phát
            var t = root.transform;

            var body = Part(t, "Body", PrimitiveType.Sphere, new Vector3(0f, 0.38f, 0f), new Vector3(0.51f, 0.47f, 0.78f), Palette.Boar);
            var head = Part(t, "Head", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0.4f), new Vector3(0.28f, 0.26f, 0.26f), Palette.Boar);
            var earL = Part(t, "EarL", PrimitiveType.Cube, new Vector3(-0.1f, 0.58f, 0.36f), new Vector3(0.07f, 0.1f, 0.04f), Palette.Boar, new Vector3(0f, 0f, 20f));
            var earR = Part(t, "EarR", PrimitiveType.Cube, new Vector3(0.1f, 0.58f, 0.36f), new Vector3(0.07f, 0.1f, 0.04f), Palette.Boar, new Vector3(0f, 0f, -20f));
            Part(t, "Ridge", PrimitiveType.Cube, new Vector3(0f, 0.6f, -0.02f), new Vector3(0.08f, 0.1f, 0.55f), Palette.BoarDark);
            Part(t, "Snout", PrimitiveType.Cylinder, new Vector3(0f, 0.38f, 0.57f), new Vector3(0.16f, 0.05f, 0.16f), Palette.Snout, new Vector3(90f, 0f, 0f));
            Part(t, "TuskL", PrimitiveType.Cube, new Vector3(-0.09f, 0.37f, 0.56f), new Vector3(0.03f, 0.09f, 0.03f), Palette.Ivory, new Vector3(-30f, 0f, 0f));
            Part(t, "TuskR", PrimitiveType.Cube, new Vector3(0.09f, 0.37f, 0.56f), new Vector3(0.03f, 0.09f, 0.03f), Palette.Ivory, new Vector3(-30f, 0f, 0f));

            var legs = new[] { new Vector3(-0.13f, 0.12f, 0.2f), new Vector3(0.13f, 0.12f, 0.2f), new Vector3(-0.13f, 0.12f, -0.22f), new Vector3(0.13f, 0.12f, -0.22f) };
            for (int i = 0; i < legs.Length; i++)
                Part(t, $"Leg{i}", PrimitiveType.Cylinder, legs[i], new Vector3(0.09f, 0.12f, 0.09f), Palette.BoarDark);

            // Vòng cổ + biểu tượng sản phẩm: phân biệt rõ con hoang / đã thuần / có sản phẩm (UX gap ở PROGRESS.md M3).
            GameObject prefab = FinishAnimal(root, controller,
                new[] { body.GetComponent<Renderer>(), head.GetComponent<Renderer>(), earL.GetComponent<Renderer>(), earR.GetComponent<Renderer>() },
                new Vector3(0f, 0.42f, 0.27f), 0.44f, 1f, BoarPrefabPath);

            var data = SaveAnimalData(BoarDataPath);
            data.id = "wild_boar";
            data.displayName = "Heo rừng";
            data.prefab = prefab;
            data.hungerDecayInterval = 20f;
            data.hungerThresholdForNeeds = 50f;
            data.feedCost = Amounts(foods.pool, 3); // ăn loại thức ăn nào cũng được
            data.feedingsToTame = 2;
            data.reproductionInterval = 90f;
            data.products = Amounts(foods.meat, 2);
            data.products.Add(new ResourceAmount { type = manure, amount = 1 });
            data.productionInterval = 30f;
            data.huntYield = Amounts(foods.meat, 6);
            EditorUtility.SetDirty(data);
        }

        // ─── Health bar ──────────────────────────────────────────────────────
        /// <summary>Thanh máu nổi trên đầu: nền tối + phần màu co giãn từ mép trái.</summary>
        internal static void BuildHealthBar(GameObject owner, float height, NpcController npc = null)
        {
            var barRoot = new GameObject("HealthBar");
            barRoot.transform.SetParent(owner.transform, false);
            barRoot.transform.localPosition = new Vector3(0f, height, 0f);

            var background = Part(barRoot.transform, "Background", PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.62f, 0.09f, 0.01f), Mat("BarBackground", Palette.Hex(0x1e1a14)));
            background.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Pivot ở mép trái: co giãn pivot theo x thì phần màu ngắn lại về phía trái.
            var fillPivot = new GameObject("FillPivot").transform;
            fillPivot.SetParent(barRoot.transform, false);
            fillPivot.localPosition = new Vector3(-0.29f, 0f, -0.012f);
            fillPivot.localScale = new Vector3(0.58f, 1f, 1f);
            var fill = Part(fillPivot, "Fill", PrimitiveType.Cube, new Vector3(0.5f, 0f, 0f),
                new Vector3(1f, 0.06f, 0.01f), Mat("BarFill", Color.white));
            var fillRenderer = fill.GetComponent<Renderer>();
            fillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            barRoot.SetActive(false);

            var bar = owner.AddComponent<HealthBar>();
            SetPrivateField(bar, "health", owner.GetComponent<HealthComponent>());
            SetPrivateField(bar, "npc", npc);
            SetPrivateField(bar, "barRoot", barRoot);
            SetPrivateField(bar, "fill", fillPivot);
            SetPrivateField(bar, "fillRenderer", fillRenderer);
        }

        // ─── Professions + villager ──────────────────────────────────────────
        // autoWork = việc tự làm khi rảnh (M5.6): dân làng chặt cây, nông dân làm ruộng + chăm thú,
        // thợ săn canh gác (đánh sói đang tấn công gần đó), trinh sát chỉ đi dạo.
        private static ProfessionData[] BuildProfessions() => new[]
        {
            SaveProfession("villager", "Dân làng", 0x8a5a2b, speed: 3f, health: 100f, damage: 5f, range: 1.2f, vision: 8f,
                NpcCapability.Gather | NpcCapability.Build | NpcCapability.Fight, NpcCapability.Gather | NpcCapability.Build),
            SaveProfession("farmer", "Nông dân", 0x7a9a3a, speed: 2.6f, health: 90f, damage: 3f, range: 1.2f, vision: 7f,
                NpcCapability.Farm | NpcCapability.TendAnimals | NpcCapability.Gather, NpcCapability.Farm | NpcCapability.TendAnimals),
            SaveProfession("hunter", "Thợ săn", 0x5a4030, speed: 3.2f, health: 110f, damage: 14f, range: 5f, vision: 10f,
                NpcCapability.Hunt | NpcCapability.Fight | NpcCapability.Gather, NpcCapability.Hunt),
            SaveProfession("scout", "Trinh sát", 0x4a7fa8, speed: 4.2f, health: 70f, damage: 6f, range: 1.2f, vision: 16f,
                NpcCapability.Scout | NpcCapability.Fight, NpcCapability.None),
        };

        private static ProfessionData SaveProfession(string id, string displayName, int color, float speed, float health,
            float damage, float range, float vision, NpcCapability capabilities, NpcCapability autoWork)
        {
            string path = ProfessionAssetPath(id);
            var data = AssetDatabase.LoadAssetAtPath<ProfessionData>(path);
            bool isNew = data == null;
            if (isNew) data = ScriptableObject.CreateInstance<ProfessionData>();

            data.id = id;
            data.displayName = displayName;
            data.tunicColor = Palette.Hex(color);
            data.moveSpeed = speed;
            data.maxHealth = health;
            data.attackDamage = damage;
            data.attackRange = range;
            data.visionRange = vision;
            data.capabilities = capabilities;
            data.autoWork = autoWork;

            if (isNew) AssetDatabase.CreateAsset(data, path);
            else EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject BuildVillager(ProfessionData[] professions)
        {
            var root = new GameObject("Villager");
            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 1.1f;
            agent.speed = 3f;
            agent.angularSpeed = 720f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.1f;
            var villagerHealth = root.AddComponent<HealthComponent>();
            SetPrivateField(villagerHealth, "regenPerSecond", 1f); // hồi 1 máu/giây khi không bị đánh 6 giây
            var npc = root.AddComponent<NpcController>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            Part(visual, "LegL", PrimitiveType.Cylinder, new Vector3(-0.09f, 0.16f, 0f), new Vector3(0.12f, 0.16f, 0.12f), Palette.Skin);
            Part(visual, "LegR", PrimitiveType.Cylinder, new Vector3(0.09f, 0.16f, 0f), new Vector3(0.12f, 0.16f, 0.12f), Palette.Skin);
            var tunic = Part(visual, "Tunic", PrimitiveType.Cylinder, new Vector3(0f, 0.52f, 0f), new Vector3(0.44f, 0.23f, 0.44f), Palette.Fur);
            Part(visual, "ArmL", PrimitiveType.Cylinder, new Vector3(-0.24f, 0.55f, 0f), new Vector3(0.1f, 0.16f, 0.1f), Palette.Skin);
            Part(visual, "ArmR", PrimitiveType.Cylinder, new Vector3(0.24f, 0.55f, 0f), new Vector3(0.1f, 0.16f, 0.1f), Palette.Skin);
            Part(visual, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.9f, 0f), Vector3.one * 0.32f, Palette.Skin);

            var maleHair = Part(visual, "HairMale", PrimitiveType.Sphere, new Vector3(0f, 0.99f, -0.03f), new Vector3(0.36f, 0.24f, 0.36f), Palette.Hair);

            // Tóc nữ: mái + tóc dài phía sau + búi — nhận ra giới tính từ góc nhìn trên cao.
            var femaleHair = new GameObject("HairFemale");
            femaleHair.transform.SetParent(visual, false);
            Part(femaleHair.transform, "Top", PrimitiveType.Sphere, new Vector3(0f, 0.99f, -0.02f), new Vector3(0.37f, 0.25f, 0.37f), Palette.Hair);
            Part(femaleHair.transform, "Back", PrimitiveType.Cube, new Vector3(0f, 0.78f, -0.14f), new Vector3(0.26f, 0.36f, 0.08f), Palette.Hair);
            Part(femaleHair.transform, "Bun", PrimitiveType.Sphere, new Vector3(0f, 1.1f, -0.08f), Vector3.one * 0.13f, Palette.Hair);

            var tools = new List<ProfessionTool>
            {
                new ProfessionTool { profession = professions[0], tool = BuildAxe(visual) },
                new ProfessionTool { profession = professions[1], tool = BuildHoe(visual) },
                new ProfessionTool { profession = professions[2], tool = BuildSpear(visual) },
                new ProfessionTool { profession = professions[3], tool = BuildFeatherBand(visual) },
            };

            var selectionRing = new GameObject("SelectionRing");
            selectionRing.transform.SetParent(root.transform, false);
            selectionRing.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            selectionRing.transform.localScale = Vector3.one * 0.6f;
            selectionRing.AddComponent<MeshFilter>().sharedMesh = RingMesh(0.62f, 0.72f, 40);
            var ringRenderer = selectionRing.AddComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = Mat("SelectionRing", Palette.Hex(0xb8f07a), emission: 0.8f);
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            selectionRing.SetActive(false);
            SetPrivateField(npc, "selectionRing", selectionRing);
            BuildHealthBar(root, 1.4f, npc);

            // Mặc định trong prefab: dân làng nam — chỉ bật tóc nam + rìu (NpcController tự đổi theo nghề/giới tính).
            femaleHair.SetActive(false);
            for (int i = 1; i < tools.Count; i++) tools[i].tool.SetActive(false);

            SetPrivateField(npc, "profession", professions[0]);
            SetPrivateField(npc, "tunicRenderer", tunic.GetComponent<Renderer>());
            SetPrivateField(npc, "maleHair", maleHair);
            SetPrivateField(npc, "femaleHair", femaleHair);
            SetPrivateField(npc, "tools", tools);

            // Gàu nước bên tay trái (M5d/F2) — chỉ hiện khi đang gánh nước.
            var bucket = new GameObject("WaterBucket").transform;
            bucket.SetParent(visual, false);
            Part(bucket, "Pail", PrimitiveType.Cylinder, new Vector3(-0.32f, 0.36f, 0.06f), new Vector3(0.16f, 0.08f, 0.16f), Palette.Plank);
            Part(bucket, "Water", PrimitiveType.Cylinder, new Vector3(-0.32f, 0.44f, 0.06f), new Vector3(0.13f, 0.005f, 0.13f), Mat("Water", Palette.Hex(0x3f7fb0), emission: 0.15f));
            Part(bucket, "Handle", PrimitiveType.Cube, new Vector3(-0.32f, 0.5f, 0.06f), new Vector3(0.015f, 0.12f, 0.015f), Palette.Rope);
            bucket.gameObject.SetActive(false);
            SetPrivateField(npc, "waterBucket", bucket.gameObject);
            return root;
        }

        private static GameObject BuildAxe(Transform parent)
        {
            var tool = new GameObject("Tool_Axe").transform;
            tool.SetParent(parent, false);
            Part(tool, "Handle", PrimitiveType.Cylinder, new Vector3(0.3f, 0.55f, 0.08f), new Vector3(0.04f, 0.25f, 0.04f), Palette.Wood, new Vector3(30f, 0f, 0f));
            Part(tool, "Head", PrimitiveType.Cube, new Vector3(0.3f, 0.76f, 0.2f), new Vector3(0.05f, 0.12f, 0.14f), Palette.Stone, new Vector3(30f, 0f, 0f));
            return tool.gameObject;
        }

        private static GameObject BuildHoe(Transform parent)
        {
            var tool = new GameObject("Tool_Hoe").transform;
            tool.SetParent(parent, false);
            Part(tool, "Handle", PrimitiveType.Cylinder, new Vector3(0.3f, 0.6f, 0.05f), new Vector3(0.04f, 0.35f, 0.04f), Palette.Wood, new Vector3(15f, 0f, 0f));
            Part(tool, "Blade", PrimitiveType.Cube, new Vector3(0.3f, 0.93f, 0.18f), new Vector3(0.12f, 0.03f, 0.16f), Palette.Stone, new Vector3(15f, 0f, 0f));
            return tool.gameObject;
        }

        private static GameObject BuildSpear(Transform parent)
        {
            var tool = new GameObject("Tool_Spear").transform;
            tool.SetParent(parent, false);
            Part(tool, "Shaft", PrimitiveType.Cylinder, new Vector3(0.3f, 0.62f, 0.05f), new Vector3(0.035f, 0.6f, 0.035f), Palette.Wood, new Vector3(14f, 0f, 0f));
            Cone(tool, "Tip", 5, new Vector3(0.3f, 1.2f, 0.2f), new Vector3(0.1f, 0.16f, 0.1f), Palette.Stone, new Vector3(14f, 0f, 0f));
            return tool.gameObject;
        }

        private static GameObject BuildFeatherBand(Transform parent)
        {
            var tool = new GameObject("Tool_FeatherBand").transform;
            tool.SetParent(parent, false);
            Part(tool, "Band", PrimitiveType.Cylinder, new Vector3(0f, 0.93f, 0f), new Vector3(0.34f, 0.02f, 0.34f), Palette.Rope);
            Part(tool, "Feather", PrimitiveType.Cube, new Vector3(0.1f, 1.12f, -0.12f), new Vector3(0.03f, 0.28f, 0.07f), Mat("Feather", Palette.Hex(0xe8e0c8)), new Vector3(-20f, 0f, -15f));
            return tool.gameObject;
        }

        // ─── Wolf (predator) ─────────────────────────────────────────────────
        private static void BuildWolfContent(ResourceTypeData food)
        {
            var data = AssetDatabase.LoadAssetAtPath<PredatorData>(WolfDataPath);
            bool isNew = data == null;
            if (isNew) data = ScriptableObject.CreateInstance<PredatorData>();

            data.id = "wolf";
            data.displayName = "Sói";
            data.maxHealth = 50f;
            data.patrolSpeed = 2f;
            data.chaseSpeed = 3.8f; // nhanh hơn dân làng (3), chậm hơn trinh sát (4.2)
            data.aggroRange = 4.5f;
            data.leashRange = 12f;
            data.patrolRadius = 2f;
            data.attackDamage = 8f;
            data.attackRange = 1.2f;
            data.attackInterval = 1.2f;
            data.huntYield = new List<ResourceAmount> { new ResourceAmount { type = food, amount = 4 } };

            if (isNew) AssetDatabase.CreateAsset(data, WolfDataPath);

            var root = new GameObject("Wolf");
            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 0.8f;
            agent.speed = data.patrolSpeed;
            agent.angularSpeed = 540f;
            agent.acceleration = 10f;
            agent.stoppingDistance = 0.2f;
            SetPrivateField(root.AddComponent<HealthComponent>(), "maxHealth", data.maxHealth);
            var ai = root.AddComponent<PredatorAI>();
            SetPrivateField(ai, "data", data);

            var fur = Mat("WolfFur", Palette.Hex(0x6b6d70));
            var furDark = Mat("WolfFurDark", Palette.Hex(0x45474a));
            var t = root.transform;
            Part(t, "Body", PrimitiveType.Sphere, new Vector3(0f, 0.42f, 0f), new Vector3(0.4f, 0.42f, 0.85f), fur);
            Part(t, "Head", PrimitiveType.Cube, new Vector3(0f, 0.56f, 0.45f), new Vector3(0.26f, 0.24f, 0.3f), fur);
            Part(t, "Snout", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0.66f), new Vector3(0.14f, 0.12f, 0.18f), furDark);
            Part(t, "Nose", PrimitiveType.Sphere, new Vector3(0f, 0.53f, 0.76f), Vector3.one * 0.05f, Mat("WolfNose", Palette.Hex(0x111111)));
            Cone(t, "EarL", 4, new Vector3(-0.08f, 0.66f, 0.42f), new Vector3(0.08f, 0.14f, 0.08f), furDark);
            Cone(t, "EarR", 4, new Vector3(0.08f, 0.66f, 0.42f), new Vector3(0.08f, 0.14f, 0.08f), furDark);
            var eyes = Mat("WolfEyes", Palette.Hex(0xf2c230), emission: 0.9f);
            Part(t, "EyeL", PrimitiveType.Sphere, new Vector3(-0.07f, 0.6f, 0.6f), Vector3.one * 0.04f, eyes);
            Part(t, "EyeR", PrimitiveType.Sphere, new Vector3(0.07f, 0.6f, 0.6f), Vector3.one * 0.04f, eyes);
            Part(t, "Tail", PrimitiveType.Cylinder, new Vector3(0f, 0.5f, -0.48f), new Vector3(0.07f, 0.18f, 0.07f), furDark, new Vector3(-55f, 0f, 0f));

            var legs = new[] { new Vector3(-0.12f, 0.2f, 0.25f), new Vector3(0.12f, 0.2f, 0.25f), new Vector3(-0.12f, 0.2f, -0.25f), new Vector3(0.12f, 0.2f, -0.25f) };
            for (int i = 0; i < legs.Length; i++)
                Part(t, $"Leg{i}", PrimitiveType.Cylinder, legs[i], new Vector3(0.07f, 0.2f, 0.07f), furDark);

            BuildHealthBar(root, 1.1f);

            data.prefab = SavePrefab(root, WolfPrefabPath);
            EditorUtility.SetDirty(data);
        }

        // ─── Công nghệ ───────────────────────────────────────────────────────
        /// <summary>Nông nghiệp mở thêm Rau; công nghệ mới "Trồng lúa" (cần Nông nghiệp) mở cây Lúa.</summary>
        private static void BuildTechs(ResourceTypeData knowledge, ResourceTypeData riceSeed)
        {
            var farming = AssetDatabase.LoadAssetAtPath<TechNode>(TechFarmingPath);
            if (farming == null)
            {
                Debug.LogError($"[GameContentBuilder] Khong tim thay {TechFarmingPath}");
                return;
            }
            farming.displayName = "Nông nghiệp";
            farming.unlockedCropIds = new List<string> { "berry", "vegetable" };
            farming.unlockedBuildingIds = new List<string> { "storage", "well" };
            EditorUtility.SetDirty(farming);

            var rice = AssetDatabase.LoadAssetAtPath<TechNode>(TechRicePath);
            bool isNew = rice == null;
            if (isNew) rice = ScriptableObject.CreateInstance<TechNode>();
            rice.id = "tech_rice";
            rice.displayName = "Trồng lúa";
            rice.cost = Amounts(knowledge, 10);
            rice.prerequisites = new List<TechNode> { farming };
            rice.unlockedBuildingIds = new List<string> { "paddy_field", "seedbed", "rice_mortar" };
            rice.unlockedCropIds = new List<string> { "rice", "rice_seedling" };
            rice.grantOnUnlock = Amounts(riceSeed, 4); // ít thóc giống để bắt đầu vụ đầu tiên
            if (isNew) AssetDatabase.CreateAsset(rice, TechRicePath);
            else EditorUtility.SetDirty(rice);

            var irrigation = AssetDatabase.LoadAssetAtPath<TechNode>(TechIrrigationPath);
            isNew = irrigation == null;
            if (isNew) irrigation = ScriptableObject.CreateInstance<TechNode>();
            irrigation.id = "tech_irrigation";
            irrigation.displayName = "Thủy lợi";
            irrigation.cost = Amounts(knowledge, 15);
            irrigation.prerequisites = new List<TechNode> { rice };
            irrigation.unlockedBuildingIds = new List<string> { "canal", "levee" };
            irrigation.unlockedCropIds = new List<string>();
            irrigation.grantOnUnlock = new List<ResourceAmount>();
            if (isNew) AssetDatabase.CreateAsset(irrigation, TechIrrigationPath);
            else EditorUtility.SetDirty(irrigation);

            var waterWheel = AssetDatabase.LoadAssetAtPath<TechNode>(TechWaterWheelPath);
            isNew = waterWheel == null;
            if (isNew) waterWheel = ScriptableObject.CreateInstance<TechNode>();
            waterWheel.id = "tech_water_wheel";
            waterWheel.displayName = "Guồng nước";
            waterWheel.cost = Amounts(knowledge, 20);
            waterWheel.prerequisites = new List<TechNode> { irrigation };
            waterWheel.unlockedBuildingIds = new List<string> { "water_wheel" };
            waterWheel.unlockedCropIds = new List<string>();
            waterWheel.grantOnUnlock = new List<ResourceAmount>();
            if (isNew) AssetDatabase.CreateAsset(waterWheel, TechWaterWheelPath);
            else EditorUtility.SetDirty(waterWheel);
        }

        // ─── Ao cá ───────────────────────────────────────────────────────────
        /// <summary>Ao cá (E3): khai thác như cây nhưng không cạn hẳn — cá sinh sôi lại 1 con / 20s, tối đa 8.</summary>
        private static GameObject BuildPond(ResourceTypeData fish)
        {
            var root = new GameObject("FishingPond");
            var node = root.AddComponent<ResourceNode>();
            SetPrivateField(node, "resourceType", fish);
            SetPrivateField(node, "amountRemaining", 8);
            SetPrivateField(node, "maxAmount", 8);
            SetPrivateField(node, "yieldPerHit", 1);
            SetPrivateField(node, "regenInterval", 20f);
            SetPrivateField(node, "actionName", "Đánh cá");
            SetPrivateField(node, "workRange", 1.7f); // đứng trên bờ
            AddObstacle(root, NavMeshObstacleShape.Capsule, new Vector3(0f, 0.2f, 0f), new Vector3(1.8f, 0.4f, 1.8f));
            SetPrivateField(root.AddComponent<WaterSource>(), "radius", 0.95f); // M5d: nguồn nước cho ruộng

            var t = root.transform;
            Part(t, "Shore", PrimitiveType.Cylinder, new Vector3(0f, 0.005f, 0f), new Vector3(2.3f, 0.01f, 2.1f), Mat("Sand", Palette.Hex(0xc9b27c)))
                .GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Part(t, "Water", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(1.9f, 0.01f, 1.7f), Mat("Water", Palette.Hex(0x3f7fb0), emission: 0.15f))
                .GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var reed = Mat("Reed", Palette.Hex(0x6a8f3a));
            foreach (var p in new[] { new Vector3(0.85f, 0f, 0.3f), new Vector3(0.9f, 0f, 0.15f), new Vector3(-0.8f, 0f, -0.45f), new Vector3(-0.7f, 0f, 0.55f) })
                Part(t, "Reed", PrimitiveType.Cylinder, p + new Vector3(0f, 0.18f, 0f), new Vector3(0.025f, 0.18f, 0.025f), reed, new Vector3(0f, 0f, p.x * 8f));
            Part(t, "Fish", PrimitiveType.Sphere, new Vector3(0.25f, 0.03f, -0.2f), new Vector3(0.18f, 0.03f, 0.07f), Mat("FishShadow", Palette.Hex(0x2c5a7d)), new Vector3(0f, 30f, 0f));
            return root;
        }

        // ─── Crop stages ─────────────────────────────────────────────────────
        private static readonly Vector3[] CropSpots =
        {
            new Vector3(-0.25f, 0f, -0.3f), new Vector3(0.25f, 0f, -0.3f),
            new Vector3(-0.25f, 0f, 0f), new Vector3(0.25f, 0f, 0f),
            new Vector3(-0.25f, 0f, 0.3f), new Vector3(0.25f, 0f, 0.3f),
        };

        private static CropData SaveCropData(string path, string id, string displayName, bool unlockedByDefault,
            float sprout, float mature, float wither, ResourceTypeData yieldType, int yieldAmount, FieldType field = FieldType.Dry)
        {
            var crop = AssetDatabase.LoadAssetAtPath<CropData>(path);
            bool isNew = crop == null;
            if (isNew) crop = ScriptableObject.CreateInstance<CropData>();
            crop.id = id;
            crop.fieldType = field;
            crop.displayName = displayName;
            crop.unlockedByDefault = unlockedByDefault;
            crop.timeToSprout = sprout;
            crop.timeToMature = mature;
            crop.witherTime = wither;
            crop.harvestYield = Amounts(yieldType, yieldAmount);
            if (isNew) AssetDatabase.CreateAsset(crop, path);
            else EditorUtility.SetDirty(crop);
            return crop;
        }

        private static void BuildCropModels(Foods foods, ResourceTypeData riceSeed, ResourceTypeData seedling, ResourceTypeData sheaf)
        {
            BuildBerryCrop(foods.berries);
            BuildRiceCrop(sheaf, seedling);
            BuildSeedlingCrop(riceSeed, seedling);
            BuildVegetableCrop(foods.vegetables);
        }

        /// <summary>
        /// Mạ (F3): gieo thóc giống trên ruộng mạ ngập nước → mạ lên dày → nhổ được 3 bó mạ. Mạ già quá thì hỏng.
        /// </summary>
        private static void BuildSeedlingCrop(ResourceTypeData riceSeed, ResourceTypeData seedling)
        {
            var crop = SaveCropData(SeedlingDataPath, "rice_seedling", "Mạ", false, sprout: 10f, mature: 20f, wither: 60f,
                seedling, 3, FieldType.Seedbed);
            crop.minWater = 0.3f;
            crop.plantCost = Amounts(riceSeed, 1);
            crop.harvestVerb = "Nhổ";
            var grainMat = Mat("SoakedSeed", Palette.Hex(0xd8c9a0));
            var shootMat = Mat("SeedlingShoot", Palette.Hex(0x9ed65a));
            var deadMat = Mat("SeedlingOld", Palette.Hex(0xb5a65a));

            // Thóc rải dày khắp ô (không theo hàng như cây trồng khác).
            crop.seedModel = SaveCropStage("Seedling_Seed", (t, p) =>
            {
                for (int i = 0; i < 3; i++)
                    Part(t, "Grain", PrimitiveType.Sphere, p + new Vector3((i - 1) * 0.07f, 0.05f, (i % 2) * 0.05f), Vector3.one * 0.035f, grainMat);
            });
            crop.sproutModel = SaveCropStage("Seedling_Sprout", (t, p) =>
            {
                for (int i = 0; i < 4; i++)
                    Part(t, "Shoot", PrimitiveType.Cylinder, p + new Vector3((i - 1.5f) * 0.05f, 0.07f, (i % 2) * 0.04f), new Vector3(0.01f, 0.04f, 0.01f), shootMat);
            });
            // Mạ lên dày như thảm (khác hẳn khóm lúa cấy thưa theo hàng).
            crop.matureModel = SaveCropStage("Seedling_Mature", (t, p) =>
            {
                for (int i = 0; i < 12; i++)
                    Part(t, "Shoot", PrimitiveType.Cylinder, p + new Vector3((i % 4 - 1.5f) * 0.05f, 0.1f, (i / 4 - 1) * 0.06f),
                        new Vector3(0.012f, 0.065f, 0.012f), shootMat, new Vector3((i / 4 - 1) * 6f, 0f, (i % 4 - 1.5f) * 6f));
            });
            crop.witheredModel = SaveCropStage("Seedling_Old", (t, p) =>
            {
                for (int i = 0; i < 12; i++)
                    Part(t, "Old", PrimitiveType.Cylinder, p + new Vector3((i % 4 - 1.5f) * 0.05f, 0.09f, (i / 4 - 1) * 0.06f),
                        new Vector3(0.012f, 0.065f, 0.012f), deadMat, new Vector3(0f, 0f, 25f));
            });
            EditorUtility.SetDirty(crop);
        }

        /// <summary>
        /// Lúa: cấy bằng mạ (1 bó mạ / ruộng), lâu lớn nhất. Gặt về là bó lúa — phải phơi, tuốt, giã ở cối giã
        /// (4 bó → 8 thóc → gạo, chừa thóc giống).
        /// </summary>
        private static void BuildRiceCrop(ResourceTypeData sheaf, ResourceTypeData seedling)
        {
            var crop = SaveCropData(RiceDataPath, "rice", "Lúa", false, sprout: 20f, mature: 40f, wither: 60f, sheaf, 4, FieldType.Paddy);
            crop.minWater = 0.3f; // lúa cần ruộng còn ngập nước mới lớn
            crop.plantCost = Amounts(seedling, 1);
            crop.harvestVerb = "Gặt";
            var stalkMat = Mat("RiceStalk", Palette.Hex(0x7fb24a));
            var youngMat = Mat("SeedlingShoot", Palette.Hex(0x9ed65a));
            var grainMat = Mat("RiceGrain", Palette.Hex(0xe2c25a), emission: 0.1f);
            var deadMat = Mat("RiceWithered", Palette.Hex(0x8a7a5a));

            // Vừa cấy: từng khóm mạ nhỏ cắm theo hàng.
            crop.seedModel = SaveCropStage("Rice_Seed", (t, p) =>
            {
                for (int i = -1; i <= 1; i += 2)
                    Part(t, "Seedling", PrimitiveType.Cylinder, p + new Vector3(i * 0.015f, 0.08f, 0f), new Vector3(0.01f, 0.045f, 0.01f), youngMat, new Vector3(0f, 0f, i * 10f));
            });
            crop.sproutModel = SaveCropStage("Rice_Sprout", (t, p) =>
            {
                for (int i = -1; i <= 1; i++)
                    Part(t, "Blade", PrimitiveType.Cylinder, p + new Vector3(i * 0.04f, 0.08f, 0f), new Vector3(0.015f, 0.08f, 0.015f), stalkMat, new Vector3(0f, 0f, i * 12f));
            });
            crop.matureModel = SaveCropStage("Rice_Mature", (t, p) =>
            {
                for (int i = -1; i <= 1; i++)
                {
                    Part(t, "Stalk", PrimitiveType.Cylinder, p + new Vector3(i * 0.05f, 0.14f, 0f), new Vector3(0.018f, 0.14f, 0.018f), stalkMat, new Vector3(0f, 0f, i * 10f));
                    Part(t, "Ear", PrimitiveType.Capsule, p + new Vector3(i * 0.07f, 0.3f, 0.02f), new Vector3(0.04f, 0.06f, 0.04f), grainMat, new Vector3(25f, 0f, i * 15f));
                }
            });
            crop.witheredModel = SaveCropStage("Rice_Withered", (t, p) =>
                Part(t, "Dead", PrimitiveType.Cylinder, p + new Vector3(0f, 0.05f, 0f), new Vector3(0.12f, 0.05f, 0.12f), deadMat, new Vector3(0f, 0f, 30f)));
            EditorUtility.SetDirty(crop);
        }

        /// <summary>Rau (E3): lớn nhanh, thu ít — mở cùng công nghệ Nông nghiệp.</summary>
        private static void BuildVegetableCrop(ResourceTypeData vegetables)
        {
            var crop = SaveCropData(VegetableDataPath, "vegetable", "Rau", false, sprout: 8f, mature: 16f, wither: 30f, vegetables, 2);
            var seedMat = Mat("VegSeed", Palette.Hex(0x4a3420));
            var leafMat = Mat("VegLeaf", Palette.Hex(0x5fbf4a));
            var headMat = Mat("VegHead", Palette.Hex(0x8fd46a));
            var deadMat = Mat("VegWithered", Palette.Hex(0x7a6a45));

            crop.seedModel = SaveCropStage("Vegetable_Seed", (t, p) =>
                Part(t, "Seed", PrimitiveType.Sphere, p + new Vector3(0f, 0.02f, 0f), Vector3.one * 0.06f, seedMat));
            crop.sproutModel = SaveCropStage("Vegetable_Sprout", (t, p) =>
                Part(t, "Leaf", PrimitiveType.Sphere, p + new Vector3(0f, 0.04f, 0f), new Vector3(0.12f, 0.05f, 0.12f), leafMat));
            crop.matureModel = SaveCropStage("Vegetable_Mature", (t, p) =>
            {
                Part(t, "Leaves", PrimitiveType.Sphere, p + new Vector3(0f, 0.06f, 0f), new Vector3(0.24f, 0.09f, 0.24f), leafMat);
                Part(t, "Head", PrimitiveType.Sphere, p + new Vector3(0f, 0.11f, 0f), Vector3.one * 0.15f, headMat);
            });
            crop.witheredModel = SaveCropStage("Vegetable_Withered", (t, p) =>
                Part(t, "Dead", PrimitiveType.Sphere, p + new Vector3(0f, 0.03f, 0f), new Vector3(0.2f, 0.05f, 0.2f), deadMat));
            EditorUtility.SetDirty(crop);
        }

        private static void BuildBerryCrop(ResourceTypeData berries)
        {
            var crop = AssetDatabase.LoadAssetAtPath<CropData>(BerryDataPath);
            if (crop == null)
            {
                Debug.LogError($"[GameContentBuilder] Khong tim thay {BerryDataPath}");
                return;
            }
            crop.displayName = "Cây mọng";
            crop.harvestYield = Amounts(berries, 3); // giữ nguyên thời gian lớn đã cân bằng trong asset

            var seedMat = Mat("BerrySeed", Palette.Hex(0x3a2412));
            var sproutMat = Mat("BerrySprout", Palette.Hex(0x6fae3a));
            var bushMat = Mat("BerryBush", Palette.Hex(0x3f8a36));
            var fruitMat = Mat("BerryFruit", Palette.Hex(0xc4193a), emission: 0.15f);
            var witheredMat = Mat("BerryWithered", Palette.Hex(0x6e5e4a));

            crop.seedModel = SaveCropStage("Berry_Seed", (t, p) =>
                Part(t, "Seed", PrimitiveType.Sphere, p + new Vector3(0f, 0.02f, 0f), Vector3.one * 0.08f, seedMat));

            crop.sproutModel = SaveCropStage("Berry_Sprout", (t, p) =>
            {
                Cone(t, "LeafL", 4, p + new Vector3(-0.03f, 0f, 0f), new Vector3(0.09f, 0.22f, 0.09f), sproutMat, new Vector3(0f, 0f, 22f));
                Cone(t, "LeafR", 4, p + new Vector3(0.03f, 0f, 0f), new Vector3(0.09f, 0.22f, 0.09f), sproutMat, new Vector3(0f, 0f, -22f));
            });

            crop.matureModel = SaveCropStage("Berry_Mature", (t, p) =>
            {
                Part(t, "Bush", PrimitiveType.Sphere, p + new Vector3(0f, 0.14f, 0f), Vector3.one * 0.28f, bushMat);
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI / 2f + p.x * 5f;
                    var offset = new Vector3(Mathf.Cos(a) * 0.12f, 0.13f + (i % 2) * 0.06f, Mathf.Sin(a) * 0.12f);
                    Part(t, $"Berry{i}", PrimitiveType.Sphere, p + offset, Vector3.one * 0.07f, fruitMat);
                }
            });

            crop.witheredModel = SaveCropStage("Berry_Withered", (t, p) =>
                Part(t, "Dead", PrimitiveType.Sphere, p + new Vector3(0f, 0.06f, 0f), new Vector3(0.28f, 0.14f, 0.28f), witheredMat));

            EditorUtility.SetDirty(crop);
        }

        private static GameObject SaveCropStage(string name, System.Action<Transform, Vector3> buildPlant)
        {
            var root = new GameObject(name);
            foreach (var spot in CropSpots)
                buildPlant(root.transform, spot);
            return SavePrefab(root, $"{CropPrefabFolder}/{name}.prefab");
        }
    }
}
