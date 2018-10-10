using System.Linq;
using Common;
using CompressImagesFunction;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class ImageQueryTests
    {
        [TestMethod]
        public void GivenDefaultConfiguration_ShouldFindAllImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration());

            Assert.AreEqual(7, images.Length, $"Images found {string.Join("; ", images)}.");
            images.Contains("a.jpg");
            images.Contains("b.png");
            images.Contains("c.png");
            images.Contains("item1.png");
            images.Contains("item2.png");
            images.Contains("item3.jpg");
            images.Contains("deepimage.png");
        }

        [TestMethod]
        public void GivenDefaultConfiguration_ShouldFindImagesWithUppercaseExtensionsToo()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration());

            Assert.AreEqual(9, images.Length, $"Images found {string.Join("; ", images)}.");
            images.Contains("a.JPG");
            images.Contains("b.PNG");
        }

        [TestMethod]
        public void GivenFullIgnoreFullPath_ShouldIgnoreImage()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "folder/item2.png"
                }
            });

            Assert.AreEqual(6, images.Length, $"Images found {string.Join("; ", images)}.");
            images.Contains("a.jpg");
            images.Contains("b.png");
            images.Contains("c.png");
            images.Contains("item1.png");
            images.Contains("item3.jpg");
            images.Contains("deepimage.png");
        }

        [TestMethod]
        public void GivenFolderSlash_ShouldIgnoreImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "folder/"
                }
            });

            Assert.AreEqual(3, images.Length, $"Images found {string.Join("; ", images)}.");
            images.Contains("a.jpg");
            images.Contains("b.png");
            images.Contains("c.png");
        }

        [TestMethod]
        public void GivenFolderWildcard_ShouldIgnoreImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "folder/*"
                }
            });

            Assert.AreEqual(3, images.Length, $"Images found {string.Join("; ", images)}.");
            images.Contains("a.jpg");
            images.Contains("b.png");
            images.Contains("c.png");
        }

        [TestMethod]
        public void GivenWildcardExtension_ShouldIgnoreImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "*.png"
                }
            });

            Assert.AreEqual(2, images.Length, $"Images found {string.Join("; ", images)}.");
            images.Contains("a.jpg");
            images.Contains("item3.jpg");
        }

        [TestMethod]
        public void GivenWildcard_ShouldIgnoreAllImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "*"
                }
            });

            Assert.AreEqual(0, images.Length, $"Images found {string.Join("; ", images)}.");
        }

        [TestMethod]
        public void GivenDifferentSlashPath_ShouldIgnoreImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "folder/item2.png",
                    "folder\\item3.jpg"
                }
            });

            Assert.AreEqual(5, images.Length, $"Images found {string.Join("; ", images)}.");
            images.Contains("a.jpg");
            images.Contains("b.png");
            images.Contains("c.png");
            images.Contains("item1.png");
            images.Contains("deepimage.png");
        }

        [TestMethod]
        public void GivenDeeplyNestedImage_ShouldIgnoreImage()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "deepimage.png",
                }
            });

            Assert.AreEqual(6, images.Length, $"Images found {string.Join("; ", images)}.");
            images.Contains("a.jpg");
            images.Contains("b.png");
            images.Contains("c.png");
            images.Contains("item1.png");
            images.Contains("item2.png");
            images.Contains("item3.jpg");
        }

        [TestMethod]
        public void GivenDeeplyNestedPath_ShouldIgnoreImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "folder/deep/nested/*",
                }
            });

            Assert.AreEqual(6, images.Length, $"Images found {string.Join("; ", images)}.");
            images.Contains("a.jpg");
            images.Contains("b.png");
            images.Contains("c.png");
            images.Contains("item1.png");
            images.Contains("item2.png");
            images.Contains("item3.jpg");
        }

        [TestMethod]
        public void GivenDeeplyNestedPathOneUp_ShouldIgnoreImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "folder/deep/*",
                }
            });

            Assert.AreEqual(6, images.Length, $"Images found {string.Join("; ", images)}.");
            images.Contains("a.jpg");
            images.Contains("b.png");
            images.Contains("c.png");
            images.Contains("item1.png");
            images.Contains("item2.png");
            images.Contains("item3.jpg");
        }
    }
}
