using UnityEditor;

namespace ProceduralUI.Editor
{
    [CustomEditor(typeof(UIGradient))]
    [CanEditMultipleObjects]
    internal sealed class UIGradientEditor : UnityEditor.Editor
    {
        private GradientInspector _gradient;

        private const string GRADIENT_PROPERTY = "gradient";

        private void OnEnable()
            => _gradient = new GradientInspector(serializedObject.FindProperty(GRADIENT_PROPERTY));

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _gradient.Draw();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
