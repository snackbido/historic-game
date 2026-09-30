using System.Collections.Generic;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>Tra PredatorData theo id và lưu/tải toàn bộ thú dữ còn sống.</summary>
    public class PredatorManager : MonoBehaviour
    {
        public static PredatorManager Instance { get; private set; }

        [SerializeField] private List<PredatorData> knownPredators = new List<PredatorData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public PredatorData FindPredator(string id) =>
            string.IsNullOrEmpty(id) ? null : knownPredators.Find(p => p != null && p.id == id);

        public List<PredatorSaveData> GetSaveData()
        {
            var data = new List<PredatorSaveData>();
            foreach (var predator in PredatorAI.All) data.Add(predator.GetSaveData());
            return data;
        }

        public void LoadFromSaveData(List<PredatorSaveData> saved)
        {
            foreach (var predator in new List<PredatorAI>(PredatorAI.All))
            {
                predator.gameObject.SetActive(false);
                Destroy(predator.gameObject);
            }

            if (saved == null) return;
            foreach (var entry in saved)
            {
                PredatorData data = FindPredator(entry.predatorId);
                if (data == null || data.prefab == null) continue;

                GameObject go = Instantiate(data.prefab, new Vector3(entry.x, 0f, entry.z), Quaternion.identity);
                if (!string.IsNullOrEmpty(entry.objectName)) go.name = entry.objectName;
                go.GetComponent<PredatorAI>()?.LoadFromSaveData(entry, data);
            }
        }
    }
}
