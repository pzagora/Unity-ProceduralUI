using System.Collections;
using System.Reflection;
using NUnit.Framework;
using ProceduralUI.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace ProceduralUI.Tests
{
    public sealed class ProceduralImageTests
    {
        private GameObject _canvas;
        private ProceduralImage _image;
        private Texture2D _texture;
        private Sprite _sprite;

        [SetUp]
        public void SetUp()
        {
            _canvas = new GameObject("Canvas", typeof(Canvas));
            _image = new GameObject("Image", typeof(RectTransform), typeof(ProceduralImage)).GetComponent<ProceduralImage>();
            _image.transform.SetParent(_canvas.transform, false);
            _image.rectTransform.sizeDelta = new Vector2(200, 200);
            _texture = new Texture2D(128, 128);
            _sprite = Sprite.Create(_texture, new Rect(32, 16, 64, 32), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect,
                new Vector4(4, 4, 4, 4));
            _image.sprite = _sprite;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
        }

        [Test]
        public void EnablingPreservesImageConfiguration()
        {
            var material = new Material(_image.defaultMaterial);
            _image.preserveAspect = true;
            _image.material = material;
            _image.enabled = false;
            _image.enabled = true;
            Assert.That(_image.preserveAspect, Is.True);
            Assert.That(_image.material, Is.SameAs(material));
            Object.DestroyImmediate(material);
        }

        [TestCase(0f, 0.5f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1f, 1f)]
        public void PreservedAspectMatchesTheGeneratedMesh(float pivotX, float pivotY)
        {
            _image.preserveAspect = true;
            _image.FalloffDistance = 0;
            _image.rectTransform.pivot = new Vector2(pivotX, pivotY);
            var rect = ProceduralImageGeometry.GetDrawingRect(_image);
            Assert.That(rect.width / rect.height, Is.EqualTo(2).Within(0.001f));
            using var mesh = Populate();
            var vertex = default(UIVertex);
            mesh.PopulateUIVertex(ref vertex, 0);
            Assert.That(vertex.position.x, Is.EqualTo(rect.x).Within(0.001f));
            Assert.That(vertex.position.y, Is.EqualTo(rect.y).Within(0.001f));
        }

        [Test]
        public void ShapeCoordinatesDoNotUseSpriteAtlasCoordinates()
        {
            using var mesh = Populate();
            var vertex = default(UIVertex);
            mesh.PopulateUIVertex(ref vertex, 0);
            Assert.That(vertex.uv0.x, Is.GreaterThan(0));
            Assert.That(vertex.uv1.z, Is.EqualTo(0).Within(0.001f));
            Assert.That(vertex.uv1.w, Is.EqualTo(0).Within(0.001f));
            mesh.PopulateUIVertex(ref vertex, 2);
            Assert.That(vertex.uv1.z, Is.EqualTo(1).Within(0.001f));
            Assert.That(vertex.uv1.w, Is.EqualTo(1).Within(0.001f));
        }

        [TestCase(Image.Type.Simple)]
        [TestCase(Image.Type.Sliced)]
        [TestCase(Image.Type.Tiled)]
        [TestCase(Image.Type.Filled)]
        public void ImageTypesGenerateFiniteShapeData(Image.Type type)
        {
            _image.type = type;
            _image.fillAmount = 0.4f;
            _image.CornerMode = CornerMode.Round;
            _image.FalloffDistance = 0;
            using var mesh = Populate();
            Assert.That(mesh.currentVertCount, Is.GreaterThan(0));
            var vertex = default(UIVertex);
            for (var index = 0; index < mesh.currentVertCount; index++)
            {
                mesh.PopulateUIVertex(ref vertex, index);
                Assert.That(float.IsNaN(vertex.uv2.x) || float.IsInfinity(vertex.uv3.y), Is.False);
                Assert.That(vertex.uv1.z, Is.InRange(-0.001f, 1.001f));
                Assert.That(vertex.uv1.w, Is.InRange(-0.001f, 1.001f));
            }
        }

        [Test]
        public void PartialFillKeepsTheFullShapeCoordinateSpace()
        {
            _image.type = Image.Type.Filled;
            _image.fillMethod = Image.FillMethod.Horizontal;
            _image.fillOrigin = 0;
            _image.fillAmount = 0.25f;
            using var mesh = Populate();
            var vertex = default(UIVertex);
            var maximum = 0f;
            for (var index = 0; index < mesh.currentVertCount; index++)
            {
                mesh.PopulateUIVertex(ref vertex, index);
                maximum = Mathf.Max(maximum, vertex.uv1.z);
            }
            Assert.That(maximum, Is.EqualTo(0.25f).Within(0.001f));
        }

        [TestCase(0, 200)]
        [TestCase(200, 0)]
        [TestCase(0, 0)]
        public void CollapsedLayoutsProduceAnEmptyMesh(float width, float height)
        {
            _image.rectTransform.sizeDelta = new Vector2(width, height);
            using var mesh = Populate();
            Assert.That(mesh.currentVertCount, Is.Zero);
        }

        [Test]
        public void RuntimeChangesDirtyGeometryOnlyWhenValuesChange()
        {
            var updates = 0;
            _image.RegisterDirtyVerticesCallback(() => updates++);
            _image.UniformRadius = 12;
            _image.UniformRadius = 12;
            Assert.That(updates, Is.EqualTo(1));
            _image.RoundedEdge = RoundedEdge.Bottom;
            Assert.That(updates, Is.EqualTo(2));
        }

        [Test]
        public void GradientChangesUseTheImageDirtyLifecycle()
        {
            var updates = 0;
            _image.RegisterDirtyVerticesCallback(() => updates++);
            _image.Gradient.Enabled = true;
            _image.Gradient.StartColor = Color.red;
            _image.Gradient.StartColor = Color.red;
            Assert.That(updates, Is.EqualTo(2));
            _image.enabled = false;
            var before = updates;
            _image.Gradient.EndColor = Color.blue;
            Assert.That(updates, Is.EqualTo(before));
            _image.enabled = true;
            Assert.That(updates, Is.GreaterThan(before));
        }

        [Test]
        public void RampSplittingKeepsRoundedShapeCoordinatesValid()
        {
            _image.CornerMode = CornerMode.Round;
            _image.preserveAspect = true;
            _image.Gradient.Enabled = true;
            _image.Gradient.Type = GradientType.Ramp;
            _image.Gradient.Angle = 315;
            _image.Gradient.SetRamp(new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.red, 0), new GradientColorKey(Color.green, 0.5f), new GradientColorKey(Color.blue, 1) }
            });
            using var mesh = Populate();
            Assert.That(mesh.currentVertCount, Is.GreaterThan(4));
            var vertex = default(UIVertex);
            for (var index = 0; index < mesh.currentVertCount; index++)
            {
                mesh.PopulateUIVertex(ref vertex, index);
                Assert.That(vertex.uv1.z, Is.InRange(-0.001f, 1.001f));
                Assert.That(vertex.uv1.w, Is.InRange(-0.001f, 1.001f));
                Assert.That(vertex.uv1.x, Is.EqualTo(201).Within(0.001f));
                Assert.That(vertex.uv1.y, Is.EqualTo(101).Within(0.001f));
            }
        }

        [Test]
        public void ReparentingConfiguresTheNewCanvas()
        {
            var other = new GameObject("Other Canvas", typeof(Canvas));
            _image.transform.SetParent(other.transform, false);
            var channels = other.GetComponent<Canvas>().additionalShaderChannels;
            Assert.That(channels.HasFlag(AdditionalCanvasShaderChannels.TexCoord3), Is.True);
            _image.transform.SetParent(_canvas.transform, false);
            Object.DestroyImmediate(other);
        }

        [Test]
        public void ZeroRadiiStayFiniteWhenTheRectCollapses()
        {
            Assert.That(ProceduralImageGeometry.CalculateRadii(default, CornerMode.Individual, 0, Vector4.zero, default),
                Is.EqualTo(Vector4.zero));
        }

        [Test]
        public void OversizedCornersFitAdjacentEdges()
        {
            var radii = ProceduralImageGeometry.CalculateRadii(new Rect(0, 0, 100, 40), CornerMode.Individual, 0,
                new Vector4(100, 40, 20, 60), default);
            Assert.That(radii.x + radii.w, Is.EqualTo(40).Within(0.001f));
            Assert.That(radii.x / radii.w, Is.EqualTo(100f / 60).Within(0.001f));
        }

        [Test]
        public void ShaderCompilesAndMaterialsAreShared()
        {
            Assert.That(_image.defaultMaterial.shader, Is.Not.Null);
            Assert.That(ShaderUtil.ShaderHasError(_image.defaultMaterial.shader), Is.False);
            var other = new GameObject("Other", typeof(ProceduralImage));
            Assert.That(other.GetComponent<ProceduralImage>().defaultMaterial, Is.SameAs(_image.defaultMaterial));
            Object.DestroyImmediate(other);
        }

        [Test]
        public void InspectorCanEditInlineCornersWithoutModifierComponents()
        {
            _image.CornerMode = CornerMode.Individual;
            var serialized = new SerializedObject(_image);
            serialized.FindProperty("cornerRadii").FindPropertyRelative("x").floatValue = 24;
            serialized.ApplyModifiedProperties();
            Assert.That(_image.CornerRadii.x, Is.EqualTo(24));
            Assert.That(_image.GetComponents<MonoBehaviour>().Length, Is.EqualTo(1));
            var editor = UnityEditor.Editor.CreateEditor(_image);
            Assert.That(editor.GetType().FullName, Is.EqualTo("ProceduralUI.Editor.ProceduralImageEditor"));
            Object.DestroyImmediate(editor);
        }

        [Test]
        public void InlineSettingsSupportUndo()
        {
            Undo.IncrementCurrentGroup();
            var serialized = new SerializedObject(_image);
            Undo.RecordObject(_image, "Change corners");
            serialized.FindProperty("cornerMode").intValue = (int)CornerMode.Uniform;
            serialized.FindProperty("uniformRadius").floatValue = 25;
            serialized.ApplyModifiedProperties();
            Undo.FlushUndoRecordObjects();
            Assert.That(_image.UniformRadius, Is.EqualTo(25));
            Undo.PerformUndo();
            Assert.That(_image.UniformRadius, Is.Zero);
            Assert.That(_image.CornerMode, Is.EqualTo(CornerMode.None));
            Undo.ClearUndo(_image);
        }

        [Test]
        public void CachedGradientApiTracksSerializedChangesAndUndo()
        {
            var gradient = _image.Gradient;
            Undo.IncrementCurrentGroup();
            var serialized = new SerializedObject(_image);
            serialized.FindProperty("gradient").FindPropertyRelative("angle").floatValue = 45;
            serialized.ApplyModifiedProperties();
            Undo.FlushUndoRecordObjects();
            Assert.That(gradient.Angle, Is.EqualTo(45));
            Undo.PerformUndo();
            Assert.That(gradient.Angle, Is.Zero);
            gradient.Angle = 90;
            serialized.Update();
            Assert.That(serialized.FindProperty("gradient").FindPropertyRelative("angle").floatValue, Is.EqualTo(90));
            Assert.That(_image.Gradient, Is.SameAs(gradient));
            Undo.ClearUndo(_image);
        }

        [Test]
        public void SpriteMeshUsesTheAspectPreservedShapeBounds()
        {
            _image.useSpriteMesh = true;
            _image.preserveAspect = true;
            _image.FalloffDistance = 0;
            using var mesh = Populate();
            var rect = ProceduralImageGeometry.GetDrawingRect(_image);
            var vertex = default(UIVertex);
            for (var index = 0; index < mesh.currentVertCount; index++)
            {
                mesh.PopulateUIVertex(ref vertex, index);
                Assert.That(vertex.position.x, Is.InRange(rect.x - 0.001f, rect.xMax + 0.001f));
                Assert.That(vertex.position.y, Is.InRange(rect.y - 0.001f, rect.yMax + 0.001f));
                Assert.That(vertex.uv1.z, Is.InRange(-0.001f, 1.001f));
                Assert.That(vertex.uv1.w, Is.InRange(-0.001f, 1.001f));
            }
        }

        [Test]
        public void EncodingDoesNotAllocateAfterWarmup()
        {
            using var mesh = Populate();
            var rect = ProceduralImageGeometry.GetDrawingRect(_image);
            var radii = Vector4.one * 20;
            ProceduralImageGeometry.Encode(mesh, rect, radii, 2, 0, false);
            var before = System.GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 1000; index++)
                ProceduralImageGeometry.Encode(mesh, rect, radii, 2, 0, false);
            var allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }

        [UnityTest]
        public IEnumerator LayoutAndAnimationChangesRebuildWithoutPolling()
        {
            _image.CornerMode = CornerMode.Round;
            yield return null;
            Canvas.ForceUpdateCanvases();
            var clip = new AnimationClip();
            try
            {
                _image.rectTransform.sizeDelta = new Vector2(300, 80);
                Canvas.ForceUpdateCanvases();
                var mesh = _image.canvasRenderer.GetMesh();
                Assert.That(mesh.bounds.size.x, Is.EqualTo(301).Within(0.01f));
                Assert.That(mesh.bounds.size.y, Is.EqualTo(81).Within(0.01f));
                clip.SetCurve(string.Empty, typeof(ProceduralImage), "falloffDistance", AnimationCurve.Constant(0, 1, 4));
                clip.SampleAnimation(_image.gameObject, 0.5f);
                Canvas.ForceUpdateCanvases();
                mesh = _image.canvasRenderer.GetMesh();
                Assert.That(mesh.bounds.size.x, Is.EqualTo(304).Within(0.01f));

                _image.Gradient.Enabled = true;
                _image.Gradient.StartColor = Color.red;
                _image.Gradient.EndColor = Color.blue;
                Object.DestroyImmediate(clip);
                clip = new AnimationClip();
                var bindings = AnimationUtility.GetAnimatableBindings(_image.gameObject, _image.gameObject);
                Assert.That(System.Array.Exists(bindings, binding => binding.propertyName == "gradient.angle"), Is.True,
                    string.Join(", ", System.Array.ConvertAll(bindings, binding => binding.propertyName)));
                clip.SetCurve(string.Empty, typeof(ProceduralImage), "gradient.angle", AnimationCurve.Constant(0, 1, 90));
                clip.SampleAnimation(_image.gameObject, 0.5f);
                Canvas.ForceUpdateCanvases();
                mesh = _image.canvasRenderer.GetMesh();
                Assert.That(_image.Gradient.Angle, Is.EqualTo(90));
                Assert.That(mesh.colors32[1].r, Is.GreaterThan(250));
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void ManagedMaskToggleAddsAndRemovesUnityMask()
        {
            Assert.That(ManagedMask.HasMask(_image.gameObject), Is.False);

            ManagedMask.SetMaskChildren(_image.gameObject, true);
            Assert.That(_image.GetComponent<Mask>(), Is.Not.Null);
            Assert.That(ManagedMask.HasMask(_image.gameObject), Is.True);

            ManagedMask.SetMaskChildren(_image.gameObject, false);
            Assert.That(_image.GetComponent<Mask>(), Is.Null);
        }

        private VertexHelper Populate()
        {
            var mesh = new VertexHelper();
            typeof(ProceduralImage).GetMethod("OnPopulateMesh", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Invoke(_image, new object[] { mesh });
            return mesh;
        }
    }
}
