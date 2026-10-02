using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Một đám cháy (M6/D4) bám trên cây (<see cref="ResourceNode"/> gỗ), công trình có máu, hoặc ruộng đang có cây.
    /// Lửa bùng dần (<see cref="Intensity"/> 0..1), cháy to thì bén sang đồ dễ cháy xung quanh. Cây cháy rụi sau một lúc,
    /// công trình mất máu theo độ lửa (sập thì tắt), ruộng thì cây chết. Dội nước / đập lửa làm giảm độ lửa, về 0 là tắt.
    /// </summary>
    public class Fire : MonoBehaviour
    {
        private static readonly List<Fire> all = new List<Fire>();
        public static IReadOnlyList<Fire> All => all;

        /// <summary>Thông số lan cháy dùng chung (do <see cref="WildfireDisaster"/> đặt).</summary>
        public class Settings
        {
            public float growPerSecond = 1f / 15f;
            public float spreadInterval = 6f;
            public float spreadRadius = 2.2f;
            public float spreadChance = 0.3f;
            public float treeBurnSeconds = 40f;
            public float fieldBurnSeconds = 8f;
            public float buildingDamagePerSecond = 1f / 45f; // phần máu tối đa mỗi giây khi lửa to nhất
            public Material flameMaterial;
            public Material coreMaterial;
            public Material smokeMaterial;
            public Material charredMaterial;
        }

        public float Intensity { get; private set; }
        public float Age { get; private set; }
        public MonoBehaviour Target { get; private set; }

        private Settings settings;
        private float spreadTimer;
        private readonly List<Transform> flames = new List<Transform>();
        private readonly List<Transform> smokePuffs = new List<Transform>();
        private Light glow;

        /// <summary>Ruộng có nước từ mức này trở lên thì không cháy.</summary>
        public const float WetFieldLevel = 0.5f;

        public static bool IsBurning(Component target) => target != null && target.GetComponent<Fire>() != null;

        /// <summary>Đối tượng này cháy được không (cây gỗ, công trình gỗ còn đứng, ruộng đang có cây).</summary>
        public static bool CanBurn(MonoBehaviour target)
        {
            if (target == null || IsBurning(target)) return false;
            switch (target)
            {
                case ResourceNode node:
                    return !node.IsDepleted && node.GetComponent<WaterSource>() == null && node.ResourceType != null && node.ResourceType.id == "wood";
                case FarmPlot plot:
                    // Ruộng còn ướt thì không bắt lửa — tưới tốt cũng là phòng cháy.
                    return (plot.State == FarmPlotState.Growing || plot.State == FarmPlotState.ReadyToHarvest) && plot.Water < WetFieldLevel;
                case BuildingInstance building:
                    return building.Health != null && !building.IsCollapsed && building.GetComponent<WaterSource>() == null; // giếng đá không cháy
                default:
                    return false;
            }
        }

        public static Fire Ignite(MonoBehaviour target, Settings settings, float startIntensity = 0.3f)
        {
            if (!CanBurn(target)) return null;
            var fire = target.gameObject.AddComponent<Fire>();
            fire.Target = target;
            fire.settings = settings;
            fire.Intensity = startIntensity;
            fire.BuildVisual();
            return fire;
        }

        /// <summary>Cháy gần điểm này nhất (null nếu không có trong bán kính).</summary>
        public static Fire Nearest(Vector3 position, float radius, Fire except = null)
        {
            Fire best = null;
            float bestDistance = radius;
            foreach (var fire in all)
            {
                if (fire == except || fire.Intensity <= 0f) continue;
                float d = InteractableRegistry.GroundDistance(position, fire.transform.position);
                if (d > bestDistance) continue;
                best = fire;
                bestDistance = d;
            }
            return best;
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => all.Clear();

        /// <summary>Dội nước / đập lửa. Trả về true nếu lửa đã tắt.</summary>
        public bool Douse(float amount)
        {
            VfxManager.Play(VfxKind.Steam, transform.position + Vector3.up * 0.4f);
            Intensity -= amount;
            if (Intensity > 0f) return false;
            PutOut();
            return true;
        }

        public void PutOut()
        {
            foreach (var puff in smokePuffs)
                if (puff != null) Destroy(puff.gameObject);
            smokePuffs.Clear();
            if (glow != null) Destroy(glow.gameObject);
            foreach (var flame in flames)
                if (flame != null) Destroy(flame.gameObject);
            flames.Clear();
            Destroy(this);
        }

        private void Update()
        {
            if (Target == null || settings == null)
            {
                PutOut();
                return;
            }

            float dt = Time.deltaTime;
            Age += dt;
            Intensity = Mathf.Min(1f, Intensity + settings.growPerSecond * dt);

            if (BurnTarget(dt)) return;

            spreadTimer += dt;
            if (spreadTimer >= settings.spreadInterval)
            {
                spreadTimer = 0f;
                if (Intensity >= 0.5f) Spread();
            }
            UpdateVisual();
        }

        /// <summary>Gây hư hại cho đối tượng đang cháy. Trả về true nếu đã cháy hết (lửa tắt).</summary>
        private bool BurnTarget(float dt)
        {
            switch (Target)
            {
                case ResourceNode node:
                    if (Age < settings.treeBurnSeconds) return false;
                    LeaveCharredStump(node.transform.position);
                    EventBus.RaiseNotification("Một cây đã cháy rụi");
                    PutOut();
                    Destroy(node.gameObject);
                    return true;

                case FarmPlot plot:
                    if (Age < settings.fieldBurnSeconds) return false;
                    plot.KillCrop("cháy rụi");
                    PutOut();
                    return true;

                case BuildingInstance building:
                    building.Damage(building.Health.Max * settings.buildingDamagePerSecond * Intensity * dt, "cháy");
                    if (!building.IsCollapsed) return false;
                    PutOut();
                    return true;
            }
            return false;
        }

        private void Spread()
        {
            float chance = settings.spreadChance;
            foreach (var candidate in Burnables())
            {
                if (candidate == Target || !CanBurn(candidate)) continue;
                if (InteractableRegistry.GroundDistance(transform.position, candidate.transform.position) > settings.spreadRadius) continue;
                if (Random.value < chance) Ignite(candidate, settings, 0.15f);
            }
        }

        public static IEnumerable<MonoBehaviour> Burnables()
        {
            foreach (var node in InteractableRegistry.All<ResourceNode>()) yield return node;
            foreach (var plot in InteractableRegistry.All<FarmPlot>()) yield return plot;
            foreach (var building in BuildingInstance.All) yield return building;
        }

        // ─── Hình ảnh ────────────────────────────────────────────────────────
        private static Mesh coneMesh;

        /// <summary>Hình nón đáy Ø1 tại gốc, cao 1 — ngọn lửa.</summary>
        private static Mesh ConeMesh()
        {
            if (coneMesh != null) return coneMesh;
            const int sides = 7;
            var vertices = new List<Vector3> { new Vector3(0f, 1f, 0f), Vector3.zero };
            var triangles = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                vertices.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f));
            }
            for (int i = 0; i < sides; i++)
            {
                int a = 2 + i, b = 2 + (i + 1) % sides;
                triangles.AddRange(new[] { 0, b, a, 1, a, b });
            }
            coneMesh = new Mesh { name = "FlameCone" };
            coneMesh.SetVertices(vertices);
            coneMesh.SetTriangles(triangles, 0);
            coneMesh.RecalculateNormals();
            return coneMesh;
        }

        private GameObject MakePart(string partName, Mesh mesh, Material material, Transform parent)
        {
            var go = new GameObject(partName);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>Chỗ đặt các ngọn lửa: quanh tán cây / quanh mái nhà / rải trên mặt ruộng.</summary>
        private Vector3[] FlameSpots()
        {
            switch (Target)
            {
                case ResourceNode _:
                    return new[] { new Vector3(0.35f, 0.7f, 0f), new Vector3(-0.3f, 0.9f, 0.2f), new Vector3(0.05f, 1.1f, -0.35f),
                                   new Vector3(-0.15f, 1.4f, -0.1f), new Vector3(0.2f, 1.25f, 0.25f) };
                case FarmPlot _:
                    return new[] { new Vector3(-0.25f, 0.05f, -0.2f), new Vector3(0.25f, 0.05f, -0.2f), new Vector3(-0.2f, 0.05f, 0.25f), new Vector3(0.22f, 0.05f, 0.22f) };
                default:
                    return new[] { new Vector3(0.25f, 0.15f, 0.1f), new Vector3(-0.25f, 0.2f, -0.1f), new Vector3(0f, 0.35f, 0.25f), new Vector3(0.05f, 0.5f, -0.15f) };
            }
        }

        private void BuildVisual()
        {
            Mesh mesh = ConeMesh();
            Mesh sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            foreach (var spot in FlameSpots())
            {
                var flame = MakePart("Flame", mesh, settings.flameMaterial, transform);
                flame.transform.localPosition = spot;
                MakePart("Core", mesh, settings.coreMaterial != null ? settings.coreMaterial : settings.flameMaterial, flame.transform)
                    .transform.localScale = new Vector3(0.55f, 0.6f, 0.55f);
                flames.Add(flame.transform);
            }
            var light = new GameObject("FireLight");
            light.transform.SetParent(transform, false);
            light.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            glow = light.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.55f, 0.2f);
            glow.range = 3.5f;
            glow.shadows = LightShadows.None;
            for (int i = 0; i < 3; i++)
                smokePuffs.Add(MakePart("Smoke", sphere, settings.smokeMaterial, transform).transform);
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            for (int i = 0; i < flames.Count; i++)
            {
                float flicker = 0.8f + 0.2f * Mathf.Sin(Time.time * (11f + i * 2.3f) + i * 1.7f);
                float size = (0.12f + 0.18f * Intensity) * (Target is FarmPlot ? 0.8f : 1f);
                flames[i].localScale = new Vector3(size, size * 2.2f * flicker, size);
            }
            if (glow != null) glow.intensity = (0.8f + 1.4f * Intensity) * (0.85f + 0.15f * Mathf.Sin(Time.time * 13f));
            float top = Target is ResourceNode ? 1.6f : Target is FarmPlot ? 0.3f : 0.75f;
            for (int i = 0; i < smokePuffs.Count; i++)
            {
                float life = Mathf.Repeat(Time.time * 0.35f + i / 3f, 1f); // mỗi cụm khói bay lên rồi tan
                float s = (0.12f + 0.25f * life) * (0.4f + 0.6f * Intensity) * (1f - life * 0.5f);
                smokePuffs[i].localPosition = new Vector3(0.15f * Mathf.Sin(i * 2.1f + life * 2f), top + life * 1.2f, 0.1f * i);
                smokePuffs[i].localScale = Vector3.one * s;
            }
        }


        private void LeaveCharredStump(Vector3 position)
        {
            var stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(stump.GetComponent<Collider>());
            stump.name = "CharredStump";
            stump.transform.position = position + new Vector3(0f, 0.12f, 0f);
            stump.transform.localScale = new Vector3(0.22f, 0.12f, 0.22f);
            if (settings.charredMaterial != null) stump.GetComponent<Renderer>().sharedMaterial = settings.charredMaterial;
        }
    }
}
