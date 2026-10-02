using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrehistoricTribe
{
    /// <summary>
    /// Thông báo trượt xuống ở giữa trên màn hình (RE-DESIGNUI bước 5), thay dòng chữ thông báo cũ: mọi
    /// <see cref="EventBus.RaiseNotification"/> hiện ở đây. Thông báo có hành động (vd "Có thể nghiên cứu…") bấm vào được.
    /// </summary>
    public class ToastUI : MonoBehaviour
    {
        [SerializeField] private float displaySeconds = 2.5f;
        [Tooltip("Thêm thời gian đọc cho câu dài (giây mỗi 10 ký tự)")]
        [SerializeField] private float extraSecondsPer10Chars = 0.25f;

        private static ToastUI instance;

        private RectTransform box;
        private TMP_Text label;
        private CanvasGroup group;
        private Button button;
        private System.Action onClick;
        private float shownAt = float.NegativeInfinity;
        private float duration;

        public static string Current => instance != null && instance.IsShowing ? instance.label.text : null;
        public static bool HasAction => instance != null && instance.IsShowing && instance.onClick != null;
        private bool IsShowing => box.gameObject.activeSelf;

        private void Awake()
        {
            instance = this;
            var rect = (RectTransform)transform;
            UiKit.Anchor(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(1100f, 60f));

            box = UiKit.Rect("Box", transform);
            UiKit.Anchor(box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            var background = UiKit.Fill(box, UiKit.PanelColor);
            button = box.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(Click);
            group = box.gameObject.AddComponent<CanvasGroup>();
            var layout = box.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 8, 8);
            layout.childControlWidth = layout.childControlHeight = true;
            var fitter = box.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var labelRect = UiKit.Rect("Text", box);
            label = UiKit.Text(labelRect, string.Empty, 22f, UiKit.TextColor);
            labelRect.gameObject.AddComponent<LayoutElement>().preferredWidth = -1f;
            box.gameObject.SetActive(false);
        }

        private void OnEnable() => EventBus.OnNotification += HandleNotification;
        private void OnDisable() => EventBus.OnNotification -= HandleNotification;

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void HandleNotification(string message) => Show(message);

        public static void Show(string message, System.Action action = null)
        {
            if (instance == null || string.IsNullOrEmpty(message)) return;
            instance.ShowInternal(message, action);
        }

        private void ShowInternal(string message, System.Action action)
        {
            onClick = action;
            // Câu quá dài thì xuống dòng trong khung rộng tối đa.
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.text = action != null ? $"{message}  <color=#FFD24A><size=80%>(bấm để xem)</size></color>" : message;
            var element = label.GetComponent<LayoutElement>();
            element.preferredWidth = label.GetPreferredValues().x > 1060f ? 1060f : -1f;
            label.textWrappingMode = element.preferredWidth > 0f ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            button.interactable = action != null;
            box.gameObject.SetActive(true);
            shownAt = Time.unscaledTime;
            duration = displaySeconds + message.Length / 10f * extraSecondsPer10Chars;
        }

        private void Update()
        {
            if (!IsShowing) return;
            float age = Time.unscaledTime - shownAt;
            // Trượt xuống 0,2s đầu, mờ dần 0,3s cuối.
            float slide = Mathf.Clamp01(age / 0.2f);
            box.anchoredPosition = new Vector2(0f, Mathf.Lerp(40f, 0f, slide));
            group.alpha = Mathf.Clamp01((duration - age) / 0.3f) * slide;
            if (age >= duration) box.gameObject.SetActive(false);
        }

        private void Click()
        {
            System.Action action = onClick;
            box.gameObject.SetActive(false);
            action?.Invoke();
        }

        /// <summary>Cho test: bấm vào thông báo đang hiện.</summary>
        public static void ClickCurrent()
        {
            if (instance != null && instance.IsShowing) instance.Click();
        }
    }
}
