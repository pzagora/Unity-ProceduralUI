using UnityEditor;

namespace ProceduralUI.Editor
{
    internal static class InspectorSection
    {
        internal static bool DrawHeader(string title, string stateKey)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                var expanded = SessionState.GetBool(stateKey, true);
                var selected = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);
                if (selected != expanded)
                    SessionState.SetBool(stateKey, selected);

                return selected;
            }
        }
    }
}
