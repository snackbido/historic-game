using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Nhịp độ kinh tế của game (M7/P4 — user chơi thử thấy "nhịp độ hơi nhanh"): mọi thời gian "chờ đợi" của làng
    /// (cây lớn, đất khô, cỏ mọc, dân làm việc, đói, tri thức, hỏng đồ, sinh nở, lớn lên, thú nuôi, phơi lúa) nhân với
    /// <see cref="Factor"/>. Di chuyển, chiến đấu, thiên tai và chu kỳ ngày/đêm không đổi. Người chơi chọn mức trong Cài đặt.
    /// </summary>
    public static class GamePace
    {
        public struct Preset
        {
            public string name;
            public float factor;
        }

        public static readonly Preset[] Presets =
        {
            new Preset { name = "Thong thả", factor = 2f },
            new Preset { name = "Vừa phải", factor = 1.5f },
            new Preset { name = "Nhanh", factor = 1f },
        };

        public const int DefaultPreset = 1;
        private const string PrefKey = "game_pace";

        private static float? factorOverride;

        /// <summary>Thời gian chờ nhân với số này (2 = chậm gấp đôi bản gốc).</summary>
        public static float Factor
        {
            get => factorOverride ?? Presets[PresetIndex].factor;
            // Test đặt thẳng hệ số (không lưu vào lựa chọn của người chơi); null = theo lựa chọn.
            set => factorOverride = value;
        }

        public static void ClearOverride() => factorOverride = null;

        /// <summary>Mức người chơi chọn (lưu giữa các lần chơi).</summary>
        public static int PresetIndex
        {
            get
            {
                // Đọc PlayerPrefs một lần rồi nhớ (Factor được hỏi mỗi khung hình ở nhiều nơi).
                if (cachedIndex < 0) cachedIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, DefaultPreset), 0, Presets.Length - 1);
                return cachedIndex;
            }
            set
            {
                cachedIndex = Mathf.Clamp(value, 0, Presets.Length - 1);
                PlayerPrefs.SetInt(PrefKey, cachedIndex);
            }
        }

        private static int cachedIndex = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => cachedIndex = -1;

        public static string PresetName => Presets[PresetIndex].name;

        /// <summary>Thời gian chờ (giây) sau khi giãn theo nhịp độ.</summary>
        public static float Duration(float seconds) => seconds * Factor;

        /// <summary>Thời gian đã trôi, quy về "thời gian gốc" (dùng khi cộng dồn bộ đếm so với ngưỡng gốc).</summary>
        public static float Scaled(float deltaTime) => deltaTime / Factor;
    }
}
