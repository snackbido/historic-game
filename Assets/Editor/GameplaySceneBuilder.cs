using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static PrehistoricTribe.EditorTools.EditorBuildUtils;

namespace PrehistoricTribe.EditorTools
{
    /// <summary>
    /// Dựng scene 2.5D Gameplay.unity bằng code (camera phối cảnh nhìn nghiêng, thế giới trên mặt phẳng XZ).
    /// Chạy qua menu Tools/Prehistoric/Build Gameplay Scene, hoặc batch mode
    /// (-executeMethod PrehistoricTribe.EditorTools.GameplaySceneBuilder.BuildAll).
    /// Scene đã có sẵn thì Editor sẽ hỏi trước khi ghi đè (tránh mất phần chỉnh tay).
    /// </summary>
    public static class GameplaySceneBuilder
    {
        private const string ScenePath = "Assets/_Scenes/Gameplay.unity";
        private const string WoodAssetPath = "Assets/_Data/ResourceType_Wood.asset";
        private const string FoodAssetPath = "Assets/_Data/ResourceType_Food.asset";
        private const string KnowledgeAssetPath = "Assets/_Data/ResourceType_Knowledge.asset";
        private const string HutDataPath = "Assets/_Data/BuildingData_Hut.asset";
        private const string StorageDataPath = "Assets/_Data/BuildingData_Storage.asset";
        private const string BerryDataPath = "Assets/_Data/CropData_Berry.asset";
        private const string TechFarmingPath = "Assets/_Data/TechNode_Farming.asset";
        private const string BoarDataPath = "Assets/_Data/AnimalData_WildBoar.asset";
        private const string ButtonPrefabPath = "Assets/Prefabs/Button.prefab";

        private const string NavMeshAssetPath = "Assets/_Scenes/Gameplay_NavMesh.asset";

        private static readonly Color SkyColor = new Color(0.81f, 0.89f, 0.9f);

        // Dân làng ban đầu (quyết định 2026-09-30: 3–4 người, có cả nam lẫn nữ, mỗi người một nghề).
        private static readonly (string name, Gender gender, string professionId, Vector2 position)[] StartingVillagers =
        {
            ("Ka", Gender.Male, "villager", new Vector2(0.2f, -3.2f)),
            ("Mây", Gender.Female, "farmer", new Vector2(-1f, -3.6f)),
            ("Đá", Gender.Male, "hunter", new Vector2(1.4f, -3.8f)),
            ("Suối", Gender.Female, "scout", new Vector2(0.4f, -4.6f)),
        };

        // Cùng bố cục với bản web (web/src/data/gameData.js): (x, z) trên mặt đất.
        private static readonly Vector2[] TreePositions =
        {
            new Vector2(2f, 1f), new Vector2(4.5f, 2.5f), new Vector2(-1.5f, 3f), new Vector2(6f, -0.5f),
            new Vector2(-5f, 2f), new Vector2(1f, 4.5f), new Vector2(7f, 3.5f), new Vector2(-6.5f, -3.5f),
        };

        [MenuItem("Tools/Prehistoric/Build All (Content + Scene)")]
        public static void BuildAll()
        {
            GameContentBuilder.Build();
            Build();
        }

