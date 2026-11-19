using UnityEngine;

namespace ProceduralUI
{
    internal static class ProceduralImageResources
    {
        private static Material _material;
        private static Sprite _whiteSprite;

        internal static Material Material
        {
            get
            {
                if (_material == null)
                    _material = Resources.Load<Material>(MATERIAL_PATH);

                return _material;
            }
        }

        internal static Sprite WhiteSprite
            => _whiteSprite != null ? _whiteSprite : _whiteSprite = Resources.Load<Sprite>(SPRITE_PATH);

        private const string MATERIAL_PATH = "ProceduralUI/Default";
        private const string SPRITE_PATH = "ProceduralUI/White";
    }
}
