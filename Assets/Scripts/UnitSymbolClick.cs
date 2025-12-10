using UnityEngine;

[RequireComponent(typeof(Collider2D))] // or Collider for 3D
public class UnitSymbolClick : MonoBehaviour
{
    public UnitManager manager;
    private UnitSymbolView view;

    private void Awake()
    {
        view = GetComponent<UnitSymbolView>();
    }

    private void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0) && manager != null)
        {
            manager.OnUnitClicked(view);
        }
    }
}
