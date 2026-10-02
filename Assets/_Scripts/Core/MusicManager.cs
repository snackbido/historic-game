using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Nhạc nền (M7/P2): ban ngày một bài, ban đêm một bài, chuyển bài thì nhỏ dần bài cũ / to dần bài mới.
    /// Bài nhạc là file bất kỳ — thay bằng nhạc riêng: đặt Day/Night (.wav/.mp3/.ogg) vào Assets/Audio/Music rồi dựng lại scene,
    /// hoặc kéo thả thẳng vào ô Day Music / Night Music của object MusicManager. Phím <see cref="muteKey"/> tắt/bật nhạc.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        [SerializeField] private AudioClip dayMusic;
        [Tooltip("Để trống thì ban đêm vẫn phát nhạc ngày")]
        [SerializeField] private AudioClip nightMusic;
        [SerializeField, Range(0f, 1f)] private float baseVolume = 0.45f;
        [Tooltip("Thời gian (giây thật) nhỏ dần / to dần khi chuyển bài")]
        [SerializeField] private float fadeSeconds = 4f;
        [SerializeField] private KeyCode muteKey = KeyCode.M;

        private const string VolumeKey = "music_volume";
        private const string MutedKey = "music_muted";

        public static MusicManager Instance { get; private set; }

        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource current;

        public AudioClip DayMusic => dayMusic;
        public AudioClip NightMusic => nightMusic;
        /// <summary>Bài đang phát (hoặc đang to dần lên).</summary>
        public AudioClip CurrentClip => current != null ? current.clip : null;

        /// <summary>Âm lượng nhạc người chơi chọn (0..1), lưu giữa các lần chơi.</summary>
        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1f);
            set => PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
        }

        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MutedKey, 0) == 1;
            set => PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
        }

        private float TargetVolume => Muted ? 0f : baseVolume * Volume;

        private void Awake()
        {
            Instance = this;
            sourceA = CreateSource();
            sourceB = CreateSource();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f; // nhạc nền: không phụ thuộc vị trí
            source.volume = 0f;
            return source;
        }

        private void Start()
        {
            current = sourceA;
            current.clip = WantedClip();
            current.volume = TargetVolume;
            if (current.clip != null) current.Play();
        }

        private void Update()
        {
            if (Input.GetKeyDown(muteKey))
            {
                Muted = !Muted;
                EventBus.RaiseNotification(Muted ? "Nhạc: tắt (M để bật)" : "Nhạc: bật");
            }

            AudioClip wanted = WantedClip();
            if (wanted != current.clip)
            {
                // Bài mới phát trên nguồn kia, to dần; bài cũ nhỏ dần rồi dừng.
                current = current == sourceA ? sourceB : sourceA;
                current.clip = wanted;
                current.volume = 0f;
                if (wanted != null) current.Play();
            }

            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds);
            Fade(sourceA, step);
            Fade(sourceB, step);
        }

        private void Fade(AudioSource source, float step)
        {
            float target = source == current ? TargetVolume : 0f;
            source.volume = Mathf.MoveTowards(source.volume, target, step * Mathf.Max(baseVolume, 0.01f));
            if (source != current && source.volume <= 0f && source.isPlaying) source.Stop();
        }

        private AudioClip WantedClip()
        {
            bool night = DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight;
            return night && nightMusic != null ? nightMusic : dayMusic;
        }
    }
}
