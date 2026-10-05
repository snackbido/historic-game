using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Chu kỳ ngày/đêm (Milestone 5c, quyết định 2026-10-01): một ngày 20 phút, ~30% là đêm.
    /// Chỉ thể hiện bằng ánh sáng (không đồng hồ): mặt trời đi qua bầu trời, hoàng hôn đổi màu,
    /// đêm có ánh trăng xanh mờ, trời + sương mù tối lại.
    /// TimeOfDay: 0 = bình minh, tăng dần; đêm bắt đầu ở <see cref="nightStart"/> và kéo dài tới hết chu kỳ.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance { get; private set; }

        /// <summary>Tắt để test cũ chạy ở ban ngày cố định; test ngày/đêm tự bật hoặc đặt giờ bằng SetTime.</summary>
        public static bool Running { get; set; } = true;

        [SerializeField] private float dayLengthSeconds = 1200f;
        [Tooltip("Phần của chu kỳ mà đêm bắt đầu (0,7 → đêm chiếm 30% = 6 phút)")]
        [SerializeField, Range(0.3f, 0.95f)] private float nightStart = 0.7f;
        [Tooltip("Độ dài hoàng hôn/bình minh (phần của chu kỳ)")]
        [SerializeField, Range(0.01f, 0.1f)] private float twilight = 0.04f;
        [SerializeField, Range(0f, 1f)] private float startTime = 0.05f;

        [Header("Cảnh")]
        [SerializeField] private Light sun;
        [SerializeField] private Camera sceneCamera;

        [Header("Màu & độ sáng")]
        // Tông ấm hơn (2026-10-05, theo ảnh mẫu người dùng gửi): nắng vàng đậm, trời/sương ngả kem-vàng thay vì
        // xanh nhạt lạnh, ambient ngả vàng nhẹ thay vì xám trung tính — cho cảnh có cảm giác "chiều vàng" ấm cúng.
        [SerializeField] private Color daySunColor = new Color(1f, 0.82f, 0.56f);
        [SerializeField] private Color duskSunColor = new Color(1f, 0.55f, 0.3f);
        [SerializeField] private Color moonColor = new Color(0.55f, 0.65f, 1f);
        [SerializeField] private float daySunIntensity = 1.15f;
        [SerializeField] private float moonIntensity = 0.18f;
        [SerializeField] private Color dayAmbient = new Color(0.47f, 0.41f, 0.32f);
        [SerializeField] private Color nightAmbient = new Color(0.1f, 0.12f, 0.2f);
        [SerializeField] private Color daySky = new Color(0.93f, 0.83f, 0.64f);
        [SerializeField] private Color duskSky = new Color(0.85f, 0.6f, 0.45f);
        [SerializeField] private Color nightSky = new Color(0.04f, 0.06f, 0.12f);

        private bool wasNight;

        public float TimeOfDay { get; private set; }
        public int Day { get; private set; } = 1;
        public float DayLengthSeconds => dayLengthSeconds;
        public bool IsNight => TimeOfDay >= nightStart;

        /// <summary>1 = ban ngày sáng hẳn, 0 = đêm tối hẳn; chuyển mượt lúc hoàng hôn/bình minh.</summary>
        public float Daylight
        {
            get
            {
                float t = TimeOfDay;
                if (t >= nightStart - twilight && t < nightStart) return Mathf.SmoothStep(1f, 0f, (t - (nightStart - twilight)) / twilight);
                if (t >= 1f - twilight) return Mathf.SmoothStep(0f, 1f, (t - (1f - twilight)) / twilight);
                return t < nightStart ? 1f : 0f;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            TimeOfDay = startTime;
            wasNight = IsNight;
            ApplyLighting();
        }

        private void Update()
        {
            if (!Running || dayLengthSeconds <= 0f) return;
            Advance(Time.deltaTime / dayLengthSeconds);
        }

        private void Advance(float fraction)
        {
            float t = TimeOfDay + fraction;
            while (t >= 1f)
            {
                t -= 1f;
                Day++;
            }
            SetTime(t);
        }

        /// <summary>Đặt giờ trong ngày (0..1). Bắn sự kiện nếu vừa chuyển giữa ngày và đêm.</summary>
        public void SetTime(float timeOfDay, int? day = null)
        {
            TimeOfDay = Mathf.Repeat(timeOfDay, 1f);
            if (day.HasValue) Day = Mathf.Max(1, day.Value);
            ApplyLighting();

            bool night = IsNight;
            if (night == wasNight) return;
            wasNight = night;
            if (night) EventBus.RaiseNightStarted();
            else EventBus.RaiseDayStarted();
        }

        private void ApplyLighting()
        {
            float daylight = Daylight;

            // Gần hoàng hôn/bình minh (ánh sáng chưa đầy) thì nắng ngả cam.
            float duskAmount = 1f - Mathf.Abs(daylight * 2f - 1f); // 0 lúc trưa/đêm, 1 lúc giữa hoàng hôn
            Color sunColor = Color.Lerp(moonColor, Color.Lerp(daySunColor, duskSunColor, duskAmount), daylight);
            Color sky = Color.Lerp(Color.Lerp(nightSky, daySky, daylight), duskSky, duskAmount * 0.6f);

            if (sun != null)
            {
                sun.color = sunColor;
                sun.intensity = Mathf.Lerp(moonIntensity, daySunIntensity, daylight);
                sun.transform.rotation = Quaternion.Euler(SunElevation(), -30f + TimeOfDay * 120f, 0f);
            }

            RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, daylight);
            RenderSettings.fogColor = sky;
            if (sceneCamera != null) sceneCamera.backgroundColor = sky;
        }

        /// <summary>Ban ngày mặt trời lên từ thấp → cao nhất buổi trưa → thấp lúc chiều; ban đêm trăng ở độ cao cố định.</summary>
        private float SunElevation()
        {
            if (IsNight) return 45f;
            float dayProgress = TimeOfDay / nightStart; // 0 sáng sớm → 1 chạng vạng
            return 15f + 50f * Mathf.Sin(dayProgress * Mathf.PI);
        }
    }
}
