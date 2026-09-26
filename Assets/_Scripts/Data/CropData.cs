using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    [CreateAssetMenu(fileName = "NewCropData", menuName = "PrehistoricTribe/Crop Data")]
    public class CropData : ScriptableObject
    {
        public string id;
        public string displayName;

        [Tooltip("Mở khóa sẵn ngay từ đầu game, không cần tech")]
        public bool unlockedByDefault;

        [Tooltip("Thời gian (giây) từ khi gieo hạt đến khi nảy mầm")]
        public float timeToSprout = 10f;

        [Tooltip("Thời gian (giây) từ khi nảy mầm đến khi trưởng thành/sẵn sàng thu hoạch")]
        public float timeToMature = 20f;

        [Tooltip("Thời gian (giây) sau khi trưởng thành mà không thu hoạch thì héo. 0 = không bao giờ héo")]
        public float witherTime = 30f;

        public List<ResourceAmount> harvestYield = new List<ResourceAmount>();

        public Sprite seedSprite;
        public Sprite sproutSprite;
        public Sprite matureSprite;
        public Sprite witheredSprite;
    }
}
