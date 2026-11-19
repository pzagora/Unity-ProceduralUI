using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProceduralUI.Tests
{
    public sealed class ProceduralImageInspectorTests
    {
        [UnityTest]
        public IEnumerator InspectorDrawsAllCornerModesAndMixedSelections()
        {
            var first = new GameObject("First", typeof(ProceduralImage)).GetComponent<ProceduralImage>();
            var second = new GameObject("Second", typeof(ProceduralImage)).GetComponent<ProceduralImage>();
            var window = ScriptableObject.CreateInstance<ProceduralImageInspectorWindow>();
            window.position = new Rect(50, 50, 400, 800);
            window.ShowUtility();

            try
            {
                window.Inspector = UnityEditor.Editor.CreateEditor(first);
                foreach (CornerMode mode in System.Enum.GetValues(typeof(CornerMode)))
                {
                    first.CornerMode = mode;
                    window.Repaint();
                    yield return null;
                }

                first.Gradient.Enabled = true;
                foreach (GradientType type in System.Enum.GetValues(typeof(GradientType)))
                {
                    first.Gradient.Type = type;
                    window.Repaint();
                    yield return null;
                }

                Object.DestroyImmediate(window.Inspector);
                first.CornerMode = CornerMode.Uniform;
                second.CornerMode = CornerMode.Individual;
                second.Gradient.Enabled = false;
                window.Inspector = UnityEditor.Editor.CreateEditor(new Object[] { first, second });
                window.Repaint();
                yield return null;
                Assert.That(window.DrawCount, Is.GreaterThan(0));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(window.Inspector);
                window.Close();
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
            }
        }
    }
}
