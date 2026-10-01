using TMPro;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>Băng cảnh báo giữa mép trên màn hình: điềm báo thiên tai sắp tới / thiên tai đang diễn ra + thời gian còn lại.</summary>
    public class DisasterBannerUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color warningColor = new Color(1f, 0.82f, 0.35f);
        [SerializeField] private Color activeColor = new Color(1f, 0.45f, 0.35f);

        private void Update()
        {
            if (label == null) return;
            DisasterManager manager = DisasterManager.Instance;
            string text = Describe(manager);
            label.text = text ?? string.Empty;
            if (text != null) label.color = manager.Phase == DisasterPhase.Active ? activeColor : warningColor;
        }

        public static string Describe(DisasterManager manager)
        {
            if (manager == null || manager.Current == null) return null;
            string left = FormatHours(manager.PhaseSecondsLeft, manager.DayLength);
            DisasterEvent e = manager.Current;
            return manager.Phase switch
            {
                DisasterPhase.Warning => $"Điềm báo: {e.WarningMessage} — {e.DisplayName.ToLowerInvariant()} sắp tới (còn {left})",
                DisasterPhase.Active => $"{e.DisplayName.ToUpperInvariant()}: {e.ActiveMessage} (còn {left})",
                _ => null
            };
        }

        /// <summary>Giây game → "N giờ" / "dưới 1 giờ" theo đồng hồ 24 giờ của một ngày game.</summary>
        public static string FormatHours(float seconds, float dayLength)
        {
            int hours = Mathf.CeilToInt(seconds / Mathf.Max(1f, dayLength) * 24f);
            return hours <= 1 ? "khoảng 1 giờ" : $"khoảng {hours} giờ";
        }
    }
}
