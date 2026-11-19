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

        [UnityTest]
        public IEnumerator CollapsedSectionBadgesDrawAtWideAndNarrowWidths()
        {
            const string shapeState = "ProceduralUI.Inspector.Shape";
            const string gradientState = "ProceduralUI.Inspector.Gradient";
            const string interactionState = "ProceduralUI.Inspector.Interaction";

            var image = new GameObject("Badged", typeof(ProceduralImage)).GetComponent<ProceduralImage>();
            image.CornerMode = CornerMode.Uniform;
            image.Gradient.Enabled = true;
            image.Gradient.Type = GradientType.Linear;

            var wasShape = SessionState.GetBool(shapeState, true);
            var wasGradient = SessionState.GetBool(gradientState, true);
            var wasInteraction = SessionState.GetBool(interactionState, true);
            SessionState.SetBool(shapeState, false);
            SessionState.SetBool(gradientState, false);
            SessionState.SetBool(interactionState, false);

            var window = ScriptableObject.CreateInstance<ProceduralImageInspectorWindow>();
            window.ShowUtility();

            try
            {
                window.Inspector = UnityEditor.Editor.CreateEditor(image);

                window.position = new Rect(50, 50, 500, 800);
                window.Repaint();
                yield return null;

                window.position = new Rect(50, 50, 80, 800);
                window.Repaint();
                yield return null;

                Assert.That(window.DrawCount, Is.GreaterThan(0));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                SessionState.SetBool(shapeState, wasShape);
                SessionState.SetBool(gradientState, wasGradient);
                SessionState.SetBool(interactionState, wasInteraction);
                Object.DestroyImmediate(window.Inspector);
                window.Close();
                Object.DestroyImmediate(image.gameObject);
            }
        }
    }
}
