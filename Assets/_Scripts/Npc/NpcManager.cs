using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>
    /// Quản lý dân làng: tra nghề theo id, lưu/tải toàn bộ NPC.
    /// Sau này (M5.6) nơi đặt logic dân số: sức chứa theo lều, cặp đôi sinh con.
    /// </summary>
    public class NpcManager : MonoBehaviour
    {
        public static NpcManager Instance { get; private set; }

        [SerializeField] private NpcController npcPrefab;
        [Tooltip("Mọi nghề trong game — dùng để tra ProfessionData theo id khi tải game")]
        [SerializeField] private List<ProfessionData> knownProfessions = new List<ProfessionData>();

        public IReadOnlyList<ProfessionData> KnownProfessions => knownProfessions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public ProfessionData FindProfession(string id) =>
            string.IsNullOrEmpty(id) ? null : knownProfessions.Find(p => p != null && p.id == id);

        public List<NpcSaveData> GetSaveData()
        {
            var data = new List<NpcSaveData>();
            foreach (var npc in NpcController.All) data.Add(npc.GetSaveData());
            return data;
        }

        public void LoadFromSaveData(List<NpcSaveData> saved)
        {
            // Dân số thay đổi trong lúc chơi (sinh con, chết) → xóa hết rồi tạo lại theo danh sách đã lưu.
            foreach (var npc in new List<NpcController>(NpcController.All))
            {
                npc.gameObject.SetActive(false); // gỡ khỏi danh sách ngay, Destroy chỉ chạy cuối frame
                Destroy(npc.gameObject);
            }

            if (saved == null || npcPrefab == null) return;
            foreach (var entry in saved)
            {
                var position = new Vector3(entry.x, 0f, entry.z);
                var rotation = Quaternion.Euler(0f, entry.rotationY, 0f);
                NpcController npc = Instantiate(npcPrefab, position, rotation);
                npc.LoadFromSaveData(entry, FindProfession(entry.professionId));
            }
        }
    }
}
