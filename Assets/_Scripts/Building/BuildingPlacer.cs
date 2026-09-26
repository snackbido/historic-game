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
    }

    public class BuildingPlacer : MonoBehaviour
    {
        public static BuildingPlacer Instance { get; private set; }

        [SerializeField] private Grid grid;
        [SerializeField] private List<BuildingData> availableBuildings = new List<BuildingData>();
        [SerializeField] private GameObject placementPreview;

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
        }

        public void SelectBuilding(BuildingData data)
        {
            selectedBuilding = data;
            selectionFrame = Time.frameCount;
        }

        public void CancelSelection()
        {
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

            if (placementPreview != null) placementPreview.SetActive(true);

            Vector3Int cell = GetCellUnderMouse();
            UpdatePreview(cell);

            bool justSelected = Time.frameCount == selectionFrame;
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI() && !justSelected)
                TryPlace(cell);
        }

        private static bool IsPointerOverUI() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private Vector3Int GetCellUnderMouse()
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPoint.z = 0f;
            return grid.WorldToCell(worldPoint);
        }

        private void UpdatePreview(Vector3Int cell)
        {
            if (placementPreview == null) return;
            placementPreview.transform.position = grid.GetCellCenterWorld(cell);
        }

        private void TryPlace(Vector3Int cell)
        {
            if (occupiedCells.Contains(cell)) return;
            if (TechManager.Instance != null && !TechManager.Instance.IsBuildingUnlocked(selectedBuilding)) return;

            PlaceBuilding(selectedBuilding, cell, spendResources: true);
        }

        private BuildingInstance PlaceBuilding(BuildingData data, Vector3Int cell, bool spendResources)
        {
            if (spendResources && !ResourceManager.Instance.SpendAll(data.costs))
                return null;

            Vector3 worldPosition = grid.GetCellCenterWorld(cell);
            GameObject instanceObject = Instantiate(data.prefab, worldPosition, Quaternion.identity);
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
                    cellZ = building.GridPosition.z
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
                PlaceBuilding(buildingData, cell, spendResources: false);
            }
        }
    }
}
