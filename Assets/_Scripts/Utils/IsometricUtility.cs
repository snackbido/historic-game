using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Hệ số chiếu isometric (tỉ lệ 2:1 chuẩn) dùng để chuyển input di chuyển
    /// (trục ngang/dọc thông thường) sang 2 hướng chéo trên lưới isometric.
    /// Khớp với cellSize của Grid isometric dựng trong GameplaySceneBuilder.
    /// </summary>
    public static class IsometricUtility
    {
        public const float TileWidth = 1f;
        public const float TileHeight = 0.5f;

        public static readonly Vector2 AxisX = new Vector2(TileWidth, TileHeight).normalized;
        public static readonly Vector2 AxisY = new Vector2(-TileWidth, TileHeight).normalized;

        /// <summary>Chuyển vector input (Horizontal/Vertical, trục vuông góc thường) sang hướng di chuyển isometric.</summary>
        public static Vector2 InputToIsometric(Vector2 input) => input.x * AxisX + input.y * AxisY;
    }
}