        [MenuItem("Tools/Prehistoric/Build Gameplay Scene")]
        public static void Build()
        {
            if (!ConfirmOverwrite("Dựng lại scene Gameplay?",
                    $"{ScenePath} đã tồn tại. Dựng lại sẽ xóa mọi thứ bạn đã chỉnh tay trong scene này.",
                    ScenePath))
                return;

            var wood = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(WoodAssetPath);
            var food = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(FoodAssetPath);
            var knowledge = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(KnowledgeAssetPath);
            var hut = AssetDatabase.LoadAssetAtPath<BuildingData>(HutDataPath);
            var storage = AssetDatabase.LoadAssetAtPath<BuildingData>(StorageDataPath);
            var berry = AssetDatabase.LoadAssetAtPath<CropData>(BerryDataPath);
            var techFarming = AssetDatabase.LoadAssetAtPath<TechNode>(TechFarmingPath);
            var boarData = AssetDatabase.LoadAssetAtPath<AnimalData>(BoarDataPath);
            var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameContentBuilder.TreePrefabPath);
            var villagerPrefab = AssetDatabase.LoadAssetAtPath<NpcController>(GameContentBuilder.VillagerPrefabPath);
            var professions = new List<ProfessionData>();
            foreach (string id in GameContentBuilder.ProfessionIds)
                professions.Add(AssetDatabase.LoadAssetAtPath<ProfessionData>(GameContentBuilder.ProfessionAssetPath(id)));
            var buttonPrefab = AssetDatabase.LoadAssetAtPath<Button>(ButtonPrefabPath);
            // M5b/E3: lương thực nhiều loại, cây lúa/rau, dê núi, công nghệ trồng lúa.
            var rice = AssetDatabase.LoadAssetAtPath<CropData>(GameContentBuilder.RiceDataPath);
            var vegetable = AssetDatabase.LoadAssetAtPath<CropData>(GameContentBuilder.VegetableDataPath);
            var techRice = AssetDatabase.LoadAssetAtPath<TechNode>(GameContentBuilder.TechRicePath);
            var goatData = AssetDatabase.LoadAssetAtPath<AnimalData>(GameContentBuilder.GoatDataPath);
            var foodTypes = new List<ResourceTypeData>();
            foreach (string name in GameContentBuilder.FoodAssetNames)
                foodTypes.Add(AssetDatabase.LoadAssetAtPath<ResourceTypeData>(GameContentBuilder.ResourcePath(name)));
            // M5d/F3: mạ (ươm ở ruộng mạ) + thóc giống.
            var seedlingCrop = AssetDatabase.LoadAssetAtPath<CropData>(GameContentBuilder.SeedlingDataPath);
            var riceSeed = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(GameContentBuilder.ResourcePath("RiceSeed"));
            var seedling = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(GameContentBuilder.ResourcePath("Seedling"));
            var manure = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(GameContentBuilder.ResourcePath("Manure"));
            var seedbed = AssetDatabase.LoadAssetAtPath<BuildingData>(GameContentBuilder.SeedbedDataPath);
            var crops = new List<CropData> { berry, vegetable, rice, seedlingCrop };
            // M5d: ruộng do người chơi xây.
            var dryField = AssetDatabase.LoadAssetAtPath<BuildingData>(GameContentBuilder.DryFieldDataPath);
            var paddyField = AssetDatabase.LoadAssetAtPath<BuildingData>(GameContentBuilder.PaddyFieldDataPath);
            var well = AssetDatabase.LoadAssetAtPath<BuildingData>(GameContentBuilder.WellDataPath);
            var mortar = AssetDatabase.LoadAssetAtPath<BuildingData>(GameContentBuilder.MortarDataPath);
            var sheaf = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(GameContentBuilder.ResourcePath("RiceSheaf"));
            var canal = AssetDatabase.LoadAssetAtPath<BuildingData>(GameContentBuilder.CanalDataPath);
            var techIrrigation = AssetDatabase.LoadAssetAtPath<TechNode>(GameContentBuilder.TechIrrigationPath);
            var waterWheel = AssetDatabase.LoadAssetAtPath<BuildingData>(GameContentBuilder.WaterWheelDataPath);
            var techWaterWheel = AssetDatabase.LoadAssetAtPath<TechNode>(GameContentBuilder.TechWaterWheelPath);
            var buildings = new List<BuildingData> { hut, storage, dryField, seedbed, paddyField, well, canal, waterWheel, mortar };
            var techs = new List<TechNode> { techFarming, techRice, techIrrigation, techWaterWheel };

            if (wood == null || food == null || knowledge == null || hut == null || storage == null ||
                berry == null || techFarming == null || boarData == null || buttonPrefab == null ||
                rice == null || vegetable == null || techRice == null || goatData == null || foodTypes.Contains(null) ||
                dryField == null || paddyField == null || well == null ||
                seedlingCrop == null || riceSeed == null || seedling == null || seedbed == null || manure == null || mortar == null || sheaf == null || canal == null || techIrrigation == null || waterWheel == null || techWaterWheel == null)
            {
                Debug.LogError("[GameplaySceneBuilder] Thieu asset can thiet. Chay 'Tools/Prehistoric/Build Missing Content' truoc (hoac dung 'Build All').");
                return;
            }

            if (hut.prefab == null || storage.prefab == null || boarData.prefab == null || treePrefab == null ||
                villagerPrefab == null || professions.Contains(null))
            {
                Debug.LogError("[GameplaySceneBuilder] Chua co prefab 3D. Chay 'Tools/Prehistoric/Build Missing Content' truoc (hoac dung 'Build All').");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Light sun = SetupLighting();
            var environment = new GameObject("Environment").transform;
            CreateGround(environment);
            CreateDecorForest(environment);
            BakeNavMesh(environment.gameObject);

            var rmGO = new GameObject("ResourceManager");
            var rm = rmGO.AddComponent<ResourceManager>();
            var knownTypes = new List<ResourceTypeData> { wood, food, knowledge };
            knownTypes.AddRange(foodTypes);
            knownTypes.Add(riceSeed);
            knownTypes.Add(seedling);
            knownTypes.Add(manure);
            knownTypes.Add(sheaf);
            SetPrivateField(rm, "knownResourceTypes", knownTypes);

            var player = CreatePlayer();
            Camera mainCamera = CreateCamera(player.transform);

            // Milestone 5c: ngày/đêm điều khiển mặt trời + màu trời; đống lửa trại giữa trại.
            var cycle = new GameObject("DayNightCycle").AddComponent<DayNightCycle>();
            SetPrivateField(cycle, "sun", sun);
            SetPrivateField(cycle, "sceneCamera", mainCamera);
            CreateCampfire(new Vector3(0f, 0f, -2f));

            var resources = new GameObject("ResourceNodes").transform;
            for (int i = 0; i < TreePositions.Length; i++)
            {
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, resources);
                tree.name = i == 0 ? "Tree" : $"Tree_{i}";
                tree.transform.position = new Vector3(TreePositions[i].x, 0f, TreePositions[i].y);
                tree.transform.rotation = Quaternion.Euler(0f, i * 47f, 0f);
            }

