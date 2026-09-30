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
        private const string BoarDataPath = "Assets/_Data/AnimalData_WildBoar.asset";
        private const string BerryDataPath = "Assets/_Data/CropData_Berry.asset";

        private const string HutPrefabPath = "Assets/Prefabs/Buildings/Hut.prefab";
        private const string StoragePrefabPath = "Assets/Prefabs/Buildings/Storage.prefab";
        private const string BoarPrefabPath = "Assets/Prefabs/Animals/WildBoar.prefab";
        public const string TreePrefabPath = "Assets/Prefabs/Resources/Tree.prefab";
        private const string CropPrefabFolder = "Assets/Prefabs/Crops";
        public const string VillagerPrefabPath = "Assets/Prefabs/Npcs/Villager.prefab";

        // Thứ tự cố định: dân làng, nông dân, thợ săn, trinh sát.
        public static readonly string[] ProfessionIds = { "villager", "farmer", "hunter", "scout" };
        public static string ProfessionAssetPath(string id) => $"Assets/_Data/Profession_{id}.asset";

        [MenuItem("Tools/Prehistoric/Build Missing Content (Buildings + Animal)")]
        public static void Build()
        {
            if (!ConfirmOverwrite("Tạo lại prefab placeholder?",
                    "Prefab lều/kho/cây/heo rừng đã tồn tại. Tạo lại sẽ ghi đè mọi chỉnh sửa hoặc model thật bạn đã thay vào.",
                    HutPrefabPath, StoragePrefabPath, BoarPrefabPath, TreePrefabPath, VillagerPrefabPath))
                return;

            var wood = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(WoodAssetPath);
            var food = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(FoodAssetPath);
            if (wood == null || food == null)
            {
                Debug.LogError($"[GameContentBuilder] Khong tim thay {WoodAssetPath} hoac {FoodAssetPath}. Dung lai.");
                return;
            }

            AssignBuildingPrefab(HutDataPath, SavePrefab(BuildHut(), HutPrefabPath));
            AssignBuildingPrefab(StorageDataPath, SavePrefab(BuildStorage(), StoragePrefabPath));
            SavePrefab(BuildTree(wood), TreePrefabPath);
            BuildWildBoarContent(food);
            BuildCropModels();
            ProfessionData[] professions = BuildProfessions();
            SavePrefab(BuildVillager(professions), VillagerPrefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameContentBuilder] Da tao xong prefab 3D: Hut, Storage, Tree, WildBoar, Villager, 4 nghe va model giai doan cho CropData_Berry");
        }

        private static void AssignBuildingPrefab(string dataAssetPath, GameObject prefab)
        {
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(dataAssetPath);
            if (data == null)
            {
                Debug.LogError($"[GameContentBuilder] Khong tim thay {dataAssetPath}");
                return;
            }
            data.prefab = prefab;
            EditorUtility.SetDirty(data);
        }

        // ─── Buildings ───────────────────────────────────────────────────────
        /// <summary>Máu + vật cản NavMesh (NPC đi vòng qua công trình đặt lúc chơi).</summary>
        private static void AddBuildingComponents(GameObject root, Vector3 center, Vector3 size, float maxHealth)
        {
            var col = root.AddComponent<BoxCollider>();
            col.center = center;
            col.size = size;
            root.AddComponent<BuildingInstance>();
            SetPrivateField(root.AddComponent<HealthComponent>(), "maxHealth", maxHealth);
            AddObstacle(root, NavMeshObstacleShape.Box, center, size);
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

        private static GameObject BuildHut()
        {
            var root = new GameObject("Hut");
            AddBuildingComponents(root, new Vector3(0f, 0.55f, 0f), new Vector3(0.9f, 1.1f, 0.9f), 200f);

            var t = root.transform;
            Cone(t, "Cover", 9, Vector3.zero, new Vector3(0.92f, 1.1f, 0.92f), Palette.Hide);
            Part(t, "Band", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(0.58f, 0.04f, 0.58f), Palette.Plank);
            Part(t, "Door", PrimitiveType.Cube, new Vector3(0f, 0.18f, -0.37f), new Vector3(0.22f, 0.36f, 0.05f), Palette.DarkWood, new Vector3(23f, 0f, 0f));

            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 20f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Part(t, $"Pole{i}", PrimitiveType.Cylinder, new Vector3(0f, 1.15f, 0f) + dir * 0.05f,
                    new Vector3(0.035f, 0.22f, 0.035f), Palette.Wood, new Vector3(20f, a, 0f));
            }
            return root;
        }

        private static GameObject BuildStorage()
        {
            var root = new GameObject("Storage");
            AddBuildingComponents(root, new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 1f, 0.9f), 300f);

            var t = root.transform;
            Part(t, "Base", PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(0.84f, 0.5f, 0.84f), Palette.Plank);
            Cone(t, "Roof", 4, new Vector3(0f, 0.5f, 0f), new Vector3(1.44f, 0.45f, 1.44f), Palette.Thatch, new Vector3(0f, 45f, 0f));
            Part(t, "Door", PrimitiveType.Cube, new Vector3(0f, 0.17f, -0.43f), new Vector3(0.26f, 0.34f, 0.03f), Palette.DarkWood);

            var logs = new[] { new Vector3(0.52f, 0.05f, -0.07f), new Vector3(0.52f, 0.05f, 0.07f), new Vector3(0.52f, 0.14f, 0f) };
            for (int i = 0; i < logs.Length; i++)
                Part(t, $"Log{i}", PrimitiveType.Cylinder, logs[i], new Vector3(0.1f, 0.25f, 0.1f), Palette.Wood, new Vector3(90f, 0f, 0f));
            return root;
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

        // ─── Wild boar ───────────────────────────────────────────────────────
        private static void BuildWildBoarContent(ResourceTypeData food)
        {
            var root = new GameObject("WildBoar");
            var controller = root.AddComponent<AnimalController>();
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
            var collar = Part(t, "TamedCollar", PrimitiveType.Cylinder, new Vector3(0f, 0.42f, 0.27f), new Vector3(0.44f, 0.03f, 0.44f), Palette.Rope, new Vector3(90f, 0f, 0f));
            var productIcon = Part(t, "ProductIcon", PrimitiveType.Sphere, new Vector3(0f, 1f, 0f), Vector3.one * 0.2f, Palette.ProductGlow);
            collar.SetActive(false); // heo mới là heo hoang — AnimalController bật lên khi thuần hóa
            productIcon.SetActive(false);

            SetPrivateField(controller, "bodyRenderers", new Renderer[]
            {
                body.GetComponent<Renderer>(), head.GetComponent<Renderer>(), earL.GetComponent<Renderer>(), earR.GetComponent<Renderer>()
            });
            SetPrivateField(controller, "tamedIndicator", collar);
            SetPrivateField(controller, "productIndicator", productIcon);

            GameObject prefab = SavePrefab(root, BoarPrefabPath);

            var data = AssetDatabase.LoadAssetAtPath<AnimalData>(BoarDataPath);
            bool isNew = data == null;
            if (isNew) data = ScriptableObject.CreateInstance<AnimalData>();

            data.id = "wild_boar";
            data.displayName = "Heo rung";
            data.prefab = prefab;
            data.hungerDecayInterval = 20f;
            data.hungerThresholdForNeeds = 50f;
            data.feedCost = new List<ResourceAmount> { new ResourceAmount { type = food, amount = 3 } };
            data.feedingsToTame = 2;
            data.reproductionInterval = 45f;
            data.products = new List<ResourceAmount> { new ResourceAmount { type = food, amount = 2 } };
            data.productionInterval = 15f;

            if (isNew)
                AssetDatabase.CreateAsset(data, BoarDataPath);
            else
                EditorUtility.SetDirty(data);
        }

        // ─── Professions + villager ──────────────────────────────────────────
        private static ProfessionData[] BuildProfessions() => new[]
        {
            SaveProfession("villager", "Dân làng", 0x8a5a2b, speed: 3f, health: 100f, damage: 5f, range: 1.2f, vision: 8f,
                NpcCapability.Gather | NpcCapability.Build | NpcCapability.Fight),
            SaveProfession("farmer", "Nông dân", 0x7a9a3a, speed: 2.6f, health: 90f, damage: 3f, range: 1.2f, vision: 7f,
                NpcCapability.Farm | NpcCapability.TendAnimals | NpcCapability.Gather),
            SaveProfession("hunter", "Thợ săn", 0x5a4030, speed: 3.2f, health: 110f, damage: 14f, range: 5f, vision: 10f,
                NpcCapability.Hunt | NpcCapability.Fight | NpcCapability.Gather),
            SaveProfession("scout", "Trinh sát", 0x4a7fa8, speed: 4.2f, health: 70f, damage: 6f, range: 1.2f, vision: 16f,
                NpcCapability.Scout | NpcCapability.Fight),
        };

        private static ProfessionData SaveProfession(string id, string displayName, int color, float speed, float health,
            float damage, float range, float vision, NpcCapability capabilities)
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
            root.AddComponent<HealthComponent>();
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

            // Mặc định trong prefab: dân làng nam — chỉ bật tóc nam + rìu (NpcController tự đổi theo nghề/giới tính).
            femaleHair.SetActive(false);
            for (int i = 1; i < tools.Count; i++) tools[i].tool.SetActive(false);

            SetPrivateField(npc, "profession", professions[0]);
            SetPrivateField(npc, "tunicRenderer", tunic.GetComponent<Renderer>());
            SetPrivateField(npc, "maleHair", maleHair);
            SetPrivateField(npc, "femaleHair", femaleHair);
            SetPrivateField(npc, "tools", tools);
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

        // ─── Crop stages ─────────────────────────────────────────────────────
        private static readonly Vector3[] CropSpots =
        {
            new Vector3(-0.25f, 0f, -0.3f), new Vector3(0.25f, 0f, -0.3f),
            new Vector3(-0.25f, 0f, 0f), new Vector3(0.25f, 0f, 0f),
            new Vector3(-0.25f, 0f, 0.3f), new Vector3(0.25f, 0f, 0.3f),
        };

        private static void BuildCropModels()
        {
            var crop = AssetDatabase.LoadAssetAtPath<CropData>(BerryDataPath);
            if (crop == null)
            {
                Debug.LogError($"[GameContentBuilder] Khong tim thay {BerryDataPath}");
                return;
            }

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
