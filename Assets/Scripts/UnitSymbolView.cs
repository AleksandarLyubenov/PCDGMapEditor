using UnityEngine;
using TMPro;

[ExecuteAlways]   // ← this is the key line
[RequireComponent(typeof(LineRenderer))]
public class UnitSymbolView : MonoBehaviour
{
    public TextMeshPro textTop;
    public TextMeshPro textCenter;
    public TextMeshPro textBottom;

    [Header("Frame settings")]
    public float frameWidth = 1.5f;
    public float frameHeight = 1.0f;
    public int circleSegments = 24;
    public float lineThicknessAtBaseZoom = 0.05f;
    public float referenceOrthoSize = 50f;

    private LineRenderer lineRenderer;
    private string frameType;

    private CameraController2D camController;
    private UnitData data;

    private bool isCluster = false;
    private int clusterCount = 1;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        // In edit mode there may be no camera controller – that's fine.
        if (!Application.isPlaying)
        {
            camController = null;
        }
        else
        {
            camController = FindFirstObjectByType<CameraController2D>();
        }
    }

    private void OnEnable()
    {
        if (Application.isPlaying && camController == null)
            camController = FindFirstObjectByType<CameraController2D>();

        if (camController != null)
            camController.OnViewChanged += UpdateLineThickness;

        // In edit mode, show something even without data bound
        if (!Application.isPlaying)
            Refresh();
    }

    private void OnDisable()
    {
        if (camController != null)
            camController.OnViewChanged -= UpdateLineThickness;
    }

    // Called automatically when you change values in the Inspector
    private void OnValidate()
    {
        // Avoid spamming when prefab is not fully initialised
        if (!isActiveAndEnabled) return;

        Refresh();
    }

    /// <summary>
    /// Rebuild frame + thickness. Safe to call in edit mode.
    /// </summary>
    public void Refresh()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        RebuildFrameGeometry();
        UpdateLineThickness();
    }

    public void Bind(UnitData unitData, bool asCluster, int clusterSize)
    {
        data = unitData;
        isCluster = asCluster;
        clusterCount = clusterSize;

        frameType = unitData.frameType;
        if (textTop) textTop.text = unitData.nationTop;
        if (textCenter) textCenter.text = asCluster ? unitData.unitCode + $" ({clusterSize})" : unitData.unitCode;
        if (textBottom) textBottom.text = unitData.labelBottom;

        transform.position = unitData.worldPos;

        Refresh();
    }

    private void RebuildFrameGeometry()
    {
        switch (frameType)
        {
            case "LAND": BuildRectangle(); break;
            case "SEA": BuildCircle(); break;
            case "SUB": BuildUSemiCircle(false); break;
            case "AIR": BuildUSemiCircle(true); break;
            default: BuildRectangle(); break;
        }
    }

    private void BuildRectangle()
    {
        Vector3[] pts = new Vector3[4];
        float w = frameWidth * 0.5f;
        float h = frameHeight * 0.5f;

        pts[0] = new Vector3(-w, -h, 0);
        pts[1] = new Vector3(-w, h, 0);
        pts[2] = new Vector3(w, h, 0);
        pts[3] = new Vector3(w, -h, 0);

        lineRenderer.positionCount = pts.Length;
        lineRenderer.SetPositions(pts);
        lineRenderer.loop = true;
    }

    private void BuildCircle()
    {
        int segs = Mathf.Max(8, circleSegments);

        lineRenderer.loop = true;
        lineRenderer.positionCount = segs;

        float radius = frameWidth * 0.5f;   // wider circle

        for (int i = 0; i < segs; i++)
        {
            float t = (float)i / segs * Mathf.PI * 2f;
            float x = Mathf.Cos(t) * radius;
            float y = Mathf.Sin(t) * radius; // keep the circle circular
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    private void BuildUSemiCircle(bool inverted)
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        int segs = Mathf.Max(8, circleSegments);
        float r = frameWidth * 0.5f;      // radius from width
        float top = frameHeight * 0.5f;
        float midY = 0f;

        // 1) Build a perfect SUBSURFACE U (open at the top) in local space.
        var pts = new System.Collections.Generic.List<Vector3>();

        // left vertical: top -> mid
        pts.Add(new Vector3(-r, top, 0));
        pts.Add(new Vector3(-r, midY, 0));

        // bottom semicircle, centre at (0, midY), angles π -> 2π
        for (int i = 0; i <= segs; i++)
        {
            float t = Mathf.Lerp(Mathf.PI, 2f * Mathf.PI, (float)i / segs);
            float x = Mathf.Cos(t) * r;
            float y = midY + Mathf.Sin(t) * r;
            pts.Add(new Vector3(x, y, 0));
        }

        // right vertical: mid -> top
        pts.Add(new Vector3(r, midY, 0));
        pts.Add(new Vector3(r, top, 0));

        // 2) If this is the AIR symbol, flip it vertically.
        if (inverted)
        {
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                p.y = -p.y;          // mirror over horizontal axis
                pts[i] = p;
            }
        }

        // 3) Push to LineRenderer
        lineRenderer.loop = false;
        lineRenderer.positionCount = pts.Count;
        lineRenderer.SetPositions(pts.ToArray());
    }



    private void UpdateLineThickness()
    {
        if (lineRenderer == null) return;

        // In edit mode (no camera), just use base thickness
        if (camController == null || !Application.isPlaying)
        {
            lineRenderer.startWidth = lineRenderer.endWidth = lineThicknessAtBaseZoom;
            return;
        }

        float factor = camController.Cam.orthographicSize / referenceOrthoSize;
        lineRenderer.startWidth = lineRenderer.endWidth = lineThicknessAtBaseZoom * factor;
    }

    public UnitData Data => data;
}