            // Ao cá phía bắc trại (nạp prefab sau khi bake NavMesh để tham chiếu còn hiệu lực).
            var pondPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameContentBuilder.PondPrefabPath);
            var pond = (GameObject)PrefabUtility.InstantiatePrefab(pondPrefab, resources);
            pond.name = "FishingPond";
            pond.transform.position = new Vector3(4.5f, 0f, 6f);

            var gridGO = new GameObject("Grid");
            var grid = gridGO.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            grid.cellSwizzle = GridLayout.CellSwizzle.XZY; // ô (x, y) ↔ thế giới (x, z)

            var previewGO = Part(null, "PlacementPreview", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f),
                new Vector3(0.98f, 0.04f, 0.98f), Palette.Preview);
            previewGO.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            previewGO.SetActive(false); // BuildingPlacer tự bật khi đang chọn công trình

            var placerGO = new GameObject("BuildingPlacer");
            var placer = placerGO.AddComponent<BuildingPlacer>();
            SetPrivateField(placer, "grid", grid);
            SetPrivateField(placer, "availableBuildings", buildings);
            SetPrivateField(placer, "placementPreview", previewGO);

            var farmManager = new GameObject("FarmManager").AddComponent<FarmManager>();
            SetPrivateField(farmManager, "knownCrops", crops);
            SetPrivateField(farmManager, "fertilizerCost", new List<ResourceAmount> { new ResourceAmount { type = manure, amount = 1 } });
            var tamingSystem = new GameObject("TamingSystem").AddComponent<TamingSystem>();
            SetPrivateField(tamingSystem, "knownAnimals", new List<AnimalData> { boarData, goatData });

            CreateInteractionHighlight(player.GetComponent<PlayerInteraction>());

            CreateFarmPlot("FarmPlot_1", new Vector3(-2f, 0f, -1.5f));
            CreateFarmPlot("FarmPlot_2", new Vector3(-3.2f, 0f, -1.5f));

            var boarInstance = (GameObject)PrefabUtility.InstantiatePrefab(boarData.prefab);
            boarInstance.name = "WildBoar";
            boarInstance.transform.SetPositionAndRotation(new Vector3(3f, 0f, -1.5f), Quaternion.Euler(0f, 200f, 0f));
            SetPrivateField(boarInstance.GetComponent<AnimalController>(), "data", boarData);

            var goatInstance = (GameObject)PrefabUtility.InstantiatePrefab(goatData.prefab);
            goatInstance.name = "Goat";
            goatInstance.transform.SetPositionAndRotation(new Vector3(-5f, 0f, -6.5f), Quaternion.Euler(0f, 160f, 0f));
            SetPrivateField(goatInstance.GetComponent<AnimalController>(), "data", goatData);

            // Nạp lại: tạo asset NavMesh ở trên có thể làm tham chiếu prefab cũ mất hiệu lực.
            villagerPrefab = AssetDatabase.LoadAssetAtPath<NpcController>(GameContentBuilder.VillagerPrefabPath);
            CreateVillagers(villagerPrefab, professions);
            new GameObject("SelectionManager").AddComponent<SelectionManager>();
            var npcManager = new GameObject("NpcManager").AddComponent<NpcManager>();
            SetPrivateField(npcManager, "npcPrefab", villagerPrefab);
            SetPrivateField(npcManager, "knownProfessions", professions);
            SetPrivateField(npcManager, "birthCost", new List<ResourceAmount> { new ResourceAmount { type = food, amount = 5 } });
            SetPrivateField(npcManager, "mealCost", new List<ResourceAmount> { new ResourceAmount { type = food, amount = 1 } });
            SetPrivateField(npcManager, "adultProfession", professions.Find(p => p.id == "villager"));
            SetPrivateField(npcManager, "maleNames", new List<string> { "Bờm", "Tùng", "Sấm", "Lửa", "Núi", "Gió", "Cọ", "Hổ" });
            SetPrivateField(npcManager, "femaleNames", new List<string> { "Hoa", "Sương", "Trăng", "Mưa", "Lá", "Nắng", "Mơ", "Sao" });

            CreateWolfDen();

            var techManagerGO = new GameObject("TechManager");
            var techManager = techManagerGO.AddComponent<TechManager>();
            SetPrivateField(techManager, "knowledgeResource", knowledge);
            SetPrivateField(techManager, "knowledgeGenerationInterval", 6f);
            SetPrivateField(techManager, "allTechs", techs);

            var gameManagerGO = new GameObject("GameManager");
            var gameManager = gameManagerGO.AddComponent<GameManager>();
            SetPrivateField(gameManager, "player", player.GetComponent<PlayerController>());
            SetPrivateField(gameManager, "resourceManager", rm);
            SetPrivateField(gameManager, "buildingPlacer", placer);
            gameManagerGO.AddComponent<SaveLoadHotkeys>();

            // Nạp lại: tạo asset NavMesh phía trên làm tham chiếu prefab (component) đã nạp trước đó mất hiệu lực.
            buttonPrefab = AssetDatabase.LoadAssetAtPath<Button>(ButtonPrefabPath);
            CreateUI(buttonPrefab, wood, food, knowledge, new[] { sheaf, riceSeed, seedling, manure }, buildings, crops, techs, player.GetComponent<PlayerInteraction>());

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GameplaySceneBuilder] Da tao scene 2.5D tai {ScenePath}");
        }

        // ─── World ───────────────────────────────────────────────────────────
        private static Light SetupLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.4f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = SkyColor;
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 55f;

            var sunGO = new GameObject("Sun");
            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.9f, 0.76f);
            sun.intensity = 1f;
            sun.shadows = LightShadows.Soft;
            sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            return sun;
        }

        /// <summary>Đống lửa trại: vòng đá + củi + ngọn lửa phát sáng + đèn điểm màu cam (chỉ bật khi tối).</summary>
        private static void CreateCampfire(Vector3 position)
        {
            var root = new GameObject("Campfire");
            root.transform.position = position;
            var t = root.transform;

            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                Part(t, $"Stone{i}", PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.32f, 0.05f, Mathf.Sin(a) * 0.32f),
                    new Vector3(0.16f, 0.1f, 0.14f), Palette.Stone);
            }
            Part(t, "LogA", PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(0.08f, 0.22f, 0.08f), Palette.Wood, new Vector3(90f, 30f, 0f));
            Part(t, "LogB", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(0.08f, 0.22f, 0.08f), Palette.Wood, new Vector3(90f, -50f, 0f));

            var flames = new GameObject("Flames");
            flames.transform.SetParent(t, false);
            var flameMat = Mat("Flame", Palette.Hex(0xff8a2a), emission: 1.5f);
            var coreMat = Mat("FlameCore", Palette.Hex(0xffd36a), emission: 1.5f);
            foreach (var renderer in new[]
            {
                Cone(flames.transform, "Flame", 6, new Vector3(0f, 0.08f, 0f), new Vector3(0.3f, 0.45f, 0.3f), flameMat).GetComponent<Renderer>(),
                Cone(flames.transform, "Core", 5, new Vector3(0f, 0.08f, 0f), new Vector3(0.16f, 0.3f, 0.16f), coreMat).GetComponent<Renderer>(),
            })
                renderer.shadowCastingMode = ShadowCastingMode.Off;

            var lightGO = new GameObject("FireLight");
            lightGO.transform.SetParent(t, false);
            lightGO.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var fireLight = lightGO.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.6f, 0.3f);
            fireLight.range = 9f;
            fireLight.intensity = 2.5f;
            fireLight.shadows = LightShadows.None;

            var obstacle = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Capsule;
            obstacle.radius = 0.4f;
            obstacle.height = 0.4f;
            obstacle.carving = true;

            var campfire = root.AddComponent<Campfire>();
            SetPrivateField(campfire, "fireLight", fireLight);
            SetPrivateField(campfire, "flames", flames);
        }

        private static void CreateGround(Transform parent)
        {
            // Plane mặc định 10×10 → scale 6 = 60×60. Không cần collider: chuột raycast vào mặt phẳng toán học.
            var ground = Part(parent, "Ground", PrimitiveType.Plane, Vector3.zero, new Vector3(6f, 1f, 6f), Palette.Grass);
            ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            var camp = Part(parent, "CampGround", PrimitiveType.Cylinder, new Vector3(0f, -0.045f, 0f),
                new Vector3(9f, 0.05f, 7.5f), Mat("CampGround", Palette.Hex(0x8a7a50)));
            camp.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Rừng trang trí bao quanh khu chơi — chỉ hình ảnh, không thu hoạch được.</summary>
        private static void CreateDecorForest(Transform parent)
        {
            var forest = new GameObject("DecorForest").transform;
            forest.SetParent(parent, false);
            var random = new System.Random(1987);

            int placed = 0;
            while (placed < 70)
            {
                float x = (float)(random.NextDouble() - 0.5) * 44f;
                float z = (float)(random.NextDouble() - 0.5) * 44f;
                if (Mathf.Abs(x) < 12f && Mathf.Abs(z) < 12f) continue;

                var tree = new GameObject($"DecorTree_{placed}").transform;
                tree.SetParent(forest, false);
                tree.SetPositionAndRotation(new Vector3(x, 0f, z), Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
                tree.localScale = Vector3.one * (1.2f + (float)random.NextDouble() * 1.2f);
                GameContentBuilder.BuildTreeVisual(tree);
                placed++;
            }
        }

        /// <summary>
        /// Bake NavMesh cho mặt đất + rừng trang trí (con của Environment). Cây thu hoạch được và công trình
        /// không bake vào mà dùng NavMeshObstacle (carve) vì chúng mất đi/xuất hiện lúc chơi.
        /// </summary>
        private static void BakeNavMesh(GameObject environment)
        {
            var surface = environment.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();

            // NavMeshData tạo trong bộ nhớ — phải lưu thành asset thì scene mới giữ được.
            AssetDatabase.DeleteAsset(NavMeshAssetPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshAssetPath);
        }

        private static void CreateVillagers(NpcController prefab, List<ProfessionData> professions)
        {
            var parent = new GameObject("Villagers").transform;
            foreach (var (npcName, gender, professionId, position) in StartingVillagers)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, parent);
                var npc = instance.GetComponent<NpcController>();
                npc.name = $"Npc_{npcName}";
                npc.transform.SetPositionAndRotation(new Vector3(position.x, 0f, position.y), Quaternion.Euler(0f, 180f, 0f));
                SetPrivateField(npc, "npcName", npcName);
                SetPrivateField(npc, "gender", gender);
                SetPrivateField(npc, "profession", professions.Find(p => p.id == professionId));
                npc.RefreshVisuals(); // bật đúng tóc/dụng cụ để nhìn trong Editor cũng đúng

                // Ghi lại override của cả component lẫn trạng thái bật/tắt của từng object con (tóc, dụng cụ),
                // nếu không scene chỉ lưu giá trị gốc của prefab.
                PrefabUtility.RecordPrefabInstancePropertyModifications(npc);
                foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
            }
        }

        /// <summary>
        /// Hang sói ở góc tây bắc, cách trại ~14m: sói đi tuần 2m quanh hang, chỉ đuổi ai lại gần 4.5m
        /// → không tự tràn vào trại, người chơi chủ động quyết định có đi săn hay không.
        /// </summary>
        private static void CreateWolfDen()
        {
            var wolfData = AssetDatabase.LoadAssetAtPath<PredatorData>(GameContentBuilder.WolfDataPath);
            if (wolfData == null || wolfData.prefab == null)
            {
                Debug.LogError("[GameplaySceneBuilder] Chua co PredatorData_Wolf/prefab. Chay 'Build Missing Content' truoc.");
                return;
            }

            var denCenter = new Vector3(-10f, 0f, 9.5f);
            var den = new GameObject("WolfDen").transform;
            den.position = denCenter;
            var rock = Mat("DenRock", Palette.Hex(0x7d7a74));
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad + 0.4f;
                Part(den, $"Rock{i}", PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 1.6f, 0.15f, Mathf.Sin(a) * 1.6f),
                    new Vector3(0.7f, 0.45f, 0.6f), rock, new Vector3(0f, i * 40f, 10f));
            }
            Part(den, "Bones", PrimitiveType.Cylinder, new Vector3(0.4f, 0.04f, -0.3f), new Vector3(0.05f, 0.18f, 0.05f),
                Palette.Ivory, new Vector3(90f, 30f, 0f));

            var positions = new[] { denCenter, denCenter + new Vector3(0.8f, 0f, -0.7f) };
            for (int i = 0; i < positions.Length; i++)
            {
                var wolf = (GameObject)PrefabUtility.InstantiatePrefab(wolfData.prefab);
                wolf.name = i == 0 ? "Wolf" : $"Wolf_{i}";
                wolf.transform.SetPositionAndRotation(positions[i], Quaternion.Euler(0f, 150f + i * 40f, 0f));
            }

            var manager = new GameObject("PredatorManager").AddComponent<PredatorManager>();
            SetPrivateField(manager, "knownPredators", new List<PredatorData> { wolfData });
        }

        private static GameObject CreatePlayer()
        {
            var playerGO = new GameObject("Player") { tag = "Player" };
            var rb = playerGO.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            var col = playerGO.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.radius = 0.3f;
            col.height = 1f;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(playerGO.transform, false);
            Part(visual, "LegL", PrimitiveType.Cylinder, new Vector3(-0.09f, 0.16f, 0f), new Vector3(0.12f, 0.16f, 0.12f), Palette.Skin);
            Part(visual, "LegR", PrimitiveType.Cylinder, new Vector3(0.09f, 0.16f, 0f), new Vector3(0.12f, 0.16f, 0.12f), Palette.Skin);
            Part(visual, "Tunic", PrimitiveType.Cylinder, new Vector3(0f, 0.52f, 0f), new Vector3(0.46f, 0.23f, 0.46f), Palette.Fur);
            Part(visual, "ArmL", PrimitiveType.Cylinder, new Vector3(-0.25f, 0.55f, 0f), new Vector3(0.11f, 0.16f, 0.11f), Palette.Skin);
            Part(visual, "ArmR", PrimitiveType.Cylinder, new Vector3(0.25f, 0.55f, 0f), new Vector3(0.11f, 0.16f, 0.11f), Palette.Skin);
            Part(visual, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.9f, 0f), Vector3.one * 0.34f, Palette.Skin);
            Part(visual, "Hair", PrimitiveType.Sphere, new Vector3(0f, 0.95f, -0.03f), new Vector3(0.37f, 0.25f, 0.37f), Palette.Hair);
            Part(visual, "SpearShaft", PrimitiveType.Cylinder, new Vector3(0.3f, 0.62f, 0.05f), new Vector3(0.04f, 0.6f, 0.04f), Palette.Wood, new Vector3(14f, 0f, 0f));
            Cone(visual, "SpearTip", 5, new Vector3(0.3f, 1.2f, 0.2f), new Vector3(0.1f, 0.16f, 0.1f), Palette.Stone, new Vector3(14f, 0f, 0f));

            var controller = playerGO.AddComponent<PlayerController>();
            SetPrivateField(controller, "visual", visual);
            var playerHealth = playerGO.AddComponent<HealthComponent>();
            SetPrivateField(playerHealth, "regenPerSecond", 1f);
            playerGO.AddComponent<PlayerCombat>();
            GameContentBuilder.BuildHealthBar(playerGO, 1.4f);
            playerGO.AddComponent<PlayerInteraction>();
            return playerGO;
        }

        private static Camera CreateCamera(Transform target)
        {
            var cameraGO = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = cameraGO.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor; // hết nền trời Skybox (nợ polish ở PROGRESS.md)
            cam.backgroundColor = SkyColor;
            cameraGO.AddComponent<AudioListener>();

            var follow = cameraGO.AddComponent<CameraFollow>();
            SetPrivateField(follow, "target", target);
            follow.SnapToTarget();
            return cam;
        }

        private static void CreateInteractionHighlight(PlayerInteraction interaction)
        {
            var root = new GameObject("InteractionHighlight");
            var ring = new GameObject("Ring");
            ring.transform.SetParent(root.transform, false);
            ring.AddComponent<MeshFilter>().sharedMesh = RingMesh(0.62f, 0.72f, 40);
            var renderer = ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Mat("HighlightRing", Palette.Hex(0xf0b95c), emission: 0.8f);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            ring.SetActive(false);

            var highlight = root.AddComponent<InteractionHighlight>();
            SetPrivateField(highlight, "interaction", interaction);
            SetPrivateField(highlight, "ring", ring);
        }

        private static void CreateFarmPlot(string name, Vector3 position)
        {
            var plotGO = new GameObject(name);
            plotGO.transform.position = position;

            // Ô vườn có sẵn: đã khai hoang, đã cày, tưới đầy nước — trồng được ngay từ đầu game.
            var plot = plotGO.AddComponent<FarmPlot>();
            GameContentBuilder.SetupFieldPlot(plot, FieldType.Dry, startsWild: false, startWater: 1f);
        }

        // ─── UI (Canvas overlay — giữ nguyên như bản 2D) ─────────────────────
        private static void CreateUI(Button buttonPrefab, ResourceTypeData wood, ResourceTypeData food, ResourceTypeData knowledge, ResourceTypeData[] farmSupplies,
            List<BuildingData> buildings, List<CropData> crops, List<TechNode> techs, PlayerInteraction interaction)
        {
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();

            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            CreateResourceLabel(canvasGO.transform, "WoodLabel", new Vector2(20f, -20f), wood, "Wood: 0", 28f);
            CreateResourceLabel(canvasGO.transform, "FoodLabel", new Vector2(20f, -55f), food, "Food: 0", 20f);
            CreateFoodBreakdownLabel(canvasGO.transform, new Vector2(20f, -80f));
            CreateResourceLabel(canvasGO.transform, "KnowledgeLabel", new Vector2(20f, -105f), knowledge, "Knowledge: 0", 20f);
            CreatePopulationLabel(canvasGO.transform, new Vector2(20f, -135f));
            // M5d/F3: thóc giống + mạ (vật tư nông nghiệp).
            for (int i = 0; i < farmSupplies.Length; i++)
                CreateResourceLabel(canvasGO.transform, $"{farmSupplies[i].id}Label", new Vector2(20f + i * 130f, -168f), farmSupplies[i], $"{farmSupplies[i].displayName}: 0", 17f);

            CreateBuildMenuPanel(canvasGO.transform, buttonPrefab, buildings);
            CreateCropSelectionPanel(canvasGO.transform, buttonPrefab, crops);
            CreateTechTreePanel(canvasGO.transform, buttonPrefab, techs);
            CreateNotificationLabel(canvasGO.transform);
            CreateInteractionPrompt(canvasGO.transform, interaction);
            CreateSelectionPanel(canvasGO.transform, buttonPrefab);
            CreateBuildingInfoPanel(canvasGO.transform, buttonPrefab);
        }

        /// <summary>Bảng thông tin công trình (cùng chỗ với bảng "Đang chọn" — hai bảng không bao giờ hiện cùng lúc).</summary>
        private static void CreateBuildingInfoPanel(Transform canvasTransform, Button buttonPrefab)
        {
            var rootGO = new GameObject("BuildingInfoPanel");
            rootGO.transform.SetParent(canvasTransform, false);
            var rootRect = rootGO.AddComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = Vector2.zero;
            rootRect.anchoredPosition = new Vector2(20f, 90f);
            rootRect.sizeDelta = new Vector2(620f, 0f);

            var content = new GameObject("Content");
            content.transform.SetParent(rootGO.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = contentRect.anchorMax = contentRect.pivot = Vector2.zero;
            contentRect.sizeDelta = new Vector2(620f, 0f);
            content.AddComponent<Image>().color = new Color(0.12f, 0.09f, 0.06f, 0.82f);
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text title = CreateLayoutLabel(content.transform, "Title", 22f, FontStyles.Bold);
            TMP_Text function = CreateLayoutLabel(content.transform, "Function", 18f, FontStyles.Normal);
            function.GetComponent<LayoutElement>().preferredHeight = 48f; // chức năng + dòng gia đình
            TMP_Text next = CreateLayoutLabel(content.transform, "NextLevel", 16f, FontStyles.Italic);
            next.GetComponent<LayoutElement>().preferredHeight = 44f; // có thể 2 dòng (kèm lý do chưa nâng được)
            Transform row = CreateButtonRow(content.transform, "Actions");
            Button upgrade = Object.Instantiate(buttonPrefab, row);
            upgrade.name = "UpgradeButton";
            content.SetActive(false);

            var panel = rootGO.AddComponent<BuildingInfoPanelUI>();
            SetPrivateField(panel, "panelRoot", content);
            SetPrivateField(panel, "titleLabel", title);
            SetPrivateField(panel, "functionLabel", function);
            SetPrivateField(panel, "nextLevelLabel", next);
            SetPrivateField(panel, "upgradeButton", upgrade);
        }

        /// <summary>Bảng "Đang chọn" góc dưới trái (trên dòng gợi ý tương tác): tên, ô theo nghề, nút đổi nghề.</summary>
        private static void CreateSelectionPanel(Transform canvasTransform, Button buttonPrefab)
        {
            var rootGO = new GameObject("SelectionPanel");
            rootGO.transform.SetParent(canvasTransform, false);
            var rootRect = rootGO.AddComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = Vector2.zero;
            rootRect.anchoredPosition = new Vector2(20f, 90f);
            rootRect.sizeDelta = new Vector2(700f, 0f);

            // Nội dung là object con: ẩn/hiện nó mà script ở object cha vẫn nghe sự kiện.
            var content = new GameObject("Content");
            content.transform.SetParent(rootGO.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = contentRect.anchorMax = contentRect.pivot = Vector2.zero;
            contentRect.sizeDelta = new Vector2(700f, 0f);
            content.AddComponent<Image>().color = new Color(0.12f, 0.09f, 0.06f, 0.82f);
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text title = CreateLayoutLabel(content.transform, "Title", 22f, FontStyles.Bold);
            TMP_Text members = CreateLayoutLabel(content.transform, "Members", 18f, FontStyles.Normal);
            Transform chips = CreateButtonRow(content.transform, "ProfessionChips");
            TMP_Text changeLabel = CreateLayoutLabel(content.transform, "ChangeLabel", 16f, FontStyles.Italic);
            changeLabel.text = "Đổi nghề cho nhóm đang chọn:";
            Transform changeRow = CreateButtonRow(content.transform, "ChangeProfession");
            content.SetActive(false);

            var panel = rootGO.AddComponent<SelectionPanelUI>();
            SetPrivateField(panel, "panelRoot", content);
            SetPrivateField(panel, "titleLabel", title);
            SetPrivateField(panel, "membersLabel", members);
            SetPrivateField(panel, "professionChipContainer", chips);
            SetPrivateField(panel, "changeProfessionContainer", changeRow);
            SetPrivateField(panel, "buttonPrefab", buttonPrefab);
        }

        private static TMP_Text CreateLayoutLabel(Transform parent, string name, float fontSize, FontStyles style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = new Color(0.96f, 0.92f, 0.85f);
            tmp.text = string.Empty;
            go.AddComponent<LayoutElement>().preferredHeight = fontSize + 6f;
            return tmp;
        }

        private static Transform CreateButtonRow(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var row = go.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 6f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = false; // nút giữ kích thước của prefab
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            go.AddComponent<LayoutElement>().preferredHeight = 32f;
            return go.transform;
        }

        private static void CreateInteractionPrompt(Transform canvasTransform, PlayerInteraction interaction)
        {
            var labelGO = new GameObject("InteractionPrompt");
            labelGO.transform.SetParent(canvasTransform, false);
            var rect = labelGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            rect.sizeDelta = new Vector2(900f, 40f);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = new Color32(0, 0, 0, 200);
            tmp.text = string.Empty;
            var prompt = labelGO.AddComponent<InteractionPromptUI>();
            SetPrivateField(prompt, "interaction", interaction);
            SetPrivateField(prompt, "combat", interaction.GetComponent<PlayerCombat>());
            SetPrivateField(prompt, "label", tmp);
        }

        private static void CreateResourceLabel(Transform canvasTransform, string name, Vector2 anchoredPosition,
            ResourceTypeData resource, string initialText, float fontSize)
        {
            var labelGO = new GameObject(name);
            labelGO.transform.SetParent(canvasTransform, false);
            var rect = labelGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(300f, fontSize + 10f);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.text = initialText;
            var barUI = labelGO.AddComponent<ResourceBarUI>();
            SetPrivateField(barUI, "displayedResource", resource);
            SetPrivateField(barUI, "label", tmp);
        }

        private static void CreateFoodBreakdownLabel(Transform canvasTransform, Vector2 anchoredPosition)
        {
            var labelGO = new GameObject("FoodBreakdownLabel");
            labelGO.transform.SetParent(canvasTransform, false);
            var rect = labelGO.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(700f, 24f);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 15f;
            tmp.color = new Color(0.9f, 0.86f, 0.75f);
            tmp.text = string.Empty;
            SetPrivateField(labelGO.AddComponent<FoodBreakdownUI>(), "label", tmp);
        }

        private static void CreatePopulationLabel(Transform canvasTransform, Vector2 anchoredPosition)
        {
            var labelGO = new GameObject("PopulationLabel");
            labelGO.transform.SetParent(canvasTransform, false);
            var rect = labelGO.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(400f, 30f);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 20f;
            tmp.text = string.Empty;
            SetPrivateField(labelGO.AddComponent<PopulationUI>(), "label", tmp);
        }

        private static void CreateNotificationLabel(Transform canvasTransform)
        {
            var labelGO = new GameObject("NotificationLabel");
            labelGO.transform.SetParent(canvasTransform, false);
            var rect = labelGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -20f);
            rect.sizeDelta = new Vector2(600f, 40f);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = string.Empty;
            var notificationUI = labelGO.AddComponent<NotificationUI>();
            SetPrivateField(notificationUI, "label", tmp);
        }

        private static Transform CreatePanelContainer(Transform canvasTransform, string name, Vector2 anchoredPosition)
        {
            var panelGO = new GameObject(name);
            panelGO.transform.SetParent(canvasTransform, false);
            var rect = panelGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(200f, 200f);

            var layout = panelGO.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperRight;
            layout.spacing = 4f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            return panelGO.transform;
        }

        private static void CreateBuildMenuPanel(Transform canvasTransform, Button buttonPrefab, List<BuildingData> buildings)
        {
            var container = CreatePanelContainer(canvasTransform, "BuildPanel", new Vector2(-20f, -20f));
            var ui = container.gameObject.AddComponent<BuildMenuUI>();
            SetPrivateField(ui, "availableBuildings", buildings);
            SetPrivateField(ui, "buttonPrefab", buttonPrefab);
            SetPrivateField(ui, "buttonContainer", container);
        }

        private static void CreateCropSelectionPanel(Transform canvasTransform, Button buttonPrefab, List<CropData> crops)
        {
            var container = CreatePanelContainer(canvasTransform, "CropPanel", new Vector2(-20f, -350f));
            var ui = container.gameObject.AddComponent<CropSelectionUI>();
            SetPrivateField(ui, "availableCrops", crops);
            SetPrivateField(ui, "buttonPrefab", buttonPrefab);
            SetPrivateField(ui, "buttonContainer", container);
        }

        private static void CreateTechTreePanel(Transform canvasTransform, Button buttonPrefab, List<TechNode> techs)
        {
            var container = CreatePanelContainer(canvasTransform, "TechPanel", new Vector2(-20f, -560f));
            var ui = container.gameObject.AddComponent<TechTreeUI>();
            SetPrivateField(ui, "allTechs", techs);
            SetPrivateField(ui, "entryButtonPrefab", buttonPrefab);
            SetPrivateField(ui, "entryContainer", container);
        }
    }
}
