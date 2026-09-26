using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PrehistoricTribe.EditorTools
{
    /// <summary>
    /// Tao cac prefab/asset con thieu de test Milestone 1 (dat cong trinh) va
    /// Milestone 3 (chan nuoi): BuildingData_Hut/Storage dang co field prefab
    /// rong, va chua co AnimalData nao ton tai. Chay qua menu
    /// Tools/Prehistoric/Build Missing Content truoc khi chay GameplaySceneBuilder.
    /// </summary>
    public static class GameContentBuilder
    {
        private const string HutDataPath = "Assets/_Data/BuildingData_Hut.asset";
        private const string StorageDataPath = "Assets/_Data/BuildingData_Storage.asset";
        private const string FoodAssetPath = "Assets/_Data/ResourceType_Food.asset";
        private const string HutPrefabPath = "Assets/Prefabs/Buildings/Hut.prefab";
        private const string StoragePrefabPath = "Assets/Prefabs/Buildings/Storage.prefab";
        private const string BoarDataPath = "Assets/_Data/AnimalData_WildBoar.asset";
        private const string BoarPrefabPath = "Assets/Prefabs/Animals/WildBoar.prefab";
        private const string BerryDataPath = "Assets/_Data/CropData_Berry.asset";

        [MenuItem("Tools/Prehistoric/Build Missing Content (Buildings + Animal)")]
        public static void Build()
        {
            var food = AssetDatabase.LoadAssetAtPath<ResourceTypeData>(FoodAssetPath);
            if (food == null)
            {
                Debug.LogError($"[GameContentBuilder] Khong tim thay {FoodAssetPath}. Dung lai.");
                return;
            }

            BuildBuildingPrefab(HutDataPath, HutPrefabPath, new Color(0.55f, 0.35f, 0.15f), "HutSprite");
            BuildBuildingPrefab(StorageDataPath, StoragePrefabPath, new Color(0.5f, 0.5f, 0.5f), "StorageSprite");
            BuildWildBoarContent(food);
            BuildCropVisuals();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameContentBuilder] Da tao xong Hut.prefab, Storage.prefab, WildBoar.prefab, AnimalData_WildBoar.asset va sprite giai doan cho CropData_Berry");
        }

        private static void BuildBuildingPrefab(string dataAssetPath, string prefabPath, Color color, string spriteName)
        {
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(dataAssetPath);
            if (data == null)
            {
                Debug.LogError($"[GameContentBuilder] Khong tim thay {dataAssetPath}");
                return;
            }

            var go = new GameObject(data.displayName);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateAndSaveSprite(color, spriteName);
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<BuildingInstance>();

            EnsureFolder(prefabPath);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            data.prefab = prefab;
            EditorUtility.SetDirty(data);
        }

        private static void BuildWildBoarContent(ResourceTypeData food)
        {
            var go = new GameObject("WildBoar");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateAndSaveSprite(new Color(0.6f, 0.4f, 0.2f), "WildBoarSprite");
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            go.AddComponent<AnimalController>();

            EnsureFolder(BoarPrefabPath);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, BoarPrefabPath);
            Object.DestroyImmediate(go);

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

        private static void BuildCropVisuals()
        {
            var crop = AssetDatabase.LoadAssetAtPath<CropData>(BerryDataPath);
            if (crop == null)
            {
                Debug.LogError($"[GameContentBuilder] Khong tim thay {BerryDataPath}");
                return;
            }

            crop.seedSprite = CreateAndSaveSprite(new Color(0.4f, 0.3f, 0.15f), "BerrySeedSprite");
            crop.sproutSprite = CreateAndSaveSprite(new Color(0.4f, 0.7f, 0.2f), "BerrySproutSprite");
            crop.matureSprite = CreateAndSaveSprite(new Color(0.8f, 0.1f, 0.2f), "BerryMatureSprite");
            crop.witheredSprite = CreateAndSaveSprite(new Color(0.4f, 0.35f, 0.3f), "BerryWitheredSprite");
            EditorUtility.SetDirty(crop);
        }

        private static void EnsureFolder(string assetPath)
        {
            string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        /// <summary>
        /// PrefabUtility.SaveAsPrefabAsset khong tu nhung duoc Sprite/Texture2D tao
        /// runtime (khac voi scene, EditorSceneManager.SaveScene nhung duoc binh thuong).
        /// Nen phai ghi ra file .png that roi import lai nhu mot Sprite asset chuan.
        /// </summary>
        private static Sprite CreateAndSaveSprite(Color color, string spriteName)
        {
            const int size = 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string path = $"Assets/Sprites/Generated/{spriteName}.png";
            EnsureFolder(path);
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
