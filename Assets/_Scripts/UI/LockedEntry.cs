using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrehistoricTribe
{
    /// <summary>
    /// Nút của mục còn khóa (công trình / cây trồng / công nghệ chưa đủ điều kiện): vẫn hiện để người chơi biết là có,
    /// chữ mờ kèm "(khóa)", bấm vào thì báo cần làm gì để mở (M7/P4).
    /// </summary>
    public static class LockedEntry
    {
        public static readonly Color LockedTextColor = new Color(0.55f, 0.55f, 0.55f);
        public const string LockedSuffix = " (khóa)";

        public static void Apply(Button button, string name, System.Func<string> hint)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = name + LockedSuffix;
                label.color = LockedTextColor;
            }
            // Gợi ý tính lúc bấm → số tri thức "đang có" luôn mới.
            button.onClick.AddListener(() => EventBus.RaiseNotification(hint()));
        }

        public static bool IsLocked(Button button)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            return label != null && label.text.EndsWith(LockedSuffix);
        }
    }
}
