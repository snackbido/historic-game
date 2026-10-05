using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PrehistoricTribe.EditorTools
{
    /// <summary>
    /// Nhập model thiên nhiên có sẵn (gói "Ultimate Stylized Nature" của Quaternius, CC0) để thay cho khối hình
    /// LandscapeBuilder tự vẽ: cây, bụi, đá, cỏ, hoa giờ là model thật, nhưng vật liệu được nhuốm theo
    /// <see cref="LandscapePalette"/> — cùng bảng màu với mặt đất — để cảnh vẫn "đồng bộ" như trước, chỉ đẹp hơn.
    /// Model đặt sẵn ở Assets/Environment/QuaterniusNature/{Models,Textures} (đã chọn lọc, không lấy nguyên gói).
    /// </summary>
    public static class NatureAssetBuilder
    {
        private const string ModelsFolder = "Assets/Environment/QuaterniusNature/Models";
        private const string TexturesFolder = "Assets/Environment/QuaterniusNature/Textures";
        private const string MaterialFolder = "Assets/Materials/Generated/Nature";

        public static readonly string[] Broadleaf = { "NormalTree_1", "NormalTree_2", "NormalTree_3", "NormalTree_4", "NormalTree_5" };
        public static readonly string[] Birch = { "BirchTree_1", "BirchTree_2", "BirchTree_3", "BirchTree_4", "BirchTree_5" };
        public static readonly string[] Pine = { "PineTree_1", "PineTree_2", "PineTree_3", "PineTree_4", "PineTree_5" };
        public static readonly string[] Maple = { "MapleTree_1", "MapleTree_2", "MapleTree_3" };
        public static readonly string[] DeadTree = { "DeadTree_1", "DeadTree_4", "DeadTree_7", "DeadTree_9" };
        public static readonly string[] Bush = { "Bush", "Bush_Small", "Bush_Large" };
        public static readonly string[] BushFlowering = { "Bush_Flowers", "Bush_Small_Flowers", "Bush_Large_Flowers" };
        public static readonly string[] Rocks = { "Rock_1", "Rock_2", "Rock_3", "Rock_4", "Rock_5" };
        // "Grass_Large" bỏ qua: slot vật liệu bên trong FBX không có tên "Grass" như hai cái kia nên remap không khớp.
        public static readonly string[] Grass = { "Grass_Small", "Grass_Large_Extruded" };
        public static readonly string[] Flowers = { "Flower_1", "Flower_2", "Flower_1_Clump", "Flower_2_Clump", "Flower_3_Clump" };

        /// <summary>Tên slot vật liệu gốc trong từng FBX (dò bằng TempMaterialProbe) → tên vật liệu của ta sẽ gán vào.</summary>
        private static readonly Dictionary<string, string[]> FamilySlots = new Dictionary<string, string[]>
        {
            ["Broadleaf"] = new[] { "NormalTree_Bark", "NormalTree_Leaves" },
            ["Birch"] = new[] { "BirchTree_Bark", "BirchTree_Leaves" },
            ["Pine"] = new[] { "PineTree_Bark", "PineTree_Leaves" },
            ["Maple"] = new[] { "MapleTree_Bark", "MapleTree_Leaves" },
            ["DeadTree"] = new[] { "NormalTree_Bark" },
            ["Bush"] = new[] { "Bush_Leaves" },
            ["BushFlowering"] = new[] { "Bush_Leaves", "Flowers" },
            ["Rocks"] = new[] { "Rock" },
            ["Grass"] = new[] { "Grass" },
            ["Flowers"] = new[] { "Flowers" },
        };

        public static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/{name}.fbx");

        public static GameObject Pick(string[] family, System.Random rng) => Load(family[rng.Next(family.Length)]);

        /// <summary>Tạo vật liệu (nhuốm theo bảng màu chung) và gán vào từng FBX. Gọi một lần trước khi dựng landscape.</summary>
        public static void Setup()
        {
            var materials = EnsureMaterials();
            EnsureImportSettings(Broadleaf, "Broadleaf", materials);
            EnsureImportSettings(Birch, "Birch", materials);
            EnsureImportSettings(Pine, "Pine", materials);
            EnsureImportSettings(Maple, "Maple", materials);
            EnsureImportSettings(DeadTree, "DeadTree", materials);
            EnsureImportSettings(Bush, "Bush", materials);
            EnsureImportSettings(BushFlowering, "BushFlowering", materials);
            EnsureImportSettings(Rocks, "Rocks", materials);
            EnsureImportSettings(Grass, "Grass", materials);
            EnsureImportSettings(Flowers, "Flowers", materials);
        }

        private static Dictionary<string, Material> EnsureMaterials()
        {
            EnsureFolder(MaterialFolder);
            var m = new Dictionary<string, Material>();
            // Vỏ cây: giữ phần lớn màu gốc của texture, chỉ nhuốm nhẹ theo TrunkColor cho ấm và khớp màu khúc gỗ/thân chết sẵn có.
            m["NormalTree_Bark"] = Opaque("NormalTree_Bark", "NormalTree_Bark.jpg", LandscapePalette.TrunkColor, 0.18f);
            m["BirchTree_Bark"] = Opaque("BirchTree_Bark", "BirchTree_Bark.jpg", LandscapePalette.BirchBark, 0.15f);
            m["PineTree_Bark"] = Opaque("PineTree_Bark", "PineTree_Bark.jpg", LandscapePalette.TrunkDark, 0.18f);
            m["MapleTree_Bark"] = Opaque("MapleTree_Bark", "MapleTree_Bark.jpg", LandscapePalette.TrunkColor, 0.15f);
            // Lá: nhuốm theo đúng màu lá cây tương ứng trong bảng màu địa hình (rừng, đồi) để tán cây hợp màu nền rừng.
            m["NormalTree_Leaves"] = Cutout("NormalTree_Leaves", "NormalTree_Leaves.png", LandscapePalette.Leaf, 0.32f);
            m["BirchTree_Leaves"] = Cutout("BirchTree_Leaves", "BirchTree_Leaves.png", LandscapePalette.LeafLight, 0.28f);
            m["PineTree_Leaves"] = Cutout("PineTree_Leaves", "PineTree_Leaves.png", LandscapePalette.Pine, 0.32f);
            m["MapleTree_Leaves"] = Cutout("MapleTree_Leaves", "MapleTree_Leaves.png", LandscapePalette.LeafAutumn, 0.2f);
            m["Bush_Leaves"] = Cutout("Bush_Leaves", "Bush_Leaves.png", LandscapePalette.Leaf, 0.25f);
            m["Flowers"] = Cutout("Flowers", "Flowers.png", LandscapePalette.FlowerColors[0], 0.12f);
            m["Rock"] = Opaque("Rock", "Rocks.jpg", LandscapePalette.Rock, 0.3f);
            m["Grass"] = Cutout("Grass", "Grass.png", LandscapePalette.GrassMid, 0.25f);
            return m;
        }

        private static Material Opaque(string name, string textureFile, Color tint, float tintAmount)
        {
            var mat = LoadOrCreateMaterial(name);
            mat.shader = Shader.Find("Standard");
            mat.SetFloat("_Glossiness", 0.05f);
            mat.SetFloat("_Metallic", 0f);
            mat.mainTexture = LoadTexture(textureFile, alpha: false);
            mat.color = Color.Lerp(Color.white, tint, tintAmount);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material Cutout(string name, string textureFile, Color tint, float tintAmount)
        {
            var mat = LoadOrCreateMaterial(name);
            mat.shader = Shader.Find("Standard");
            mat.SetFloat("_Glossiness", 0.05f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Mode", 1f); // Cutout
            mat.SetFloat("_Cutoff", 0.45f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            mat.mainTexture = LoadTexture(textureFile, alpha: true);
            mat.color = Color.Lerp(Color.white, tint, tintAmount);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material LoadOrCreateMaterial(string name)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Texture2D LoadTexture(string fileName, bool alpha)
        {
            string path = $"{TexturesFolder}/{fileName}";
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                bool changed = importer.textureType != TextureImporterType.Default
                    || importer.maxTextureSize != 512 || importer.alphaIsTransparency != alpha || importer.mipmapEnabled != true;
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = alpha;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Compressed;
                if (changed) importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void EnsureImportSettings(string[] names, string family, Dictionary<string, Material> materials)
        {
            string[] slots = FamilySlots[family];
            foreach (var name in names)
            {
                string path = $"{ModelsFolder}/{name}.fbx";
                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;
                // Chốt cứng useFileScale=false, globalScale=1 (đúng bằng kích thước gốc đo được lần import đầu tiên) —
                // SaveAndReimport() qua script có lúc tự bật useFileScale=true, khiến cây/đá co lại ~150 lần.
                bool changed = importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard
                    || importer.importAnimation || importer.addCollider || importer.isReadable || !importer.weldVertices
                    || importer.useFileScale || !Mathf.Approximately(importer.globalScale, 1f);
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.importAnimation = false;
                importer.addCollider = false;
                importer.isReadable = false;
                importer.weldVertices = true;
                importer.useFileScale = false;
                importer.globalScale = 1f;
                foreach (var slot in slots)
                {
                    if (!materials.TryGetValue(slot, out var mat)) continue;
                    var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), slot);
                    if (!importer.GetExternalObjectMap().TryGetValue(id, out var current) || current != mat)
                    {
                        importer.AddRemap(id, mat);
                        changed = true;
                    }
                }
                if (changed) importer.SaveAndReimport();
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
