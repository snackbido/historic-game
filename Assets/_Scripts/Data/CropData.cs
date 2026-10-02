using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>Loại ruộng (Milestone 5d): ruộng cạn cho rau/quả, ruộng nước cấy lúa, ruộng mạ ươm mạ.</summary>
    public enum FieldType
    {
        Dry,
        Paddy,
        Seedbed
    }

    [CreateAssetMenu(fileName = "NewCropData", menuName = "PrehistoricTribe/Crop Data")]
    public class CropData : ScriptableObject
    {
        public string id;
        public string displayName;

        [Tooltip("Mở khóa sẵn ngay từ đầu game, không cần tech")]
        public bool unlockedByDefault;

        [Tooltip("Icon trên thanh công cụ (sinh tự động từ model cây chín)")]
        public Sprite icon;

        [Tooltip("Trồng trên loại ruộng nào (lúa = ruộng nước)")]
        public FieldType fieldType = FieldType.Dry;

        [Tooltip("Mức nước tối thiểu (0..1) của ruộng để cây còn lớn; thấp hơn là khô hạn (lúa cần ruộng còn ngập)")]
        [Range(0f, 1f)] public float minWater = 0.01f;

        [Tooltip("Giống tốn khi gieo/cấy (vd mạ: 1 thóc giống; lúa: 1 bó mạ). Trống = không tốn")]
        public List<ResourceAmount> plantCost = new List<ResourceAmount>();

        [Tooltip("Động từ khi thu hoạch hiện trên gợi ý, vd \"Thu hoạch\", \"Nhổ\"")]
        public string harvestVerb = "Thu hoạch";

        [Tooltip("Thời gian (giây) từ khi gieo hạt đến khi nảy mầm")]
        public float timeToSprout = 10f;

        [Tooltip("Thời gian (giây) từ khi nảy mầm đến khi trưởng thành/sẵn sàng thu hoạch")]
        public float timeToMature = 20f;

        [Tooltip("Thời gian (giây) sau khi trưởng thành mà không thu hoạch thì héo. 0 = không bao giờ héo")]
        public float witherTime = 30f;

        public List<ResourceAmount> harvestYield = new List<ResourceAmount>();

        [Header("Model hiển thị theo giai đoạn (prefab, sinh ra trên ô đất)")]
        public GameObject seedModel;
        public GameObject sproutModel;
        public GameObject matureModel;
        public GameObject witheredModel;
    }
}
