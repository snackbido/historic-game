using UnityEngine;

namespace PrehistoricTribe.EditorTools
{
    /// <summary>
    /// Bảng màu chung cho địa hình (LandscapeBuilder) và model thiên nhiên nhập từ gói ngoài (NatureAssetBuilder).
    /// Một nguồn màu duy nhất để cả hai luôn "đồng bộ" — đổi màu ở đây là đổi cho cả mặt đất và cây/đá/cỏ.
    /// </summary>
    public static class LandscapePalette
    {
        public static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f);

        public static readonly Color GrassDark = Hex(0x3f6b2a), GrassMid = Hex(0x4f8a33), GrassLight = Hex(0x5f9a3a), GrassYellow = Hex(0x7cb446);
        public static readonly Color ForestFloor = Hex(0x355c25), ForestFloorLight = Hex(0x41692b);
        public static readonly Color Dirt = Hex(0x806440), DirtDark = Hex(0x6e5434), DryDirt = Hex(0x93764c);
        public static readonly Color Sand = Hex(0xc9b27c), SandLight = Hex(0xd8c48f), WetSand = Hex(0xa8935f), Mud = Hex(0x5c4a32);
        public static readonly Color RockDark = Hex(0x5e5a55), Rock = Hex(0x7d7a74), RockLight = Hex(0xa39d90), MossStone = Hex(0x6b7a55);
        public static readonly Color TrunkColor = Hex(0x6b4a2b), TrunkDark = Hex(0x4e3520), BirchBark = Hex(0xd8d2c0);
        public static readonly Color PineDark = Hex(0x2f5a2c), Pine = Hex(0x3b6e33), PineLight = Hex(0x4f8a3a);
        public static readonly Color LeafDark = Hex(0x3f7a2e), Leaf = Hex(0x56963a), LeafLight = Hex(0x74b048), LeafAutumn = Hex(0xa8a03a);
        public static readonly Color Reed = Hex(0x8a9a48), ReedTop = Hex(0x7a5a32);
        public static readonly Color[] FlowerColors = { Hex(0xf2e27a), Hex(0xf4f0e0), Hex(0xe9a0b0), Hex(0xb08ad8) };
    }
}
