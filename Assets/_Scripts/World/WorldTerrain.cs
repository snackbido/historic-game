using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Địa hình có đồi núi (thiết kế theo .claude/LANDSCAPE_DESIGN.md, 2026-10-05): lưới độ cao đều trên mặt XZ, mesh
    /// low-poly dựng từ đúng lưới này (Editor/LandscapeBuilder). Mọi chỗ cần độ cao mặt đất (người chơi bước đi, đặt
    /// công trình, chuột chọn ô, sinh thú…) hỏi <see cref="HeightAt"/> — tính thẳng từ lưới, nội suy theo đúng tam giác của
    /// mesh, KHÔNG dùng MeshCollider/raycast vật lý (lần thử terrain Blender 2026-10-03 hỏng chính ở đó).
    /// Không có địa hình trong scene → mặt đất phẳng y = 0 như trước (test, scene khác).
    /// </summary>
    public class WorldTerrain : MonoBehaviour
    {
        [SerializeField] private int resolution;        // số đỉnh mỗi cạnh
        [SerializeField] private float cellSize = 2f;    // khoảng cách giữa hai đỉnh (m)
        [SerializeField] private Vector2 origin;          // (x, z) của đỉnh [0, 0]
        [SerializeField] private float[] heights = new float[0];
        [SerializeField] private float waterLevel = -0.55f;
        [Tooltip("Nước nông hơn chừng này (m) thì lội qua được (chỗ cạn trên sông)")]
        [SerializeField] private float wadeDepth = 0.3f;
        [Tooltip("Dốc hơn góc này thì người không leo được")]
        [SerializeField] private float maxWalkSlope = 38f;

        private static WorldTerrain instance;

        public static WorldTerrain Instance => instance;
        public static bool Exists => instance != null;
        public static float WaterLevel => instance != null ? instance.waterLevel : float.NegativeInfinity;
        public float CellSize => cellSize;
        public Vector2 Origin => origin;
        public int Resolution => resolution;
        public float Size => (resolution - 1) * cellSize;

        private void Awake() => instance = this;

        private void OnEnable() => instance = this;

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>Gán lưới độ cao (Editor dựng scene gọi).</summary>
        public void Configure(int vertsPerSide, float cell, Vector2 originXZ, float[] heightData, float water)
        {
            resolution = vertsPerSide;
            cellSize = cell;
            origin = originXZ;
            heights = heightData;
            waterLevel = water;
            instance = this;
        }

        public float RawHeight(int ix, int iz)
        {
            ix = Mathf.Clamp(ix, 0, resolution - 1);
            iz = Mathf.Clamp(iz, 0, resolution - 1);
            return heights[iz * resolution + ix];
        }

        /// <summary>
        /// Độ cao bề mặt tại (x, z), khớp đúng mesh: mỗi ô chia 2 tam giác theo đường chéo (0,0)–(1,1).
        /// </summary>
        public float Sample(float x, float z)
        {
            if (heights == null || heights.Length == 0) return 0f;
            float fx = (x - origin.x) / cellSize;
            float fz = (z - origin.y) / cellSize;
            int ix = Mathf.Clamp(Mathf.FloorToInt(fx), 0, resolution - 2);
            int iz = Mathf.Clamp(Mathf.FloorToInt(fz), 0, resolution - 2);
            float u = Mathf.Clamp01(fx - ix);
            float v = Mathf.Clamp01(fz - iz);
            float h00 = RawHeight(ix, iz), h10 = RawHeight(ix + 1, iz), h01 = RawHeight(ix, iz + 1), h11 = RawHeight(ix + 1, iz + 1);
            return u >= v
                ? h00 + (h10 - h00) * u + (h11 - h10) * v   // tam giác (0,0) (1,0) (1,1)
                : h00 + (h01 - h00) * v + (h11 - h01) * u;  // tam giác (0,0) (1,1) (0,1)
        }

        public bool Contains(float x, float z)
        {
            float size = Size;
            return x >= origin.x && z >= origin.y && x <= origin.x + size && z <= origin.y + size;
        }

        // ─── Truy vấn tĩnh (an toàn khi không có địa hình) ──────────────────
        public static float HeightAt(float x, float z) => instance != null ? instance.Sample(x, z) : 0f;
        public static float HeightAt(Vector3 position) => HeightAt(position.x, position.z);

        /// <summary>Cùng (x, z), y đặt lên mặt đất.</summary>
        public static Vector3 Ground(Vector3 position) => new Vector3(position.x, HeightAt(position.x, position.z), position.z);

        /// <summary>Độ dốc (độ) quanh điểm này, đo bằng chênh cao trong 1m.</summary>
        public static float SlopeAt(float x, float z)
        {
            if (instance == null) return 0f;
            const float d = 0.5f;
            float dx = HeightAt(x + d, z) - HeightAt(x - d, z);
            float dz = HeightAt(x, z + d) - HeightAt(x, z - d);
            return Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz) / (2f * d)) * Mathf.Rad2Deg;
        }

        /// <summary>Chênh cao lớn nhất trong ô vuông cạnh <paramref name="size"/> quanh tâm (dùng để chặn xây trên dốc).</summary>
        public static float HeightRange(Vector3 center, float size)
        {
            if (instance == null) return 0f;
            float half = size * 0.5f;
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            for (int i = 0; i <= 2; i++)
            for (int j = 0; j <= 2; j++)
            {
                float h = HeightAt(center.x - half + i * half, center.z - half + j * half);
                min = Mathf.Min(min, h);
                max = Mathf.Max(max, h);
            }
            return max - min;
        }

        /// <summary>Điểm này nằm dưới mặt nước (sông, biển)?</summary>
        public static bool IsUnderwater(float x, float z, float margin = 0f) =>
            instance != null && HeightAt(x, z) < instance.waterLevel + margin;

        /// <summary>Người đi tới được điểm này không: trong bản đồ, không quá dốc, nước không quá sâu (chỗ cạn lội được).</summary>
        public static bool IsWalkable(float x, float z)
        {
            if (instance == null) return true;
            if (!instance.Contains(x, z)) return false;
            if (HeightAt(x, z) < instance.waterLevel - instance.wadeDepth) return false;
            return SlopeAt(x, z) <= instance.maxWalkSlope;
        }

        /// <summary>
        /// Tia chuột cắt mặt đất ở đâu: dò dọc tia theo bước nhỏ rồi chia đôi khoảng tìm được (không cần collider).
        /// Không có địa hình → cắt mặt phẳng y = 0.
        /// </summary>
        public static bool Raycast(Ray ray, out Vector3 point, float maxDistance = 400f)
        {
            point = default;
            if (instance == null)
            {
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (!plane.Raycast(ray, out float enter)) return false;
                point = ray.GetPoint(enter);
                return true;
            }

            const float step = 0.5f;
            float previous = 0f;
            for (float t = 0f; t <= maxDistance; t += step)
            {
                Vector3 p = ray.GetPoint(t);
                if (p.y > HeightAt(p.x, p.z)) { previous = t; continue; }
                // Chia đôi giữa điểm còn trên mặt đất và điểm đã xuống dưới.
                float lo = previous, hi = t;
                for (int i = 0; i < 12; i++)
                {
                    float mid = (lo + hi) * 0.5f;
                    Vector3 m = ray.GetPoint(mid);
                    if (m.y > HeightAt(m.x, m.z)) lo = mid; else hi = mid;
                }
                point = ray.GetPoint(hi);
                return true;
            }
            return false;
        }
    }
}
