using System;
using UnityEngine;

namespace ProceduralUI
{
    internal sealed class ProceduralImageAssets : ScriptableObject
    {
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Sprite whiteSprite;

        private static ProceduralImageAssets _instance;

        internal static Material Material => Instance.defaultMaterial;
        internal static Sprite WhiteSprite => Instance.whiteSprite;

        internal static ProceduralImageAssets Instance
        {
            get
            {
#if UNITY_EDITOR
                if (_instance == null)
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<ProceduralImageAssets>(
                        UnityEditor.AssetDatabase.GUIDToAssetPath(ASSET_GUID));
#endif
                return _instance != null ? _instance : throw new InvalidOperationException(MISSING_ASSETS);
            }
        }

        private const string ASSET_GUID = "4b70bc2af84b46bfb9782285b14185e2";
        private const string MISSING_ASSETS = "Procedural UI assets are unavailable. Ensure the package asset is included in Player Settings > Preloaded Assets.";

        private void OnEnable()
            => _instance = this;

        private void OnDisable()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
