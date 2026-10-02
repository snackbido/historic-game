using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    public enum VfxKind
    {
        WoodChips,  // chặt cây
        Leaves,     // cây đổ, làm cỏ
        Splash,     // đánh cá, dội nước
        Dust,       // cuốc đất, sửa nhà, công trình bị đánh
        BigDust,    // xây xong, công trình sập, con vật chết
        Harvest,    // gặt hái
        Sparkle,    // nâng cấp, mở công nghệ
        Hit,        // người / thú bị đánh
        Steam,      // dập lửa
        Lightning   // sét đánh
    }

    /// <summary>
    /// Hiệu ứng hạt (M7/P1) dựng hoàn toàn bằng code: mỗi loại hiệu ứng là một ParticleSystem dùng chung (không lặp),
    /// <see cref="Play"/> phun một đợt hạt tại vị trí cần. Hạt là ô vuông không texture → qua hậu kỳ pixel thành điểm ảnh.
    /// Không có manager trong scene thì mọi lời gọi bỏ qua (test, scene khác).
    /// </summary>
    public class VfxManager : MonoBehaviour
    {
        [SerializeField] private Material particleMaterial;

        private static VfxManager instance;
        private readonly Dictionary<VfxKind, ParticleSystem> systems = new Dictionary<VfxKind, ParticleSystem>();
        private ParticleSystem rain;
        private Light flash;
        private float flashTimer;

        /// <summary>Số lần mỗi loại hiệu ứng đã phát (để test kiểm tra móc nối).</summary>
        private static readonly Dictionary<VfxKind, int> playCounts = new Dictionary<VfxKind, int>();

        public static int PlayCount(VfxKind kind) => playCounts.TryGetValue(kind, out int n) ? n : 0;
        public static bool IsRaining => instance != null && instance.rain != null && instance.rain.isEmitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => playCounts.Clear();

        private void Awake()
        {
            instance = this;
            foreach (VfxKind kind in System.Enum.GetValues(typeof(VfxKind)))
                systems[kind] = CreateBurstSystem(kind);
            rain = CreateRain();

            var flashGO = new GameObject("LightningFlash");
            flashGO.transform.SetParent(transform, false);
            flash = flashGO.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.range = 30f;
            flash.color = new Color(0.85f, 0.9f, 1f);
            flash.intensity = 0f;
            flash.enabled = false;
        }

        private void OnEnable()
        {
            EventBus.OnBuildingPlaced += HandleBuildingPlaced;
            EventBus.OnBuildingUpgraded += HandleBuildingUpgraded;
        }

        private void OnDisable()
        {
            EventBus.OnBuildingPlaced -= HandleBuildingPlaced;
            EventBus.OnBuildingUpgraded -= HandleBuildingUpgraded;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void HandleBuildingPlaced(BuildingInstance building) => Play(VfxKind.BigDust, building.transform.position, SfxKind.Build);
        private void HandleBuildingUpgraded(BuildingInstance building) => Play(VfxKind.Sparkle, building.transform.position + Vector3.up * 0.8f);

        private void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                // Chớp hai nhịp rồi tắt.
                flash.intensity = flashTimer > 0.18f || (flashTimer > 0.05f && flashTimer < 0.1f) ? 6f : 0f;
                if (flashTimer <= 0f) flash.enabled = false;
            }

            // Mưa đi theo camera (vùng mưa phủ khung nhìn).
            if (rain != null && rain.isEmitting && Camera.main != null)
            {
                Vector3 focus = Camera.main.transform.position + Camera.main.transform.forward * 12f;
                rain.transform.position = new Vector3(focus.x, 12f, focus.z);
            }
        }

        // ─── API ────────────────────────────────────────────────────────────
        /// <summary>Phun một đợt hạt kèm tiếng tương ứng (<paramref name="sound"/> để đổi tiếng mặc định của hiệu ứng).</summary>
        public static void Play(VfxKind kind, Vector3 position, SfxKind? sound = null)
        {
            playCounts[kind] = PlayCount(kind) + 1;
            SfxKind sfx = sound ?? SoundOf(kind);
            if (sfx == SfxKind.Thunder) SfxManager.Play(sfx); // sấm vang khắp nơi
            else SfxManager.Play(sfx, position);
            if (instance == null || !instance.systems.TryGetValue(kind, out ParticleSystem system) || system == null) return;

            var emit = new ParticleSystem.EmitParams { position = position, applyShapeToPosition = true };
            system.Emit(emit, BurstCount(kind));
            if (!system.isPlaying) system.Play(); // hệ đang dừng thì hạt vừa phun không được cập nhật (đứng im/không hiện)
            if (kind == VfxKind.Lightning) instance.Flash(position);
        }

        private static SfxKind SoundOf(VfxKind kind)
        {
            switch (kind)
            {
                case VfxKind.WoodChips: return SfxKind.Chop;
                case VfxKind.Leaves: return SfxKind.Rustle;
                case VfxKind.Splash: return SfxKind.Splash;
                case VfxKind.BigDust: return SfxKind.Crash;
                case VfxKind.Harvest: return SfxKind.Harvest;
                case VfxKind.Sparkle: return SfxKind.Chime;
                case VfxKind.Hit: return SfxKind.Hit;
                case VfxKind.Steam: return SfxKind.Hiss;
                case VfxKind.Lightning: return SfxKind.Thunder;
                default: return SfxKind.Dig;
            }
        }

        /// <summary>Bật/tắt mưa (lũ lụt).</summary>
        public static void SetRain(bool on)
        {
            SfxManager.SetRain(on);
            if (instance == null || instance.rain == null) return;
            if (on && !instance.rain.isEmitting) instance.rain.Play();
            else if (!on && instance.rain.isEmitting) instance.rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void Flash(Vector3 position)
        {
            flash.transform.position = position + Vector3.up * 6f;
            flash.enabled = true;
            flashTimer = 0.3f;
        }

        // ─── Dựng hệ hạt ────────────────────────────────────────────────────
        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f);

        private static int BurstCount(VfxKind kind)
        {
            switch (kind)
            {
                case VfxKind.WoodChips: return 6;
                case VfxKind.Leaves: return 14;
                case VfxKind.Splash: return 10;
                case VfxKind.Dust: return 6;
                case VfxKind.BigDust: return 22;
                case VfxKind.Harvest: return 12;
                case VfxKind.Sparkle: return 16;
                case VfxKind.Hit: return 6;
                case VfxKind.Steam: return 8;
                case VfxKind.Lightning: return 18;
                default: return 8;
            }
        }

        private struct Style
        {
            public Color a, b;
            public float lifetime, speed, size, gravity;
            public ParticleSystemShapeType shape;
            public float radius, angle;
            public bool rises;    // bay lên (hơi nước, lấp lánh) thay vì rơi
            public bool grows;    // to dần (bụi, khói) thay vì nhỏ dần
        }

        private static Style StyleOf(VfxKind kind)
        {
            switch (kind)
            {
                case VfxKind.WoodChips:
                    return new Style { a = Hex(0xc8a06a), b = Hex(0x8a6236), lifetime = 0.6f, speed = 2.6f, size = 0.09f, gravity = 1.4f, shape = ParticleSystemShapeType.Hemisphere, radius = 0.15f };
                case VfxKind.Leaves:
                    return new Style { a = Hex(0x5f9a3a), b = Hex(0x3f6b2a), lifetime = 1.4f, speed = 1.6f, size = 0.12f, gravity = 0.25f, shape = ParticleSystemShapeType.Sphere, radius = 0.5f };
                case VfxKind.Splash:
                    return new Style { a = Hex(0x8cc4e6), b = Hex(0x4687b8), lifetime = 0.6f, speed = 2.8f, size = 0.08f, gravity = 1.6f, shape = ParticleSystemShapeType.Cone, angle = 25f, radius = 0.2f };
                case VfxKind.Dust:
                    return new Style { a = Hex(0x93764c), b = Hex(0xa39d90), lifetime = 0.7f, speed = 0.9f, size = 0.16f, gravity = -0.05f, shape = ParticleSystemShapeType.Hemisphere, radius = 0.3f, grows = true };
                case VfxKind.BigDust:
                    return new Style { a = Hex(0xb8a888), b = Hex(0x8f8a80), lifetime = 1.1f, speed = 1.6f, size = 0.3f, gravity = -0.05f, shape = ParticleSystemShapeType.Circle, radius = 0.7f, grows = true };
                case VfxKind.Harvest:
                    return new Style { a = Hex(0xf2e27a), b = Hex(0x7cb446), lifetime = 0.9f, speed = 2.2f, size = 0.1f, gravity = 0.8f, shape = ParticleSystemShapeType.Cone, angle = 35f, radius = 0.4f };
                case VfxKind.Sparkle:
                    return new Style { a = Hex(0xffe9a0), b = Hex(0xffc94a), lifetime = 1.2f, speed = 1.2f, size = 0.09f, gravity = -0.15f, shape = ParticleSystemShapeType.Sphere, radius = 0.8f, rises = true };
                case VfxKind.Hit:
                    return new Style { a = Hex(0xb8322a), b = Hex(0x7a1f1a), lifetime = 0.45f, speed = 2.2f, size = 0.08f, gravity = 1.2f, shape = ParticleSystemShapeType.Sphere, radius = 0.1f };
                case VfxKind.Steam:
                    return new Style { a = Hex(0xe8ecee), b = Hex(0xa8b0b4), lifetime = 1.3f, speed = 0.9f, size = 0.22f, gravity = -0.12f, shape = ParticleSystemShapeType.Circle, radius = 0.4f, rises = true, grows = true };
                case VfxKind.Lightning:
                    return new Style { a = Color.white, b = Hex(0xffe9a0), lifetime = 0.5f, speed = 4f, size = 0.1f, gravity = 1f, shape = ParticleSystemShapeType.Hemisphere, radius = 0.2f };
                default:
                    return new Style { a = Color.white, b = Color.grey, lifetime = 0.5f, speed = 1f, size = 0.1f, shape = ParticleSystemShapeType.Sphere, radius = 0.2f };
            }
        }

        private ParticleSystem CreateBurstSystem(VfxKind kind)
        {
            Style style = StyleOf(kind);
            var go = new GameObject($"Vfx_{kind}");
            go.transform.SetParent(transform, false);
            // Phun lên trên: hình nón/bán cầu mặc định hướng theo trục Z → xoay cho trục Z chỉ lên trời.
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(style.lifetime * 0.7f, style.lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(style.speed * 0.5f, style.speed);
            main.startSize = new ParticleSystem.MinMaxCurve(style.size * 0.7f, style.size * 1.3f);
            main.startColor = new ParticleSystem.MinMaxGradient(style.a, style.b);
            main.gravityModifier = style.gravity;
            main.maxParticles = 400;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = style.shape;
            shape.radius = style.radius;
            shape.angle = style.angle;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, style.grows ? AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f) : AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;

            if (style.rises)
            {
                var velocity = ps.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.World;
                velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                velocity.y = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
                velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            }

            SetupRenderer(ps, ParticleSystemRenderMode.Billboard);
            return ps;
        }

        private ParticleSystem CreateRain()
        {
            var go = new GameObject("Vfx_Rain");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, 12f, 0f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // hộp phun nằm ngang, hạt rơi xuống
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.1f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 13f);
            main.startSize = 0.05f;
            main.startColor = new Color(0.62f, 0.75f, 0.88f, 0.75f);
            main.maxParticles = 1500;

            var emission = ps.emission;
            emission.rateOverTime = 600f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 36f, 0.1f);

            SetupRenderer(ps, ParticleSystemRenderMode.Stretch);
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.velocityScale = 0.04f;
            renderer.lengthScale = 1f;
            return ps;
        }

        private void SetupRenderer(ParticleSystem ps, ParticleSystemRenderMode mode)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = mode;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (particleMaterial != null) renderer.sharedMaterial = particleMaterial;
        }
    }
}
