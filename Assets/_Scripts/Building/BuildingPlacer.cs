using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PrehistoricTribe
{
    [System.Serializable]
    public struct PlacedBuildingData
    {
        public string buildingId;
        public int cellX;
        public int cellY;
        public int cellZ;
        // Save cũ chưa có field này → đọc ra 0 → coi như cấp 1.
        public int level;
    }

    public class BuildingPlacer : MonoBehaviour
    {
        public static BuildingPlacer Instance { get; private set; }

        private static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);
        private static readonly Color ValidPreviewColor = new Color(0.6f, 0.85f, 0.4f);
        private static readonly Color InvalidPreviewColor = new Color(0.9f, 0.45f, 0.35f);

        [Tooltip("Grid nằm trên mặt đất: Cell Swizzle = XZY để ô (x, y) ứng với tọa độ thế giới (x, z)")]
        [SerializeField] private Grid grid;
        [SerializeField] private List<BuildingData> availableBuildings = new List<BuildingData>();
        [SerializeField] private GameObject placementPreview;

        private Renderer previewRenderer;
        private readonly Dictionary<string, BuildingData> buildingsById = new Dictionary<string, BuildingData>();
        private readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
        private readonly List<BuildingInstance> placedBuildings = new List<BuildingInstance>();

        private BuildingData selectedBuilding;
        private int selectionFrame = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (var building in availableBuildings)
                if (building != null) buildingsById[building.id] = building;

            if (placementPreview != null)
                previewRenderer = placementPreview.GetComponentInChildren<Renderer>(true);
        }

        public bool IsPlacing => selectedBuilding != null;

        /// <summary>
        /// Chuột phải vừa hủy đặt công trình trong frame này — hệ thống khác (ra lệnh NPC) bỏ qua
        /// cú click đó để không vừa hủy vừa ra lệnh.
        /// </summary>
        public bool UsedMouseThisFrame => Time.frameCount == lastCancelFrame;
        private int lastCancelFrame = -1;

        public void SelectBuilding(BuildingData data)
        {
            selectedBuilding = data;
            selectionFrame = Time.frameCount;
        }

        public void CancelSelection()
        {
            if (selectedBuilding != null) lastCancelFrame = Time.frameCount;
            selectedBuilding = null;
            if (placementPreview != null) placementPreview.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                CancelSelection();

            if (selectedBuilding == null || grid == null)
            {
                if (placementPreview != null) placementPreview.SetActive(false);
                return;
            }

            if (!TryGetCellUnderMouse(out Vector3Int cell))
            {
                if (placementPreview != null) placementPreview.SetActive(false);
                return;
            }

            UpdatePreview(cell);

            bool justSelected = Time.frameCount == selectionFrame;
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI() && !justSelected)
                TryPlace(cell);
        }

        private static bool IsPointerOverUI() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private bool TryGetCellUnderMouse(out Vector3Int cell)
        {
            cell = default;
            Camera cam = Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!GroundPlane.Raycast(ray, out float enter)) return false;

            cell = grid.WorldToCell(ray.GetPoint(enter));
            return true;
        }

        /// <summary>Tâm ô trên mặt đất (y = 0), không phụ thuộc cellSize trục đứng của Grid.</summary>
        private Vector3 CellToGround(Vector3Int cell)
        {
            Vector3 center = grid.GetCellCenterWorld(cell);
            center.y = 0f;
            return center;
        }

        private bool CanPlace(Vector3Int cell) => PlacementBlocker(selectedBuilding, cell) == null;

        /// <summary>Không xây sát mép nước hơn chừng này (m).</summary>
        private const float WaterClearance = 0.3f;

        /// <summary>Lý do không đặt được công trình tại ô này (null = đặt được).</summary>
        public string PlacementBlocker(BuildingData data, Vector3Int cell)
        {
            if (data == null) return "Chưa chọn công trình";
            if (occupiedCells.Contains(cell)) return "Ô này đã có công trình";
            if (TechManager.Instance != null && !TechManager.Instance.IsBuildingUnlocked(data)) return "Chưa mở khóa";

            Vector3 center = CellToGround(cell);
            if (WaterSource.IsOnWater(center, WaterClearance)) return "Không xây đè lên mặt nước";
            // Cần nguồn nước lớn (ao, mương) — giếng không đủ nước cho ruộng ngập.
            if (data.requiresWaterWithin > 0f && !WaterSource.AnyWithin(center, data.requiresWaterWithin, forPaddy: true))
                return $"{data.displayName} phải ở gần nguồn nước lớn (ao)";

            if (!ResourceManager.Instance.CanAfford(data.costs)) return "Chưa đủ tài nguyên";
            return null;
        }

        private void UpdatePreview(Vector3Int cell)
        {
            if (placementPreview == null) return;
            placementPreview.SetActive(true);
            placementPreview.transform.position = CellToGround(cell);
            if (previewRenderer != null)
                previewRenderer.material.color = CanPlace(cell) ? ValidPreviewColor : InvalidPreviewColor;
        }

        private void TryPlace(Vector3Int cell)
        {
            string blocker = PlacementBlocker(selectedBuilding, cell);
            if (blocker != null)
            {
                EventBus.RaiseNotification(blocker);
                return;
            }
            PlaceBuilding(selectedBuilding, cell, spendResources: true);
        }

        /// <summary>Đặt công trình tại ô (dùng cho đặt bằng chuột, tải game, test). Trả về null nếu không đủ tài nguyên.</summary>
        public BuildingInstance PlaceBuilding(BuildingData data, Vector3Int cell, bool spendResources)
        {
            if (spendResources && !ResourceManager.Instance.SpendAll(data.costs))
                return null;

            Vector3 worldPosition = CellToGround(cell);
            GameObject instanceObject = Instantiate(data.prefab, worldPosition, Quaternion.identity);
            // Tên duy nhất theo ô: lưu/tải các phần gắn theo tên (vd ô ruộng) khớp đúng công trình.
            instanceObject.name = $"{data.id}_{cell.x}_{cell.y}";
            BuildingInstance instance = instanceObject.GetComponent<BuildingInstance>();
            instance.Initialize(data, cell);

            occupiedCells.Add(cell);
            placedBuildings.Add(instance);
            EventBus.RaiseBuildingPlaced(instance);
            return instance;
        }

        public List<PlacedBuildingData> GetSaveData()
        {
            var data = new List<PlacedBuildingData>();
            foreach (var building in placedBuildings)
            {
                data.Add(new PlacedBuildingData
                {
                    buildingId = building.Data.id,
                    cellX = building.GridPosition.x,
                    cellY = building.GridPosition.y,
                    cellZ = building.GridPosition.z,
                    level = building.Level
                });
            }
            return data;
        }

        public void LoadFromSaveData(List<PlacedBuildingData> data)
        {
            foreach (var building in placedBuildings)
                if (building != null) Destroy(building.gameObject);
            placedBuildings.Clear();
            occupiedCells.Clear();

            if (data == null) return;

            foreach (var entry in data)
            {
                if (!buildingsById.TryGetValue(entry.buildingId, out var buildingData)) continue;

                Vector3Int cell = new Vector3Int(entry.cellX, entry.cellY, entry.cellZ);
                BuildingInstance placed = PlaceBuilding(buildingData, cell, spendResources: false);
                if (placed != null) placed.SetLevel(Mathf.Max(1, entry.level));
            }
        }
    }
}
