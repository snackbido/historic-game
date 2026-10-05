using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrehistoricTribe
{
    /// <summary>
    /// Tooltip dùng chung (RE-DESIGNUI bước 4): hộp chữ hiện phía trên phần tử đang rê chuột (sát mép trên thì lật xuống dưới),
    /// kẹp trong màn hình, không chặn chuột. Đặt object này gần cuối Canvas để nằm trên các bảng khác.
    /// </summary>
    public class TooltipUI : MonoBehaviour
    {
        private static TooltipUI instance;

        private RectTransform box;
        private TMP_Text label;
        private Object owner;

        public static bool Visible => instance != null && instance.box.gameObject.activeSelf;
        public static string Text => instance != null ? instance.label.text : null;

        private void Awake()
        {
            instance = this;
            var rect = (RectTransform)transform;
            UiKit.Stretch(rect);

            box = UiKit.Rect("Box", transform);
            UiKit.Anchor(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            UiKit.Fill(box, new Color(0.08f, 0.06f, 0.04f, 0.95f), raycast: false);
            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.childControlWidth = layout.childControlHeight = true;
            var fitter = box.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            label = UiKit.Text(UiKit.Rect("Text", box), string.Empty, 18f, UiKit.TextColor, TextAlignmentOptions.TopLeft);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            box.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>Hiện tooltip cho <paramref name="target"/>; <paramref name="from"/> là nơi gọi (chỉ nơi đó mới ẩn được).</summary>
        public static void Show(string text, RectTransform target, Object from = null)
        {
            if (instance == null || string.IsNullOrEmpty(text)) return;
            instance.owner = from;
            instance.label.text = text;
            instance.box.gameObject.SetActive(true);
            instance.Place(target);
        }

        public static void Hide(Object from = null)
        {
            if (instance == null || (from != null && instance.owner != from)) return;
            instance.owner = null;
            instance.box.gameObject.SetActive(false);
        }

        /// <summary>Làm mới chữ nếu tooltip đang hiện cho <paramref name="from"/> (vd tài nguyên vừa đổi).</summary>
        public static void Refresh(Object from, string text)
        {
            if (instance == null || !Visible || instance.owner != from) return;
            instance.label.text = text;
        }

        private void Place(RectTransform target)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(box);
            var canvasRect = (RectTransform)transform;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners); // 0 dưới-trái, 1 trên-trái, 2 trên-phải, 3 dưới-phải
            Vector2 top = ToLocal(canvasRect, (corners[1] + corners[2]) * 0.5f);
            Vector2 bottom = ToLocal(canvasRect, (corners[0] + corners[3]) * 0.5f);

            Rect area = canvasRect.rect;
            Vector2 size = box.rect.size;
            bool above = top.y + 8f + size.y <= area.yMax;
            box.pivot = new Vector2(0.5f, above ? 0f : 1f);
            Vector2 position = above ? top + new Vector2(0f, 8f) : bottom - new Vector2(0f, 8f);
            position.x = Mathf.Clamp(position.x, area.xMin + size.x * 0.5f + 4f, area.xMax - size.x * 0.5f - 4f);
            box.anchoredPosition = position;
        }

        private static Vector2 ToLocal(RectTransform canvasRect, Vector3 world)
        {
            // Canvas phủ màn hình: không cần camera; canvas gắn camera (vd lúc chụp ảnh kiểm tra) thì phải dùng camera đó.
            Canvas canvas = canvasRect.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, cam, out Vector2 local);
            return local;
        }
    }
}
