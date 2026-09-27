using UnityEngine;
using UnityEngine.EventSystems;

namespace PrehistoricTribe
{
    public class CommandSystem : MonoBehaviour
    {
        public static CommandSystem Instance { get; private set; }

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
            if (!Input.GetMouseButtonDown(1)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (SelectionManager.Instance == null || SelectionManager.Instance.Selected.Count == 0) return;

            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;

            Collider2D hit = Physics2D.OverlapPoint(world);
            ResourceNode targetNode = hit != null ? hit.GetComponent<ResourceNode>() : null;

            int count = SelectionManager.Instance.Selected.Count;
            foreach (var villager in SelectionManager.Instance.Selected)
            {
                if (targetNode != null)
                    villager.AssignGathering(targetNode);
                else
                    villager.AssignGuarding(world);
            }

            EventBus.RaiseNotification(targetNode != null
                ? $"Đã giao {count} dân làng thu thập {targetNode.ResourceType.displayName}"
                : $"Đã giao {count} dân làng canh gác vị trí mới");
        }
    }
}
