using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PrehistoricTribe
{
    /// <summary>
    /// Menu (M7/P3), tự dựng giao diện bằng code khi chạy: menu chính lúc vào game (Chơi mới / Chơi tiếp / Cài đặt / Thoát),
    /// menu tạm dừng (Esc) và màn cài đặt (âm lượng nhạc + tiếng động, chế độ pixel, toàn màn hình). Mở menu là dừng thời gian
    /// trong game và các script điều khiển ngừng nhận phím (<see cref="IsOpen"/>).
    /// </summary>
    public class GameMenuUI : MonoBehaviour
    {
        public enum MenuScreen { None, Main, Pause, Settings }

        /// <summary>Vào scene thì hiện menu chính (test tắt đi để chơi ngay).</summary>
        public static bool ShowMainMenuOnStart { get; set; } = true;

        [SerializeField] private string gameTitle = "BỘ LẠC TIỀN SỬ";

        private static GameMenuUI instance;
        public static bool IsOpen => instance != null && instance.Current != MenuScreen.None;
        public static GameMenuUI Instance => instance;

        public MenuScreen Current { get; private set; } = MenuScreen.None;

        private GameObject overlay;
        private GameObject mainPanel;
        private GameObject pausePanel;
        private GameObject settingsPanel;
        private Button continueButton;
        private Slider musicSlider;
        private Slider sfxSlider;
        private TMP_Text pixelLabel;
        private TMP_Text fullscreenLabel;
        private MenuScreen settingsReturn = MenuScreen.Main;
        private float timeScaleBefore = 1f;
        private bool placerWasPlacing;

        private static readonly Color PanelColor = new Color(0.12f, 0.09f, 0.06f, 0.94f);
        private static readonly Color ButtonColor = new Color(0.38f, 0.26f, 0.15f);
        private static readonly Color TextColor = new Color(0.96f, 0.92f, 0.85f);
        private static readonly Color TitleColor = new Color(1f, 0.8f, 0.42f);

        private void Awake()
        {
            instance = this;
            BuildUI();
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
            if (Current != MenuScreen.None) Time.timeScale = timeScaleBefore; // rời scene khi đang mở menu
        }

        private void Start()
        {
            if (ShowMainMenuOnStart) Open(MenuScreen.Main);
            else ShowPanels(MenuScreen.None);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            switch (Current)
            {
                case MenuScreen.Settings: Show(settingsReturn); break;
                case MenuScreen.Pause: Resume(); break;
                case MenuScreen.None:
                    // Esc đang dùng để hủy đặt công trình thì không mở menu.
                    if (!placerWasPlacing) Open(MenuScreen.Pause);
                    break;
            }
        }

        private void LateUpdate()
        {
            placerWasPlacing = BuildingPlacer.Instance != null && BuildingPlacer.Instance.IsPlacing;
        }

        // ─── Điều hướng ─────────────────────────────────────────────────────
        public void Open(MenuScreen screen)
        {
            if (Current == MenuScreen.None)
            {
                timeScaleBefore = Time.timeScale;
                Time.timeScale = 0f;
            }
            Show(screen);
        }

        private void Show(MenuScreen screen)
        {
            Current = screen;
            ShowPanels(screen);
            if (screen == MenuScreen.Main) continueButton.interactable = SaveSystem.HasSaveFile();
            if (screen == MenuScreen.Settings) RefreshSettings();
        }

        private void ShowPanels(MenuScreen screen)
        {
            overlay.SetActive(screen != MenuScreen.None);
            mainPanel.SetActive(screen == MenuScreen.Main);
            pausePanel.SetActive(screen == MenuScreen.Pause);
            settingsPanel.SetActive(screen == MenuScreen.Settings);
        }

        /// <summary>Đóng menu, thời gian chạy lại như trước khi mở.</summary>
        public void Resume()
        {
            if (Current == MenuScreen.None) return;
            Current = MenuScreen.None;
            ShowPanels(MenuScreen.None);
            Time.timeScale = timeScaleBefore;
        }

        public void NewGame() => Resume(); // scene vừa nạp đã là ván mới

        public void ContinueGame()
        {
            if (!SaveSystem.HasSaveFile()) return;
            Resume();
            GameManager.Instance.LoadGame();
        }

        public void OpenSettings()
        {
            settingsReturn = Current == MenuScreen.Pause ? MenuScreen.Pause : MenuScreen.Main;
            Show(MenuScreen.Settings);
        }

        public void SaveFromPause()
        {
            GameManager.Instance.SaveGame();
            EventBus.RaiseNotification("Đã lưu game");
        }

        /// <summary>Về menu chính: nạp lại scene (ván chưa lưu sẽ mất).</summary>
        public void BackToMainMenu()
        {
            Time.timeScale = timeScaleBefore;
            Current = MenuScreen.None;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ─── Cài đặt ────────────────────────────────────────────────────────
        private void RefreshSettings()
        {
            musicSlider.SetValueWithoutNotify(MusicManager.Volume);
            sfxSlider.SetValueWithoutNotify(SfxManager.Volume);
            pixelLabel.text = $"Đồ họa pixel: {(PixelModeOn() ? "BẬT" : "TẮT")}";
            fullscreenLabel.text = $"Toàn màn hình: {(Screen.fullScreen ? "BẬT" : "TẮT")}";
        }

        private static bool PixelModeOn()
        {
            var pixel = Camera.main != null ? Camera.main.GetComponent<PixelArtCamera>() : null;
            return pixel != null ? pixel.PixelModeOn : PixelArtCamera.SavedPreference;
        }

        public void TogglePixelMode()
        {
            bool on = !PixelModeOn();
            PixelArtCamera.SavedPreference = on;
            var pixel = Camera.main != null ? Camera.main.GetComponent<PixelArtCamera>() : null;
            if (pixel != null) pixel.PixelModeOn = on;
            RefreshSettings();
        }

        public void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            fullscreenLabel.text = $"Toàn màn hình: {(!Screen.fullScreen ? "BẬT" : "TẮT")}"; // áp dụng ở khung hình sau
        }

        // ─── Dựng giao diện ─────────────────────────────────────────────────
        private void BuildUI()
        {
            transform.SetAsLastSibling(); // nằm trên mọi bảng khác

            overlay = new GameObject("MenuOverlay", typeof(RectTransform));
            overlay.transform.SetParent(transform, false);
            Stretch((RectTransform)overlay.transform);
            overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f); // chặn chuột xuống cảnh

            mainPanel = Panel("MainMenu");
            Label(mainPanel.transform, gameTitle, 46f, FontStyles.Bold, TitleColor, 64f);
            Label(mainPanel.transform, "Dẫn dắt bộ lạc qua mùa màng, thú dữ và thiên tai", 18f, FontStyles.Italic, TextColor, 30f);
            MenuButton(mainPanel.transform, "Chơi mới", NewGame);
            continueButton = MenuButton(mainPanel.transform, "Chơi tiếp (bản lưu)", ContinueGame);
            MenuButton(mainPanel.transform, "Cài đặt", OpenSettings);
            MenuButton(mainPanel.transform, "Thoát", Quit);
            Label(mainPanel.transform,
                "WASD di chuyển · E tương tác · F đâm giáo · R ném đá\nChuột trái chọn dân (kéo để chọn nhiều) · chuột phải ra lệnh\nP đồ họa pixel · M nhạc · F5 lưu · F9 tải · Esc tạm dừng",
                15f, FontStyles.Normal, new Color(0.8f, 0.75f, 0.66f), 70f);

            pausePanel = Panel("PauseMenu");
            Label(pausePanel.transform, "TẠM DỪNG", 38f, FontStyles.Bold, TitleColor, 56f);
            MenuButton(pausePanel.transform, "Chơi tiếp", Resume);
            MenuButton(pausePanel.transform, "Lưu game", SaveFromPause);
            MenuButton(pausePanel.transform, "Cài đặt", OpenSettings);
            MenuButton(pausePanel.transform, "Về menu chính", BackToMainMenu);
            MenuButton(pausePanel.transform, "Thoát", Quit);

            settingsPanel = Panel("SettingsMenu");
            Label(settingsPanel.transform, "CÀI ĐẶT", 38f, FontStyles.Bold, TitleColor, 56f);
            Label(settingsPanel.transform, "Âm lượng nhạc", 20f, FontStyles.Normal, TextColor, 28f);
            musicSlider = VolumeSlider(settingsPanel.transform, v => MusicManager.Volume = v);
            Label(settingsPanel.transform, "Âm lượng tiếng động", 20f, FontStyles.Normal, TextColor, 28f);
            sfxSlider = VolumeSlider(settingsPanel.transform, v => SfxManager.Volume = v);
            pixelLabel = MenuButton(settingsPanel.transform, "Đồ họa pixel", TogglePixelMode).GetComponentInChildren<TMP_Text>();
            fullscreenLabel = MenuButton(settingsPanel.transform, "Toàn màn hình", ToggleFullscreen).GetComponentInChildren<TMP_Text>();
            MenuButton(settingsPanel.transform, "Quay lại", () => Show(settingsReturn));
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private GameObject Panel(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(overlay.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(460f, 0f);
            go.AddComponent<Image>().color = PanelColor;
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 22, 22);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            go.SetActive(false);
            return go;
        }

        private static TMP_Text Label(Transform parent, string text, float size, FontStyles style, Color color, float height)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            go.AddComponent<LayoutElement>().preferredHeight = height;
            return tmp;
        }

        private static Button MenuButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Button_{text}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = Color.white;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = ButtonColor * 1.35f;
            colors.pressedColor = ButtonColor * 0.75f;
            colors.selectedColor = ButtonColor;
            colors.disabledColor = new Color(0.25f, 0.22f, 0.2f, 0.7f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(onClick);
            go.AddComponent<LayoutElement>().preferredHeight = 44f;

            var labelGO = new GameObject("Text", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            Stretch((RectTransform)labelGO.transform);
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 24f;
            label.color = TextColor;
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static Slider VolumeSlider(Transform parent, UnityEngine.Events.UnityAction<float> onChange)
        {
            var go = new GameObject("VolumeSlider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 26f;

            var background = new GameObject("Background", typeof(RectTransform));
            background.transform.SetParent(go.transform, false);
            var bgRect = (RectTransform)background.transform;
            bgRect.anchorMin = new Vector2(0f, 0.3f);
            bgRect.anchorMax = new Vector2(1f, 0.7f);
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            background.AddComponent<Image>().color = new Color(0.25f, 0.18f, 0.12f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var areaRect = (RectTransform)fillArea.transform;
            areaRect.anchorMin = new Vector2(0f, 0.3f);
            areaRect.anchorMax = new Vector2(1f, 0.7f);
            areaRect.offsetMin = areaRect.offsetMax = Vector2.zero;
            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = (RectTransform)fill.transform;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = new Color(0.85f, 0.62f, 0.3f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            Stretch((RectTransform)handleArea.transform);
            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = (RectTransform)handle.transform;
            handleRect.sizeDelta = new Vector2(16f, 0f);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = TextColor;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.onValueChanged.AddListener(onChange);
            return slider;
        }
    }
}
