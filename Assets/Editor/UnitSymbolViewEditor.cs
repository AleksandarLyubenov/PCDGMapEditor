#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UnitSymbolView))]
public class UnitSymbolViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default fields first
        base.OnInspectorGUI();

        var view = (UnitSymbolView)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh Symbol"))
        {
            //view.Refresh();

            // Make sure scene knows it changed
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(view);
                SceneView.RepaintAll();
            }
        }
    }
}
#endif
