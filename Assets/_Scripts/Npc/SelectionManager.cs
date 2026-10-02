using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PrehistoricTribe
{
    /// <summary>
    /// Chọn và ra lệnh cho dân làng kiểu RTS (quyết định M5, 2026-09-30):
    /// chuột trái kéo khung / click để chọn (Shift = chọn thêm), chuột phải lên mặt đất = đi tới đó,
    /// Ctrl+1..9 lưu nhóm, 1..9 gọi lại nhóm. Chỉ người lớn mới chọn được.
    /// Nhường chuột cho BuildingPlacer khi đang đặt công trình.
    /// </summary>
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager Instance { get; private set; }

        private static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

        [Tooltip("Kéo quá số pixel này mới tính là kéo khung (nhỏ hơn là click)")]
        [SerializeField] private float dragThreshold = 8f;
        [Tooltip("Click cách NPC trong bán kính này (pixel) thì chọn NPC đó")]
        [SerializeField] private float clickPickRadius = 40f;
        [Tooltip("Khoảng cách giữa các NPC khi cả nhóm đi tới một điểm")]
        [SerializeField] private float formationSpacing = 0.9f;
        [Tooltip("Chuột phải cách một đối tượng (cây, ô đất, thú) trong bán kính này (mét) thì giao việc tại đối tượng đó")]
        [SerializeField] private float targetPickRadius = 0.8f;
        [SerializeField] private Color boxFill = new Color(0.95f, 0.8f, 0.4f, 0.15f);
        [SerializeField] private Color boxBorder = new Color(0.95f, 0.8f, 0.4f, 0.9f);

        private readonly List<NpcController> selected = new List<NpcController>();
        private BuildingInstance selectedBuilding;
        private bool hadBuilding;
        private readonly Dictionary<int, List<NpcController>> groups = new Dictionary<int, List<NpcController>>();

        private bool pressStartedOnWorld;
        private bool dragging;
        private Vector2 dragStart;

        public IReadOnlyList<NpcController> Selected => selected;
        /// <summary>Công trình đang được chọn (click vào lều/kho) — chọn NPC thì bỏ chọn công trình và ngược lại.</summary>
        public BuildingInstance SelectedBuilding => selectedBuilding;

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
            if (GameMenuUI.IsOpen) return; // đang mở menu: không nhận lệnh trong game
            PruneSelection();

            BuildingPlacer placer = BuildingPlacer.Instance;
            if (placer != null && (placer.IsPlacing || placer.UsedMouseThisFrame))
            {
                pressStartedOnWorld = false;
                dragging = false;
                return;
            }

            HandleGroupHotkeys();
            HandleSelectionMouse();
            HandleCommandMouse();
        }

        // ─── Input ───────────────────────────────────────────────────────────
        private void HandleSelectionMouse()
        {
            Vector2 mouse = Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                pressStartedOnWorld = !IsPointerOverUI();
                dragStart = mouse;
                dragging = false;
            }
            if (!pressStartedOnWorld) return;

            if (Input.GetMouseButton(0) && !dragging && (mouse - dragStart).magnitude > dragThreshold)
                dragging = true;

            if (Input.GetMouseButtonUp(0))
            {
                bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (dragging) SelectInScreenRect(ScreenRect(dragStart, mouse), additive);
                else SelectAt(mouse, additive);

                dragging = false;
                pressStartedOnWorld = false;
            }
        }

        private void HandleCommandMouse()
        {
            if (selected.Count == 0 || !Input.GetMouseButtonDown(1) || IsPointerOverUI()) return;
            if (TryGetGroundPoint(Input.mousePosition, out Vector3 point))
                IssueCommandAt(point);
        }

        private void HandleGroupHotkeys()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            for (int number = 1; number <= 9; number++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha0 + number)) continue;
                if (ctrl) SaveGroup(number);
                else RecallGroup(number);
            }
        }

        private static bool IsPointerOverUI() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // ─── Public API (dùng cho input và test) ─────────────────────────────
        public void SelectInScreenRect(Rect screenRect, bool additive)
        {
            if (!additive) ClearInternal();
            foreach (var npc in NpcController.All)
            {
                if (npc.IsAdult && TryGetScreenPoint(npc, out Vector2 point) && screenRect.Contains(point))
                    AddInternal(npc);
            }
            NotifyChanged();
        }

        public void SelectAt(Vector2 screenPosition, bool additive)
        {
            NpcController npc = FindNpcAt(screenPosition);

            // Không trúng người nào → thử công trình dưới con trỏ (lều, kho…).
            if (npc == null && !additive)
            {
                BuildingInstance building = FindBuildingAt(screenPosition);
                if (building != null)
                {
                    SelectBuilding(building);
                    return;
                }
            }

            if (!additive) ClearInternal();

            if (npc != null)
            {
                if (additive && npc.IsSelected) RemoveInternal(npc);
                else AddInternal(npc);
            }
            NotifyChanged();
            if (npc == null && !additive) SelectBuilding(null); // click chỗ trống → bỏ chọn cả công trình
        }

        public void ClearSelection()
        {
            ClearInternal();
            NotifyChanged();
            SelectBuilding(null);
        }

        /// <summary>Chọn một công trình (null = bỏ chọn). Bỏ chọn mọi NPC đang chọn.</summary>
        public void SelectBuilding(BuildingInstance building)
        {
            if (building != null && selected.Count > 0)
            {
                ClearInternal();
                NotifyChanged();
            }
            if (building == selectedBuilding && hadBuilding == (building != null)) return;

            if (selectedBuilding != null) selectedBuilding.SetSelected(false);
            selectedBuilding = building;
            hadBuilding = building != null;
            if (building != null) building.SetSelected(true);
            EventBus.RaiseBuildingSelected(building);
        }

        private static BuildingInstance FindBuildingAt(Vector2 screenPosition)
        {
            Camera cam = Camera.main;
            if (cam == null) return null;
            // Công trình có BoxCollider (dùng để chặn người chơi) → raycast vật lý trúng thẳng vào nó.
            if (!Physics.Raycast(cam.ScreenPointToRay(screenPosition), out RaycastHit hit, 200f)) return null;
            return hit.collider.GetComponentInParent<BuildingInstance>();
        }

        /// <summary>Thay toàn bộ lựa chọn (vd lọc theo nghề trên UI).</summary>
        public void SetSelection(IEnumerable<NpcController> npcs)
        {
            ClearInternal();
            foreach (var npc in npcs)
                if (npc != null && npc.IsAdult) AddInternal(npc);
            NotifyChanged();
        }

        /// <summary>
        /// Lệnh chuột phải: trúng cây/ô đất/thú thì mỗi người nhận việc hợp với nghề, ai không làm được
        /// thì đi theo tới đó; trúng chỗ trống thì cả nhóm đi tới.
        /// </summary>
        public void IssueCommandAt(Vector3 groundPoint)
        {
            if (selected.Count == 0) return;

            PredatorAI predator = FindPredatorNear(groundPoint);
            if (predator != null)
            {
                IssueAttackCommand(predator);
                return;
            }

            MonoBehaviour target = InteractableRegistry.FindNearest(groundPoint, targetPickRadius);
            if (target == null)
            {
                IssueMoveCommand(groundPoint);
                return;
            }

            var working = new Dictionary<string, int>();
            var followers = new List<NpcController>();
            foreach (var npc in selected)
            {
                NpcJob job = NpcJobFactory.Create(npc, target);
                if (job == null)
                {
                    followers.Add(npc);
                    continue;
                }
                npc.AssignJob(job);
                working.TryGetValue(job.Description, out int count);
                working[job.Description] = count + 1;
            }

            // Người không làm được việc này thì đi theo, đứng quanh đối tượng.
            List<Vector3> offsets = FormationOffsets(followers.Count, formationSpacing);
            Vector3 followPoint = target.transform.position + Vector3.back * 1.2f;
            for (int i = 0; i < followers.Count; i++)
                followers[i].MoveTo(followPoint + offsets[i]);

            var parts = new List<string>();
            foreach (var entry in working) parts.Add($"{entry.Value} người đi {entry.Key}");
            if (followers.Count > 0) parts.Add($"{followers.Count} người đi theo");
            EventBus.RaiseNotification(string.Join(" · ", parts));
        }

        /// <summary>Cả nhóm đi tới điểm đích, dàn thành lưới quanh điểm đó để không đứng chồng lên nhau.</summary>
        public void IssueMoveCommand(Vector3 target)
        {
            List<Vector3> offsets = FormationOffsets(selected.Count, formationSpacing);
            for (int i = 0; i < selected.Count; i++)
                selected[i].MoveTo(target + offsets[i]);
        }

        /// <summary>
        /// Chuột phải lên thú dữ: người biết đánh xông vào; người không biết đánh ĐỨNG YÊN
        /// (ngoại lệ của quy tắc "đi theo" — không đưa người không biết đánh vào chỗ nguy hiểm).
        /// </summary>
        public void IssueAttackCommand(PredatorAI predator)
        {
            int fighters = 0;
            int stayed = 0;
            foreach (var npc in selected)
            {
                NpcJob job = NpcJobFactory.Create(npc, predator);
                if (job == null)
                {
                    stayed++;
                    continue;
                }
                npc.AssignJob(job);
                fighters++;
            }

            string message = fighters > 0 ? $"{fighters} người xông vào đánh {predator.Data.displayName}" : "Không ai trong nhóm biết chiến đấu";
            if (stayed > 0) message += $" · {stayed} người không biết đánh, đứng lại";
            EventBus.RaiseNotification(message);
        }

        private PredatorAI FindPredatorNear(Vector3 groundPoint)
        {
            PredatorAI best = null;
            float bestDistance = targetPickRadius * 1.5f; // thú chạy nhảy → nới vùng click
            foreach (var predator in PredatorAI.All)
            {
                float distance = InteractableRegistry.GroundDistance(groundPoint, predator.transform.position);
                if (distance > bestDistance) continue;
                best = predator;
                bestDistance = distance;
            }
            return best;
        }

        public void SaveGroup(int number)
        {
            if (selected.Count == 0) return;
            groups[number] = new List<NpcController>(selected);
            EventBus.RaiseNotification($"Đã lưu nhóm {number} ({selected.Count} người)");
        }

        public void RecallGroup(int number)
        {
            if (!groups.TryGetValue(number, out var group)) return;
            group.RemoveAll(n => n == null || !n.isActiveAndEnabled);

            ClearInternal();
            foreach (var npc in group) AddInternal(npc);
            NotifyChanged();
        }

        // ─── Helpers ─────────────────────────────────────────────────────────
        private NpcController FindNpcAt(Vector2 screenPosition)
        {
            NpcController best = null;
            float bestDistance = clickPickRadius;
            foreach (var npc in NpcController.All)
            {
                if (!npc.IsAdult || !TryGetScreenPoint(npc, out Vector2 point)) continue;
                float distance = Vector2.Distance(point, screenPosition);
                if (distance < bestDistance)
                {
                    best = npc;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static bool TryGetScreenPoint(NpcController npc, out Vector2 point)
        {
            point = default;
            Camera cam = Camera.main;
            if (cam == null || npc.IsInsideHut) return false; // đang ngủ trong lều: không thấy, không chọn được

            // Lấy điểm giữa thân (không phải bàn chân) cho dễ trúng khi click.
            Vector3 screen = cam.WorldToScreenPoint(npc.transform.position + Vector3.up * 0.5f);
            if (screen.z <= 0f) return false;
            point = screen;
            return true;
        }

        private static bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 point)
        {
            point = default;
            Camera cam = Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(screenPosition);
            if (!GroundPlane.Raycast(ray, out float enter)) return false;
            point = ray.GetPoint(enter);
            return true;
        }

        public static List<Vector3> FormationOffsets(int count, float spacing)
        {
            var offsets = new List<Vector3>(count);
            if (count <= 0) return offsets;

            int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt(count / (float)columns);
            for (int i = 0; i < count; i++)
            {
                float x = (i % columns - (columns - 1) * 0.5f) * spacing;
                float z = (i / columns - (rows - 1) * 0.5f) * spacing;
                offsets.Add(new Vector3(x, 0f, z));
            }
            return offsets;
        }

        private static Rect ScreenRect(Vector2 a, Vector2 b) =>
            Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));

        private void AddInternal(NpcController npc)
        {
            if (npc == null || selected.Contains(npc)) return;
            selected.Add(npc);
            npc.SetSelected(true);
        }

        private void RemoveInternal(NpcController npc)
        {
            selected.Remove(npc);
            npc.SetSelected(false);
        }

        private void ClearInternal()
        {
            foreach (var npc in selected)
                if (npc != null) npc.SetSelected(false);
            selected.Clear();
        }

        private void PruneSelection()
        {
            if (selected.RemoveAll(n => n == null || !n.isActiveAndEnabled) > 0)
                NotifyChanged();

            // Công trình đang chọn bị xóa (vd tải game) → bỏ chọn.
            if (hadBuilding && (selectedBuilding == null || !selectedBuilding.isActiveAndEnabled))
            {
                selectedBuilding = null;
                hadBuilding = false;
                EventBus.RaiseBuildingSelected(null);
            }
        }

        private void NotifyChanged()
        {
            EventBus.RaiseSelectionChanged(selected);
            if (selected.Count > 0 && hadBuilding) SelectBuilding(null); // chọn người thì bỏ chọn công trình
        }

        // ─── Khung kéo chọn ──────────────────────────────────────────────────
        private void OnGUI()
        {
            if (!dragging) return;

            Rect rect = ScreenRect(dragStart, Input.mousePosition);
            // GUI tính y từ trên xuống, Input tính từ dưới lên.
            var guiRect = new Rect(rect.xMin, Screen.height - rect.yMax, rect.width, rect.height);
            const float border = 2f;

            DrawRect(guiRect, boxFill);
            DrawRect(new Rect(guiRect.xMin, guiRect.yMin, guiRect.width, border), boxBorder);
            DrawRect(new Rect(guiRect.xMin, guiRect.yMax - border, guiRect.width, border), boxBorder);
            DrawRect(new Rect(guiRect.xMin, guiRect.yMin, border, guiRect.height), boxBorder);
            DrawRect(new Rect(guiRect.xMax - border, guiRect.yMin, border, guiRect.height), boxBorder);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
