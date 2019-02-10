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
            Assert.IsTrue(images.Any(s => s == "data/a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/b.png"));
            Assert.IsTrue(images.Any(s => s == "data/c.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item1.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item2.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item3.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/folder/deep/nested/deepimage.png"));
        }

        [TestMethod]
        public void GivenDefaultConfiguration_ShouldFindImagesWithUppercaseExtensions()
        {
            var images = ImageQuery.FindImages("cased-data", new RepoConfiguration());

            Assert.AreEqual(2, images.Length, $"Images found {string.Join("; ", images)}.");
            Assert.IsTrue(images.Any(s => s == "cased-data/uppercase-jpg.JPG"));
            Assert.IsTrue(images.Any(s => s == "cased-data/uppercase-png.PNG"));
        }

        [TestMethod]
        public void GivenWronglyCasedIgnore_ShouldIgnoreImages()
        {
            var images = ImageQuery.FindImages("cased-data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "uppercase-jpg.jpg"
                }
            });

            Assert.AreEqual(1, images.Length, $"Images found {string.Join("; ", images)}.");
            Assert.IsTrue(images.Any(s => s == "cased-data/uppercase-png.PNG"));
        }

        [TestMethod]
        public void GivenFullIgnoreFullPath_ShouldIgnoreImage()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "data/folder/item2.png"
                }
            });

            Assert.AreEqual(6, images.Length, $"Images found {string.Join("; ", images)}.");
            Assert.IsTrue(images.Any(s => s == "data/a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/b.png"));
            Assert.IsTrue(images.Any(s => s == "data/c.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item1.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item3.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/folder/deep/nested/deepimage.png"));
        }

        [TestMethod]
        public void GivenFolderSlash_ShouldIgnoreImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "data/folder/"
                }
            });

            Assert.AreEqual(3, images.Length, $"Images found {string.Join("; ", images)}.");
            Assert.IsTrue(images.Any(s => s == "data/a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/b.png"));
            Assert.IsTrue(images.Any(s => s == "data/c.png"));
        }

        [TestMethod]
        public void GivenFolderWildcard_ShouldIgnoreImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "data/folder/*"
                }
            });

            Assert.AreEqual(3, images.Length, $"Images found {string.Join("; ", images)}.");
            Assert.IsTrue(images.Any(s => s == "data/a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/b.png"));
            Assert.IsTrue(images.Any(s => s == "data/c.png"));
        }

        [TestMethod]
        public void GivenFolderWildcardWithNoSlash_ShouldIgnoreImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "data/folder*"
                }
            });

            Assert.AreEqual(3, images.Length, $"Images found {string.Join("; ", images)}.");
            Assert.IsTrue(images.Any(s => s == "data/a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/b.png"));
            Assert.IsTrue(images.Any(s => s == "data/c.png"));
        }

        [TestMethod]
        public void GivenNestedSameFolder_ShouldFindAllImages()
        {
            var images = ImageQuery.FindImages("data_samenested", new RepoConfiguration());

            Assert.AreEqual(6, images.Length, $"Images found {string.Join("; ", images)}.");
            Assert.IsTrue(images.Any(s => s == "data_samenested/folder1/f1_a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data_samenested/folder1/test_images/f1_test_a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data_samenested/folder2/f2_b.png"));
            Assert.IsTrue(images.Any(s => s == "data_samenested/folder2/test_images/f2_test_b.png"));
            Assert.IsTrue(images.Any(s => s == "data_samenested/folder3/f3_c.png"));
            Assert.IsTrue(images.Any(s => s == "data_samenested/folder3/test_images/f3_test_c.png"));
        }

        [TestMethod]
        public void GivenNestedIgnoreFolder_ShouldIgnoreImages()
        {
            var images = ImageQuery.FindImages("data_samenested", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "**/test_images/**"
                }
            });

            Assert.AreEqual(3, images.Length, $"Images found {string.Join("; ", images)}.");
            Assert.IsTrue(images.Any(s => s == "data_samenested/folder1/f1_a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data_samenested/folder2/f2_b.png"));
            Assert.IsTrue(images.Any(s => s == "data_samenested/folder3/f3_c.png"));
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
            Assert.IsTrue(images.Any(s => s == "data/a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item3.jpg"));
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
        public void GivenRegexAll_ShouldIgnoreAllImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    ".*"
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
                    "data/folder/item2.png",
                    "data\\folder\\item3.jpg"
                }
            });

            Assert.AreEqual(5, images.Length, $"Images found {string.Join("; ", images)}.");
            Assert.IsTrue(images.Any(s => s == "data/a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/b.png"));
            Assert.IsTrue(images.Any(s => s == "data/c.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item1.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/deep/nested/deepimage.png"));
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
            Assert.IsTrue(images.Any(s => s == "data/a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/b.png"));
            Assert.IsTrue(images.Any(s => s == "data/c.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item1.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item2.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item3.jpg"));
        }

        [TestMethod]
        public void GivenDeeplyNestedPath_ShouldIgnoreImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "data/folder/deep/nested/*",
                }
            });

            Assert.AreEqual(6, images.Length, $"Images found {string.Join("; ", images)}.");
            Assert.IsTrue(images.Any(s => s == "data/a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/b.png"));
            Assert.IsTrue(images.Any(s => s == "data/c.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item1.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item2.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item3.jpg"));
        }

        [TestMethod]
        public void GivenDeeplyNestedPathOneUp_ShouldIgnoreImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration
            {
                IgnoredFiles = new[]
                {
                    "data/folder/deep/*",
                }
            });

            Assert.AreEqual(6, images.Length, $"Images found {string.Join("; ", images)}.");
            Assert.IsTrue(images.Any(s => s == "data/a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/b.png"));
            Assert.IsTrue(images.Any(s => s == "data/c.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item1.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item2.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item3.jpg"));
        }
    }
}
