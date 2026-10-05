using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PrehistoricTribe.EditorTools
{
    /// <summary>
    /// Sinh icon cho giao diện (thiết kế lại UI theo .claude/RE-DESIGNUI.md, 2026-10-02):
    /// - Công trình / cây trồng: chụp model 3D thật (góc nhìn chéo, nền trong suốt, 64px lọc Point → hợp phong cách pixel).
    /// - Công nghệ: dùng icon của thứ đầu tiên nó mở khóa (ưu tiên cây trồng).
    /// - Tài nguyên + ổ khóa + chấm báo: vẽ pixel 16×16 bằng bản đồ ký tự.
    /// Gán thẳng vào trường <c>icon</c> của từng asset dữ liệu. Thay icon riêng: sửa trường icon trong Inspector
    /// (sẽ bị ghi đè khi Build All) hoặc đặt PNG cùng tên vào <see cref="CustomFolder"/>.
    /// </summary>
    public static class IconBuilder
    {
        public const string Folder = "Assets/Textures/Icons";
        public const string CustomFolder = "Assets/Textures/Icons/Custom";
        public const string LockPath = Folder + "/Lock.png";
        public const string DotPath = Folder + "/Dot.png";
        public const string PeoplePath = Folder + "/People.png";
        private const int ModelIconSize = 64;

        [MenuItem("Tools/Prehistoric/Build Icons")]
        public static void BuildAll()
        {
            // Chụp model cần mở scene trống — chạy từ menu thì hỏi lưu scene đang sửa trước.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Folder);
            var written = new List<string>();

            // ─── Pixel 16×16 ─────────────────────────────────────────────────
            foreach (var pair in PixelIcons)
                written.Add(SavePng($"{Folder}/{pair.Key}.png", Draw(pair.Value)));

            // ─── Model 3D ────────────────────────────────────────────────────
            // Mở scene trống sẽ dọn asset không còn ai giữ khỏi bộ nhớ → lưu dữ liệu vừa sửa trước, nạp lại sau.
            AssetDatabase.SaveAssets();
            var buildings = LoadAll<BuildingData>();
            var crops = LoadAll<CropData>();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var studio = SetupStudio();
            foreach (var building in buildings)
                if (building.prefab != null) written.Add(SavePng($"{Folder}/Building_{building.id}.png", RenderModel(studio, building.prefab)));
            foreach (var crop in crops)
                if (crop.matureModel != null) written.Add(SavePng($"{Folder}/Crop_{crop.id}.png", RenderModel(studio, crop.matureModel)));
            Object.DestroyImmediate(studio.gameObject);

            AssetDatabase.Refresh();
            foreach (var path in written) ConfigureSprite(path);

            // ─── Gán vào dữ liệu ─────────────────────────────────────────────
            buildings = LoadAll<BuildingData>();
            crops = LoadAll<CropData>();
            foreach (var building in buildings) Assign(building, $"Building_{building.id}");
            foreach (var crop in crops) Assign(crop, $"Crop_{crop.id}");
            foreach (var type in LoadAll<ResourceTypeData>()) Assign(type, $"Resource_{type.id}");
            foreach (var tech in LoadAll<TechNode>())
            {
                CropData crop = crops.Find(c => tech.unlockedCropIds.Contains(c.id));
                BuildingData building = buildings.Find(b => tech.unlockedBuildingIds.Contains(b.id));
                tech.icon = Custom($"Tech_{tech.id}") ?? (crop != null ? crop.icon : building != null ? building.icon : null);
                EditorUtility.SetDirty(tech);
            }
            AssetDatabase.SaveAssets();
        }

        public static Sprite Lock => AssetDatabase.LoadAssetAtPath<Sprite>(LockPath);
        public static Sprite Dot => AssetDatabase.LoadAssetAtPath<Sprite>(DotPath);
        public static Sprite People => AssetDatabase.LoadAssetAtPath<Sprite>(PeoplePath);

        private static List<T> LoadAll<T>() where T : Object
        {
            var list = new List<T>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/_Data" }))
                list.Add(AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)));
            return list;
        }

        private static Sprite Custom(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{CustomFolder}/{name}.png");

        private static void Assign(Object data, string name)
        {
            Sprite sprite = Custom(name) ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");
            switch (data)
            {
                case BuildingData b: b.icon = sprite; break;
                case CropData c: c.icon = sprite; break;
                case ResourceTypeData r: r.icon = sprite; break;
            }
            EditorUtility.SetDirty(data);
        }

        // ─── Chụp model ──────────────────────────────────────────────────────
        private static Camera SetupStudio()
        {
            var root = new GameObject("IconStudio");
            var light = new GameObject("Key").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.transform.SetParent(root.transform);
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);

            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.transform.SetParent(root.transform);
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.transform.rotation = Quaternion.Euler(30f, 45f, 0f); // góc nhìn chéo như trong game
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
            return cam;
        }

        private static Texture2D RenderModel(Camera cam, GameObject prefab)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = Vector3.zero;

            var bounds = new Bounds();
            bool any = false;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.gameObject.activeInHierarchy || !renderer.enabled) continue;
                if (any) bounds.Encapsulate(renderer.bounds);
                else { bounds = renderer.bounds; any = true; }
            }

            // Khung vừa khít model: chiếu 8 góc hộp bao lên mặt phẳng camera.
            Vector3 center = bounds.center;
            float half = 0.1f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 local = cam.transform.InverseTransformDirection(corner - center);
                half = Mathf.Max(half, Mathf.Abs(local.x), Mathf.Abs(local.y));
            }
            cam.orthographicSize = half * 1.08f;
            cam.transform.position = center - cam.transform.forward * 30f;

            var rt = RenderTexture.GetTemporary(ModelIconSize, ModelIconSize, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(ModelIconSize, ModelIconSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, ModelIconSize, ModelIconSize), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(instance);
            return Outline(tex);
        }

        /// <summary>Viền tối 1px quanh hình (giống viền của hậu kỳ pixel trong game) cho icon nổi trên nền.</summary>
        private static Texture2D Outline(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            Color32[] src = tex.GetPixels32();
            var dst = (Color32[])src.Clone();
            var edge = new Color32(36, 26, 18, 255);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (src[y * w + x].a > 40) continue;
                bool nearSolid = false;
                for (int dy = -1; dy <= 1 && !nearSolid; dy++)
                for (int dx = -1; dx <= 1 && !nearSolid; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    nearSolid = src[ny * w + nx].a > 40;
                }
                if (nearSolid) dst[y * w + x] = edge;
            }
            tex.SetPixels32(dst);
            tex.Apply();
            return tex;
        }

        // ─── Icon pixel vẽ tay ───────────────────────────────────────────────
        private static readonly Dictionary<char, Color32> Ink = new Dictionary<char, Color32>
        {
            { 'k', new Color32(42, 29, 20, 255) },    // viền
            { 'b', new Color32(138, 90, 43, 255) },   // nâu
            { 'B', new Color32(192, 138, 74, 255) },  // nâu sáng
            { 'y', new Color32(242, 208, 107, 255) }, // vàng
            { 'Y', new Color32(217, 165, 49, 255) },  // vàng đậm
            { 'g', new Color32(79, 138, 51, 255) },   // xanh lá
            { 'G', new Color32(124, 180, 70, 255) },  // xanh lá sáng
            { 'r', new Color32(194, 59, 46, 255) },   // đỏ
            { 'R', new Color32(232, 112, 90, 255) },  // đỏ sáng
            { 'w', new Color32(244, 240, 224, 255) }, // trắng ngà
            { 's', new Color32(217, 160, 102, 255) }, // da
            { 'h', new Color32(74, 48, 32, 255) },    // tóc
            { 'd', new Color32(110, 84, 52, 255) },   // đất
            { 'm', new Color32(150, 150, 150, 255) }, // xám kim loại
        };

        private static readonly Dictionary<string, string[]> PixelIcons = new Dictionary<string, string[]>
        {
            { "Resource_wood", new[] {
                "................",
                "................",
                "....kkkkkkkkk...",
                "...kBkbbbbbbbk..",
                "..kyykbbbbbbbbk.",
                "..kyBykbbbbbbbk.",
                "..kyykbbbbbbbbk.",
                "...kkkkkkkkkkk..",
                "..kkkkkkkkkk....",
                ".kBkbbbbbbbbk...",
                "kyykbbbbbbbbbk..",
                "kyBykbbbbbbbbk..",
                "kyykbbbbbbbbbk..",
                ".kkkkkkkkkkkk...",
                "................",
                "................" } },
            { "Resource_food", new[] {
                "................",
                ".......gg.......",
                "......gGGg......",
                ".......gk.......",
                ".....kkkkkk.....",
                "....kRrrkRrk....",
                "...kRRrrkrRrk...",
                "...krrrrkrrrk...",
                "...kkrrkkkrrk...",
                "..kRrkkRrrkk....",
                "..kRRrkRRrrk....",
                "..krrrkrrrrk....",
                "...kkkkkrrk.....",
                "........kk......",
                "................",
                "................" } },
            { "Resource_knowledge", new[] {
                "................",
                ".......kk.......",
                "......kyyk......",
                ".....kyyyyk.....",
                "..kk.kyYYyk.kk..",
                "..kykkyYYykkyk..",
                "...kyyyyyyyyk...",
                "....kyyYYyyk....",
                "...kyyyYYyyyk...",
                "..kyyyyyyyyyyk..",
                "...kkkyyyykkk...",
                ".....kyyyyk.....",
                "....kyykkyyk....",
                "....kyk..kyk....",
                "....kk....kk....",
                "................" } },
            { "People", new[] {
                "................",
                "......kkkk......",
                ".....khhhhk.....",
                ".....khsshk.....",
                ".....kssssk.....",
                "......kssk......",
                "....kkkkkkkk....",
                "...kBBBBBBBBk...",
                "..ksBBBBBBBBsk..",
                "..ksBBBBBBBBsk..",
                "..kskBBBBBBksk..",
                "...k.kBBBBk.k...",
                ".....kbkkbk.....",
                ".....kbk.kbk....",
                ".....kk...kk....",
                "................" } },
            { "Resource_rice_sheaf", new[] {
                "................",
                "....y.y.y.y.....",
                "...yYyYyYyYy....",
                "....yYyYyYy.....",
                "....kyyyyyk.....",
                ".....kyyyk......",
                ".....kyyyk......",
                ".....kbbbk......",
                ".....kyyyk......",
                "....kyyyyyk.....",
                "...kyyYyYyyk....",
                "...kyYyyyYyk....",
                "..kyyyyyyyyyk...",
                "..kkkkkkkkkkk...",
                "................",
                "................" } },
            { "Resource_rice_seed", new[] {
                "................",
                "................",
                "................",
                ".......kk.......",
                "......kYyk......",
                "....kk.kk.kk....",
                "...kYyk..kYyk...",
                "....kk.kk.kk....",
                "......kYyk......",
                "...kk..kk..kk...",
                "..kYyk....kYyk..",
                "...kk.kk.kk.....",
                ".....kYyk.......",
                "......kk........",
                "................",
                "................" } },
            { "Resource_rice_seedling", new[] {
                "................",
                "................",
                "....G.....G.....",
                "...GgG...GgG....",
                "....gG..Gg......",
                ".....g..g...G...",
                "..G..gg.g..GgG..",
                ".GgG..g.g..g....",
                "..gG..g.g.gg....",
                "...g..g.g.g.....",
                "...gg.g.g.g.....",
                "....g.g.g.g.....",
                "..kkkkkkkkkkkk..",
                "..kddddddddddk..",
                "..kkkkkkkkkkkk..",
                "................" } },
            { "Resource_manure", new[] {
                "................",
                "................",
                "................",
                "................",
                ".......kk.......",
                "......kbbk......",
                ".....kbBbbk.....",
                "....kkkbbkkk....",
                "...kbbbbbbbbk...",
                "...kbBbbbbbbk...",
                "..kkkkbbbbkkkk..",
                ".kbbbbbbbbbbbbk.",
                ".kbBbbbbbbbBbbk.",
                "..kkkkkkkkkkkk..",
                "................",
                "................" } },
            { "Lock", new[] {
                "................",
                "................",
                ".....kkkkk......",
                "....kmmmmmk.....",
                "....kmk.kmk.....",
                "....kmk.kmk.....",
                "...kkkkkkkkk....",
                "...kYYYYYYYk....",
                "...kYYYkYYYk....",
                "...kYYYkYYYk....",
                "...kYYYYYYYk....",
                "...kkkkkkkkk....",
                "................",
                "................",
                "................",
                "................" } },
            { "Dot", new[] {
                "................",
                "................",
                "................",
                "................",
                "......kkkk......",
                ".....kRRrrk.....",
                "....kRRrrrrk....",
                "....krrrrrrk....",
                "....krrrrrrk....",
                "....krrrrrrk....",
                ".....krrrrk.....",
                "......kkkk......",
                "................",
                "................",
                "................",
                "................" } },
        };

        private static Texture2D Draw(string[] rows)
        {
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
            {
                string row = y < rows.Length ? rows[y] : string.Empty;
                for (int x = 0; x < size; x++)
                {
                    char c = x < row.Length ? row[x] : '.';
                    // Dòng đầu bản đồ = hàng trên cùng của ảnh (texture tính y từ dưới lên).
                    tex.SetPixel(x, size - 1 - y, Ink.TryGetValue(c, out var color) ? color : clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static string SavePng(string path, Texture2D tex)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            return path;
        }

        private static void ConfigureSprite(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
