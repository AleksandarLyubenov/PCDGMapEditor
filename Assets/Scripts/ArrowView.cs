using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ArrowView : MonoBehaviour
{
    [Header("Material")]
    public Material arrowMaterialTemplate;

    public float lineWidth = 0.08f;
    public float headLength = 0.7f;
    public float headAngle = 25f;

    private LineRenderer lr;
    private Color arrowColor = Color.white;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.startWidth = lr.endWidth = lineWidth;

        // Use the template material so builds always have a valid shader
        if (Application.isPlaying)
        {
            if (arrowMaterialTemplate != null)
            {
                lr.material = new Material(arrowMaterialTemplate);
            }
            else if (lr.material == null)
            {
                lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            }
        }
        else
        {
            if (lr.sharedMaterial == null && arrowMaterialTemplate != null)
            {
                lr.sharedMaterial = arrowMaterialTemplate;
            }
        }
    }


    // MAIN API (used when creating & rebuilding arrows)
    public void SetArrow(Vector2 from, Vector2 to, float startOffset, float endOffset, Color color)
    {
        arrowColor = color;
        InternalSetArrow(from, to, startOffset, endOffset, arrowColor);
    }

    // OVERLOAD for old uses (optional but convenient)
    public void SetArrow(Vector2 from, Vector2 to, Color color)
    {
        arrowColor = color;
        InternalSetArrow(from, to, 0.5f, 0.3f, arrowColor);
    }

    // Called when the parent unit moves – keep same color
    public void UpdateArrow(Vector2 from, Vector2 to, float startOffset, float endOffset)
    {
        InternalSetArrow(from, to, startOffset, endOffset, arrowColor);
    }

    private void InternalSetArrow(Vector2 from, Vector2 to, float startOffset, float endOffset, Color color)
    {
        if (lr == null) lr = GetComponent<LineRenderer>();

        Vector3 a = new Vector3(from.x, from.y, 0);
        Vector3 b = new Vector3(to.x, to.y, 0);
        Vector3 dir = b - a;

        if (dir.sqrMagnitude < 0.0001f)
        {
            lr.positionCount = 0;
            return;
        }

        dir.Normalize();

        Vector3 start = a + dir * startOffset;
        Vector3 end = b - dir * endOffset;

        Vector3 backDir = -dir;
        Quaternion rotRight = Quaternion.Euler(0, 0, headAngle);
        Quaternion rotLeft = Quaternion.Euler(0, 0, -headAngle);

        Vector3 headDir1 = rotRight * backDir;
        Vector3 headDir2 = rotLeft * backDir;

        Vector3 head1 = end + headDir1 * headLength;
        Vector3 head2 = end + headDir2 * headLength;

        lr.positionCount = 5;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.SetPosition(2, head1);
        lr.SetPosition(3, end);
        lr.SetPosition(4, head2);

        lr.startColor = lr.endColor = color;
        if (Application.isPlaying && lr.material != null)
            lr.material.color = color;
    }
}
