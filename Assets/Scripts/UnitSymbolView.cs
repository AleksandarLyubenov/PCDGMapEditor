using UnityEngine;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class UnitSymbolView : MonoBehaviour
{
    [Header("Materials")]
    public Material lineMaterialTemplate;

    public TextMeshPro textTop;
    public TextMeshPro textCenter;
    public TextMeshPro textBottom;

    [Header("Frame settings")]
    public float frameWidth = 2f;
    public float frameHeight = 1.2f;
    public int circleSegments = 32;
    public float lineThickness = 0.1f;

    [Header("Colors")]
    public Color friendlyColor = new Color(0.1f, 0.6f, 1f);
    public Color hostileColor = new Color(1f, 0.2f, 0.2f);
    public Color neutralColor = new Color(0.2f, 0.9f, 0.4f);
    public Color selectedTint = new Color(1f, 1f, 0.4f);

    private LineRenderer lineRenderer;
    private UnitData boundData;
    private bool isSelected;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        ConfigureLineRenderer();
    }

    private void OnEnable()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        ConfigureLineRenderer();
#if UNITY_EDITOR
        if (!Application.isPlaying)
            RefreshEditorPreview();
#endif
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        ConfigureLineRenderer();
#if UNITY_EDITOR
        if (!Application.isPlaying)
            RefreshEditorPreview();
#endif
    }

    private void ConfigureLineRenderer()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) return;

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.startWidth = lineRenderer.endWidth = lineThickness;

        if (Application.isPlaying)
        {
            // RUNTIME: each symbol gets its own instance so colors are independent
            if (lineRenderer.material == null ||
                (lineMaterialTemplate != null && lineRenderer.material.shader != lineMaterialTemplate.shader))
            {
                Material baseMat = lineMaterialTemplate != null
                    ? lineMaterialTemplate
                    : lineRenderer.sharedMaterial;

                if (baseMat == null)
                {
                    // very last fallback, but only in play mode
                    baseMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    baseMat.color = Color.white;
                }

                lineRenderer.material = new Material(baseMat);
            }
        }
        else
        {
            // EDITOR: only use sharedMaterial; no runtime instances
            if (lineRenderer.sharedMaterial == null && lineMaterialTemplate != null)
            {
                lineRenderer.sharedMaterial = lineMaterialTemplate;
            }
        }
    }



    // ---------- Public API ----------

    public void Bind(UnitData data)
    {
        boundData = data;

        transform.position = data.worldPos;

        if (textTop) textTop.text = data.nationTop;
        if (textCenter) textCenter.text = data.unitCode;
        if (textBottom) textBottom.text = data.labelBottom;

        RebuildFrameGeometry();
        ApplyAffiliationColor();
    }

    public void UpdateDataFromView()
    {
        if (boundData == null) return;
        boundData.worldPos = transform.position;
        boundData.nationTop = textTop ? textTop.text : boundData.nationTop;
        boundData.unitCode = textCenter ? textCenter.text : boundData.unitCode;
        boundData.labelBottom = textBottom ? textBottom.text : boundData.labelBottom;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyAffiliationColor();
    }

    public void SetAffiliation(UnitAffiliation aff)
    {
        if (boundData != null)
            boundData.affiliation = aff;

        ApplyAffiliationColor();
    }

    public UnitData Data => boundData;

    // ---------- Coloring ----------

    private void ApplyAffiliationColor()
    {
        if (lineRenderer == null) return;

        Color baseColor = Color.white;
        if (boundData != null)
        {
            switch (boundData.affiliation)
            {
                case UnitAffiliation.Friendly: baseColor = friendlyColor; break;
                case UnitAffiliation.Hostile: baseColor = hostileColor; break;
                case UnitAffiliation.Neutral: baseColor = neutralColor; break;
            }
        }

        if (isSelected)
            baseColor = Color.Lerp(baseColor, selectedTint, 0.5f);

        lineRenderer.startColor = lineRenderer.endColor = baseColor;

        if (Application.isPlaying && lineRenderer.material != null)
            lineRenderer.material.color = baseColor;
    }



    // ---------- Frame geometry ----------

    public void RefreshEditorPreview()
    {
        // Use LAND as default preview
        BuildRectangle();
        lineRenderer.startWidth = lineRenderer.endWidth = lineThickness;
    }

    public float GetArrowStartOffset()
    {
        float halfW = frameWidth * 0.5f;
        float halfH = frameHeight * 0.5f;
        float r = Mathf.Max(halfW, halfH);
        return r + lineThickness * 1.5f;
    }


    private void RebuildFrameGeometry()
    {
        string type = boundData != null ? boundData.frameType : "LAND";

        switch (type)
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
        float w = frameWidth * 0.5f;
        float h = frameHeight * 0.5f;

        Vector3[] pts =
        {
            new(-w, -h, 0),
            new(-w,  h, 0),
            new( w,  h, 0),
            new( w, -h, 0),
        };

        lineRenderer.loop = true;
        lineRenderer.positionCount = pts.Length;
        lineRenderer.SetPositions(pts);
    }

    private void BuildCircle()
    {
        int segs = Mathf.Max(8, circleSegments);
        float r = frameWidth * 0.5f;

        lineRenderer.loop = true;
        lineRenderer.positionCount = segs;

        for (int i = 0; i < segs; i++)
        {
            float t = (float)i / segs * Mathf.PI * 2f;
            float x = Mathf.Cos(t) * r;
            float y = Mathf.Sin(t) * r;
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    // U (SUB) and ∩ (AIR) using mirror trick from before
    private void BuildUSemiCircle(bool inverted)
    {
        int segs = Mathf.Max(8, circleSegments);
        float r = frameWidth * 0.5f;
        float top = frameHeight * 0.5f;
        float midY = 0f;

        var pts = new System.Collections.Generic.List<Vector3>();

        // base: U open at top
        pts.Add(new Vector3(-r, top, 0));
        pts.Add(new Vector3(-r, midY, 0));

        for (int i = 0; i <= segs; i++)
        {
            float t = Mathf.Lerp(Mathf.PI, 2f * Mathf.PI, (float)i / segs);
            float x = Mathf.Cos(t) * r;
            float y = midY + Mathf.Sin(t) * r;
            pts.Add(new Vector3(x, y, 0));
        }

        pts.Add(new Vector3(r, midY, 0));
        pts.Add(new Vector3(r, top, 0));

        if (inverted)
        {
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                p.y = -p.y;
                pts[i] = p;
            }
        }

        lineRenderer.loop = false;
        lineRenderer.positionCount = pts.Count;
        lineRenderer.SetPositions(pts.ToArray());
    }
}
