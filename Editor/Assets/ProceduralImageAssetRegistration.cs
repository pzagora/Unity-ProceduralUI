using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ProceduralUI.Editor
{
    [InitializeOnLoad]
    internal sealed class ProceduralImageAssetRegistration : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private const string INVALID_ASSETS = "Procedural UI requires its default material, shader and white sprite to build.";

        static ProceduralImageAssetRegistration()
            => EditorApplication.delayCall += Register;

        public void OnPreprocessBuild(BuildReport report)
        {
            Register();
            if (ProceduralImageAssets.Material == null || ProceduralImageAssets.Material.shader == null ||
                ProceduralImageAssets.WhiteSprite == null)
                throw new BuildFailedException(INVALID_ASSETS);
        }

        internal static void Register()
        {
            var asset = ProceduralImageAssets.Instance;
            var preloaded = PlayerSettings.GetPreloadedAssets();
            if (Array.IndexOf(preloaded, asset) >= 0)
                return;

            Array.Resize(ref preloaded, preloaded.Length + 1);
            preloaded[preloaded.Length - 1] = asset;
            PlayerSettings.SetPreloadedAssets(preloaded);
        }
    }
}
