using UnityEngine;

[RequireComponent(typeof(UnitSymbolView))]
public class UnitSymbolDrag : MonoBehaviour
{
    public int mouseButton = 1; // 1 = RMB

    private bool dragging;
    private Vector3 offset;
    private CameraController2D camController;
    private UnitSymbolView view;

    private void Awake()
    {
        camController = FindFirstObjectByType<CameraController2D>();
        view = GetComponent<UnitSymbolView>();
    }

    private void OnMouseDown()
    {
        if (Input.GetMouseButton(mouseButton))
        {
            dragging = true;
            Vector3 world = camController.Cam.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0;
            offset = transform.position - world;
        }
    }

    private void OnMouseUp()
    {
        dragging = false;
    }

    private void Update()
    {
        if (dragging)
        {
            Vector3 world = camController.Cam.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0;
            transform.position = world + offset;

            // Update underlying data position
            if (view.Data != null)
            {
                view.Data.worldPos = transform.position;
            }
        }
    }
}
