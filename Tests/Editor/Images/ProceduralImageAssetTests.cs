using System.Linq;
using NUnit.Framework;
using ProceduralUI.Editor;
using UnityEditor;
using UnityEngine;

namespace ProceduralUI.Tests
{
    public sealed class ProceduralImageAssetTests
    {
        [Test]
        public void RegistrationPreservesExistingAssetsAndIsIdempotent()
        {
            var original = PlayerSettings.GetPreloadedAssets();
            var assets = ProceduralImageAssets.Instance;
            try
            {
                var other = ProceduralImageAssets.WhiteSprite;
                var initial = original.Where(asset => asset != assets).Append(other).ToArray();
                PlayerSettings.SetPreloadedAssets(initial);

                ProceduralImageAssetRegistration.Register();
                ProceduralImageAssetRegistration.Register();

                var registered = PlayerSettings.GetPreloadedAssets();
                Assert.That(registered.Take(initial.Length), Is.EqualTo(initial));
                Assert.That(registered.Count(asset => asset == assets), Is.EqualTo(1));
            }
            finally
            {
                PlayerSettings.SetPreloadedAssets(original);
            }
        }

        [Test]
        public void PreloadedAssetReferencesMaterialShaderAndSpriteOutsideResources()
        {
            var assets = ProceduralImageAssets.Instance;
            var dependencies = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(assets), true);
            Assert.That(dependencies, Does.Contain(AssetDatabase.GetAssetPath(ProceduralImageAssets.Material)));
            Assert.That(dependencies, Does.Contain(AssetDatabase.GetAssetPath(ProceduralImageAssets.Material.shader)));
            Assert.That(dependencies, Does.Contain(AssetDatabase.GetAssetPath(ProceduralImageAssets.WhiteSprite)));
            Assert.That(dependencies.Any(path => path.Contains("/Resources/")), Is.False);
        }

        [Test]
        public void RuntimeCreatedImagesShareDefaultAssets()
        {
            var first = new GameObject("First", typeof(ProceduralImage));
            var second = new GameObject("Second", typeof(ProceduralImage));
            try
            {
                var firstImage = first.GetComponent<ProceduralImage>();
                var secondImage = second.GetComponent<ProceduralImage>();
                Assert.That(firstImage.defaultMaterial, Is.SameAs(ProceduralImageAssets.Material));
                Assert.That(firstImage.defaultMaterial, Is.SameAs(secondImage.defaultMaterial));
                Assert.That(firstImage.sprite, Is.SameAs(ProceduralImageAssets.WhiteSprite));
                Assert.That(firstImage.sprite, Is.SameAs(secondImage.sprite));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }
    }
}
