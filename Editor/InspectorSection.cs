using UnityEditor;
using UnityEngine;

namespace ProceduralUI.Editor
{
    internal static class InspectorSection
    {
        private static GUIStyle _headerStyle;

        internal static bool DrawHeader(string title, string stateKey)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                var expanded = SessionState.GetBool(stateKey, true);
                _headerStyle ??= new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
                var selected = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), expanded, title, true, _headerStyle);
                if (selected != expanded)
                    SessionState.SetBool(stateKey, selected);

                return selected;
            }
        }
    }
}
