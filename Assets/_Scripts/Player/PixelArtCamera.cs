using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Phong cách pixel art cho camera 3D (thử nghiệm 2026-10-02): vẽ khung hình ở độ phân giải thấp
    /// (<see cref="pixelHeight"/> điểm ảnh chiều cao), viền tối mép vật thể, ép màu về bảng màu cố định rồi phóng to
    /// không làm mịn. Giao diện (Canvas overlay) không bị ảnh hưởng; chuột/chọn đối tượng vẫn tính trên màn hình thật.
    /// Phím <see cref="toggleKey"/> bật/tắt để so sánh.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public class PixelArtCamera : MonoBehaviour
    {
        [SerializeField] private Material material;
        [Tooltip("Chiều cao khung hình pixel (điểm ảnh) — nhỏ hơn = điểm ảnh to hơn")]
        [SerializeField, Range(120, 540)] private int pixelHeight = 240;
        [SerializeField, Range(0f, 1f)] private float outlineStrength = 0.55f;
        [SerializeField] private float outlineThreshold = 0.06f;
        [SerializeField, Range(0f, 1f)] private float paletteStrength = 1f;
        [SerializeField, Range(0.5f, 2f)] private float colorBoost = 1.15f;
        [Tooltip("Rải điểm khi ép bảng màu (0 = vệt màu cứng, ~0.1 = chuyển màu mượt kiểu pixel art)")]
        [SerializeField, Range(0f, 0.3f)] private float dither = 0.09f;
        [SerializeField] private Color[] palette = new Color[0];
        [SerializeField] private KeyCode toggleKey = KeyCode.P;

        private static readonly int LowTexelId = Shader.PropertyToID("_LowTexel");
        private static readonly int OutlineStrengthId = Shader.PropertyToID("_OutlineStrength");
        private static readonly int OutlineThresholdId = Shader.PropertyToID("_OutlineThreshold");
        private static readonly int PaletteStrengthId = Shader.PropertyToID("_PaletteStrength");
        private static readonly int ColorBoostId = Shader.PropertyToID("_ColorBoost");
        private static readonly int DitherId = Shader.PropertyToID("_Dither");
        private static readonly int PaletteSizeId = Shader.PropertyToID("_PaletteSize");
        private static readonly int PaletteId = Shader.PropertyToID("_Palette");
        private const int MaxPalette = 48;

        public bool PixelModeOn { get; set; } = true;
        public int PixelHeight => pixelHeight;

        private void OnEnable() => GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

        private void Update()
        {
            if (Application.isPlaying && Input.GetKeyDown(toggleKey))
            {
                PixelModeOn = !PixelModeOn;
                EventBus.RaiseNotification(PixelModeOn ? "Đồ họa: pixel" : "Đồ họa: 3D thường");
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!PixelModeOn || material == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            int height = Mathf.Max(16, pixelHeight);
            int width = Mathf.Max(16, Mathf.RoundToInt(height * (float)source.width / source.height));
            material.SetVector(LowTexelId, new Vector4(1f / width, 1f / height, width, height));
            material.SetFloat(OutlineStrengthId, outlineStrength);
            material.SetFloat(OutlineThresholdId, outlineThreshold);
            material.SetFloat(PaletteStrengthId, palette.Length > 0 ? paletteStrength : 0f);
            material.SetFloat(ColorBoostId, colorBoost);
            material.SetFloat(DitherId, dither);
            int count = Mathf.Min(palette.Length, MaxPalette);
            if (count > 0)
            {
                var colors = new Vector4[MaxPalette];
                bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear; // khung hình ở hệ màu tuyến tính
                for (int i = 0; i < count; i++) colors[i] = linear ? palette[i].linear : palette[i];
                material.SetVectorArray(PaletteId, colors);
            }
            material.SetInt(PaletteSizeId, count);

            // Vẽ khung hình nhỏ (mỗi ô lấy màu ở tâm, có viền + ép màu) rồi phóng to bằng lọc Point → điểm ảnh to, sắc.
            RenderTexture low = RenderTexture.GetTemporary(width, height, 0, source.format);
            low.filterMode = FilterMode.Point;
            Graphics.Blit(source, low, material);
            Graphics.Blit(low, destination);
            RenderTexture.ReleaseTemporary(low);
        }
    }
}
