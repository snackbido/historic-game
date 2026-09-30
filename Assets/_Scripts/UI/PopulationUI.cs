using TMPro;
using UnityEngine;

namespace PrehistoricTribe
{
    /// <summary>Nhãn "Dân số: 5/6 (1 trẻ em)" — cập nhật vài lần mỗi giây.</summary>
    public class PopulationUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float refreshInterval = 0.5f;

        private float timer;

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f || label == null || NpcManager.Instance == null) return;
            timer = refreshInterval;
            label.text = Describe(NpcManager.Instance);
        }

        public static string Describe(NpcManager manager)
        {
            int children = 0;
            foreach (var npc in NpcController.All)
                if (!npc.IsAdult) children++;

            string text = $"Dân số: {manager.Population}/{manager.Capacity}";
            return children > 0 ? $"{text} ({children} trẻ em)" : text;
        }
    }
}
