using UnityEngine;
using TMPro;

[RequireComponent(typeof(LineRenderer))]
public class UnitSymbolView : MonoBehaviour
{
    public TextMeshProUGUI textTop;
    public TextMeshProUGUI textCenter;
    public TextMeshProUGUI textBottom;

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
        camController = FindFirstObjectByType<CameraController2D>();
    }

    private void OnEnable()
    {
        if (camController != null)
            camController.OnViewChanged += UpdateLineThickness;
    }

    private void OnDisable()
    {
        if (camController != null)
            camController.OnViewChanged -= UpdateLineThickness;
    }

    public void Bind(UnitData unitData, bool asCluster, int clusterSize)
    {
        data = unitData;
        isCluster = asCluster;
        clusterCount = clusterSize;

        frameType = unitData.frameType;
        textTop.text = unitData.nationTop;
        textCenter.text = asCluster ? unitData.unitCode + $" ({clusterSize})" : unitData.unitCode;
        textBottom.text = unitData.labelBottom;

        transform.position = unitData.worldPos;

        RebuildFrameGeometry();
        UpdateLineThickness();
    }

    private void RebuildFrameGeometry()
    {
        switch (frameType)
        {
            case "LAND":
                BuildRectangle();
                break;
            case "SEA":
                BuildCircle();
                break;
            case "SUB":
                BuildUSemiCircle(false); // open top
                break;
            case "AIR":
                BuildUSemiCircle(true); // open bottom (∩)
                break;
            default:
                BuildRectangle();
                break;
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
    }

    private void BuildCircle()
    {
        int segs = Mathf.Max(8, circleSegments);
        lineRenderer.positionCount = segs;

        float radius = Mathf.Min(frameWidth, frameHeight) * 0.5f;
        for (int i = 0; i < segs; i++)
        {
            float t = (float)i / segs * Mathf.PI * 2f;
            float x = Mathf.Cos(t) * radius;
            float y = Mathf.Sin(t) * radius;
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    // U and inverted-U shapes (open on one side)
    private void BuildUSemiCircle(bool inverted)
    {
        int segs = Mathf.Max(8, circleSegments);
        lineRenderer.positionCount = segs + 3; // sides + arc

        float radius = frameWidth * 0.5f;
        float h = frameHeight;

        // left vertical
        lineRenderer.SetPosition(0, new Vector3(-radius, -h * 0.5f, 0));
        lineRenderer.SetPosition(1, new Vector3(-radius, h * 0.5f, 0));

        // arc
        // For SUB (open top) we draw bottom arc; for AIR (open bottom) we draw top arc
        float startAngle = inverted ? 0f : Mathf.PI;
        float endAngle = inverted ? Mathf.PI : Mathf.PI * 2f;
        int arcPoints = segs;
        for (int i = 0; i <= arcPoints; i++)
        {
            float t = Mathf.Lerp(startAngle, endAngle, (float)i / arcPoints);
            float x = Mathf.Cos(t) * radius;
            float y = Mathf.Sin(t) * (h * 0.5f / radius); // squash to fit height
            lineRenderer.SetPosition(2 + i, new Vector3(x, y, 0));
        }

        // right vertical
        lineRenderer.SetPosition(lineRenderer.positionCount - 1,
            new Vector3(radius, -h * 0.5f, 0));
    }

    private void UpdateLineThickness()
    {
        if (camController == null) return;

        float factor = camController.Cam.orthographicSize / referenceOrthoSize;
        lineRenderer.startWidth = lineRenderer.endWidth = lineThicknessAtBaseZoom * factor;
    }

    public UnitData Data => data;
}
