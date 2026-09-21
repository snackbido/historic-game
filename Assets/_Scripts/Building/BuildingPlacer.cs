using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField] private Grid grid;
        [SerializeField] private BuildingData buildingToPlace;
        [SerializeField] private GameObject placementPreview;

        private readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
        private readonly List<BuildingInstance> placedBuildings = new List<BuildingInstance>();

        private void Update()
        {
            if (buildingToPlace == null || grid == null) return;

            Vector3Int cell = GetCellUnderMouse();
            UpdatePreview(cell);

            if (Input.GetMouseButtonDown(0))
                TryPlace(cell);
        }

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

            PlaceBuilding(buildingToPlace, cell, spendResources: true);
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
                if (buildingToPlace == null || buildingToPlace.id != entry.buildingId) continue;

                Vector3Int cell = new Vector3Int(entry.cellX, entry.cellY, entry.cellZ);
                PlaceBuilding(buildingToPlace, cell, spendResources: false);
            }
        }
    }
}
