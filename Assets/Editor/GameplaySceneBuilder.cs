using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PrehistoricTribe.EditorTools
{
    /// <summary>
    /// Dựng scene placeholder Gameplay.unity bằng code để test vòng lặp
    /// di chuyển + thu thập tài nguyên (Milestone 1) mà không cần kéo-thả tay.
    /// Chạy qua menu Tools/Prehistoric/Build Gameplay Scene, hoặc batch mode
    /// (-executeMethod PrehistoricTribe.EditorTools.GameplaySceneBuilder.Build).
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

        [MenuItem("Tools/Prehistoric/Build All (Content + Scene)")]
        public static void BuildAll()
        {
            GameContentBuilder.Build();
            Build();
        }

        [MenuItem("Tools/Prehistoric/Build Gameplay Scene (Milestone 1)")]
        public static void Build()
        {
            var wood = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(WoodAssetPath);
            var food = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(FoodAssetPath);
            var knowledge = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(KnowledgeAssetPath);
            var hut = AssetDatabase.LoadAssetAtPath<BuildingData>(HutDataPath);
            var storage = AssetDatabase.LoadAssetAtPath<BuildingData>(StorageDataPath);
            var berry = AssetDatabase.LoadAssetAtPath<CropData>(BerryDataPath);
            var techFarming = AssetDatabase.LoadAssetAtPath<TechNode>(TechFarmingPath);
            var boarData = AssetDatabase.LoadAssetAtPath<AnimalData>(BoarDataPath);
            var buttonPrefab = AssetDatabase.LoadAssetAtPath<Button>(ButtonPrefabPath);

            if (wood == null || food == null || knowledge == null || hut == null || storage == null ||
                berry == null || techFarming == null || boarData == null || buttonPrefab == null)
            {
                Debug.LogError("[GameplaySceneBuilder] Thieu asset can thiet. Chay 'Tools/Prehistoric/Build Missing Content' truoc (hoac dung 'Build All').");
                return;
            }

            if (hut.prefab == null || storage.prefab == null || boarData.prefab == null)
            {
                Debug.LogError("[GameplaySceneBuilder] BuildingData/AnimalData chua co prefab. Chay 'Tools/Prehistoric/Build Missing Content' truoc (hoac dung 'Build All').");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            var cam = cameraGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cameraGO.AddComponent<AudioListener>();
            var camFollow = cameraGO.AddComponent<CameraFollow>();

            var rmGO = new GameObject("ResourceManager");
            var rm = rmGO.AddComponent<ResourceManager>();
            SetPrivateField(rm, "knownResourceTypes", new List<ResourceTypeData> { wood, food, knowledge });

            var playerGO = new GameObject("Player");
            playerGO.tag = "Player";
            playerGO.transform.position = Vector3.zero;
            var playerSr = playerGO.AddComponent<SpriteRenderer>();
            playerSr.sprite = CreateSquareSprite(new Color(0.2f, 0.5f, 1f), "PlayerSprite");
            var rb = playerGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var playerCol = playerGO.AddComponent<CircleCollider2D>();
            playerCol.radius = 0.45f;
            playerGO.AddComponent<PlayerController>();
            playerGO.AddComponent<PlayerInteraction>();

            SetPrivateField(camFollow, "target", playerGO.transform);

            var treeGO = new GameObject("Tree");
            treeGO.transform.position = new Vector3(2f, 1f, 0f);
            var treeSr = treeGO.AddComponent<SpriteRenderer>();
            treeSr.sprite = CreateSquareSprite(new Color(0.2f, 0.6f, 0.2f), "TreeSprite");
            var treeCol = treeGO.AddComponent<BoxCollider2D>();
            treeCol.isTrigger = true;
            var node = treeGO.AddComponent<ResourceNode>();
            SetPrivateField(node, "resourceType", wood);
            SetPrivateField(node, "amountRemaining", 10);
            SetPrivateField(node, "yieldPerHit", 1);

            var gridGO = new GameObject("Grid");
            gridGO.AddComponent<Grid>();

            var previewGO = new GameObject("PlacementPreview");
            var previewSr = previewGO.AddComponent<SpriteRenderer>();
            previewSr.sprite = CreateSquareSprite(new Color(1f, 1f, 1f, 0.4f), "PlacementPreviewSprite");

            var placerGO = new GameObject("BuildingPlacer");
            var placer = placerGO.AddComponent<BuildingPlacer>();
            SetPrivateField(placer, "grid", gridGO.GetComponent<Grid>());
            SetPrivateField(placer, "availableBuildings", new List<BuildingData> { hut, storage });
            SetPrivateField(placer, "placementPreview", previewGO);

            var farmManagerGO = new GameObject("FarmManager");
            farmManagerGO.AddComponent<FarmManager>();

            var tamingSystemGO = new GameObject("TamingSystem");
            tamingSystemGO.AddComponent<TamingSystem>();

            CreateFarmPlot("FarmPlot_1", new Vector3(-2f, -1.5f, 0f));
            CreateFarmPlot("FarmPlot_2", new Vector3(-3.2f, -1.5f, 0f));

            var boarInstance = (GameObject)PrefabUtility.InstantiatePrefab(boarData.prefab);
            boarInstance.name = "WildBoar";
            boarInstance.transform.position = new Vector3(3f, -1.5f, 0f);
            SetPrivateField(boarInstance.GetComponent<AnimalController>(), "data", boarData);

            var techManagerGO = new GameObject("TechManager");
            var techManager = techManagerGO.AddComponent<TechManager>();
            SetPrivateField(techManager, "knowledgeResource", knowledge);
            SetPrivateField(techManager, "knowledgeGenerationInterval", 3f);

            var gameManagerGO = new GameObject("GameManager");
            var gameManager = gameManagerGO.AddComponent<GameManager>();
            SetPrivateField(gameManager, "player", playerGO.GetComponent<PlayerController>());
            SetPrivateField(gameManager, "resourceManager", rm);
            SetPrivateField(gameManager, "buildingPlacer", placer);
            gameManagerGO.AddComponent<SaveLoadHotkeys>();

            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();

            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var textGO = new GameObject("WoodLabel");
            textGO.transform.SetParent(canvasGO.transform, false);
            var rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(300f, 50f);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 28f;
            tmp.text = "Wood: 0";
            var barUI = textGO.AddComponent<ResourceBarUI>();
            SetPrivateField(barUI, "displayedResource", wood);
            SetPrivateField(barUI, "label", tmp);

            CreateResourceLabel(canvasGO.transform, "FoodLabel", new Vector2(20f, -50f), food, "Food: 0");
            CreateResourceLabel(canvasGO.transform, "KnowledgeLabel", new Vector2(20f, -80f), knowledge, "Knowledge: 0");

            CreateBuildMenuPanel(canvasGO.transform, buttonPrefab, hut, storage);
            CreateCropSelectionPanel(canvasGO.transform, buttonPrefab, berry);
            CreateTechTreePanel(canvasGO.transform, buttonPrefab, techFarming);
            CreateNotificationLabel(canvasGO.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GameplaySceneBuilder] Da tao scene tai {ScenePath}");
        }

        private static void CreateFarmPlot(string name, Vector3 position)
        {
            var plotGO = new GameObject(name);
            plotGO.transform.position = position;
            var soilSr = plotGO.AddComponent<SpriteRenderer>();
            soilSr.sprite = CreateSquareSprite(new Color(0.35f, 0.25f, 0.15f), "SoilSprite");
            var col = plotGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            var cropVisualGO = new GameObject("CropVisual");
            cropVisualGO.transform.SetParent(plotGO.transform, false);
            var cropSr = cropVisualGO.AddComponent<SpriteRenderer>();
            cropSr.sortingOrder = 1;

            var plot = plotGO.AddComponent<FarmPlot>();
            SetPrivateField(plot, "cropRenderer", cropSr);
        }

        private static void CreateResourceLabel(Transform canvasTransform, string name, Vector2 anchoredPosition, ResourceTypeData resource, string initialText)
        {
            var labelGO = new GameObject(name);
            labelGO.transform.SetParent(canvasTransform, false);
            var rect = labelGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(300f, 30f);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 20f;
            tmp.text = initialText;
            var barUI = labelGO.AddComponent<ResourceBarUI>();
            SetPrivateField(barUI, "displayedResource", resource);
            SetPrivateField(barUI, "label", tmp);
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

        private static void CreateBuildMenuPanel(Transform canvasTransform, Button buttonPrefab, BuildingData hut, BuildingData storage)
        {
            var container = CreatePanelContainer(canvasTransform, "BuildPanel", new Vector2(-20f, -20f));
            var ui = container.gameObject.AddComponent<BuildMenuUI>();
            SetPrivateField(ui, "availableBuildings", new List<BuildingData> { hut, storage });
            SetPrivateField(ui, "buttonPrefab", buttonPrefab);
            SetPrivateField(ui, "buttonContainer", container);
        }

        private static void CreateCropSelectionPanel(Transform canvasTransform, Button buttonPrefab, CropData berry)
        {
            var container = CreatePanelContainer(canvasTransform, "CropPanel", new Vector2(-20f, -220f));
            var ui = container.gameObject.AddComponent<CropSelectionUI>();
            SetPrivateField(ui, "availableCrops", new List<CropData> { berry });
            SetPrivateField(ui, "buttonPrefab", buttonPrefab);
            SetPrivateField(ui, "buttonContainer", container);
        }

        private static void CreateTechTreePanel(Transform canvasTransform, Button buttonPrefab, TechNode techFarming)
        {
            var container = CreatePanelContainer(canvasTransform, "TechPanel", new Vector2(-20f, -420f));
            var ui = container.gameObject.AddComponent<TechTreeUI>();
            SetPrivateField(ui, "allTechs", new List<TechNode> { techFarming });
            SetPrivateField(ui, "entryButtonPrefab", buttonPrefab);
            SetPrivateField(ui, "entryContainer", container);
        }

        private static Sprite CreateSquareSprite(Color color, string spriteName)
        {
            const int size = 8;
            var tex = new Texture2D(size, size) { name = spriteName };
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = spriteName;
            return sprite;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogError($"[GameplaySceneBuilder] Khong tim thay field '{fieldName}' tren {target.GetType()}");
                return;
            }
            field.SetValue(target, value);
        }
    }
}
