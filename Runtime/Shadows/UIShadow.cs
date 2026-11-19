using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ProceduralUI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("")]
    public sealed class UIShadow : MonoBehaviour, IMeshModifier
    {
        [SerializeField] private Color color = new(0f, 0f, 0f, 0.5f);
        [SerializeField] private Vector2 offset = Vector2.zero;
        [SerializeField, Min(0)] private float size = 5f;
        [SerializeField, Min(0)] private float spread;
        [SerializeField] private bool sampleContentColor;

        private static readonly int ClipRectId = Shader.PropertyToID("_ClipRect");
        private static readonly int ColorMaskId = Shader.PropertyToID("_ColorMask");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BlurDirId = Shader.PropertyToID("_BlurDir");
        private const int MAX_TEXTURE_SIZE = 4096;
        private const int MAX_DOWNSAMPLE = 4;
        private const float MAX_WORKING_RADIUS = 12f;
        private const int BLUR_PASSES = 2;
        private const string SHADER_NAME = "Hidden/ProceduralUI/UIShadowDisplay";
        private const string BLUR_SHADER_NAME = "Hidden/ProceduralUI/UIShadowBlur";
        private const string SAMPLE_CONTENT_KEYWORD = "PROCEDURALUI_SAMPLE_CONTENT";

        private Graphic _graphic;
        private Mesh _casterMesh;
        private CommandBuffer _command;
        private MaterialPropertyBlock _properties;
        private RenderTexture _shadowTexture;
        [SerializeField, HideInInspector] private UIShadowRenderer _renderer;
        private Material _material;
        private Material _blurMaterial;
        private bool _materialSampleContent;
        private bool _dirty;
        private bool _hasShadow;
        private Bounds _lastBounds;
        private float _lastPadding;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private Vector3 _lastScale;

        public Color Color
        {
            get => color;
            set => SetValue(ref color, value);
        }

        public Vector2 Offset
        {
            get => offset;
            set => SetValue(ref offset, value);
        }

        public float Size
        {
            get => size;
            set => SetValue(ref size, Mathf.Max(0f, value));
        }

        public float Spread
        {
            get => spread;
            set => SetValue(ref spread, Mathf.Max(0f, value));
        }

        public bool SampleContentColor
        {
            get => sampleContentColor;
            set => SetValue(ref sampleContentColor, value);
        }

        private Graphic Graphic => _graphic != null ? _graphic : _graphic = GetComponent<Graphic>();

        private void OnEnable()
        {
            _casterMesh = new Mesh { name = "UI Shadow Caster", hideFlags = HideFlags.HideAndDontSave };
            _command = new CommandBuffer { name = "UI Shadow" };
            _properties = new MaterialPropertyBlock();
            Graphic.SetVerticesDirty();
            _dirty = true;
        }

        private void OnDisable()
        {
            DestroyRenderer();
            ReleaseShadowTexture();
            if (_material != null)
                DestroyImmediate(_material);
            _material = null;
            if (_blurMaterial != null)
                DestroyImmediate(_blurMaterial);
            _blurMaterial = null;
            _command?.Release();
            _command = null;
            _properties = null;
            if (_casterMesh != null)
                DestroyImmediate(_casterMesh);
            _casterMesh = null;
            _hasShadow = false;
            if (Graphic != null)
                Graphic.SetVerticesDirty();
        }

        public void ModifyMesh(Mesh mesh)
        {
        }

        public void ModifyMesh(VertexHelper verts)
        {
            if (!isActiveAndEnabled || _casterMesh == null)
                return;

            verts.FillMesh(_casterMesh);
            _casterMesh.RecalculateBounds();
            _dirty = true;
        }

        private void OnDidApplyAnimationProperties() => _dirty = true;

        private void OnRectTransformDimensionsChange() => _dirty = true;

#if UNITY_EDITOR
        private void OnValidate()
        {
            size = Mathf.Max(0f, size);
            spread = Mathf.Max(0f, spread);
            _dirty = true;
        }
#endif

        private void SetValue<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            _dirty = true;
        }

        public void Refresh() => _dirty = true;

        private void LateUpdate()
        {
            if (Graphic == null || Graphic.canvas == null || !Graphic.isActiveAndEnabled)
            {
                HideRenderer();
                return;
            }

            if (_dirty)
            {
                _dirty = false;
                Regenerate();
            }
            else if (_hasShadow && SourceMoved())
            {
                Layout(_lastBounds, _lastPadding);
            }
        }

        private bool SourceMoved()
        {
            var transform = Graphic.transform;
            return transform.position != _lastPosition
                || transform.rotation != _lastRotation
                || transform.lossyScale != _lastScale;
        }

        private void Regenerate()
        {
            if (!TryGetCasterMesh(out var mesh))
            {
                HideRenderer();
                return;
            }

            var bounds = mesh.bounds;
            if (bounds.size.x <= 0f || bounds.size.y <= 0f)
            {
                HideRenderer();
                return;
            }

            var scale = Mathf.Max(0.01f, Graphic.canvas.scaleFactor);
            var paddingUnits = size + spread;
            var paddingPixels = Mathf.CeilToInt(paddingUnits * scale);
            var contentWidth = Mathf.CeilToInt(bounds.size.x * scale);
            var contentHeight = Mathf.CeilToInt(bounds.size.y * scale);
            var textureWidth = Mathf.Clamp(contentWidth + paddingPixels * 2, 1, MAX_TEXTURE_SIZE);
            var textureHeight = Mathf.Clamp(contentHeight + paddingPixels * 2, 1, MAX_TEXTURE_SIZE);

            EnsureShadowTexture(textureWidth, textureHeight);

            var imprint = RenderTexture.GetTemporary(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);
            Capture(imprint, mesh, bounds, contentWidth, contentHeight, paddingPixels);

            var blurred = Blur(imprint, size * scale);
            Graphics.Blit(blurred, _shadowTexture);

            if (blurred != imprint)
                RenderTexture.ReleaseTemporary(blurred);
            RenderTexture.ReleaseTemporary(imprint);

            EnsureRenderer();
            EnsureMaterial();
            _renderer.Apply(_shadowTexture, color, _material);

            _lastBounds = bounds;
            _lastPadding = paddingUnits;
            _hasShadow = true;
            Layout(bounds, paddingUnits);
        }

        private bool TryGetCasterMesh(out Mesh mesh)
        {
            mesh = _casterMesh;
            return mesh != null && mesh.vertexCount > 0;
        }

        private void EnsureMaterial()
        {
            if (_material == null || _materialSampleContent != sampleContentColor)
            {
                if (_material != null)
                    DestroyImmediate(_material);

                var shader = LoadShader();
                if (shader == null)
                    return;

                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                _materialSampleContent = sampleContentColor;
                if (sampleContentColor)
                    _material.EnableKeyword(SAMPLE_CONTENT_KEYWORD);
            }
        }

        private static Shader LoadShader()
        {
            Shader shader = null;
            try
            {
                shader = ProceduralImageAssets.ShadowDisplayShader;
            }
            catch
            {
                shader = null;
            }

            return shader != null ? shader : Shader.Find(SHADER_NAME);
        }

        private void Capture(RenderTexture target, Mesh mesh, Bounds bounds, int contentWidth, int contentHeight, int paddingPixels)
        {
            var material = Graphic.materialForRendering;
            if (material == null)
                return;

            _command.Clear();
            _command.SetRenderTarget(target);
            _command.ClearRenderTarget(true, true, Color.clear);
            _command.SetViewport(new Rect(paddingPixels, paddingPixels, contentWidth, contentHeight));
            _command.SetViewProjectionMatrices(
                Matrix4x4.identity,
                Matrix4x4.Ortho(bounds.min.x, bounds.max.x, bounds.min.y, bounds.max.y, -1f, 1f));

            _properties.Clear();
            _properties.SetVector(ClipRectId, new Vector4(-1e30f, -1e30f, 1e30f, 1e30f));
            _properties.SetInt(ColorMaskId, (int)ColorWriteMask.All);

            var texture = Graphic.mainTexture;
            if (texture != null)
                _properties.SetTexture(MainTexId, texture);

            for (var pass = 0; pass < material.passCount; pass++)
                _command.DrawMesh(mesh, Matrix4x4.identity, material, 0, pass, _properties);

            Graphics.ExecuteCommandBuffer(_command);
        }

        private RenderTexture Blur(RenderTexture source, float radiusPixels)
        {
            if (radiusPixels < 0.5f)
                return source;

            EnsureBlurMaterial();
            if (_blurMaterial == null)
                return source;

            var downsample = 0;
            var working = radiusPixels;
            while (working > MAX_WORKING_RADIUS && downsample < MAX_DOWNSAMPLE)
            {
                downsample++;
                working *= 0.5f;
            }

            var width = Mathf.Max(1, source.width >> downsample);
            var height = Mathf.Max(1, source.height >> downsample);
            var a = RenderTexture.GetTemporary(width, height, 0, source.format);
            var b = RenderTexture.GetTemporary(width, height, 0, source.format);

            Graphics.Blit(source, a);

            var spacing = working * 0.2f;
            var horizontal = new Vector4(spacing / width, 0f, 0f, 0f);
            var vertical = new Vector4(0f, spacing / height, 0f, 0f);
            for (var pass = 0; pass < BLUR_PASSES; pass++)
            {
                _blurMaterial.SetVector(BlurDirId, horizontal);
                Graphics.Blit(a, b, _blurMaterial, 0);
                _blurMaterial.SetVector(BlurDirId, vertical);
                Graphics.Blit(b, a, _blurMaterial, 0);
            }

            var result = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            Graphics.Blit(a, result);

            RenderTexture.ReleaseTemporary(a);
            RenderTexture.ReleaseTemporary(b);
            return result;
        }

        private void EnsureBlurMaterial()
        {
            if (_blurMaterial != null)
                return;

            var shader = LoadBlurShader();
            if (shader != null)
                _blurMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        private static Shader LoadBlurShader()
        {
            Shader shader = null;
            try
            {
                shader = ProceduralImageAssets.ShadowBlurShader;
            }
            catch
            {
                shader = null;
            }

            return shader != null ? shader : Shader.Find(BLUR_SHADER_NAME);
        }

        private void Layout(Bounds bounds, float paddingUnits)
        {
            if (_renderer == null)
                return;

            var source = Graphic.rectTransform;
            var shadow = _renderer.transform as RectTransform;
            if (shadow == null)
                return;

            if (shadow.parent != source.parent)
                shadow.SetParent(source.parent, false);
            if (shadow.GetSiblingIndex() > source.GetSiblingIndex())
                shadow.SetSiblingIndex(source.GetSiblingIndex());

            shadow.localScale = source.localScale;
            shadow.localRotation = source.localRotation;

            var targetSize = new Vector2(bounds.size.x + paddingUnits * 2f, bounds.size.y + paddingUnits * 2f);
            if (shadow.sizeDelta != targetSize)
                shadow.sizeDelta = targetSize;

            var centerWorld = source.TransformPoint(bounds.center);
            var offsetWorld = source.TransformVector(offset.x, offset.y, 0f);
            shadow.position = centerWorld + offsetWorld;

            _lastPosition = source.position;
            _lastRotation = source.rotation;
            _lastScale = source.lossyScale;
        }

        private void EnsureShadowTexture(int width, int height)
        {
            if (_shadowTexture != null && _shadowTexture.width == width && _shadowTexture.height == height)
                return;

            ReleaseShadowTexture();
            _shadowTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "UI Shadow",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };
        }

        private void ReleaseShadowTexture()
        {
            if (_shadowTexture == null)
                return;

            _shadowTexture.Release();
            DestroyImmediate(_shadowTexture);
            _shadowTexture = null;
        }

        private void EnsureRenderer()
        {
            if (_renderer != null)
                return;

            var gameObject = new GameObject("UI Shadow")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            gameObject.AddComponent<CanvasRenderer>();
            gameObject.AddComponent<RawImage>();
            _renderer = gameObject.AddComponent<UIShadowRenderer>();
            _renderer.Owner = this;
        }

        private void HideRenderer()
        {
            _hasShadow = false;
            if (_renderer != null)
                _renderer.Image.color = Color.clear;
        }

        private void DestroyRenderer()
        {
            if (_renderer == null)
                return;

            var target = _renderer.gameObject;
            _renderer = null;

            if (Application.isPlaying)
            {
                Destroy(target);
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (target != null)
                    DestroyImmediate(target);
            };
#else
            DestroyImmediate(target);
#endif
        }
    }
}
