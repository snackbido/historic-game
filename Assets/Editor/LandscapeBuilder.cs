using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PrehistoricTribe.EditorTools
{
    /// <summary>
    /// Dựng địa hình theo .claude/LANDSCAPE_DESIGN.md (2026-10-05): bản đồ 96×96m, làng ở đồng bằng giữa, núi phía bắc có
    /// khe sông chảy qua, rừng hai bên trên đồi, vài gò thấp ngay trong khu chơi, sông uốn khúc phía tây làng (có chỗ cạn
    /// lội qua), bãi cát + biển phía nam, đường mòn đất nối làng với rừng / chỗ lội / bãi biển / ao.
    /// - Mặt đất: lưới độ cao 2m → mesh low-poly (mỗi tam giác một màu theo vùng: cỏ, nền rừng, đất mòn, cát, đá).
    ///   Lưới độ cao lưu vào <see cref="WorldTerrain"/> để gameplay hỏi độ cao — không dùng collider.
    /// - Nước: một mặt nước chung ở mực nước (sông + biển = chỗ đất thấp hơn mực nước).
    /// - Cây/bụi/đá/cỏ/hoa/lau/khúc gỗ: sinh theo vùng + cụm, gộp thành ít mesh màu-đỉnh theo ô 16m (máy yếu vẫn chạy).
    /// Mọi thứ cố định theo seed → dựng lại ra y hệt (không làm bẩn git).
    /// </summary>
    public static class LandscapeBuilder
    {
        // ─── Thông số bản đồ ─────────────────────────────────────────────────
        public const float MapHalf = 48f;
        public const float Cell = 1f;
        public const float WaterLevel = -0.55f;
        private const int Resolution = (int)(MapHalf * 2f / Cell) + 1; // 49 đỉnh mỗi cạnh
        private const float ChunkSize = 16f;
        private const string MeshFolder = "Assets/Models/Generated/Landscape";
        private const string GrainTexturePath = "Assets/Textures/Pixel/Grain.png";
        private const string MaterialPath = "Assets/Materials/Generated/LandscapeVertexColor.mat";

        /// <summary>Tâm làng (đống lửa ở (0, -2)) — vùng này giữ gần phẳng để xây dễ.</summary>
        public static readonly Vector2 VillageCenter = new Vector2(0f, -1f);
        public static readonly Vector2 PondCenter = new Vector2(4.5f, 6f);

        private static readonly Vector2[] River =
        {
            new Vector2(-4f, 50f), new Vector2(-7f, 38f), new Vector2(-12f, 29f), new Vector2(-19f, 21f),
            new Vector2(-24f, 11f), new Vector2(-22f, 1f), new Vector2(-25f, -9f), new Vector2(-21f, -20f),
            new Vector2(-15f, -28f), new Vector2(-13f, -50f),
        };
        private static readonly Vector2 Ford = new Vector2(-22.5f, -2f);

        private static readonly Vector2[][] Paths =
        {
            new[] { new Vector2(-1f, 0f), new Vector2(-7f, 5f), new Vector2(-14f, 11f), new Vector2(-21f, 15f) },        // lên rừng tây bắc
            new[] { new Vector2(-2f, -2f), new Vector2(-9f, -3f), new Vector2(-16f, -2.5f), new Vector2(-22.5f, -2f) },  // ra chỗ lội sông
            new[] { new Vector2(1f, -5f), new Vector2(3f, -12f), new Vector2(4f, -19f), new Vector2(6f, -26f) },         // xuống bãi biển
            new[] { new Vector2(1.5f, 1f), new Vector2(3.5f, 3.8f) },                                                    // ra ao
            new[] { new Vector2(3f, -2f), new Vector2(10f, -3f), new Vector2(18f, -2f), new Vector2(27f, -4f) },        // sang rừng phía đông
        };

        private struct Bump { public Vector2 c; public float amp, sigma; }

        private static readonly Bump[] Hills =
        {
            // Đồi rừng hai bên
            new Bump { c = new Vector2(-25f, 16f), amp = 3.6f, sigma = 6.5f },
            new Bump { c = new Vector2(24f, 16f), amp = 3.2f, sigma = 6f },
            new Bump { c = new Vector2(33f, -6f), amp = 2.8f, sigma = 6f },
            new Bump { c = new Vector2(-36f, -8f), amp = 2.4f, sigma = 5f },
            new Bump { c = new Vector2(30f, -22f), amp = 1.8f, sigma = 5f },
            // Gò thấp ngay trong khu chơi (đi lên được, xây trên đỉnh tròn được)
            new Bump { c = new Vector2(13f, -12f), amp = 2.2f, sigma = 3.8f },
            new Bump { c = new Vector2(-12f, -15f), amp = 1.8f, sigma = 3.4f },
            new Bump { c = new Vector2(14f, 8f), amp = 1.6f, sigma = 3.4f },
        };

        // ─── Bảng màu (theo LANDSCAPE_DESIGN §2.2, khớp texture pixel sẵn có) ─
        private static readonly Color GrassDark = Hex(0x3f6b2a), GrassMid = Hex(0x4f8a33), GrassLight = Hex(0x5f9a3a), GrassYellow = Hex(0x7cb446);
        private static readonly Color ForestFloor = Hex(0x355c25), ForestFloorLight = Hex(0x41692b);
        private static readonly Color Dirt = Hex(0x806440), DirtDark = Hex(0x6e5434), DryDirt = Hex(0x93764c);
        private static readonly Color Sand = Hex(0xc9b27c), SandLight = Hex(0xd8c48f), WetSand = Hex(0xa8935f), Mud = Hex(0x5c4a32);
        private static readonly Color RockDark = Hex(0x5e5a55), Rock = Hex(0x7d7a74), RockLight = Hex(0xa39d90), MossStone = Hex(0x6b7a55);
        private static readonly Color TrunkColor = Hex(0x6b4a2b), TrunkDark = Hex(0x4e3520), BirchBark = Hex(0xd8d2c0);
        private static readonly Color PineDark = Hex(0x2f5a2c), Pine = Hex(0x3b6e33), PineLight = Hex(0x4f8a3a);
        private static readonly Color LeafDark = Hex(0x3f7a2e), Leaf = Hex(0x56963a), LeafLight = Hex(0x74b048), LeafAutumn = Hex(0xa8a03a);
        private static readonly Color Reed = Hex(0x8a9a48), ReedTop = Hex(0x7a5a32);
        private static readonly Color[] FlowerColors = { Hex(0xf2e27a), Hex(0xf4f0e0), Hex(0xe9a0b0), Hex(0xb08ad8) };

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f);

        // ─── Độ cao ──────────────────────────────────────────────────────────
        public static float Height(float x, float z)
        {
            var p = new Vector2(x, z);
            float distVillage = Vector2.Distance(p, VillageCenter);

            // Đồng bằng nhấp nhô nhẹ, xa làng thì gồ ghề hơn.
            float rough = Smooth(12f, 26f, distVillage);
            float h = 0.35f * Fbm(x * 0.06f, z * 0.06f, 3) + rough * 0.8f * Fbm(x * 0.11f + 40f, z * 0.11f, 3);

            foreach (var hill in Hills)
                h += hill.amp * Mathf.Exp(-(p - hill.c).sqrMagnitude / (2f * hill.sigma * hill.sigma));

            // Làng: giữ gần phẳng để xây dễ.
            h = Mathf.Lerp(h * 0.2f, h, Smooth(9f, 15f, distVillage));
            // Ao cá: mặt bằng quanh ao.
            float distPond = Vector2.Distance(p, PondCenter);
            h = Mathf.Lerp(0.02f, h, Smooth(2f, 3.6f, distPond));

            // Núi phía bắc, có khe theo dòng sông.
            float riverDist = DistanceToPolyline(p, River, out float along);
            float mountain = Smooth(24f, 38f, z) * (8f + 6f * Ridged(x * 0.07f, z * 0.07f));
            float pass = Mathf.Exp(-riverDist * riverDist / (2f * 5f * 5f));
            h += mountain * (1f - 0.8f * pass);

            // Bờ biển uốn lượn: biển rộng phía nam, hai mép đông/tây và mép bắc (sau núi) cũng thoải xuống nước →
            // vùng đất trông như một hòn đảo tự nhiên, không phải miếng vuông cắt thẳng.
            float coast = Mathf.Min(Mathf.Min(MapHalf - Mathf.Abs(x), z + 33f), MapHalf + 2f - z);
            coast += 4.5f * Fbm(x * 0.05f + 70f, z * 0.05f + 20f, 3);
            h = Mathf.Lerp(-2.6f, h, Smooth(0f, 9f, coast));

            // Sông: lòng sông thấp hơn mực nước, bờ thoải; chỗ cạn lội qua được.
            float fordFactor = Mathf.Exp(-(p - Ford).sqrMagnitude / (2f * 2.2f * 2.2f));
            float bed = Mathf.Lerp(-1.5f, WaterLevel - 0.18f, fordFactor);
            // Bờ thoải: đất ven sông hạ dần về sát mặt nước (bãi bồi), rồi mới xuống lòng sông.
            float bankMask = 1f - Smooth(2.5f, 7.5f, riverDist);
            h = Mathf.Lerp(h, Mathf.Min(h, WaterLevel + 0.25f), bankMask);
            float riverMask = 1f - Smooth(1.4f, 3f, riverDist);
            h = Mathf.Lerp(h, Mathf.Min(h, bed), riverMask);
            return h;
        }

        private static float Smooth(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Nhiễu fractal trong khoảng ~[-1, 1].</summary>
        private static float Fbm(float x, float z, int octaves)
        {
            float sum = 0f, amp = 0.5f, freq = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * (Mathf.PerlinNoise(x * freq + 13.7f * i, z * freq + 7.3f * i) * 2f - 1f);
                norm += amp;
                amp *= 0.5f;
                freq *= 2.1f;
            }
            return sum / norm;
        }

        /// <summary>Nhiễu "sống núi" [0, 1].</summary>
        private static float Ridged(float x, float z)
        {
            float n = 1f - Mathf.Abs(Fbm(x, z, 4));
            return n * n;
        }

        private static float DistanceToPolyline(Vector2 p, Vector2[] line, out float along)
        {
            float best = float.PositiveInfinity;
            along = 0f;
            float walked = 0f;
            for (int i = 0; i < line.Length - 1; i++)
            {
                Vector2 a = line[i], b = line[i + 1];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                float d = Vector2.Distance(p, a + ab * t);
                if (d < best)
                {
                    best = d;
                    along = walked + ab.magnitude * t;
                }
                walked += ab.magnitude;
            }
            return best;
        }

        private static float PathDistance(Vector2 p)
        {
            float best = float.PositiveInfinity;
            foreach (var path in Paths) best = Mathf.Min(best, DistanceToPolyline(p, path, out _));
            return best;
        }

        /// <summary>Mật độ rừng 0..1 theo vùng (rừng tây bắc, đông bắc, hai sườn, chân núi) + nhiễu cụm.</summary>
        private static float ForestDensity(Vector2 p)
        {
            float zone = 0f;
            zone = Mathf.Max(zone, Blob(p, new Vector2(-24f, 16f), 15f));
            zone = Mathf.Max(zone, Blob(p, new Vector2(24f, 15f), 13f));
            zone = Mathf.Max(zone, Blob(p, new Vector2(35f, -8f), 10f));
            zone = Mathf.Max(zone, Blob(p, new Vector2(-37f, -8f), 9f));
            zone = Mathf.Max(zone, Blob(p, new Vector2(-30f, -24f), 8f));
            zone = Mathf.Max(zone, Blob(p, new Vector2(18f, -20f), 7f) * 0.6f);
            zone = Mathf.Max(zone, Smooth(20f, 27f, p.y) * (1f - Smooth(30f, 36f, p.y)) * 0.8f); // chân núi
            float cluster = Mathf.PerlinNoise(p.x * 0.13f + 100f, p.y * 0.13f + 50f);
            return Mathf.Clamp01(zone * Smooth(0.2f, 0.5f, cluster) * 1.4f);
        }

        private static float Blob(Vector2 p, Vector2 c, float r) => 1f - Smooth(r * 0.55f, r, Vector2.Distance(p, c));

        // ─── Dựng ────────────────────────────────────────────────────────────
        /// <summary>Dựng toàn bộ cảnh vào <paramref name="parent"/>. Trả về WorldTerrain (đã có lưới độ cao).</summary>
        public static WorldTerrain Build(Transform parent)
        {
            EnsureFolder(MeshFolder);
            Material material = LandscapeMaterial();
            bool linear = PlayerSettings.colorSpace == ColorSpace.Linear;

            var heights = new float[Resolution * Resolution];
            for (int iz = 0; iz < Resolution; iz++)
            for (int ix = 0; ix < Resolution; ix++)
                heights[iz * Resolution + ix] = Height(-MapHalf + ix * Cell, -MapHalf + iz * Cell);

            var root = new GameObject("Landscape").transform;
            root.SetParent(parent, false);
            var terrain = root.gameObject.AddComponent<WorldTerrain>();
            terrain.Configure(Resolution, Cell, new Vector2(-MapHalf, -MapHalf), heights, WaterLevel);

            BuildGround(root, heights, material, linear);
            BuildWater(root);
            BuildDecor(root, material, linear);
            return terrain;
        }

        private static float H(float[] heights, int ix, int iz) => heights[iz * Resolution + ix];

        private static void BuildGround(Transform root, float[] heights, Material material, bool linear)
        {
            var land = new MeshBuilder(linear);
            var bed = new MeshBuilder(linear);
            // Màu theo từng đỉnh (chuyển mượt giữa cỏ / đất mòn / cát / đá), pháp tuyến vẫn theo mặt → low-poly khi có nắng.
            var colors = new Color[Resolution * Resolution];
            for (int iz = 0; iz < Resolution; iz++)
            for (int ix = 0; ix < Resolution; ix++)
            {
                Vector3 v = Vert(heights, ix, iz);
                float dx = H(heights, Mathf.Min(ix + 1, Resolution - 1), iz) - H(heights, Mathf.Max(ix - 1, 0), iz);
                float dz = H(heights, ix, Mathf.Min(iz + 1, Resolution - 1)) - H(heights, ix, Mathf.Max(iz - 1, 0));
                float slope = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz) / (2f * Cell)) * Mathf.Rad2Deg;
                colors[iz * Resolution + ix] = GroundColor(v, slope);
            }
            Color C(int ix, int iz) => colors[iz * Resolution + ix];

            for (int iz = 0; iz < Resolution - 1; iz++)
            for (int ix = 0; ix < Resolution - 1; ix++)
            {
                Vector3 p00 = Vert(heights, ix, iz), p10 = Vert(heights, ix + 1, iz);
                Vector3 p01 = Vert(heights, ix, iz + 1), p11 = Vert(heights, ix + 1, iz + 1);
                // Chéo (0,0)–(1,1) — đúng như WorldTerrain.Sample.
                AddGroundTriangle(land, bed, p00, p11, p10, C(ix, iz), C(ix + 1, iz + 1), C(ix + 1, iz));
                AddGroundTriangle(land, bed, p00, p01, p11, C(ix, iz), C(ix, iz + 1), C(ix + 1, iz + 1));
            }

            var landGO = MeshObject(root, "Ground", land.ToMesh("Ground"), material, castShadows: false);
            landGO.isStatic = true;
            // Lòng sông / đáy biển: không cho NavMesh đi xuống dưới nước (trừ chỗ cạn nằm trong mesh "Ground").
            var bedGO = MeshObject(root, "WaterBed", bed.ToMesh("WaterBed"), material, castShadows: false);
            var modifier = bedGO.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = 1; // Not Walkable
        }

        private static Vector3 Vert(float[] heights, int ix, int iz) =>
            new Vector3(-MapHalf + ix * Cell, H(heights, ix, iz), -MapHalf + iz * Cell);

        private static void AddGroundTriangle(MeshBuilder land, MeshBuilder bed, Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
        {
            bool deep = Mathf.Max(a.y, Mathf.Max(b.y, c.y)) < WaterLevel - 0.3f;
            (deep ? bed : land).AddTriangle(a, b, c, ca, cb, cc);
        }

        /// <summary>Màu mặt đất tại một điểm: cỏ (đậm/nhạt theo nhiễu) → nền rừng → đất mòn (đường, trại) → đá (dốc, cao) → cát (bờ nước, bãi biển).</summary>
        private static Color GroundColor(Vector3 v, float slope)
        {
            var p = new Vector2(v.x, v.z);
            float n = Mathf.PerlinNoise(v.x * 0.11f + 3f, v.z * 0.11f + 9f);      // mảng lớn
            float n2 = Mathf.PerlinNoise(v.x * 0.42f + 31f, v.z * 0.42f + 17f);   // chi tiết

            Color color = Color.Lerp(Color.Lerp(GrassDark, GrassMid, Smooth(0.2f, 0.5f, n)), GrassLight, Smooth(0.5f, 0.8f, n));
            color = Color.Lerp(color, GrassYellow, Smooth(0.72f, 0.95f, n2) * 0.55f);
            color = Color.Lerp(color, Color.Lerp(ForestFloor, ForestFloorLight, n2), Smooth(0.15f, 0.5f, ForestDensity(p)));

            float dirt = Mathf.Max(1f - Smooth(0.35f, 1.1f, PathDistance(p)), 1f - Smooth(3.2f, 5.2f, Vector2.Distance(p, new Vector2(0f, -2f))));
            color = Color.Lerp(color, Color.Lerp(DirtDark, Dirt, n2), dirt * (0.8f + 0.2f * n2));

            float rock = Mathf.Max(Smooth(26f, 38f, slope), Smooth(5f, 7.5f, v.y));
            Color stone = v.y > 10f ? Color.Lerp(Rock, RockLight, n2) : Color.Lerp(RockDark, Rock, n2);
            stone = Color.Lerp(stone, MossStone, (1f - Smooth(6f, 9f, v.y)) * Smooth(0.55f, 0.75f, n) * 0.7f);
            color = Color.Lerp(color, stone, rock);

            float sand = Mathf.Max(1f - Smooth(WaterLevel + 0.15f, WaterLevel + 0.6f, v.y),
                (1f - Smooth(-25f, -20f, v.z)) * (1f - Smooth(0.5f, 1.1f, v.y)));
            color = Color.Lerp(color, v.y < WaterLevel + 0.08f ? WetSand : Color.Lerp(Sand, SandLight, n2), sand);
            if (v.y < WaterLevel - 0.1f) color = Mud;

            // Cho đồi "đọc" được dưới nắng nghiêng: sườn dốc tối đi, chỗ cao sáng lên chút (như tô bóng tay của pixel art).
            float shade = Mathf.Lerp(1.06f, 0.8f, Smooth(4f, 26f, slope)) * (1f + Mathf.Clamp(v.y, -1f, 4f) * 0.025f);
            return color * shade;
        }

        private static void BuildWater(Transform root)
        {
            // Một mặt nước lớn (rộng hơn bản đồ để thấy biển tới chân trời); đất cao hơn mực nước che mất nó.
            var water = EditorBuildUtils.Part(root, "Water", PrimitiveType.Plane, new Vector3(0f, WaterLevel, -10f),
                new Vector3(22f, 1f, 22f), EditorBuildUtils.TexturedMat("SeaPixel", PixelTextureBuilder.Water, new Vector2(30f, 30f)));
            water.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var modifier = water.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
        }

        // ─── Cây cỏ, đá ──────────────────────────────────────────────────────
        private static readonly Vector2[] KeepClear =
        {
            // Đống lửa + trại, cây thu hoạch được, ruộng sẵn có, hang sói, thú ban đầu.
            new Vector2(0f, -2f), new Vector2(-2.6f, -1.5f), new Vector2(-10f, 9.5f), new Vector2(3f, -1.5f), new Vector2(-5f, -6.5f),
        };

        private static bool Blocked(Vector2 p, float extra)
        {
            if (Vector2.Distance(p, VillageCenter) < 9f + extra) return true; // khu làng để trống cho xây
            if (Vector2.Distance(p, PondCenter) < 2.8f + extra) return true;
            foreach (var k in KeepClear) if (Vector2.Distance(p, k) < 2.5f + extra) return true;
            if (PathDistance(p) < 1.6f + extra) return true;
            return false;
        }

        private static void BuildDecor(Transform root, Material material, bool linear)
        {
            var chunks = new Dictionary<Vector2Int, MeshBuilder>();      // cây, đá, khúc gỗ (đổ bóng, chắn NavMesh)
            var smallChunks = new Dictionary<Vector2Int, MeshBuilder>(); // cỏ, hoa, lau, sỏi (không bóng, bỏ qua NavMesh)
            MeshBuilder Chunk(Dictionary<Vector2Int, MeshBuilder> map, Vector3 p)
            {
                var key = new Vector2Int(Mathf.FloorToInt(p.x / ChunkSize), Mathf.FloorToInt(p.z / ChunkSize));
                if (!map.TryGetValue(key, out var b)) map[key] = b = new MeshBuilder(linear);
                return b;
            }

            var rng = new System.Random(2026);
            float R() => (float)rng.NextDouble();

            // Cây: lưới thưa có xê dịch, xác suất theo mật độ rừng; rải lác đác trên đồng bằng.
            for (float z = -MapHalf + 1f; z < MapHalf - 1f; z += 2.3f)
            for (float x = -MapHalf + 1f; x < MapHalf - 1f; x += 2.3f)
            {
                var p = new Vector2(x + (R() - 0.5f) * 1.8f, z + (R() - 0.5f) * 1.8f);
                float h = Height(p.x, p.y);
                if (h < WaterLevel + 0.35f || h > 9f || Blocked(p, 0f)) continue;
                float slope = SlopeDeg(p);
                if (slope > 30f) continue;
                float density = ForestDensity(p);
                float chance = density > 0.05f ? density * 1.05f : 0.025f;
                if (R() > chance) continue;

                var at = new Vector3(p.x, h - 0.05f, p.y);
                float scale = 0.85f + R() * 0.7f;
                float yaw = R() * 360f;
                float pick = R();
                MeshBuilder chunk = Chunk(chunks, at);
                if (h > 3.5f || pick < 0.45f) AddPine(chunk, at, scale, yaw, R());
                else if (pick < 0.88f) AddBroadleaf(chunk, at, scale, yaw, rng);
                else if (pick < 0.96f) AddBirch(chunk, at, scale, yaw, rng);
                else AddDeadTree(chunk, at, scale, yaw);

                // Bụi dưới tán, đá rêu lác đác.
                if (R() < 0.35f) AddBush(chunk, at + new Vector3((R() - 0.5f) * 2f, 0f, (R() - 0.5f) * 2f), 0.5f + R() * 0.4f, rng, R() < 0.15f);
                if (R() < 0.08f) AddRock(chunk, at + new Vector3(1f, 0f, -0.6f), 0.35f + R() * 0.3f, rng, mossy: true);
            }

            // Đá: chân núi, bờ sông, bãi biển, lác đác trên đồng bằng.
            for (int i = 0; i < 900; i++)
            {
                var p = new Vector2((R() * 2f - 1f) * (MapHalf - 1f), (R() * 2f - 1f) * (MapHalf - 1f));
                float h = Height(p.x, p.y);
                if (h < WaterLevel - 0.1f || Blocked(p, 0f)) continue;
                float riverDist = DistanceToPolyline(p, River, out _);
                bool mountainFoot = p.y > 20f;
                bool riverbank = riverDist > 1.4f && riverDist < 4.5f;
                bool beach = p.y < -20f && p.y > -32f && p.x > 8f;
                float chance = mountainFoot ? 0.5f : riverbank ? 0.35f : beach ? 0.45f : 0.03f;
                if (R() > chance) continue;
                var at = new Vector3(p.x, h - 0.08f, p.y);
                float size = mountainFoot ? 0.5f + R() * 1.4f : 0.25f + R() * 0.6f;
                MeshBuilder chunk = Chunk(chunks, at);
                AddRock(chunk, at, size, rng, mossy: !beach && R() < 0.4f);
                if (R() < 0.5f) AddRock(chunk, at + new Vector3(size * 0.9f, 0f, size * 0.4f), size * 0.5f, rng, mossy: false);
            }

            // Khúc gỗ đổ trong rừng.
            for (int i = 0; i < 24; i++)
            {
                var p = new Vector2((R() * 2f - 1f) * 44f, (R() * 2f - 1f) * 44f);
                if (ForestDensity(p) < 0.3f || Blocked(p, 0f)) continue;
                float h = Height(p.x, p.y);
                if (h < WaterLevel + 0.3f || SlopeDeg(p) > 20f) continue;
                AddLog(Chunk(chunks, new Vector3(p.x, h, p.y)), new Vector3(p.x, h + 0.12f, p.y), 0.9f + R() * 0.8f, R() * 180f);
            }

            // Cỏ, hoa, sỏi (nhỏ, nhiều): đồng bằng + ven rừng; lau sậy ven sông.
            for (int i = 0; i < 2600; i++)
            {
                var p = new Vector2((R() * 2f - 1f) * (MapHalf - 1f), (R() * 2f - 1f) * (MapHalf - 1f));
                float h = Height(p.x, p.y);
                if (h > 6f || SlopeDeg(p) > 30f) continue;
                float riverDist = DistanceToPolyline(p, River, out _);
                var at = new Vector3(p.x, h, p.y);
                MeshBuilder chunk = Chunk(smallChunks, at);
                if (h < WaterLevel + 0.15f && h > WaterLevel - 0.35f && riverDist < 4f) { AddReeds(chunk, at, rng); continue; }
                if (h < WaterLevel + 0.3f) continue;
                if (PathDistance(p) < 0.9f || Vector2.Distance(p, new Vector2(0f, -2f)) < 4.5f) continue;
                if (p.y < -21f) { if (R() < 0.15f) AddPebbles(chunk, at, rng); continue; }
                float roll = R();
                if (roll < 0.72f) AddGrassTuft(chunk, at, rng);
                else if (roll < 0.9f) AddFlower(chunk, at, rng);
                else AddPebbles(chunk, at, rng);
            }

            var decor = new GameObject("Decor").transform;
            decor.SetParent(root, false);
            foreach (var pair in chunks)
            {
                var go = MeshObject(decor, $"Nature_{pair.Key.x}_{pair.Key.y}", pair.Value.ToMesh($"Nature_{pair.Key.x}_{pair.Key.y}"), material, castShadows: true);
                go.isStatic = true;
            }
            foreach (var pair in smallChunks)
            {
                var go = MeshObject(decor, $"Grass_{pair.Key.x}_{pair.Key.y}", pair.Value.ToMesh($"Grass_{pair.Key.x}_{pair.Key.y}"), material, castShadows: false);
                go.isStatic = true;
                go.AddComponent<NavMeshModifier>().ignoreFromBuild = true; // cỏ hoa không chắn đường
            }
        }

        private static float SlopeDeg(Vector2 p)
        {
            const float d = 0.6f;
            float dx = Height(p.x + d, p.y) - Height(p.x - d, p.y);
            float dz = Height(p.x, p.y + d) - Height(p.x, p.y - d);
            return Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz) / (2f * d)) * Mathf.Rad2Deg;
        }

        // ─── Hình dạng (low-poly, mỗi mặt một màu) ───────────────────────────
        private static void AddPine(MeshBuilder b, Vector3 at, float scale, float yaw, float shade)
        {
            Color dark = Color.Lerp(PineDark, Pine, shade * 0.5f), mid = Color.Lerp(Pine, PineLight, shade * 0.4f);
            b.AddCylinder(at, 0.13f * scale, 0.6f * scale, 6, TrunkDark, yaw);
            b.AddCone(at + Vector3.up * 0.45f * scale, 0.75f * scale, 1.0f * scale, 7, dark, yaw);
            b.AddCone(at + Vector3.up * 0.95f * scale, 0.6f * scale, 0.9f * scale, 7, mid, yaw + 20f);
            b.AddCone(at + Vector3.up * 1.42f * scale, 0.4f * scale, 0.8f * scale, 6, Color.Lerp(mid, PineLight, 0.5f), yaw + 40f);
        }

        private static void AddBroadleaf(MeshBuilder b, Vector3 at, float scale, float yaw, System.Random rng)
        {
            float R() => (float)rng.NextDouble();
            bool autumn = R() < 0.12f;
            Color leaf = autumn ? LeafAutumn : Color.Lerp(LeafDark, Leaf, R());
            b.AddCylinder(at, 0.14f * scale, 1.0f * scale, 6, TrunkColor, yaw);
            Vector3 crown = at + Vector3.up * 1.35f * scale;
            b.AddBlob(crown, new Vector3(0.85f, 0.7f, 0.85f) * scale, leaf, rng, 1);
            b.AddBlob(crown + new Vector3(0.45f, 0.25f, 0.2f) * scale, new Vector3(0.55f, 0.5f, 0.55f) * scale, Color.Lerp(leaf, LeafLight, 0.45f), rng, 1);
            b.AddBlob(crown + new Vector3(-0.35f, 0.15f, -0.35f) * scale, new Vector3(0.5f, 0.45f, 0.5f) * scale, leaf * 0.9f, rng, 1);
        }

        private static void AddBirch(MeshBuilder b, Vector3 at, float scale, float yaw, System.Random rng)
        {
            b.AddCylinder(at, 0.09f * scale, 1.5f * scale, 6, BirchBark, yaw);
            b.AddBlob(at + Vector3.up * 1.7f * scale, new Vector3(0.5f, 0.75f, 0.5f) * scale, LeafLight, rng, 1);
        }

        private static void AddDeadTree(MeshBuilder b, Vector3 at, float scale, float yaw)
        {
            b.AddCylinder(at, 0.11f * scale, 1.3f * scale, 5, TrunkDark, yaw);
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            b.AddCylinder(at + Vector3.up * 0.9f * scale, 0.05f * scale, 0.6f * scale, 4, TrunkDark, yaw, q * Quaternion.Euler(0f, 0f, 50f));
            b.AddCylinder(at + Vector3.up * 1.05f * scale, 0.04f * scale, 0.5f * scale, 4, TrunkDark, yaw, q * Quaternion.Euler(0f, 0f, -45f));
        }

        private static void AddBush(MeshBuilder b, Vector3 at, float size, System.Random rng, bool berries)
        {
            Color c = Color.Lerp(LeafDark, Leaf, (float)rng.NextDouble());
            b.AddBlob(at + Vector3.up * size * 0.35f, new Vector3(size, size * 0.7f, size), c, rng, 1);
            if (!berries) return;
            for (int i = 0; i < 5; i++)
            {
                Vector3 off = new Vector3(((float)rng.NextDouble() - 0.5f) * size * 1.4f, size * (0.45f + (float)rng.NextDouble() * 0.3f), ((float)rng.NextDouble() - 0.5f) * size * 1.4f);
                b.AddBlob(at + off, Vector3.one * 0.07f, Hex(0xc23b2e), rng, 0);
            }
        }

        private static void AddRock(MeshBuilder b, Vector3 at, float size, System.Random rng, bool mossy)
        {
            float t = (float)rng.NextDouble();
            Color c = t < 0.33f ? RockDark : t < 0.75f ? Rock : RockLight;
            b.AddBlob(at + Vector3.up * size * 0.25f, new Vector3(size, size * (0.55f + t * 0.3f), size * (0.8f + t * 0.3f)), c, rng, 0, jitter: 0.28f);
            if (mossy) b.AddBlob(at + Vector3.up * size * 0.55f, new Vector3(size * 0.6f, size * 0.2f, size * 0.6f), MossStone, rng, 0, jitter: 0.2f);
        }

        private static void AddLog(MeshBuilder b, Vector3 at, float length, float yaw)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, 90f);
            b.AddCylinder(at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(-length * 0.5f, 0f, 0f), 0.13f, length, 6, TrunkColor, 0f, q);
        }

        private static void AddGrassTuft(MeshBuilder b, Vector3 at, System.Random rng)
        {
            Color c = Color.Lerp(GrassMid, GrassYellow, (float)rng.NextDouble() * 0.7f);
            for (int i = 0; i < 3; i++)
            {
                float a = ((float)rng.NextDouble() * 360f) * Mathf.Deg2Rad;
                float h = 0.18f + (float)rng.NextDouble() * 0.15f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 side = Vector3.Cross(dir, Vector3.up) * 0.035f;
                Vector3 tip = at + Vector3.up * h + dir * 0.06f;
                b.AddDoubleSided(at - side, at + side, tip, c);
            }
        }

        private static void AddFlower(MeshBuilder b, Vector3 at, System.Random rng)
        {
            AddGrassTuft(b, at, rng);
            Color c = FlowerColors[rng.Next(FlowerColors.Length)];
            b.AddBlob(at + Vector3.up * 0.26f, Vector3.one * 0.06f, c, rng, 0);
        }

        private static void AddPebbles(MeshBuilder b, Vector3 at, System.Random rng)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 off = new Vector3(((float)rng.NextDouble() - 0.5f) * 0.5f, 0.02f, ((float)rng.NextDouble() - 0.5f) * 0.5f);
                b.AddBlob(at + off, new Vector3(0.08f, 0.04f, 0.07f), (float)rng.NextDouble() < 0.5f ? Rock : RockLight, rng, 0);
            }
        }

        private static void AddReeds(MeshBuilder b, Vector3 at, System.Random rng)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 off = new Vector3(((float)rng.NextDouble() - 0.5f) * 0.6f, 0f, ((float)rng.NextDouble() - 0.5f) * 0.6f);
                float h = 0.6f + (float)rng.NextDouble() * 0.4f;
                b.AddCylinder(at + off, 0.02f, h, 3, Reed, 0f);
                b.AddCylinder(at + off + Vector3.up * h * 0.8f, 0.04f, h * 0.22f, 4, ReedTop, 0f);
            }
        }

        // ─── Tiện ích ────────────────────────────────────────────────────────
        private static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material material, bool castShadows)
        {
            string path = $"{MeshFolder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                mesh = existing;
            }
            else AssetDatabase.CreateAsset(mesh, path);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        private static Material LandscapeMaterial()
        {
            var grain = BuildGrainTexture();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                EnsureFolder(MaterialPath);
                mat = new Material(Shader.Find("PrehistoricTribe/VertexColorLit"));
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            mat.shader = Shader.Find("PrehistoricTribe/VertexColorLit");
            mat.SetTexture("_Grain", grain);
            mat.SetFloat("_GrainStrength", 0.3f);
            mat.SetFloat("_GrainScale", 0.5f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Texture "hạt" xám 32×32 (trung bình 0.5) lọc Point — mỗi điểm ảnh phủ 1/16 m² theo _GrainScale.</summary>
        private static Texture2D BuildGrainTexture()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var rng = new System.Random(77);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float v = 0.5f + ((float)rng.NextDouble() - 0.5f) * 0.22f;
                tex.SetPixel(x, y, new Color(v, v, v, 1f));
            }
            tex.Apply();
            EnsureFolder(GrainTexturePath);
            File.WriteAllBytes(GrainTexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(GrainTexturePath);
            if (AssetImporter.GetAtPath(GrainTexturePath) is TextureImporter importer)
            {
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.sRGBTexture = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(GrainTexturePath);
        }

        private static void EnsureFolder(string assetPath)
        {
            string folder = assetPath.EndsWith(".asset") || assetPath.EndsWith(".mat") || assetPath.EndsWith(".png")
                ? Path.GetDirectoryName(assetPath).Replace('\\', '/')
                : assetPath;
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        // ─── Gộp mesh màu đỉnh, mặt phẳng (flat shading) ─────────────────────
        private class MeshBuilder
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Color> colors = new List<Color>();
            private readonly List<int> triangles = new List<int>();
            private readonly bool linear;

            public MeshBuilder(bool linearColorSpace) => linear = linearColorSpace;

            /// <summary>Tam giác một màu, mặt hướng theo chiều kim đồng hồ khi nhìn từ phía trước.</summary>
            public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
            {
                Color vc = linear ? color.linear : color;
                int i = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                colors.Add(vc); colors.Add(vc); colors.Add(vc);
                triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            }

            /// <summary>Tam giác có màu riêng từng đỉnh (mặt đất chuyển màu mượt).</summary>
            public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
            {
                int i = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                colors.Add(linear ? ca.linear : ca); colors.Add(linear ? cb.linear : cb); colors.Add(linear ? cc.linear : cc);
                triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            }

            public void AddDoubleSided(Vector3 a, Vector3 b, Vector3 c, Color color)
            {
                AddTriangle(a, b, c, color);
                AddTriangle(a, c, b, color * 0.85f);
            }

            /// <summary>Tam giác hướng ra ngoài tâm <paramref name="center"/> (tự đảo thứ tự nếu cần).</summary>
            private void AddOutward(Vector3 a, Vector3 b, Vector3 c, Vector3 center, Color color)
            {
                Vector3 n = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(n, (a + b + c) / 3f - center) < 0f) AddTriangle(a, c, b, color);
                else AddTriangle(a, b, c, color);
            }

            public void AddCone(Vector3 baseCenter, float radius, float height, int sides, Color color, float yaw)
            {
                Vector3 tip = baseCenter + Vector3.up * height;
                Vector3 mid = baseCenter + Vector3.up * height * 0.35f;
                for (int i = 0; i < sides; i++)
                {
                    Vector3 a = baseCenter + Ring(i, sides, radius, yaw);
                    Vector3 b = baseCenter + Ring(i + 1, sides, radius, yaw);
                    // Mặt sáng/tối xen kẽ nhẹ cho rõ khối.
                    AddOutward(a, b, tip, mid, color * (i % 2 == 0 ? 1f : 0.92f));
                    AddOutward(a, b, baseCenter, mid, color * 0.7f);
                }
            }

            public void AddCylinder(Vector3 bottom, float radius, float height, int sides, Color color, float yaw, Quaternion? rotation = null)
            {
                Quaternion q = rotation ?? Quaternion.identity;
                Vector3 up = q * Vector3.up;
                Vector3 center = bottom + up * height * 0.5f;
                for (int i = 0; i < sides; i++)
                {
                    Vector3 a = bottom + q * Ring(i, sides, radius, yaw);
                    Vector3 b = bottom + q * Ring(i + 1, sides, radius, yaw);
                    Vector3 a2 = a + up * height, b2 = b + up * height;
                    Color side = color * (i % 2 == 0 ? 1f : 0.9f);
                    AddOutward(a, b, b2, center, side);
                    AddOutward(a, b2, a2, center, side);
                    AddOutward(a2, b2, bottom + up * height, center, color * 1.05f);
                }
            }

            /// <summary>Khối tròn low-poly (khối 20 mặt, chia thêm nếu <paramref name="subdivisions"/> = 1) có méo ngẫu nhiên.</summary>
            public void AddBlob(Vector3 center, Vector3 radii, Color color, System.Random rng, int subdivisions, float jitter = 0.12f)
            {
                foreach (var tri in Icosphere(subdivisions))
                {
                    Vector3 a = Deform(tri.a), b = Deform(tri.b), c = Deform(tri.c);
                    // Mặt hướng lên sáng hơn, hướng xuống tối hơn → khối rõ dù ánh sáng phẳng.
                    float up = Vector3.Dot(Vector3.Cross(b - a, c - a).normalized, Vector3.up);
                    AddOutward(a, b, c, center, color * (0.82f + 0.22f * (up * 0.5f + 0.5f)));
                }

                Vector3 Deform(Vector3 v)
                {
                    // Méo theo vị trí đỉnh (cùng đỉnh → cùng méo, khối không bị hở).
                    float n = Mathf.PerlinNoise(v.x * 3.1f + center.x * 0.37f + 11f, v.z * 3.1f + v.y * 1.7f + center.z * 0.37f);
                    float k = 1f + (n - 0.5f) * 2f * jitter;
                    return center + Vector3.Scale(v * k, radii);
                }
            }

            private static Vector3 Ring(int i, int sides, float radius, float yawDeg)
            {
                float a = (i / (float)sides) * Mathf.PI * 2f + yawDeg * Mathf.Deg2Rad;
                return new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private struct Tri { public Vector3 a, b, c; }

        private static readonly Dictionary<int, List<Tri>> icosphereCache = new Dictionary<int, List<Tri>>();

        private static List<Tri> Icosphere(int subdivisions)
        {
            if (icosphereCache.TryGetValue(subdivisions, out var cached)) return cached;
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var v = new[]
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            int[] f =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            var tris = new List<Tri>();
            for (int i = 0; i < f.Length; i += 3)
                tris.Add(new Tri { a = v[f[i]].normalized, b = v[f[i + 1]].normalized, c = v[f[i + 2]].normalized });
            for (int s = 0; s < subdivisions; s++)
            {
                var next = new List<Tri>();
                foreach (var tri in tris)
                {
                    Vector3 ab = ((tri.a + tri.b) * 0.5f).normalized, bc = ((tri.b + tri.c) * 0.5f).normalized, ca = ((tri.c + tri.a) * 0.5f).normalized;
                    next.Add(new Tri { a = tri.a, b = ab, c = ca });
                    next.Add(new Tri { a = tri.b, b = bc, c = ab });
                    next.Add(new Tri { a = tri.c, b = ca, c = bc });
                    next.Add(new Tri { a = ab, b = bc, c = ca });
                }
                tris = next;
            }
            icosphereCache[subdivisions] = tris;
            return tris;
        }
    }
}
