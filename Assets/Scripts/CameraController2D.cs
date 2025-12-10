using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController2D : MonoBehaviour
{
    public float panSpeed = 20f;
    public float zoomSpeed = 10f;
    public float minSize = 10f;
    public float maxSize = 200f;

    private Camera cam;
    public System.Action OnViewChanged; // hook for clustering

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        bool changed = false;

        // Pan with MMB or WASD (customise as needed)
        Vector3 delta = Vector3.zero;

        if (Input.GetMouseButton(2))
        {
            float dx = -Input.GetAxis("Mouse X");
            float dy = -Input.GetAxis("Mouse Y");
            delta += new Vector3(dx, dy, 0f) * panSpeed * Time.deltaTime * cam.orthographicSize / 50f;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            delta += new Vector3(h, v, 0f) * panSpeed * Time.deltaTime * cam.orthographicSize / 50f;
        }

        if (delta.sqrMagnitude > 0f)
        {
            transform.position += delta;
            changed = true;
        }

        // Zoom with scroll
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newSize = cam.orthographicSize * Mathf.Exp(-scroll * zoomSpeed * 0.01f);
            newSize = Mathf.Clamp(newSize, minSize, maxSize);
            if (!Mathf.Approximately(newSize, cam.orthographicSize))
            {
                cam.orthographicSize = newSize;
                changed = true;
            }
        }

        if (changed && OnViewChanged != null)
        {
            OnViewChanged.Invoke();
        }
    }

    public Camera Cam => cam;
}
