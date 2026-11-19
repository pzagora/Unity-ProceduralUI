using UnityEngine;
using UnityEngine.UI;

namespace ProceduralUI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    [AddComponentMenu("")]
    public sealed class UIShadowRenderer : MonoBehaviour, ILayoutIgnorer
    {
        [SerializeField, HideInInspector] private RawImage image;
        [SerializeField, HideInInspector] private UIShadow owner;

        public bool ignoreLayout => true;

        public RawImage Image => image != null ? image : image = GetComponent<RawImage>();

        public UIShadow Owner
        {
            set => owner = value;
        }

        public bool IsOrphan => owner == null;

        private void Update()
        {
            if (owner == null)
                DestroySelf();
        }

        public void Apply(Texture texture, Color color, Material material)
        {
            var target = Image;
            target.texture = texture;
            target.color = color;
            target.material = null;
            target.material = material;
            target.raycastTarget = false;
            target.SetAllDirty();
        }

        private void DestroySelf()
        {
            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }
    }
}
