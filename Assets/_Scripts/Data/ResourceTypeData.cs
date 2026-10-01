using UnityEngine;

namespace PrehistoricTribe
{
    public enum ResourceCategory
    {
        Material,
        Food,
        Knowledge
    }

    [CreateAssetMenu(fileName = "NewResourceType", menuName = "PrehistoricTribe/Resource Type")]
    public class ResourceTypeData : ScriptableObject
    {
        public string id;
        public string displayName;
        public ResourceCategory category;

        [Header("Lương thực")]
        [Tooltip("Loại 'gộp': số lượng = tổng mọi loại lương thực; dùng trong chi phí để trả bằng loại nào cũng được (vd 5 Thức ăn)")]
        public bool isFoodPool;
        [Tooltip("Loại gộp: khi CỘNG vào loại gộp thì thực ra cộng vào loại này")]
        public ResourceTypeData poolDefault;
        [Tooltip("Dễ hỏng (thịt, cá, sữa): được dùng trước khi trả chi phí lương thực; hỏng dần nếu để ngoài kho (E4)")]
        public bool perishable;

        public bool IsFood => category == ResourceCategory.Food && !isFoodPool;
    }
}
