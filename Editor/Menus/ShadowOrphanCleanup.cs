using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralUI.Editor
{
    [InitializeOnLoad]
    internal static class ShadowOrphanCleanup
    {
        private const string RENDERER_NAME = "UI Shadow";

        static ShadowOrphanCleanup()
        {
            AssemblyReloadEvents.beforeAssemblyReload += DestroyRenderers;
            EditorApplication.delayCall += Sweep;
        }

        private static void Sweep()
        {
            DestroyRenderers();

            foreach (var shadow in Resources.FindObjectsOfTypeAll<UIShadow>())
            {
                if (shadow != null && !EditorUtility.IsPersistent(shadow))
                    shadow.Refresh();
            }

            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static void DestroyRenderers()
        {
            foreach (var image in Resources.FindObjectsOfTypeAll<RawImage>())
            {
                if (image == null || EditorUtility.IsPersistent(image))
                    continue;

                var gameObject = image.gameObject;
                if (gameObject.name != RENDERER_NAME || (gameObject.hideFlags & HideFlags.HideInHierarchy) == 0)
                    continue;

                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
