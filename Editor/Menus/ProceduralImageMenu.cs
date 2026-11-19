using UnityEditor;
using UnityEngine;

namespace ProceduralUI.Editor
{
    internal static class ProceduralImageMenu
    {
        private const string CREATE_MENU = "GameObject/UI (Canvas)/Procedural Image";
        private const string OBJECT_NAME = "Procedural Image";
        private const string IMAGE_MENU = "GameObject/UI (Canvas)/Image";

        [MenuItem(CREATE_MENU, false, 2001)]
        private static void Create(MenuCommand command)
        {
            var undoGroup = Undo.GetCurrentGroup();
            if (command.context is GameObject parent)
                Selection.activeGameObject = parent;

            if (!EditorApplication.ExecuteMenuItem(IMAGE_MENU))
                return;

            var gameObject = Selection.activeGameObject;
            var image = gameObject.GetComponent<UnityEngine.UI.Image>();
            Undo.DestroyObjectImmediate(image);
            Undo.AddComponent<ProceduralImage>(gameObject);
            Undo.RecordObject(gameObject, OBJECT_NAME);
            gameObject.name = OBJECT_NAME;
            Undo.CollapseUndoOperations(undoGroup);
        }
    }
}
