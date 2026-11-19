using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ProceduralUI.Tests
{
    public sealed class ProceduralImageRenderingTests
    {
        [UnityTest]
        public IEnumerator RenderShapeAndImageModes()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Rendering requires a graphics device.");

            var root = new GameObject("Rendering Test");
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
            var texture = CreateAtlasTexture();
            var sprite = Sprite.Create(texture, new Rect(32, 16, 64, 32), Vector2.one * 0.5f, 100, 0,
                SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
            Texture2D capture = null;
            var previousTarget = RenderTexture.active;

            try
            {
                var uniform = AddImage(canvas.transform, 0, "Uniform");
                uniform.CornerMode = CornerMode.Uniform;
                uniform.UniformRadius = 24;
                var individual = AddImage(canvas.transform, 1, "Individual");
                individual.CornerMode = CornerMode.Individual;
                individual.CornerRadii = new Vector4(40, 4, 24, 0);
                var round = AddImage(canvas.transform, 2, "Round");
                round.CornerMode = CornerMode.Round;
                var edge = AddImage(canvas.transform, 3, "Top Edge");
                edge.CornerMode = CornerMode.Edge;
                edge.UniformRadius = 35;

                var outline = AddImage(canvas.transform, 4, "Outline");
                outline.CornerMode = CornerMode.Uniform;
                outline.UniformRadius = 24;
                outline.BorderWidth = 5;
                var aspect = AddImage(canvas.transform, 5, "Atlas + Aspect");
                aspect.sprite = sprite;
                aspect.preserveAspect = true;
                aspect.CornerMode = CornerMode.Round;
                aspect.color = Color.white;
                var sliced = AddImage(canvas.transform, 6, "Sliced");
                sliced.sprite = sprite;
                sliced.type = Image.Type.Sliced;
                sliced.CornerMode = CornerMode.Uniform;
                sliced.UniformRadius = 20;
                sliced.color = Color.white;
                var tiled = AddImage(canvas.transform, 7, "Tiled");
                tiled.sprite = sprite;
                tiled.type = Image.Type.Tiled;
                tiled.CornerMode = CornerMode.Round;
                tiled.color = Color.white;

                var filled = AddImage(canvas.transform, 8, "Horizontal Fill");
                filled.type = Image.Type.Filled;
                filled.fillMethod = Image.FillMethod.Horizontal;
                filled.fillAmount = 0.6f;
                filled.CornerMode = CornerMode.Round;
                var radial = AddImage(canvas.transform, 9, "Radial Fill");
                radial.type = Image.Type.Filled;
                radial.fillMethod = Image.FillMethod.Radial360;
                radial.fillAmount = 0.7f;
                radial.CornerMode = CornerMode.Round;
                var soft = AddImage(canvas.transform, 10, "Soft Edge");
                soft.CornerMode = CornerMode.Round;
                soft.FalloffDistance = 8;
                var masked = AddImage(canvas.transform, 11, "RectMask2D");
                masked.CornerMode = CornerMode.Round;
                var mask = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D));
                mask.transform.SetParent(canvas.transform, false);
                var maskRect = mask.GetComponent<RectTransform>();
                maskRect.anchoredPosition = masked.rectTransform.anchoredPosition;
                maskRect.sizeDelta = new Vector2(95, 80);
                mask.GetComponent<RectMask2D>().softness = new Vector2Int(12, 12);
                masked.transform.SetParent(mask.transform, false);
                masked.rectTransform.anchoredPosition = Vector2.zero;

                yield return null;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                capture = new Texture2D(800, 600, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
                capture.Apply();
                var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Artifacts"));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, "procedural-images.png"), capture.EncodeToPNG());
                Assert.That(capture.GetPixel(100, 500).g, Is.GreaterThan(0.3f));
                Assert.That(capture.GetPixel(25, 545).g, Is.LessThan(0.2f));
                Assert.That(capture.GetPixel(100, 300).g, Is.LessThan(0.2f));
            }
            finally
            {
                RenderTexture.active = previousTarget;
                camera.targetTexture = null;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(capture);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        private static ProceduralImage AddImage(Transform parent, int index, string title)
        {
            var position = new Vector2(-300 + index % 4 * 200, 200 - index / 4 * 200);
            var image = new GameObject(title, typeof(RectTransform), typeof(ProceduralImage)).GetComponent<ProceduralImage>();
            image.transform.SetParent(parent, false);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = new Vector2(160, 100);
            image.color = new Color(0.2f, 0.8f, 0.65f, 1);
            var label = new GameObject(title + " Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            label.rectTransform.anchoredPosition = position + new Vector2(0, -70);
            label.rectTransform.sizeDelta = new Vector2(190, 30);
            label.text = title;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
            return image;
        }

        private static Texture2D CreateAtlasTexture()
        {
            var texture = new Texture2D(128, 128);
            var colors = new Color[128 * 128];
            for (var y = 0; y < 128; y++)
            for (var x = 0; x < 128; x++)
                colors[y * 128 + x] = x >= 32 && x < 96 && y >= 16 && y < 48
                    ? new Color(0.15f + (x - 32) / 100f, 0.45f, 0.95f, 1)
                    : Color.red;
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }
    }
}
