using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class UnitManager : MonoBehaviour
{
    [Header("Scene refs")]
    public CameraController2D camController;
    public Transform unitContainer;
    public UnitSymbolView unitPrefab;
    public ArrowView arrowPrefab;

    [Header("UI")]
    public TMP_Dropdown dropdownFrameType;    // LAND/SEA/SUB/AIR
    public TMP_Dropdown dropdownAffiliation;  // Friendly/Hostile/Neutral
    public TMP_InputField inputNationTop;
    public TMP_InputField inputUnitCode;
    public TMP_InputField inputLabelBottom;

    public GameObject unitEditorPanel;
    public GameObject escapeMenuPanel;

    [Header("Save / Load paths (relative to project root)")]
    [Tooltip("Folder where map save files live, relative to project root")]
    public string savesFolderRelative = "Saves";
    [Tooltip("Default save filename inside the saves folder")]
    public string defaultSaveName = "map01.json";

    private readonly List<UnitData> units = new();
    private readonly Dictionary<string, UnitSymbolView> views = new();

    private readonly List<ArrowData> arrows = new();
    private readonly List<ArrowView> arrowViews = new();

    private UnitData selectedUnit;
    private UnitSymbolView selectedView;

    private UnitSymbolView draggedView;
    private Vector3 dragOffset;

    private bool isDrawingArrow = false;

    private void Awake()
    {
        if (camController == null)
            camController = FindFirstObjectByType<CameraController2D>();

        if (unitEditorPanel != null)
            unitEditorPanel.SetActive(false);

        if (escapeMenuPanel != null)
            escapeMenuPanel.SetActive(false);
    }

    private void Update()
    {
        HandleEscape();

        if (isDrawingArrow && Input.GetMouseButtonDown(0) && selectedUnit != null)
        {
            var cam = camController.Cam;
            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0;

            CreateArrowFromSelectedTo(world);
            isDrawingArrow = false;
        }

        HandleDrag();
    }

    private void HandleDrag()
    {
        var cam = camController != null ? camController.Cam : Camera.main;
        if (cam == null) return;

        // Start dragging with RMB
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 world2D = new Vector2(world.x, world.y);

            RaycastHit2D hit = Physics2D.Raycast(world2D, Vector2.zero);
            if (hit.collider != null)
            {
                var view = hit.collider.GetComponent<UnitSymbolView>();
                if (view != null)
                {
                    draggedView = view;
                    dragOffset = view.transform.position - new Vector3(world2D.x, world2D.y, 0);

                    // Optionally also select the unit when you start dragging
                    if (view.Data != null)
                        OnUnitClicked(view);
                }
            }
        }

        // While holding RMB, move the dragged view
        if (Input.GetMouseButton(1) && draggedView != null)
        {
            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0;
            Vector3 newPos = world + dragOffset;
            newPos.z = 0;

            draggedView.transform.position = newPos;

            if (draggedView.Data != null)
                draggedView.Data.worldPos = newPos;
        }

        if (Input.GetMouseButton(1) && draggedView != null)
        {
            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0;
            Vector3 newPos = world + dragOffset;
            newPos.z = 0;

            draggedView.transform.position = newPos;

            if (draggedView.Data != null)
            {
                draggedView.Data.worldPos = newPos;
                UpdateArrowsForUnit(draggedView.Data);
            }
        }

        // Release RMB to stop dragging
        if (Input.GetMouseButtonUp(1))
        {
            draggedView = null;
        }
    }

    private void UpdateArrowsForUnit(UnitData unit)
    {
        if (unit == null) return;
        string uid = unit.id;

        if (!views.TryGetValue(uid, out var view) || view == null)
            return;

        float startOffset = view.GetArrowStartOffset();
        float endOffset = 0.3f;

        for (int i = 0; i < arrows.Count; i++)
        {
            if (arrows[i].fromUnitId == uid)
            {
                var ad = arrows[i];
                ad.from = unit.worldPos;
                arrows[i] = ad;

                if (i < arrowViews.Count && arrowViews[i] != null)
                {
                    arrowViews[i].UpdateArrow(ad.from, ad.to, startOffset, endOffset);
                }
            }
        }
    }

    public void OnDeleteArrowsFromSelectedClicked()
    {
        if (selectedUnit == null) return;
        string uid = selectedUnit.id;

        for (int i = arrows.Count - 1; i >= 0; i--)
        {
            if (arrows[i].fromUnitId == uid)
            {
                if (i < arrowViews.Count && arrowViews[i] != null)
                    Destroy(arrowViews[i].gameObject);

                arrows.RemoveAt(i);
                arrowViews.RemoveAt(i);
            }
        }
    }


    private void HandleEscape()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (escapeMenuPanel != null)
                escapeMenuPanel.SetActive(!escapeMenuPanel.activeSelf);
        }
    }

    // ---------- UI: Add / Delete / Save unit / Arrow ----------

    public void OnAddUnitClicked()
    {
        Vector3 world = camController.Cam.transform.position;
        world.z = 0;

        UnitData data = new UnitData
        {
            id = System.Guid.NewGuid().ToString(),
            worldPos = world,
            frameType = "LAND",
            nationTop = "",
            unitCode = "",
            labelBottom = "",
            affiliation = UnitAffiliation.Friendly
        };

        units.Add(data);
        var view = Instantiate(unitPrefab, unitContainer);
        view.Bind(data);

        var click = view.GetComponent<UnitSymbolClick>();
        if (click != null)
            click.manager = this;

        views[data.id] = view;

        SelectUnit(data, view);
    }

    public void OnDeleteUnitClicked()
    {
        if (selectedUnit == null) return;

        for (int i = arrows.Count - 1; i >= 0; i--)
        {
            if (arrows[i].fromUnitId == selectedUnit.id)
            {
                arrows.RemoveAt(i);
                Destroy(arrowViews[i].gameObject);
                arrowViews.RemoveAt(i);
            }
        }

        units.Remove(selectedUnit);

        if (views.TryGetValue(selectedUnit.id, out var v) && v != null)
            Destroy(v.gameObject);

        views.Remove(selectedUnit.id);
        selectedUnit = null;
        selectedView = null;

        if (unitEditorPanel != null)
            unitEditorPanel.SetActive(false);
    }

    public void OnSaveUnitClicked()
    {
        if (selectedUnit == null || selectedView == null) return;

        ApplyUiToSelectedData();
        selectedView.Bind(selectedUnit);
        selectedView.SetSelected(false);

        selectedUnit = null;
        selectedView = null;

        if (unitEditorPanel != null)
            unitEditorPanel.SetActive(false);
    }

    public void OnDrawArrowClicked()
    {
        if (selectedUnit == null) return;
        ApplyUiToSelectedData();
        isDrawingArrow = true;
    }

    // ---------- Selection from click ----------

    public void OnUnitClicked(UnitSymbolView view)
    {
        if (view == null) return;

        if (selectedUnit != null && selectedView != null && view != selectedView)
        {
            ApplyUiToSelectedData();
            selectedView.Bind(selectedUnit);
            selectedView.SetSelected(false);
        }

        SelectUnit(view.Data, view);
    }

    private void SelectUnit(UnitData data, UnitSymbolView view)
    {
        selectedUnit = data;
        selectedView = view;

        foreach (var kv in views)
        {
            if (kv.Value != null)
                kv.Value.SetSelected(kv.Value == selectedView);
        }

        if (unitEditorPanel != null)
            unitEditorPanel.SetActive(true);

        PopulateUiFromSelectedData();
    }

    private void PopulateUiFromSelectedData()
    {
        if (selectedUnit == null) return;

        inputNationTop.text = selectedUnit.nationTop;
        inputUnitCode.text = selectedUnit.unitCode;
        inputLabelBottom.text = selectedUnit.labelBottom;

        dropdownFrameType.value = FrameTypeToIndex(selectedUnit.frameType);
        dropdownAffiliation.value = (int)selectedUnit.affiliation;
    }

    private void ApplyUiToSelectedData()
    {
        if (selectedUnit == null) return;

        selectedUnit.nationTop = inputNationTop.text;
        selectedUnit.unitCode = inputUnitCode.text;
        selectedUnit.labelBottom = inputLabelBottom.text;

        selectedUnit.frameType = IndexToFrameType(dropdownFrameType.value);
        selectedUnit.affiliation = (UnitAffiliation)dropdownAffiliation.value;

        if (selectedView != null)
        {
            selectedView.Bind(selectedUnit);
            selectedView.SetAffiliation(selectedUnit.affiliation);
        }
    }

    private int FrameTypeToIndex(string type) => type switch
    {
        "LAND" => 0,
        "SEA" => 1,
        "SUB" => 2,
        "AIR" => 3,
        _ => 0
    };

    private string IndexToFrameType(int index) => index switch
    {
        0 => "LAND",
        1 => "SEA",
        2 => "SUB",
        3 => "AIR",
        _ => "LAND"
    };

    // ---------- Arrows ----------

    private void CreateArrowFromSelectedTo(Vector2 target)
    {
        if (selectedUnit == null) return;

        float startOffset = selectedView != null ? selectedView.GetArrowStartOffset() : 0.5f;
        float endOffset = 0.3f;

        Color color = AffiliationColor(selectedUnit.affiliation);

        ArrowData ad = new ArrowData
        {
            fromUnitId = selectedUnit.id,
            from = selectedUnit.worldPos,
            to = target,
            color = color
        };
        arrows.Add(ad);

        var av = Instantiate(arrowPrefab);
        av.SetArrow(ad.from, ad.to, startOffset, endOffset, ad.color);
        arrowViews.Add(av);
    }

    private Color AffiliationColor(UnitAffiliation aff)
    {
        return aff switch
        {
            UnitAffiliation.Friendly => new Color(0.1f, 0.6f, 1f),
            UnitAffiliation.Hostile => new Color(1f, 0.2f, 0.2f),
            UnitAffiliation.Neutral => new Color(0.2f, 0.9f, 0.4f),
            _ => Color.white
        };
    }
    private void RebuildArrows()
    {
        foreach (var av in arrowViews)
            if (av != null) Destroy(av.gameObject);
        arrowViews.Clear();

        for (int i = 0; i < arrows.Count; i++)
        {
            ArrowData ad = arrows[i];
            var av = Instantiate(arrowPrefab);

            float startOffset = 0.5f;
            float endOffset = 0.3f;

            if (views.TryGetValue(ad.fromUnitId, out var v) && v != null && v.Data != null)
            {
                startOffset = v.GetArrowStartOffset();
                ad.from = v.Data.worldPos;
                arrows[i] = ad;
            }

            av.SetArrow(ad.from, ad.to, startOffset, endOffset, ad.color);
            arrowViews.Add(av);
        }
    }


    // ---------- Save / Load using folder + first file ----------

    public void OnSaveFileClicked()
    {
        string folder = ResolveProjectPath(savesFolderRelative);
        Directory.CreateDirectory(folder);

        string path = Path.Combine(folder, defaultSaveName);

        MapSaveData save = new MapSaveData
        {
            units = new List<UnitData>(units),
            arrows = new List<ArrowData>(arrows),
            //backgroundRelativePath = GetBackgroundRelativePath()
        };

        string json = JsonUtility.ToJson(save, true);
        File.WriteAllText(path, json);
        Debug.Log("Saved map to: " + path);
    }

    public void OnLoadFileClicked()
    {
        string folder = ResolveProjectPath(savesFolderRelative);
        if (!Directory.Exists(folder))
        {
            Debug.LogWarning("Save folder not found: " + folder);
            return;
        }

        string[] files = Directory.GetFiles(folder, "*.json");
        if (files.Length == 0)
        {
            Debug.LogWarning("No save files found in " + folder);
            return;
        }

        string path = files[0]; // first file it finds
        string json = File.ReadAllText(path);
        var save = JsonUtility.FromJson<MapSaveData>(json);

        // clear old
        foreach (var v in views.Values)
            if (v != null) Destroy(v.gameObject);
        views.Clear();
        units.Clear();

        foreach (var av in arrowViews)
            if (av != null) Destroy(av.gameObject);
        arrows.Clear();
        arrowViews.Clear();

        if (save.units != null)
            units.AddRange(save.units);
        if (save.arrows != null)
            arrows.AddRange(save.arrows);

        foreach (var u in units)
        {
            var view = Instantiate(unitPrefab, unitContainer);
            view.Bind(u);

            var click = view.GetComponent<UnitSymbolClick>();
            if (click != null)
                click.manager = this;

            views[u.id] = view;
        }

        RebuildArrows();

        if (!string.IsNullOrEmpty(save.backgroundRelativePath))
        {
            MapLoader ml = FindFirstObjectByType<MapLoader>();
            //if (ml != null)
            //    ml.LoadFromRelativePath(save.backgroundRelativePath);
        }

        Debug.Log("Loaded map from: " + path);
    }

    public void OnLoadBackgroundClicked()
    {
        // purely path-based now: edit MapLoader.relativeImagePath in inspector
        // and call this to reload
        MapLoader ml = FindFirstObjectByType<MapLoader>();
        //if (ml != null && !string.IsNullOrEmpty(ml.relativeImagePath))
        //{
        //    string full = ResolveProjectPath(ml.relativeImagePath);
        //    ml.LoadImageFromPath(full);
        //}
    }

    //private string GetBackgroundRelativePath()
    //{
    //    MapLoader ml = FindFirstObjectByType<MapLoader>();
    //    if (ml == null)
    //        return null;

    //    // If we know the full path, compress to relative; else just use what’s set
    //    //if (!string.IsNullOrEmpty(MapLoader.CurrentImagePath))
    //    //    return ml.GetRelativePathFromFull(MapLoader.CurrentImagePath);

    //    //return ml.relativeImagePath;
    //}

    private string ResolveProjectPath(string rel)
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.GetFullPath(Path.Combine(root, rel));
    }
}
