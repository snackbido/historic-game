using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    public enum SfxKind
    {
        Chop,     // chặt cây
        Rustle,   // lá xào xạc (cây đổ, làm cỏ, khai hoang)
        Splash,   // nước
        Dig,      // cuốc, gieo, giã
        Build,    // dựng công trình (gõ búa)
        Crash,    // sập, đổ
        Harvest,  // gặt hái
        Chime,    // nâng cấp, mở công nghệ
        Hit,      // đánh trúng
        Hiss,     // dập lửa
        Thunder,  // sấm sét
        Howl,     // sói hú
        Rain,     // mưa (lặp)
        Fire      // lửa cháy (lặp)
    }

    /// <summary>
    /// Hiệu ứng âm thanh (M7/P2b): phát một lần tại vị trí (nghe to nhỏ theo khoảng cách tới camera) hoặc lặp
    /// (mưa, lửa). Tiếng sinh bằng code (Editor/SfxBuilder), thay được bằng file riêng cùng tên trong Assets/Audio/SFX.
    /// Không có manager trong scene thì mọi lời gọi bỏ qua.
    /// </summary>
    public class SfxManager : MonoBehaviour
    {
        [System.Serializable]
        public struct Entry
        {
            public SfxKind kind;
            public AudioClip clip;
        }

        [SerializeField] private List<Entry> clips = new List<Entry>();
        [SerializeField, Range(0f, 1f)] private float baseVolume = 0.7f;
        [SerializeField] private int voices = 10;

        private const string VolumeKey = "sfx_volume";
        /// <summary>Cùng một tiếng không phát lại trong khoảng này (nhiều người chặt cây cùng lúc).</summary>
        private const float MinRepeatSeconds = 0.08f;

        private static SfxManager instance;
        private static readonly Dictionary<SfxKind, int> playCounts = new Dictionary<SfxKind, int>();

        private readonly Dictionary<SfxKind, AudioClip> lookup = new Dictionary<SfxKind, AudioClip>();
        private readonly Dictionary<SfxKind, float> lastPlayed = new Dictionary<SfxKind, float>();
        private readonly List<AudioSource> pool = new List<AudioSource>();
        private int nextVoice;
        private AudioSource rain;

        public static int PlayCount(SfxKind kind) => playCounts.TryGetValue(kind, out int n) ? n : 0;
        public static bool HasClip(SfxKind kind) => instance != null && instance.lookup.TryGetValue(kind, out var c) && c != null;
        public static bool IsRaining => instance != null && instance.rain != null && instance.rain.isPlaying;

        /// <summary>Âm lượng hiệu ứng người chơi chọn (0..1), lưu giữa các lần chơi.</summary>
        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
                if (instance != null) instance.ApplyLoopVolumes();
            }
        }

        private float Gain => baseVolume * Volume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => playCounts.Clear();

        private void Awake()
        {
            instance = this;
            foreach (var entry in clips) lookup[entry.kind] = entry.clip;
            for (int i = 0; i < voices; i++)
            {
                var go = new GameObject($"Voice_{i}");
                go.transform.SetParent(transform, false);
                pool.Add(Configure(go.AddComponent<AudioSource>(), spatial: true));
            }
            rain = Configure(gameObject.AddComponent<AudioSource>(), spatial: false);
            rain.loop = true;
            rain.clip = Clip(SfxKind.Rain);
        }

        private void OnEnable()
        {
            EventBus.OnTechUnlocked += HandleTechUnlocked;
        }

        private void OnDisable()
        {
            EventBus.OnTechUnlocked -= HandleTechUnlocked;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void HandleTechUnlocked(TechNode _) => Play(SfxKind.Chime);

        private static AudioSource Configure(AudioSource source, bool spatial)
        {
            source.playOnAwake = false;
            // Camera nhìn từ trên cao ~15–20m: nửa 3D để tiếng gần to hơn, tiếng xa vẫn nghe được.
            source.spatialBlend = spatial ? 0.6f : 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 12f;
            source.maxDistance = 45f;
            source.dopplerLevel = 0f;
            return source;
        }

        private AudioClip Clip(SfxKind kind) => lookup.TryGetValue(kind, out var clip) ? clip : null;

        // ─── API ────────────────────────────────────────────────────────────
        /// <summary>Phát một lần tại vị trí (null = nghe đều như tiếng xa, vd sói hú, sấm).</summary>
        public static void Play(SfxKind kind, Vector3? position = null, float volume = 1f)
        {
            playCounts[kind] = PlayCount(kind) + 1;
            if (instance == null) return;
            instance.PlayInternal(kind, position, volume);
        }

        private void PlayInternal(SfxKind kind, Vector3? position, float volume)
        {
            AudioClip clip = Clip(kind);
            if (clip == null || pool.Count == 0) return;
            if (lastPlayed.TryGetValue(kind, out float last) && Time.unscaledTime - last < MinRepeatSeconds) return;
            lastPlayed[kind] = Time.unscaledTime;

            AudioSource voice = pool[nextVoice];
            nextVoice = (nextVoice + 1) % pool.Count;
            voice.transform.position = position ?? (Camera.main != null ? Camera.main.transform.position : transform.position);
            voice.spatialBlend = position.HasValue ? 0.6f : 0f;
            voice.pitch = Random.Range(0.92f, 1.08f); // khỏi nghe lặp máy móc
            voice.clip = clip;
            voice.volume = Gain * volume;
            voice.Play();
        }

        /// <summary>Bật/tắt tiếng mưa (lũ lụt).</summary>
        public static void SetRain(bool on)
        {
            if (instance == null || instance.rain == null || instance.rain.clip == null) return;
            if (on && !instance.rain.isPlaying)
            {
                instance.rain.volume = instance.Gain * 0.6f;
                instance.rain.Play();
            }
            else if (!on && instance.rain.isPlaying) instance.rain.Stop();
        }

        /// <summary>Gắn tiếng lặp (vd lửa cháy) vào đối tượng; hủy đối tượng/thành phần thì tiếng tắt theo.</summary>
        public static AudioSource AttachLoop(GameObject target, SfxKind kind, float volume = 1f)
        {
            if (instance == null) return null;
            AudioClip clip = instance.Clip(kind);
            if (clip == null) return null;
            var source = Configure(target.AddComponent<AudioSource>(), spatial: true);
            source.loop = true;
            source.clip = clip;
            source.volume = instance.Gain * volume;
            source.time = Random.Range(0f, clip.length); // nhiều đám cháy không đồng thanh
            source.Play();
            return source;
        }

        private void ApplyLoopVolumes()
        {
            if (rain != null) rain.volume = Gain * 0.6f;
        }
    }
}
