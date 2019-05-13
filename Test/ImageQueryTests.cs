using System.Linq;
using Common;
using CompressImagesFunction.Find;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class ImageQueryTests
    {
        [TestMethod]
        public void GivenDefaultConfiguration_ShouldFindAllImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration()).ImagePaths;

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
        public void GivenDefaultConfiguration_ShouldSortImages()
        {
            var images = ImageQuery.FindImages("data", new RepoConfiguration()).ImagePaths;
            var expected = new[]
            {
                "data/a.jpg",
                "data/b.png",
                "data/c.png",
                "data/folder/deep/nested/deepimage.png",
                "data/folder/item1.png",
                "data/folder/item2.png",
                "data/folder/item3.jpg",
            };
            CollectionAssert.AreEqual(images, expected);
        }

        [TestMethod]
        public void GivenDefaultConfiguration_ShouldFindImagesWithUppercaseExtensions()
        {
            var images = ImageQuery.FindImages("cased-data", new RepoConfiguration()).ImagePaths;

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
            }).ImagePaths;

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
            }).ImagePaths;

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
            }).ImagePaths;

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
            }).ImagePaths;

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
            }).ImagePaths;

            Assert.AreEqual(3, images.Length, $"Images found {string.Join("; ", images)}.");
            Assert.IsTrue(images.Any(s => s == "data/a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/b.png"));
            Assert.IsTrue(images.Any(s => s == "data/c.png"));
        }

        [TestMethod]
        public void GivenNestedSameFolder_ShouldFindAllImages()
        {
            var images = ImageQuery.FindImages("data_samenested", new RepoConfiguration()).ImagePaths;

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
            }).ImagePaths;

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
            }).ImagePaths;

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
            }).ImagePaths;

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
            }).ImagePaths;

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
            }).ImagePaths;

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
            }).ImagePaths;

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
            }).ImagePaths;

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
            }).ImagePaths;

            Assert.AreEqual(6, images.Length, $"Images found {string.Join("; ", images)}.");
            Assert.IsTrue(images.Any(s => s == "data/a.jpg"));
            Assert.IsTrue(images.Any(s => s == "data/b.png"));
            Assert.IsTrue(images.Any(s => s == "data/c.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item1.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item2.png"));
            Assert.IsTrue(images.Any(s => s == "data/folder/item3.jpg"));
        }

        [TestMethod]
        public void GivenPagedImages_ShouldSegment()
        {
            var imageQueryResult1 = ImageQuery.FindImages("data", new RepoConfiguration(), page: 1, pageSize: 3);

            var expected1 = new[]
            {
                "data/a.jpg",
                "data/b.png",
                "data/c.png",

                // "data/folder/deep/nested/deepimage.png",
                // "data/folder/item1.png",
                // "data/folder/item2.png",
                // "data/folder/item3.jpg",
            };
            CollectionAssert.AreEqual(imageQueryResult1.ImagePaths, expected1);
            Assert.IsTrue(imageQueryResult1.HasMoreImages);

            var imageQueryResult2 = ImageQuery.FindImages("data", new RepoConfiguration(), page: 2, pageSize: 3);

            var expected2 = new[]
            {
                // "data/a.jpg",
                // "data/b.png",
                // "data/c.png",
                "data/folder/deep/nested/deepimage.png",
                "data/folder/item1.png",
                "data/folder/item2.png",

                // "data/folder/item3.jpg",
            };
            CollectionAssert.AreEqual(imageQueryResult2.ImagePaths, expected2);
            Assert.IsTrue(imageQueryResult2.HasMoreImages);

            var imageQueryResult3 = ImageQuery.FindImages("data", new RepoConfiguration(), page: 3, pageSize: 3);

            var expected3 = new[]
            {
                // "data/a.jpg",
                // "data/b.png",
                // "data/c.png",
                // "data/folder/deep/nested/deepimage.png",
                // "data/folder/item1.png",
                // "data/folder/item2.png",
                "data/folder/item3.jpg",
            };
            CollectionAssert.AreEqual(imageQueryResult3.ImagePaths, expected3);
            Assert.IsFalse(imageQueryResult3.HasMoreImages);
        }
    }
}
