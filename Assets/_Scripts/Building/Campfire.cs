using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Đống lửa trại (Milestone 5c): tự cháy khi trời tối, chiếu sáng quanh trại (ánh lửa bập bùng).
    /// Người chưa có lều ngủ quanh đây; thú dữ không dám lại gần trong <see cref="SafeRadius"/> khi lửa cháy.
    /// </summary>
    public class Campfire : MonoBehaviour
    {
        private static readonly List<Campfire> all = new List<Campfire>();
        public static IReadOnlyList<Campfire> All => all;

        [SerializeField] private Light fireLight;
        [Tooltip("Ngọn lửa (hiện khi cháy)")]
        [SerializeField] private GameObject flames;
        [SerializeField] private float lightIntensity = 2.5f;
        [Tooltip("Thú dữ không dám vào trong bán kính này khi lửa cháy (m)")]
        [SerializeField] private float safeRadius = 6f;
        [Tooltip("Trời tối dưới mức này (Daylight) thì nhóm lửa")]
        [SerializeField, Range(0f, 1f)] private float lightBelowDaylight = 0.6f;
        [Tooltip("Người chưa có lều ngủ quanh đây được (đống lửa trại có, đuốc thì không)")]
        [SerializeField] private bool isSleepSpot = true;

        public float SafeRadius => safeRadius;
        public bool IsSleepSpot => isSleepSpot;
        public bool IsLit { get; private set; }

        private void OnEnable()
        {
            all.Add(this);
            if (started) Refresh(force: true);
        }

        private void OnDisable()
        {
            all.Remove(this);
            // Tắt (vd đuốc bị đổ): không còn sáng, không còn che chở.
            IsLit = false;
            if (flames != null) flames.SetActive(false);
            if (fireLight != null) fireLight.enabled = false;
        }

        private bool started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => all.Clear();

        private void Start()
        {
            started = true;
            Refresh(force: true);
        }

        private void Update() => Refresh(force: false);

        private void Refresh(bool force)
        {
            DayNightCycle cycle = DayNightCycle.Instance;
            bool lit = cycle != null && cycle.Daylight < lightBelowDaylight;
            if (force || lit != IsLit)
            {
                IsLit = lit;
                if (flames != null) flames.SetActive(lit);
                if (fireLight != null) fireLight.enabled = lit;
            }

            // Lửa bập bùng: dao động nhẹ độ sáng.
            if (IsLit && fireLight != null)
                fireLight.intensity = lightIntensity * (0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 3f, 0.3f));
        }

        /// <summary>Điểm này có đang được một đống lửa đang cháy bảo vệ không.</summary>
        public static bool IsProtected(Vector3 position)
        {
            foreach (var fire in all)
                if (fire.IsLit && InteractableRegistry.GroundDistance(position, fire.transform.position) <= fire.SafeRadius)
                    return true;
            return false;
        }
    }
}
