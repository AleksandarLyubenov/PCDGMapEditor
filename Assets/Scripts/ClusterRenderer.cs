//using System.Collections.Generic;
//using UnityEngine;

//public class ClusterRenderer : MonoBehaviour
//{
//    [Header("Settings")]
//    public float cellSizePixels = 80f;
//    public float noClusterMaxOrthoSize = 30f; // below this, show all individuals

//    private UnitManager unitManager;
//    private CameraController2D camController;

//    // internal structure
//    private class CellInfo
//    {
//        public List<UnitData> members = new();
//        public UnitData representative;
//        public Vector2 worldPos;
//    }

//    public void Setup(UnitManager manager, CameraController2D cam)
//    {
//        unitManager = manager;
//        camController = cam;
//        if (camController != null)
//            camController.OnViewChanged += RebuildClusters;
//    }

//    private void OnDestroy()
//    {
//        if (camController != null)
//            camController.OnViewChanged -= RebuildClusters;
//    }

//    public void RebuildClusters()
//    {
//        if (unitManager == null || camController == null) return;

//        var units = unitManager.GetAllUnits();
//        var cam = camController.Cam;

//        // If very zoomed in, no clustering, just bind each view directly
//        if (cam.orthographicSize <= noClusterMaxOrthoSize)
//        {
//            foreach (var u in units)
//            {
//                var view = unitManager.GetOrCreateView(u);
//                view.Bind(u, asCluster: false, clusterSize: 1);
//            }
//            return;
//        }

//        // Build cell map
//        Dictionary<Vector2Int, CellInfo> cells = new();

//        foreach (var u in units)
//        {
//            Vector3 screenPos = cam.WorldToScreenPoint(u.worldPos);
//            if (screenPos.z < 0) continue; // behind camera

//            int cellX = Mathf.FloorToInt(screenPos.x / cellSizePixels);
//            int cellY = Mathf.FloorToInt(screenPos.y / cellSizePixels);
//            var key = new Vector2Int(cellX, cellY);

//            if (!cells.TryGetValue(key, out var cell))
//            {
//                cell = new CellInfo();
//                cells[key] = cell;
//            }

//            cell.members.Add(u);
//        }

//        // Decide representatives
//        foreach (var kv in cells)
//        {
//            var cell = kv.Value;
//            if (cell.members.Count == 0) continue;

//            byte bestImp = 0;
//            UnitData rep = null;
//            Vector2 sum = Vector2.zero;

//            foreach (var u in cell.members)
//            {
//                sum += u.worldPos;
//                if (rep == null || u.importance > bestImp)
//                {
//                    bestImp = u.importance;
//                    rep = u;
//                }
//            }

//            cell.representative = rep;
//            cell.worldPos = sum / cell.members.Count;
//        }

//        // For now, we just show the representative as a cluster, others are hidden visually.
//        // Under the hood they still exist in data for later zoom.

//        HashSet<string> usedIds = new();

//        foreach (var kv in cells)
//        {
//            var cell = kv.Value;
//            var rep = cell.representative;
//            if (rep == null) continue;

//            usedIds.Add(rep.id);

//            var view = unitManager.GetOrCreateView(rep);

//            // Temporarily override position to cluster center for display
//            Vector2 originalPos = rep.worldPos;
//            Vector2 displayPos = cell.worldPos;

//            var temp = new UnitData
//            {
//                id = rep.id,
//                frameType = rep.frameType,
//                unitCode = rep.unitCode,
//                nationTop = rep.nationTop,
//                labelBottom = rep.labelBottom,
//                importance = rep.importance,
//                worldPos = displayPos
//            };

//            view.Bind(temp, asCluster: cell.members.Count > 1, clusterSize: cell.members.Count);
//        }

//        // Optionally hide views for units that are not representatives
//        // easiest way: just don't create them / don't update them. they won't exist.
//    }
//}
