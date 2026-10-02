using System.IO;
using UnityEditor;
using UnityEngine;

namespace PrehistoricTribe.EditorTools
{
    /// <summary>
    /// Sinh texture pixel art cho mặt đất bằng code (thử nghiệm phong cách pixel 2026-10-02): cỏ, đất trại, cát, nước.
    /// Mỗi texture lát liền mạch (nhiễu theo khối có quấn mép), vài màu cố định, lọc Point không làm mịn.
    /// </summary>
    public static class PixelTextureBuilder
    {
        public const string Folder = "Assets/Textures/Pixel";

        public static Texture2D Grass => Load("Grass");
        public static Texture2D Dirt => Load("Dirt");
        public static Texture2D Sand => Load("Sand");
        public static Texture2D Water => Load("Water");

        private static Texture2D Load(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{name}.png");

        [MenuItem("Tools/Prehistoric/Build Pixel Textures")]
        public static void BuildAll()
        {
            Save("Grass", BuildGrass());
            Save("Dirt", BuildDirt());
            Save("Sand", BuildSand());
            Save("Water", BuildWater());
            AssetDatabase.Refresh();
            foreach (var name in new[] { "Grass", "Dirt", "Sand", "Water" }) ConfigureImporter($"{Folder}/{name}.png");
        }

        private static Color C(int rgb) => Palette.Hex(rgb);

        // ─── Nhiễu lát liền mạch ─────────────────────────────────────────────
        /// <summary>Nhiễu giá trị theo khối <paramref name="cell"/> điểm ảnh, nội suy mượt, quấn mép để lát liền.</summary>
        private static float[,] TileNoise(int size, int cell, int seed)
        {
            int cells = size / cell;
            var rng = new System.Random(seed);
            var grid = new float[cells, cells];
            for (int y = 0; y < cells; y++)
            for (int x = 0; x < cells; x++)
                grid[x, y] = (float)rng.NextDouble();

            var noise = new float[size, size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = (float)x / cell, fy = (float)y / cell;
                int x0 = Mathf.FloorToInt(fx) % cells, y0 = Mathf.FloorToInt(fy) % cells;
                int x1 = (x0 + 1) % cells, y1 = (y0 + 1) % cells;
                float tx = Smooth(fx - Mathf.Floor(fx)), ty = Smooth(fy - Mathf.Floor(fy));
                float a = Mathf.Lerp(grid[x0, y0], grid[x1, y0], tx);
                float b = Mathf.Lerp(grid[x0, y1], grid[x1, y1], tx);
                noise[x, y] = Mathf.Lerp(a, b, ty);
            }
            return noise;
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private static Texture2D New(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            return tex;
        }

        private static void Put(Texture2D tex, int x, int y, Color c)
        {
            int s = tex.width;
            tex.SetPixel(((x % s) + s) % s, ((y % s) + s) % s, c);
        }

        /// <summary>Tô theo nhiễu: mỗi điểm ảnh chọn màu theo ngưỡng (kèm hạt nhiễu nhỏ cho đỡ phẳng).</summary>
        private static void Fill(Texture2D tex, float[,] noise, Color[] shades, float[] thresholds, System.Random rng, float grain)
        {
            int s = tex.width;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float v = noise[x, y] + ((float)rng.NextDouble() - 0.5f) * grain;
                int i = 0;
                while (i < thresholds.Length && v > thresholds[i]) i++;
                tex.SetPixel(x, y, shades[i]);
            }
        }

        // ─── Các loại đất ────────────────────────────────────────────────────
        private static Texture2D BuildGrass()
        {
            const int size = 64;
            var tex = New(size);
            var rng = new System.Random(11);
            Color dark = C(0x3f6b2a), mid = C(0x4f8a33), light = C(0x5f9a3a), bright = C(0x7cb446);
            Fill(tex, TileNoise(size, 16, 3), new[] { dark, mid, light }, new[] { 0.32f, 0.68f }, rng, 0.35f);

            // Khóm cỏ: 2 lá chữ V, gốc tối, ngọn sáng.
            for (int i = 0; i < 48; i++)
            {
                int x = rng.Next(size), y = rng.Next(size);
                Put(tex, x, y, dark);
                Put(tex, x - 1, y + 1, mid);
                Put(tex, x + 1, y + 1, mid);
                Put(tex, x - 1, y + 2, bright);
                Put(tex, x + 1, y + 2, bright);
            }
            // Vài bông hoa dại.
            Color[] flowers = { C(0xf2e27a), C(0xf4f0e0), C(0xe9a0b0) };
            for (int i = 0; i < 12; i++)
                Put(tex, rng.Next(size), rng.Next(size), flowers[i % flowers.Length]);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildDirt()
        {
            const int size = 32;
            var tex = New(size);
            var rng = new System.Random(23);
            Fill(tex, TileNoise(size, 8, 5), new[] { C(0x6e5434), C(0x806440), C(0x93764c) }, new[] { 0.33f, 0.66f }, rng, 0.4f);
            // Sỏi: đá xám nhỏ có bóng đổ phía dưới.
            for (int i = 0; i < 9; i++)
            {
                int x = rng.Next(size), y = rng.Next(size);
                Put(tex, x, y, C(0xa39d90));
                Put(tex, x + 1, y, C(0x8f8a80));
                Put(tex, x, y - 1, C(0x5c4a32));
                Put(tex, x + 1, y - 1, C(0x5c4a32));
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildSand()
        {
            const int size = 16;
            var tex = New(size);
            var rng = new System.Random(37);
            Fill(tex, TileNoise(size, 4, 7), new[] { C(0xb59d68), C(0xc9b27c), C(0xd8c48f) }, new[] { 0.35f, 0.7f }, rng, 0.3f);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildWater()
        {
            const int size = 32;
            var tex = New(size);
            var rng = new System.Random(41);
            Fill(tex, TileNoise(size, 16, 9), new[] { C(0x2f6a9a), C(0x3a78aa), C(0x4687b8) }, new[] { 0.4f, 0.7f }, rng, 0.15f);
            // Gợn sóng: nét ngang ngắn sáng màu, so le.
            Color ripple = C(0x8cc4e6);
            for (int i = 0; i < 7; i++)
            {
                int x = rng.Next(size), y = rng.Next(size);
                int length = 3 + rng.Next(3);
                for (int k = 0; k < length; k++) Put(tex, x + k, y + (k == 0 || k == length - 1 ? -1 : 0), ripple);
            }
            tex.Apply();
            return tex;
        }

        // ─── Lưu + cấu hình nhập ─────────────────────────────────────────────
        private static void Save(string name, Texture2D tex)
        {
            string path = $"{Folder}/{name}.png";
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureImporter(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
