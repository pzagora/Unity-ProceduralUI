using UnityEditor;
using UnityEngine;

namespace ProceduralUI.Tests
{
    internal sealed class ProceduralImageInspectorWindow : EditorWindow
    {
        internal UnityEditor.Editor Inspector;
        internal int DrawCount;

        private void OnGUI()
        {
            if (Inspector == null)
                return;

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width)))
                Inspector.OnInspectorGUI();
            DrawCount++;
        }
    }
}
