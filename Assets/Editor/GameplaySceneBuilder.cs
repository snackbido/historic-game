using System.Collections.Generic;
using System.Linq;
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

        // ─── Terrain Blender thật (2026-10-03): thay nền phẳng bằng địa hình núi/sông thật ─────
        private const string TerrainFbxPath = "Assets/_ImportedBlender/PrehistoricTerrain.fbx";
        private const string GroundLayerName = "Ground";
        private const string GroundPhysicMaterialPath = "Assets/Materials/GroundFriction.physicMaterial";
        private static readonly string[] GroundMeshNames =
            { "Terrain_Main", "MountainPeak_01", "MountainPeak_03", "MountainPeak_04", "MountainPeak_05" };
        // Marker Blender chỉ để định vị (trại/ruộng khởi đầu) — ẩn đi, game tự dựng model thật tại đúng vị trí đó.
        private static readonly string[] HiddenMarkerNames =
            { "Start_Campfire", "Start_Shelter", "Start_Storage", "Start_FarmPlot", "StartArea_Center" };
        private static readonly string[] SkipColliderPrefixes = { "River", "Stream", "Sea", "Waterfall" };

        private static readonly Color SkyColor = new Color(0.93f, 0.83f, 0.64f); // tông chiều vàng, khớp daySky của DayNightCycle
        private const string PixelArtMaterialPath = "Assets/Materials/PixelArt.mat";

        /// <summary>Bảng màu đất – rừng – nước cho phong cách pixel (khung hình bị ép về các màu này).</summary>
        private static Color[] PixelPalette => new[]
        {
            0x0e0c0a, 0x1f3a1c, 0x2f5a28, 0x3f6b2a, 0x4f8a33, 0x66a33d, 0x86bf4c, 0xb0d86a,
            0x2e1d10, 0x47301a, 0x5a3d22, 0x6b4423, 0x8b5e34, 0xa87b4a, 0xc9a35a, 0xd9b36c,
            0xc98d5e, 0xe0b088, 0x3a3632, 0x5c5752, 0x8a857c, 0xb5b0a5, 0xe8e4d8, 0xcfe3e6,
            0x1c2e4a, 0x2f5f86, 0x3f7fb0, 0x6fa8d0, 0xa8d0e6, 0xe2c25a, 0xffd36a, 0xff8a2a,
            0xf0561a, 0xc0381a, 0x8a2a20, 0x141a2e, 0x263452, 0x3a4e70, 0x4a7fa8, 0x7a9a3a,
        }.Select(Palette.Hex).ToArray();

        // Dân làng ban đầu (quyết định 2026-09-30: 3–4 người, có cả nam lẫn nữ, mỗi người một nghề).
        private static readonly (string name, Gender gender, string professionId, Vector2 position)[] StartingVillagers =
        {
            ("Ka", Gender.Male, "villager", new Vector2(0.2f, -3.2f)),
            ("Mây", Gender.Female, "farmer", new Vector2(-1f, -3.6f)),
            ("Đá", Gender.Male, "hunter", new Vector2(1.4f, -3.8f)),
            ("Suối", Gender.Female, "scout", new Vector2(0.4f, -4.6f)),
        };

        // Vị trí cây gỗ thu hoạch được = 8 điểm "Resource_Wood_*" do người thiết kế đặt trong Blender.
        // Lấy tọa độ thật lúc dựng scene (FindBlenderMarkerPosition) thay vì tự quy đổi trục Blender→Unity
        // bằng tay — dễ sai dấu/trục khi FBX export (đã từng sai, xem log 2026-10-03).
        private static readonly string[] WoodMarkerNames =
        {
            "Resource_Wood_001", "Resource_Wood_002", "Resource_Wood_003", "Resource_Wood_004",
            "Resource_Wood_005", "Resource_Wood_006", "Resource_Wood_007", "Resource_Wood_008",
        };
        // Dùng khi terrain Blender đang tắt (xem ghi chú ở CreateBlenderTerrain) — cùng bố cục với bản web
        // (web/src/data/gameData.js): (x, z) trên mặt đất, gần trại.
        private static readonly Vector2[] FallbackTreePositions =
        {
            new Vector2(2f, 1f), new Vector2(4.5f, 2.5f), new Vector2(-1.5f, 3f), new Vector2(6f, -0.5f),
            new Vector2(-5f, 2f), new Vector2(1f, 4.5f), new Vector2(7f, 3.5f), new Vector2(-6.5f, -3.5f),
        };

        [MenuItem("Tools/Prehistoric/Build All (Content + Scene)")]
        public static void BuildAll()
        {
            PixelTextureBuilder.BuildAll(); // thử nghiệm phong cách pixel (2026-10-02)
            MusicBuilder.BuildAll(); // nhạc nền sinh bằng code (M7/P2)
            SfxBuilder.BuildAll(); // tiếng động sinh bằng code (M7/P2b)
            GameContentBuilder.Build();
            IconBuilder.BuildAll(); // icon thanh công cụ + tài nguyên (cần prefab vừa dựng)
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
            var levee = AssetDatabase.LoadAssetAtPath<BuildingData>(GameContentBuilder.LeveeDataPath);
            var torch = AssetDatabase.LoadAssetAtPath<BuildingData>(GameContentBuilder.TorchDataPath);
            var fence = AssetDatabase.LoadAssetAtPath<BuildingData>(GameContentBuilder.FenceDataPath);
            var buildings = new List<BuildingData> { hut, storage, dryField, seedbed, paddyField, well, canal, levee, waterWheel, mortar, torch, fence };
            var techs = new List<TechNode> { techFarming, techRice, techIrrigation, techWaterWheel };

            if (wood == null || food == null || knowledge == null || hut == null || storage == null ||
                berry == null || techFarming == null || boarData == null || buttonPrefab == null ||
                rice == null || vegetable == null || techRice == null || goatData == null || foodTypes.Contains(null) ||
                dryField == null || paddyField == null || well == null ||
                seedlingCrop == null || riceSeed == null || seedling == null || seedbed == null || manure == null || mortar == null || sheaf == null || canal == null || techIrrigation == null || waterWheel == null || techWaterWheel == null || levee == null || torch == null || fence == null)
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
            // Terrain Blender thật (2026-10-03) thử nghiệm: hiển thị đúng hướng nhưng MeshCollider không lồi
            // bị lỗi engine Unity (raycast/va chạm trượt toàn bộ — đã kiểm chứng kỹ) và NavMesh bake chỉ phủ
            // được một mảng nhỏ xa khu trại. Tạm quay lại nền phẳng cho tới khi có hướng giải quyết khác
            // (vd Unity Terrain + heightmap thay vì MeshCollider). File FBX + CreateBlenderTerrain() vẫn giữ
            // nguyên trong code để dùng lại sau, chỉ không gọi trong luồng Build() mặc định.
            // 2026-10-05: địa hình có đồi núi, sông, biển + cây cỏ theo .claude/LANDSCAPE_DESIGN.md (LandscapeBuilder) —
            // độ cao lấy từ lưới (WorldTerrain), không qua collider. Grounded() bên dưới đặt mọi thứ lên mặt đất này.
            Transform terrainRoot = null;
            LandscapeBuilder.Build(environment);
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
            Vector3 campfirePos = FindBlenderMarkerPosition(terrainRoot, "Start_Campfire") ?? Grounded(0f, -2f);
            CreateCampfire(campfirePos);
            // Milestone 6: lịch thiên tai (các loại thiên tai gắn thêm vào object này ở D2–D5).
            var disasters = new GameObject("DisasterManager");
            disasters.AddComponent<DisasterManager>();
            ConfigureDisaster(disasters.AddComponent<DroughtDisaster>(), "drought", "Hạn hán",
                "trời oi bức, nắng gắt không một gợn mây",
                "ruộng khô nhanh gấp 3, ao cạn dần — dùng giếng, guồng nước",
                "Hạn hán đã qua — trời dịu mát, ao đầy nước trở lại", durationDays: 1f);
            var flood = disasters.AddComponent<FloodDisaster>();
            ConfigureDisaster(flood, "flood", "Lũ lụt",
                "mây đen kéo về, mưa rả rích không dứt",
                "ao tràn bờ — ruộng cạn, nhà gần ao bị ngập. Đắp đê để chắn nước",
                "Nước lũ đã rút — sửa lại nhà cửa, dọn ruộng úng", durationDays: 0.5f);
            SetPrivateField(flood, "floodWaterMaterial", Mat("FloodWater", Palette.Hex(0x6b8c94), emission: 0.05f));
            var wildfire = disasters.AddComponent<WildfireDisaster>();
            ConfigureDisaster(wildfire, "wildfire", "Cháy rừng",
                "trời hanh khô, gió lớn, sấm chớp xa xa",
                "sét đánh cháy cây, lửa lan sang nhà và ruộng — gánh nước dập lửa!",
                "Đám cháy đã tắt — dọn dẹp, sửa lại nhà cửa", durationDays: 0.5f);
            SetPrivateField(wildfire, "flameMaterial", Mat("WildfireFlame", Palette.Hex(0xf0561a), emission: 0.55f));
            SetPrivateField(wildfire, "flameCoreMaterial", Mat("WildfireFlameCore", Palette.Hex(0xffc94a), emission: 0.8f));
            SetPrivateField(wildfire, "smokeMaterial", Mat("Smoke", Palette.Hex(0x6a6560)));
            SetPrivateField(wildfire, "charredMaterial", Mat("CharredWood", Palette.Hex(0x3a2a1c)));
            var raid = disasters.AddComponent<WolfRaidDisaster>();
            ConfigureDisaster(raid, "wolf_raid", "Sói đột kích",
                "tiếng sói hú vọng về từ rừng sâu, mỗi lúc một gần",
                "bầy sói kéo vào làng khi trời tối — vào lều, đứng gần lửa, người biết đánh ra chặn",
                "Bầy sói đã rút về rừng", durationDays: 1f);
            SetPrivateField(raid, "wolf", AssetDatabase.LoadAssetAtPath<PredatorData>(GameContentBuilder.WolfDataPath));
            // Milestone 7: hiệu ứng hạt (chặt, gặt, xây, đánh, mưa, sét…).
            SetPrivateField(new GameObject("VfxManager").AddComponent<VfxManager>(), "particleMaterial", ParticleMaterial());
            // Nhạc nền ngày/đêm: file của người dùng trong Assets/Audio/Music (Day/Night.*) nếu có, không thì bản sinh bằng code.
            // Tiếng động: file của người dùng trong Assets/Audio/SFX (Chop.wav, Howl.mp3…) nếu có, không thì bản sinh bằng code.
            var sfxEntries = new List<SfxManager.Entry>();
            foreach (SfxKind kind in System.Enum.GetValues(typeof(SfxKind)))
                sfxEntries.Add(new SfxManager.Entry { kind = kind, clip = SfxBuilder.Resolve(kind) });
            SetPrivateField(new GameObject("SfxManager").AddComponent<SfxManager>(), "clips", sfxEntries);
            var music = new GameObject("MusicManager").AddComponent<MusicManager>();
            SetPrivateField(music, "dayMusic", MusicBuilder.Resolve("Day"));
            SetPrivateField(music, "nightMusic", MusicBuilder.Resolve("Night"));

            var resources = new GameObject("ResourceNodes").transform;
            for (int i = 0; i < FallbackTreePositions.Length; i++)
            {
                Vector3? markerPos = FindBlenderMarkerPosition(terrainRoot, WoodMarkerNames[i]);
                Vector3 groundXZ = markerPos.HasValue
                    ? Grounded(markerPos.Value.x, markerPos.Value.z)
                    : Grounded(FallbackTreePositions[i].x, FallbackTreePositions[i].y);
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, resources);
                tree.name = i == 0 ? "Tree" : $"Tree_{i}";
                tree.transform.position = groundXZ;
                tree.transform.rotation = Quaternion.Euler(0f, i * 47f, 0f);
            }

            // Ao cá phía bắc trại (nạp prefab sau khi bake NavMesh để tham chiếu còn hiệu lực).
            var pondPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameContentBuilder.PondPrefabPath);
            var pond = (GameObject)PrefabUtility.InstantiatePrefab(pondPrefab, resources);
            pond.name = "FishingPond";
            pond.transform.position = Grounded(4.5f, 6f);

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

            CreateFarmPlot("FarmPlot_1", Grounded(-2f, -1.5f));
            CreateFarmPlot("FarmPlot_2", Grounded(-3.2f, -1.5f));

            var boarInstance = (GameObject)PrefabUtility.InstantiatePrefab(boarData.prefab);
            boarInstance.name = "WildBoar";
            boarInstance.transform.SetPositionAndRotation(Grounded(3f, -1.5f), Quaternion.Euler(0f, 200f, 0f));
            SetPrivateField(boarInstance.GetComponent<AnimalController>(), "data", boarData);

            var goatInstance = (GameObject)PrefabUtility.InstantiatePrefab(goatData.prefab);
            goatInstance.name = "Goat";
            goatInstance.transform.SetPositionAndRotation(Grounded(-5f, -6.5f), Quaternion.Euler(0f, 160f, 0f));
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
            SetPrivateField(techManager, "knownBuildings", buildings);
            SetPrivateField(techManager, "knownCrops", crops);

            var gameManagerGO = new GameObject("GameManager");
            var gameManager = gameManagerGO.AddComponent<GameManager>();
            SetPrivateField(gameManager, "player", player.GetComponent<PlayerController>());
            SetPrivateField(gameManager, "resourceManager", rm);
            SetPrivateField(gameManager, "buildingPlacer", placer);
            // Tải game: cây đã chặt sau lúc lưu được tạo lại từ prefab (nạp lại — tham chiếu cũ có thể đã mất sau bake NavMesh).
            SetPrivateField(gameManager, "treePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(GameContentBuilder.TreePrefabPath));
            gameManagerGO.AddComponent<SaveLoadHotkeys>();

            // Nạp lại: tạo asset NavMesh phía trên làm tham chiếu prefab (component) đã nạp trước đó mất hiệu lực.
            buttonPrefab = AssetDatabase.LoadAssetAtPath<Button>(ButtonPrefabPath);
            CreateUI(buttonPrefab, wood, food, knowledge, new[] { sheaf, riceSeed, seedling, manure }, buildings, crops, techs, player.GetComponent<PlayerInteraction>());

            EditorSceneManager.SaveScene(scene, ScenePath);
            // Scene phải có trong Build Settings thì menu "Về menu chính" mới nạp lại được (và bản build mới có scene).
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[GameplaySceneBuilder] Da tao scene 2.5D tai {ScenePath}");
        }

        // ─── World ───────────────────────────────────────────────────────────
        private static Light SetupLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.47f, 0.41f, 0.32f); // khớp dayAmbient của DayNightCycle
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = SkyColor;
            RenderSettings.fogStartDistance = 32f; // địa hình rộng 96m: lùi sương mù để thấy núi, biển khi kéo camera xa
            RenderSettings.fogEndDistance = 80f;

            var sunGO = new GameObject("Sun");
            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.82f, 0.56f); // khớp daySunColor của DayNightCycle — chiều vàng ấm
            sun.intensity = 1.15f;
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

        /// <summary>
        /// Dựng địa hình thật lấy từ Blender (núi, sông, cây, đá — export qua Blender MCP 2026-10-03),
        /// thay cho nền phẳng + rừng trang trí sinh bằng code trước đây. Gắn MeshCollider cho vật lý/raycast
        /// đặt công trình; riêng Terrain_Main + 4 đỉnh núi vào layer "Ground" để lấy cao độ chính xác
        /// (không tính cây/đá/mặt nước, tránh lấy nhầm độ cao ngọn cây làm mặt đất).
        /// </summary>
        private static Transform CreateBlenderTerrain(Transform parent)
        {
            // FBX mặc định import với Read/Write tắt (tiết kiệm RAM) → gán MeshCollider.sharedMesh vẫn chạy
            // không lỗi nhưng collider rỗng, mọi raycast xuống đất đều trượt (đã gặp thực tế 2026-10-03).
            // Bật Read/Write + reimport nếu asset chưa bật, trước khi nạp/instantiate.
            if (AssetImporter.GetAtPath(TerrainFbxPath) is ModelImporter modelImporter && !modelImporter.isReadable)
            {
                modelImporter.isReadable = true;
                modelImporter.SaveAndReimport();
            }

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(TerrainFbxPath);
            if (fbx == null)
            {
                Debug.LogError($"[GameplaySceneBuilder] Khong tim thay terrain Blender tai {TerrainFbxPath} — dung nen phang tam thoi.");
                CreateGround(parent);
                CreateDecorForest(parent);
                return parent;
            }

            var terrainGO = (GameObject)PrefabUtility.InstantiatePrefab(fbx, parent);
            terrainGO.name = "BlenderTerrain";
            // Sửa lệch trục khi export/import FBX: Terrain_Main đo được cao 200 đơn vị (trục Y) và chỉ sâu
            // 24.41 (trục Z) — ngược hẳn một nền đất (phải thấp, rộng). Xoay -90° quanh X đưa trục "lên" và
            // "sâu" về đúng chỗ cho cả 244 object con cùng lúc (xác nhận bằng renderer.bounds thực đo trong Unity).
            terrainGO.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            int groundLayer = LayerMask.NameToLayer(GroundLayerName);
            if (groundLayer < 0)
                Debug.LogWarning($"[GameplaySceneBuilder] Chua co layer '{GroundLayerName}' — lay cao do se khong hoat dong dung.");

            foreach (var filter in terrainGO.GetComponentsInChildren<MeshFilter>(true))
            {
                var go = filter.gameObject;
                if (System.Array.IndexOf(HiddenMarkerNames, go.name) >= 0)
                {
                    go.SetActive(false);
                    continue;
                }
                if (System.Array.Exists(SkipColliderPrefixes, p => go.name.StartsWith(p)))
                    continue; // mặt nước (sông/suối/biển/thác): chỉ hình ảnh, không va chạm

                var collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                go.isStatic = true;

                if (groundLayer >= 0 && System.Array.IndexOf(GroundMeshNames, go.name) >= 0)
                {
                    go.layer = groundLayer;
                    collider.material = GroundPhysicMaterial();
                }
            }

            Physics.SyncTransforms(); // để Grounded()/SampleGroundHeight() raycast đúng ngay trong cùng lệnh dựng scene
            return terrainGO.transform;
        }

        /// <summary>Tìm transform theo tên trong cây con terrain Blender (vd Start_Campfire) để lấy đúng tọa độ nhà thiết kế đặt.</summary>
        private static Vector3? FindBlenderMarkerPosition(Transform root, string name)
        {
            if (root == null) return null; // terrain Blender đang tắt trong Build() mặc định (xem ghi chú ở CreateBlenderTerrain)
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.position;
            return null;
        }

        /// <summary>Cao độ mặt đất thật tại (x, z), bằng raycast xuống chỉ vào layer "Ground" (núi/nền, không tính cây/đá).</summary>
        private static float SampleGroundHeight(float x, float z, float fallback = 0f)
        {
            int mask = LayerMask.GetMask(GroundLayerName);
            if (mask != 0 && Physics.Raycast(new Vector3(x, 500f, z), Vector3.down, out RaycastHit hit, 1000f, mask))
                return hit.point.y;
            return fallback;
        }

        private static Vector3 Grounded(float x, float z, float extraHeight = 0f) =>
            new Vector3(x, WorldTerrain.HeightAt(x, z) + extraHeight, z);

        /// <summary>
        /// Terrain thật có suối/sông cắt ngang nên vài điểm trong cụm trại ban đầu rơi đúng ngoài NavMesh
        /// (thử nhiều điểm thấy lệch, 2026-10-03). Snap về điểm NavMesh gần nhất thay vì dò tọa độ tay —
        /// NpcController/PredatorAI cần đứng đúng trên NavMesh mới tạo agent được.
        /// </summary>
        private static Vector3 SnapToNavMesh(Vector3 desired, float maxDistance = 8f)
        {
            if (UnityEngine.AI.NavMesh.SamplePosition(desired, out var hit, maxDistance, UnityEngine.AI.NavMesh.AllAreas))
                return hit.position;
            Debug.LogWarning($"[GameplaySceneBuilder] Khong tim thay NavMesh gan {desired} trong {maxDistance}m.");
            return desired;
        }

        private static PhysicsMaterial groundPhysicMaterial;
        private static PhysicsMaterial GroundPhysicMaterial()
        {
            if (groundPhysicMaterial != null) return groundPhysicMaterial;
            groundPhysicMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(GroundPhysicMaterialPath);
            if (groundPhysicMaterial == null)
            {
                groundPhysicMaterial = new PhysicsMaterial("GroundFriction")
                {
                    dynamicFriction = 1f,
                    staticFriction = 1f,
                    frictionCombine = PhysicsMaterialCombine.Maximum,
                    bounciness = 0f,
                    bounceCombine = PhysicsMaterialCombine.Minimum,
                };
                AssetDatabase.CreateAsset(groundPhysicMaterial, GroundPhysicMaterialPath);
            }
            return groundPhysicMaterial;
        }

        /// <summary>Nền phẳng dự phòng — chỉ dùng khi chưa export/chưa có terrain Blender.</summary>
        private static void CreateGround(Transform parent)
        {
            // Plane mặc định 10×10 → scale 6 = 60×60. Không cần collider: chuột raycast vào mặt phẳng toán học.
            // Cỏ pixel: mỗi ô texture 64 điểm ảnh phủ 4m (60m → lặp 15 lần).
            var ground = Part(parent, "Ground", PrimitiveType.Plane, Vector3.zero, new Vector3(6f, 1f, 6f),
                TexturedMat("GroundPixel", PixelTextureBuilder.Grass, new Vector2(15f, 15f)));
            ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            var camp = Part(parent, "CampGround", PrimitiveType.Cylinder, new Vector3(0f, -0.045f, 0f),
                new Vector3(9f, 0.05f, 7.5f), TexturedMat("CampGroundPixel", PixelTextureBuilder.Dirt, new Vector2(4.5f, 3.75f)));
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
                npc.transform.SetPositionAndRotation(SnapToNavMesh(Grounded(position.x, position.y)), Quaternion.Euler(0f, 180f, 0f));
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

            var denCenter = Grounded(-10f, 9.5f);
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
                wolf.transform.SetPositionAndRotation(SnapToNavMesh(positions[i]), Quaternion.Euler(0f, 150f + i * 40f, 0f));
            }

            var manager = new GameObject("PredatorManager").AddComponent<PredatorManager>();
            SetPrivateField(manager, "knownPredators", new List<PredatorData> { wolfData });
        }

        private static GameObject CreatePlayer()
        {
            var playerGO = new GameObject("Player") { tag = "Player" };
            var rb = playerGO.AddComponent<Rigidbody>();
            // Đồi núi (2026-10-05): không trọng lực, không collider mặt đất — PlayerController tự đặt độ cao theo
            // WorldTerrain mỗi bước vật lý. Trục Y để tự do (không khóa) thì MovePosition mới lên/xuống dốc được.
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
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

        /// <summary>Hạt hình vuông, màu lấy từ màu hạt (vertex color), mờ dần được.</summary>
        private static Material ParticleMaterial()
        {
            const string path = "Assets/Materials/Particle.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f)); // ×2 trong shader → giữ nguyên màu hạt
            EditorUtility.SetDirty(mat);
            return mat;
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

            // Thử nghiệm phong cách pixel art (2026-10-02) — phím P bật/tắt.
            var pixel = cameraGO.AddComponent<PixelArtCamera>();
            var pixelMat = AssetDatabase.LoadAssetAtPath<Material>(PixelArtMaterialPath);
            if (pixelMat == null)
            {
                pixelMat = new Material(Shader.Find("Hidden/PrehistoricTribe/PixelArt"));
                AssetDatabase.CreateAsset(pixelMat, PixelArtMaterialPath);
            }
            SetPrivateField(pixel, "material", pixelMat);
            SetPrivateField(pixel, "palette", PixelPalette);

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
            // Thiết kế lại UI (.claude/RE-DESIGNUI.md): co giãn theo màn hình, tham chiếu 1920×1080.
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Dải tài nguyên trên cùng: Gỗ, Thức ăn, Tri thức (+ Dân số) luôn hiện; vật tư nông nghiệp chỉ hiện khi có.
            var hud = UIObject("TopHUD", canvasGO.transform).AddComponent<HudUI>();
            SetPrivateField(hud, "entries", new List<HudUI.Entry>
            {
                new HudUI.Entry { type = wood, alwaysShow = true, hint = "Chặt cây (E) — dùng để xây và sửa công trình" },
                new HudUI.Entry { type = food, alwaysShow = true, hint = "Dân làng tự lấy ăn khi đói; đồ dễ hỏng để ngoài kho sẽ hỏng dần" },
                new HudUI.Entry { type = knowledge, alwaysShow = true, hint = "Tự tăng theo thời gian — dùng để nghiên cứu (tab Nghiên cứu, phím T)" },
                new HudUI.Entry { type = farmSupplies[0], hint = "Phơi khô ở cối giã rồi tuốt thành thóc" },
                new HudUI.Entry { type = farmSupplies[1], hint = "Gieo ở ruộng mạ, hoặc giã thành gạo" },
                new HudUI.Entry { type = farmSupplies[2], hint = "Cấy sang ruộng nước" },
                new HudUI.Entry { type = farmSupplies[3], hint = "Bón ruộng để tăng sản lượng" },
            });
            SetPrivateField(hud, "peopleIcon", IconBuilder.People);

            // Thanh công cụ cạnh dưới: tab Xây dựng / Trồng trọt / Nghiên cứu (thay 3 cột nút bên phải).
            var toolbar = UIObject("BottomBar", canvasGO.transform).AddComponent<ToolbarUI>();
            SetPrivateField(toolbar, "buildings", buildings);
            SetPrivateField(toolbar, "crops", crops);
            SetPrivateField(toolbar, "techs", techs);
            SetPrivateField(toolbar, "lockSprite", IconBuilder.Lock);
            SetPrivateField(toolbar, "badgeSprite", IconBuilder.Dot);

            CreateDisasterBanner(canvasGO.transform);
            UIObject("Toast", canvasGO.transform).AddComponent<ToastUI>(); // thay dòng thông báo cũ
            CreateInteractionPrompt(canvasGO.transform, interaction);
            CreateSelectionPanel(canvasGO.transform, buttonPrefab);
            CreateBuildingInfoPanel(canvasGO.transform, buttonPrefab);
            UIObject("Tooltip", canvasGO.transform).AddComponent<TooltipUI>(); // trên mọi bảng, dưới menu
            // M7/P3: menu chính / tạm dừng / cài đặt — phủ toàn màn hình, nằm trên cùng, tự dựng giao diện khi chạy.
            var menuGO = new GameObject("GameMenu", typeof(RectTransform));
            menuGO.transform.SetParent(canvasGO.transform, false);
            var menuRect = (RectTransform)menuGO.transform;
            menuRect.anchorMin = Vector2.zero;
            menuRect.anchorMax = Vector2.one;
            menuRect.offsetMin = menuRect.offsetMax = Vector2.zero;
            menuGO.AddComponent<GameMenuUI>();
        }

        private static GameObject UIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Bảng thông tin công trình (cùng chỗ với bảng "Đang chọn" — hai bảng không bao giờ hiện cùng lúc).</summary>
        private static void CreateBuildingInfoPanel(Transform canvasTransform, Button buttonPrefab)
        {
            var rootGO = new GameObject("BuildingInfoPanel");
            rootGO.transform.SetParent(canvasTransform, false);
            var rootRect = rootGO.AddComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = Vector2.zero;
            rootRect.anchoredPosition = new Vector2(20f, 235f); // trên dòng gợi ý + thanh công cụ cạnh dưới
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
            rootRect.anchoredPosition = new Vector2(20f, 235f); // trên dòng gợi ý + thanh công cụ cạnh dưới
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
            rect.anchoredPosition = new Vector2(0f, 185f); // ngay trên bảng công cụ (khi mở)
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

        private static void ConfigureDisaster(DisasterEvent disaster, string id, string displayName, string warning,
            string active, string end, float durationDays, float weight = 1f)
        {
            SetPrivateField(disaster, "id", id);
            SetPrivateField(disaster, "displayName", displayName);
            SetPrivateField(disaster, "warningMessage", warning);
            SetPrivateField(disaster, "activeMessage", active);
            SetPrivateField(disaster, "endMessage", end);
            SetPrivateField(disaster, "durationDays", durationDays);
            SetPrivateField(disaster, "weight", weight);
        }

        private static void CreateDisasterBanner(Transform canvasTransform)
        {
            var labelGO = new GameObject("DisasterBanner");
            labelGO.transform.SetParent(canvasTransform, false);
            var rect = labelGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -62f);
            rect.sizeDelta = new Vector2(900f, 34f);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 22f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = string.Empty;
            SetPrivateField(labelGO.AddComponent<DisasterBannerUI>(), "label", tmp);
        }
    }
}
