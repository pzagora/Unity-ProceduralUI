using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralUI.Editor
{
    internal static class ManagedMask
    {
        private const string SHOW_MASK_GRAPHIC_PROPERTY = "m_ShowMaskGraphic";

        internal static bool HasMask(GameObject gameObject)
            => gameObject != null && gameObject.TryGetComponent<Mask>(out _);

        internal static void SetMaskChildren(GameObject gameObject, bool enabled)
        {
            if (gameObject == null)
                return;

            var has = gameObject.TryGetComponent<Mask>(out var mask);
            if (enabled && !has)
            {
                mask = Undo.AddComponent<Mask>(gameObject);
                Hide(mask);
            }
            else if (!enabled && has)
            {
                Undo.DestroyObjectImmediate(mask);
            }
        }

        internal static void HideManagedMask(GameObject gameObject)
        {
            if (gameObject != null && gameObject.TryGetComponent<Mask>(out var mask))
                Hide(mask);
        }

        private static void Hide(Mask mask)
        {
            if ((mask.hideFlags & HideFlags.HideInInspector) != 0)
                return;

            mask.hideFlags |= HideFlags.HideInInspector;
        }

        internal static string ShowMaskGraphicProperty => SHOW_MASK_GRAPHIC_PROPERTY;
    }
}
