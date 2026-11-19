using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralUI.Tests
{
    public sealed class GradientMeshTests
    {
        private GradientMeshRenderer _renderer;
        private ImageGradient _gradient;
        private VertexHelper _mesh;
        private GradientSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _renderer = new GradientMeshRenderer();
            _settings = GradientSettings.Create();
            _gradient = new ImageGradient(GetSettings, () => { }) { Enabled = true, StartColor = Color.red, EndColor = Color.blue };
            _mesh = new VertexHelper();
            AddQuad(_mesh, 0, 0, 100, 100);
        }

        [TearDown]
        public void TearDown()
            => _mesh.Dispose();

        [TestCase(0, 0)]
        [TestCase(90, 1)]
        [TestCase(180, 2)]
        [TestCase(270, 3)]
        public void LinearDirectionMatchesTheConfiguredAngle(float angle, int redCorner)
        {
            _gradient.Angle = angle;
            Apply();
            AssertColor(Vertex(redCorner).color, Color.red);
            AssertColor(Vertex((redCorner + 2) % 4).color, Color.blue);
            Assert.That(_mesh.currentVertCount, Is.EqualTo(4));
        }

        [Test]
        public void MultiplyAndOverrideRespectIntensityAndAlpha()
        {
            var original = new Color(0.2f, 0.4f, 0.6f, 0.5f);
            var tint = new Color(0.8f, 0.6f, 0.4f, 0.25f);
            _gradient.Intensity = 0.5f;
            _gradient.BlendMode = GradientBlendMode.Multiply;
            Assert.That(_settings.Blend(original, tint), Is.EqualTo(Color.Lerp(original, original * tint, 0.5f)));
            _gradient.BlendMode = GradientBlendMode.Override;
            Assert.That(_settings.Blend(original, tint), Is.EqualTo(Color.Lerp(original, tint, 0.5f)));
        }

        [Test]
        public void CornerColorsRemainIndependent()
        {
            _gradient.Type = GradientType.Corners;
            _gradient.TopLeft = Color.red;
            _gradient.TopRight = Color.green;
            _gradient.BottomLeft = Color.blue;
            _gradient.BottomRight = Color.yellow;
            Apply();
            AssertColor(Vertex(0).color, Color.blue);
            AssertColor(Vertex(1).color, Color.red);
            AssertColor(Vertex(2).color, Color.green);
            AssertColor(Vertex(3).color, Color.yellow);
        }

        [TestCase(0)]
        [TestCase(45)]
        [TestCase(90)]
        [TestCase(180)]
        [TestCase(315)]
        public void RampCutsPreserveAreaAndAllVertexChannels(float angle)
        {
            ConfigureRamp();
            _gradient.Angle = angle;
            Apply();
            Assert.That(Area(_mesh), Is.EqualTo(10000).Within(0.02f));
            for (var index = 0; index < _mesh.currentVertCount; index++)
            {
                var vertex = Vertex(index);
                Assert.That(vertex.uv0.z, Is.EqualTo(vertex.position.x * 0.01f).Within(0.0001f));
                Assert.That(vertex.uv1.w, Is.EqualTo(vertex.position.y * 0.01f).Within(0.0001f));
                Assert.That(vertex.uv2.z, Is.EqualTo(0.75f).Within(0.0001f));
                Assert.That(vertex.uv3.w, Is.EqualTo(0.25f).Within(0.0001f));
                Assert.That(vertex.normal, Is.EqualTo(Vector3.back));
                Assert.That(vertex.tangent, Is.EqualTo(new Vector4(1, 0, 0, -1)));
            }
        }

        [Test]
        public void RampCreatesVerticesAtIndependentColorAndAlphaStops()
        {
            var ramp = ConfigureRamp();
            Apply();
            var greenFound = false;
            var alphaFound = false;
            for (var index = 0; index < _mesh.currentVertCount; index++)
            {
                var vertex = Vertex(index);
                var position = vertex.position.y / 100f;
                AssertColor(vertex.color, ramp.Evaluate(position));
                greenFound |= Mathf.Abs(position - 0.4f) < 0.0001f;
                alphaFound |= Mathf.Abs(position - 0.7f) < 0.0001f;
            }
            Assert.That(greenFound && alphaFound, Is.True);
        }

        [Test]
        public void FixedRampHasHardTransitionsWithinEveryTriangle()
        {
            var ramp = ConfigureRamp();
            ramp.mode = GradientMode.Fixed;
            _gradient.SetRamp(ramp);
            Apply();
            var triangles = new List<UIVertex>();
            _mesh.GetUIVertexStream(triangles);
            for (var index = 0; index < triangles.Count; index += 3)
            {
                var center = (triangles[index].position + triangles[index + 1].position + triangles[index + 2].position) / 3;
                if (TriangleArea(triangles[index], triangles[index + 1], triangles[index + 2]) < 0.0001f)
                    continue;
                var expected = ramp.Evaluate(center.y / 100f);
                AssertColor(triangles[index].color, expected);
                AssertColor(triangles[index + 1].color, expected);
                AssertColor(triangles[index + 2].color, expected);
            }
        }

        [Test]
        public void RuntimeRampUpdatesReplaceTheCachedStops()
        {
            ConfigureRamp();
            Apply();
            var ramp = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.red, 0), new GradientColorKey(Color.yellow, 0.2f), new GradientColorKey(Color.blue, 1) }
            };
            _gradient.SetRamp(ramp);
            ResetMesh();
            Apply();
            var found = false;
            for (var index = 0; index < _mesh.currentVertCount; index++)
                found |= Mathf.Abs(Vertex(index).position.y - 20) < 0.001f;
            Assert.That(found, Is.True);
        }

        [Test]
        public void PerceptualRampUsesBoundedIntermediateSamples()
        {
            var ramp = ConfigureRamp();
            ramp.mode = GradientMode.PerceptualBlend;
            _gradient.SetRamp(ramp);
            Apply();
            Assert.That(_mesh.currentVertCount, Is.LessThan(1000));
            Assert.That(Area(_mesh), Is.EqualTo(10000).Within(0.02f));
            for (var index = 0; index < _mesh.currentVertCount; index++)
            {
                var vertex = Vertex(index);
                AssertColor(vertex.color, ramp.Evaluate(vertex.position.y / 100));
            }
        }

        [Test]
        public void RampConfigurationDoesNotExposeMutableInternalState()
        {
            var ramp = ConfigureRamp();
            ramp.SetKeys(new[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.black, 1) }, ramp.alphaKeys);
            Assert.That(_gradient.CopyRamp().Evaluate(0), Is.EqualTo(Color.red));
            var copy = _gradient.CopyRamp();
            copy.mode = GradientMode.Fixed;
            Assert.That(_gradient.CopyRamp().mode, Is.EqualTo(GradientMode.Blend));
        }

        [TestCase(GradientType.Linear)]
        [TestCase(GradientType.Corners)]
        [TestCase(GradientType.Ramp)]
        public void RebuildsDoNotAllocateAfterWarmup(GradientType type)
        {
            ConfigureRamp();
            _gradient.Type = type;
            Apply();
            ResetMesh();
            Apply();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 100; index++)
            {
                ResetMesh();
                Apply();
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }

        [Test]
        public void DisabledAndZeroIntensityGradientsLeaveTheMeshUntouched()
        {
            _gradient.Enabled = false;
            Apply();
            AssertColor(Vertex(0).color, Color.white);
            _gradient.Enabled = true;
            _gradient.Intensity = 0;
            Apply();
            AssertColor(Vertex(0).color, Color.white);
            Assert.That(_mesh.currentVertCount, Is.EqualTo(4));
        }

        [Test]
        public void CollapsedLayoutDoesNotGenerateInvalidColors()
        {
            _renderer.Apply(_mesh, new Rect(0, 0, 0, 100), _settings);
            AssertColor(Vertex(0).color, Color.white);
        }

        [Test]
        public void OversizedMeshesKeepTheirOriginalGeometry()
        {
            ConfigureRamp();
            _mesh.Clear();
            for (var index = 0; index < 4000; index++)
                AddQuad(_mesh, 0, 0, 100, 100);
            Apply();
            Assert.That(_mesh.currentVertCount, Is.EqualTo(16000));
            Assert.That(_mesh.currentIndexCount, Is.EqualTo(24000));
        }

        private Gradient ConfigureRamp()
        {
            var ramp = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.red, 0), new GradientColorKey(Color.green, 0.4f), new GradientColorKey(Color.blue, 1) },
                alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.25f, 0.7f), new GradientAlphaKey(1, 1) }
            };
            _gradient.Type = GradientType.Ramp;
            _gradient.SetRamp(ramp);
            return ramp;
        }

        private void ResetMesh()
        {
            _mesh.Clear();
            AddQuad(_mesh, 0, 0, 100, 100);
        }

        private void Apply()
            => _renderer.Apply(_mesh, new Rect(0, 0, 100, 100), _settings);

        private ref GradientSettings GetSettings()
            => ref _settings;

        private UIVertex Vertex(int index)
        {
            var vertex = default(UIVertex);
            _mesh.PopulateUIVertex(ref vertex, index);
            return vertex;
        }

        private static void AddQuad(VertexHelper mesh, float x, float y, float width, float height)
        {
            var start = mesh.currentVertCount;
            AddVertex(mesh, x, y);
            AddVertex(mesh, x, y + height);
            AddVertex(mesh, x + width, y + height);
            AddVertex(mesh, x + width, y);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }

        private static void AddVertex(VertexHelper mesh, float x, float y)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(x, y);
            vertex.color = Color.white;
            vertex.uv0 = new Vector4(x / 100, y / 100, x / 100, y / 100);
            vertex.uv1 = new Vector4(100, 100, x / 100, y / 100);
            vertex.uv2 = new Vector4(0, 0, 0.75f, 0.5f);
            vertex.uv3 = new Vector4(0, 0, 0.75f, 0.25f);
            vertex.normal = Vector3.back;
            vertex.tangent = new Vector4(1, 0, 0, -1);
            mesh.AddVert(vertex);
        }

        private static float Area(VertexHelper mesh)
        {
            var triangles = new List<UIVertex>();
            mesh.GetUIVertexStream(triangles);
            var area = 0f;
            for (var index = 0; index < triangles.Count; index += 3)
                area += TriangleArea(triangles[index], triangles[index + 1], triangles[index + 2]);
            return area;
        }

        private static float TriangleArea(UIVertex a, UIVertex b, UIVertex c)
            => Vector3.Cross(b.position - a.position, c.position - a.position).magnitude * 0.5f;

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(1f / 255));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(1f / 255));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(1f / 255));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(1f / 255));
        }
    }
}
