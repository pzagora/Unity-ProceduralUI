using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ProceduralUI.Tests
{
    public sealed class GradientRenderingTests
    {
        [UnityTest]
        public IEnumerator GradientsRenderWithCornersFillsMasksAndCanvasOpacity()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Rendering requires a graphics device.");

            var root = new GameObject("Gradient Rendering");
            var canvas = new GameObject("Canvas", typeof(Canvas)).GetComponent<Canvas>();
            canvas.transform.SetParent(root.transform);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 600);
            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(root.transform);
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 300;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.055f, 0.08f, 1);
            var target = new RenderTexture(800, 600, 24);
            camera.targetTexture = target;
            canvas.worldCamera = camera;
            Texture2D capture = null;
            var previousTarget = RenderTexture.active;

            try
            {
                AddImage(canvas.transform, 0, "Linear");
                var angled = AddImage(canvas.transform, 1, "Diagonal");
                angled.Gradient.Angle = 45;
                var corners = AddImage(canvas.transform, 2, "Four Corners");
                corners.Gradient.Type = GradientType.Corners;
                corners.Gradient.TopLeft = Color.red;
                corners.Gradient.TopRight = Color.yellow;
                corners.Gradient.BottomLeft = Color.blue;
                corners.Gradient.BottomRight = Color.green;
                var ramp = CreateRamp();
                var multi = AddImage(canvas.transform, 3, "Multi-stop Ramp");
                multi.Gradient.Type = GradientType.Ramp;
                multi.Gradient.SetRamp(ramp);
                multi.Gradient.Angle = 315;

                var outline = AddImage(canvas.transform, 4, "Gradient Outline");
                outline.BorderWidth = 5;
                var fill = AddImage(canvas.transform, 5, "Partial Fill");
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillAmount = 0.65f;
                fill.Gradient.Type = GradientType.Ramp;
                fill.Gradient.SetRamp(ramp);
                fill.Gradient.Angle = 90;
                var fixedRamp = AddImage(canvas.transform, 6, "Fixed Ramp");
                fixedRamp.Gradient.Type = GradientType.Ramp;
                ramp.mode = GradientMode.Fixed;
                fixedRamp.Gradient.SetRamp(ramp);
                fixedRamp.Gradient.Angle = 90;
                var perceptual = AddImage(canvas.transform, 7, "Perceptual Ramp");
                perceptual.Gradient.Type = GradientType.Ramp;
                ramp.mode = GradientMode.PerceptualBlend;
                perceptual.Gradient.SetRamp(ramp);
                perceptual.Gradient.Angle = 90;

                var faded = AddImage(canvas.transform, 8, "CanvasGroup 50%");
                faded.Gradient.BlendMode = GradientBlendMode.Override;
                faded.gameObject.AddComponent<CanvasGroup>().alpha = 0.5f;
                var multiplied = AddImage(canvas.transform, 9, "Tint × Gradient");
                multiplied.color = new Color(0.5f, 0.5f, 0.5f, 1);
                var masked = AddImage(canvas.transform, 10, "RectMask2D");
                var mask = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D));
                mask.transform.SetParent(canvas.transform, false);
                var maskRect = mask.GetComponent<RectTransform>();
                maskRect.anchoredPosition = masked.rectTransform.anchoredPosition;
                maskRect.sizeDelta = new Vector2(100, 80);
                mask.GetComponent<RectMask2D>().softness = new Vector2Int(10, 10);
                masked.transform.SetParent(mask.transform, false);
                masked.rectTransform.anchoredPosition = Vector2.zero;
                var standalone = new GameObject("Standalone", typeof(RectTransform), typeof(Image), typeof(UIGradient));
                standalone.transform.SetParent(canvas.transform, false);
                var standaloneRect = standalone.GetComponent<RectTransform>();
                standaloneRect.anchoredPosition = new Vector2(300, -200);
                standaloneRect.sizeDelta = new Vector2(160, 100);
                standalone.GetComponent<UIGradient>().Gradient.StartColor = Color.red;
                standalone.GetComponent<UIGradient>().Gradient.EndColor = Color.blue;
                AddLabel(canvas.transform, new Vector2(300, -200), "Standalone Image");

                yield return null;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                capture = new Texture2D(800, 600, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
                capture.Apply();
                var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Artifacts"));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, "gradients.png"), capture.EncodeToPNG());
                Assert.That(capture.GetPixel(100, 470).r, Is.GreaterThan(capture.GetPixel(100, 530).r));
                Assert.That(capture.GetPixel(100, 530).b, Is.GreaterThan(capture.GetPixel(100, 470).b));
                Assert.That(capture.GetPixel(25, 545).r, Is.LessThan(0.15f));
                Assert.That(capture.GetPixel(100, 300).r, Is.LessThan(0.15f));
                Assert.That(capture.GetPixel(100, 100).r, Is.LessThan(capture.GetPixel(100, 500).r * 0.75f));
                Assert.That(capture.GetPixel(700, 70).r, Is.GreaterThan(capture.GetPixel(700, 130).r));
            }
            finally
            {
                RenderTexture.active = previousTarget;
                camera.targetTexture = null;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(capture);
            }
        }

        private static ProceduralImage AddImage(Transform parent, int index, string title)
        {
            var position = new Vector2(-300 + index % 4 * 200, 200 - index / 4 * 200);
            var image = new GameObject(title, typeof(RectTransform), typeof(ProceduralImage)).GetComponent<ProceduralImage>();
            image.transform.SetParent(parent, false);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = new Vector2(160, 100);
            image.CornerMode = CornerMode.Uniform;
            image.UniformRadius = 24;
            image.Gradient.Enabled = true;
            image.Gradient.StartColor = Color.red;
            image.Gradient.EndColor = Color.blue;
            AddLabel(parent, position, title);
            return image;
        }

        private static void AddLabel(Transform parent, Vector2 position, string title)
        {
            var label = new GameObject(title + " Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            label.rectTransform.anchoredPosition = position + new Vector2(0, -70);
            label.rectTransform.sizeDelta = new Vector2(190, 30);
            label.text = title;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
        }

        private static Gradient CreateRamp()
            => new()
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.1f, 0.4f, 1), 0),
                    new GradientColorKey(new Color(0.9f, 0.15f, 0.3f), 0.45f),
                    new GradientColorKey(new Color(1, 0.75f, 0.1f), 1)
                }
            };
    }
}
