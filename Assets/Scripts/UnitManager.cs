using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;

public class UnitManager : MonoBehaviour
{
    [Header("Scene references")]
    public CameraController2D camController;
    public Transform unitContainer;
    public UnitSymbolView unitSymbolPrefab;

    [Header("UI")]
    public TMP_Dropdown dropdownFrameType; // LAND/SEA/SUB/AIR
    public TMP_InputField inputUnitCode;
    public TMP_InputField inputNationTop;
    public TMP_InputField inputLabelBottom;
    public TMP_InputField inputImportance; // 0-255, or use slider

    public string lastSavePath = "map_save.json";

    private List<UnitData> allUnits = new List<UnitData>();
    private Dictionary<string, UnitSymbolView> activeUnitViews = new();
    private UnitData pendingUnit = null;
    private bool isPlacing = false;

    private ClusterRenderer clusterRenderer;

    private void Awake()
    {
        if (camController == null)
            camController = FindFirstObjectByType<CameraController2D>();

        clusterRenderer = FindFirstObjectByType<ClusterRenderer>();
    }

    private void Start()
    {
        if (clusterRenderer != null)
            clusterRenderer.Setup(this, camController);
    }

    private void Update()
    {
        if (isPlacing && pendingUnit != null)
        {
            // Move preview to mouse
            Vector3 world = camController.Cam.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0;
            pendingUnit.worldPos = world;

            if (Input.GetMouseButtonDown(0)) // LMB to place
            {
                CommitPendingUnit();
            }
        }
    }

    // Called by "Add Unit" button
    public void OnAddUnitClicked()
    {
        pendingUnit = new UnitData();
        pendingUnit.id = System.Guid.NewGuid().ToString();
        pendingUnit.frameType = FrameTypeFromDropdown();
        pendingUnit.unitCode = inputUnitCode.text;
        pendingUnit.nationTop = inputNationTop.text;
        pendingUnit.labelBottom = inputLabelBottom.text;

        byte imp = 0;
        byte.TryParse(inputImportance.text, out imp);
        pendingUnit.importance = imp;

        isPlacing = true;
    }

    // After user clicks on map
    private void CommitPendingUnit()
    {
        allUnits.Add(pendingUnit);

        if (!activeUnitViews.ContainsKey(pendingUnit.id))
        {
            var view = Instantiate(unitSymbolPrefab, unitContainer);
            view.Bind(pendingUnit, asCluster: false, clusterSize: 1);
            activeUnitViews.Add(pendingUnit.id, view);
        }

        isPlacing = false;
        pendingUnit = null;

        if (clusterRenderer != null)
            clusterRenderer.RebuildClusters();
    }

    private string FrameTypeFromDropdown()
    {
        switch (dropdownFrameType.value)
        {
            case 0: return "LAND";
            case 1: return "SEA";
            case 2: return "SUB";
            case 3: return "AIR";
            default: return "LAND";
        }
    }

    public IReadOnlyList<UnitData> GetAllUnits() => allUnits;

    public void ClearAllViews()
    {
        foreach (var kv in activeUnitViews)
        {
            if (kv.Value != null)
                Destroy(kv.Value.gameObject);
        }
        activeUnitViews.Clear();
    }

    // Called by ClusterRenderer to rebuild visible views
    public UnitSymbolView GetOrCreateView(UnitData data)
    {
        if (activeUnitViews.TryGetValue(data.id, out var view) && view != null)
        {
            return view;
        }
        else
        {
            var newView = Instantiate(unitSymbolPrefab, unitContainer);
            activeUnitViews[data.id] = newView;
            return newView;
        }
    }

    // --- Saving / Loading ---

    public void SaveToFile()
    {
        MapSaveData save = new MapSaveData();
        save.units = new List<UnitData>(allUnits);

        string json = JsonUtility.ToJson(save, true);
        File.WriteAllText(lastSavePath, json);
        Debug.Log($"Saved to {Path.GetFullPath(lastSavePath)}");
    }

    public void LoadFromFile()
    {
        if (!File.Exists(lastSavePath))
        {
            Debug.LogWarning("Save file not found: " + lastSavePath);
            return;
        }

        string json = File.ReadAllText(lastSavePath);
        MapSaveData save = JsonUtility.FromJson<MapSaveData>(json);

        allUnits = save.units ?? new List<UnitData>();

        ClearAllViews();
        if (clusterRenderer != null)
            clusterRenderer.RebuildClusters();
        else
        {
            // Fallback: spawn all as individual views
            foreach (var u in allUnits)
            {
                var view = GetOrCreateView(u);
                view.Bind(u, false, 1);
            }
        }
    }
}
