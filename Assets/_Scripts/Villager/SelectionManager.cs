using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PrehistoricTribe
{
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager Instance { get; private set; }

        [Tooltip("Kéo chuột dưới ngưỡng này (world units) được tính là click chọn 1 đơn vị, không phải kéo vùng")]
        [SerializeField] private float clickDragThreshold = 0.15f;

        private readonly List<VillagerController> selected = new List<VillagerController>();

        private Vector2 dragStart;
        private bool isDragging;

        public IReadOnlyList<VillagerController> Selected => selected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (Input.GetMouseButtonDown(0))
            {
                dragStart = GetMouseWorldPosition();
                isDragging = true;
            }

            if (isDragging && Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                SelectInRect(dragStart, GetMouseWorldPosition());
            }
        }

        private static Vector2 GetMouseWorldPosition()
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            return new Vector2(world.x, world.y);
        }

        private void SelectInRect(Vector2 a, Vector2 b)
        {
            var allVillagers = Object.FindObjectsByType<VillagerController>(FindObjectsInactive.Exclude);

            selected.Clear();

            if (Vector2.Distance(a, b) < clickDragThreshold)
            {
                VillagerController nearest = null;
                float nearestDistance = 0.5f;
                foreach (var villager in allVillagers)
                {
                    float distance = Vector2.Distance(villager.transform.position, a);
                    if (distance <= nearestDistance)
                    {
                        nearest = villager;
                        nearestDistance = distance;
                    }
                }
                if (nearest != null) selected.Add(nearest);
            }
            else
            {
                Vector2 min = Vector2.Min(a, b);
                Vector2 max = Vector2.Max(a, b);
                foreach (var villager in allVillagers)
                {
                    Vector2 pos = villager.transform.position;
                    if (pos.x >= min.x && pos.x <= max.x && pos.y >= min.y && pos.y <= max.y)
                        selected.Add(villager);
                }
            }

            foreach (var villager in allVillagers)
                villager.SetSelected(selected.Contains(villager));

            EventBus.RaiseSelectionChanged();
        }
    }
}
