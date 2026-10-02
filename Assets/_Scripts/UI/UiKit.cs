using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PrehistoricTribe
{
    /// <summary>Dựng phần tử giao diện bằng code (thanh công cụ, dải tài nguyên, tooltip, thông báo) cho cùng một phong cách.</summary>
    public static class UiKit
    {
        public static readonly Color PanelColor = new Color(0.12f, 0.09f, 0.06f, 0.9f);
        public static readonly Color SlotColor = new Color(0.22f, 0.16f, 0.1f, 0.95f);
        public static readonly Color TextColor = new Color(0.96f, 0.92f, 0.85f);
        public static readonly Color DimTextColor = new Color(0.62f, 0.58f, 0.52f);
        public static readonly Color AccentColor = new Color(1f, 0.82f, 0.29f);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        public static Image Fill(RectTransform rect, Color color, Sprite sprite = null, bool raycast = true)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.preserveAspect = sprite != null;
            image.raycastTarget = raycast;
            return image;
        }

        public static TMP_Text Text(RectTransform rect, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
        {
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            return tmp;
        }
    }

    /// <summary>Rê chuột vào phần tử → hiện tooltip (chữ tính lúc rê, luôn mới); rời ra → ẩn.</summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public System.Func<string> Content { get; set; }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Content != null) TooltipUI.Show(Content(), (RectTransform)transform, this);
        }

        public void OnPointerExit(PointerEventData eventData) => TooltipUI.Hide(this);

        private void OnDisable() => TooltipUI.Hide(this);
    }
}
