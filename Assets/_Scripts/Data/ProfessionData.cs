using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>Những việc một nghề làm được khi nhận lệnh hoặc tự làm lúc rảnh.</summary>
    [System.Flags]
    public enum NpcCapability
    {
        None = 0,
        Gather = 1 << 0,       // chặt cây, nhặt tài nguyên
        Build = 1 << 1,        // xây công trình
        Farm = 1 << 2,         // gieo/thu hoạch ô đất
        TendAnimals = 1 << 3,  // cho vật nuôi ăn, thu sản phẩm
        Hunt = 1 << 4,         // săn thú hoang
        Fight = 1 << 5,        // đánh kẻ địch
        Scout = 1 << 6         // dò đường, phát hiện nguy hiểm từ xa
    }

    [CreateAssetMenu(fileName = "NewProfession", menuName = "PrehistoricTribe/Profession Data")]
    public class ProfessionData : ScriptableObject
    {
        public string id;
        public string displayName;

        [Tooltip("Màu áo — nhận ra nghề bằng mắt trên bản đồ")]
        public Color tunicColor = Color.white;

        public float moveSpeed = 3f;
        public float maxHealth = 100f;
        public float attackDamage = 5f;
        public float attackRange = 1.2f;

        [Tooltip("Tầm nhìn (mét) — trinh sát phát hiện nguy hiểm từ xa hơn")]
        public float visionRange = 8f;

        public NpcCapability capabilities;

        public bool Can(NpcCapability capability) => (capabilities & capability) == capability;
    }
}
